# DBH Chaincode Tester

A small console harness to exercise the Fabric chaincode functions implemented in `DBH.Shared.Infrastructure.Blockchain.Services`.

## Prerequisites
- .NET 8 SDK
- Hyperledger Fabric network running if you want real transactions. When Fabric is unavailable, leave `SimulationMode` as `true` to generate fake tx hashes.

## Configure
Edit `appsettings.json` in this folder (or override with `CHAINCODE_TESTER_HyperledgerFabric__*` environment variables):
- Point `CertificatePath`, `PrivateKeyDirectory` or `PrivateKeyPath`, and `TlsCertificatePath` to your enrollment materials.
- Set `SimulationMode` to `false` for real network calls.
- Adjust `TestData` to match DIDs and record IDs you want to exercise.

## Run
From the repo root:

```
dotnet run --project src/DBH.Chaincode.Tester -- smoke
```

Replace `smoke` with a specific command (see the help output). Examples:

```
dotnet run --project src/DBH.Chaincode.Tester -- ping

dotnet run --project src/DBH.Chaincode.Tester -- ehr-get ehr-demo-001 1

dotnet run --project src/DBH.Chaincode.Tester -- consent-verify consent-demo-001 did:example:doctor-123
```

## Commands
- `ping`: Check Fabric gateway connectivity
- `smoke`: Commit a sample EHR hash, grant consent, and write an audit entry
- `ehr-commit`, `ehr-get`, `ehr-history`, `ehr-verify`
- `consent-grant`, `consent-revoke`, `consent-get`, `consent-verify`, `consent-patient`, `consent-history`
- `audit-commit`, `audit-get`, `audit-by-patient`, `audit-by-actor`
- `emergency-access`, `emergency-by-record`, `emergency-by-accessor`, `emergency-all`
- `account-register`, `account-login`

Fabric CA enrollments are not stored in the application database. The tester prints the enrollment secret and the expected MSP path for the identity, which is typically `/tmp/fabric-crypto/peerOrganizations/<org>/users/<enrollmentId>@<org>/msp` in the running containerized environment.

Outputs are printed as JSON where applicable. Any missing arguments will be prompted interactively.
