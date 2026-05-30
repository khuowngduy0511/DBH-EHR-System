#!/bin/sh
# Test Fabric CA register with Basic Auth
curl -ks -u admin:adminpw \
  -X POST https://ca_hospital1:7054/api/v1/register \
  -H "Content-Type: application/json" \
  -d '{"id":"testuser999","type":"client","secret":"testpw123456","affiliation":"org1.department1","attrs":[{"name":"username","value":"test","ecert":true}],"caname":"ca-hospital1"}'