-- =============================================================================
-- DBH-EHR System - Patient-Centric Access Control Schema
-- =============================================================================
-- This schema implements PATIENT-CENTRIC data control:
-- 1. Patients OWN their EHR data and control who can access it
-- 2. Patients GRANT/REVOKE access to healthcare organizations
-- 3. All access changes require PATIENT CONSENT
-- 4. Soft delete via REVOKE (never hard delete medical records)
-- =============================================================================

-- =============================================================================
-- ENUM TYPES
-- =============================================================================

-- Status for EHR records
CREATE TYPE ehr_status AS ENUM ('ACTIVE', 'REVOKED');

-- Access permission levels
CREATE TYPE access_level AS ENUM ('READ', 'READ_WRITE', 'FULL');

-- Consent action types
CREATE TYPE consent_action AS ENUM ('GRANT', 'REVOKE');

-- Request status for pending operations
CREATE TYPE request_status AS ENUM ('PENDING', 'APPROVED', 'REJECTED', 'APPLIED');

-- =============================================================================
-- TABLE: patients
-- =============================================================================
-- Patient registry - the data owners

CREATE TABLE patients (
    patient_id          VARCHAR(50) PRIMARY KEY,
    patient_name        VARCHAR(200) NOT NULL,
    date_of_birth       DATE,
    contact_info        JSONB,                      -- Email, phone, etc.
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- =============================================================================
-- TABLE: organizations
-- =============================================================================
-- Healthcare organizations that may request access

CREATE TABLE organizations (
    org_id              VARCHAR(50) PRIMARY KEY,
    org_name            VARCHAR(200) NOT NULL,
    org_type            VARCHAR(50) NOT NULL,       -- 'HOSPITAL', 'CLINIC', 'LAB', etc.
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Seed some organizations for demo
INSERT INTO organizations (org_id, org_name, org_type) VALUES
    ('HospitalA', 'City General Hospital', 'HOSPITAL'),
    ('HospitalB', 'Regional Medical Center', 'HOSPITAL'),
    ('LabX', 'DiagnostiCare Laboratory', 'LAB');

-- =============================================================================
-- TABLE: ehr_index
-- =============================================================================
-- Stores metadata/index for EHR documents stored off-chain (in MongoDB)
-- The PATIENT owns this record

CREATE TABLE ehr_index (
    record_id           UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    patient_id          VARCHAR(50) NOT NULL REFERENCES patients(patient_id),
    owner_org           VARCHAR(50) NOT NULL REFERENCES organizations(org_id),  -- Org that created it
    record_type         VARCHAR(50) NOT NULL,       -- e.g., 'DiagnosticReport', 'Observation'
    current_version     INTEGER NOT NULL DEFAULT 1,
    offchain_doc_id     VARCHAR(100) NOT NULL,      -- MongoDB document _id reference
    status              ehr_status NOT NULL DEFAULT 'ACTIVE',
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    -- Audit trail
    created_by          VARCHAR(100) NOT NULL,      -- Provider who created it
    
    CONSTRAINT uq_offchain_doc UNIQUE (offchain_doc_id)
);

-- Indexes for common query patterns
CREATE INDEX idx_ehr_index_patient_id ON ehr_index(patient_id);
CREATE INDEX idx_ehr_index_owner_org ON ehr_index(owner_org);
CREATE INDEX idx_ehr_index_status ON ehr_index(status);

-- =============================================================================
-- TABLE: access_grants
-- =============================================================================
-- Patient-controlled access permissions
-- ONLY THE PATIENT can grant/revoke access to their records

CREATE TABLE access_grants (
    grant_id            UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    record_id           UUID NOT NULL REFERENCES ehr_index(record_id),
    patient_id          VARCHAR(50) NOT NULL REFERENCES patients(patient_id),
    grantee_org         VARCHAR(50) NOT NULL REFERENCES organizations(org_id),
    access_level        access_level NOT NULL DEFAULT 'READ',
    is_active           BOOLEAN NOT NULL DEFAULT TRUE,
    
    -- Grant metadata
    granted_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    granted_reason      TEXT,
    
    -- Revoke metadata (if revoked)
    revoked_at          TIMESTAMPTZ,
    revoked_reason      TEXT,
    
    -- Each org can only have one grant per record (can be reactivated)
    CONSTRAINT uq_grant_record_org UNIQUE (record_id, grantee_org)
);

-- Index for access lookups
CREATE INDEX idx_access_grants_record ON access_grants(record_id) WHERE is_active = TRUE;
CREATE INDEX idx_access_grants_patient ON access_grants(patient_id);
CREATE INDEX idx_access_grants_grantee ON access_grants(grantee_org) WHERE is_active = TRUE;

-- =============================================================================
-- TABLE: consent_log
-- =============================================================================
-- Immutable audit log of all patient consent actions
-- Required for healthcare compliance (HIPAA, etc.)

CREATE TABLE consent_log (
    log_id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    patient_id          VARCHAR(50) NOT NULL REFERENCES patients(patient_id),
    action              consent_action NOT NULL,
    record_id           UUID REFERENCES ehr_index(record_id),
    target_org          VARCHAR(50) REFERENCES organizations(org_id),
    access_level        access_level,
    reason              TEXT,
    
    -- Consent verification (in real system: digital signature, OTP, etc.)
    consent_method      VARCHAR(50) NOT NULL DEFAULT 'DEMO_VERIFIED',
    consent_timestamp   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    -- Metadata
    ip_address          VARCHAR(50),
    user_agent          TEXT
);

-- Index for audit queries
CREATE INDEX idx_consent_log_patient ON consent_log(patient_id);
CREATE INDEX idx_consent_log_timestamp ON consent_log(consent_timestamp);

-- =============================================================================
-- TABLE: ehr_create_requests
-- =============================================================================
-- Requests from providers to create EHR records
-- Requires PATIENT CONSENT before record is created

CREATE TABLE ehr_create_requests (
    request_id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    patient_id          VARCHAR(50) NOT NULL REFERENCES patients(patient_id),
    requesting_org      VARCHAR(50) NOT NULL REFERENCES organizations(org_id),
    requesting_provider VARCHAR(100) NOT NULL,
    
    -- Record details
    record_type         VARCHAR(50) NOT NULL,
    offchain_doc_id     VARCHAR(100) NOT NULL,
    payload             JSONB NOT NULL,             -- Additional metadata
    
    -- Status
    status              request_status NOT NULL DEFAULT 'PENDING',
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    -- Resolution
    resolved_at         TIMESTAMPTZ,
    resolved_by         VARCHAR(100),               -- Patient ID or system
    created_record_id   UUID REFERENCES ehr_index(record_id)
);

CREATE INDEX idx_ehr_create_requests_patient ON ehr_create_requests(patient_id, status);

-- =============================================================================
-- FUNCTION: register_patient
-- =============================================================================
-- Register a new patient in the system

CREATE OR REPLACE FUNCTION register_patient(
    p_patient_id        VARCHAR(50),
    p_patient_name      VARCHAR(200),
    p_date_of_birth     DATE DEFAULT NULL,
    p_contact_info      JSONB DEFAULT NULL
) RETURNS VARCHAR(50) AS $$
BEGIN
    INSERT INTO patients (patient_id, patient_name, date_of_birth, contact_info)
    VALUES (p_patient_id, p_patient_name, p_date_of_birth, p_contact_info)
    ON CONFLICT (patient_id) DO UPDATE SET
        patient_name = EXCLUDED.patient_name,
        updated_at = NOW();
    
    RETURN p_patient_id;
END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- FUNCTION: provider_request_create_ehr
-- =============================================================================
-- Provider requests to create an EHR record for a patient
-- Record is NOT created until patient consents

CREATE OR REPLACE FUNCTION provider_request_create_ehr(
    p_patient_id        VARCHAR(50),
    p_requesting_org    VARCHAR(50),
    p_requesting_provider VARCHAR(100),
    p_record_type       VARCHAR(50),
    p_offchain_doc_id   VARCHAR(100),
    p_additional_info   JSONB DEFAULT '{}'
) RETURNS UUID AS $$
DECLARE
    v_request_id UUID;
BEGIN
    -- Verify patient exists
    IF NOT EXISTS (SELECT 1 FROM patients WHERE patient_id = p_patient_id) THEN
        RAISE EXCEPTION 'Patient % not found. Patient must be registered first.', p_patient_id;
    END IF;
    
    -- Verify organization exists
    IF NOT EXISTS (SELECT 1 FROM organizations WHERE org_id = p_requesting_org) THEN
        RAISE EXCEPTION 'Organization % not found.', p_requesting_org;
    END IF;
    
    -- Create the request
    INSERT INTO ehr_create_requests (
        patient_id, requesting_org, requesting_provider,
        record_type, offchain_doc_id, payload
    )
    VALUES (
        p_patient_id, p_requesting_org, p_requesting_provider,
        p_record_type, p_offchain_doc_id, p_additional_info
    )
    RETURNING request_id INTO v_request_id;
    
    RETURN v_request_id;
END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- FUNCTION: patient_consent_create_ehr
-- =============================================================================
-- Patient consents to create the EHR record
-- This is the PATIENT granting permission for their data to be stored

CREATE OR REPLACE FUNCTION patient_consent_create_ehr(
    p_request_id        UUID,
    p_patient_id        VARCHAR(50),
    p_consent_reason    TEXT DEFAULT 'Patient approved record creation'
) RETURNS UUID AS $$
DECLARE
    v_request ehr_create_requests%ROWTYPE;
    v_record_id UUID;
BEGIN
    -- Get and verify the request
    SELECT * INTO v_request
    FROM ehr_create_requests
    WHERE request_id = p_request_id
    FOR UPDATE;
    
    IF NOT FOUND THEN
        RAISE EXCEPTION 'Request % not found', p_request_id;
    END IF;
    
    -- Verify the patient owns this request
    IF v_request.patient_id != p_patient_id THEN
        RAISE EXCEPTION 'Patient % is not authorized to consent for this request', p_patient_id;
    END IF;
    
    IF v_request.status != 'PENDING' THEN
        RAISE EXCEPTION 'Request % is not pending (current status: %)', p_request_id, v_request.status;
    END IF;
    
    -- Create the EHR record
    INSERT INTO ehr_index (
        patient_id, owner_org, record_type, offchain_doc_id, created_by
    )
    VALUES (
        v_request.patient_id,
        v_request.requesting_org,
        v_request.record_type,
        v_request.offchain_doc_id,
        v_request.requesting_provider
    )
    RETURNING record_id INTO v_record_id;
    
    -- Update the request
    UPDATE ehr_create_requests
    SET status = 'APPLIED',
        resolved_at = NOW(),
        resolved_by = p_patient_id,
        created_record_id = v_record_id
    WHERE request_id = p_request_id;
    
    -- Auto-grant access to the creating organization
    INSERT INTO access_grants (record_id, patient_id, grantee_org, access_level, granted_reason)
    VALUES (v_record_id, p_patient_id, v_request.requesting_org, 'FULL', 'Auto-granted to creating organization');
    
    -- Log consent
    INSERT INTO consent_log (patient_id, action, record_id, target_org, access_level, reason, consent_method)
    VALUES (p_patient_id, 'GRANT', v_record_id, v_request.requesting_org, 'FULL', p_consent_reason, 'PATIENT_CONSENT');
    
    RETURN v_record_id;
END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- FUNCTION: patient_grant_access
-- =============================================================================
-- Patient grants access to their EHR record to another organization
-- THIS IS THE CORE PATIENT-CENTRIC FUNCTION

CREATE OR REPLACE FUNCTION patient_grant_access(
    p_patient_id        VARCHAR(50),
    p_record_id         UUID,
    p_grantee_org       VARCHAR(50),
    p_access_level      access_level DEFAULT 'READ',
    p_reason            TEXT DEFAULT NULL
) RETURNS UUID AS $$
DECLARE
    v_record ehr_index%ROWTYPE;
    v_grant_id UUID;
    v_existing_grant access_grants%ROWTYPE;
BEGIN
    -- Verify the record exists and patient owns it
    SELECT * INTO v_record
    FROM ehr_index
    WHERE record_id = p_record_id AND status = 'ACTIVE';
    
    IF NOT FOUND THEN
        RAISE EXCEPTION 'Record % not found or not active', p_record_id;
    END IF;
    
    IF v_record.patient_id != p_patient_id THEN
        RAISE EXCEPTION 'Patient % does not own record %. Only the patient can grant access.', p_patient_id, p_record_id;
    END IF;
    
    -- Verify grantee organization exists
    IF NOT EXISTS (SELECT 1 FROM organizations WHERE org_id = p_grantee_org) THEN
        RAISE EXCEPTION 'Organization % not found', p_grantee_org;
    END IF;
    
    -- Check if grant already exists and is active
    SELECT * INTO v_existing_grant
    FROM access_grants 
    WHERE record_id = p_record_id AND grantee_org = p_grantee_org;
    
    IF FOUND AND v_existing_grant.is_active = TRUE THEN
        RAISE EXCEPTION 'Organization % already has active access to record %', p_grantee_org, p_record_id;
    END IF;
    
    -- Create or reactivate the access grant
    IF FOUND THEN
        -- Reactivate existing grant
        UPDATE access_grants
        SET is_active = TRUE,
            access_level = p_access_level,
            granted_at = NOW(),
            granted_reason = p_reason,
            revoked_at = NULL,
            revoked_reason = NULL
        WHERE grant_id = v_existing_grant.grant_id
        RETURNING grant_id INTO v_grant_id;
    ELSE
        -- Create new grant
        INSERT INTO access_grants (record_id, patient_id, grantee_org, access_level, granted_reason)
        VALUES (p_record_id, p_patient_id, p_grantee_org, p_access_level, p_reason)
        RETURNING grant_id INTO v_grant_id;
    END IF;
    
    -- Log consent
    INSERT INTO consent_log (patient_id, action, record_id, target_org, access_level, reason)
    VALUES (p_patient_id, 'GRANT', p_record_id, p_grantee_org, p_access_level, p_reason);
    
    RETURN v_grant_id;
END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- FUNCTION: patient_revoke_access
-- =============================================================================
-- Patient revokes access from an organization
-- THIS IS THE CORE PATIENT-CENTRIC FUNCTION

CREATE OR REPLACE FUNCTION patient_revoke_access(
    p_patient_id        VARCHAR(50),
    p_record_id         UUID,
    p_grantee_org       VARCHAR(50),
    p_reason            TEXT DEFAULT NULL
) RETURNS BOOLEAN AS $$
DECLARE
    v_record ehr_index%ROWTYPE;
BEGIN
    -- Verify the record exists and patient owns it
    SELECT * INTO v_record
    FROM ehr_index
    WHERE record_id = p_record_id;
    
    IF NOT FOUND THEN
        RAISE EXCEPTION 'Record % not found', p_record_id;
    END IF;
    
    IF v_record.patient_id != p_patient_id THEN
        RAISE EXCEPTION 'Patient % does not own record %. Only the patient can revoke access.', p_patient_id, p_record_id;
    END IF;
    
    -- Check if grant exists and is active
    IF NOT EXISTS (
        SELECT 1 FROM access_grants 
        WHERE record_id = p_record_id AND grantee_org = p_grantee_org AND is_active = TRUE
    ) THEN
        RAISE EXCEPTION 'Organization % does not have active access to record %', p_grantee_org, p_record_id;
    END IF;
    
    -- Revoke the access (soft delete - keep audit trail)
    UPDATE access_grants
    SET is_active = FALSE,
        revoked_at = NOW(),
        revoked_reason = p_reason
    WHERE record_id = p_record_id AND grantee_org = p_grantee_org AND is_active = TRUE;
    
    -- Log consent action
    INSERT INTO consent_log (patient_id, action, record_id, target_org, reason)
    VALUES (p_patient_id, 'REVOKE', p_record_id, p_grantee_org, p_reason);
    
    RETURN TRUE;
END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- FUNCTION: check_access
-- =============================================================================
-- Check if an organization has access to a record
-- Used by applications before showing data

CREATE OR REPLACE FUNCTION check_access(
    p_record_id         UUID,
    p_requesting_org    VARCHAR(50)
) RETURNS TABLE (
    has_access BOOLEAN,
    access_level access_level,
    granted_at TIMESTAMPTZ
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        TRUE as has_access,
        ag.access_level,
        ag.granted_at
    FROM access_grants ag
    WHERE ag.record_id = p_record_id 
      AND ag.grantee_org = p_requesting_org
      AND ag.is_active = TRUE;
    
    -- If no rows returned, return no access
    IF NOT FOUND THEN
        RETURN QUERY SELECT FALSE, NULL::access_level, NULL::TIMESTAMPTZ;
    END IF;
END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- FUNCTION: get_patient_records
-- =============================================================================
-- Get all EHR records for a patient (only patient can see all their records)

CREATE OR REPLACE FUNCTION get_patient_records(
    p_patient_id VARCHAR(50)
) RETURNS TABLE (
    record_id UUID,
    record_type VARCHAR,
    owner_org VARCHAR,
    offchain_doc_id VARCHAR,
    status ehr_status,
    created_at TIMESTAMPTZ,
    access_grants_count BIGINT
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        e.record_id,
        e.record_type,
        e.owner_org,
        e.offchain_doc_id,
        e.status,
        e.created_at,
        (SELECT COUNT(*) FROM access_grants ag WHERE ag.record_id = e.record_id AND ag.is_active = TRUE)
    FROM ehr_index e
    WHERE e.patient_id = p_patient_id
    ORDER BY e.created_at DESC;
END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- FUNCTION: get_accessible_records
-- =============================================================================
-- Get records an organization has access to (via patient grants)

CREATE OR REPLACE FUNCTION get_accessible_records(
    p_org_id VARCHAR(50)
) RETURNS TABLE (
    record_id UUID,
    patient_id VARCHAR,
    record_type VARCHAR,
    offchain_doc_id VARCHAR,
    access_level access_level,
    granted_at TIMESTAMPTZ
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        e.record_id,
        e.patient_id,
        e.record_type,
        e.offchain_doc_id,
        ag.access_level,
        ag.granted_at
    FROM ehr_index e
    JOIN access_grants ag ON e.record_id = ag.record_id
    WHERE ag.grantee_org = p_org_id 
      AND ag.is_active = TRUE
      AND e.status = 'ACTIVE'
    ORDER BY ag.granted_at DESC;
END;
$$ LANGUAGE plpgsql;

-- =============================================================================
-- VIEW: v_consent_audit
-- =============================================================================
-- Audit view for compliance reporting

CREATE VIEW v_consent_audit AS
SELECT 
    cl.log_id,
    cl.consent_timestamp,
    cl.patient_id,
    p.patient_name,
    cl.action,
    cl.record_id,
    cl.target_org,
    o.org_name as target_org_name,
    cl.access_level,
    cl.reason,
    cl.consent_method
FROM consent_log cl
JOIN patients p ON cl.patient_id = p.patient_id
LEFT JOIN organizations o ON cl.target_org = o.org_id
ORDER BY cl.consent_timestamp DESC;

-- =============================================================================
-- INITIAL VERIFICATION OUTPUT
-- =============================================================================

DO $$
BEGIN
    RAISE NOTICE '=== DBH-EHR Patient-Centric Schema Installation Complete ===';
    RAISE NOTICE '';
    RAISE NOTICE 'Tables created:';
    RAISE NOTICE '  - patients: Patient registry (data owners)';
    RAISE NOTICE '  - organizations: Healthcare orgs';
    RAISE NOTICE '  - ehr_index: EHR metadata (owned by patients)';
    RAISE NOTICE '  - access_grants: Patient-controlled access permissions';
    RAISE NOTICE '  - consent_log: Immutable audit trail';
    RAISE NOTICE '  - ehr_create_requests: Provider requests pending patient consent';
    RAISE NOTICE '';
    RAISE NOTICE 'Core Patient Functions:';
    RAISE NOTICE '  - patient_grant_access(): Patient grants org access to their record';
    RAISE NOTICE '  - patient_revoke_access(): Patient revokes org access';
    RAISE NOTICE '  - patient_consent_create_ehr(): Patient approves record creation';
    RAISE NOTICE '';
    RAISE NOTICE 'KEY PRINCIPLE: Only PATIENTS can control access to their data!';
    RAISE NOTICE '================================================================';
END $$;
