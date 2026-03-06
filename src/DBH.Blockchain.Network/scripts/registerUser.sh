#!/usr/bin/env bash
#
# Register and enroll a new user (doctor, patient, etc.) with a Fabric CA
#
# Usage:
#   ./scripts/registerUser.sh -org hospital1 -type doctor -id doctor001
#   ./scripts/registerUser.sh -org clinic -type patient -id patient001
#   ./scripts/registerUser.sh -org hospital2 -type doctor -id doctor002 -secret mysecret
#
# This can be called from your C# backend via process execution,
# or you can use the Fabric CA REST API directly.
#

ROOTDIR=$(cd "$(dirname "$0")"/.. && pwd)
. ${ROOTDIR}/scripts/utils.sh

# Parse arguments
ORG=""
USER_TYPE=""
USER_ID=""
USER_SECRET=""

while [[ $# -ge 1 ]] ; do
  key="$1"
  case $key in
  -org )
    ORG="$2"
    shift
    ;;
  -type )
    USER_TYPE="$2"
    shift
    ;;
  -id )
    USER_ID="$2"
    shift
    ;;
  -secret )
    USER_SECRET="$2"
    shift
    ;;
  -h )
    echo "Usage: registerUser.sh -org <hospital1|hospital2|clinic> -type <doctor|patient|admin|client> -id <userId> [-secret <password>]"
    exit 0
    ;;
  * )
    errorln "Unknown flag: $key"
    exit 1
    ;;
  esac
  shift
done

# Validate required arguments
if [ -z "$ORG" ] || [ -z "$USER_TYPE" ] || [ -z "$USER_ID" ]; then
  fatalln "Missing required arguments. Usage: registerUser.sh -org <org> -type <type> -id <userId>"
fi

# Default secret if not provided
if [ -z "$USER_SECRET" ]; then
  USER_SECRET="${USER_ID}pw"
fi

# Map org to CA details
case $ORG in
  hospital1 )
    CA_NAME="ca-hospital1"
    CA_PORT=7054
    CA_DIR="${ROOTDIR}/organizations/fabric-ca/hospital1"
    ORG_DOMAIN="hospital1.ehr.com"
    ORG_DIR="${ROOTDIR}/organizations/peerOrganizations/hospital1.ehr.com"
    ;;
  hospital2 )
    CA_NAME="ca-hospital2"
    CA_PORT=8054
    CA_DIR="${ROOTDIR}/organizations/fabric-ca/hospital2"
    ORG_DOMAIN="hospital2.ehr.com"
    ORG_DIR="${ROOTDIR}/organizations/peerOrganizations/hospital2.ehr.com"
    ;;
  clinic )
    CA_NAME="ca-clinic"
    CA_PORT=10054
    CA_DIR="${ROOTDIR}/organizations/fabric-ca/clinic"
    ORG_DOMAIN="clinic.ehr.com"
    ORG_DIR="${ROOTDIR}/organizations/peerOrganizations/clinic.ehr.com"
    ;;
  * )
    fatalln "Unknown organization: $ORG. Must be hospital1, hospital2, or clinic"
    ;;
esac

# Map user type to Fabric CA type
# Doctors and patients are registered as 'client' type with custom attributes
case $USER_TYPE in
  doctor )
    FABRIC_TYPE="client"
    ATTRS="hf.Type=doctor:ecert"
    ;;
  patient )
    FABRIC_TYPE="client"
    ATTRS="hf.Type=patient:ecert"
    ;;
  admin )
    FABRIC_TYPE="admin"
    ATTRS=""
    ;;
  client )
    FABRIC_TYPE="client"
    ATTRS=""
    ;;
  * )
    fatalln "Unknown user type: $USER_TYPE. Must be doctor, patient, admin, or client"
    ;;
esac

# Check CA cert exists
if [ ! -f "${CA_DIR}/ca-cert.pem" ]; then
  fatalln "CA certificate not found at ${CA_DIR}/ca-cert.pem. Is the CA running?"
fi

# Set FABRIC_CA_CLIENT_HOME to the org directory (uses the admin certs for registration)
export FABRIC_CA_CLIENT_HOME=${ORG_DIR}

infoln "Registering ${USER_TYPE} '${USER_ID}' with ${CA_NAME}"

# Register the user
REGISTER_CMD="fabric-ca-client register --caname ${CA_NAME} --id.name ${USER_ID} --id.secret ${USER_SECRET} --id.type ${FABRIC_TYPE}"
if [ -n "$ATTRS" ]; then
  REGISTER_CMD="${REGISTER_CMD} --id.attrs '${ATTRS}'"
fi
REGISTER_CMD="${REGISTER_CMD} --tls.certfiles ${CA_DIR}/ca-cert.pem"

set -x
eval $REGISTER_CMD
res=$?
{ set +x; } 2>/dev/null

if [ $res -ne 0 ]; then
  fatalln "Failed to register ${USER_TYPE} '${USER_ID}'"
fi

# Enroll the user to generate their MSP
USER_MSP_DIR="${ORG_DIR}/users/${USER_ID}@${ORG_DOMAIN}/msp"
infoln "Enrolling ${USER_TYPE} '${USER_ID}' to generate MSP"

set -x
fabric-ca-client enroll -u https://${USER_ID}:${USER_SECRET}@localhost:${CA_PORT} --caname ${CA_NAME} -M "${USER_MSP_DIR}" --tls.certfiles "${CA_DIR}/ca-cert.pem"
res=$?
{ set +x; } 2>/dev/null

if [ $res -ne 0 ]; then
  fatalln "Failed to enroll ${USER_TYPE} '${USER_ID}'"
fi

# Copy the NodeOUs config
cp "${ORG_DIR}/msp/config.yaml" "${USER_MSP_DIR}/config.yaml"

successln "Successfully registered and enrolled ${USER_TYPE} '${USER_ID}' in ${ORG}"
infoln "MSP directory: ${USER_MSP_DIR}"
infoln ""
infoln "To use this identity, set:"
infoln "  export CORE_PEER_MSPCONFIGPATH=${USER_MSP_DIR}"
