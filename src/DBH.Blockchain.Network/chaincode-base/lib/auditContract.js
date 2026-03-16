'use strict';

const { Contract } = require('fabric-contract-api');

class AuditContract extends Contract {
    async CreateAuditEntry(ctx, auditID, entryJSON) {
        const entry = JSON.parse(entryJSON);

        const key = ctx.stub.createCompositeKey('AUDIT', [auditID]);
        const now = new Date().toISOString();
        const existing = await ctx.stub.getState(key);
        if (existing && existing.length > 0) {
            throw new Error(`Audit entry already exists: ${auditID}`);
        }

        entry.txId = ctx.stub.getTxID();
        if (!entry.timestamp) {
            entry.timestamp = now;
        }

        const entryBytes = Buffer.from(JSON.stringify(entry));

        await ctx.stub.putState(key, entryBytes);

        const actorKey = ctx.stub.createCompositeKey('ACTOR_AUDIT', [entry.actorDid, auditID]);
        await ctx.stub.putState(actorKey, entryBytes);

        if (entry.patientDid) {
            const patientKey = ctx.stub.createCompositeKey('PATIENT_AUDIT', [entry.patientDid, auditID]);
            await ctx.stub.putState(patientKey, entryBytes);
        }

        const targetKey = ctx.stub.createCompositeKey('TARGET_AUDIT', [entry.targetType, entry.targetId, auditID]);
        await ctx.stub.putState(targetKey, entryBytes);

        const eventPayload = Buffer.from(JSON.stringify({
            type: 'AUDIT_CREATED',
            auditId: auditID,
            actorDid: entry.actorDid,
            action: entry.action,
            targetType: entry.targetType,
            targetId: entry.targetId,
            txId: entry.txId,
            timestamp: now,
        }));
        ctx.stub.setEvent('AuditCreated', eventPayload);
    }

    async GetAuditEntry(ctx, auditID) {
        const key = ctx.stub.createCompositeKey('AUDIT', [auditID]);
        const entryBytes = await ctx.stub.getState(key);
        if (!entryBytes || entryBytes.length === 0) {
            return '{}';
        }
        return entryBytes.toString();
    }

    async GetAuditsByPatient(ctx, patientDID) {
        const iterator = await ctx.stub.getStateByPartialCompositeKey('PATIENT_AUDIT', [patientDID]);
        const entries = [];
        let result = await iterator.next();
        while (!result.done) {
            try {
                const entry = JSON.parse(result.value.value.toString());
                entries.push(entry);
            } catch (e) {
                // skip malformed entries
            }
            result = await iterator.next();
        }
        await iterator.close();
        return JSON.stringify(entries);
    }

    async GetAuditsByActor(ctx, actorDID) {
        const iterator = await ctx.stub.getStateByPartialCompositeKey('ACTOR_AUDIT', [actorDID]);
        const entries = [];
        let result = await iterator.next();
        while (!result.done) {
            try {
                const entry = JSON.parse(result.value.value.toString());
                entries.push(entry);
            } catch (e) {
                // skip malformed entries
            }
            result = await iterator.next();
        }
        await iterator.close();
        return JSON.stringify(entries);
    }

    async GetAuditsByTarget(ctx, targetType, targetID) {
        const iterator = await ctx.stub.getStateByPartialCompositeKey('TARGET_AUDIT', [targetType, targetID]);
        const entries = [];
        let result = await iterator.next();
        while (!result.done) {
            try {
                const entry = JSON.parse(result.value.value.toString());
                entries.push(entry);
            } catch (e) {
                // skip malformed entries
            }
            result = await iterator.next();
        }
        await iterator.close();
        return JSON.stringify(entries);
    }
}

module.exports = AuditContract;
