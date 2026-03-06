#!/bin/bash
set -e

export ORDERER_CA=/opt/gopath/src/github.com/hyperledger/fabric/peer/organizations/ordererOrganizations/dbh.com/orderers/orderer.dbh.com/msp/tlscacerts/tlsca.dbh.com-cert.pem
export ORG1_TLS=/opt/gopath/src/github.com/hyperledger/fabric/peer/organizations/peerOrganizations/org1.dbh.com/peers/peer0.org1.dbh.com/tls/ca.crt
export ORG2_TLS=/opt/gopath/src/github.com/hyperledger/fabric/peer/organizations/peerOrganizations/org2.dbh.com/peers/peer0.org2.dbh.com/tls/ca.crt
export ORG1_MSP=/opt/gopath/src/github.com/hyperledger/fabric/peer/organizations/peerOrganizations/org1.dbh.com/users/Admin@org1.dbh.com/msp

invoke_cc() {
  local channel=$1 chaincode=$2 json=$3
  CORE_PEER_ADDRESS=peer0.org1.dbh.com:7051 \
  CORE_PEER_LOCALMSPID=Org1MSP \
  CORE_PEER_TLS_ENABLED=true \
  CORE_PEER_TLS_ROOTCERT_FILE=$ORG1_TLS \
  CORE_PEER_MSPCONFIGPATH=$ORG1_MSP \
  peer chaincode invoke \
    -o orderer.dbh.com:7050 --tls --cafile $ORDERER_CA \
    -C "$channel" -n "$chaincode" \
    --peerAddresses peer0.org1.dbh.com:7051 --tlsRootCertFiles $ORG1_TLS \
    --peerAddresses peer0.org2.dbh.com:9051 --tlsRootCertFiles $ORG2_TLS \
    -c "$json" 2>&1
}

query_cc() {
  local channel=$1 chaincode=$2 json=$3
  CORE_PEER_ADDRESS=peer0.org1.dbh.com:7051 \
  CORE_PEER_LOCALMSPID=Org1MSP \
  CORE_PEER_TLS_ENABLED=true \
  CORE_PEER_TLS_ROOTCERT_FILE=$ORG1_TLS \
  CORE_PEER_MSPCONFIGPATH=$ORG1_MSP \
  peer chaincode query \
    -C "$channel" -n "$chaincode" \
    -c "$json" 2>&1
}

echo ""
echo "============================================"
echo "  DBH-EHR Blockchain Chaincode Tests"
echo "============================================"
echo ""

echo "--- TEST 1: CreateEhrHash ---"
invoke_cc ehr-hash-channel ehr '{"function":"CreateEhrHash","Args":["ehr-001","1","{\"ehrId\":\"ehr-001\",\"patientDid\":\"did:dbh:patient:12345\",\"createdByDid\":\"did:dbh:doctor:67890\",\"organizationId\":\"org-hospital-a\",\"version\":1,\"contentHash\":\"abc123def456\",\"fileHash\":\"hash789xyz\",\"timestamp\":\"2026-03-06T10:00:00Z\"}"]}'
sleep 2

echo ""
echo "--- TEST 2: GetEhrHash ---"
query_cc ehr-hash-channel ehr '{"function":"GetEhrHash","Args":["ehr-001","1"]}'

echo ""
echo "--- TEST 3: VerifyEhrIntegrity ---"
query_cc ehr-hash-channel ehr '{"function":"VerifyEhrIntegrity","Args":["ehr-001","1","abc123def456"]}'

echo ""
echo "--- TEST 4: GrantConsent ---"
invoke_cc consent-channel consent '{"function":"GrantConsent","Args":["consent-001","did:dbh:patient:12345","did:dbh:doctor:67890","{\"consentId\":\"consent-001\",\"patientDid\":\"did:dbh:patient:12345\",\"granteeDid\":\"did:dbh:doctor:67890\",\"granteeType\":\"DOCTOR\",\"permission\":\"READ\",\"purpose\":\"Treatment\",\"grantedAt\":\"2026-03-06T10:00:00Z\",\"expiresAt\":\"2027-03-06T10:00:00Z\",\"status\":\"ACTIVE\"}"]}'
sleep 2

echo ""
echo "--- TEST 5: GetConsent ---"
query_cc consent-channel consent '{"function":"GetConsent","Args":["consent-001"]}'

echo ""
echo "--- TEST 6: VerifyConsent ---"
query_cc consent-channel consent '{"function":"VerifyConsent","Args":["consent-001","did:dbh:doctor:67890"]}'

echo ""
echo "--- TEST 7: CreateAuditEntry ---"
invoke_cc audit-channel audit '{"function":"CreateAuditEntry","Args":["audit-001","{\"auditId\":\"audit-001\",\"actorDid\":\"did:dbh:doctor:67890\",\"actorType\":\"DOCTOR\",\"action\":\"READ\",\"targetType\":\"EHR\",\"targetId\":\"ehr-001\",\"patientDid\":\"did:dbh:patient:12345\",\"result\":\"SUCCESS\",\"details\":\"Accessed patient record\",\"ipAddress\":\"192.168.1.1\",\"timestamp\":\"2026-03-06T10:05:00Z\"}"]}'
sleep 2

echo ""
echo "--- TEST 8: GetAuditEntry ---"
query_cc audit-channel audit '{"function":"GetAuditEntry","Args":["audit-001"]}'

echo ""
echo "--- TEST 9: GetAuditsByPatient ---"
query_cc audit-channel audit '{"function":"GetAuditsByPatient","Args":["did:dbh:patient:12345"]}'

echo ""
echo "============================================"
echo "  ALL 9 TESTS COMPLETE!"
echo "============================================"
