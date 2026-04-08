#!/usr/bin/env bash
#
# Copyright IBM Corp. All Rights Reserved.
#
# SPDX-License-Identifier: Apache-2.0
#

# import utils
EHR_NETWORK_HOME=${EHR_NETWORK_HOME:-${PWD}}
WIN_EHR_NETWORK_HOME=${WIN_PWD:-${PWD}}
. ${EHR_NETWORK_HOME}/scripts/configUpdate.sh


# NOTE: This requires jq and configtxlator for execution.
createAnchorPeerUpdate() {
  infoln "Fetching channel config for channel $CHANNEL_NAME"
  fetchChannelConfig $ORG $CHANNEL_NAME ${WIN_EHR_NETWORK_HOME}/channel-artifacts/${CORE_PEER_LOCALMSPID}config.json

  infoln "Generating anchor peer update transaction for Org${ORG} on channel $CHANNEL_NAME"

  if [ $ORG -eq 1 ]; then
    HOST="peer0.hospital1.ehr.com"
    PORT=7051
  elif [ $ORG -eq 2 ]; then
    HOST="peer0.hospital2.ehr.com"
    PORT=9051
  elif [ $ORG -eq 3 ]; then
    HOST="peer0.clinic.ehr.com"
    PORT=11051
  else
    errorln "Org${ORG} unknown"
  fi

  set -x
  jq '.channel_group.groups.Application.groups.'${CORE_PEER_LOCALMSPID}'.values += {"AnchorPeers":{"mod_policy": "Admins","value":{"anchor_peers": [{"host": "'$HOST'","port": '$PORT'}]},"version": "0"}}' ${WIN_EHR_NETWORK_HOME}/channel-artifacts/${CORE_PEER_LOCALMSPID}config.json > ${WIN_EHR_NETWORK_HOME}/channel-artifacts/${CORE_PEER_LOCALMSPID}modified_config.json
  res=$?
  { set +x; } 2>/dev/null
  verifyResult $res "Channel configuration update for anchor peer failed, make sure you have jq installed"

  createConfigUpdate ${CHANNEL_NAME} ${WIN_EHR_NETWORK_HOME}/channel-artifacts/${CORE_PEER_LOCALMSPID}config.json ${WIN_EHR_NETWORK_HOME}/channel-artifacts/${CORE_PEER_LOCALMSPID}modified_config.json ${WIN_EHR_NETWORK_HOME}/channel-artifacts/${CORE_PEER_LOCALMSPID}anchors.tx
}

updateAnchorPeer() {
  peer channel update -o localhost:7050 --ordererTLSHostnameOverride orderer.ehr.com -c $CHANNEL_NAME -f ${WIN_EHR_NETWORK_HOME}/channel-artifacts/${CORE_PEER_LOCALMSPID}anchors.tx --tls --cafile "$ORDERER_CA" >&log.txt
  res=$?
  cat log.txt
  verifyResult $res "Anchor peer update failed"
  successln "Anchor peer set for org '$CORE_PEER_LOCALMSPID' on channel '$CHANNEL_NAME'"
}

ORG=$1
CHANNEL_NAME=$2

setGlobals $ORG

createAnchorPeerUpdate

updateAnchorPeer
