# DBH Chaincode Base (Unified)

Single package that bundles EHR hash, Consent, Audit, and Emergency Access contracts for deployment to one Fabric channel.

## Layout
- `index.js` exports all four contracts in one chaincode package.
- `lib/ehrContract.js`, `lib/consentContract.js`, `lib/auditContract.js`, `lib/emergencyContract.js` contain the logic merged from the existing separated chaincodes.

## Quick Build
```
npm install
```

## Package (optional)
From `DBH.Blockchain.Network`:
```
peer lifecycle chaincode package chaincode-base.tar.gz \
  --path ./chaincode-base \
  --lang node \
  --label chaincode-base_1
```

## Deploy Example (single channel)
```
./network.sh deployCC \
  -ccn chaincode-base \
  -ccp ./chaincode-base \
  -ccl javascript \
  -c <your-channel-name>
```
If your script expects init, add `-cci ""`.

## Functions
- EHR: `CreateEhrHash`, `UpdateEhrHash`, `GetEhrHash`, `GetEhrHistory`, `VerifyEhrIntegrity`
- Consent: `GrantConsent`, `RevokeConsent`, `GetConsent`, `VerifyConsent`, `GetPatientConsents`, `GetConsentHistory`, `GetConsentsByRecord`, `GetConsentsByGrantee`, `GetAllConsents`
- Audit: `CreateAuditEntry`, `GetAuditEntry`, `GetAuditsByPatient`, `GetAuditsByActor`, `GetAuditsByTarget`
- Emergency: `EmergencyAccess`, `GetEmergencyAccessByRecord`, `GetEmergencyAccessByAccessor`, `GetAllEmergencyAccess`

## Notes
- `SimulationMode` in the .NET gateway remains applicable; this chaincode is ready for real peer deployment.
- CouchDB rich queries are used for the consent queries; ensure CouchDB is enabled for the channel peers.
