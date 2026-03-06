/*
 * Access Log Functions — On-chain audit trail for record reads
 *
 * Handles: CreateAccessLog, GetAccessLogsByRecord, GetAccessLogsByAccessor, GetAllAccessLogs
 *
 * Note: These functions are standalone and not automatically called by ReadEHRWithConsent.
 *       The C# backend decides when to create access logs.
 */

'use strict';

const stringify = require('json-stringify-deterministic');
const sortKeysRecursive = require('sort-keys-recursive');
const { DOC_TYPES, ACCESS_ACTIONS } = require('./models');

/**
 * Access Log function implementations — mixed into the main contract
 */
const AccessLogFunctions = {

    // CreateAccessLog - Explicitly creates an access log entry on-chain
    async CreateAccessLog(ctx, logId, targetRecordDid, accessorDid, action, reason) {
        const accessorOrg = ctx.clientIdentity.getMSPID();
        const now = new Date().toISOString();

        // Validate action
        const validActions = Object.values(ACCESS_ACTIONS);
        if (!validActions.includes(action)) {
            throw new Error(
                `Invalid action "${action}". Must be one of: ${validActions.join(', ')}`
            );
        }

        const accessLog = {
            docType: DOC_TYPES.ACCESS_LOG,
            logId,
            targetRecordDid,
            accessorDid,
            accessorOrg,
            action,
            reason: reason || '',
            timestamp: now,
        };

        await ctx.stub.putState(logId, Buffer.from(stringify(sortKeysRecursive(accessLog))));
        return JSON.stringify(accessLog);
    },

    // GetAccessLogsByRecord - Returns all access logs for a given EHR record
    async GetAccessLogsByRecord(ctx, targetRecordDid) {
        const queryString = JSON.stringify({
            selector: { docType: DOC_TYPES.ACCESS_LOG, targetRecordDid },
        });
        return await this._getQueryResult(ctx, queryString);
    },

    // GetAccessLogsByAccessor - Returns all access logs by a specific user
    async GetAccessLogsByAccessor(ctx, accessorDid) {
        const queryString = JSON.stringify({
            selector: { docType: DOC_TYPES.ACCESS_LOG, accessorDid },
        });
        return await this._getQueryResult(ctx, queryString);
    },

    // GetAllAccessLogs - Returns all access log records
    async GetAllAccessLogs(ctx) {
        return await this._queryByDocType(ctx, DOC_TYPES.ACCESS_LOG);
    },
};

module.exports = AccessLogFunctions;
