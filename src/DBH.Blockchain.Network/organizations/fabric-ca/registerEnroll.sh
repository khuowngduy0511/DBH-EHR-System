#!/usr/bin/env bash
#
# Register and enroll identities for EHR network using Fabric CA
# Called by network.sh when CRYPTO="Certificate Authorities"
#

function createHospital1() {
  infoln "Enrolling the CA admin"
  if [ -e "organizations/peerOrganizations" ] && [ ! -d "organizations/peerOrganizations" ]; then
    rm -f organizations/peerOrganizations
  fi
  mkdir -p organizations/peerOrganizations/hospital1.ehr.com/

  export FABRIC_CA_CLIENT_HOME=${WIN_PWD}/organizations/peerOrganizations/hospital1.ehr.com/

  set -x
  fabric-ca-client enroll -u https://admin:adminpw@localhost:7054 --caname ca-hospital1 --tls.certfiles "${WIN_PWD}/organizations/fabric-ca/hospital1/ca-cert.pem"
  { set +x; } 2>/dev/null

  echo 'NodeOUs:
  Enable: true
  ClientOUIdentifier:
    Certificate: cacerts/localhost-7054-ca-hospital1.pem
    OrganizationalUnitIdentifier: client
  PeerOUIdentifier:
    Certificate: cacerts/localhost-7054-ca-hospital1.pem
    OrganizationalUnitIdentifier: peer
  AdminOUIdentifier:
    Certificate: cacerts/localhost-7054-ca-hospital1.pem
    OrganizationalUnitIdentifier: admin
  OrdererOUIdentifier:
    Certificate: cacerts/localhost-7054-ca-hospital1.pem
    OrganizationalUnitIdentifier: orderer' > "${PWD}/organizations/peerOrganizations/hospital1.ehr.com/msp/config.yaml"

  # Copy CA cert to org-level directories
  mkdir -p "${PWD}/organizations/peerOrganizations/hospital1.ehr.com/msp/tlscacerts"
  cp "${PWD}/organizations/fabric-ca/hospital1/ca-cert.pem" "${PWD}/organizations/peerOrganizations/hospital1.ehr.com/msp/tlscacerts/ca.crt"

  mkdir -p "${PWD}/organizations/peerOrganizations/hospital1.ehr.com/tlsca"
  cp "${PWD}/organizations/fabric-ca/hospital1/ca-cert.pem" "${PWD}/organizations/peerOrganizations/hospital1.ehr.com/tlsca/tlsca.hospital1.ehr.com-cert.pem"

  mkdir -p "${PWD}/organizations/peerOrganizations/hospital1.ehr.com/ca"
  cp "${PWD}/organizations/fabric-ca/hospital1/ca-cert.pem" "${PWD}/organizations/peerOrganizations/hospital1.ehr.com/ca/ca.hospital1.ehr.com-cert.pem"

  # Register peer0
  infoln "Registering peer0"
  set -x
  fabric-ca-client register --caname ca-hospital1 --id.name peer0 --id.secret peer0pw --id.type peer --tls.certfiles "${WIN_PWD}/organizations/fabric-ca/hospital1/ca-cert.pem"
  { set +x; } 2>/dev/null

  # Register user1
  infoln "Registering user"
  set -x
  fabric-ca-client register --caname ca-hospital1 --id.name user1 --id.secret user1pw --id.type client --tls.certfiles "${WIN_PWD}/organizations/fabric-ca/hospital1/ca-cert.pem"
  { set +x; } 2>/dev/null

  # Register org admin
  infoln "Registering the org admin"
  set -x
  fabric-ca-client register --caname ca-hospital1 --id.name hospital1admin --id.secret hospital1adminpw --id.type admin --tls.certfiles "${WIN_PWD}/organizations/fabric-ca/hospital1/ca-cert.pem"
  { set +x; } 2>/dev/null

  # Generate peer0 MSP
  infoln "Generating the peer0 msp"
  set -x
  fabric-ca-client enroll -u https://peer0:peer0pw@localhost:7054 --caname ca-hospital1 -M "${WIN_PWD}/organizations/peerOrganizations/hospital1.ehr.com/peers/peer0.hospital1.ehr.com/msp" --tls.certfiles "${WIN_PWD}/organizations/fabric-ca/hospital1/ca-cert.pem"
  { set +x; } 2>/dev/null

  cp "${PWD}/organizations/peerOrganizations/hospital1.ehr.com/msp/config.yaml" "${PWD}/organizations/peerOrganizations/hospital1.ehr.com/peers/peer0.hospital1.ehr.com/msp/config.yaml"

  # Generate peer0 TLS certificates
  infoln "Generating the peer0-tls certificates"
  set -x
  fabric-ca-client enroll -u https://peer0:peer0pw@localhost:7054 --caname ca-hospital1 -M "${WIN_PWD}/organizations/peerOrganizations/hospital1.ehr.com/peers/peer0.hospital1.ehr.com/tls" --enrollment.profile tls --csr.hosts peer0.hospital1.ehr.com --csr.hosts localhost --tls.certfiles "${WIN_PWD}/organizations/fabric-ca/hospital1/ca-cert.pem"
  { set +x; } 2>/dev/null

  # Copy TLS certs to well-known file names
  cp "${PWD}/organizations/peerOrganizations/hospital1.ehr.com/peers/peer0.hospital1.ehr.com/tls/tlscacerts/"* "${PWD}/organizations/peerOrganizations/hospital1.ehr.com/peers/peer0.hospital1.ehr.com/tls/ca.crt"
  cp "${PWD}/organizations/peerOrganizations/hospital1.ehr.com/peers/peer0.hospital1.ehr.com/tls/signcerts/"* "${PWD}/organizations/peerOrganizations/hospital1.ehr.com/peers/peer0.hospital1.ehr.com/tls/server.crt"
  cp "${PWD}/organizations/peerOrganizations/hospital1.ehr.com/peers/peer0.hospital1.ehr.com/tls/keystore/"* "${PWD}/organizations/peerOrganizations/hospital1.ehr.com/peers/peer0.hospital1.ehr.com/tls/server.key"

  # Generate user MSP
  infoln "Generating the user msp"
  set -x
  fabric-ca-client enroll -u https://user1:user1pw@localhost:7054 --caname ca-hospital1 -M "${WIN_PWD}/organizations/peerOrganizations/hospital1.ehr.com/users/User1@hospital1.ehr.com/msp" --tls.certfiles "${WIN_PWD}/organizations/fabric-ca/hospital1/ca-cert.pem"
  { set +x; } 2>/dev/null

  cp "${PWD}/organizations/peerOrganizations/hospital1.ehr.com/msp/config.yaml" "${PWD}/organizations/peerOrganizations/hospital1.ehr.com/users/User1@hospital1.ehr.com/msp/config.yaml"

  # Generate org admin MSP
  infoln "Generating the org admin msp"
  set -x
  fabric-ca-client enroll -u https://hospital1admin:hospital1adminpw@localhost:7054 --caname ca-hospital1 -M "${WIN_PWD}/organizations/peerOrganizations/hospital1.ehr.com/users/Admin@hospital1.ehr.com/msp" --tls.certfiles "${WIN_PWD}/organizations/fabric-ca/hospital1/ca-cert.pem"
  { set +x; } 2>/dev/null

  cp "${PWD}/organizations/peerOrganizations/hospital1.ehr.com/msp/config.yaml" "${PWD}/organizations/peerOrganizations/hospital1.ehr.com/users/Admin@hospital1.ehr.com/msp/config.yaml"
}

function createHospital2() {
  infoln "Enrolling the CA admin"
  if [ -e "organizations/peerOrganizations" ] && [ ! -d "organizations/peerOrganizations" ]; then
    rm -f organizations/peerOrganizations
  fi
  mkdir -p organizations/peerOrganizations/hospital2.ehr.com/

  export FABRIC_CA_CLIENT_HOME=${WIN_PWD}/organizations/peerOrganizations/hospital2.ehr.com/

  set -x
  fabric-ca-client enroll -u https://admin:adminpw@localhost:8054 --caname ca-hospital2 --tls.certfiles "${WIN_PWD}/organizations/fabric-ca/hospital2/ca-cert.pem"
  { set +x; } 2>/dev/null

  echo 'NodeOUs:
  Enable: true
  ClientOUIdentifier:
    Certificate: cacerts/localhost-8054-ca-hospital2.pem
    OrganizationalUnitIdentifier: client
  PeerOUIdentifier:
    Certificate: cacerts/localhost-8054-ca-hospital2.pem
    OrganizationalUnitIdentifier: peer
  AdminOUIdentifier:
    Certificate: cacerts/localhost-8054-ca-hospital2.pem
    OrganizationalUnitIdentifier: admin
  OrdererOUIdentifier:
    Certificate: cacerts/localhost-8054-ca-hospital2.pem
    OrganizationalUnitIdentifier: orderer' > "${PWD}/organizations/peerOrganizations/hospital2.ehr.com/msp/config.yaml"

  mkdir -p "${PWD}/organizations/peerOrganizations/hospital2.ehr.com/msp/tlscacerts"
  cp "${PWD}/organizations/fabric-ca/hospital2/ca-cert.pem" "${PWD}/organizations/peerOrganizations/hospital2.ehr.com/msp/tlscacerts/ca.crt"

  mkdir -p "${PWD}/organizations/peerOrganizations/hospital2.ehr.com/tlsca"
  cp "${PWD}/organizations/fabric-ca/hospital2/ca-cert.pem" "${PWD}/organizations/peerOrganizations/hospital2.ehr.com/tlsca/tlsca.hospital2.ehr.com-cert.pem"

  mkdir -p "${PWD}/organizations/peerOrganizations/hospital2.ehr.com/ca"
  cp "${PWD}/organizations/fabric-ca/hospital2/ca-cert.pem" "${PWD}/organizations/peerOrganizations/hospital2.ehr.com/ca/ca.hospital2.ehr.com-cert.pem"

  infoln "Registering peer0"
  set -x
  fabric-ca-client register --caname ca-hospital2 --id.name peer0 --id.secret peer0pw --id.type peer --tls.certfiles "${WIN_PWD}/organizations/fabric-ca/hospital2/ca-cert.pem"
  { set +x; } 2>/dev/null

  infoln "Registering user"
  set -x
  fabric-ca-client register --caname ca-hospital2 --id.name user1 --id.secret user1pw --id.type client --tls.certfiles "${WIN_PWD}/organizations/fabric-ca/hospital2/ca-cert.pem"
  { set +x; } 2>/dev/null

  infoln "Registering the org admin"
  set -x
  fabric-ca-client register --caname ca-hospital2 --id.name hospital2admin --id.secret hospital2adminpw --id.type admin --tls.certfiles "${WIN_PWD}/organizations/fabric-ca/hospital2/ca-cert.pem"
  { set +x; } 2>/dev/null

  infoln "Generating the peer0 msp"
  set -x
  fabric-ca-client enroll -u https://peer0:peer0pw@localhost:8054 --caname ca-hospital2 -M "${WIN_PWD}/organizations/peerOrganizations/hospital2.ehr.com/peers/peer0.hospital2.ehr.com/msp" --tls.certfiles "${WIN_PWD}/organizations/fabric-ca/hospital2/ca-cert.pem"
  { set +x; } 2>/dev/null

  cp "${PWD}/organizations/peerOrganizations/hospital2.ehr.com/msp/config.yaml" "${PWD}/organizations/peerOrganizations/hospital2.ehr.com/peers/peer0.hospital2.ehr.com/msp/config.yaml"

  infoln "Generating the peer0-tls certificates"
  set -x
  fabric-ca-client enroll -u https://peer0:peer0pw@localhost:8054 --caname ca-hospital2 -M "${WIN_PWD}/organizations/peerOrganizations/hospital2.ehr.com/peers/peer0.hospital2.ehr.com/tls" --enrollment.profile tls --csr.hosts peer0.hospital2.ehr.com --csr.hosts localhost --tls.certfiles "${WIN_PWD}/organizations/fabric-ca/hospital2/ca-cert.pem"
  { set +x; } 2>/dev/null

  cp "${PWD}/organizations/peerOrganizations/hospital2.ehr.com/peers/peer0.hospital2.ehr.com/tls/tlscacerts/"* "${PWD}/organizations/peerOrganizations/hospital2.ehr.com/peers/peer0.hospital2.ehr.com/tls/ca.crt"
  cp "${PWD}/organizations/peerOrganizations/hospital2.ehr.com/peers/peer0.hospital2.ehr.com/tls/signcerts/"* "${PWD}/organizations/peerOrganizations/hospital2.ehr.com/peers/peer0.hospital2.ehr.com/tls/server.crt"
  cp "${PWD}/organizations/peerOrganizations/hospital2.ehr.com/peers/peer0.hospital2.ehr.com/tls/keystore/"* "${PWD}/organizations/peerOrganizations/hospital2.ehr.com/peers/peer0.hospital2.ehr.com/tls/server.key"

  infoln "Generating the user msp"
  set -x
  fabric-ca-client enroll -u https://user1:user1pw@localhost:8054 --caname ca-hospital2 -M "${WIN_PWD}/organizations/peerOrganizations/hospital2.ehr.com/users/User1@hospital2.ehr.com/msp" --tls.certfiles "${WIN_PWD}/organizations/fabric-ca/hospital2/ca-cert.pem"
  { set +x; } 2>/dev/null

  cp "${PWD}/organizations/peerOrganizations/hospital2.ehr.com/msp/config.yaml" "${PWD}/organizations/peerOrganizations/hospital2.ehr.com/users/User1@hospital2.ehr.com/msp/config.yaml"

  infoln "Generating the org admin msp"
  set -x
  fabric-ca-client enroll -u https://hospital2admin:hospital2adminpw@localhost:8054 --caname ca-hospital2 -M "${WIN_PWD}/organizations/peerOrganizations/hospital2.ehr.com/users/Admin@hospital2.ehr.com/msp" --tls.certfiles "${WIN_PWD}/organizations/fabric-ca/hospital2/ca-cert.pem"
  { set +x; } 2>/dev/null

  cp "${PWD}/organizations/peerOrganizations/hospital2.ehr.com/msp/config.yaml" "${PWD}/organizations/peerOrganizations/hospital2.ehr.com/users/Admin@hospital2.ehr.com/msp/config.yaml"
}

function createClinic() {
  infoln "Enrolling the CA admin"
  if [ -e "organizations/peerOrganizations" ] && [ ! -d "organizations/peerOrganizations" ]; then
    rm -f organizations/peerOrganizations
  fi
  mkdir -p organizations/peerOrganizations/clinic.ehr.com/

  export FABRIC_CA_CLIENT_HOME=${WIN_PWD}/organizations/peerOrganizations/clinic.ehr.com/

  set -x
  fabric-ca-client enroll -u https://admin:adminpw@localhost:10054 --caname ca-clinic --tls.certfiles "${WIN_PWD}/organizations/fabric-ca/clinic/ca-cert.pem"
  { set +x; } 2>/dev/null

  echo 'NodeOUs:
  Enable: true
  ClientOUIdentifier:
    Certificate: cacerts/localhost-10054-ca-clinic.pem
    OrganizationalUnitIdentifier: client
  PeerOUIdentifier:
    Certificate: cacerts/localhost-10054-ca-clinic.pem
    OrganizationalUnitIdentifier: peer
  AdminOUIdentifier:
    Certificate: cacerts/localhost-10054-ca-clinic.pem
    OrganizationalUnitIdentifier: admin
  OrdererOUIdentifier:
    Certificate: cacerts/localhost-10054-ca-clinic.pem
    OrganizationalUnitIdentifier: orderer' > "${PWD}/organizations/peerOrganizations/clinic.ehr.com/msp/config.yaml"

  mkdir -p "${PWD}/organizations/peerOrganizations/clinic.ehr.com/msp/tlscacerts"
  cp "${PWD}/organizations/fabric-ca/clinic/ca-cert.pem" "${PWD}/organizations/peerOrganizations/clinic.ehr.com/msp/tlscacerts/ca.crt"

  mkdir -p "${PWD}/organizations/peerOrganizations/clinic.ehr.com/tlsca"
  cp "${PWD}/organizations/fabric-ca/clinic/ca-cert.pem" "${PWD}/organizations/peerOrganizations/clinic.ehr.com/tlsca/tlsca.clinic.ehr.com-cert.pem"

  mkdir -p "${PWD}/organizations/peerOrganizations/clinic.ehr.com/ca"
  cp "${PWD}/organizations/fabric-ca/clinic/ca-cert.pem" "${PWD}/organizations/peerOrganizations/clinic.ehr.com/ca/ca.clinic.ehr.com-cert.pem"

  infoln "Registering peer0"
  set -x
  fabric-ca-client register --caname ca-clinic --id.name peer0 --id.secret peer0pw --id.type peer --tls.certfiles "${WIN_PWD}/organizations/fabric-ca/clinic/ca-cert.pem"
  { set +x; } 2>/dev/null

  infoln "Registering user"
  set -x
  fabric-ca-client register --caname ca-clinic --id.name user1 --id.secret user1pw --id.type client --tls.certfiles "${WIN_PWD}/organizations/fabric-ca/clinic/ca-cert.pem"
  { set +x; } 2>/dev/null

  infoln "Registering the org admin"
  set -x
  fabric-ca-client register --caname ca-clinic --id.name clinicadmin --id.secret clinicadminpw --id.type admin --tls.certfiles "${WIN_PWD}/organizations/fabric-ca/clinic/ca-cert.pem"
  { set +x; } 2>/dev/null

  infoln "Generating the peer0 msp"
  set -x
  fabric-ca-client enroll -u https://peer0:peer0pw@localhost:10054 --caname ca-clinic -M "${WIN_PWD}/organizations/peerOrganizations/clinic.ehr.com/peers/peer0.clinic.ehr.com/msp" --tls.certfiles "${WIN_PWD}/organizations/fabric-ca/clinic/ca-cert.pem"
  { set +x; } 2>/dev/null

  cp "${PWD}/organizations/peerOrganizations/clinic.ehr.com/msp/config.yaml" "${PWD}/organizations/peerOrganizations/clinic.ehr.com/peers/peer0.clinic.ehr.com/msp/config.yaml"

  infoln "Generating the peer0-tls certificates"
  set -x
  fabric-ca-client enroll -u https://peer0:peer0pw@localhost:10054 --caname ca-clinic -M "${WIN_PWD}/organizations/peerOrganizations/clinic.ehr.com/peers/peer0.clinic.ehr.com/tls" --enrollment.profile tls --csr.hosts peer0.clinic.ehr.com --csr.hosts localhost --tls.certfiles "${WIN_PWD}/organizations/fabric-ca/clinic/ca-cert.pem"
  { set +x; } 2>/dev/null

  cp "${PWD}/organizations/peerOrganizations/clinic.ehr.com/peers/peer0.clinic.ehr.com/tls/tlscacerts/"* "${PWD}/organizations/peerOrganizations/clinic.ehr.com/peers/peer0.clinic.ehr.com/tls/ca.crt"
  cp "${PWD}/organizations/peerOrganizations/clinic.ehr.com/peers/peer0.clinic.ehr.com/tls/signcerts/"* "${PWD}/organizations/peerOrganizations/clinic.ehr.com/peers/peer0.clinic.ehr.com/tls/server.crt"
  cp "${PWD}/organizations/peerOrganizations/clinic.ehr.com/peers/peer0.clinic.ehr.com/tls/keystore/"* "${PWD}/organizations/peerOrganizations/clinic.ehr.com/peers/peer0.clinic.ehr.com/tls/server.key"

  infoln "Generating the user msp"
  set -x
  fabric-ca-client enroll -u https://user1:user1pw@localhost:10054 --caname ca-clinic -M "${WIN_PWD}/organizations/peerOrganizations/clinic.ehr.com/users/User1@clinic.ehr.com/msp" --tls.certfiles "${WIN_PWD}/organizations/fabric-ca/clinic/ca-cert.pem"
  { set +x; } 2>/dev/null

  cp "${PWD}/organizations/peerOrganizations/clinic.ehr.com/msp/config.yaml" "${PWD}/organizations/peerOrganizations/clinic.ehr.com/users/User1@clinic.ehr.com/msp/config.yaml"

  infoln "Generating the org admin msp"
  set -x
  fabric-ca-client enroll -u https://clinicadmin:clinicadminpw@localhost:10054 --caname ca-clinic -M "${WIN_PWD}/organizations/peerOrganizations/clinic.ehr.com/users/Admin@clinic.ehr.com/msp" --tls.certfiles "${WIN_PWD}/organizations/fabric-ca/clinic/ca-cert.pem"
  { set +x; } 2>/dev/null

  cp "${PWD}/organizations/peerOrganizations/clinic.ehr.com/msp/config.yaml" "${PWD}/organizations/peerOrganizations/clinic.ehr.com/users/Admin@clinic.ehr.com/msp/config.yaml"
}

function createOrderer() {
  infoln "Enrolling the CA admin"
  if [ -e "organizations/ordererOrganizations" ] && [ ! -d "organizations/ordererOrganizations" ]; then
    rm -f organizations/ordererOrganizations
  fi
  mkdir -p organizations/ordererOrganizations/ehr.com

  export FABRIC_CA_CLIENT_HOME=${WIN_PWD}/organizations/ordererOrganizations/ehr.com

  set -x
  fabric-ca-client enroll -u https://admin:adminpw@localhost:9054 --caname ca-orderer --tls.certfiles "${WIN_PWD}/organizations/fabric-ca/ordererOrg/ca-cert.pem"
  { set +x; } 2>/dev/null

  echo 'NodeOUs:
  Enable: true
  ClientOUIdentifier:
    Certificate: cacerts/localhost-9054-ca-orderer.pem
    OrganizationalUnitIdentifier: client
  PeerOUIdentifier:
    Certificate: cacerts/localhost-9054-ca-orderer.pem
    OrganizationalUnitIdentifier: peer
  AdminOUIdentifier:
    Certificate: cacerts/localhost-9054-ca-orderer.pem
    OrganizationalUnitIdentifier: admin
  OrdererOUIdentifier:
    Certificate: cacerts/localhost-9054-ca-orderer.pem
    OrganizationalUnitIdentifier: orderer' > "${PWD}/organizations/ordererOrganizations/ehr.com/msp/config.yaml"

  mkdir -p "${PWD}/organizations/ordererOrganizations/ehr.com/msp/tlscacerts"
  cp "${PWD}/organizations/fabric-ca/ordererOrg/ca-cert.pem" "${PWD}/organizations/ordererOrganizations/ehr.com/msp/tlscacerts/tlsca.ehr.com-cert.pem"

  mkdir -p "${PWD}/organizations/ordererOrganizations/ehr.com/tlsca"
  cp "${PWD}/organizations/fabric-ca/ordererOrg/ca-cert.pem" "${PWD}/organizations/ordererOrganizations/ehr.com/tlsca/tlsca.ehr.com-cert.pem"

  # Register and enroll the orderer
  infoln "Registering orderer"
  set -x
  fabric-ca-client register --caname ca-orderer --id.name orderer --id.secret ordererpw --id.type orderer --tls.certfiles "${WIN_PWD}/organizations/fabric-ca/ordererOrg/ca-cert.pem"
  { set +x; } 2>/dev/null

  infoln "Generating the orderer MSP"
  set -x
  fabric-ca-client enroll -u https://orderer:ordererpw@localhost:9054 --caname ca-orderer -M "${WIN_PWD}/organizations/ordererOrganizations/ehr.com/orderers/orderer.ehr.com/msp" --tls.certfiles "${WIN_PWD}/organizations/fabric-ca/ordererOrg/ca-cert.pem"
  { set +x; } 2>/dev/null

  cp "${PWD}/organizations/ordererOrganizations/ehr.com/msp/config.yaml" "${PWD}/organizations/ordererOrganizations/ehr.com/orderers/orderer.ehr.com/msp/config.yaml"

  infoln "Generating the orderer TLS certificates"
  set -x
  fabric-ca-client enroll -u https://orderer:ordererpw@localhost:9054 --caname ca-orderer -M "${WIN_PWD}/organizations/ordererOrganizations/ehr.com/orderers/orderer.ehr.com/tls" --enrollment.profile tls --csr.hosts orderer.ehr.com --csr.hosts localhost --tls.certfiles "${WIN_PWD}/organizations/fabric-ca/ordererOrg/ca-cert.pem"
  { set +x; } 2>/dev/null

  # Copy TLS certs to well-known file names
  cp "${PWD}/organizations/ordererOrganizations/ehr.com/orderers/orderer.ehr.com/tls/tlscacerts/"* "${PWD}/organizations/ordererOrganizations/ehr.com/orderers/orderer.ehr.com/tls/ca.crt"
  cp "${PWD}/organizations/ordererOrganizations/ehr.com/orderers/orderer.ehr.com/tls/signcerts/"* "${PWD}/organizations/ordererOrganizations/ehr.com/orderers/orderer.ehr.com/tls/server.crt"
  cp "${PWD}/organizations/ordererOrganizations/ehr.com/orderers/orderer.ehr.com/tls/keystore/"* "${PWD}/organizations/ordererOrganizations/ehr.com/orderers/orderer.ehr.com/tls/server.key"

  # Copy orderer CA cert to orderer's MSP tlscacerts
  mkdir -p "${PWD}/organizations/ordererOrganizations/ehr.com/orderers/orderer.ehr.com/msp/tlscacerts"
  cp "${PWD}/organizations/ordererOrganizations/ehr.com/orderers/orderer.ehr.com/tls/tlscacerts/"* "${PWD}/organizations/ordererOrganizations/ehr.com/orderers/orderer.ehr.com/msp/tlscacerts/tlsca.ehr.com-cert.pem"

  # Register and enroll orderer admin
  infoln "Registering the orderer admin"
  set -x
  fabric-ca-client register --caname ca-orderer --id.name ordererAdmin --id.secret ordererAdminpw --id.type admin --tls.certfiles "${WIN_PWD}/organizations/fabric-ca/ordererOrg/ca-cert.pem"
  { set +x; } 2>/dev/null

  infoln "Generating the admin msp"
  set -x
  fabric-ca-client enroll -u https://ordererAdmin:ordererAdminpw@localhost:9054 --caname ca-orderer -M "${WIN_PWD}/organizations/ordererOrganizations/ehr.com/users/Admin@ehr.com/msp" --tls.certfiles "${WIN_PWD}/organizations/fabric-ca/ordererOrg/ca-cert.pem"
  { set +x; } 2>/dev/null

  cp "${PWD}/organizations/ordererOrganizations/ehr.com/msp/config.yaml" "${PWD}/organizations/ordererOrganizations/ehr.com/users/Admin@ehr.com/msp/config.yaml"
}