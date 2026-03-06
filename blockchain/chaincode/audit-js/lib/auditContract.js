// ============================================================================
// DBH-EHR System - Audit Chaincode (JavaScript)
// ============================================================================
// Ghi audit trail bất biến lên blockchain
// Functions:
//   - CreateAuditEntry: Ghi audit entry mới
//   - GetAuditEntry: Lấy audit entry theo ID
//   - GetAuditsByPatient: Lấy tất cả audit liên quan đến patient
//   - GetAuditsByActor: Lấy tất cả audit theo actor
//   - GetAuditsByTarget: Lấy audit theo target resource
// ============================================================================

'use strict';

const { Contract } = require('fabric-contract-api');

class AuditContract extends Contract {

    // ========================================================================
    // CreateAuditEntry - Ghi audit entry mới lên ledger
    // Args: auditID (string), entryJSON (string)
    // ========================================================================
    async CreateAuditEntry(ctx, auditID, entryJSON) {
        const entry = JSON.parse(entryJSON);

        // Composite key: AUDIT_{auditId}
        const key = ctx.stub.createCompositeKey('AUDIT', [auditID]);

        // Check for duplicate
        const existing = await ctx.stub.getState(key);
        if (existing && existing.length > 0) {
            throw new Error(`Audit entry already exists: ${auditID}`);
        }

        // Set metadata
        entry.txId = ctx.stub.getTxID();
        if (!entry.timestamp) {
            entry.timestamp = new Date().toISOString();
        }

        const entryBytes = Buffer.from(JSON.stringify(entry));

        // Store by audit ID
        await ctx.stub.putState(key, entryBytes);

        // Index by actor DID
        const actorKey = ctx.stub.createCompositeKey('ACTOR_AUDIT', [entry.actorDid, auditID]);
        await ctx.stub.putState(actorKey, entryBytes);

        // Index by patient DID (if present)
        if (entry.patientDid) {
            const patientKey = ctx.stub.createCompositeKey('PATIENT_AUDIT', [entry.patientDid, auditID]);
            await ctx.stub.putState(patientKey, entryBytes);
        }

        // Index by target
        const targetKey = ctx.stub.createCompositeKey('TARGET_AUDIT', [entry.targetType, entry.targetId, auditID]);
        await ctx.stub.putState(targetKey, entryBytes);

        // Emit event
        const eventPayload = Buffer.from(JSON.stringify({
            type: 'AUDIT_CREATED',
            auditId: auditID,
            actorDid: entry.actorDid,
            action: entry.action,
            targetType: entry.targetType,
            targetId: entry.targetId,
            txId: entry.txId,
        }));
        ctx.stub.setEvent('AuditCreated', eventPayload);
    }

    // ========================================================================
    // GetAuditEntry - Lấy audit entry theo ID
    // Args: auditID (string)
    // Returns: JSON string of AuditEntry
    // ========================================================================
    async GetAuditEntry(ctx, auditID) {
        const key = ctx.stub.createCompositeKey('AUDIT', [auditID]);
        const entryBytes = await ctx.stub.getState(key);

        if (!entryBytes || entryBytes.length === 0) {
            return '{}';
        }

        return entryBytes.toString();
    }

    // ========================================================================
    // GetAuditsByPatient - Lấy tất cả audit liên quan đến patient
    // Args: patientDID (string)
    // Returns: JSON array of AuditEntry
    // ========================================================================
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

    // ========================================================================
    // GetAuditsByActor - Lấy tất cả audit theo actor
    // Args: actorDID (string)
    // Returns: JSON array of AuditEntry
    // ========================================================================
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

    // ========================================================================
    // GetAuditsByTarget - Lấy audit theo target resource
    // Args: targetType (string), targetID (string)
    // Returns: JSON array of AuditEntry
    // ========================================================================
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
