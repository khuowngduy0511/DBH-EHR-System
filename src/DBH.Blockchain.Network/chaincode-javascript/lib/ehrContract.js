/*
 * SPDX-License-Identifier: Apache-2.0
 *
 * EHR Smart Contract — Electronic Health Record management on Hyperledger Fabric
 *
 * Main contract that composes all function categories:
 *   - EHR Functions:              CRUD + query for medical record pointers
 *   - Consent Functions:          Per-user consent with READ/WRITE/DELETE permissions
 *   - Access Log Functions:       On-chain audit trail for record reads
 *   - Emergency Access Functions: Consent bypass with immutable on-chain proof
 *
 * Organizations: Hospital1, Hospital2, Clinic
 * All IDs (recordDid, patientDid, creatorDid, consentId, logId) use UUID/GUID format.
 */

'use strict';

const stringify = require('json-stringify-deterministic');
const sortKeysRecursive = require('sort-keys-recursive');
const { Contract } = require('fabric-contract-api');
const { DOC_TYPES, CONSENT_STATUS, PERMISSIONS } = require('./models');

// Import separated function categories
const EhrFunctions = require('./ehrFunctions');
const ConsentFunctions = require('./consentFunctions');
const AccessLogFunctions = require('./accessLogFunctions');
const EmergencyAccessFunctions = require('./emergencyAccessFunctions');

class EHRContract extends Contract {

    // =========================================================================
    //  InitLedger - Seeds the ledger with sample data
    // =========================================================================
    async InitLedger(ctx) {
        const records = [
            {
                docType: DOC_TYPES.EHR,
                recordDid: 'a1b2c3d4-e5f6-7890-abcd-ef1234567890',
                patientDid: 'p1a2b3c4-d5e6-7890-abcd-111111111111',
                creatorDid: 'd1a2b3c4-0001-4000-a000-000000000001',
                creatorOrg: 'Hospital1MSP',
                ipfsCid: 'QmExampleCid1abcdefghijklmnopqrstuvwxyz123456',
                recordType: 'Lab Report',
                timestamp: '2026-01-15T10:30:00Z',
            },
            {
                docType: DOC_TYPES.EHR,
                recordDid: 'b2c3d4e5-f6a7-8901-bcde-f12345678901',
                patientDid: 'p2b3c4d5-e6f7-8901-bcde-222222222222',
                creatorDid: 'd2b3c4d5-0002-4000-a000-000000000002',
                creatorOrg: 'Hospital1MSP',
                ipfsCid: 'QmExampleCid2abcdefghijklmnopqrstuvwxyz789012',
                recordType: 'Prescription',
                timestamp: '2026-01-16T09:00:00Z',
            },
            {
                docType: DOC_TYPES.EHR,
                recordDid: 'c3d4e5f6-a7b8-9012-cdef-123456789012',
                patientDid: 'p3c4d5e6-f7a8-9012-cdef-333333333333',
                creatorDid: 'd3c4d5e6-0003-4000-a000-000000000003',
                creatorOrg: 'Hospital2MSP',
                ipfsCid: 'QmExampleCid3abcdefghijklmnopqrstuvwxyz345678',
                recordType: 'Radiology Image',
                timestamp: '2026-01-17T14:15:00Z',
            },
        ];

        // Seed sample consents (new per-user model)
        const consents = [
            {
                docType: DOC_TYPES.CONSENT,
                consentId: 'cc000001-aaaa-4000-b000-000000000001',
                patientDid: 'p1a2b3c4-d5e6-7890-abcd-111111111111',
                targetRecordDid: 'a1b2c3d4-e5f6-7890-abcd-ef1234567890',
                granteeDid: 'd3c4d5e6-0003-4000-a000-000000000003',
                granteeOrg: 'Hospital2MSP',
                permission: PERMISSIONS.READ,
                expiresAt: '2027-01-15T10:35:00Z',
                status: CONSENT_STATUS.ACTIVE,
                grantedAt: '2026-01-15T10:35:00Z',
                revokedAt: null,
            },
            {
                docType: DOC_TYPES.CONSENT,
                consentId: 'cc000002-bbbb-4000-b000-000000000002',
                patientDid: 'p1a2b3c4-d5e6-7890-abcd-111111111111',
                targetRecordDid: 'a1b2c3d4-e5f6-7890-abcd-ef1234567890',
                granteeDid: 'd4e5f6a7-0004-4000-a000-000000000004',
                granteeOrg: 'ClinicMSP',
                permission: PERMISSIONS.READ,
                expiresAt: null,
                status: CONSENT_STATUS.ACTIVE,
                grantedAt: '2026-01-18T12:00:00Z',
                revokedAt: null,
            },
        ];

        for (const record of records) {
            await ctx.stub.putState(record.recordDid, Buffer.from(stringify(sortKeysRecursive(record))));
        }

        for (const consent of consents) {
            await ctx.stub.putState(consent.consentId, Buffer.from(stringify(sortKeysRecursive(consent))));
        }
    }

    // =========================================================================
    //                         INTERNAL HELPER FUNCTIONS
    // =========================================================================

    async _assetExists(ctx, key) {
        const data = await ctx.stub.getState(key);
        return data && data.length > 0;
    }

    /**
     * Check if a user has the required consent for a specific record.
     * Checks: granteeDid match, permission level, expiry date, active status.
     */
    async _checkConsent(ctx, targetRecordDid, accessorDid, requiredPermission) {
        const queryString = JSON.stringify({
            selector: {
                docType: DOC_TYPES.CONSENT,
                targetRecordDid,
                granteeDid: accessorDid,
                status: CONSENT_STATUS.ACTIVE,
            },
        });

        const resultsIterator = await ctx.stub.getQueryResult(queryString);
        let result = await resultsIterator.next();
        const now = new Date();

        while (!result.done) {
            const consent = JSON.parse(Buffer.from(result.value.value.toString()).toString('utf8'));

            // Check expiry
            if (consent.expiresAt) {
                const expiryDate = new Date(consent.expiresAt);
                if (expiryDate < now) {
                    // Consent expired — skip (optionally could auto-revoke here)
                    result = await resultsIterator.next();
                    continue;
                }
            }

            // Check permission matches
            if (consent.permission === requiredPermission) {
                return true;
            }

            result = await resultsIterator.next();
        }
        return false;
    }

    async _queryByDocType(ctx, docType) {
        const allResults = [];
        const iterator = await ctx.stub.getStateByRange('', '');
        let result = await iterator.next();
        while (!result.done) {
            const strValue = Buffer.from(result.value.value.toString()).toString('utf8');
            try {
                const record = JSON.parse(strValue);
                if (record.docType === docType) {
                    allResults.push(record);
                }
            } catch (err) {
                console.log(err);
            }
            result = await iterator.next();
        }
        return JSON.stringify(allResults);
    }

    async _getQueryResult(ctx, queryString) {
        const resultsIterator = await ctx.stub.getQueryResult(queryString);
        const allResults = [];
        let result = await resultsIterator.next();
        while (!result.done) {
            const strValue = Buffer.from(result.value.value.toString()).toString('utf8');
            try {
                allResults.push(JSON.parse(strValue));
            } catch (err) {
                console.log(err);
                allResults.push(strValue);
            }
            result = await resultsIterator.next();
        }
        return JSON.stringify(allResults);
    }
}

// =========================================================================
//  Mixin: Copy all category functions into the contract prototype
// =========================================================================
Object.assign(EHRContract.prototype, EhrFunctions);
Object.assign(EHRContract.prototype, ConsentFunctions);
Object.assign(EHRContract.prototype, AccessLogFunctions);
Object.assign(EHRContract.prototype, EmergencyAccessFunctions);

module.exports = EHRContract;
