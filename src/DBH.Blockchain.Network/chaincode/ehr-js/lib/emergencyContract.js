'use strict';

const { Contract } = require('fabric-contract-api');

class EmergencyContract extends Contract {

    // ========================================================================
    // EmergencyAccess - Bypasses consent to read an EHR, creates immutable on-chain record
    // ========================================================================
    async EmergencyAccess(ctx, recordDid, accessorDid, reason) {
        if (!reason || reason.trim() === '') {
            throw new Error('Emergency access requires a reason');
        }

        // Create immutable emergency access log
        const accessorOrg = ctx.clientIdentity.getMSPID();
        const now = new Date().toISOString();
        const timestamp = Date.now().toString();

        const logId = `EMRG_${recordDid}_${timestamp}`;

        const emergencyLog = {
            logId,
            targetRecordDid: recordDid,
            accessorDid,
            accessorOrg,
            reason,
            timestamp: now,
        };

        const emergencyLogBytes = Buffer.from(JSON.stringify(emergencyLog));

        // Use composite keys for querying like in ehrContract
        const key = ctx.stub.createCompositeKey('EMERGENCY_ACCESS', [recordDid, accessorDid, timestamp]);
        await ctx.stub.putState(key, emergencyLogBytes);

        // Also store by accessor Did
        const accessorKey = ctx.stub.createCompositeKey('EMERGENCY_ACCESS_BY_ACCESSOR', [accessorDid, recordDid, timestamp]);
        await ctx.stub.putState(accessorKey, emergencyLogBytes);

        // Emit event
        const eventPayload = Buffer.from(JSON.stringify({
            type: 'EMERGENCY_ACCESS_LOGGED',
            logId,
            targetRecordDid: recordDid,
            accessorDid,
            accessorOrg,
            timestamp: now
        }));
        ctx.stub.setEvent('EmergencyAccessLogged', eventPayload);

        return JSON.stringify(emergencyLog);
    }

    // ========================================================================
    // GetEmergencyAccessByRecord - Returns all emergency access logs for a given EHR record
    // ========================================================================
    async GetEmergencyAccessByRecord(ctx, targetRecordDid) {
        const iterator = await ctx.stub.getStateByPartialCompositeKey('EMERGENCY_ACCESS', [targetRecordDid]);
        const records = [];

        let result = await iterator.next();
        while (!result.done) {
            try {
                const record = JSON.parse(result.value.value.toString());
                records.push(record);
            } catch (e) {
                // skip malformed records
            }
            result = await iterator.next();
        }
        await iterator.close();

        return JSON.stringify(records);
    }

    // ========================================================================
    // GetEmergencyAccessByAccessor - Returns all emergency access logs by a specific user
    // ========================================================================
    async GetEmergencyAccessByAccessor(ctx, accessorDid) {
        const iterator = await ctx.stub.getStateByPartialCompositeKey('EMERGENCY_ACCESS_BY_ACCESSOR', [accessorDid]);
        const records = [];

        let result = await iterator.next();
        while (!result.done) {
            try {
                const record = JSON.parse(result.value.value.toString());
                records.push(record);
            } catch (e) {
                // skip malformed records
            }
            result = await iterator.next();
        }
        await iterator.close();

        return JSON.stringify(records);
    }

    // ========================================================================
    // GetAllEmergencyAccess - Returns all emergency access records
    // ========================================================================
    async GetAllEmergencyAccess(ctx) {
        const iterator = await ctx.stub.getStateByPartialCompositeKey('EMERGENCY_ACCESS', []);
        const records = [];

        let result = await iterator.next();
        while (!result.done) {
            try {
                const record = JSON.parse(result.value.value.toString());
                records.push(record);
            } catch (e) {
                // skip malformed records
            }
            result = await iterator.next();
        }
        await iterator.close();

        return JSON.stringify(records);
    }
}

module.exports = EmergencyContract;
