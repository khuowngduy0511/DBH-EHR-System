/*
 * Emergency Access Functions — Consent bypass with on-chain proof
 *
 * Handles: EmergencyAccess, GetEmergencyAccessByRecord, GetEmergencyAccessByAccessor, GetAllEmergencyAccess
 *
 * Any doctor can call EmergencyAccess to read any EHR without consent.
 * The bypass is permanently recorded on-chain and cannot be deleted or modified.
 */

'use strict';

const stringify = require('json-stringify-deterministic');
const sortKeysRecursive = require('sort-keys-recursive');
const { DOC_TYPES } = require('./models');

/**
 * Emergency Access function implementations — mixed into the main contract
 */
const EmergencyAccessFunctions = {

    // EmergencyAccess - Bypasses consent to read an EHR, creates immutable on-chain record
    async EmergencyAccess(ctx, recordDid, accessorDid, reason) {
        if (!reason || reason.trim() === '') {
            throw new Error('Emergency access requires a reason');
        }

        // 1. Read the EHR record (no consent check)
        const ehrString = await this.ReadEHR(ctx, recordDid);
        const ehr = JSON.parse(ehrString);

        // 2. Create immutable emergency access log
        const accessorOrg = ctx.clientIdentity.getMSPID();
        const now = new Date().toISOString();
        const logId = `EMRG_${recordDid}_${Date.now()}`;

        const emergencyLog = {
            docType: DOC_TYPES.EMERGENCY_ACCESS,
            logId,
            targetRecordDid: recordDid,
            accessorDid,
            accessorOrg,
            reason,
            timestamp: now,
        };

        await ctx.stub.putState(logId, Buffer.from(stringify(sortKeysRecursive(emergencyLog))));

        return JSON.stringify({
            ehr,
            emergencyLog,
        });
    },

    // GetEmergencyAccessByRecord - Returns all emergency access logs for a given EHR record
    async GetEmergencyAccessByRecord(ctx, targetRecordDid) {
        const queryString = JSON.stringify({
            selector: { docType: DOC_TYPES.EMERGENCY_ACCESS, targetRecordDid },
        });
        return await this._getQueryResult(ctx, queryString);
    },

    // GetEmergencyAccessByAccessor - Returns all emergency access logs by a specific user
    async GetEmergencyAccessByAccessor(ctx, accessorDid) {
        const queryString = JSON.stringify({
            selector: { docType: DOC_TYPES.EMERGENCY_ACCESS, accessorDid },
        });
        return await this._getQueryResult(ctx, queryString);
    },

    // GetAllEmergencyAccess - Returns all emergency access records
    async GetAllEmergencyAccess(ctx) {
        return await this._queryByDocType(ctx, DOC_TYPES.EMERGENCY_ACCESS);
    },
};

module.exports = EmergencyAccessFunctions;
