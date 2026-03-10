# EHR Network — Account Reference

All credentials for the EHR Hyperledger Fabric network.

---

## Fabric CA Bootstrap Admins

These credentials are set in `compose-ca.yaml` and `fabric-ca-server-config.yaml`.
They are used to register all other identities.

| CA Container   | Admin User | Password  | Port  | Purpose                       |
| -------------- | ---------- | --------- | ----- | ----------------------------- |
| `ca_hospital1` | `admin`    | `adminpw` | 7054  | Register Hospital1 identities |
| `ca_hospital2` | `admin`    | `adminpw` | 8054  | Register Hospital2 identities |
| `ca_clinic`    | `admin`    | `adminpw` | 10054 | Register Clinic identities    |
| `ca_orderer`   | `admin`    | `adminpw` | 9054  | Register Orderer identities   |

---

## Hospital1 Identities (registered via ca_hospital1)

| Identity         | Password           | Type   | Purpose                                        |
| ---------------- | ------------------ | ------ | ---------------------------------------------- |
| `peer0`          | `peer0pw`          | peer   | Peer node process (`peer0.hospital1.ehr.com`)  |
| `user1`          | `user1pw`          | client | Default user for testing                       |
| `hospital1admin` | `hospital1adminpw` | admin  | Org admin — can register users, update channel |

**MSP paths:**

- Admin: `organizations/peerOrganizations/hospital1.ehr.com/users/Admin@hospital1.ehr.com/msp`
- User1: `organizations/peerOrganizations/hospital1.ehr.com/users/User1@hospital1.ehr.com/msp`
- Peer0: `organizations/peerOrganizations/hospital1.ehr.com/peers/peer0.hospital1.ehr.com/msp`

---

## Hospital2 Identities (registered via ca_hospital2)

| Identity         | Password           | Type   | Purpose                                        |
| ---------------- | ------------------ | ------ | ---------------------------------------------- |
| `peer0`          | `peer0pw`          | peer   | Peer node process (`peer0.hospital2.ehr.com`)  |
| `user1`          | `user1pw`          | client | Default user for testing                       |
| `hospital2admin` | `hospital2adminpw` | admin  | Org admin — can register users, update channel |

**MSP paths:**

- Admin: `organizations/peerOrganizations/hospital2.ehr.com/users/Admin@hospital2.ehr.com/msp`
- User1: `organizations/peerOrganizations/hospital2.ehr.com/users/User1@hospital2.ehr.com/msp`
- Peer0: `organizations/peerOrganizations/hospital2.ehr.com/peers/peer0.hospital2.ehr.com/msp`

---

## Clinic Identities (registered via ca_clinic)

| Identity      | Password        | Type   | Purpose                                        |
| ------------- | --------------- | ------ | ---------------------------------------------- |
| `peer0`       | `peer0pw`       | peer   | Peer node process (`peer0.clinic.ehr.com`)     |
| `user1`       | `user1pw`       | client | Default user for testing                       |
| `clinicadmin` | `clinicadminpw` | admin  | Org admin — can register users, update channel |

**MSP paths:**

- Admin: `organizations/peerOrganizations/clinic.ehr.com/users/Admin@clinic.ehr.com/msp`
- User1: `organizations/peerOrganizations/clinic.ehr.com/users/User1@clinic.ehr.com/msp`
- Peer0: `organizations/peerOrganizations/clinic.ehr.com/peers/peer0.clinic.ehr.com/msp`

---

## Orderer Identities (registered via ca_orderer)

| Identity       | Password         | Type    | Purpose                                  |
| -------------- | ---------------- | ------- | ---------------------------------------- |
| `orderer`      | `ordererpw`      | orderer | Orderer node process (`orderer.ehr.com`) |
| `ordererAdmin` | `ordererAdminpw` | admin   | Orderer org admin                        |

**MSP paths:**

- Admin: `organizations/ordererOrganizations/ehr.com/users/Admin@ehr.com/msp`
- Orderer: `organizations/ordererOrganizations/ehr.com/orderers/orderer.ehr.com/msp`

---

## CouchDB Credentials

| Container          | User    | Password  | Port | URL                           |
| ------------------ | ------- | --------- | ---- | ----------------------------- |
| `couchdbHospital1` | `admin` | `adminpw` | 5984 | http://localhost:5984/\_utils |
| `couchdbHospital2` | `admin` | `adminpw` | 7984 | http://localhost:7984/\_utils |
| `couchdbClinic`    | `admin` | `adminpw` | 9984 | http://localhost:9984/\_utils |

---

## Identity Types Reference

| Type      | Who                     | Can do                                            |
| --------- | ----------------------- | ------------------------------------------------- |
| `peer`    | Peer containers         | Endorse, commit, gossip                           |
| `client`  | Doctors, patients, apps | Submit & query transactions                       |
| `admin`   | Org administrators      | Register users, update channel, install chaincode |
| `orderer` | Orderer containers      | Order transactions into blocks                    |
