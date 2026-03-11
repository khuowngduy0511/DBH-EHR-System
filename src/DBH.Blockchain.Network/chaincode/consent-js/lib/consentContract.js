// ============================================================================
// DBH-EHR System - Consent Chaincode (JavaScript)
// ============================================================================
// Quản lý consent (cấp/thu hồi quyền truy cập) trên blockchain
// Functions:
//   - GrantConsent: Ghi consent mới lên ledger
//   - RevokeConsent: Thu hồi consent
//   - GetConsent: Lấy consent theo ID
//   - VerifyConsent: Kiểm tra consent còn hiệu lực
//   - GetPatientConsents: Lấy tất cả consent của patient
//   - GetConsentHistory: Lấy lịch sử thay đổi consent
// ============================================================================

'use strict';

const { Contract } = require('fabric-contract-api');

class ConsentContract extends Contract {

    // ========================================================================
    // GrantConsent - Ghi consent mới lên ledger
    // Args: consentID, patientDID, granteeDID, recordJSON, encryptedAesKey
    // ========================================================================
    async GrantConsent(ctx, consentID, patientDID, granteeDID, recordJSON, encryptedAesKey) {
        const record = JSON.parse(recordJSON);
        record.encryptedAesKey = encryptedAesKey;

        // Composite key: CONSENT_{consentId}
        const key = ctx.stub.createCompositeKey('CONSENT', [consentID]);

        // Check if consent already exists
        const existing = await ctx.stub.getState(key);
        if (existing && existing.length > 0) {
            throw new Error(`Consent already exists: ${consentID}`);
        }

        // Set metadata
        record.txId = ctx.stub.getTxID();
        record.status = 'ACTIVE';
        if (!record.grantedAt) {
            record.grantedAt = new Date().toISOString();
        }

        const recordBytes = Buffer.from(JSON.stringify(record));

        // Store by consent ID
        await ctx.stub.putState(key, recordBytes);

        // Index by patient DID
        const patientKey = ctx.stub.createCompositeKey('PATIENT_CONSENT', [patientDID, consentID]);
        await ctx.stub.putState(patientKey, recordBytes);

        // Index by grantee DID
        const granteeKey = ctx.stub.createCompositeKey('GRANTEE_CONSENT', [granteeDID, consentID]);
        await ctx.stub.putState(granteeKey, recordBytes);

        // Emit event
        const eventPayload = Buffer.from(JSON.stringify({
            type: 'CONSENT_GRANTED',
            consentId: consentID,
            patientDid: patientDID,
            granteeDid: granteeDID,
            permission: record.permission,
            purpose: record.purpose,
            txId: record.txId,
        }));
        ctx.stub.setEvent('ConsentGranted', eventPayload);
    }

    // ========================================================================
    // RevokeConsent - Thu hồi consent
    // Args: consentID, revokedAt, reason
    // ========================================================================
    async RevokeConsent(ctx, consentID, revokedAt, reason) {
        const key = ctx.stub.createCompositeKey('CONSENT', [consentID]);

        const recordBytes = await ctx.stub.getState(key);
        if (!recordBytes || recordBytes.length === 0) {
            throw new Error(`Consent not found: ${consentID}`);
        }

        const record = JSON.parse(recordBytes.toString());

        if (record.status === 'REVOKED') {
            throw new Error(`Consent already revoked: ${consentID}`);
        }

        // Update status
        record.status = 'REVOKED';
        record.revokedAt = revokedAt;
        record.revokeReason = reason;
        // Optionally clear the key on revoke to ensure further access is strictly disconnected
        record.encryptedAesKey = null;
        record.txId = ctx.stub.getTxID();

        const updatedBytes = Buffer.from(JSON.stringify(record));

        // Update all indexes
        await ctx.stub.putState(key, updatedBytes);

        const patientKey = ctx.stub.createCompositeKey('PATIENT_CONSENT', [record.patientDid, consentID]);
        await ctx.stub.putState(patientKey, updatedBytes);

        const granteeKey = ctx.stub.createCompositeKey('GRANTEE_CONSENT', [record.granteeDid, consentID]);
        await ctx.stub.putState(granteeKey, updatedBytes);

        // Emit event
        const eventPayload = Buffer.from(JSON.stringify({
            type: 'CONSENT_REVOKED',
            consentId: consentID,
            patientDid: record.patientDid,
            granteeDid: record.granteeDid,
            reason: reason,
            txId: record.txId,
        }));
        ctx.stub.setEvent('ConsentRevoked', eventPayload);
    }

    // ========================================================================
    // GetConsent - Lấy consent theo ID
    // Args: consentID
    // Returns: JSON string of ConsentRecord
    // ========================================================================
    async GetConsent(ctx, consentID) {
        const key = ctx.stub.createCompositeKey('CONSENT', [consentID]);
        const recordBytes = await ctx.stub.getState(key);

        if (!recordBytes || recordBytes.length === 0) {
            return '{}';
        }

        return recordBytes.toString();
    }

    // ========================================================================
    // VerifyConsent - Kiểm tra consent còn hiệu lực
    // Args: consentID, granteeDID
    // Returns: JSON with valid (bool), permission, purpose
    // ========================================================================
    async VerifyConsent(ctx, consentID, granteeDID) {
        const consentJSON = await this.GetConsent(ctx, consentID);

        if (consentJSON === '{}') {
            return JSON.stringify({
                valid: false,
                reason: 'consent not found',
            });
        }

        const record = JSON.parse(consentJSON);

        // Check status
        if (record.status !== 'ACTIVE') {
            return JSON.stringify({
                valid: false,
                reason: `consent status is ${record.status}`,
            });
        }

        // Check grantee
        if (record.granteeDid !== granteeDID) {
            return JSON.stringify({
                valid: false,
                reason: 'grantee DID does not match',
            });
        }

        // Check expiry
        if (record.expiresAt) {
            const expiresAt = new Date(record.expiresAt);
            if (new Date() > expiresAt) {
                return JSON.stringify({
                    valid: false,
                    reason: 'consent has expired',
                });
            }
        }

        return JSON.stringify({
            valid: true,
            consentId: consentID,
            permission: record.permission,
            purpose: record.purpose,
        });
    }

    // ========================================================================
    // GetPatientConsents - Lấy tất cả consent của patient
    // Args: patientDID
    // Returns: JSON array of ConsentRecord
    // ========================================================================
    async GetPatientConsents(ctx, patientDID) {
        const iterator = await ctx.stub.getStateByPartialCompositeKey('PATIENT_CONSENT', [patientDID]);
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
    // GetConsentHistory - Lấy lịch sử thay đổi consent trên ledger
    // Args: consentID
    // Returns: JSON array of {txId, timestamp, isDelete, record}
    // ========================================================================
    async GetConsentHistory(ctx, consentID) {
        const key = ctx.stub.createCompositeKey('CONSENT', [consentID]);
        const iterator = await ctx.stub.getHistoryForKey(key);
        const history = [];

        let result = await iterator.next();
        while (!result.done) {
            try {
                const modification = result.value;
                let record = {};
                if (modification.value && modification.value.length > 0) {
                    record = JSON.parse(modification.value.toString());
                }

                history.push({
                    txId: modification.txId,
                    timestamp: modification.timestamp
                        ? new Date(modification.timestamp.seconds.low * 1000).toISOString()
                        : '',
                    isDelete: modification.isDelete,
                    record: record,
                });
            } catch (e) {
                // skip malformed entries
            }
            result = await iterator.next();
        }
        await iterator.close();

        return JSON.stringify(history);
    }

    // GetConsentsByRecord - Returns all consents for a given EHR record
    async GetConsentsByRecord(ctx, targetRecordDid) {
        const queryString = JSON.stringify({
            selector: { docType: DOC_TYPES.CONSENT, targetRecordDid },
        });
        return await this._getQueryResult(ctx, queryString);
    }

    // GetConsentsByGrantee - Returns all consents granted to a specific user
    async GetConsentsByGrantee(ctx, granteeDid) {
        const queryString = JSON.stringify({
            selector: { docType: DOC_TYPES.CONSENT, granteeDid },
        });
        return await this._getQueryResult(ctx, queryString);
    }

    // GetAllConsents - Returns all consent records
    async GetAllConsents(ctx) {
        return await this._queryByDocType(ctx, DOC_TYPES.CONSENT);
    }
}

module.exports = ConsentContract;
