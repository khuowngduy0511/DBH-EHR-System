'use strict';

const { Contract } = require('fabric-contract-api');

class EhrContract extends Contract {
    async CreateEhrHash(ctx, ehrID, version, recordJSON, encryptedAesKey) {
        const record = JSON.parse(recordJSON);
        record.encryptedAesKey = encryptedAesKey;

        const key = ctx.stub.createCompositeKey('EHR', [ehrID, version]);

        const existing = await ctx.stub.getState(key);
        if (existing && existing.length > 0) {
            throw new Error(`EHR hash already exists: ehrId=${ehrID}, version=${version}`);
        }

        record.txId = ctx.stub.getTxID();
        if (!record.timestamp) {
            record.timestamp = new Date().toISOString();
        }

        const recordBytes = Buffer.from(JSON.stringify(record));

        await ctx.stub.putState(key, recordBytes);

        const patientKey = ctx.stub.createCompositeKey('PATIENT_EHR', [record.patientDid, ehrID, version]);
        await ctx.stub.putState(patientKey, recordBytes);

        const eventPayload = Buffer.from(JSON.stringify({
            type: 'EHR_HASH_CREATED',
            ehrId: ehrID,
            version: version,
            hash: record.contentHash,
            txId: record.txId,
        }));
        ctx.stub.setEvent('EhrHashCreated', eventPayload);
    }

    async UpdateEhrHash(ctx, ehrID, version, recordJSON, encryptedAesKey) {
        return this.CreateEhrHash(ctx, ehrID, version, recordJSON, encryptedAesKey);
    }

    async GetEhrHash(ctx, ehrID, version) {
        const key = ctx.stub.createCompositeKey('EHR', [ehrID, version]);
        const recordBytes = await ctx.stub.getState(key);

        if (!recordBytes || recordBytes.length === 0) {
            return '{}';
        }

        return recordBytes.toString();
    }

    async GetEhrHistory(ctx, ehrID) {
        const iterator = await ctx.stub.getStateByPartialCompositeKey('EHR', [ehrID]);
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

    async VerifyEhrIntegrity(ctx, ehrID, version, providedHash) {
        const storedJSON = await this.GetEhrHash(ctx, ehrID, version);

        if (storedJSON === '{}') {
            return JSON.stringify({
                valid: false,
                reason: 'no record found',
                ehrId: ehrID,
                version: version,
            });
        }

        const stored = JSON.parse(storedJSON);
        const isValid = stored.contentHash === providedHash;

        return JSON.stringify({
            valid: isValid,
            ehrId: ehrID,
            version: version,
            storedHash: stored.contentHash,
            providedHash: providedHash,
        });
    }
}

module.exports = EhrContract;
