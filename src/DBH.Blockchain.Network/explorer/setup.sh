#!/bin/bash
# Script to setup explorer

echo "Starting Explorer setup..."

# Directories
EXPLORER_DIR=$(dirname "$0")
cd $EXPLORER_DIR

# Using absolute paths to avoid confusion
EXPLORER_ABS_PATH=$(pwd)
# Assuming organizations is at ../organizations relative to this script
ORG_SOURCE_PATH=$(cd ../organizations && pwd)
ORG_DEST_PATH="${EXPLORER_ABS_PATH}/organizations"
PROFILE_PATH="${EXPLORER_ABS_PATH}/connection-profile/ehr-network.json"

mkdir -p "${EXPLORER_ABS_PATH}/connection-profile"

if [ ! -d "$ORG_SOURCE_PATH" ]; then
  echo "Error: Source organizations directory not found at ../organizations"
  echo "Measured path: $ORG_SOURCE_PATH"
  exit 1
fi

echo "Cleaning up existing organizations directory..."
if [ -d "${ORG_DEST_PATH}" ]; then
    rm -rf "${ORG_DEST_PATH}"
fi
mkdir -p "${ORG_DEST_PATH}"

echo "Copying crypto material from ${ORG_SOURCE_PATH}..."
# Copy peerOrganizations and ordererOrganizations
cp -r "${ORG_SOURCE_PATH}/peerOrganizations" "${ORG_DEST_PATH}/"
cp -r "${ORG_SOURCE_PATH}/ordererOrganizations" "${ORG_DEST_PATH}/"
cp -r "${ORG_SOURCE_PATH}/fabric-ca" "${ORG_DEST_PATH}/"

# Update connection profile with new private key
echo "Updating connection profile..."

HOSPITAL1_KEYSTORE_DIR="${ORG_DEST_PATH}/peerOrganizations/hospital1.ehr.com/users/Admin@hospital1.ehr.com/msp/keystore"
HOSPITAL2_KEYSTORE_DIR="${ORG_DEST_PATH}/peerOrganizations/hospital2.ehr.com/users/Admin@hospital2.ehr.com/msp/keystore"
CLINIC_KEYSTORE_DIR="${ORG_DEST_PATH}/peerOrganizations/clinic.ehr.com/users/Admin@clinic.ehr.com/msp/keystore"

if [ ! -d "$HOSPITAL1_KEYSTORE_DIR" ]; then
    echo "Error: Keystore directory not found at $HOSPITAL1_KEYSTORE_DIR"
    echo "Please ensure the network is up and crypto material is generated."
    exit 1
fi

HOSPITAL1_PRIVATE_KEY=$(ls "$HOSPITAL1_KEYSTORE_DIR" | grep "_sk" | head -n 1)
HOSPITAL2_PRIVATE_KEY=""
CLINIC_PRIVATE_KEY=""

if [ -d "$HOSPITAL2_KEYSTORE_DIR" ]; then
    HOSPITAL2_PRIVATE_KEY=$(ls "$HOSPITAL2_KEYSTORE_DIR" | grep "_sk" | head -n 1)
fi

if [ -d "$CLINIC_KEYSTORE_DIR" ]; then
    CLINIC_PRIVATE_KEY=$(ls "$CLINIC_KEYSTORE_DIR" | grep "_sk" | head -n 1)
fi

if [ -z "$HOSPITAL1_PRIVATE_KEY" ]; then
    echo "Error: Missing Hospital1 admin private key in keystore directory"
    exit 1
fi

echo "Found Hospital1 admin key: $HOSPITAL1_PRIVATE_KEY"
if [ -n "$HOSPITAL2_PRIVATE_KEY" ]; then
    echo "Found Hospital2 admin key: $HOSPITAL2_PRIVATE_KEY"
fi

if [ -n "$CLINIC_PRIVATE_KEY" ]; then
    echo "Found Clinic admin key: $CLINIC_PRIVATE_KEY"
fi

# Update the profile using sed
# This assumes the path format in the file matches /tmp/crypto/.../keystore/.*_sk
# We capture the path up to keystore/ and replace the filename
TEMP_FILE="${PROFILE_PATH}.tmp"

if [ ! -f "$PROFILE_PATH" ]; then
    echo "Error: Connection profile not found at $PROFILE_PATH"
    exit 1
fi

# Replace placeholder keys for each org with the current generated *_sk file names
SED_ARGS=(
    -E
    -e "s|PLACEHOLDER_HOSPITAL1_SK|${HOSPITAL1_PRIVATE_KEY}|g"
    -e "s|(/tmp/crypto/peerOrganizations/hospital1\.ehr\.com/users/Admin@hospital1\.ehr\.com/msp/keystore/)[^\"]+_sk|\\1${HOSPITAL1_PRIVATE_KEY}|g"
)

if [ -n "$HOSPITAL2_PRIVATE_KEY" ]; then
    SED_ARGS+=(
        -e "s|PLACEHOLDER_HOSPITAL2_SK|${HOSPITAL2_PRIVATE_KEY}|g"
        -e "s|(/tmp/crypto/peerOrganizations/hospital2\.ehr\.com/users/Admin@hospital2\.ehr\.com/msp/keystore/)[^\"]+_sk|\\1${HOSPITAL2_PRIVATE_KEY}|g"
    )
fi

if [ -n "$CLINIC_PRIVATE_KEY" ]; then
    SED_ARGS+=(
        -e "s|PLACEHOLDER_CLINIC_SK|${CLINIC_PRIVATE_KEY}|g"
        -e "s|(/tmp/crypto/peerOrganizations/clinic\.ehr\.com/users/Admin@clinic\.ehr\.com/msp/keystore/)[^\"]+_sk|\\1${CLINIC_PRIVATE_KEY}|g"
    )
fi

sed "${SED_ARGS[@]}" "$PROFILE_PATH" > "$TEMP_FILE"

if [ $? -eq 0 ]; then
    mv "$TEMP_FILE" "$PROFILE_PATH"
    echo "Updated ${PROFILE_PATH} successfully."
else
    echo "Error updating ${PROFILE_PATH}"
    if [ -f "$TEMP_FILE" ]; then rm "$TEMP_FILE"; fi
    exit 1
fi

echo "Explorer setup complete."
