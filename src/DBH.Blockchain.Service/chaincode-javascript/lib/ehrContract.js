/*
 * SPDX-License-Identifier: Apache-2.0
 *
 * EHR Smart Contract - Electronic Health Record management on Hyperledger Fabric
 *
 * Assets:
 *   - EHR:       Pointer to an IPFS-stored medical record
 *   - Consent:   Permission gate for cross-org record access
 *   - AccessLog: Immutable audit trail for every record read
 *
 * Organizations: Hospital1, Hospital2, Clinic
 *
 * All IDs (recordDid, patientDid, creatorDid, consentId, logId) use UUID/GUID format.
 */

'use strict';

const stringify = require('json-stringify-deterministic');
const sortKeysRecursive = require('sort-keys-recursive');
const { Contract } = require('fabric-contract-api');

class EHRContract extends Contract {

    // =========================================================================
    //  InitLedger - Seeds the ledger with sample EHR records and consents
    // =========================================================================
    async InitLedger(ctx) {
        const records = [
            {
                docType: 'ehr',
                recordDid: 'a1b2c3d4-e5f6-7890-abcd-ef1234567890',
                patientDid: 'p1a2b3c4-d5e6-7890-abcd-111111111111',
                creatorDid: 'd1a2b3c4-0001-4000-a000-000000000001',
                creatorOrg: 'Hospital1MSP',
                ipfsCid: 'QmExampleCid1abcdefghijklmnopqrstuvwxyz123456',
                recordType: 'Lab Report',
                timestamp: '2026-01-15T10:30:00Z',
            },
            {
                docType: 'ehr',
                recordDid: 'b2c3d4e5-f6a7-8901-bcde-f12345678901',
                patientDid: 'p2b3c4d5-e6f7-8901-bcde-222222222222',
                creatorDid: 'd2b3c4d5-0002-4000-a000-000000000002',
                creatorOrg: 'Hospital1MSP',
                ipfsCid: 'QmExampleCid2abcdefghijklmnopqrstuvwxyz789012',
                recordType: 'Prescription',
                timestamp: '2026-01-16T09:00:00Z',
            },
            {
                docType: 'ehr',
                recordDid: 'c3d4e5f6-a7b8-9012-cdef-123456789012',
                patientDid: 'p3c4d5e6-f7a8-9012-cdef-333333333333',
                creatorDid: 'd3c4d5e6-0003-4000-a000-000000000003',
                creatorOrg: 'Hospital2MSP',
                ipfsCid: 'QmExampleCid3abcdefghijklmnopqrstuvwxyz345678',
                recordType: 'Radiology Image',
                timestamp: '2026-01-17T14:15:00Z',
            },
            {
                docType: 'ehr',
                recordDid: 'd4e5f6a7-b8c9-0123-defa-234567890123',
                patientDid: 'p1a2b3c4-d5e6-7890-abcd-111111111111',
                creatorDid: 'd4e5f6a7-0004-4000-a000-000000000004',
                creatorOrg: 'ClinicMSP',
                ipfsCid: 'QmExampleCid4abcdefghijklmnopqrstuvwxyz901234',
                recordType: 'Consultation Note',
                timestamp: '2026-01-18T11:45:00Z',
            },
            {
                docType: 'ehr',
                recordDid: 'e5f6a7b8-c9d0-1234-efab-345678901234',
                patientDid: 'p4d5e6f7-a8b9-0123-defa-444444444444',
                creatorDid: 'd5f6a7b8-0005-4000-a000-000000000005',
                creatorOrg: 'Hospital2MSP',
                ipfsCid: 'QmExampleCid5abcdefghijklmnopqrstuvwxyz567890',
                recordType: 'Discharge Summary',
                timestamp: '2026-01-19T16:00:00Z',
            },
            {
                docType: 'ehr',
                recordDid: 'f6a7b8c9-d0e1-2345-fabc-456789012345',
                patientDid: 'p5e6f7a8-b9c0-1234-efab-555555555555',
                creatorDid: 'd6a7b8c9-0006-4000-a000-000000000006',
                creatorOrg: 'ClinicMSP',
                ipfsCid: 'QmExampleCid6abcdefghijklmnopqrstuvwxyz112233',
                recordType: 'Annual Check-up',
                timestamp: '2026-01-20T08:30:00Z',
            },
        ];

        // Seed sample consents
        const consents = [
            {
                docType: 'consent',
                consentId: 'cc000001-aaaa-4000-b000-000000000001',
                patientDid: 'p1a2b3c4-d5e6-7890-abcd-111111111111',
                targetRecordDid: 'a1b2c3d4-e5f6-7890-abcd-ef1234567890',
                grantedToOrgs: ['Hospital1MSP', 'ClinicMSP'],
                status: 'ACTIVE',
                timestamp: '2026-01-15T10:35:00Z',
            },
            {
                docType: 'consent',
                consentId: 'cc000002-bbbb-4000-b000-000000000002',
                patientDid: 'p1a2b3c4-d5e6-7890-abcd-111111111111',
                targetRecordDid: 'd4e5f6a7-b8c9-0123-defa-234567890123',
                grantedToOrgs: ['Hospital1MSP', 'Hospital2MSP', 'ClinicMSP'],
                status: 'ACTIVE',
                timestamp: '2026-01-18T12:00:00Z',
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
    //                         EHR RECORD FUNCTIONS
    // =========================================================================

    // CreateEHR - Creates a new EHR pointer on the ledger
    async CreateEHR(ctx, recordDid, patientDid, creatorDid, ipfsCid, recordType) {
        const exists = await this._assetExists(ctx, recordDid);
        if (exists) {
            throw new Error(`The EHR record ${recordDid} already exists`);
        }

        const creatorOrg = ctx.clientIdentity.getMSPID();
        const now = new Date().toISOString();

        const ehr = {
            docType: 'ehr',
            recordDid,
            patientDid,
            creatorDid,
            creatorOrg,
            ipfsCid,
            recordType,
            timestamp: now,
        };

        await ctx.stub.putState(recordDid, Buffer.from(stringify(sortKeysRecursive(ehr))));
        return JSON.stringify(ehr);
    }

    // ReadEHR - Returns the EHR metadata (no consent check)
    async ReadEHR(ctx, recordDid) {
        const ehrJSON = await ctx.stub.getState(recordDid);
        if (!ehrJSON || ehrJSON.length === 0) {
            throw new Error(`The EHR record ${recordDid} does not exist`);
        }
        const ehr = JSON.parse(ehrJSON.toString());
        if (ehr.docType !== 'ehr') {
            throw new Error(`${recordDid} is not an EHR record`);
        }
        return ehrJSON.toString();
    }

    // ReadEHRWithConsent - Access-gated read that checks consent and logs access
    async ReadEHRWithConsent(ctx, recordDid, reason) {
        // 1. Read the EHR record
        const ehrString = await this.ReadEHR(ctx, recordDid);
        const ehr = JSON.parse(ehrString);

        const accessorOrg = ctx.clientIdentity.getMSPID();
        const accessorId = ctx.clientIdentity.getID();

        // 2. If the accessor is the creator org, allow access without consent check
        if (accessorOrg !== ehr.creatorOrg) {
            // 3. Check for active consent
            const hasConsent = await this._checkConsent(ctx, recordDid, accessorOrg);
            if (!hasConsent) {
                throw new Error(`Organization ${accessorOrg} does not have consent to access record ${recordDid}`);
            }
        }

        // 4. Create access log
        const logId = `LOG_${recordDid}_${Date.now()}`;
        const accessLog = {
            docType: 'accessLog',
            logId,
            targetRecordDid: recordDid,
            accessorId,
            accessorOrg,
            action: 'READ_IPFS_CID',
            reason: reason || '',
            timestamp: new Date().toISOString(),
        };

        await ctx.stub.putState(logId, Buffer.from(stringify(sortKeysRecursive(accessLog))));

        return JSON.stringify({
            ehr,
            accessLog,
        });
    }

    // UpdateEHR - Updates the IPFS CID of an existing EHR (only creator org can update)
    async UpdateEHR(ctx, recordDid, newIpfsCid, recordType) {
        const ehrString = await this.ReadEHR(ctx, recordDid);
        const ehr = JSON.parse(ehrString);

        const callerOrg = ctx.clientIdentity.getMSPID();
        if (callerOrg !== ehr.creatorOrg) {
            throw new Error(`Only the creator organization (${ehr.creatorOrg}) can update record ${recordDid}`);
        }

        ehr.ipfsCid = newIpfsCid;
        if (recordType) {
            ehr.recordType = recordType;
        }
        ehr.timestamp = new Date().toISOString();

        await ctx.stub.putState(recordDid, Buffer.from(stringify(sortKeysRecursive(ehr))));
        return JSON.stringify(ehr);
    }

    // DeleteEHR - Deletes an EHR from the ledger (only creator org can delete)
    async DeleteEHR(ctx, recordDid) {
        const ehrString = await this.ReadEHR(ctx, recordDid);
        const ehr = JSON.parse(ehrString);

        const callerOrg = ctx.clientIdentity.getMSPID();
        if (callerOrg !== ehr.creatorOrg) {
            throw new Error(`Only the creator organization (${ehr.creatorOrg}) can delete record ${recordDid}`);
        }

        return ctx.stub.deleteState(recordDid);
    }

    // GetAllEHRs - Returns all EHR records
    async GetAllEHRs(ctx) {
        return await this._queryByDocType(ctx, 'ehr');
    }

    // GetEHRsByPatient - Returns all EHR records for a given patient (CouchDB rich query)
    async GetEHRsByPatient(ctx, patientDid) {
        const queryString = JSON.stringify({
            selector: {
                docType: 'ehr',
                patientDid: patientDid,
            },
        });
        return await this._getQueryResult(ctx, queryString);
    }

    // GetEHRsByOrganization - Returns all EHR records created by an organization
    async GetEHRsByOrganization(ctx, creatorOrg) {
        const queryString = JSON.stringify({
            selector: {
                docType: 'ehr',
                creatorOrg: creatorOrg,
            },
        });
        return await this._getQueryResult(ctx, queryString);
    }

    // GetEHRsByCreator - Returns all EHR records created by a specific creator DID
    async GetEHRsByCreator(ctx, creatorDid) {
        const queryString = JSON.stringify({
            selector: {
                docType: 'ehr',
                creatorDid: creatorDid,
            },
        });
        return await this._getQueryResult(ctx, queryString);
    }

    // GetEHRHistory - Returns the full change history for an EHR record
    async GetEHRHistory(ctx, recordDid) {
        const exists = await this._assetExists(ctx, recordDid);
        if (!exists) {
            throw new Error(`The EHR record ${recordDid} does not exist`);
        }

        const allResults = [];
        const iterator = await ctx.stub.getHistoryForKey(recordDid);
        let result = await iterator.next();
        while (!result.done) {
            const record = {
                txId: result.value.txId,
                timestamp: result.value.timestamp,
                isDelete: result.value.isDelete,
            };
            if (!result.value.isDelete) {
                try {
                    record.value = JSON.parse(Buffer.from(result.value.value.toString()).toString('utf8'));
                } catch (err) {
                    console.log(err);
                    record.value = result.value.value.toString();
                }
            }
            allResults.push(record);
            result = await iterator.next();
        }
        return JSON.stringify(allResults);
    }

    // =========================================================================
    //                         CONSENT FUNCTIONS
    // =========================================================================

    // GrantConsent - Creates or updates a consent record granting orgs access to a record
    async GrantConsent(ctx, consentId, patientDid, targetRecordDid, grantedToOrgsJSON) {
        // Verify the EHR record exists
        await this.ReadEHR(ctx, targetRecordDid);

        const grantedToOrgs = JSON.parse(grantedToOrgsJSON);

        const consent = {
            docType: 'consent',
            consentId,
            patientDid,
            targetRecordDid,
            grantedToOrgs,
            status: 'ACTIVE',
            timestamp: new Date().toISOString(),
        };

        await ctx.stub.putState(consentId, Buffer.from(stringify(sortKeysRecursive(consent))));
        return JSON.stringify(consent);
    }

    // RevokeConsent - Revokes an existing consent
    async RevokeConsent(ctx, consentId) {
        const consentJSON = await ctx.stub.getState(consentId);
        if (!consentJSON || consentJSON.length === 0) {
            throw new Error(`Consent ${consentId} does not exist`);
        }

        const consent = JSON.parse(consentJSON.toString());
        if (consent.docType !== 'consent') {
            throw new Error(`${consentId} is not a consent record`);
        }

        consent.status = 'REVOKED';
        consent.timestamp = new Date().toISOString();

        await ctx.stub.putState(consentId, Buffer.from(stringify(sortKeysRecursive(consent))));
        return JSON.stringify(consent);
    }

    // ReadConsent - Reads a consent record by ID
    async ReadConsent(ctx, consentId) {
        const consentJSON = await ctx.stub.getState(consentId);
        if (!consentJSON || consentJSON.length === 0) {
            throw new Error(`Consent ${consentId} does not exist`);
        }
        const consent = JSON.parse(consentJSON.toString());
        if (consent.docType !== 'consent') {
            throw new Error(`${consentId} is not a consent record`);
        }
        return consentJSON.toString();
    }

    // GetConsentsByPatient - Returns all consents for a given patient
    async GetConsentsByPatient(ctx, patientDid) {
        const queryString = JSON.stringify({
            selector: {
                docType: 'consent',
                patientDid: patientDid,
            },
        });
        return await this._getQueryResult(ctx, queryString);
    }

    // GetConsentsByRecord - Returns all consents for a given EHR record
    async GetConsentsByRecord(ctx, targetRecordDid) {
        const queryString = JSON.stringify({
            selector: {
                docType: 'consent',
                targetRecordDid: targetRecordDid,
            },
        });
        return await this._getQueryResult(ctx, queryString);
    }

    // GetAllConsents - Returns all consent records
    async GetAllConsents(ctx) {
        return await this._queryByDocType(ctx, 'consent');
    }

    // =========================================================================
    //                         ACCESS LOG FUNCTIONS
    // =========================================================================

    // GetAccessLogsByRecord - Returns all access logs for a given EHR record
    async GetAccessLogsByRecord(ctx, targetRecordDid) {
        const queryString = JSON.stringify({
            selector: {
                docType: 'accessLog',
                targetRecordDid: targetRecordDid,
            },
        });
        return await this._getQueryResult(ctx, queryString);
    }

    // GetAccessLogsByAccessor - Returns all access logs by an accessor org
    async GetAccessLogsByAccessor(ctx, accessorOrg) {
        const queryString = JSON.stringify({
            selector: {
                docType: 'accessLog',
                accessorOrg: accessorOrg,
            },
        });
        return await this._getQueryResult(ctx, queryString);
    }

    // GetAllAccessLogs - Returns all access log records
    async GetAllAccessLogs(ctx) {
        return await this._queryByDocType(ctx, 'accessLog');
    }

    // =========================================================================
    //                         INTERNAL HELPER FUNCTIONS
    // =========================================================================

    async _assetExists(ctx, key) {
        const data = await ctx.stub.getState(key);
        return data && data.length > 0;
    }

    async _checkConsent(ctx, targetRecordDid, accessorOrg) {
        const queryString = JSON.stringify({
            selector: {
                docType: 'consent',
                targetRecordDid: targetRecordDid,
                status: 'ACTIVE',
            },
        });

        const resultsIterator = await ctx.stub.getQueryResult(queryString);
        let result = await resultsIterator.next();
        while (!result.done) {
            const consent = JSON.parse(Buffer.from(result.value.value.toString()).toString('utf8'));
            if (consent.grantedToOrgs && consent.grantedToOrgs.includes(accessorOrg)) {
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

module.exports = EHRContract;
