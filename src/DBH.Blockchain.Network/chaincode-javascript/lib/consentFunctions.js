/*
 * Consent Functions — Per-user consent management
 *
 * Handles: GrantConsent, RevokeConsent, ReadConsent,
 *          GetConsentsByPatient, GetConsentsByRecord, GetConsentsByGrantee, GetAllConsents
 */

'use strict';

const stringify = require('json-stringify-deterministic');
const sortKeysRecursive = require('sort-keys-recursive');
const { DOC_TYPES, CONSENT_STATUS, PERMISSIONS } = require('./models');

const VALID_PERMISSIONS = Object.values(PERMISSIONS);

/**
 * Consent function implementations — mixed into the main contract
 */
const ConsentFunctions = {

    // GrantConsent - Creates a per-user consent granting access to a specific record
    async GrantConsent(ctx, consentId, patientDid, targetRecordDid, granteeDid, granteeOrg, permission, expiresAt) {
        // Verify the EHR record exists
        await this.ReadEHR(ctx, targetRecordDid);

        // Validate permission value
        if (!VALID_PERMISSIONS.includes(permission)) {
            throw new Error(
                `Invalid permission "${permission}". Must be one of: ${VALID_PERMISSIONS.join(', ')}`
            );
        }

        const now = new Date().toISOString();

        const consent = {
            docType: DOC_TYPES.CONSENT,
            consentId,
            patientDid,
            targetRecordDid,
            granteeDid,
            granteeOrg,
            permission,
            expiresAt: expiresAt || null,
            status: CONSENT_STATUS.ACTIVE,
            grantedAt: now,
            revokedAt: null,
        };

        await ctx.stub.putState(consentId, Buffer.from(stringify(sortKeysRecursive(consent))));
        return JSON.stringify(consent);
    },

    // RevokeConsent - Revokes an existing consent
    async RevokeConsent(ctx, consentId) {
        const consentJSON = await ctx.stub.getState(consentId);
        if (!consentJSON || consentJSON.length === 0) {
            throw new Error(`Consent ${consentId} does not exist`);
        }

        const consent = JSON.parse(consentJSON.toString());
        if (consent.docType !== DOC_TYPES.CONSENT) {
            throw new Error(`${consentId} is not a consent record`);
        }

        consent.status = CONSENT_STATUS.REVOKED;
        consent.revokedAt = new Date().toISOString();

        await ctx.stub.putState(consentId, Buffer.from(stringify(sortKeysRecursive(consent))));
        return JSON.stringify(consent);
    },

    // ReadConsent - Reads a consent record by ID
    async ReadConsent(ctx, consentId) {
        const consentJSON = await ctx.stub.getState(consentId);
        if (!consentJSON || consentJSON.length === 0) {
            throw new Error(`Consent ${consentId} does not exist`);
        }
        const consent = JSON.parse(consentJSON.toString());
        if (consent.docType !== DOC_TYPES.CONSENT) {
            throw new Error(`${consentId} is not a consent record`);
        }
        return consentJSON.toString();
    },

    // GetConsentsByPatient - Returns all consents for a given patient
    async GetConsentsByPatient(ctx, patientDid) {
        const queryString = JSON.stringify({
            selector: { docType: DOC_TYPES.CONSENT, patientDid },
        });
        return await this._getQueryResult(ctx, queryString);
    },

    // GetConsentsByRecord - Returns all consents for a given EHR record
    async GetConsentsByRecord(ctx, targetRecordDid) {
        const queryString = JSON.stringify({
            selector: { docType: DOC_TYPES.CONSENT, targetRecordDid },
        });
        return await this._getQueryResult(ctx, queryString);
    },

    // GetConsentsByGrantee - Returns all consents granted to a specific user
    async GetConsentsByGrantee(ctx, granteeDid) {
        const queryString = JSON.stringify({
            selector: { docType: DOC_TYPES.CONSENT, granteeDid },
        });
        return await this._getQueryResult(ctx, queryString);
    },

    // GetAllConsents - Returns all consent records
    async GetAllConsents(ctx) {
        return await this._queryByDocType(ctx, DOC_TYPES.CONSENT);
    },
};

module.exports = ConsentFunctions;
