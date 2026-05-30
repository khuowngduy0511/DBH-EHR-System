#!/usr/bin/env python3
"""Test Fabric CA registration with Basic Auth"""
import urllib.request
import json

url = "https://localhost:7054/api/v1/register"
username = "admin"
password = "adminpw"

body = json.dumps({
    "id": "testuser999",
    "type": "client",
    "secret": "testpw123456",
    "affiliation": "org1.department1",
    "attrs": [
        {"name": "username", "value": "test", "ecert": True}
    ],
    "caname": "ca-hospital1"
}).encode("utf-8")

import base64
credentials = base64.b64encode(f"{username}:{password}".encode()).decode()
headers = {
    "Content-Type": "application/json",
    "Authorization": f"Basic {credentials}"
}

import ssl
ctx = ssl.create_default_context()
ctx.check_hostname = False
ctx.verify_mode = ssl.CERT_NONE

req = urllib.request.Request(url, data=body, headers=headers, method="POST")
try:
    resp = urllib.request.urlopen(req, context=ctx)
    print(f"Status: {resp.status}")
    print(f"Body: {resp.read().decode()}")
except urllib.error.HTTPError as e:
    print(f"Status: {e.code}")
    print(f"Body: {e.read().decode()}")