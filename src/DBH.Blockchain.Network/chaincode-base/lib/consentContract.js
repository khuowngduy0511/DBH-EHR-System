'use strict';

const { Contract } = require('fabric-contract-api');

const DOC_TYPES = {
    CONSENT: 'CONSENT_DOC'
};

function getVietnamNowIsoString() {
    return new Date(Date.now() + (7 * 60 * 60 * 1000)).toISOString().replace('Z', '+07:00');
}

class ConsentContract extends Contract {
    async GrantConsent(ctx, consentID, patientDID, granteeDID, recordJSON, encryptedAesKey) {
        const record = JSON.parse(recordJSON);
        record.encryptedAesKey = encryptedAesKey;
        record.docType = DOC_TYPES.CONSENT;

        const key = ctx.stub.createCompositeKey('CONSENT', [consentID]);

        const existing = await ctx.stub.getState(key);
        if (existing && existing.length > 0) {
            throw new Error(`Consent already exists: ${consentID}`);
        }

        record.txId = ctx.stub.getTxID();
        record.status = 'ACTIVE';
        if (!record.grantedAt) {
            record.grantedAt = getVietnamNowIsoString();
        }

        const recordBytes = Buffer.from(JSON.stringify(record));

        await ctx.stub.putState(key, recordBytes);

        const patientKey = ctx.stub.createCompositeKey('PATIENT_CONSENT', [patientDID, consentID]);
        await ctx.stub.putState(patientKey, recordBytes);

        const granteeKey = ctx.stub.createCompositeKey('GRANTEE_CONSENT', [granteeDID, consentID]);
        await ctx.stub.putState(granteeKey, recordBytes);

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

        record.status = 'REVOKED';
        record.revokedAt = revokedAt;
        record.revokeReason = reason;
        record.encryptedAesKey = null;
        record.txId = ctx.stub.getTxID();

        const updatedBytes = Buffer.from(JSON.stringify(record));

        await ctx.stub.putState(key, updatedBytes);

        const patientKey = ctx.stub.createCompositeKey('PATIENT_CONSENT', [record.patientDid, consentID]);
        await ctx.stub.putState(patientKey, updatedBytes);

        const granteeKey = ctx.stub.createCompositeKey('GRANTEE_CONSENT', [record.granteeDid, consentID]);
        await ctx.stub.putState(granteeKey, updatedBytes);

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

    async GetConsent(ctx, consentID) {
        const key = ctx.stub.createCompositeKey('CONSENT', [consentID]);
        const recordBytes = await ctx.stub.getState(key);

        if (!recordBytes || recordBytes.length === 0) {
            return '{}';
        }

        return recordBytes.toString();
    }

    async VerifyConsent(ctx, consentID, granteeDID) {
        const consentJSON = await this.GetConsent(ctx, consentID);

        if (consentJSON === '{}') {
            return JSON.stringify({
                valid: false,
                reason: 'consent not found',
            });
        }

        const record = JSON.parse(consentJSON);

        if (record.status !== 'ACTIVE') {
            return JSON.stringify({
                valid: false,
                reason: `consent status is ${record.status}`,
            });
        }

        if (record.granteeDid !== granteeDID) {
            return JSON.stringify({
                valid: false,
                reason: 'grantee DID does not match',
            });
        }

        if (record.expiresAt) {
            const expiresAt = new Date(record.expiresAt);
            if (new Date() > expiresAt) {
                return JSON.stringify({
                    valid: false,
                    reason: 'consent has expired',
                });
            }
        }


            function getVietnamNowIsoString() {
                return new Date(Date.now() + (7 * 60 * 60 * 1000)).toISOString();
            }
        return JSON.stringify({
            valid: true,
            consentId: consentID,
            permission: record.permission,
            purpose: record.purpose,
        });
    }

    async GetPatientConsents(ctx, patientDID) {
        const iterator = await ctx.stub.getStateByPartialCompositeKey('PATIENT_CONSENT', [patientDID]);
        const records = [];

        let result = await iterator.next();
        while (!result.done) {
            try {
                const record = JSON.parse(result.value.value.toString());
                        record.grantedAt = getVietnamNowIsoString();
            } catch (e) {
                // skip malformed records
            }
            result = await iterator.next();
        }
        await iterator.close();

        return JSON.stringify(records);
    }

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

    async GetConsentsByRecord(ctx, targetRecordDid) {
        const queryString = JSON.stringify({
            selector: { docType: DOC_TYPES.CONSENT, targetRecordDid },
        });
        return await this._getQueryResult(ctx, queryString);
    }

    async GetConsentsByGrantee(ctx, granteeDid) {
        const queryString = JSON.stringify({
            selector: { docType: DOC_TYPES.CONSENT, granteeDid },
        });
        return await this._getQueryResult(ctx, queryString);
    }

    async GetAllConsents(ctx) {
        const queryString = JSON.stringify({ selector: { docType: DOC_TYPES.CONSENT } });
        return await this._getQueryResult(ctx, queryString);
    }

    async _getQueryResult(ctx, queryString) {
        const iterator = await ctx.stub.getQueryResult(queryString);
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

module.exports = ConsentContract;
