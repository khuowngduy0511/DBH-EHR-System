#!/usr/bin/env bash
#
# SPDX-License-Identifier: Apache-2.0
#
# EHR Network - Hyperledger Fabric network for Electronic Health Records
# with three organizations: Hospital1, Hospital2, and Clinic

ROOTDIR=$(cd "$(dirname "$0")" && pwd)
export PATH=${ROOTDIR}/bin:${PWD}/bin:$PATH
export VERBOSE=false

# Prevent Git Bash MSYS path mangling on Windows (e.g. /var/run → C:\Program Files\Git\var)
export MSYS_NO_PATHCONV=1

# push to the required directory & set a trap to go back if needed
pushd ${ROOTDIR} > /dev/null
trap "popd > /dev/null" EXIT

# Windows-compatible PWD for Fabric Go binaries (fabric-ca-client, peer, configtxgen, osnadmin)
# pwd -W returns D:/path on Git Bash/MSYS2; falls back to $PWD on Linux/Mac
WIN_PWD=$(pwd -W 2>/dev/null || pwd)
export WIN_PWD

export FABRIC_CFG_PATH=${WIN_PWD}/configtx

. scripts/utils.sh

: ${CONTAINER_CLI:="docker"}
if command -v ${CONTAINER_CLI}-compose > /dev/null 2>&1; then
    : ${CONTAINER_CLI_COMPOSE:="${CONTAINER_CLI}-compose"}
else
    : ${CONTAINER_CLI_COMPOSE:="${CONTAINER_CLI} compose"}
fi
infoln "Using ${CONTAINER_CLI} and ${CONTAINER_CLI_COMPOSE}"

# Obtain CONTAINER_IDS and remove them
function clearContainers() {
  infoln "Removing remaining containers"
  ${CONTAINER_CLI} rm -f $(${CONTAINER_CLI} ps -aq --filter label=service=hyperledger-fabric) 2>/dev/null || true
  ${CONTAINER_CLI} rm -f $(${CONTAINER_CLI} ps -aq --filter name='dev-peer*') 2>/dev/null || true
  ${CONTAINER_CLI} kill "$(${CONTAINER_CLI} ps -q --filter name=ccaas)" 2>/dev/null || true
}

function removeUnwantedImages() {
  infoln "Removing generated chaincode docker images"
  ${CONTAINER_CLI} image rm -f $(${CONTAINER_CLI} images -aq --filter reference='dev-peer*') 2>/dev/null || true
}

NONWORKING_VERSIONS="^1\.0\. ^1\.1\. ^1\.2\. ^1\.3\. ^1\.4\."

function checkPrereqs() {
  peer version > /dev/null 2>&1

  if [[ $? -ne 0 || ! -d "config" ]]; then
    errorln "Peer binary and configuration files not found.."
    errorln
    errorln "Follow the instructions in the Fabric docs to install the Fabric Binaries:"
    errorln "https://hyperledger-fabric.readthedocs.io/en/latest/install.html"
    exit 1
  fi

  LOCAL_VERSION=$(peer version | sed -ne 's/^ Version: //p')
  DOCKER_IMAGE_VERSION=$(${CONTAINER_CLI} run --rm hyperledger/fabric-peer:latest peer version | sed -ne 's/^ Version: //p')

  infoln "LOCAL_VERSION=$LOCAL_VERSION"
  infoln "DOCKER_IMAGE_VERSION=$DOCKER_IMAGE_VERSION"

  if [ "$LOCAL_VERSION" != "$DOCKER_IMAGE_VERSION" ]; then
    warnln "Local fabric binaries and docker images are out of sync. This may cause problems."
  fi

  for UNSUPPORTED_VERSION in $NONWORKING_VERSIONS; do
    infoln "$LOCAL_VERSION" | grep -q $UNSUPPORTED_VERSION
    if [ $? -eq 0 ]; then
      fatalln "Local Fabric binary version of $LOCAL_VERSION does not match the versions supported by the EHR network."
    fi

    infoln "$DOCKER_IMAGE_VERSION" | grep -q $UNSUPPORTED_VERSION
    if [ $? -eq 0 ]; then
      fatalln "Fabric Docker image version of $DOCKER_IMAGE_VERSION does not match the versions supported by the EHR network."
    fi
  done
}

function getCaDataVolumeFromContainer() {
  local ca_container="$1"
  ${CONTAINER_CLI} inspect -f '{{range .Mounts}}{{if eq .Destination "/etc/hyperledger/fabric-ca-server"}}{{.Name}}{{end}}{{end}}' "$ca_container" 2>/dev/null
}

function stageCaCertsFromVolume() {
  local ca_container="$1"
  local org_fabric_ca_subdir="$2"

  mkdir -p "${PWD}/${org_fabric_ca_subdir}"

  # On MINGW/Git Bash, docker cp needs Windows-style host paths (D:\...)
  # pwd -W gives the Windows path; on Linux/Mac it falls back to PWD
  local host_base
  host_base=$(cd "${PWD}" && pwd -W 2>/dev/null || pwd)
  local host_dir="${host_base}/${org_fabric_ca_subdir}"

  # Use docker cp directly — more reliable on Windows Docker Desktop
  set -x
  ${CONTAINER_CLI} cp "${ca_container}:/etc/hyperledger/fabric-ca-server/ca-cert.pem" "${host_dir}/ca-cert.pem"
  res=$?
  if [ $res -eq 0 ]; then
    ${CONTAINER_CLI} cp "${ca_container}:/etc/hyperledger/fabric-ca-server/tls-cert.pem" "${host_dir}/tls-cert.pem"
    res=$?
  fi
  { set +x; } 2>/dev/null

  if [ $res -ne 0 ]; then
    fatalln "Failed to stage CA certs from container '${ca_container}'"
  fi
}

function waitForAndStageCaCerts() {
  local ca_container="$1"
  local org_fabric_ca_subdir="$2"
  local attempts=1
  local rc=1

  while [[ $rc -ne 0 && $attempts -le $MAX_RETRY ]]; do
    sleep 1
    stageCaCertsFromVolume "$ca_container" "$org_fabric_ca_subdir" && rc=0 || rc=1
    attempts=$((attempts + 1))
  done

  if [ $rc -ne 0 ]; then
    fatalln "CA '${ca_container}' is not ready after ${MAX_RETRY} retries"
  fi
}

function restoreOrganizationsFromCryptoVolume() {
  : ${CONTAINER_CLI:="docker"}
  local crypto_volume="${FABRIC_CRYPTO_VOLUME:-fabric-crypto}"

  if ! ${CONTAINER_CLI} volume inspect "${crypto_volume}" >/dev/null 2>&1; then
    fatalln "Crypto volume '${crypto_volume}' does not exist"
  fi

  mkdir -p "${PWD}/organizations"

  set -x
  ${CONTAINER_CLI} run --rm \
    -v "${crypto_volume}:/crypto:ro" \
    -v "${PWD}:/workspace" \
    busybox sh -c 'test -d /crypto/peerOrganizations && test -d /crypto/ordererOrganizations && rm -rf /workspace/organizations/peerOrganizations /workspace/organizations/ordererOrganizations && cp -r /crypto/peerOrganizations /workspace/organizations/ && cp -r /crypto/ordererOrganizations /workspace/organizations/'
  res=$?
  { set +x; } 2>/dev/null

  if [ $res -ne 0 ]; then
    fatalln "Failed to restore organizations from docker volume '${crypto_volume}'"
  fi

  infoln "Restored organizations crypto from volume '${crypto_volume}'"
}

function ensureOrganizationsOnHost() {
  if [ ! -d "organizations/peerOrganizations" ] || [ ! -d "organizations/ordererOrganizations" ]; then
    infoln "Host organizations crypto missing; restoring from docker volume"
    restoreOrganizationsFromCryptoVolume
  fi
}

function cleanupHostOrganizations() {
  if [ -d "organizations/peerOrganizations" ] || [ -d "organizations/ordererOrganizations" ]; then
    infoln "Cleaning host organizations crypto (volume remains source-of-truth)"
    rm -rf organizations/peerOrganizations organizations/ordererOrganizations
  fi
}

# Create Organization crypto material using cryptogen or Fabric CA
function createOrgs() {
  if [ -e "organizations/peerOrganizations" ] || [ -e "organizations/ordererOrganizations" ]; then
    rm -rf organizations/peerOrganizations organizations/ordererOrganizations
  fi

  if [ "$CRYPTO" == "cryptogen" ]; then
    which cryptogen
    if [ "$?" -ne 0 ]; then
      fatalln "cryptogen tool not found. exiting"
    fi
    infoln "Generating certificates using cryptogen tool"

    infoln "Creating Hospital1 Identities"
    set -x
    cryptogen generate --config=./organizations/cryptogen/crypto-config-hospital1.yaml --output="organizations"
    res=$?
    { set +x; } 2>/dev/null
    if [ $res -ne 0 ]; then
      fatalln "Failed to generate certificates..."
    fi

    infoln "Creating Hospital2 Identities"
    set -x
    cryptogen generate --config=./organizations/cryptogen/crypto-config-hospital2.yaml --output="organizations"
    res=$?
    { set +x; } 2>/dev/null
    if [ $res -ne 0 ]; then
      fatalln "Failed to generate certificates..."
    fi

    infoln "Creating Clinic Identities"
    set -x
    cryptogen generate --config=./organizations/cryptogen/crypto-config-clinic.yaml --output="organizations"
    res=$?
    { set +x; } 2>/dev/null
    if [ $res -ne 0 ]; then
      fatalln "Failed to generate certificates..."
    fi

    infoln "Creating Orderer Org Identities"
    set -x
    cryptogen generate --config=./organizations/cryptogen/crypto-config-orderer.yaml --output="organizations"
    res=$?
    { set +x; } 2>/dev/null
    if [ $res -ne 0 ]; then
      fatalln "Failed to generate certificates..."
    fi

  elif [ "$CRYPTO" == "CA" ]; then
    infoln "Generating certificates using Fabric Certificate Authorities"
    ${CONTAINER_CLI_COMPOSE} -f compose/$COMPOSE_FILE_CA up -d 2>&1

    . organizations/fabric-ca/registerEnroll.sh

    # Wait for CA servers to start
    infoln "Waiting for Fabric CA servers to start..."
    sleep 5

    # --- Hospital1 CA readiness ---
    while :
    do
      if [ ! -f "organizations/fabric-ca/hospital1/tls-cert.pem" ]; then
        sleep 1
      else
        break
      fi
    done

    export FABRIC_CA_CLIENT_HOME=${WIN_PWD}/organizations/peerOrganizations/hospital1.ehr.com/
    COUNTER=0
    rc=1
    while [[ $rc -ne 0 && $COUNTER -lt $MAX_RETRY ]]; do
      sleep 1
      set -x
      fabric-ca-client getcainfo -u https://admin:adminpw@localhost:7054 --caname ca-hospital1 --tls.certfiles "${WIN_PWD}/organizations/fabric-ca/hospital1/ca-cert.pem"
      res=$?
      { set +x; } 2>/dev/null
      rc=$res
      COUNTER=$((COUNTER + 1))
    done

    infoln "Creating Hospital1 Identities"
    createHospital1

    # --- Hospital2 CA readiness ---
    while :
    do
      if [ ! -f "organizations/fabric-ca/hospital2/tls-cert.pem" ]; then
        sleep 1
      else
        break
      fi
    done

    export FABRIC_CA_CLIENT_HOME=${WIN_PWD}/organizations/peerOrganizations/hospital2.ehr.com/
    COUNTER=0
    rc=1
    while [[ $rc -ne 0 && $COUNTER -lt $MAX_RETRY ]]; do
      sleep 1
      set -x
      fabric-ca-client getcainfo -u https://admin:adminpw@localhost:8054 --caname ca-hospital2 --tls.certfiles "${WIN_PWD}/organizations/fabric-ca/hospital2/ca-cert.pem"
      res=$?
      { set +x; } 2>/dev/null
      rc=$res
      COUNTER=$((COUNTER + 1))
    done

    infoln "Creating Hospital2 Identities"
    createHospital2

    # --- Clinic CA readiness ---
    while :
    do
      if [ ! -f "organizations/fabric-ca/clinic/tls-cert.pem" ]; then
        sleep 1
      else
        break
      fi
    done

    export FABRIC_CA_CLIENT_HOME=${WIN_PWD}/organizations/peerOrganizations/clinic.ehr.com/
    COUNTER=0
    rc=1
    while [[ $rc -ne 0 && $COUNTER -lt $MAX_RETRY ]]; do
      sleep 1
      set -x
      fabric-ca-client getcainfo -u https://admin:adminpw@localhost:10054 --caname ca-clinic --tls.certfiles "${WIN_PWD}/organizations/fabric-ca/clinic/ca-cert.pem"
      res=$?
      { set +x; } 2>/dev/null
      rc=$res
      COUNTER=$((COUNTER + 1))
    done

    infoln "Creating Clinic Identities"
    createClinic

    # --- Orderer CA readiness ---
    while :
    do
      if [ ! -f "organizations/fabric-ca/ordererOrg/tls-cert.pem" ]; then
        sleep 1
      else
        break
      fi
    done

    export FABRIC_CA_CLIENT_HOME=${WIN_PWD}/organizations/ordererOrganizations/ehr.com/
    COUNTER=0
    rc=1
    while [[ $rc -ne 0 && $COUNTER -lt $MAX_RETRY ]]; do
      sleep 1
      set -x
      fabric-ca-client getcainfo -u https://admin:adminpw@localhost:9054 --caname ca-orderer --tls.certfiles "${WIN_PWD}/organizations/fabric-ca/ordererOrg/ca-cert.pem"
      res=$?
      { set +x; } 2>/dev/null
      rc=$res
      COUNTER=$((COUNTER + 1))
    done

    infoln "Creating Orderer Org Identities"
    createOrderer

  else
    fatalln "Unknown crypto mode: $CRYPTO. Use 'cryptogen' or 'CA'"
  fi

  infoln "Generating CCP files for Hospital1, Hospital2, and Clinic"
  ./organizations/ccp-generate.sh
}

# Bring up the peer and orderer nodes using docker compose.
function networkUp() {
  checkPrereqs

  # generate artifacts if they don't exist
  if [ ! -d "organizations/peerOrganizations" ]; then
    createOrgs
  fi
  infoln "Finish Generating CCP files"

  COMPOSE_FILES="-f compose/${COMPOSE_FILE_BASE} -f compose/${CONTAINER_CLI}/${CONTAINER_CLI}-${COMPOSE_FILE_BASE} "
  
  if [ "${DATABASE}" == "couchdb" ]; then
    COMPOSE_FILES="${COMPOSE_FILES} -f compose/${COMPOSE_FILE_COUCH}"
  fi
  infoln "Starting network..."  
  DOCKER_SOCK="${DOCKER_SOCK}" ${CONTAINER_CLI_COMPOSE} ${COMPOSE_FILES} up -d 2>&1
  infoln "Network started successfully" 
  $CONTAINER_CLI ps -a
  if [ $? -ne 0 ]; then
    fatalln "Unable to start network"
  fi
}

function startExplorer() {
  local explorerScript="${ROOTDIR}/explorer/explorer.sh"

  if [ ! -f "$explorerScript" ]; then
    warnln "Explorer script not found at ${explorerScript}. Skipping Explorer startup."
    return 0
  fi

  infoln "Starting Hyperledger Explorer..."
  (
    cd "${ROOTDIR}/explorer"
    bash ./explorer.sh start
  )

  if [ $? -ne 0 ]; then
    warnln "Explorer failed to start. Network is up; run explorer/explorer.sh start manually to retry."
    return 0
  fi

  successln "Explorer started successfully"
}

# Create the channel, join peers, update anchor peers
function createChannel() {
  bringUpNetwork="false"

  if ! $CONTAINER_CLI info > /dev/null 2>&1 ; then
    fatalln "$CONTAINER_CLI network is required to be running to create a channel"
  fi

  # check if all containers are present
  CONTAINERS=($(${CONTAINER_CLI} ps | grep hyperledger/ | awk '{print $2}'))
  len=$(echo ${#CONTAINERS[@]})

  if [[ $len -ge 4 ]] && [[ ! -d "organizations/peerOrganizations" ]]; then
    echo "Bringing network down to sync certs with containers"
    networkDown
  fi

  [[ $len -lt 4 ]] || [[ ! -d "organizations/peerOrganizations" ]] && bringUpNetwork="true" || echo "Network Running Already"

  if [ $bringUpNetwork == "true"  ]; then
    infoln "Bringing up network"
    networkUp
  fi

  if [ "$CHANNEL_MODE" == "multi" ]; then
    infoln "Multi-channel mode: creating 3 channels"
    for ch in "$CHANNEL_CONSENT" "$CHANNEL_AUDIT" "$CHANNEL_EHR_HASH"; do
      infoln "Creating channel '${ch}'..."
      scripts/createChannel.sh $ch $CLI_DELAY $MAX_RETRY $VERBOSE
    done
    successln "All 3 channels created: $CHANNEL_CONSENT, $CHANNEL_AUDIT, $CHANNEL_EHR_HASH"
  else
    scripts/createChannel.sh $CHANNEL_NAME $CLI_DELAY $MAX_RETRY $VERBOSE
  fi
}


## Call the script to deploy a chaincode to the channel
function deployCC() {
  if [ "$CHANNEL_MODE" == "multi" ]; then
    infoln "Multi-channel mode: deploying channel-specific chaincodes"

    # channel -> (cc_name, cc_path)
    CHANNELS=($CHANNEL_CONSENT $CHANNEL_AUDIT $CHANNEL_EHR_HASH)
    CC_NAMES=("consentcc" "auditcc" "ehrcc")
    CC_PATHS=("./chaincode/consent-js" "./chaincode/audit-js" "./chaincode/ehr-js")

    for idx in ${!CHANNELS[@]}; do
      ch=${CHANNELS[$idx]}
      cc_name=${CC_NAMES[$idx]}
      cc_path=${CC_PATHS[$idx]}

      infoln "Deploying chaincode '${cc_name}' from '${cc_path}' to channel '${ch}'..."
      scripts/deployCC.sh $ch $cc_name $cc_path $CC_SRC_LANGUAGE $CC_VERSION $CC_SEQUENCE $CC_INIT_FCN $CC_END_POLICY $CC_COLL_CONFIG $CLI_DELAY $MAX_RETRY $VERBOSE
      if [ $? -ne 0 ]; then
        fatalln "Deploying chaincode '${cc_name}' to channel '${ch}' failed"
      fi
    done
    successln "Chaincodes deployed: consent-js -> ${CHANNEL_CONSENT}, audit-js -> ${CHANNEL_AUDIT}, ehr-js -> ${CHANNEL_EHR_HASH}"
  else
    scripts/deployCC.sh $CHANNEL_NAME $CC_NAME $CC_SRC_PATH $CC_SRC_LANGUAGE $CC_VERSION $CC_SEQUENCE $CC_INIT_FCN $CC_END_POLICY $CC_COLL_CONFIG $CLI_DELAY $MAX_RETRY $VERBOSE
    if [ $? -ne 0 ]; then
      fatalln "Deploying chaincode failed"
    fi
  fi
}

## Call the script to package the chaincode
function packageChaincode() {
  infoln "Packaging chaincode"
  scripts/packageCC.sh $CC_NAME $CC_SRC_PATH $CC_SRC_LANGUAGE $CC_VERSION true

  if [ $? -ne 0 ]; then
    fatalln "Packaging the chaincode failed"
  fi
}

## Call the script to list installed and committed chaincode on a peer
function listChaincode() {
  ensureOrganizationsOnHost

  export FABRIC_CFG_PATH=${WIN_PWD}/config
  . scripts/envVar.sh
  . scripts/ccutils.sh
  setGlobals $ORG
  println
  queryInstalledOnPeer
  println
  listAllCommitted
}

## Call the script to invoke
function invokeChaincode() {
  ensureOrganizationsOnHost

  export FABRIC_CFG_PATH=${WIN_PWD}/config
  . scripts/envVar.sh
  . scripts/ccutils.sh
  setGlobals $ORG
  chaincodeInvoke $ORG $CHANNEL_NAME $CC_NAME $CC_INVOKE_CONSTRUCTOR
}

## Call the script to query chaincode
function queryChaincode() {
  ensureOrganizationsOnHost

  export FABRIC_CFG_PATH=${WIN_PWD}/config
  . scripts/envVar.sh
  . scripts/ccutils.sh
  setGlobals $ORG
  chaincodeQuery $ORG $CHANNEL_NAME $CC_NAME $CC_QUERY_CONSTRUCTOR
}


# Tear down running network
function networkDown() {
  COMPOSE_BASE_FILES="-f compose/${COMPOSE_FILE_BASE} -f compose/${CONTAINER_CLI}/${CONTAINER_CLI}-${COMPOSE_FILE_BASE}"
  COMPOSE_COUCH_FILES="-f compose/${COMPOSE_FILE_COUCH}"
  COMPOSE_CA_FILES="-f compose/${COMPOSE_FILE_CA}"
  COMPOSE_FILES="${COMPOSE_BASE_FILES} ${COMPOSE_COUCH_FILES} ${COMPOSE_CA_FILES}"

  if [ "${CONTAINER_CLI}" == "docker" ]; then
    DOCKER_SOCK=$DOCKER_SOCK ${CONTAINER_CLI_COMPOSE} ${COMPOSE_FILES} down --volumes --remove-orphans
  elif [ "${CONTAINER_CLI}" == "podman" ]; then
    ${CONTAINER_CLI_COMPOSE} ${COMPOSE_FILES} down --volumes
  else
    fatalln "Container CLI ${CONTAINER_CLI} not supported"
  fi

  # Don't remove the generated artifacts -- note, the ledgers are always removed
  if [ "$MODE" != "restart" ]; then
    # Bring down the network, deleting the volumes
    ${CONTAINER_CLI} volume rm docker_orderer.ehr.com docker_peer0.hospital1.ehr.com docker_peer0.hospital2.ehr.com docker_peer0.clinic.ehr.com 2>/dev/null
    #Cleanup the chaincode containers
    clearContainers
    #Cleanup images
    removeUnwantedImages
    # remove orderer block and other channel configuration transactions and certs
    ${CONTAINER_CLI} run --rm -v "$(pwd):/data" busybox sh -c 'cd /data && rm -rf system-genesis-block/*.block organizations/peerOrganizations organizations/ordererOrganizations'
    # remove channel and script artifacts
    ${CONTAINER_CLI} run --rm -v "$(pwd):/data" busybox sh -c 'cd /data && rm -rf channel-artifacts log.txt *.tar.gz'
    # remove Fabric CA artifacts
    ${CONTAINER_CLI} run --rm -v "$(pwd):/data" busybox sh -c 'cd /data && rm -rf organizations/fabric-ca/hospital1/msp organizations/fabric-ca/hospital1/tls-cert.pem organizations/fabric-ca/hospital1/ca-cert.pem organizations/fabric-ca/hospital1/IssuerPublicKey organizations/fabric-ca/hospital1/IssuerRevocationPublicKey organizations/fabric-ca/hospital1/fabric-ca-server.db'
    ${CONTAINER_CLI} run --rm -v "$(pwd):/data" busybox sh -c 'cd /data && rm -rf organizations/fabric-ca/hospital2/msp organizations/fabric-ca/hospital2/tls-cert.pem organizations/fabric-ca/hospital2/ca-cert.pem organizations/fabric-ca/hospital2/IssuerPublicKey organizations/fabric-ca/hospital2/IssuerRevocationPublicKey organizations/fabric-ca/hospital2/fabric-ca-server.db'
    ${CONTAINER_CLI} run --rm -v "$(pwd):/data" busybox sh -c 'cd /data && rm -rf organizations/fabric-ca/clinic/msp organizations/fabric-ca/clinic/tls-cert.pem organizations/fabric-ca/clinic/ca-cert.pem organizations/fabric-ca/clinic/IssuerPublicKey organizations/fabric-ca/clinic/IssuerRevocationPublicKey organizations/fabric-ca/clinic/fabric-ca-server.db'
    ${CONTAINER_CLI} run --rm -v "$(pwd):/data" busybox sh -c 'cd /data && rm -rf organizations/fabric-ca/ordererOrg/msp organizations/fabric-ca/ordererOrg/tls-cert.pem organizations/fabric-ca/ordererOrg/ca-cert.pem organizations/fabric-ca/ordererOrg/IssuerPublicKey organizations/fabric-ca/ordererOrg/IssuerRevocationPublicKey organizations/fabric-ca/ordererOrg/fabric-ca-server.db'
  fi
}

# print help message
function printHelp() {
  println "Usage: "
  println "  network.sh <Mode> [Flags]"
  println "    Modes:"
  println "      up - Bring up Fabric orderer and peer nodes."
  println "      down - Bring down the network."
  println "      restart - Restart the network."
  println "      createChannel - Create and join a channel."
  println "      deployCC - Deploy a chaincode to a channel."
  println "      cc - Chaincode operations (package, list, invoke, query)."
  println
  println "    Flags:"
  println "      -c <channel name> - Name of channel to create (defaults to \"ehr-channel\")"
  println "      -channels <mode> - Channel mode: 'single' (default, 1 channel) or 'multi' (3 channels: consent, audit, ehr-hash)"
  println "      -r <max retry> - CLI times out after specified number of attempts (defaults to 5)"
  println "      -d <delay> - CLI delays for a specified number of seconds (defaults to 3)"
  println "      -s <dbtype> - Peer state database to deploy: goleveldb (default) or couchdb"
  println "      -verbose - Verbose mode"
  println "      -crypto <mode> - Crypto material generation: cryptogen (default) or 'Certificate Authorities'"
  println "      -ccn <name> - Chaincode name."
  println "      -ccv <version> - Chaincode version."
  println "      -ccs <sequence> - Chaincode definition sequence."
  println "      -ccp <path> - File path to the chaincode."
  println "      -ccl <language> - Programming language of the chaincode: go, java, javascript, typescript"
  println "      -ccep <policy> - (Optional) Chaincode endorsement policy."
  println "      -cccg <collection-config> - (Optional) File path to private data collections configuration file."
  println "      -cci <fcn name> - (Optional) Name of the chaincode init function to call."
  println
  println "  Examples:"
  println "    network.sh up -s couchdb"
  println "    network.sh up -s couchdb -crypto 'Certificate Authorities'"
  println "    network.sh createChannel -c ehr-channel"
  println "    network.sh createChannel -channels multi              # creates 3 channels"
  println "    network.sh deployCC -ccn ehrcc -ccp ./chaincode-javascript -ccl javascript"
  println "    network.sh deployCC -channels multi -ccn ehrcc -ccp ./chaincode-javascript -ccl javascript  # deploy to 3 channels"
}

. ./network.config

# use this as the default docker-compose yaml definition
COMPOSE_FILE_BASE=compose-ehr-net.yaml
# docker-compose.yaml file if you are using couchdb
COMPOSE_FILE_COUCH=compose-couch.yaml
# docker-compose.yaml file for certificate authorities
COMPOSE_FILE_CA=compose-ca.yaml

# Get docker sock path from environment variable
SOCK="${DOCKER_HOST:-/var/run/docker.sock}"
DOCKER_SOCK="${SOCK##unix://}"

# Parse commandline args

## Parse mode
if [[ $# -lt 1 ]] ; then
  printHelp
  exit 0
else
  MODE=$1
  shift
fi

## if no parameters are passed, show the help for cc
if [ "$MODE" == "cc" ] && [[ $# -lt 1 ]]; then
  printHelp $MODE
  exit 0
fi

# parse subcommands if used
if [[ $# -ge 1 ]] ; then
  key="$1"
  if [[ "$key" == "createChannel" ]]; then
      export MODE="createChannel"
      shift
  elif [[ "$MODE" == "cc" ]]; then
    if [ "$1" != "-h" ]; then
      export SUBCOMMAND=$key
      shift
    fi
  fi
fi


# parse flags
while [[ $# -ge 1 ]] ; do
  key="$1"
  case $key in
  -h )
    printHelp $MODE
    exit 0
    ;;
  -c )
    CHANNEL_NAME="$2"
    shift
    ;;
  -r )
    MAX_RETRY="$2"
    shift
    ;;
  -d )
    CLI_DELAY="$2"
    shift
    ;;
  -s )
    DATABASE="$2"
    shift
    ;;
  -ccl )
    CC_SRC_LANGUAGE="$2"
    shift
    ;;
  -ccn )
    CC_NAME="$2"
    shift
    ;;
  -ccv )
    CC_VERSION="$2"
    shift
    ;;
  -ccs )
    CC_SEQUENCE="$2"
    shift
    ;;
  -ccp )
    CC_SRC_PATH="$2"
    shift
    ;;
  -ccep )
    CC_END_POLICY="$2"
    shift
    ;;
  -cccg )
    CC_COLL_CONFIG="$2"
    shift
    ;;
  -cci )
    CC_INIT_FCN="$2"
    shift
    ;;
  -verbose )
    VERBOSE=true
    ;;
  -crypto )
    CRYPTO="$2"
    shift
    ;;
  -channels )
    CHANNEL_MODE="$2"
    if [ "$CHANNEL_MODE" != "single" ] && [ "$CHANNEL_MODE" != "multi" ]; then
      errorln "Invalid channel mode: $CHANNEL_MODE. Use 'single' or 'multi'."
      exit 1
    fi
    shift
    ;;
  -org )
    ORG="$2"
    shift
    ;;
  -i )
    IMAGETAG="$2"
    shift
    ;;
  -ccic )
    CC_INVOKE_CONSTRUCTOR="$2"
    shift
    ;;
  -ccqc )
    CC_QUERY_CONSTRUCTOR="$2"
    shift
    ;;
  * )
    errorln "Unknown flag: $key"
    printHelp
    exit 1
    ;;
  esac
  shift
done

# Are we generating crypto material with this command?
if [ ! -d "organizations/peerOrganizations" ]; then
  CRYPTO_MODE="with crypto from '${CRYPTO}'"
else
  CRYPTO_MODE=""
fi

# Determine mode of operation and printing out what we asked for
if [ "$MODE" == "up" ]; then
  CRYPTO="CA"
  DATABASE="couchdb"
  CHANNEL_MODE="multi"
  infoln "Starting nodes with CLI timeout of '${MAX_RETRY}' tries and CLI delay of '${CLI_DELAY}' seconds and using database '${DATABASE}' ${CRYPTO_MODE}"
  infoln "Channel mode: ${CHANNEL_MODE}"
  infoln "Auto bootstrap enabled for 'up': CA startup + 3 channels + per-channel chaincode deployment"
  networkUp
  createChannel
  deployCC
  startExplorer
elif [ "$MODE" == "createChannel" ]; then
  if [ "$CHANNEL_MODE" == "multi" ]; then
    infoln "Creating 3 channels: '${CHANNEL_CONSENT}', '${CHANNEL_AUDIT}', '${CHANNEL_EHR_HASH}'."
  else
    infoln "Creating channel '${CHANNEL_NAME}'."
  fi
  infoln "If network is not up, starting nodes with CLI timeout of '${MAX_RETRY}' tries and CLI delay of '${CLI_DELAY}' seconds and using database '${DATABASE} ${CRYPTO_MODE}"
  createChannel
elif [ "$MODE" == "down" ]; then
  infoln "Stopping network"
  networkDown
elif [ "$MODE" == "restart" ]; then
  infoln "Restarting network"
  networkDown
  networkUp
elif [ "$MODE" == "deployCC" ]; then
  if [ "$CHANNEL_MODE" == "multi" ]; then
    infoln "Deploying chaincode to 3 channels: '${CHANNEL_CONSENT}', '${CHANNEL_AUDIT}', '${CHANNEL_EHR_HASH}'"
  else
    infoln "deploying chaincode on channel '${CHANNEL_NAME}'"
  fi
  deployCC
elif [ "$MODE" == "cc" ] && [ "$SUBCOMMAND" == "package" ]; then
  packageChaincode
elif [ "$MODE" == "cc" ] && [ "$SUBCOMMAND" == "list" ]; then
  listChaincode
elif [ "$MODE" == "cc" ] && [ "$SUBCOMMAND" == "invoke" ]; then
  invokeChaincode
elif [ "$MODE" == "cc" ] && [ "$SUBCOMMAND" == "query" ]; then
  queryChaincode
else
  printHelp
  exit 1
fi