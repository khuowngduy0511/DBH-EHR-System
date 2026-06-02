#!/bin/sh
# Test CA hospital2 enrollment
echo "=== Testing CA hospital2 connectivity ==="
curl -sk https://ca_hospital2:8054/api/v1/enroll \
  -X POST \
  -u admin:adminpw \
  -H "Content-Type: application/json" \
  -d '{"certificate_request":"test"}'
echo ""
echo "=== Done ==="