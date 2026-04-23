# DBH.UnitTest - Test Documentation

Integration and E2E test suites for DBH-EHR-System using xUnit + HttpClient.

## Focus

- Unit-style API integration tests by service under `unitTest/*`.
- Real workflow E2E tests under `e2e/*`.
- One test case per file convention is applied across `unitTest` and `e2e`.

## Current Inventory

| Suite | Folder | Test Files |
|---|---|---:|
| E2E | `e2e` | 17 |
| Auth Service | `unitTest/auth-service` | 34 |
| Appointment Service | `unitTest/appointment-service` | 34 |
| Audit Service | `unitTest/audit-service` | 14 |
| Blockchain Service | `unitTest/blockchain-service` | 30 |
| Consent Service | `unitTest/consent-service` | 20 |
| EHR Service | `unitTest/ehr-service` | 35 |
| Notification Service | `unitTest/notification-service` | 12 |
| Organization Service | `unitTest/organization-service` | 28 |
| Payment Service | `unitTest/payment-service` | 10 |
| **Total** |  | **234** |

## E2E Real-Flow Suites

- `AppointmentLifecycleTests.cs`
- `AuditTrailTests.cs`
- `ConsentWorkflowTests.cs`
- `EhrBulkQueryTests.cs`
- `EhrCrudLifecycleTests.cs`
- `EhrDataAccessControlTests.cs`
- `EhrFileManagementTests.cs`
- `EhrLifecycleTests.cs`
- `EhrVersionHistoryTests.cs`
- `FullOrgSetup_CreateToMembership_ShouldSucceed.cs`
- `VerifySeedOrganization_ThenListDepartmentsAndMemberships.cs`
- `FullPatientJourney_RegisterToDeactivateToReRegister.cs`
- `LoginAllSeedUsers_ShouldSucceed.cs`
- `RegisterDuplicate_ThenLoginOriginal_ShouldWork.cs`
- `InvoiceLifecycle_CreateToPayCash_ShouldSucceed.cs`
- `InvoiceCancel_ShouldUpdateStatus.cs`
- `Checkout_WithFakeInvoice_ShouldReturnErrorWithMessage.cs`

## Prerequisites

- .NET 8 SDK
- Docker + Docker Compose
- All services running (recommended with dev compose)

## Run

```bash
docker compose -f docker-compose.dev.yml up -d
dotnet test src/DBH.UnitTest/DBH.UnitTest.csproj
```

Run only E2E:

```bash
dotnet test src/DBH.UnitTest/DBH.UnitTest.csproj --filter "FullyQualifiedName~DBH.UnitTest.E2E"
```

Run only one flow:

```bash
dotnet test src/DBH.UnitTest/DBH.UnitTest.csproj --filter "FullyQualifiedName~FullPatientJourney_RegisterToDeactivateToReRegister"
```

Quiet output:

```powershell
dotnet test src/DBH.UnitTest/DBH.UnitTest.csproj `
    --logger "console;verbosity=quiet" `
    -v minimal
```

With TRX artifact:

```powershell
dotnet test src/DBH.UnitTest/DBH.UnitTest.csproj `
    --logger "console;verbosity=quiet" `
    --logger "trx;LogFileName=DBH.UnitTest.trx" `
    --results-directory .\TestResults `
    -v minimal
```
```
