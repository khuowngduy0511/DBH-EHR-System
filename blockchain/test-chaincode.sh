#!/bin/bash
# ============================================================================
# DBH-EHR System - Test Chaincodes
# ============================================================================
# Invoke and query each chaincode to verify deployment
# Usage: ./test-chaincode.sh
# ============================================================================

set -euo pipefail

CHANNEL_EHR="ehr-hash-channel"
CHANNEL_CONSENT="consent-channel"
CHANNEL_AUDIT="audit-channel"
CLI_CONTAINER="dbh_fabric_cli"
ORDERER_ADDRESS="orderer.dbh.com:7050"
ORDERER_CA="/opt/gopath/src/github.com/hyperledger/fabric/peer/organizations/ordererOrganizations/dbh.com/orderers/orderer.dbh.com/msp/tlscacerts/tlsca.dbh.com-cert.pem"

RED='\033[0;31m'
GREEN='\033[0;32m'
BLUE='\033[0;34m'
NC='\033[0m'
log_info()  { echo -e "${BLUE}[TEST]${NC} $1"; }
log_ok()    { echo -e "${GREEN}[PASS]${NC} $1"; }
log_fail()  { echo -e "${RED}[FAIL]${NC} $1"; }

# Helper to run peer chaincode invoke
invoke_cc() {
    local CH_NAME=$1
    local CC_NAME=$2
    local FUNC=$3
    shift 3
    local ARGS_JSON="$*"

    docker exec \
        -e CORE_PEER_ADDRESS=peer0_org1:7051 \
        -e CORE_PEER_LOCALMSPID=Org1MSP \
        -e CORE_PEER_TLS_ENABLED=true \
        -e CORE_PEER_TLS_ROOTCERT_FILE=/opt/gopath/src/github.com/hyperledger/fabric/peer/organizations/peerOrganizations/org1.dbh.com/peers/peer0.org1.dbh.com/tls/ca.crt \
        -e CORE_PEER_MSPCONFIGPATH=/opt/gopath/src/github.com/hyperledger/fabric/peer/organizations/peerOrganizations/org1.dbh.com/users/Admin@org1.dbh.com/msp \
        "${CLI_CONTAINER}" peer chaincode invoke \
            -o "${ORDERER_ADDRESS}" \
            -C "${CH_NAME}" \
            -n "${CC_NAME}" \
            -c "{\"function\":\"${FUNC}\",\"Args\":[${ARGS_JSON}]}" \
            --tls --cafile "${ORDERER_CA}" \
            --peerAddresses peer0_org1:7051 \
            --tlsRootCertFiles /opt/gopath/src/github.com/hyperledger/fabric/peer/organizations/peerOrganizations/org1.dbh.com/peers/peer0.org1.dbh.com/tls/ca.crt \
            --peerAddresses peer0_org2:9051 \
            --tlsRootCertFiles /opt/gopath/src/github.com/hyperledger/fabric/peer/organizations/peerOrganizations/org2.dbh.com/peers/peer0.org2.dbh.com/tls/ca.crt \
            --waitForEvent
}

# Helper to run peer chaincode query
query_cc() {
    local CH_NAME=$1
    local CC_NAME=$2
    local FUNC=$3
    shift 3
    local ARGS_JSON="$*"

    docker exec \
        -e CORE_PEER_ADDRESS=peer0_org1:7051 \
        -e CORE_PEER_LOCALMSPID=Org1MSP \
        -e CORE_PEER_TLS_ENABLED=true \
        -e CORE_PEER_TLS_ROOTCERT_FILE=/opt/gopath/src/github.com/hyperledger/fabric/peer/organizations/peerOrganizations/org1.dbh.com/peers/peer0.org1.dbh.com/tls/ca.crt \
        -e CORE_PEER_MSPCONFIGPATH=/opt/gopath/src/github.com/hyperledger/fabric/peer/organizations/peerOrganizations/org1.dbh.com/users/Admin@org1.dbh.com/msp \
        "${CLI_CONTAINER}" peer chaincode query \
            -C "${CH_NAME}" \
            -n "${CC_NAME}" \
            -c "{\"function\":\"${FUNC}\",\"Args\":[${ARGS_JSON}]}"
}

echo ""
echo "========================================"
echo " DBH-EHR Chaincode Integration Tests"
echo "========================================"
echo ""

# ---------------------------------------------------
# Test 1: EHR Chaincode - Create & Verify Hash
# ---------------------------------------------------
log_info "=== Test 1: EHR Chaincode ==="

EHR_ID="ehr-test-001"
RECORD_JSON='{"ehrId":"ehr-test-001","patientDid":"did:dbh:patient:123","createdByDid":"did:dbh:doctor:456","organizationId":"org1","version":1,"contentHash":"sha256:abc123def456","fileHash":"sha256:file789","timestamp":"2026-03-05T10:00:00Z"}'

log_info "Creating EHR hash..."
invoke_cc "${CHANNEL_EHR}" "ehr-chaincode" "CreateEhrHash" "\"${EHR_ID}\"" "\"1\"" "\"${RECORD_JSON}\"" \
    && log_ok "CreateEhrHash" || log_fail "CreateEhrHash"

sleep 2

log_info "Querying EHR hash..."
query_cc "${CHANNEL_EHR}" "ehr-chaincode" "GetEhrHash" "\"${EHR_ID}\"" "\"1\"" \
    && log_ok "GetEhrHash" || log_fail "GetEhrHash"

log_info "Verifying EHR integrity..."
query_cc "${CHANNEL_EHR}" "ehr-chaincode" "VerifyEhrIntegrity" "\"${EHR_ID}\"" "\"1\"" "\"sha256:abc123def456\"" \
    && log_ok "VerifyEhrIntegrity" || log_fail "VerifyEhrIntegrity"

# ---------------------------------------------------
# Test 2: Consent Chaincode - Grant & Verify
# ---------------------------------------------------
log_info ""
log_info "=== Test 2: Consent Chaincode ==="

CONSENT_ID="consent-test-001"
CONSENT_JSON='{"consentId":"consent-test-001","patientDid":"did:dbh:patient:123","granteeDid":"did:dbh:doctor:456","granteeType":"DOCTOR","permission":"READ","purpose":"TREATMENT","grantedAt":"2026-03-05T10:00:00Z","expiresAt":"2027-03-05T10:00:00Z","status":"ACTIVE"}'

log_info "Granting consent..."
invoke_cc "${CHANNEL_CONSENT}" "consent-chaincode" "GrantConsent" "\"${CONSENT_ID}\"" "\"did:dbh:patient:123\"" "\"did:dbh:doctor:456\"" "\"${CONSENT_JSON}\"" \
    && log_ok "GrantConsent" || log_fail "GrantConsent"

sleep 2

log_info "Querying consent..."
query_cc "${CHANNEL_CONSENT}" "consent-chaincode" "GetConsent" "\"${CONSENT_ID}\"" \
    && log_ok "GetConsent" || log_fail "GetConsent"

log_info "Verifying consent..."
query_cc "${CHANNEL_CONSENT}" "consent-chaincode" "VerifyConsent" "\"${CONSENT_ID}\"" "\"did:dbh:doctor:456\"" \
    && log_ok "VerifyConsent" || log_fail "VerifyConsent"

# ---------------------------------------------------
# Test 3: Audit Chaincode - Create & Query
# ---------------------------------------------------
log_info ""
log_info "=== Test 3: Audit Chaincode ==="

AUDIT_ID="audit-test-001"
AUDIT_JSON='{"auditId":"audit-test-001","actorDid":"did:dbh:doctor:456","actorType":"DOCTOR","action":"VIEW","targetType":"EHR","targetId":"ehr-test-001","patientDid":"did:dbh:patient:123","result":"SUCCESS","timestamp":"2026-03-05T10:01:00Z"}'

log_info "Creating audit entry..."
invoke_cc "${CHANNEL_AUDIT}" "audit-chaincode" "CreateAuditEntry" "\"${AUDIT_ID}\"" "\"${AUDIT_JSON}\"" \
    && log_ok "CreateAuditEntry" || log_fail "CreateAuditEntry"

sleep 2

log_info "Querying audit entry..."
query_cc "${CHANNEL_AUDIT}" "audit-chaincode" "GetAuditEntry" "\"${AUDIT_ID}\"" \
    && log_ok "GetAuditEntry" || log_fail "GetAuditEntry"

log_info "Querying audits by patient..."
query_cc "${CHANNEL_AUDIT}" "audit-chaincode" "GetAuditsByPatient" "\"did:dbh:patient:123\"" \
    && log_ok "GetAuditsByPatient" || log_fail "GetAuditsByPatient"

echo ""
echo "========================================"
echo " Tests Complete!"
echo "========================================"
