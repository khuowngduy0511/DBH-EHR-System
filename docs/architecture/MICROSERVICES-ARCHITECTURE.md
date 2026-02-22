# DBH-EHR System - Microservices Architecture

## Overview

Hệ thống DBH-EHR được thiết kế theo kiến trúc **Microservices** với các bounded contexts rõ ràng, tuân thủ nguyên tắc:
- **Single Responsibility**: Mỗi service chịu trách nhiệm một domain cụ thể
- **Loose Coupling**: Các services giao tiếp qua API Gateway và Message Queue
- **High Cohesion**: Các chức năng liên quan được nhóm trong cùng service
- **Domain-Driven Design**: Tổ chức theo business domains

## System Context (C4 Level 1)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              EXTERNAL USERS                                  │
├──────────────┬──────────────┬──────────────┬──────────────────────────────────┤
│   Patient    │   Doctor/    │    Admin     │      External Systems           │
│  Mobile App  │  Clinic App  │   Web Portal │   (Other Hospitals, Labs)       │
│  (Flutter)   │  (Flutter)   │  (React)     │                                 │
└──────┬───────┴──────┬───────┴──────┬───────┴──────────────┬──────────────────┘
       │              │              │                      │
       └──────────────┴──────────────┴──────────────────────┘
                                    │
                    ┌───────────────▼───────────────┐
                    │       API GATEWAY             │
                    │   (YARP / Ocelot / Kong)      │
                    │   - Rate Limiting             │
                    │   - Authentication            │
                    │   - Load Balancing            │
                    └───────────────┬───────────────┘
                                    │
       ┌────────────────────────────┼────────────────────────────┐
       │                            │                            │
       ▼                            ▼                            ▼
┌──────────────┐           ┌──────────────┐           ┌──────────────┐
│    AUTH      │           │     EHR      │           │   CONSENT    │
│   SERVICE    │           │   SERVICE    │           │   SERVICE    │
└──────────────┘           └──────────────┘           └──────────────┘
       │                            │                            │
       └────────────────────────────┼────────────────────────────┘
                                    │
       ┌────────────────────────────┼────────────────────────────┐
       │                            │                            │
       ▼                            ▼                            ▼
┌──────────────┐           ┌──────────────┐           ┌──────────────┐
│    AUDIT     │           │ ORGANIZATION │           │ NOTIFICATION │
│   SERVICE    │           │   SERVICE    │           │   SERVICE    │
└──────────────┘           └──────────────┘           └──────────────┘
```

## Microservices Definition

### 1. DBH.Gateway (API Gateway)
**Port**: 5000

**Responsibilities**:
- Request routing to backend services
- Authentication/Authorization validation
- Rate limiting & throttling
- Request/Response logging
- Load balancing
- SSL termination
- API versioning

**Technology**: YARP (Yet Another Reverse Proxy) / Ocelot

---

### 2. DBH.Auth.Service (Identity & Access Management)
**Port**: 5001

**Domain**: User Identity, Authentication, Authorization

**Responsibilities**:
- User registration & verification
- Login/Logout (JWT + Refresh Token)
- Multi-Factor Authentication (MFA)
- DID (Decentralized Identifier) management
- Role-Based Access Control (RBAC)
- OAuth2 / OpenID Connect
- Password management
- Session management

**Database**: PostgreSQL (`dbh_identity`)

**Key Entities**:
- User
- UserDid
- Role
- UserRole
- RefreshToken

**APIs**:
```
POST   /api/v1/auth/register          # User registration
POST   /api/v1/auth/login             # Login
POST   /api/v1/auth/refresh           # Refresh token
POST   /api/v1/auth/logout            # Logout
POST   /api/v1/auth/mfa/enable        # Enable MFA
POST   /api/v1/auth/mfa/verify        # Verify MFA code
POST   /api/v1/auth/password/change   # Change password
POST   /api/v1/auth/password/reset    # Reset password
GET    /api/v1/auth/me                # Get current user
PUT    /api/v1/auth/profile           # Update profile

# DID Management
POST   /api/v1/did/generate           # Generate new DID
GET    /api/v1/did/{did}              # Resolve DID
POST   /api/v1/did/verify             # Verify DID
POST   /api/v1/did/rotate-keys        # Rotate DID keys
```

---

### 3. DBH.Organization.Service (Organization Management)
**Port**: 5002

**Domain**: Hospitals, Clinics, Departments, Staff Management

**Responsibilities**:
- Hospital/Clinic registration & verification
- Department management
- Staff membership management
- Organization hierarchy
- License verification
- Blockchain MSP enrollment

**Database**: PostgreSQL (`dbh_organization`)

**Key Entities**:
- Organization
- OrganizationDepartment
- OrganizationMembership

**APIs**:
```
# Organizations
POST   /api/v1/organizations                    # Create organization
GET    /api/v1/organizations                    # List organizations
GET    /api/v1/organizations/{id}               # Get organization
PUT    /api/v1/organizations/{id}               # Update organization
POST   /api/v1/organizations/{id}/verify        # Verify organization (Admin)
DELETE /api/v1/organizations/{id}               # Deactivate organization

# Departments
POST   /api/v1/organizations/{id}/departments   # Create department
GET    /api/v1/organizations/{id}/departments   # List departments
PUT    /api/v1/departments/{id}                 # Update department

# Memberships
POST   /api/v1/organizations/{id}/members       # Add member
GET    /api/v1/organizations/{id}/members       # List members
PUT    /api/v1/memberships/{id}                 # Update membership
DELETE /api/v1/memberships/{id}                 # Remove member
```

---

### 4. DBH.EHR.Service (Electronic Health Records)
**Port**: 5003

**Domain**: EHR Records, FHIR Documents, Medical Files

**Responsibilities**:
- Create/Update EHR records
- FHIR-compliant document storage
- Encrypted file upload/download (S3)
- Version management
- Hash generation for blockchain
- Integrity verification

**Database**: 
- PostgreSQL (`dbh_ehr`) - Metadata
- MongoDB (`dbh_fhir`) - FHIR Documents
- S3 - Encrypted files

**Key Entities**:
- EhrRecord
- EhrVersion
- EhrFile
- FHIR Documents (Observation, DiagnosticReport, MedicationRequest, etc.)

**APIs**:
```
# EHR Records
POST   /api/v1/ehr                              # Create EHR record
GET    /api/v1/ehr                              # Query EHR records
GET    /api/v1/ehr/{id}                         # Get EHR detail
PUT    /api/v1/ehr/{id}                         # Update EHR record
GET    /api/v1/ehr/{id}/versions                # Get version history
GET    /api/v1/ehr/{id}/versions/{version}      # Get specific version

# Files
POST   /api/v1/ehr/{id}/files                   # Upload file
GET    /api/v1/ehr/{id}/files                   # List files
GET    /api/v1/ehr/files/{fileId}/download      # Download file
DELETE /api/v1/ehr/files/{fileId}               # Delete file

# FHIR Resources
GET    /api/v1/fhir/Patient/{patientDid}        # Get patient FHIR resources
GET    /api/v1/fhir/Observation/{id}            # Get observation
GET    /api/v1/fhir/DiagnosticReport/{id}       # Get diagnostic report
GET    /api/v1/fhir/MedicationRequest/{id}      # Get medication request

# Integrity
POST   /api/v1/ehr/{id}/verify                  # Verify EHR integrity
```

---

### 5. DBH.Consent.Service (Consent Management - Blockchain)
**Port**: 5004

**Domain**: Patient Consent, Access Permissions

**Responsibilities**:
- Grant/Revoke consent (write to blockchain)
- Check consent validity (read from blockchain)
- Consent history
- Emergency access management
- Consent conditions (time-based, purpose-based)

**Database**: 
- PostgreSQL (`dbh_consent`) - Cache only
- Hyperledger Fabric (`consent-channel`) - Source of truth

**Key Entities**:
- ConsentCache (local cache, NOT source of truth)
- ConsentChaincode (on blockchain)

**APIs**:
```
# Consent Management
POST   /api/v1/consents                         # Grant consent
GET    /api/v1/consents                         # List my consents
GET    /api/v1/consents/{id}                    # Get consent detail
POST   /api/v1/consents/{id}/revoke             # Revoke consent
PUT    /api/v1/consents/{id}/extend             # Extend consent validity

# Consent Verification
POST   /api/v1/consents/check                   # Check if requester has consent
GET    /api/v1/consents/patient/{patientDid}    # Get all consents for patient
GET    /api/v1/consents/grantee/{granteeDid}    # Get consents granted to user

# Access Requests
POST   /api/v1/access-requests                  # Request access to patient EHR
GET    /api/v1/access-requests                  # List access requests
PUT    /api/v1/access-requests/{id}/approve     # Approve request
PUT    /api/v1/access-requests/{id}/deny        # Deny request
```

---

### 6. DBH.Audit.Service (Audit & Compliance - Blockchain)
**Port**: 5005

**Domain**: Audit Logging, Compliance, Traceability

**Responsibilities**:
- Log all EHR access events (write to blockchain)
- Query audit history
- Compliance reporting
- Anomaly detection
- Export audit reports

**Database**: 
- PostgreSQL (`dbh_audit`) - Cache for fast queries
- Hyperledger Fabric (`audit-channel`) - Immutable source of truth

**Key Entities**:
- AuditLogCache (local cache)
- AuditLogChaincode (on blockchain)

**APIs**:
```
# Audit Logs
GET    /api/v1/audit/patient/{patientDid}       # Get patient's audit trail
GET    /api/v1/audit/user/{userDid}             # Get user's access history
GET    /api/v1/audit/ehr/{ehrId}                # Get EHR access history
GET    /api/v1/audit/organization/{orgId}       # Get organization audit

# Reports
GET    /api/v1/audit/reports/summary            # Get audit summary
GET    /api/v1/audit/reports/compliance         # Compliance report
POST   /api/v1/audit/reports/export             # Export audit report

# Internal (called by other services)
POST   /api/v1/audit/log                        # Log access event (internal)
```

---

### 7. DBH.Notification.Service (Notifications)
**Port**: 5006

**Domain**: Notifications, Alerts, Communication

**Responsibilities**:
- Push notifications (FCM/APNs)
- Email notifications
- SMS notifications
- In-app notifications
- Notification preferences

**Database**: PostgreSQL (`dbh_notification`)

**Key Entities**:
- Notification
- NotificationTemplate
- NotificationPreference
- DeviceToken

**APIs**:
```
# Notifications
GET    /api/v1/notifications                    # Get my notifications
PUT    /api/v1/notifications/{id}/read          # Mark as read
PUT    /api/v1/notifications/read-all           # Mark all as read
DELETE /api/v1/notifications/{id}               # Delete notification

# Preferences
GET    /api/v1/notifications/preferences        # Get preferences
PUT    /api/v1/notifications/preferences        # Update preferences

# Device Tokens
POST   /api/v1/notifications/devices            # Register device
DELETE /api/v1/notifications/devices/{id}       # Unregister device

# Internal (called by other services)
POST   /api/v1/notifications/send               # Send notification (internal)
```

---

## Inter-Service Communication

### Synchronous (HTTP/gRPC)
- Gateway → All Services (REST API)
- EHR Service → Consent Service (check consent before access)
- EHR Service → Audit Service (log access)
- Auth Service → Organization Service (validate membership)

### Asynchronous (Message Queue - RabbitMQ/Azure Service Bus)

```
┌─────────────────────────────────────────────────────────────────┐
│                     MESSAGE BROKER                              │
│                 (RabbitMQ / Azure Service Bus)                  │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  Exchanges/Topics:                                              │
│  ├── user.events                                                │
│  │   ├── user.registered                                        │
│  │   ├── user.verified                                          │
│  │   └── user.deactivated                                       │
│  │                                                              │
│  ├── consent.events                                             │
│  │   ├── consent.granted                                        │
│  │   ├── consent.revoked                                        │
│  │   └── consent.request.created                                │
│  │                                                              │
│  ├── ehr.events                                                 │
│  │   ├── ehr.created                                            │
│  │   ├── ehr.updated                                            │
│  │   ├── ehr.accessed                                           │
│  │   └── ehr.shared                                             │
│  │                                                              │
│  └── notification.commands                                      │
│      └── notification.send                                      │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

**Event Flow Examples**:

1. **Patient Grants Consent**:
   ```
   Consent Service → [consent.granted] → Notification Service
                                       → Audit Service
   ```

2. **Doctor Accesses EHR**:
   ```
   EHR Service → [ehr.accessed] → Audit Service
                                → Notification Service (notify patient)
   ```

3. **New Access Request**:
   ```
   Consent Service → [consent.request.created] → Notification Service
   ```

---

## Database Architecture

```
┌──────────────────────────────────────────────────────────────────────────┐
│                          DATA LAYER                                       │
├──────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐          │
│  │   PostgreSQL    │  │    MongoDB      │  │   AWS S3        │          │
│  │  (Relational)   │  │   (Document)    │  │ (Object Store)  │          │
│  ├─────────────────┤  ├─────────────────┤  ├─────────────────┤          │
│  │ • dbh_identity  │  │ • dbh_fhir      │  │ • Encrypted     │          │
│  │ • dbh_ehr       │  │   - observations│  │   EHR files     │          │
│  │ • dbh_org       │  │   - reports     │  │ • Medical       │          │
│  │ • dbh_consent   │  │   - medications │  │   images        │          │
│  │ • dbh_audit     │  │   - encounters  │  │ • Lab results   │          │
│  │ • dbh_notif     │  │                 │  │                 │          │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘          │
│                                                                          │
│  ┌─────────────────────────────────────────────────────────────┐        │
│  │              HYPERLEDGER FABRIC BLOCKCHAIN                   │        │
│  ├─────────────────────────────────────────────────────────────┤        │
│  │                                                              │        │
│  │  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────┐  │        │
│  │  │ consent-channel │  │  audit-channel  │  │ ehr-hash-   │  │        │
│  │  │                 │  │                 │  │ channel     │  │        │
│  │  │ • ConsentRecord │  │ • AuditLog      │  │ • EhrHash   │  │        │
│  │  │ • Permissions   │  │ • AccessEvent   │  │ • Integrity │  │        │
│  │  └─────────────────┘  └─────────────────┘  └─────────────┘  │        │
│  │                                                              │        │
│  │  Organizations:                                              │        │
│  │  • Org1MSP (Hospital A)                                      │        │
│  │  • Org2MSP (Hospital B)                                      │        │
│  │  • Org3MSP (Clinic C)                                        │        │
│  │                                                              │        │
│  └─────────────────────────────────────────────────────────────┘        │
│                                                                          │
└──────────────────────────────────────────────────────────────────────────┘
```

---

## Deployment Architecture (AWS/Azure)

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         CLOUD INFRASTRUCTURE                             │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │                     KUBERNETES CLUSTER (EKS/AKS)                 │   │
│  │                                                                   │   │
│  │  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ │   │
│  │  │   Gateway   │ │    Auth     │ │    EHR      │ │   Consent   │ │   │
│  │  │   Service   │ │   Service   │ │   Service   │ │   Service   │ │   │
│  │  │  (3 pods)   │ │  (3 pods)   │ │  (3 pods)   │ │  (2 pods)   │ │   │
│  │  └─────────────┘ └─────────────┘ └─────────────┘ └─────────────┘ │   │
│  │                                                                   │   │
│  │  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐                 │   │
│  │  │    Audit    │ │Organization │ │Notification │                 │   │
│  │  │   Service   │ │   Service   │ │   Service   │                 │   │
│  │  │  (2 pods)   │ │  (2 pods)   │ │  (2 pods)   │                 │   │
│  │  └─────────────┘ └─────────────┘ └─────────────┘                 │   │
│  │                                                                   │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  ┌─────────────────────┐  ┌─────────────────────┐                      │
│  │  Application LB     │  │   CloudFront/CDN    │                      │
│  │  (ALB/Azure LB)     │  │   (Static Assets)   │                      │
│  └─────────────────────┘  └─────────────────────┘                      │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │                    HYPERLEDGER FABRIC NETWORK                    │   │
│  │                                                                   │   │
│  │  ┌───────────────┐  ┌───────────────┐  ┌───────────────┐         │   │
│  │  │  Org1 Peers   │  │  Org2 Peers   │  │  Orderer      │         │   │
│  │  │  (Hospital A) │  │  (Hospital B) │  │  Nodes        │         │   │
│  │  └───────────────┘  └───────────────┘  └───────────────┘         │   │
│  │                                                                   │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Project Structure

```
DBH-EHR-Backend/
├── src/
│   ├── DBH.Gateway/                    # API Gateway
│   ├── DBH.Auth.Service/               # Authentication & Authorization
│   ├── DBH.Organization.Service/       # Organization Management (NEW)
│   ├── DBH.EHR.Service/                # EHR Records & FHIR
│   ├── DBH.Consent.Service/            # Consent (Blockchain) (NEW)
│   ├── DBH.Audit.Service/              # Audit Logs (Blockchain) (NEW)
│   ├── DBH.Notification.Service/       # Notifications (NEW)
│   │
│   ├── DBH.Shared.Domain/              # Shared Domain Models
│   ├── DBH.Shared.Infrastructure/      # Shared Infrastructure
│   ├── DBH.Shared.Contracts/           # Shared DTOs/Events/Interfaces
│   └── DBH.Shared.Messaging/           # Message Queue Contracts (NEW)
│
├── blockchain/
│   ├── network/                        # Fabric network config
│   ├── chaincode/
│   │   ├── consent-chaincode/          # Consent management chaincode
│   │   ├── audit-chaincode/            # Audit logging chaincode
│   │   └── ehr-hash-chaincode/         # EHR hash storage chaincode
│   └── scripts/                        # Network setup scripts
│
├── tests/
│   ├── DBH.Auth.Service.Tests/
│   ├── DBH.EHR.Service.Tests/
│   ├── DBH.Consent.Service.Tests/
│   ├── DBH.Integration.Tests/
│   └── DBH.E2E.Tests/
│
├── docs/
│   ├── architecture/
│   ├── api/
│   └── deployment/
│
├── deploy/
│   ├── docker/
│   ├── kubernetes/
│   └── terraform/
│
├── docker-compose.yml
├── docker-compose.override.yml
└── DBH.EHR.System.sln
```

---

## Technology Stack Summary

| Component | Technology |
|-----------|------------|
| API Gateway | YARP / Ocelot |
| Backend Services | .NET 8 Web API |
| Authentication | JWT + OAuth2 + DID |
| Relational DB | PostgreSQL 15 |
| Document DB | MongoDB 7.0 |
| Object Storage | AWS S3 / Azure Blob |
| Blockchain | Hyperledger Fabric 2.5 |
| Message Queue | RabbitMQ / Azure Service Bus |
| Caching | Redis |
| Container | Docker + Kubernetes (EKS/AKS) |
| CI/CD | GitHub Actions / Azure DevOps |
| Monitoring | Prometheus + Grafana |
| Logging | ELK Stack / Azure Monitor |
| API Documentation | Swagger / OpenAPI 3.0 |

---

## Security Considerations

1. **Authentication**: JWT with short-lived access tokens + refresh tokens
2. **Authorization**: RBAC with DID-based identity
3. **Encryption**: 
   - In-transit: TLS 1.3
   - At-rest: AES-256-GCM (KMS managed keys)
4. **Data Privacy**: HIPAA & GDPR compliant
5. **Audit**: Immutable blockchain audit trail
6. **MFA**: TOTP / SMS / Email verification

---

## Next Steps

1. ✅ Create shared domain models
2. ✅ Create shared infrastructure
3. ⬜ Create Organization Service
4. ⬜ Create Consent Service
5. ⬜ Create Audit Service
6. ⬜ Create Notification Service
7. ⬜ Update Gateway with routing
8. ⬜ Add Message Queue integration
9. ⬜ Setup Hyperledger Fabric network
10. ⬜ Create integration tests
