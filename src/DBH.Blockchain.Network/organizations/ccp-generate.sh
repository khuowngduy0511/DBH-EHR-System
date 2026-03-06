#!/usr/bin/env bash

# Function to convert multi-line PEM to a single line with \n characters
function one_line_pem {
    echo "$(awk 'NF {sub(/\r/, ""); printf "%s\\n",$0;}' "$1")"
}

# Function to generate JSON Connection Profile
function json_ccp {
    local PP=$(one_line_pem "$5")
    local CP=$(one_line_pem "$6")
    sed -e "s/\${ORG}/$1/" \
        -e "s/\${ORGDOMAIN}/$2/" \
        -e "s/\${P0PORT}/$3/" \
        -e "s/\${CAPORT}/$4/" \
        -e "s#\${PEERPEM}#$PP#" \
        -e "s#\${CAPEM}#$CP#" \
        organizations/ccp-template.json
}

# Function to generate YAML Connection Profile (Linux-optimized)
function yaml_ccp {
    local PP=$(one_line_pem "$5")
    local CP=$(one_line_pem "$6")
    sed -e "s/\${ORG}/$1/" \
        -e "s/\${ORGDOMAIN}/$2/" \
        -e "s/\${P0PORT}/$3/" \
        -e "s/\${CAPORT}/$4/" \
        -e "s#\${PEERPEM}#$PP#" \
        -e "s#\${CAPEM}#$CP#" \
        organizations/ccp-template.yaml | sed ':a;N;$!ba;s/\\n/\n          /g'
}

# --- Hospital 1 ---
ORG=Hospital1
ORGDOMAIN=hospital1.ehr.com
P0PORT=7051
CAPORT=7054
PEERPEM=organizations/peerOrganizations/hospital1.ehr.com/tlsca/tlsca.hospital1.ehr.com-cert.pem
CAPEM=organizations/peerOrganizations/hospital1.ehr.com/ca/ca.hospital1.ehr.com-cert.pem

json_ccp $ORG $ORGDOMAIN $P0PORT $CAPORT $PEERPEM $CAPEM > organizations/peerOrganizations/hospital1.ehr.com/connection-hospital1.json
yaml_ccp $ORG $ORGDOMAIN $P0PORT $CAPORT $PEERPEM $CAPEM > organizations/peerOrganizations/hospital1.ehr.com/connection-hospital1.yaml

# --- Hospital 2 ---
ORG=Hospital2
ORGDOMAIN=hospital2.ehr.com
P0PORT=9051
CAPORT=8054
PEERPEM=organizations/peerOrganizations/hospital2.ehr.com/tlsca/tlsca.hospital2.ehr.com-cert.pem
CAPEM=organizations/peerOrganizations/hospital2.ehr.com/ca/ca.hospital2.ehr.com-cert.pem

json_ccp $ORG $ORGDOMAIN $P0PORT $CAPORT $PEERPEM $CAPEM > organizations/peerOrganizations/hospital2.ehr.com/connection-hospital2.json
yaml_ccp $ORG $ORGDOMAIN $P0PORT $CAPORT $PEERPEM $CAPEM > organizations/peerOrganizations/hospital2.ehr.com/connection-hospital2.yaml

# --- Clinic ---
ORG=Clinic
ORGDOMAIN=clinic.ehr.com
P0PORT=11051
CAPORT=9054
PEERPEM=organizations/peerOrganizations/clinic.ehr.com/tlsca/tlsca.clinic.ehr.com-cert.pem
CAPEM=organizations/peerOrganizations/clinic.ehr.com/ca/ca.clinic.ehr.com-cert.pem

json_ccp $ORG $ORGDOMAIN $P0PORT $CAPORT $PEERPEM $CAPEM > organizations/peerOrganizations/clinic.ehr.com/connection-clinic.json
yaml_ccp $ORG $ORGDOMAIN $P0PORT $CAPORT $PEERPEM $CAPEM > organizations/peerOrganizations/clinic.ehr.com/connection-clinic.yaml