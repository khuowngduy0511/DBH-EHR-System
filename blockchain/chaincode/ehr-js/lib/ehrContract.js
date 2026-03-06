// ============================================================================
// DBH-EHR System - EHR Chaincode (JavaScript)
// ============================================================================
// Lưu trữ EHR hash trên blockchain để đảm bảo integrity
// Functions:
//   - CreateEhrHash: Tạo hash record cho EHR version
//   - UpdateEhrHash: Cập nhật hash cho version mới
//   - GetEhrHash: Lấy hash theo ehr_id + version
//   - GetEhrHistory: Lấy toàn bộ lịch sử thay đổi
//   - VerifyEhrIntegrity: Verify hash hiện tại match với blockchain
// ============================================================================

'use strict';

const { Contract } = require('fabric-contract-api');

class EhrContract extends Contract {

    // ========================================================================
    // CreateEhrHash - Tạo hash record cho EHR version
    // Args: ehrID (string), version (string), recordJSON (string)
    // ========================================================================
    async CreateEhrHash(ctx, ehrID, version, recordJSON) {
        const record = JSON.parse(recordJSON);

        // Composite key: EHR_{ehrId}_{version}
        const key = ctx.stub.createCompositeKey('EHR', [ehrID, version]);

        // Check if key already exists
        const existing = await ctx.stub.getState(key);
        if (existing && existing.length > 0) {
            throw new Error(`EHR hash already exists: ehrId=${ehrID}, version=${version}`);
        }

        // Add transaction metadata
        record.txId = ctx.stub.getTxID();
        if (!record.timestamp) {
            record.timestamp = new Date().toISOString();
        }

        const recordBytes = Buffer.from(JSON.stringify(record));

        await ctx.stub.putState(key, recordBytes);

        // Also store by patient DID for patient-centric queries
        const patientKey = ctx.stub.createCompositeKey('PATIENT_EHR', [record.patientDid, ehrID, version]);
        await ctx.stub.putState(patientKey, recordBytes);

        // Emit event
        const eventPayload = Buffer.from(JSON.stringify({
            type: 'EHR_HASH_CREATED',
            ehrId: ehrID,
            version: version,
            hash: record.contentHash,
            txId: record.txId,
        }));
        ctx.stub.setEvent('EhrHashCreated', eventPayload);
    }

    // ========================================================================
    // UpdateEhrHash - Tạo version mới (EHR versions are immutable)
    // Args: ehrID (string), version (string), recordJSON (string)
    // ========================================================================
    async UpdateEhrHash(ctx, ehrID, version, recordJSON) {
        return this.CreateEhrHash(ctx, ehrID, version, recordJSON);
    }

    // ========================================================================
    // GetEhrHash - Lấy hash theo ehrId và version
    // Args: ehrID (string), version (string)
    // Returns: JSON string of EhrHashRecord
    // ========================================================================
    async GetEhrHash(ctx, ehrID, version) {
        const key = ctx.stub.createCompositeKey('EHR', [ehrID, version]);
        const recordBytes = await ctx.stub.getState(key);

        if (!recordBytes || recordBytes.length === 0) {
            return '{}';
        }

        return recordBytes.toString();
    }

    // ========================================================================
    // GetEhrHistory - Lấy toàn bộ lịch sử thay đổi của EHR (all versions)
    // Args: ehrID (string)
    // Returns: JSON array of EhrHashRecord
    // ========================================================================
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

    // ========================================================================
    // VerifyEhrIntegrity - So sánh hash với hash lưu trên blockchain
    // Args: ehrID (string), version (string), providedHash (string)
    // Returns: JSON with valid (bool), storedHash, providedHash
    // ========================================================================
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
