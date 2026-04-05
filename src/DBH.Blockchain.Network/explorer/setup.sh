#!/usr/bin/env bash
set -euo pipefail

echo "Starting Explorer setup..."

EXPLORER_DIR="$(cd "$(dirname "$0")" && pwd)"
cd "$EXPLORER_DIR"

if [ -f ".env" ]; then
    set -a
    . ./.env
    set +a
fi

PROFILE_PATH="${EXPLORER_DIR}/connection-profile/ehr-network.json"
VOLUME_NAME="${FABRIC_CRYPTO_VOLUME:-fabric-crypto}"
KEYSTORE_DIR="/crypto/peerOrganizations/hospital1.ehr.com/users/User1@hospital1.ehr.com/msp/keystore"

if ! docker volume inspect "$VOLUME_NAME" >/dev/null 2>&1; then
    echo "Error: Docker volume '$VOLUME_NAME' not found."
    echo "Ensure network enrollment sync has populated the crypto volume."
    exit 1
fi

echo "Reading private key from docker volume '${VOLUME_NAME}'..."
PRIVATE_KEY=$(docker run --rm -v "${VOLUME_NAME}:/crypto:ro" busybox sh -c "ls ${KEYSTORE_DIR} 2>/dev/null | grep '_sk' | head -n 1")

if [ -z "$PRIVATE_KEY" ]; then
    echo "Error: No private key found in ${KEYSTORE_DIR} inside volume '${VOLUME_NAME}'."
    echo "Ensure peerOrganizations/ordererOrganizations exist in the crypto volume."
    exit 1
fi

echo "Found private key: ${PRIVATE_KEY}"

if [ ! -f "$PROFILE_PATH" ]; then
    echo "Error: Connection profile not found at ${PROFILE_PATH}"
    exit 1
fi

TEMP_FILE="${PROFILE_PATH}.tmp"
sed -E "s|keystore/[^/]+_sk|keystore/${PRIVATE_KEY}|g" "$PROFILE_PATH" > "$TEMP_FILE"
mv "$TEMP_FILE" "$PROFILE_PATH"

echo "Updated ${PROFILE_PATH} successfully."
echo "Explorer setup complete."
