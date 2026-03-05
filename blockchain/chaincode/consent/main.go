// ============================================================================
// DBH-EHR System - Consent Chaincode
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

package main

import (
	"encoding/json"
	"fmt"
	"time"

	"github.com/hyperledger/fabric-contract-api-go/contractapi"
)

// ConsentChaincode implements chaincode interface
type ConsentChaincode struct {
	contractapi.Contract
}

// ConsentRecord represents a consent record on blockchain
type ConsentRecord struct {
	ConsentID    string `json:"consentId"`
	PatientDID   string `json:"patientDid"`
	GranteeDID   string `json:"granteeDid"`
	GranteeType  string `json:"granteeType"`
	Permission   string `json:"permission"`
	Purpose      string `json:"purpose"`
	EhrID        string `json:"ehrId,omitempty"`
	GrantedAt    string `json:"grantedAt"`
	ExpiresAt    string `json:"expiresAt,omitempty"`
	Status       string `json:"status"`
	RevokedAt    string `json:"revokedAt,omitempty"`
	RevokeReason string `json:"revokeReason,omitempty"`
	TxID         string `json:"txId"`
}

// GrantConsent creates a new consent record on the ledger
func (c *ConsentChaincode) GrantConsent(ctx contractapi.TransactionContextInterface, consentID string, patientDID string, granteeDID string, recordJSON string) error {
	var record ConsentRecord
	err := json.Unmarshal([]byte(recordJSON), &record)
	if err != nil {
		return fmt.Errorf("failed to parse record JSON: %v", err)
	}

	// Composite key: CONSENT_{consentId}
	key, err := ctx.GetStub().CreateCompositeKey("CONSENT", []string{consentID})
	if err != nil {
		return fmt.Errorf("failed to create composite key: %v", err)
	}

	// Check if consent already exists
	existing, err := ctx.GetStub().GetState(key)
	if err != nil {
		return fmt.Errorf("failed to read from world state: %v", err)
	}
	if existing != nil {
		return fmt.Errorf("consent already exists: %s", consentID)
	}

	// Set metadata
	record.TxID = ctx.GetStub().GetTxID()
	record.Status = "ACTIVE"
	if record.GrantedAt == "" {
		record.GrantedAt = time.Now().UTC().Format(time.RFC3339)
	}

	recordBytes, err := json.Marshal(record)
	if err != nil {
		return fmt.Errorf("failed to marshal record: %v", err)
	}

	// Store by consent ID
	err = ctx.GetStub().PutState(key, recordBytes)
	if err != nil {
		return fmt.Errorf("failed to put state: %v", err)
	}

	// Index by patient DID
	patientKey, _ := ctx.GetStub().CreateCompositeKey("PATIENT_CONSENT", []string{patientDID, consentID})
	ctx.GetStub().PutState(patientKey, recordBytes)

	// Index by grantee DID
	granteeKey, _ := ctx.GetStub().CreateCompositeKey("GRANTEE_CONSENT", []string{granteeDID, consentID})
	ctx.GetStub().PutState(granteeKey, recordBytes)

	// Emit event
	eventPayload, _ := json.Marshal(map[string]interface{}{
		"type":       "CONSENT_GRANTED",
		"consentId":  consentID,
		"patientDid": patientDID,
		"granteeDid": granteeDID,
		"permission": record.Permission,
		"purpose":    record.Purpose,
		"txId":       record.TxID,
	})
	ctx.GetStub().SetEvent("ConsentGranted", eventPayload)

	return nil
}

// RevokeConsent revokes an existing consent
func (c *ConsentChaincode) RevokeConsent(ctx contractapi.TransactionContextInterface, consentID string, revokedAt string, reason string) error {
	key, err := ctx.GetStub().CreateCompositeKey("CONSENT", []string{consentID})
	if err != nil {
		return fmt.Errorf("failed to create composite key: %v", err)
	}

	recordBytes, err := ctx.GetStub().GetState(key)
	if err != nil {
		return fmt.Errorf("failed to read from world state: %v", err)
	}
	if recordBytes == nil {
		return fmt.Errorf("consent not found: %s", consentID)
	}

	var record ConsentRecord
	err = json.Unmarshal(recordBytes, &record)
	if err != nil {
		return fmt.Errorf("failed to unmarshal record: %v", err)
	}

	if record.Status == "REVOKED" {
		return fmt.Errorf("consent already revoked: %s", consentID)
	}

	// Update status
	record.Status = "REVOKED"
	record.RevokedAt = revokedAt
	record.RevokeReason = reason
	record.TxID = ctx.GetStub().GetTxID()

	updatedBytes, err := json.Marshal(record)
	if err != nil {
		return fmt.Errorf("failed to marshal updated record: %v", err)
	}

	// Update all indexes
	ctx.GetStub().PutState(key, updatedBytes)

	patientKey, _ := ctx.GetStub().CreateCompositeKey("PATIENT_CONSENT", []string{record.PatientDID, consentID})
	ctx.GetStub().PutState(patientKey, updatedBytes)

	granteeKey, _ := ctx.GetStub().CreateCompositeKey("GRANTEE_CONSENT", []string{record.GranteeDID, consentID})
	ctx.GetStub().PutState(granteeKey, updatedBytes)

	// Emit event
	eventPayload, _ := json.Marshal(map[string]interface{}{
		"type":       "CONSENT_REVOKED",
		"consentId":  consentID,
		"patientDid": record.PatientDID,
		"granteeDid": record.GranteeDID,
		"reason":     reason,
		"txId":       record.TxID,
	})
	ctx.GetStub().SetEvent("ConsentRevoked", eventPayload)

	return nil
}

// GetConsent retrieves a consent by ID
func (c *ConsentChaincode) GetConsent(ctx contractapi.TransactionContextInterface, consentID string) (string, error) {
	key, err := ctx.GetStub().CreateCompositeKey("CONSENT", []string{consentID})
	if err != nil {
		return "{}", fmt.Errorf("failed to create composite key: %v", err)
	}

	recordBytes, err := ctx.GetStub().GetState(key)
	if err != nil {
		return "{}", fmt.Errorf("failed to read from world state: %v", err)
	}

	if recordBytes == nil {
		return "{}", nil
	}

	return string(recordBytes), nil
}

// VerifyConsent checks if a consent is valid for a specific grantee
func (c *ConsentChaincode) VerifyConsent(ctx contractapi.TransactionContextInterface, consentID string, granteeDID string) (string, error) {
	consentJSON, err := c.GetConsent(ctx, consentID)
	if err != nil {
		return "", err
	}

	if consentJSON == "{}" {
		result, _ := json.Marshal(map[string]interface{}{
			"valid":  false,
			"reason": "consent not found",
		})
		return string(result), nil
	}

	var record ConsentRecord
	json.Unmarshal([]byte(consentJSON), &record)

	// Check status
	if record.Status != "ACTIVE" {
		result, _ := json.Marshal(map[string]interface{}{
			"valid":  false,
			"reason": fmt.Sprintf("consent status is %s", record.Status),
		})
		return string(result), nil
	}

	// Check grantee
	if record.GranteeDID != granteeDID {
		result, _ := json.Marshal(map[string]interface{}{
			"valid":  false,
			"reason": "grantee DID does not match",
		})
		return string(result), nil
	}

	// Check expiry
	if record.ExpiresAt != "" {
		expiresAt, err := time.Parse(time.RFC3339, record.ExpiresAt)
		if err == nil && time.Now().UTC().After(expiresAt) {
			result, _ := json.Marshal(map[string]interface{}{
				"valid":  false,
				"reason": "consent has expired",
			})
			return string(result), nil
		}
	}

	result, _ := json.Marshal(map[string]interface{}{
		"valid":      true,
		"consentId":  consentID,
		"permission": record.Permission,
		"purpose":    record.Purpose,
	})
	return string(result), nil
}

// GetPatientConsents retrieves all consents for a patient
func (c *ConsentChaincode) GetPatientConsents(ctx contractapi.TransactionContextInterface, patientDID string) (string, error) {
	resultsIterator, err := ctx.GetStub().GetStateByPartialCompositeKey("PATIENT_CONSENT", []string{patientDID})
	if err != nil {
		return "[]", fmt.Errorf("failed to get state: %v", err)
	}
	defer resultsIterator.Close()

	var records []ConsentRecord
	for resultsIterator.HasNext() {
		queryResponse, err := resultsIterator.Next()
		if err != nil {
			continue
		}

		var record ConsentRecord
		err = json.Unmarshal(queryResponse.Value, &record)
		if err != nil {
			continue
		}
		records = append(records, record)
	}

	result, _ := json.Marshal(records)
	return string(result), nil
}

// GetConsentHistory retrieves the ledger history for a consent
func (c *ConsentChaincode) GetConsentHistory(ctx contractapi.TransactionContextInterface, consentID string) (string, error) {
	key, err := ctx.GetStub().CreateCompositeKey("CONSENT", []string{consentID})
	if err != nil {
		return "[]", fmt.Errorf("failed to create composite key: %v", err)
	}

	historyIterator, err := ctx.GetStub().GetHistoryForKey(key)
	if err != nil {
		return "[]", fmt.Errorf("failed to get history: %v", err)
	}
	defer historyIterator.Close()

	var history []map[string]interface{}
	for historyIterator.HasNext() {
		modification, err := historyIterator.Next()
		if err != nil {
			continue
		}

		var record ConsentRecord
		json.Unmarshal(modification.Value, &record)

		entry := map[string]interface{}{
			"txId":      modification.TxId,
			"timestamp": modification.Timestamp.AsTime().Format(time.RFC3339),
			"isDelete":  modification.IsDelete,
			"record":    record,
		}
		history = append(history, entry)
	}

	result, _ := json.Marshal(history)
	return string(result), nil
}

func main() {
	chaincode, err := contractapi.NewChaincode(&ConsentChaincode{})
	if err != nil {
		fmt.Printf("Error creating Consent chaincode: %s", err.Error())
		return
	}

	if err := chaincode.Start(); err != nil {
		fmt.Printf("Error starting Consent chaincode: %s", err.Error())
	}
}
