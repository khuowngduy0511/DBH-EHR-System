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

# Generate the connection profile from scratch with the discovered keys
cat > "$PROFILE_PATH" <<CONN_EOF
{
    "name": "ehr-network",
    "version": "1.0.0",
    "client": {
        "tlsEnable": true,
        "adminCredential": {
            "id": "exploreradmin",
            "password": "exploreradminpw"
        },
        "enableAuthentication": true,
        "organization": "Hospital1",
        "connection": {
            "timeout": {
                "peer": { "endorser": "300" },
                "orderer": "300"
            }
        }
    },
    "channels": {
        "consent-channel": {
            "peers": {
                "peer0.hospital1.ehr.com": {},
                "peer0.hospital2.ehr.com": {},
                "peer0.clinic.ehr.com": {}
            }
        },
        "audit-channel": {
            "peers": {
                "peer0.hospital1.ehr.com": {},
                "peer0.hospital2.ehr.com": {},
                "peer0.clinic.ehr.com": {}
            }
        },
        "ehr-hash-channel": {
            "peers": {
                "peer0.hospital1.ehr.com": {},
                "peer0.hospital2.ehr.com": {},
                "peer0.clinic.ehr.com": {}
            }
        }
    },
    "organizations": {
        "Hospital1": {
            "mspid": "Hospital1MSP",
            "adminPrivateKey": {
                "path": "/tmp/crypto/peerOrganizations/hospital1.ehr.com/users/Admin@hospital1.ehr.com/msp/keystore/${HOSPITAL1_PRIVATE_KEY}"
            },
            "peers": ["peer0.hospital1.ehr.com"],
            "signedCert": {
                "path": "/tmp/crypto/peerOrganizations/hospital1.ehr.com/users/Admin@hospital1.ehr.com/msp/signcerts/cert.pem"
            }
        },
        "Hospital2": {
            "mspid": "Hospital2MSP",
            "adminPrivateKey": {
                "path": "/tmp/crypto/peerOrganizations/hospital2.ehr.com/users/Admin@hospital2.ehr.com/msp/keystore/${HOSPITAL2_PRIVATE_KEY}"
            },
            "peers": ["peer0.hospital2.ehr.com"],
            "signedCert": {
                "path": "/tmp/crypto/peerOrganizations/hospital2.ehr.com/users/Admin@hospital2.ehr.com/msp/signcerts/cert.pem"
            }
        },
        "Clinic": {
            "mspid": "ClinicMSP",
            "adminPrivateKey": {
                "path": "/tmp/crypto/peerOrganizations/clinic.ehr.com/users/Admin@clinic.ehr.com/msp/keystore/${CLINIC_PRIVATE_KEY}"
            },
            "peers": ["peer0.clinic.ehr.com"],
            "signedCert": {
                "path": "/tmp/crypto/peerOrganizations/clinic.ehr.com/users/Admin@clinic.ehr.com/msp/signcerts/cert.pem"
            }
        }
    },
    "peers": {
        "peer0.hospital1.ehr.com": {
            "url": "grpcs://peer0.hospital1.ehr.com:7051",
            "tlsCACerts": {
                "path": "/tmp/crypto/peerOrganizations/hospital1.ehr.com/peers/peer0.hospital1.ehr.com/tls/ca.crt"
            },
            "grpcOptions": {
                "ssl-target-name-override": "peer0.hospital1.ehr.com"
            }
        },
        "peer0.hospital2.ehr.com": {
            "url": "grpcs://peer0.hospital2.ehr.com:9051",
            "tlsCACerts": {
                "path": "/tmp/crypto/peerOrganizations/hospital2.ehr.com/peers/peer0.hospital2.ehr.com/tls/ca.crt"
            },
            "grpcOptions": {
                "ssl-target-name-override": "peer0.hospital2.ehr.com"
            }
        },
        "peer0.clinic.ehr.com": {
            "url": "grpcs://peer0.clinic.ehr.com:11051",
            "tlsCACerts": {
                "path": "/tmp/crypto/peerOrganizations/clinic.ehr.com/peers/peer0.clinic.ehr.com/tls/ca.crt"
            },
            "grpcOptions": {
                "ssl-target-name-override": "peer0.clinic.ehr.com"
            }
        }
    }
}
CONN_EOF

echo "Generated ${PROFILE_PATH} successfully."
echo "Explorer setup complete."
