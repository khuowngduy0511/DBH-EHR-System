#!/bin/bash
# ============================================================================
# DBH-EHR System - Hyperledger Fabric Network Manager
# ============================================================================
# Usage:
#   ./network.sh up        - Start network (generate certs, create channel, deploy CC)
#   ./network.sh down      - Stop network and clean up
#   ./network.sh restart   - Restart network
#   ./network.sh channel   - Create channel & join peers only
#   ./network.sh deploy    - Deploy chaincodes only
#   ./network.sh status    - Check network status
# ============================================================================

set -euo pipefail

# ============================================================
# Configuration
# ============================================================
# 3 channels theo thiết kế: consent-channel, audit-channel, ehr-hash-channel
CHANNEL_EHR="ehr-hash-channel"
CHANNEL_CONSENT="consent-channel"
CHANNEL_AUDIT="audit-channel"
CHANNELS=("${CHANNEL_EHR}" "${CHANNEL_CONSENT}" "${CHANNEL_AUDIT}")
COMPOSE_FILE="../docker-compose.fabric.yml"
CRYPTO_CONFIG_DIR="./organizations"
CHANNEL_ARTIFACTS="./channel-artifacts"
CHAINCODE_DIR="./chaincode"
SCRIPTS_DIR="./scripts"

# Chaincode names matching BlockchainContracts.cs
CC_EHR_NAME="ehr-chaincode"
CC_CONSENT_NAME="consent-chaincode"
CC_AUDIT_NAME="audit-chaincode"
CC_EHR_VERSION="1.0"
CC_CONSENT_VERSION="1.0"
CC_AUDIT_VERSION="1.0"
CC_SEQUENCE=1

# Container names (matching docker-compose.fabric.yml)
ORDERER_CONTAINER="dbh_orderer"
PEER0_ORG1_CONTAINER="dbh_peer0_org1"
PEER0_ORG2_CONTAINER="dbh_peer0_org2"
CLI_CONTAINER="dbh_fabric_cli"

# Orderer
ORDERER_ADDRESS="orderer.dbh.com:7050"
ORDERER_CA="/opt/gopath/src/github.com/hyperledger/fabric/peer/organizations/ordererOrganizations/dbh.com/orderers/orderer.dbh.com/msp/tlscacerts/tlsca.dbh.com-cert.pem"
ORDERER_ADMIN_ADDRESS="orderer.dbh.com:7053"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# ============================================================
# Helper Functions
# ============================================================
log_info()  { echo -e "${BLUE}[INFO]${NC} $1"; }
log_ok()    { echo -e "${GREEN}[OK]${NC} $1"; }
log_warn()  { echo -e "${YELLOW}[WARN]${NC} $1"; }
log_error() { echo -e "${RED}[ERROR]${NC} $1"; }

check_prerequisites() {
    log_info "Checking prerequisites..."

    if ! command -v docker &> /dev/null; then
        log_error "Docker is not installed"
        exit 1
    fi

    if ! docker info &> /dev/null 2>&1; then
        log_error "Docker daemon is not running"
        exit 1
    fi

    log_ok "Prerequisites OK"
}

# ============================================================
# Step 1: Generate Crypto Material
# ============================================================
generate_crypto() {
    log_info "=========================================="
    log_info "Step 1: Generating crypto material..."
    log_info "=========================================="

    # Clean old crypto
    rm -rf "${CRYPTO_CONFIG_DIR}"
    mkdir -p "${CRYPTO_CONFIG_DIR}"

    # Generate orderer crypto
    log_info "Generating orderer organization crypto..."
    docker run --rm \
        -v "$(pwd):/work" \
        -w /work \
        hyperledger/fabric-tools:2.5 \
        cryptogen generate \
            --config=crypto-config-orderer.yaml \
            --output=organizations

    # Generate peer orgs crypto
    log_info "Generating peer organizations crypto..."
    docker run --rm \
        -v "$(pwd):/work" \
        -w /work \
        hyperledger/fabric-tools:2.5 \
        cryptogen generate \
            --config=crypto-config-peers.yaml \
            --output=organizations

    log_ok "Crypto material generated at ${CRYPTO_CONFIG_DIR}/"
}

# ============================================================
# Step 2: Generate Channel Artifacts
# ============================================================
generate_channel_artifacts() {
    log_info "=========================================="
    log_info "Step 2: Generating channel artifacts..."
    log_info "=========================================="

    rm -rf "${CHANNEL_ARTIFACTS}"
    mkdir -p "${CHANNEL_ARTIFACTS}"

    # Generate genesis block for orderer (Fabric 2.5 uses osnadmin, but we still need channel config)
    # Generate genesis block for each channel
    for ch in "${CHANNELS[@]}"; do
        log_info "Generating genesis block for channel '${ch}'..."
        docker run --rm \
            -v "$(pwd):/work" \
            -w /work \
            -e FABRIC_CFG_PATH=/work \
            hyperledger/fabric-tools:2.5 \
            configtxgen \
                -profile DBHOrdererGenesis \
                -channelID "${ch}" \
                -outputBlock "${CHANNEL_ARTIFACTS}/${ch}.block"
    done

    log_ok "Channel artifacts generated for ${#CHANNELS[@]} channels at ${CHANNEL_ARTIFACTS}/"
}

# ============================================================
# Step 3: Start Docker Containers
# ============================================================
start_containers() {
    log_info "=========================================="
    log_info "Step 3: Starting Fabric containers..."
    log_info "=========================================="

    cd "$(dirname "${COMPOSE_FILE}")"
    docker compose -f "$(basename "${COMPOSE_FILE}")" up -d
    cd - > /dev/null

    log_info "Waiting for containers to be ready..."
    sleep 5

    # Wait for orderer
    local retries=0
    while [ $retries -lt 30 ]; do
        if docker exec "${ORDERER_CONTAINER}" ls /var/hyperledger/production/orderer &> /dev/null 2>&1; then
            break
        fi
        retries=$((retries + 1))
        sleep 2
    done

    # Wait for peers
    retries=0
    while [ $retries -lt 30 ]; do
        if docker exec "${PEER0_ORG1_CONTAINER}" peer node status &> /dev/null 2>&1; then
            break
        fi
        retries=$((retries + 1))
        sleep 2
    done

    log_ok "All Fabric containers started"
}

# ============================================================
# Step 4: Create Channel & Join Peers
# ============================================================
create_channel() {
    log_info "=========================================="
    log_info "Step 4: Creating 3 channels & joining peers..."
    log_info "=========================================="

    # Copy crypto and channel artifacts into CLI container
    docker cp "${CRYPTO_CONFIG_DIR}" "${CLI_CONTAINER}:/opt/gopath/src/github.com/hyperledger/fabric/peer/organizations"
    docker cp "${CHANNEL_ARTIFACTS}" "${CLI_CONTAINER}:/opt/gopath/src/github.com/hyperledger/fabric/peer/channel-artifacts"

    for ch in "${CHANNELS[@]}"; do
        log_info "--- Creating channel '${ch}' ---"

        # Use osnadmin to join orderer to channel (Fabric 2.5+)
        log_info "  Joining orderer to '${ch}' via osnadmin..."
        docker exec "${CLI_CONTAINER}" osnadmin channel join \
            --channelID "${ch}" \
            --config-block "/opt/gopath/src/github.com/hyperledger/fabric/peer/channel-artifacts/${ch}.block" \
            -o "${ORDERER_ADMIN_ADDRESS}" \
            --ca-file "${ORDERER_CA}" \
            --client-cert "/opt/gopath/src/github.com/hyperledger/fabric/peer/organizations/ordererOrganizations/dbh.com/orderers/orderer.dbh.com/tls/server.crt" \
            --client-key "/opt/gopath/src/github.com/hyperledger/fabric/peer/organizations/ordererOrganizations/dbh.com/orderers/orderer.dbh.com/tls/server.key"

        sleep 2

        # Peer0 Org1 join channel
        log_info "  Joining peer0.org1 to '${ch}'..."
        docker exec \
            -e CORE_PEER_ADDRESS=peer0_org1:7051 \
            -e CORE_PEER_LOCALMSPID=Org1MSP \
            -e CORE_PEER_TLS_ENABLED=true \
            -e CORE_PEER_TLS_ROOTCERT_FILE=/opt/gopath/src/github.com/hyperledger/fabric/peer/organizations/peerOrganizations/org1.dbh.com/peers/peer0.org1.dbh.com/tls/ca.crt \
            -e CORE_PEER_MSPCONFIGPATH=/opt/gopath/src/github.com/hyperledger/fabric/peer/organizations/peerOrganizations/org1.dbh.com/users/Admin@org1.dbh.com/msp \
            "${CLI_CONTAINER}" peer channel join \
                -b "/opt/gopath/src/github.com/hyperledger/fabric/peer/channel-artifacts/${ch}.block"

        # Peer0 Org2 join channel
        log_info "  Joining peer0.org2 to '${ch}'..."
        docker exec \
            -e CORE_PEER_ADDRESS=peer0_org2:9051 \
            -e CORE_PEER_LOCALMSPID=Org2MSP \
            -e CORE_PEER_TLS_ENABLED=true \
            -e CORE_PEER_TLS_ROOTCERT_FILE=/opt/gopath/src/github.com/hyperledger/fabric/peer/organizations/peerOrganizations/org2.dbh.com/peers/peer0.org2.dbh.com/tls/ca.crt \
            -e CORE_PEER_MSPCONFIGPATH=/opt/gopath/src/github.com/hyperledger/fabric/peer/organizations/peerOrganizations/org2.dbh.com/users/Admin@org2.dbh.com/msp \
            "${CLI_CONTAINER}" peer channel join \
                -b "/opt/gopath/src/github.com/hyperledger/fabric/peer/channel-artifacts/${ch}.block"

        log_ok "  Channel '${ch}' created and peers joined"
    done

    log_ok "All 3 channels created: ${CHANNELS[*]}"
}

set_anchor_peers() {
    log_info "Setting anchor peers for all channels..."

    for ch in "${CHANNELS[@]}"; do
        log_info "  Setting anchor peers on channel '${ch}'..."

        # Org1 anchor peer
        docker exec \
            -e CORE_PEER_ADDRESS=peer0_org1:7051 \
            -e CORE_PEER_LOCALMSPID=Org1MSP \
            -e CORE_PEER_TLS_ENABLED=true \
            -e CORE_PEER_TLS_ROOTCERT_FILE=/opt/gopath/src/github.com/hyperledger/fabric/peer/organizations/peerOrganizations/org1.dbh.com/peers/peer0.org1.dbh.com/tls/ca.crt \
            -e CORE_PEER_MSPCONFIGPATH=/opt/gopath/src/github.com/hyperledger/fabric/peer/organizations/peerOrganizations/org1.dbh.com/users/Admin@org1.dbh.com/msp \
            "${CLI_CONTAINER}" peer channel update \
                -o "${ORDERER_ADDRESS}" \
                -c "${ch}" \
                -f "/opt/gopath/src/github.com/hyperledger/fabric/peer/channel-artifacts/Org1MSPanchors.tx" \
                --tls --cafile "${ORDERER_CA}" 2>/dev/null || log_warn "Anchor peer update for Org1 on ${ch} skipped"

        # Org2 anchor peer
        docker exec \
            -e CORE_PEER_ADDRESS=peer0_org2:9051 \
            -e CORE_PEER_LOCALMSPID=Org2MSP \
            -e CORE_PEER_TLS_ENABLED=true \
            -e CORE_PEER_TLS_ROOTCERT_FILE=/opt/gopath/src/github.com/hyperledger/fabric/peer/organizations/peerOrganizations/org2.dbh.com/peers/peer0.org2.dbh.com/tls/ca.crt \
            -e CORE_PEER_MSPCONFIGPATH=/opt/gopath/src/github.com/hyperledger/fabric/peer/organizations/peerOrganizations/org2.dbh.com/users/Admin@org2.dbh.com/msp \
            "${CLI_CONTAINER}" peer channel update \
                -o "${ORDERER_ADDRESS}" \
                -c "${ch}" \
                -f "/opt/gopath/src/github.com/hyperledger/fabric/peer/channel-artifacts/Org2MSPanchors.tx" \
                --tls --cafile "${ORDERER_CA}" 2>/dev/null || log_warn "Anchor peer update for Org2 on ${ch} skipped"
    done

    log_ok "Anchor peers configured for all channels"
}

# ============================================================
# Step 5: Deploy Chaincodes
# ============================================================
deploy_chaincodes() {
    log_info "=========================================="
    log_info "Step 5: Deploying chaincodes..."
    log_info "=========================================="

    # Each chaincode deployed to its dedicated channel
    deploy_single_chaincode "${CC_EHR_NAME}" "${CC_EHR_VERSION}" "ehr" "${CHANNEL_EHR}"
    deploy_single_chaincode "${CC_CONSENT_NAME}" "${CC_CONSENT_VERSION}" "consent" "${CHANNEL_CONSENT}"
    deploy_single_chaincode "${CC_AUDIT_NAME}" "${CC_AUDIT_VERSION}" "audit" "${CHANNEL_AUDIT}"

    log_ok "All chaincodes deployed successfully!"
}

deploy_single_chaincode() {
    local CC_NAME=$1
    local CC_VERSION=$2
    local CC_DIR=$3
    local CHANNEL_NAME=$4

    log_info "--- Deploying ${CC_NAME} v${CC_VERSION} to channel '${CHANNEL_NAME}' ---"

    # Package chaincode
    log_info "  Packaging ${CC_NAME}..."
    docker exec \
        -e CORE_PEER_ADDRESS=peer0_org1:7051 \
        -e CORE_PEER_LOCALMSPID=Org1MSP \
        -e CORE_PEER_TLS_ENABLED=true \
        -e CORE_PEER_TLS_ROOTCERT_FILE=/opt/gopath/src/github.com/hyperledger/fabric/peer/organizations/peerOrganizations/org1.dbh.com/peers/peer0.org1.dbh.com/tls/ca.crt \
        -e CORE_PEER_MSPCONFIGPATH=/opt/gopath/src/github.com/hyperledger/fabric/peer/organizations/peerOrganizations/org1.dbh.com/users/Admin@org1.dbh.com/msp \
        "${CLI_CONTAINER}" peer lifecycle chaincode package \
            "/opt/gopath/src/github.com/chaincode/${CC_NAME}.tar.gz" \
            --path "/opt/gopath/src/github.com/chaincode/${CC_DIR}" \
            --lang golang \
            --label "${CC_NAME}_${CC_VERSION}"

    # Install on Org1
    log_info "  Installing ${CC_NAME} on peer0.org1..."
    docker exec \
        -e CORE_PEER_ADDRESS=peer0_org1:7051 \
        -e CORE_PEER_LOCALMSPID=Org1MSP \
        -e CORE_PEER_TLS_ENABLED=true \
        -e CORE_PEER_TLS_ROOTCERT_FILE=/opt/gopath/src/github.com/hyperledger/fabric/peer/organizations/peerOrganizations/org1.dbh.com/peers/peer0.org1.dbh.com/tls/ca.crt \
        -e CORE_PEER_MSPCONFIGPATH=/opt/gopath/src/github.com/hyperledger/fabric/peer/organizations/peerOrganizations/org1.dbh.com/users/Admin@org1.dbh.com/msp \
        "${CLI_CONTAINER}" peer lifecycle chaincode install \
            "/opt/gopath/src/github.com/chaincode/${CC_NAME}.tar.gz"

    # Install on Org2
    log_info "  Installing ${CC_NAME} on peer0.org2..."
    docker exec \
        -e CORE_PEER_ADDRESS=peer0_org2:9051 \
        -e CORE_PEER_LOCALMSPID=Org2MSP \
        -e CORE_PEER_TLS_ENABLED=true \
        -e CORE_PEER_TLS_ROOTCERT_FILE=/opt/gopath/src/github.com/hyperledger/fabric/peer/organizations/peerOrganizations/org2.dbh.com/peers/peer0.org2.dbh.com/tls/ca.crt \
        -e CORE_PEER_MSPCONFIGPATH=/opt/gopath/src/github.com/hyperledger/fabric/peer/organizations/peerOrganizations/org2.dbh.com/users/Admin@org2.dbh.com/msp \
        "${CLI_CONTAINER}" peer lifecycle chaincode install \
            "/opt/gopath/src/github.com/chaincode/${CC_NAME}.tar.gz"

    # Get package ID
    log_info "  Querying package ID for ${CC_NAME}..."
    local PACKAGE_ID
    PACKAGE_ID=$(docker exec \
        -e CORE_PEER_ADDRESS=peer0_org1:7051 \
        -e CORE_PEER_LOCALMSPID=Org1MSP \
        -e CORE_PEER_TLS_ENABLED=true \
        -e CORE_PEER_TLS_ROOTCERT_FILE=/opt/gopath/src/github.com/hyperledger/fabric/peer/organizations/peerOrganizations/org1.dbh.com/peers/peer0.org1.dbh.com/tls/ca.crt \
        -e CORE_PEER_MSPCONFIGPATH=/opt/gopath/src/github.com/hyperledger/fabric/peer/organizations/peerOrganizations/org1.dbh.com/users/Admin@org1.dbh.com/msp \
        "${CLI_CONTAINER}" peer lifecycle chaincode queryinstalled 2>&1 \
        | grep "${CC_NAME}_${CC_VERSION}" | awk -F 'Package ID: ' '{print $2}' | awk -F ',' '{print $1}')

    if [ -z "${PACKAGE_ID}" ]; then
        log_error "Failed to get package ID for ${CC_NAME}"
        return 1
    fi
    log_info "  Package ID: ${PACKAGE_ID}"

    # Approve for Org1
    log_info "  Approving ${CC_NAME} for Org1..."
    docker exec \
        -e CORE_PEER_ADDRESS=peer0_org1:7051 \
        -e CORE_PEER_LOCALMSPID=Org1MSP \
        -e CORE_PEER_TLS_ENABLED=true \
        -e CORE_PEER_TLS_ROOTCERT_FILE=/opt/gopath/src/github.com/hyperledger/fabric/peer/organizations/peerOrganizations/org1.dbh.com/peers/peer0.org1.dbh.com/tls/ca.crt \
        -e CORE_PEER_MSPCONFIGPATH=/opt/gopath/src/github.com/hyperledger/fabric/peer/organizations/peerOrganizations/org1.dbh.com/users/Admin@org1.dbh.com/msp \
        "${CLI_CONTAINER}" peer lifecycle chaincode approveformyorg \
            -o "${ORDERER_ADDRESS}" \
            --channelID "${CHANNEL_NAME}" \
            --name "${CC_NAME}" \
            --version "${CC_VERSION}" \
            --package-id "${PACKAGE_ID}" \
            --sequence ${CC_SEQUENCE} \
            --tls --cafile "${ORDERER_CA}"

    # Approve for Org2
    log_info "  Approving ${CC_NAME} for Org2..."
    docker exec \
        -e CORE_PEER_ADDRESS=peer0_org2:9051 \
        -e CORE_PEER_LOCALMSPID=Org2MSP \
        -e CORE_PEER_TLS_ENABLED=true \
        -e CORE_PEER_TLS_ROOTCERT_FILE=/opt/gopath/src/github.com/hyperledger/fabric/peer/organizations/peerOrganizations/org2.dbh.com/peers/peer0.org2.dbh.com/tls/ca.crt \
        -e CORE_PEER_MSPCONFIGPATH=/opt/gopath/src/github.com/hyperledger/fabric/peer/organizations/peerOrganizations/org2.dbh.com/users/Admin@org2.dbh.com/msp \
        "${CLI_CONTAINER}" peer lifecycle chaincode approveformyorg \
            -o "${ORDERER_ADDRESS}" \
            --channelID "${CHANNEL_NAME}" \
            --name "${CC_NAME}" \
            --version "${CC_VERSION}" \
            --package-id "${PACKAGE_ID}" \
            --sequence ${CC_SEQUENCE} \
            --tls --cafile "${ORDERER_CA}"

    # Check commit readiness
    log_info "  Checking commit readiness for ${CC_NAME}..."
    docker exec \
        -e CORE_PEER_ADDRESS=peer0_org1:7051 \
        -e CORE_PEER_LOCALMSPID=Org1MSP \
        -e CORE_PEER_TLS_ENABLED=true \
        -e CORE_PEER_TLS_ROOTCERT_FILE=/opt/gopath/src/github.com/hyperledger/fabric/peer/organizations/peerOrganizations/org1.dbh.com/peers/peer0.org1.dbh.com/tls/ca.crt \
        -e CORE_PEER_MSPCONFIGPATH=/opt/gopath/src/github.com/hyperledger/fabric/peer/organizations/peerOrganizations/org1.dbh.com/users/Admin@org1.dbh.com/msp \
        "${CLI_CONTAINER}" peer lifecycle chaincode checkcommitreadiness \
            --channelID "${CHANNEL_NAME}" \
            --name "${CC_NAME}" \
            --version "${CC_VERSION}" \
            --sequence ${CC_SEQUENCE} \
            --tls --cafile "${ORDERER_CA}" \
            --output json

    # Commit chaincode
    log_info "  Committing ${CC_NAME} to channel..."
    docker exec \
        -e CORE_PEER_ADDRESS=peer0_org1:7051 \
        -e CORE_PEER_LOCALMSPID=Org1MSP \
        -e CORE_PEER_TLS_ENABLED=true \
        -e CORE_PEER_TLS_ROOTCERT_FILE=/opt/gopath/src/github.com/hyperledger/fabric/peer/organizations/peerOrganizations/org1.dbh.com/peers/peer0.org1.dbh.com/tls/ca.crt \
        -e CORE_PEER_MSPCONFIGPATH=/opt/gopath/src/github.com/hyperledger/fabric/peer/organizations/peerOrganizations/org1.dbh.com/users/Admin@org1.dbh.com/msp \
        "${CLI_CONTAINER}" peer lifecycle chaincode commit \
            -o "${ORDERER_ADDRESS}" \
            --channelID "${CHANNEL_NAME}" \
            --name "${CC_NAME}" \
            --version "${CC_VERSION}" \
            --sequence ${CC_SEQUENCE} \
            --tls --cafile "${ORDERER_CA}" \
            --peerAddresses peer0_org1:7051 \
            --tlsRootCertFiles /opt/gopath/src/github.com/hyperledger/fabric/peer/organizations/peerOrganizations/org1.dbh.com/peers/peer0.org1.dbh.com/tls/ca.crt \
            --peerAddresses peer0_org2:9051 \
            --tlsRootCertFiles /opt/gopath/src/github.com/hyperledger/fabric/peer/organizations/peerOrganizations/org2.dbh.com/peers/peer0.org2.dbh.com/tls/ca.crt

    log_ok "  Chaincode ${CC_NAME} v${CC_VERSION} deployed and committed!"
}

# ============================================================
# Network Lifecycle Commands
# ============================================================

network_up() {
    check_prerequisites

    log_info "============================================"
    log_info " DBH-EHR Fabric Network - Starting Up"
    log_info "============================================"

    generate_crypto
    generate_channel_artifacts
    start_containers
    create_channel
    deploy_chaincodes

    echo ""
    log_ok "============================================"
    log_ok " DBH-EHR Fabric Network is READY!"
    log_ok "============================================"
    echo ""
    log_info "Network configuration:"
    log_info "  Channels:   ${CHANNELS[*]}"
    log_info "  Orderer:    localhost:7050"
    log_info "  Peer Org1:  localhost:7051  (Org1MSP - Hospital A)"
    log_info "  Peer Org2:  localhost:9051  (Org2MSP - Hospital B)"
    log_info "  CouchDB 1:  http://localhost:5984/_utils"
    log_info "  CouchDB 2:  http://localhost:7984/_utils"
    echo ""
    log_info "Chaincodes deployed:"
    log_info "  - ${CC_EHR_NAME}     v${CC_EHR_VERSION}"
    log_info "  - ${CC_CONSENT_NAME} v${CC_CONSENT_VERSION}"
    log_info "  - ${CC_AUDIT_NAME}   v${CC_AUDIT_VERSION}"
    echo ""
    log_info "To connect .NET app, set in appsettings.json:"
    log_info '  "HyperledgerFabric": {'
    log_info '    "Enabled": true,'
    log_info '    "PeerEndpoint": "localhost:7051",'
    log_info '    "MspId": "Org1MSP",'
    log_info '    "CertificatePath": "blockchain/organizations/peerOrganizations/org1.dbh.com/users/Admin@org1.dbh.com/msp/signcerts/Admin@org1.dbh.com-cert.pem",'
    log_info '    "PrivateKeyPath": "blockchain/organizations/peerOrganizations/org1.dbh.com/users/Admin@org1.dbh.com/msp/keystore/<private_key>",'
    log_info '    "TlsCertificatePath": "blockchain/organizations/peerOrganizations/org1.dbh.com/peers/peer0.org1.dbh.com/tls/ca.crt"'
    log_info '  }'
}

network_down() {
    log_info "============================================"
    log_info " DBH-EHR Fabric Network - Shutting Down"
    log_info "============================================"

    cd "$(dirname "${COMPOSE_FILE}")"
    docker compose -f "$(basename "${COMPOSE_FILE}")" down --volumes --remove-orphans 2>/dev/null || true
    cd - > /dev/null

    # Clean up chaincode containers
    docker rm -f $(docker ps -aq --filter "name=dev-peer") 2>/dev/null || true
    docker rmi -f $(docker images -q --filter "reference=dev-peer*") 2>/dev/null || true

    # Clean generated artifacts
    rm -rf "${CRYPTO_CONFIG_DIR}"
    rm -rf "${CHANNEL_ARTIFACTS}"
    rm -f "${CHAINCODE_DIR}"/*.tar.gz

    log_ok "Network stopped and cleaned up"
}

network_status() {
    log_info "=========================================="
    log_info " DBH-EHR Fabric Network Status"
    log_info "=========================================="

    echo ""
    echo "--- Docker Containers ---"
    docker ps --filter "network=dbh_fabric_network" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}" 2>/dev/null || echo "No containers found"

    echo ""
    echo "--- Channel Info ---"
    docker exec \
        -e CORE_PEER_ADDRESS=peer0_org1:7051 \
        -e CORE_PEER_LOCALMSPID=Org1MSP \
        -e CORE_PEER_TLS_ENABLED=true \
        -e CORE_PEER_TLS_ROOTCERT_FILE=/opt/gopath/src/github.com/hyperledger/fabric/peer/organizations/peerOrganizations/org1.dbh.com/peers/peer0.org1.dbh.com/tls/ca.crt \
        -e CORE_PEER_MSPCONFIGPATH=/opt/gopath/src/github.com/hyperledger/fabric/peer/organizations/peerOrganizations/org1.dbh.com/users/Admin@org1.dbh.com/msp \
        "${CLI_CONTAINER}" peer channel list 2>/dev/null || echo "CLI container not running"

    echo ""
    echo "--- Installed Chaincodes on Org1 ---"
    docker exec \
        -e CORE_PEER_ADDRESS=peer0_org1:7051 \
        -e CORE_PEER_LOCALMSPID=Org1MSP \
        -e CORE_PEER_TLS_ENABLED=true \
        -e CORE_PEER_TLS_ROOTCERT_FILE=/opt/gopath/src/github.com/hyperledger/fabric/peer/organizations/peerOrganizations/org1.dbh.com/peers/peer0.org1.dbh.com/tls/ca.crt \
        -e CORE_PEER_MSPCONFIGPATH=/opt/gopath/src/github.com/hyperledger/fabric/peer/organizations/peerOrganizations/org1.dbh.com/users/Admin@org1.dbh.com/msp \
        "${CLI_CONTAINER}" peer lifecycle chaincode queryinstalled 2>/dev/null || echo "Cannot query"

    echo ""
    echo "--- Committed Chaincodes ---"
    local -A CC_CHANNEL_MAP=(
        ["${CC_EHR_NAME}"]="${CHANNEL_EHR}"
        ["${CC_CONSENT_NAME}"]="${CHANNEL_CONSENT}"
        ["${CC_AUDIT_NAME}"]="${CHANNEL_AUDIT}"
    )
    for cc in "${CC_EHR_NAME}" "${CC_CONSENT_NAME}" "${CC_AUDIT_NAME}"; do
        local ch="${CC_CHANNEL_MAP[${cc}]}"
        docker exec \
            -e CORE_PEER_ADDRESS=peer0_org1:7051 \
            -e CORE_PEER_LOCALMSPID=Org1MSP \
            -e CORE_PEER_TLS_ENABLED=true \
            -e CORE_PEER_TLS_ROOTCERT_FILE=/opt/gopath/src/github.com/hyperledger/fabric/peer/organizations/peerOrganizations/org1.dbh.com/peers/peer0.org1.dbh.com/tls/ca.crt \
            -e CORE_PEER_MSPCONFIGPATH=/opt/gopath/src/github.com/hyperledger/fabric/peer/organizations/peerOrganizations/org1.dbh.com/users/Admin@org1.dbh.com/msp \
            "${CLI_CONTAINER}" peer lifecycle chaincode querycommitted \
                --channelID "${ch}" \
                --name "${cc}" 2>/dev/null || echo "${cc}: not committed on ${ch}"
    done
}

# ============================================================
# Main
# ============================================================
case "${1:-help}" in
    up)
        network_up
        ;;
    down)
        network_down
        ;;
    restart)
        network_down
        sleep 2
        network_up
        ;;
    channel)
        create_channel
        ;;
    deploy)
        deploy_chaincodes
        ;;
    status)
        network_status
        ;;
    *)
        echo ""
        echo "Usage: $0 {up|down|restart|channel|deploy|status}"
        echo ""
        echo "Commands:"
        echo "  up       - Start full Fabric network (certs, channel, chaincodes)"
        echo "  down     - Stop network and clean all artifacts"
        echo "  restart  - Stop then start network"
        echo "  channel  - Create channel and join peers only"
        echo "  deploy   - Deploy chaincodes only"
        echo "  status   - Show network status"
        echo ""
        ;;
esac
