#!/bin/bash
# ============================================================================
# Generate Connection Profiles (CCP) for .NET SDK
# ============================================================================
# Reads crypto material and outputs connection-org1.json, connection-org2.json
# These JSON files are used by FabricGatewayClient to connect to the network
# ============================================================================

set -euo pipefail

CRYPTO_DIR="./organizations"

generate_ccp() {
    local ORG=$1
    local PEER_PORT=$2
    local CA_PORT=$3
    local ORG_DOMAIN="${ORG}.dbh.com"
    local ORG_UPPER="Org${ORG: -1}"
    local MSP_ID="${ORG_UPPER}MSP"

    local PEER_PEM
    local CA_PEM

    PEER_PEM_FILE="${CRYPTO_DIR}/peerOrganizations/${ORG_DOMAIN}/tlsca/tlsca.${ORG_DOMAIN}-cert.pem"
    CA_PEM_FILE="${CRYPTO_DIR}/peerOrganizations/${ORG_DOMAIN}/ca/ca.${ORG_DOMAIN}-cert.pem"

    if [ -f "${PEER_PEM_FILE}" ]; then
        PEER_PEM=$(sed 's/$/\\n/' "${PEER_PEM_FILE}" | tr -d '\r\n')
    else
        PEER_PEM=""
        echo "Warning: ${PEER_PEM_FILE} not found, generating template..."
    fi

    if [ -f "${CA_PEM_FILE}" ]; then
        CA_PEM=$(sed 's/$/\\n/' "${CA_PEM_FILE}" | tr -d '\r\n')
    else
        CA_PEM=""
    fi

    cat > "connection-${ORG}.json" <<EOF
{
    "name": "dbh-ehr-network-${ORG}",
    "version": "1.0.0",
    "client": {
        "organization": "${MSP_ID}",
        "connection": {
            "timeout": {
                "peer": {
                    "endorser": "300"
                }
            }
        }
    },
    "organizations": {
        "${MSP_ID}": {
            "mspid": "${MSP_ID}",
            "peers": [
                "peer0.${ORG_DOMAIN}"
            ],
            "certificateAuthorities": [
                "ca.${ORG_DOMAIN}"
            ]
        }
    },
    "peers": {
        "peer0.${ORG_DOMAIN}": {
            "url": "grpcs://localhost:${PEER_PORT}",
            "tlsCACerts": {
                "pem": "${PEER_PEM}"
            },
            "grpcOptions": {
                "ssl-target-name-override": "peer0.${ORG_DOMAIN}",
                "hostnameOverride": "peer0.${ORG_DOMAIN}"
            }
        }
    },
    "certificateAuthorities": {
        "ca.${ORG_DOMAIN}": {
            "url": "https://localhost:${CA_PORT}",
            "caName": "ca-${ORG}",
            "tlsCACerts": {
                "pem": ["${CA_PEM}"]
            },
            "httpOptions": {
                "verify": false
            }
        }
    }
}
EOF

    echo "Generated connection-${ORG}.json"
}

echo "Generating Connection Profiles..."
generate_ccp "org1" "7051" "7054"
generate_ccp "org2" "9051" "8054"
echo "Done! Connection profiles generated."
