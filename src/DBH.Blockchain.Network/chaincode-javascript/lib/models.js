/*
 * EHR Chaincode Data Models
 *
 * Defines the structure of all asset types stored on the Hyperledger Fabric ledger.
 * These are reference definitions — JavaScript doesn't enforce schemas,
 * but this documents the expected shape of each object.
 */

'use strict';

// =============================================================================
//  EHR Record — Pointer to an IPFS-stored medical record
// =============================================================================
/**
 * @typedef {Object} EHR
 * @property {string} docType        - Always "ehr"
 * @property {string} recordDid      - Unique record ID (UUID/GUID)
 * @property {string} patientDid     - Patient identifier (UUID/GUID)
 * @property {string} creatorDid     - Creator (doctor/staff) identifier (UUID/GUID)
 * @property {string} creatorOrg     - MSP ID of the creating organization (e.g., "Hospital1MSP")
 *                                     Auto-captured from the caller's certificate
 * @property {string} ipfsCid        - IPFS Content Identifier pointing to the actual medical data
 * @property {string} recordType     - Type of record (e.g., "Lab Report", "Prescription", "Radiology Image")
 * @property {string} timestamp      - ISO 8601 timestamp of creation/last update
 */
const EHR_EXAMPLE = {
    docType: 'ehr',
    recordDid: 'a1b2c3d4-e5f6-7890-abcd-ef1234567890',
    patientDid: 'p1a2b3c4-d5e6-7890-abcd-111111111111',
    creatorDid: 'd1a2b3c4-0001-4000-a000-000000000001',
    creatorOrg: 'Hospital1MSP',
    ipfsCid: 'QmExampleCid1abcdefghijklmnopqrstuvwxyz123456',
    recordType: 'Lab Report',
    timestamp: '2026-01-15T10:30:00Z',
};


// =============================================================================
//  Consent — Per-user permission for EHR access
// =============================================================================
/**
 * @typedef {Object} Consent
 * @property {string}  docType          - Always "consent"
 * @property {string}  consentId        - Unique consent ID (UUID/GUID)
 * @property {string}  patientDid       - Patient who grants the consent (UUID/GUID)
 * @property {string}  targetRecordDid  - The specific EHR record this consent applies to
 * @property {string}  granteeDid       - The specific user (doctor/staff) who receives access (UUID/GUID)
 * @property {string}  granteeOrg       - MSP ID of the grantee's organization (e.g., "ClinicMSP")
 * @property {string}  permission       - Permission level: "READ", "WRITE", or "DELETE"
 * @property {string|null} expiresAt    - ISO 8601 expiry timestamp, or null/empty for no expiry
 * @property {string}  status           - "ACTIVE" or "REVOKED"
 * @property {string}  grantedAt        - ISO 8601 timestamp when consent was created
 * @property {string|null} revokedAt    - ISO 8601 timestamp when revoked, null if still active
 */
const CONSENT_EXAMPLE = {
    docType: 'consent',
    consentId: 'cc000001-aaaa-4000-b000-000000000001',
    patientDid: 'p1a2b3c4-d5e6-7890-abcd-111111111111',
    targetRecordDid: 'a1b2c3d4-e5f6-7890-abcd-ef1234567890',
    granteeDid: 'd5f6a7b8-0005-4000-a000-000000000005',
    granteeOrg: 'ClinicMSP',
    permission: 'READ',
    expiresAt: '2026-12-31T23:59:59Z',
    status: 'ACTIVE',
    grantedAt: '2026-01-15T10:35:00Z',
    revokedAt: null,
};


// =============================================================================
//  Access Log — Immutable on-chain audit trail for record reads
// =============================================================================
/**
 * @typedef {Object} AccessLog
 * @property {string} docType          - Always "accessLog"
 * @property {string} logId            - Unique log ID (format: "LOG_{recordDid}_{timestamp}")
 * @property {string} targetRecordDid  - The EHR record that was accessed
 * @property {string} accessorDid      - DID of the user who accessed the record
 * @property {string} accessorOrg      - MSP ID of the accessor's organization
 * @property {string} action           - What was done (e.g., "READ", "EMERGENCY_READ")
 * @property {string} reason           - Free-text reason provided by the accessor
 * @property {string} timestamp        - ISO 8601 timestamp of access
 */
const ACCESS_LOG_EXAMPLE = {
    docType: 'accessLog',
    logId: 'LOG_a1b2c3d4-e5f6-7890-abcd-ef1234567890_1709283023000',
    targetRecordDid: 'a1b2c3d4-e5f6-7890-abcd-ef1234567890',
    accessorDid: 'd5f6a7b8-0005-4000-a000-000000000005',
    accessorOrg: 'ClinicMSP',
    action: 'READ',
    reason: 'Patient referral review',
    timestamp: '2026-03-01T07:30:23Z',
};


// =============================================================================
//  Emergency Access — Immutable on-chain record of consent bypass
// =============================================================================
/**
 * @typedef {Object} EmergencyAccess
 * @property {string} docType          - Always "emergencyAccess"
 * @property {string} logId            - Unique log ID (format: "EMRG_{recordDid}_{timestamp}")
 * @property {string} targetRecordDid  - The EHR record that was accessed
 * @property {string} accessorDid      - DID of the doctor who used emergency access
 * @property {string} accessorOrg      - MSP ID of the accessor's organization
 * @property {string} reason           - Mandatory reason for emergency access
 * @property {string} timestamp        - ISO 8601 timestamp of access
 */
const EMERGENCY_ACCESS_EXAMPLE = {
    docType: 'emergencyAccess',
    logId: 'EMRG_a1b2c3d4-e5f6-7890-abcd-ef1234567890_1709283023000',
    targetRecordDid: 'a1b2c3d4-e5f6-7890-abcd-ef1234567890',
    accessorDid: 'd5f6a7b8-0005-4000-a000-000000000005',
    accessorOrg: 'ClinicMSP',
    reason: 'Patient unconscious, need allergy information',
    timestamp: '2026-03-01T07:30:23Z',
};


// =============================================================================
//  Constants
// =============================================================================
const DOC_TYPES = {
    EHR: 'ehr',
    CONSENT: 'consent',
    ACCESS_LOG: 'accessLog',
    EMERGENCY_ACCESS: 'emergencyAccess',
};

const CONSENT_STATUS = {
    ACTIVE: 'ACTIVE',
    REVOKED: 'REVOKED',
};

const PERMISSIONS = {
    READ: 'READ',
    WRITE: 'WRITE',
    DELETE: 'DELETE',
};

const ACCESS_ACTIONS = {
    READ: 'READ',
    EMERGENCY_READ: 'EMERGENCY_READ',
};

module.exports = {
    DOC_TYPES,
    CONSENT_STATUS,
    PERMISSIONS,
    ACCESS_ACTIONS,
};
