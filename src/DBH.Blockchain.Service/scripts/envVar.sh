#!/usr/bin/env bash
#
# Copyright IBM Corp All Rights Reserved
#
# SPDX-License-Identifier: Apache-2.0
#

# This is a collection of bash functions used by different scripts

# imports
EHR_NETWORK_HOME=${EHR_NETWORK_HOME:-${PWD}}
. ${EHR_NETWORK_HOME}/scripts/utils.sh

export CORE_PEER_TLS_ENABLED=true
export ORDERER_CA=${EHR_NETWORK_HOME}/organizations/ordererOrganizations/ehr.com/tlsca/tlsca.ehr.com-cert.pem
export PEER0_HOSPITAL1_CA=${EHR_NETWORK_HOME}/organizations/peerOrganizations/hospital1.ehr.com/tlsca/tlsca.hospital1.ehr.com-cert.pem
export PEER0_HOSPITAL2_CA=${EHR_NETWORK_HOME}/organizations/peerOrganizations/hospital2.ehr.com/tlsca/tlsca.hospital2.ehr.com-cert.pem
export PEER0_CLINIC_CA=${EHR_NETWORK_HOME}/organizations/peerOrganizations/clinic.ehr.com/tlsca/tlsca.clinic.ehr.com-cert.pem

# Set environment variables for the peer org
setGlobals() {
  local USING_ORG=""
  if [ -z "$OVERRIDE_ORG" ]; then
    USING_ORG=$1
  else
    USING_ORG="${OVERRIDE_ORG}"
  fi
  infoln "Using organization ${USING_ORG}"
  if [ $USING_ORG -eq 1 ]; then
    export CORE_PEER_LOCALMSPID=Hospital1MSP
    export CORE_PEER_TLS_ROOTCERT_FILE=$PEER0_HOSPITAL1_CA
    export CORE_PEER_MSPCONFIGPATH=${EHR_NETWORK_HOME}/organizations/peerOrganizations/hospital1.ehr.com/users/Admin@hospital1.ehr.com/msp
    export CORE_PEER_ADDRESS=localhost:7051
  elif [ $USING_ORG -eq 2 ]; then
    export CORE_PEER_LOCALMSPID=Hospital2MSP
    export CORE_PEER_TLS_ROOTCERT_FILE=$PEER0_HOSPITAL2_CA
    export CORE_PEER_MSPCONFIGPATH=${EHR_NETWORK_HOME}/organizations/peerOrganizations/hospital2.ehr.com/users/Admin@hospital2.ehr.com/msp
    export CORE_PEER_ADDRESS=localhost:9051
  elif [ $USING_ORG -eq 3 ]; then
    export CORE_PEER_LOCALMSPID=ClinicMSP
    export CORE_PEER_TLS_ROOTCERT_FILE=$PEER0_CLINIC_CA
    export CORE_PEER_MSPCONFIGPATH=${EHR_NETWORK_HOME}/organizations/peerOrganizations/clinic.ehr.com/users/Admin@clinic.ehr.com/msp
    export CORE_PEER_ADDRESS=localhost:11051
  else
    errorln "ORG Unknown"
  fi

  if [ "$VERBOSE" = "true" ]; then
    env | grep CORE
  fi
}

# parsePeerConnectionParameters $@
# Helper function that sets the peer connection parameters for a chaincode
# operation
parsePeerConnectionParameters() {
  PEER_CONN_PARMS=()
  PEERS=""
  while [ "$#" -gt 0 ]; do
    setGlobals $1
    if [ $1 -eq 1 ]; then
      PEER="peer0.hospital1"
    elif [ $1 -eq 2 ]; then
      PEER="peer0.hospital2"
    elif [ $1 -eq 3 ]; then
      PEER="peer0.clinic"
    fi
    ## Set peer addresses
    if [ -z "$PEERS" ]
    then
	PEERS="$PEER"
    else
	PEERS="$PEERS $PEER"
    fi
    PEER_CONN_PARMS=("${PEER_CONN_PARMS[@]}" --peerAddresses $CORE_PEER_ADDRESS)
    ## Set path to TLS certificate
    if [ $1 -eq 1 ]; then
      TLSINFO=(--tlsRootCertFiles "${PEER0_HOSPITAL1_CA}")
    elif [ $1 -eq 2 ]; then
      TLSINFO=(--tlsRootCertFiles "${PEER0_HOSPITAL2_CA}")
    elif [ $1 -eq 3 ]; then
      TLSINFO=(--tlsRootCertFiles "${PEER0_CLINIC_CA}")
    fi
    PEER_CONN_PARMS=("${PEER_CONN_PARMS[@]}" "${TLSINFO[@]}")
    # shift by one to get to the next organization
    shift
  done
}

verifyResult() {
  if [ $1 -ne 0 ]; then
    fatalln "$2"
  fi
}
