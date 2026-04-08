# EHR Network - Hyperledger Fabric

An Electronic Health Record (EHR) management system built on Hyperledger Fabric with three organizations: **Hospital1**, **Hospital2**, and **Clinic**. Medical records are stored in **IPFS**; the blockchain stores pointers (CIDs) and manages access control through consent-based permissions.

Certificates are managed by **Fabric Certificate Authorities (CA)** — not `cryptogen` — enabling dynamic user registration and enrollment at runtime.

## Network Architecture

| Component           | Container                 | Port  |
| ------------------- | ------------------------- | ----- |
| Orderer             | `orderer.ehr.com`         | 7050  |
| Hospital1 Peer      | `peer0.hospital1.ehr.com` | 7051  |
| Hospital2 Peer      | `peer0.hospital2.ehr.com` | 9051  |
| Clinic Peer         | `peer0.clinic.ehr.com`    | 11051 |
| CouchDB (Hospital1) | `couchdbHospital1`        | 5984  |
| CouchDB (Hospital2) | `couchdbHospital2`        | 7984  |
| CouchDB (Clinic)    | `couchdbClinic`           | 9984  |
| CA (Hospital1)      | `ca_hospital1`            | 7054  |
| CA (Hospital2)      | `ca_hospital2`            | 8054  |
| CA (Clinic)         | `ca_clinic`               | 10054 |
| CA (Orderer)        | `ca_orderer`              | 9054  |

## Prerequisites

- [Hyperledger Fabric Binaries](https://hyperledger-fabric.readthedocs.io/en/latest/install.html) (`peer`, `cryptogen`, `configtxgen`, `osnadmin`)
- Docker and Docker Compose
- jq
- Node.js >= 20 (for chaincode)

## Quick Start

Running on linux

### Change from crlf to lf

find . -type f -name "*.sh" -exec sed -i 's/\r$//' {} +

### Change permission

chmod +x organizations/ccp-generate.sh
chmod +x scripts/*.sh
chmod +x network.sh
chmod +x explorer/setup.sh

### 1. Bring Up the Network

```bash
cd DBH.Blockchain.Network
./network.sh up -s couchdb
```

This uses **Fabric CA** by default (`CRYPTO="CA"` in `network.config`). The CA containers start first, then identities for all orgs are registered and enrolled automatically via `organizations/fabric-ca/registerEnroll.sh`.

- If there is error, try

### 2. Create the Channel

```bash
./network.sh createChannel -c ehr-channel
```

### 3. Deploy the EHR Chaincode

```bash
./network.sh deployCC -ccn ehrcc -ccp ./chaincode-javascript -ccl javascript -c ehr-channel
```

### 4. Set Environment for Hospital1

```bash
export FABRIC_CFG_PATH=$PWD/../config/
export CORE_PEER_TLS_ENABLED=true
export CORE_PEER_LOCALMSPID=Hospital1MSP
export CORE_PEER_TLS_ROOTCERT_FILE=${PWD}/organizations/peerOrganizations/hospital1.ehr.com/tlsca/tlsca.hospital1.ehr.com-cert.pem
export CORE_PEER_MSPCONFIGPATH=${PWD}/organizations/peerOrganizations/hospital1.ehr.com/users/Admin@hospital1.ehr.com/msp
export CORE_PEER_ADDRESS=localhost:7051
export ORDERER_CA=${PWD}/organizations/ordererOrganizations/ehr.com/tlsca/tlsca.ehr.com-cert.pem
export PEER0_H1_CA=${PWD}/organizations/peerOrganizations/hospital1.ehr.com/tlsca/tlsca.hospital1.ehr.com-cert.pem
export PEER0_H2_CA=${PWD}/organizations/peerOrganizations/hospital2.ehr.com/tlsca/tlsca.hospital2.ehr.com-cert.pem
export PEER0_CL_CA=${PWD}/organizations/peerOrganizations/clinic.ehr.com/tlsca/tlsca.clinic.ehr.com-cert.pem
```

### 5. Initialize the Ledger

```bash
peer chaincode invoke -o localhost:7050 \
  --ordererTLSHostnameOverride orderer.ehr.com --tls --cafile $ORDERER_CA \
  -C ehr-channel -n ehrcc \
  --peerAddresses localhost:7051 --tlsRootCertFiles $PEER0_H1_CA \
  --peerAddresses localhost:9051 --tlsRootCertFiles $PEER0_H2_CA \
  --peerAddresses localhost:11051 --tlsRootCertFiles $PEER0_CL_CA \
  -c '{"function":"InitLedger","Args":[]}'
```

### 6. Create an EHR Record

```bash
peer chaincode invoke -o localhost:7050 \
  --ordererTLSHostnameOverride orderer.ehr.com --tls --cafile $ORDERER_CA \
  -C ehr-channel -n ehrcc \
  --peerAddresses localhost:7051 --tlsRootCertFiles $PEER0_H1_CA \
  --peerAddresses localhost:9051 --tlsRootCertFiles $PEER0_H2_CA \
  --peerAddresses localhost:11051 --tlsRootCertFiles $PEER0_CL_CA \
  -c '{"function":"CreateEHR","Args":["EHR007","PAT006","DOC005","QmNewIpfsCidAbcde12345","Prescription"]}'
```

### 7. Grant Consent (Allow Clinic to Read a Record)

```bash
peer chaincode invoke -o localhost:7050 \
  --ordererTLSHostnameOverride orderer.ehr.com --tls --cafile $ORDERER_CA \
  -C ehr-channel -n ehrcc \
  --peerAddresses localhost:7051 --tlsRootCertFiles $PEER0_H1_CA \
  --peerAddresses localhost:9051 --tlsRootCertFiles $PEER0_H2_CA \
  --peerAddresses localhost:11051 --tlsRootCertFiles $PEER0_CL_CA \
  -c '{"function":"GrantConsent","Args":["CONSENT003","PAT006","EHR007","[\"ClinicMSP\"]"]}'
```

### 8. Read EHR with Consent Check (Creates Audit Log)

```bash
peer chaincode invoke -o localhost:7050 \
  --ordererTLSHostnameOverride orderer.ehr.com --tls --cafile $ORDERER_CA \
  -C ehr-channel -n ehrcc \
  --peerAddresses localhost:7051 --tlsRootCertFiles $PEER0_H1_CA \
  --peerAddresses localhost:9051 --tlsRootCertFiles $PEER0_H2_CA \
  --peerAddresses localhost:11051 --tlsRootCertFiles $PEER0_CL_CA \
  -c '{"function":"ReadEHRWithConsent","Args":["EHR007","Patient referral review"]}'
```

### 9. Query All EHR Records

```bash
peer chaincode query -C ehr-channel -n ehrcc -c '{"Args":["GetAllEHRs"]}'
```

### 10. Query Access Logs for a Record

```bash
peer chaincode query -C ehr-channel -n ehrcc -c '{"Args":["GetAccessLogsByRecord","EHR007"]}'
```

### 11. Shut Down the Network

```bash
./network.sh down
```

## Smart Contract — 3 Asset Types

### EHR Record (IPFS Pointer)

```json
{
  "docType": "ehr",
  "recordId": "EHR001",
  "patientId": "PAT001",
  "creatorOrg": "Hospital1MSP",
  "ipfsCid": "QmExampleCid1abc...",
  "recordType": "Lab Report",
  "timestamp": "2026-01-15T10:30:00Z"
}
```

### Consent (Access Permission)

```json
{
  "docType": "consent",
  "consentId": "CONSENT001",
  "patientId": "PAT001",
  "targetRecordId": "EHR001",
  "grantedToOrgs": ["Hospital1MSP", "ClinicMSP"],
  "status": "ACTIVE",
  "timestamp": "2026-01-15T10:35:00Z"
}
```

### Access Log (Audit Trail)

```json
{
  "docType": "accessLog",
  "logId": "LOG_EHR001_1709283023000",
  "targetRecordId": "EHR001",
  "accessorId": "x509::CN=admin...",
  "accessorOrg": "ClinicMSP",
  "action": "READ_IPFS_CID",
  "reason": "Patient referral review",
  "timestamp": "2026-03-01T07:30:23Z"
}
```

## Smart Contract Functions

### EHR Functions

| Function                | Description                                                    |
| ----------------------- | -------------------------------------------------------------- |
| `CreateEHR`             | Creates a new EHR pointer (auto-captures creator org from MSP) |
| `ReadEHR`               | Reads EHR metadata (no consent check)                          |
| `ReadEHRWithConsent`    | Consent-gated read + auto-creates access log                   |
| `UpdateEHR`             | Updates IPFS CID (creator org only)                            |
| `DeleteEHR`             | Deletes EHR (creator org only)                                 |
| `GetAllEHRs`            | Returns all EHR records                                        |
| `GetEHRsByPatient`      | Query by patient ID (CouchDB)                                  |
| `GetEHRsByOrganization` | Query by creator org (CouchDB)                                 |
| `GetEHRHistory`         | Full change history for a record                               |

### Consent Functions

| Function               | Description                                |
| ---------------------- | ------------------------------------------ |
| `GrantConsent`         | Grants org(s) access to a specific record  |
| `RevokeConsent`        | Revokes a consent (sets status to REVOKED) |
| `ReadConsent`          | Reads a consent by ID                      |
| `GetConsentsByPatient` | All consents for a patient (CouchDB)       |
| `GetConsentsByRecord`  | All consents for a record (CouchDB)        |
| `GetAllConsents`       | Returns all consent records                |

### Access Log Functions

| Function                  | Description                            |
| ------------------------- | -------------------------------------- |
| `GetAccessLogsByRecord`   | All access logs for a record (CouchDB) |
| `GetAccessLogsByAccessor` | All access logs by an org (CouchDB)    |
| `GetAllAccessLogs`        | Returns all access log records         |

## Endorsement Policy

MAJORITY endorsement — requires at least **2 out of 3** organizations to endorse a transaction.

## Directory Structure

```
ehr/
├── chaincode-javascript/     # EHR Smart Contract (Node.js)
│   ├── lib/
│   │   └── ehrContract.js    # Main contract (EHR + Consent + AccessLog)
│   ├── index.js
│   └── package.json
├── compose/                  # Docker Compose files
│   ├── compose-ehr-net.yaml
│   ├── compose-couch.yaml
│   ├── compose-ca.yaml       # Fabric CA containers
│   └── docker/
├── configtx/
│   └── configtx.yaml
├── organizations/
│   ├── cryptogen/            # Cryptogen configs (fallback)
│   ├── fabric-ca/            # Fabric CA configs & enrollment scripts
│   │   ├── hospital1/
│   │   ├── hospital2/
│   │   ├── clinic/
│   │   ├── ordererOrg/
│   │   └── registerEnroll.sh # Identity registration & enrollment
│   ├── ccp-generate.sh
│   ├── ccp-template.json
│   └── ccp-template.yaml
├── scripts/
├── network.sh
├── network.config            # Default: CRYPTO="CA"
└── README.md
```
