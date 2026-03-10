/*
 * EHR Functions — Electronic Health Record management
 *
 * Handles: CreateEHR, ReadEHR, ReadEHRWithConsent, UpdateEHR, DeleteEHR,
 *          GetAllEHRs, GetEHRsByPatient, GetEHRsByOrganization, GetEHRsByCreator, GetEHRHistory
 */

'use strict';

const stringify = require('json-stringify-deterministic');
const sortKeysRecursive = require('sort-keys-recursive');
const { DOC_TYPES } = require('./models');

/**
 * EHR function implementations — mixed into the main contract
 */
const EhrFunctions = {

    // CreateEHR - Creates a new EHR pointer on the ledger
    async CreateEHR(ctx, recordDid, patientDid, creatorDid, ipfsCid, recordType) {
        const exists = await this._assetExists(ctx, recordDid);
        if (exists) {
            throw new Error(`The EHR record ${recordDid} already exists`);
        }

        const creatorOrg = ctx.clientIdentity.getMSPID();
        const now = new Date().toISOString();

        const ehr = {
            docType: DOC_TYPES.EHR,
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
    },

    // ReadEHR - Returns the EHR metadata (no consent check)
    async ReadEHR(ctx, recordDid) {
        const ehrJSON = await ctx.stub.getState(recordDid);
        if (!ehrJSON || ehrJSON.length === 0) {
            throw new Error(`The EHR record ${recordDid} does not exist`);
        }
        const ehr = JSON.parse(ehrJSON.toString());
        if (ehr.docType !== DOC_TYPES.EHR) {
            throw new Error(`${recordDid} is not an EHR record`);
        }
        return ehrJSON.toString();
    },

    // ReadEHRWithConsent - Access-gated read that checks per-user consent
    async ReadEHRWithConsent(ctx, recordDid, accessorDid, reason) {
        // 1. Read the EHR record
        const ehrString = await this.ReadEHR(ctx, recordDid);
        const ehr = JSON.parse(ehrString);

        // 2. If the accessor is the creator, allow access without consent check
        if (accessorDid !== ehr.creatorDid) {
            // 3. Check for active READ consent for this specific user
            const hasConsent = await this._checkConsent(ctx, recordDid, accessorDid, 'READ');
            if (!hasConsent) {
                throw new Error(
                    `User ${accessorDid} does not have READ consent to access record ${recordDid}`
                );
            }
        }

        return JSON.stringify(ehr);
    },

    // UpdateEHR - Updates the IPFS CID (creator or user with WRITE consent)
    async UpdateEHR(ctx, recordDid, callerDid, newIpfsCid, recordType) {
        const ehrString = await this.ReadEHR(ctx, recordDid);
        const ehr = JSON.parse(ehrString);

        // Creator can always update; others need WRITE consent
        if (callerDid !== ehr.creatorDid) {
            const hasConsent = await this._checkConsent(ctx, recordDid, callerDid, 'WRITE');
            if (!hasConsent) {
                throw new Error(
                    `User ${callerDid} does not have WRITE consent to update record ${recordDid}`
                );
            }
        }

        ehr.ipfsCid = newIpfsCid;
        if (recordType) {
            ehr.recordType = recordType;
        }
        ehr.timestamp = new Date().toISOString();

        await ctx.stub.putState(recordDid, Buffer.from(stringify(sortKeysRecursive(ehr))));
        return JSON.stringify(ehr);
    },

    // DeleteEHR - Deletes an EHR (creator or user with DELETE consent)
    async DeleteEHR(ctx, recordDid, callerDid) {
        const ehrString = await this.ReadEHR(ctx, recordDid);
        const ehr = JSON.parse(ehrString);

        // Creator can always delete; others need DELETE consent
        if (callerDid !== ehr.creatorDid) {
            const hasConsent = await this._checkConsent(ctx, recordDid, callerDid, 'DELETE');
            if (!hasConsent) {
                throw new Error(
                    `User ${callerDid} does not have DELETE consent to delete record ${recordDid}`
                );
            }
        }

        return ctx.stub.deleteState(recordDid);
    },

    // GetAllEHRs - Returns all EHR records
    async GetAllEHRs(ctx) {
        return await this._queryByDocType(ctx, DOC_TYPES.EHR);
    },

    // GetEHRsByPatient - Returns all EHR records for a given patient
    async GetEHRsByPatient(ctx, patientDid) {
        const queryString = JSON.stringify({
            selector: { docType: DOC_TYPES.EHR, patientDid },
        });
        return await this._getQueryResult(ctx, queryString);
    },

    // GetEHRsByOrganization - Returns all EHR records created by an organization
    async GetEHRsByOrganization(ctx, creatorOrg) {
        const queryString = JSON.stringify({
            selector: { docType: DOC_TYPES.EHR, creatorOrg },
        });
        return await this._getQueryResult(ctx, queryString);
    },

    // GetEHRsByCreator - Returns all EHR records by a specific creator DID
    async GetEHRsByCreator(ctx, creatorDid) {
        const queryString = JSON.stringify({
            selector: { docType: DOC_TYPES.EHR, creatorDid },
        });
        return await this._getQueryResult(ctx, queryString);
    },

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
    },
};

module.exports = EhrFunctions;
