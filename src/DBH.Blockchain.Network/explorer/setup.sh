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

# Update connection profile with new private key
echo "Updating connection profile..."

# Find the private key file
# Based on usage of Hospital1MSP -> hospital1.ehr.com -> User1
KEYSTORE_DIR="${ORG_DEST_PATH}/peerOrganizations/hospital1.ehr.com/users/User1@hospital1.ehr.com/msp/keystore"

if [ ! -d "$KEYSTORE_DIR" ]; then
    echo "Error: Keystore directory not found at $KEYSTORE_DIR"
    echo "Please ensure the network is up and crypto material is generated."
    exit 1
fi

PRIVATE_KEY=$(ls "$KEYSTORE_DIR" | grep "_sk" | head -n 1)

if [ -z "$PRIVATE_KEY" ]; then
    echo "Error: No private key found in $KEYSTORE_DIR"
    exit 1
fi

echo "Found private key: $PRIVATE_KEY"

# Update the profile using sed
# This assumes the path format in the file matches /tmp/crypto/.../keystore/.*_sk
# We capture the path up to keystore/ and replace the filename
TEMP_FILE="${PROFILE_PATH}.tmp"

if [ ! -f "$PROFILE_PATH" ]; then
    echo "Error: Connection profile not found at $PROFILE_PATH"
    exit 1
fi

# sed command to replace the key filename
# We look for the pattern "keystore/SOME_KEY_sk" and replace it with "keystore/$PRIVATE_KEY"
sed -E "s|keystore/[^/]+_sk|keystore/${PRIVATE_KEY}|g" "$PROFILE_PATH" > "$TEMP_FILE"

if [ $? -eq 0 ]; then
    mv "$TEMP_FILE" "$PROFILE_PATH"
    echo "Updated ${PROFILE_PATH} successfully."
else
    echo "Error updating ${PROFILE_PATH}"
    if [ -f "$TEMP_FILE" ]; then rm "$TEMP_FILE"; fi
    exit 1
fi

echo "Explorer setup complete."
