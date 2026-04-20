# DBH.UnitTest - API Integration Tests

API integration tests for all DBH-EHR-System microservices using **xUnit** and **HttpClient**.

## Project Structure

```
api/
├── ApiTestBase.cs              # Shared HttpClient setup + authentication helpers
├── ApiEndpoints.cs             # Centralized API URL constants
├── TestSeedData.cs             # Known DB seed IDs and credentials
├── AuthServiceTests.cs         # Auth, Doctors, Patients, Staff (18 tests)
├── AppointmentServiceTests.cs  # Appointments, Encounters (13 tests)
├── AuditServiceTests.cs        # Audit logs (8 tests)
├── ConsentServiceTests.cs      # Consents, Access Requests (12 tests)
├── EhrServiceTests.cs          # EHR Records, Versions, Files, IPFS (15 tests)
├── NotificationServiceTests.cs # Notifications, Device Tokens, Preferences (12 tests)
├── OrganizationServiceTests.cs # Organizations, Departments, Memberships (16 tests)
└── PaymentServiceTests.cs      # Invoices, Payments, Webhook (10 tests)
```

## Prerequisites

- **.NET 8 SDK**
- All microservices running locally (via Docker Compose)

## Configuration

Service URLs are in `appsettings.Test.json`:

| Service       | Port  |
|---------------|-------|
| Gateway       | 5000  |
| Auth          | 5101  |
| Organization  | 5002  |
| EHR           | 5003  |
| Consent       | 5004  |
| Audit         | 5005  |
| Notification  | 5006  |
| Appointment   | 5007  |
| Payment       | 5008  |

## Seed Data

Tests validate against seeded database records defined in `TestSeedData.cs`:

| User         | Email                 | Password          | Role         |
|--------------|-----------------------|-------------------|--------------|
| Admin        | admin@dbh.com         | admin123          | Admin        |
| Doctor       | doctor@dbh.com        | doctor123         | Doctor       |
| Pharmacist   | pharmacist@dbh.com    | pharma123         | Pharmacist   |
| Nurse        | nurse@dbh.com         | nurse123          | Nurse        |
| Patient      | patient@dbh.com       | patient123        | Patient      |
| Receptionist | receptionist@dbh.com  | receptionist123   | Receptionist |

**Organizations:** Hospital A, Hospital B, Clinic (with departments and memberships).

## Running Tests

```bash
# Start services
docker compose -f docker-compose.dev.yml up -d

# Run all tests
dotnet test src/DBH.UnitTest/DBH.UnitTest.csproj

# Run a specific service
dotnet test src/DBH.UnitTest/DBH.UnitTest.csproj --filter "FullyQualifiedName~AuthServiceTests"

# Run with detailed output
dotnet test src/DBH.UnitTest/DBH.UnitTest.csproj -v detailed
```

```powershell
# Run tests with plain HTTP trace output (no diagnostic noise)
dotnet test src/DBH.UnitTest/DBH.UnitTest.csproj `
	--logger "console;verbosity=quiet" `
	-v minimal
```

Optional (only when you need artifacts):
```powershell
dotnet test src/DBH.UnitTest/DBH.UnitTest.csproj `
	--logger "console;verbosity=quiet" `
	--logger "trx;LogFileName=DBH.UnitTest.trx" `
	--results-directory .\TestResults `
	-v minimal
```

## Key Design Patterns

- **`TestSeedData.cs`** — Tests use real seed data IDs to verify DB content matches
- **Response validation** — Checks `success` flag, `message` content, and `data` fields (not just HTTP status codes)
- **Not-found testing** — Fake GUIDs verify proper 404 responses with error messages
