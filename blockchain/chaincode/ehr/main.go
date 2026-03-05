// ============================================================================
// DBH-EHR System - EHR Chaincode
// ============================================================================
// Lưu trữ EHR hash trên blockchain để đảm bảo integrity
// Functions:
//   - CreateEhrHash: Tạo hash record cho EHR version
//   - UpdateEhrHash: Cập nhật hash cho version mới
//   - GetEhrHash: Lấy hash theo ehr_id + version
//   - GetEhrHistory: Lấy toàn bộ lịch sử thay đổi
//   - VerifyEhrIntegrity: Verify hash hiện tại match với blockchain
// ============================================================================

package main

import (
	"encoding/json"
	"fmt"
	"time"

	"github.com/hyperledger/fabric-contract-api-go/contractapi"
)

// EhrChaincode implements chaincode interface
type EhrChaincode struct {
	contractapi.Contract
}

// EhrHashRecord represents an EHR hash stored on blockchain
type EhrHashRecord struct {
	EhrID          string `json:"ehrId"`
	PatientDID     string `json:"patientDid"`
	CreatedByDID   string `json:"createdByDid"`
	OrganizationID string `json:"organizationId"`
	Version        int    `json:"version"`
	ContentHash    string `json:"contentHash"`
	FileHash       string `json:"fileHash"`
	Timestamp      string `json:"timestamp"`
	TxID           string `json:"txId"`
}

// CreateEhrHash creates a new EHR hash record on the ledger
func (c *EhrChaincode) CreateEhrHash(ctx contractapi.TransactionContextInterface, ehrID string, version string, recordJSON string) error {
	var record EhrHashRecord
	err := json.Unmarshal([]byte(recordJSON), &record)
	if err != nil {
		return fmt.Errorf("failed to parse record JSON: %v", err)
	}

	// Composite key: EHR_{ehrId}_{version}
	key, err := ctx.GetStub().CreateCompositeKey("EHR", []string{ehrID, version})
	if err != nil {
		return fmt.Errorf("failed to create composite key: %v", err)
	}

	// Check if key already exists
	existing, err := ctx.GetStub().GetState(key)
	if err != nil {
		return fmt.Errorf("failed to read from world state: %v", err)
	}
	if existing != nil {
		return fmt.Errorf("EHR hash already exists: ehrId=%s, version=%s", ehrID, version)
	}

	// Add transaction metadata
	record.TxID = ctx.GetStub().GetTxID()
	if record.Timestamp == "" {
		record.Timestamp = time.Now().UTC().Format(time.RFC3339)
	}

	recordBytes, err := json.Marshal(record)
	if err != nil {
		return fmt.Errorf("failed to marshal record: %v", err)
	}

	err = ctx.GetStub().PutState(key, recordBytes)
	if err != nil {
		return fmt.Errorf("failed to put state: %v", err)
	}

	// Also store by patient DID for patient-centric queries
	patientKey, _ := ctx.GetStub().CreateCompositeKey("PATIENT_EHR", []string{record.PatientDID, ehrID, version})
	ctx.GetStub().PutState(patientKey, recordBytes)

	// Emit event
	eventPayload, _ := json.Marshal(map[string]interface{}{
		"type":    "EHR_HASH_CREATED",
		"ehrId":   ehrID,
		"version": version,
		"hash":    record.ContentHash,
		"txId":    record.TxID,
	})
	ctx.GetStub().SetEvent("EhrHashCreated", eventPayload)

	return nil
}

// UpdateEhrHash creates a new version hash (EHR versions are immutable, so this creates a new key)
func (c *EhrChaincode) UpdateEhrHash(ctx contractapi.TransactionContextInterface, ehrID string, version string, recordJSON string) error {
	return c.CreateEhrHash(ctx, ehrID, version, recordJSON)
}

// GetEhrHash retrieves an EHR hash by ehrId and version
func (c *EhrChaincode) GetEhrHash(ctx contractapi.TransactionContextInterface, ehrID string, version string) (string, error) {
	key, err := ctx.GetStub().CreateCompositeKey("EHR", []string{ehrID, version})
	if err != nil {
		return "", fmt.Errorf("failed to create composite key: %v", err)
	}

	recordBytes, err := ctx.GetStub().GetState(key)
	if err != nil {
		return "", fmt.Errorf("failed to read from world state: %v", err)
	}

	if recordBytes == nil {
		return "{}", nil
	}

	return string(recordBytes), nil
}

// GetEhrHistory retrieves the full history of an EHR (all versions)
func (c *EhrChaincode) GetEhrHistory(ctx contractapi.TransactionContextInterface, ehrID string) (string, error) {
	resultsIterator, err := ctx.GetStub().GetStateByPartialCompositeKey("EHR", []string{ehrID})
	if err != nil {
		return "[]", fmt.Errorf("failed to get state by partial key: %v", err)
	}
	defer resultsIterator.Close()

	var records []EhrHashRecord
	for resultsIterator.HasNext() {
		queryResponse, err := resultsIterator.Next()
		if err != nil {
			return "[]", fmt.Errorf("failed to iterate: %v", err)
		}

		var record EhrHashRecord
		err = json.Unmarshal(queryResponse.Value, &record)
		if err != nil {
			continue
		}
		records = append(records, record)
	}

	result, err := json.Marshal(records)
	if err != nil {
		return "[]", fmt.Errorf("failed to marshal results: %v", err)
	}

	return string(result), nil
}

// VerifyEhrIntegrity compares provided hash with stored hash
func (c *EhrChaincode) VerifyEhrIntegrity(ctx contractapi.TransactionContextInterface, ehrID string, version string, providedHash string) (string, error) {
	storedJSON, err := c.GetEhrHash(ctx, ehrID, version)
	if err != nil {
		return "", err
	}

	if storedJSON == "{}" {
		result, _ := json.Marshal(map[string]interface{}{
			"valid":   false,
			"reason":  "no record found",
			"ehrId":   ehrID,
			"version": version,
		})
		return string(result), nil
	}

	var stored EhrHashRecord
	json.Unmarshal([]byte(storedJSON), &stored)

	isValid := stored.ContentHash == providedHash
	result, _ := json.Marshal(map[string]interface{}{
		"valid":        isValid,
		"ehrId":        ehrID,
		"version":      version,
		"storedHash":   stored.ContentHash,
		"providedHash": providedHash,
	})

	return string(result), nil
}

func main() {
	chaincode, err := contractapi.NewChaincode(&EhrChaincode{})
	if err != nil {
		fmt.Printf("Error creating EHR chaincode: %s", err.Error())
		return
	}

	if err := chaincode.Start(); err != nil {
		fmt.Printf("Error starting EHR chaincode: %s", err.Error())
	}
}
