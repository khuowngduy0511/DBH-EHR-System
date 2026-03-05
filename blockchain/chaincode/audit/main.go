// ============================================================================
// DBH-EHR System - Audit Chaincode
// ============================================================================
// Ghi audit trail bất biến lên blockchain
// Functions:
//   - CreateAuditEntry: Ghi audit entry mới
//   - GetAuditEntry: Lấy audit entry theo ID
//   - GetAuditsByPatient: Lấy tất cả audit liên quan đến patient
//   - GetAuditsByActor: Lấy tất cả audit theo actor
//   - GetAuditsByTarget: Lấy audit theo target resource
// ============================================================================

package main

import (
	"encoding/json"
	"fmt"
	"time"

	"github.com/hyperledger/fabric-contract-api-go/contractapi"
)

// AuditChaincode implements chaincode interface
type AuditChaincode struct {
	contractapi.Contract
}

// AuditEntry represents an audit log entry on blockchain
type AuditEntry struct {
	AuditID    string `json:"auditId"`
	ActorDID   string `json:"actorDid"`
	ActorType  string `json:"actorType"`
	Action     string `json:"action"`
	TargetType string `json:"targetType"`
	TargetID   string `json:"targetId"`
	PatientDID string `json:"patientDid,omitempty"`
	Result     string `json:"result"`
	Details    string `json:"details,omitempty"`
	IpAddress  string `json:"ipAddress,omitempty"`
	Timestamp  string `json:"timestamp"`
	TxID       string `json:"txId"`
}

// CreateAuditEntry records a new audit entry on the ledger
func (a *AuditChaincode) CreateAuditEntry(ctx contractapi.TransactionContextInterface, auditID string, entryJSON string) error {
	var entry AuditEntry
	err := json.Unmarshal([]byte(entryJSON), &entry)
	if err != nil {
		return fmt.Errorf("failed to parse entry JSON: %v", err)
	}

	// Composite key: AUDIT_{auditId}
	key, err := ctx.GetStub().CreateCompositeKey("AUDIT", []string{auditID})
	if err != nil {
		return fmt.Errorf("failed to create composite key: %v", err)
	}

	// Check for duplicate
	existing, err := ctx.GetStub().GetState(key)
	if err != nil {
		return fmt.Errorf("failed to read from world state: %v", err)
	}
	if existing != nil {
		return fmt.Errorf("audit entry already exists: %s", auditID)
	}

	// Set metadata
	entry.TxID = ctx.GetStub().GetTxID()
	if entry.Timestamp == "" {
		entry.Timestamp = time.Now().UTC().Format(time.RFC3339)
	}

	entryBytes, err := json.Marshal(entry)
	if err != nil {
		return fmt.Errorf("failed to marshal entry: %v", err)
	}

	// Store by audit ID
	err = ctx.GetStub().PutState(key, entryBytes)
	if err != nil {
		return fmt.Errorf("failed to put state: %v", err)
	}

	// Index by actor DID
	actorKey, _ := ctx.GetStub().CreateCompositeKey("ACTOR_AUDIT", []string{entry.ActorDID, auditID})
	ctx.GetStub().PutState(actorKey, entryBytes)

	// Index by patient DID (if present)
	if entry.PatientDID != "" {
		patientKey, _ := ctx.GetStub().CreateCompositeKey("PATIENT_AUDIT", []string{entry.PatientDID, auditID})
		ctx.GetStub().PutState(patientKey, entryBytes)
	}

	// Index by target
	targetKey, _ := ctx.GetStub().CreateCompositeKey("TARGET_AUDIT", []string{entry.TargetType, entry.TargetID, auditID})
	ctx.GetStub().PutState(targetKey, entryBytes)

	// Emit event
	eventPayload, _ := json.Marshal(map[string]interface{}{
		"type":       "AUDIT_CREATED",
		"auditId":    auditID,
		"actorDid":   entry.ActorDID,
		"action":     entry.Action,
		"targetType": entry.TargetType,
		"targetId":   entry.TargetID,
		"txId":       entry.TxID,
	})
	ctx.GetStub().SetEvent("AuditCreated", eventPayload)

	return nil
}

// GetAuditEntry retrieves an audit entry by ID
func (a *AuditChaincode) GetAuditEntry(ctx contractapi.TransactionContextInterface, auditID string) (string, error) {
	key, err := ctx.GetStub().CreateCompositeKey("AUDIT", []string{auditID})
	if err != nil {
		return "{}", fmt.Errorf("failed to create composite key: %v", err)
	}

	entryBytes, err := ctx.GetStub().GetState(key)
	if err != nil {
		return "{}", fmt.Errorf("failed to read from world state: %v", err)
	}

	if entryBytes == nil {
		return "{}", nil
	}

	return string(entryBytes), nil
}

// GetAuditsByPatient retrieves all audit entries for a patient
func (a *AuditChaincode) GetAuditsByPatient(ctx contractapi.TransactionContextInterface, patientDID string) (string, error) {
	resultsIterator, err := ctx.GetStub().GetStateByPartialCompositeKey("PATIENT_AUDIT", []string{patientDID})
	if err != nil {
		return "[]", fmt.Errorf("failed to get state: %v", err)
	}
	defer resultsIterator.Close()

	var entries []AuditEntry
	for resultsIterator.HasNext() {
		queryResponse, err := resultsIterator.Next()
		if err != nil {
			continue
		}

		var entry AuditEntry
		err = json.Unmarshal(queryResponse.Value, &entry)
		if err != nil {
			continue
		}
		entries = append(entries, entry)
	}

	result, _ := json.Marshal(entries)
	return string(result), nil
}

// GetAuditsByActor retrieves all audit entries by actor
func (a *AuditChaincode) GetAuditsByActor(ctx contractapi.TransactionContextInterface, actorDID string) (string, error) {
	resultsIterator, err := ctx.GetStub().GetStateByPartialCompositeKey("ACTOR_AUDIT", []string{actorDID})
	if err != nil {
		return "[]", fmt.Errorf("failed to get state: %v", err)
	}
	defer resultsIterator.Close()

	var entries []AuditEntry
	for resultsIterator.HasNext() {
		queryResponse, err := resultsIterator.Next()
		if err != nil {
			continue
		}

		var entry AuditEntry
		err = json.Unmarshal(queryResponse.Value, &entry)
		if err != nil {
			continue
		}
		entries = append(entries, entry)
	}

	result, _ := json.Marshal(entries)
	return string(result), nil
}

// GetAuditsByTarget retrieves audit entries by target resource
func (a *AuditChaincode) GetAuditsByTarget(ctx contractapi.TransactionContextInterface, targetType string, targetID string) (string, error) {
	resultsIterator, err := ctx.GetStub().GetStateByPartialCompositeKey("TARGET_AUDIT", []string{targetType, targetID})
	if err != nil {
		return "[]", fmt.Errorf("failed to get state: %v", err)
	}
	defer resultsIterator.Close()

	var entries []AuditEntry
	for resultsIterator.HasNext() {
		queryResponse, err := resultsIterator.Next()
		if err != nil {
			continue
		}

		var entry AuditEntry
		err = json.Unmarshal(queryResponse.Value, &entry)
		if err != nil {
			continue
		}
		entries = append(entries, entry)
	}

	result, _ := json.Marshal(entries)
	return string(result), nil
}

func main() {
	chaincode, err := contractapi.NewChaincode(&AuditChaincode{})
	if err != nil {
		fmt.Printf("Error creating Audit chaincode: %s", err.Error())
		return
	}

	if err := chaincode.Start(); err != nil {
		fmt.Printf("Error starting Audit chaincode: %s", err.Error())
	}
}
