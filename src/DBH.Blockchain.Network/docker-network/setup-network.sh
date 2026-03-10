#!/bin/bash
#
# Copyright IBM Corp All Rights Reserved
#
# SPDX-License-Identifier: Apache-2.0
#
# Script to create the docker network independently

NETWORK_NAME="fabric_test"

echo "Creating Docker network '$NETWORK_NAME'..."

# Check if the network already exists
if docker network ls | grep -q "$NETWORK_NAME"; then
    echo "Network '$NETWORK_NAME' already exists."
else
    docker network create --opt com.docker.network.enable_ipv6=false "$NETWORK_NAME"
    if [ $? -eq 0 ]; then
        echo "Successfully created network '$NETWORK_NAME'."
    else
        echo "Failed to create network '$NETWORK_NAME'."
        exit 1
    fi
fi
