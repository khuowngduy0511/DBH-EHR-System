'use strict';

const { Contract } = require('fabric-contract-api');

class EmergencyContract extends Contract {
    async EmergencyAccess(ctx, recordDid, accessorDid, reason) {
        if (!reason || reason.trim() === '') {
            throw new Error('Emergency access requires a reason');
        }

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

        const key = ctx.stub.createCompositeKey('EMERGENCY_ACCESS', [recordDid, accessorDid, timestamp]);
        await ctx.stub.putState(key, emergencyLogBytes);

        const accessorKey = ctx.stub.createCompositeKey('EMERGENCY_ACCESS_BY_ACCESSOR', [accessorDid, recordDid, timestamp]);
        await ctx.stub.putState(accessorKey, emergencyLogBytes);

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
