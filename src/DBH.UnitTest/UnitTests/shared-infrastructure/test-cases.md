# Shared Infrastructure Test Cases

This document defines scenario coverage for each function declared in the service interfaces.

## IBlockchainSyncService

### EnqueueEhrHash

- Signature: void EnqueueEhrHash(EhrHashRecord record, Func<BlockchainTransactionResult, Task>? onSuccess = null, Func<string, Task>? onFailure = null);
- Return Type: void
- Parameters: EhrHashRecord record, Func<BlockchainTransactionResult, Task>? onSuccess = null, Func<string, Task>? onFailure = null

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| EnqueueEhrHash-01 | HappyPath | Valid record, Func<BlockchainTransactionResult, null, Func<string, null provided | Returns success payload matching declared return type |
| EnqueueEhrHash-02 | InvalidInput | Invalid record OR Invalid Func<BlockchainTransactionResult OR Invalid null OR Invalid Func<string OR Invalid null | Returns validation error (400 or 422) or equivalent domain error |
| EnqueueEhrHash-03 | UnauthorizedOrForbidden | User lacks permission for this action on record, Func<BlockchainTransactionResult, null, Func<string, null | Returns unauthorized or forbidden response, or operation rejected by policy |

### EnqueueConsentGrant

- Signature: void EnqueueConsentGrant(ConsentRecord record, Func<BlockchainTransactionResult, Task>? onSuccess = null, Func<string, Task>? onFailure = null);
- Return Type: void
- Parameters: ConsentRecord record, Func<BlockchainTransactionResult, Task>? onSuccess = null, Func<string, Task>? onFailure = null

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| EnqueueConsentGrant-01 | HappyPath | Valid record, Func<BlockchainTransactionResult, null, Func<string, null provided | Returns success payload matching declared return type |
| EnqueueConsentGrant-02 | InvalidInput | Invalid record OR Invalid Func<BlockchainTransactionResult OR Invalid null OR Invalid Func<string OR Invalid null | Returns validation error (400 or 422) or equivalent domain error |
| EnqueueConsentGrant-03 | UnauthorizedOrForbidden | User lacks permission for this action on record, Func<BlockchainTransactionResult, null, Func<string, null | Returns unauthorized or forbidden response, or operation rejected by policy |

### EnqueueConsentRevoke

- Signature: void EnqueueConsentRevoke(string consentId, string revokedAt, string? reason, Func<BlockchainTransactionResult, Task>? onSuccess = null, Func<string, Task>? onFailure = null);
- Return Type: void
- Parameters: string consentId, string revokedAt, string? reason, Func<BlockchainTransactionResult, Task>? onSuccess = null, Func<string, Task>? onFailure = null

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| EnqueueConsentRevoke-01 | HappyPath | Valid consentId, revokedAt, reason, Func<BlockchainTransactionResult, null, Func<string, null provided | Returns success payload matching declared return type |
| EnqueueConsentRevoke-02 | InvalidInput | consentId = null/empty OR revokedAt = null/empty OR reason = null/empty OR Invalid Func<BlockchainTransactionResult OR Invalid null OR Invalid Func<string OR Invalid null | Returns validation error (400 or 422) or equivalent domain error |
| EnqueueConsentRevoke-03 | UnauthorizedOrForbidden | User lacks permission for this action on consentId, revokedAt, reason, Func<BlockchainTransactionResult, null, Func<string, null | Returns unauthorized or forbidden response, or operation rejected by policy |
| EnqueueConsentRevoke-CONSENTID-EmptyString | InvalidInput | consentId = null or empty string | Returns validation error (400 or 422) |
| EnqueueConsentRevoke-REVOKEDAT-EmptyString | InvalidInput | revokedAt = null or empty string | Returns validation error (400 or 422) |

### EnqueueAuditEntry

- Signature: void EnqueueAuditEntry(AuditEntry entry, Func<BlockchainTransactionResult, Task>? onSuccess = null, Func<string, Task>? onFailure = null);
- Return Type: void
- Parameters: AuditEntry entry, Func<BlockchainTransactionResult, Task>? onSuccess = null, Func<string, Task>? onFailure = null

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| EnqueueAuditEntry-01 | HappyPath | Valid entry, Func<BlockchainTransactionResult, null, Func<string, null provided | Returns success payload matching declared return type |
| EnqueueAuditEntry-02 | InvalidInput | Invalid entry OR Invalid Func<BlockchainTransactionResult OR Invalid null OR Invalid Func<string OR Invalid null | Returns validation error (400 or 422) or equivalent domain error |
| EnqueueAuditEntry-03 | UnauthorizedOrForbidden | User lacks permission for this action on entry, Func<BlockchainTransactionResult, null, Func<string, null | Returns unauthorized or forbidden response, or operation rejected by policy |

### EnqueueFabricCaEnrollment

- Signature: void EnqueueFabricCaEnrollment(string enrollmentId, string username, string role, Func<string, Task>? onFailure = null);
- Return Type: void
- Parameters: string enrollmentId, string username, string role, Func<string, Task>? onFailure = null

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| EnqueueFabricCaEnrollment-01 | HappyPath | Valid enrollmentId, username, role, Func<string, null provided | Returns success payload matching declared return type |
| EnqueueFabricCaEnrollment-02 | InvalidInput | enrollmentId = null/empty OR username = null/empty OR role = null/empty OR Invalid Func<string OR Invalid null | Returns validation error (400 or 422) or equivalent domain error |
| EnqueueFabricCaEnrollment-03 | UnauthorizedOrForbidden | User lacks permission for this action on enrollmentId, username, role, Func<string, null | Returns unauthorized or forbidden response, or operation rejected by policy |
| EnqueueFabricCaEnrollment-ENROLLMENTID-EmptyString | InvalidInput | enrollmentId = null or empty string | Returns validation error (400 or 422) |
| EnqueueFabricCaEnrollment-USERNAME-EmptyString | InvalidInput | username = null or empty string | Returns validation error (400 or 422) |
| EnqueueFabricCaEnrollment-ROLE-EmptyString | InvalidInput | role = null or empty string | Returns validation error (400 or 422) |

## IFabricRuntimeIdentityResolver

### ResolveForCurrentContextAsync

- Signature: Task<FabricRuntimeIdentity> ResolveForCurrentContextAsync(CancellationToken cancellationToken = default);
- Return Type: Task<FabricRuntimeIdentity>
- Parameters: CancellationToken cancellationToken = default

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| ResolveForCurrentContextAsync-01 | HappyPath | Valid default provided | Returns success payload matching declared return type |
| ResolveForCurrentContextAsync-02 | InvalidInput | Invalid default | Returns validation error (400 or 422) or equivalent domain error |

## Blockchain Classes

### EnrollUserAsync

- Signature: Task<FabricEnrollResult> EnrollUserAsync(string enrollmentId, string username, string role, string? secret = null);
- Return Type: Task<FabricEnrollResult>
- Parameters: string enrollmentId, string username, string role, string? secret = null

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| EnrollUserAsync-01 | HappyPath | Valid enrollmentId, username, role, null provided | Returns success payload matching declared return type |
| EnrollUserAsync-02 | InvalidInput | enrollmentId = null/empty OR username = null/empty OR role = null/empty OR null = null/empty | Returns validation error (400 or 422) or equivalent domain error |
| EnrollUserAsync-ENROLLMENTID-EmptyString | InvalidInput | enrollmentId = null or empty string | Returns validation error (400 or 422) |
| EnrollUserAsync-USERNAME-EmptyString | InvalidInput | username = null or empty string | Returns validation error (400 or 422) |
| EnrollUserAsync-ROLE-EmptyString | InvalidInput | role = null or empty string | Returns validation error (400 or 422) |

### SubmitTransactionAsync

- Signature: Task<BlockchainTransactionResult> SubmitTransactionAsync(string channelName, string chaincodeName, string functionName, params string[] args);
- Return Type: Task<BlockchainTransactionResult>
- Parameters: string channelName, string chaincodeName, string functionName, params string[] args

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| SubmitTransactionAsync-01 | HappyPath | Valid channelName, chaincodeName, functionName, args provided | Returns success payload matching declared return type |
| SubmitTransactionAsync-02 | InvalidInput | channelName = null/empty OR chaincodeName = null/empty OR functionName = null/empty OR Invalid args | Returns validation error (400 or 422) or equivalent domain error |
| SubmitTransactionAsync-CHANNELNAME-EmptyString | InvalidInput | channelName = null or empty string | Returns validation error (400 or 422) |
| SubmitTransactionAsync-CHAINCODENAME-EmptyString | InvalidInput | chaincodeName = null or empty string | Returns validation error (400 or 422) |
| SubmitTransactionAsync-FUNCTIONNAME-EmptyString | InvalidInput | functionName = null or empty string | Returns validation error (400 or 422) |

### EvaluateTransactionAsync

- Signature: Task<string> EvaluateTransactionAsync(string channelName, string chaincodeName, string functionName, params string[] args);
- Return Type: Task<string>
- Parameters: string channelName, string chaincodeName, string functionName, params string[] args

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| EvaluateTransactionAsync-01 | HappyPath | Valid channelName, chaincodeName, functionName, args provided | Returns success payload matching declared return type |
| EvaluateTransactionAsync-02 | InvalidInput | channelName = null/empty OR chaincodeName = null/empty OR functionName = null/empty OR Invalid args | Returns validation error (400 or 422) or equivalent domain error |
| EvaluateTransactionAsync-CHANNELNAME-EmptyString | InvalidInput | channelName = null or empty string | Returns validation error (400 or 422) |
| EvaluateTransactionAsync-CHAINCODENAME-EmptyString | InvalidInput | chaincodeName = null or empty string | Returns validation error (400 or 422) |
| EvaluateTransactionAsync-FUNCTIONNAME-EmptyString | InvalidInput | functionName = null or empty string | Returns validation error (400 or 422) |

### IsConnectedAsync

- Signature: Task<bool> IsConnectedAsync();
- Return Type: Task<bool>
- Parameters: 

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| IsConnectedAsync-01 | HappyPath | No parameters, valid state | Returns success payload matching declared return type |
| IsConnectedAsync-02 | InvalidInput | N/A | Returns validation error (400 or 422) or equivalent domain error |
| IsConnectedAsync-03 | BooleanFalsePath | Specific conditions met | Returns false |
| IsConnectedAsync-04 | BooleanTruePath | Specific conditions met | Returns true |

### DisposeAsync

- Signature: ValueTask DisposeAsync();
- Return Type: ValueTask
- Parameters: 

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| DisposeAsync-01 | HappyPath | No parameters, valid state | Returns success payload matching declared return type |
| DisposeAsync-02 | InvalidInput | N/A | Returns validation error (400 or 422) or equivalent domain error |

### CommitAuditEntryAsync

- Signature: Task<BlockchainTransactionResult> CommitAuditEntryAsync(AuditEntry entry);
- Return Type: Task<BlockchainTransactionResult>
- Parameters: AuditEntry entry

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| CommitAuditEntryAsync-01 | HappyPath | Valid entry provided | Returns success payload matching declared return type |
| CommitAuditEntryAsync-02 | InvalidInput | Invalid entry | Returns validation error (400 or 422) or equivalent domain error |

### GetAuditEntryAsync

- Signature: Task<AuditEntry?> GetAuditEntryAsync(string auditId);
- Return Type: Task<AuditEntry?>
- Parameters: string auditId

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetAuditEntryAsync-01 | HappyPath | Valid auditId provided | Returns success payload matching declared return type |
| GetAuditEntryAsync-02 | InvalidInput | auditId = null/empty | Returns validation error (400 or 422) or equivalent domain error |
| GetAuditEntryAsync-03 | NotFoundOrNoData | auditId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| GetAuditEntryAsync-04 | NullableReturn | Valid input, but resource is naturally null | Returns null without throwing |
| GetAuditEntryAsync-AUDITID-EmptyString | InvalidInput | auditId = null or empty string | Returns validation error (400 or 422) |

### GetAuditsByPatientAsync

- Signature: Task<List<AuditEntry>> GetAuditsByPatientAsync(string patientDid);
- Return Type: Task<List<AuditEntry>>
- Parameters: string patientDid

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetAuditsByPatientAsync-01 | HappyPath | Valid patientDid provided | Returns success payload matching declared return type |
| GetAuditsByPatientAsync-02 | InvalidInput | patientDid = null/empty | Returns validation error (400 or 422) or equivalent domain error |
| GetAuditsByPatientAsync-03 | NotFoundOrNoData | Requested resource not found | Returns null, empty, false, or not-found response according to contract |
| GetAuditsByPatientAsync-04 | EmptyCollection | Specific input for EmptyCollection with patientDid | Returns empty collection, not null |
| GetAuditsByPatientAsync-PATIENTDID-EmptyString | InvalidInput | patientDid = null or empty string | Returns validation error (400 or 422) |

### GetAuditsByActorAsync

- Signature: Task<List<AuditEntry>> GetAuditsByActorAsync(string actorDid);
- Return Type: Task<List<AuditEntry>>
- Parameters: string actorDid

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetAuditsByActorAsync-01 | HappyPath | Valid actorDid provided | Returns success payload matching declared return type |
| GetAuditsByActorAsync-02 | InvalidInput | actorDid = null/empty | Returns validation error (400 or 422) or equivalent domain error |
| GetAuditsByActorAsync-03 | NotFoundOrNoData | Requested resource not found | Returns null, empty, false, or not-found response according to contract |
| GetAuditsByActorAsync-04 | EmptyCollection | Specific input for EmptyCollection with actorDid | Returns empty collection, not null |
| GetAuditsByActorAsync-ACTORDID-EmptyString | InvalidInput | actorDid = null or empty string | Returns validation error (400 or 422) |

### GrantConsentAsync

- Signature: Task<BlockchainTransactionResult> GrantConsentAsync(ConsentRecord record);
- Return Type: Task<BlockchainTransactionResult>
- Parameters: ConsentRecord record

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GrantConsentAsync-01 | HappyPath | Valid record provided | Returns success payload matching declared return type |
| GrantConsentAsync-02 | InvalidInput | Invalid record | Returns validation error (400 or 422) or equivalent domain error |
| GrantConsentAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on record | Returns unauthorized or forbidden response, or operation rejected by policy |

### RevokeConsentAsync

- Signature: Task<BlockchainTransactionResult> RevokeConsentAsync(string consentId, string revokedAt, string? reason);
- Return Type: Task<BlockchainTransactionResult>
- Parameters: string consentId, string revokedAt, string? reason

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| RevokeConsentAsync-01 | HappyPath | Valid consentId, revokedAt, reason provided | Returns success payload matching declared return type |
| RevokeConsentAsync-02 | InvalidInput | consentId = null/empty OR revokedAt = null/empty OR reason = null/empty | Returns validation error (400 or 422) or equivalent domain error |
| RevokeConsentAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on consentId, revokedAt, reason | Returns unauthorized or forbidden response, or operation rejected by policy |
| RevokeConsentAsync-CONSENTID-EmptyString | InvalidInput | consentId = null or empty string | Returns validation error (400 or 422) |
| RevokeConsentAsync-REVOKEDAT-EmptyString | InvalidInput | revokedAt = null or empty string | Returns validation error (400 or 422) |

### GetConsentAsync

- Signature: Task<ConsentRecord?> GetConsentAsync(string consentId);
- Return Type: Task<ConsentRecord?>
- Parameters: string consentId

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetConsentAsync-01 | HappyPath | Valid consentId provided | Returns success payload matching declared return type |
| GetConsentAsync-02 | InvalidInput | consentId = null/empty | Returns validation error (400 or 422) or equivalent domain error |
| GetConsentAsync-03 | NotFoundOrNoData | consentId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| GetConsentAsync-04 | NullableReturn | Valid input, but resource is naturally null | Returns null without throwing |
| GetConsentAsync-CONSENTID-EmptyString | InvalidInput | consentId = null or empty string | Returns validation error (400 or 422) |

### VerifyConsentAsync

- Signature: Task<bool> VerifyConsentAsync(string consentId, string granteeDid);
- Return Type: Task<bool>
- Parameters: string consentId, string granteeDid

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| VerifyConsentAsync-01 | HappyPath | Valid consentId, granteeDid provided | Returns success payload matching declared return type |
| VerifyConsentAsync-02 | InvalidInput | consentId = null/empty OR granteeDid = null/empty | Returns validation error (400 or 422) or equivalent domain error |
| VerifyConsentAsync-03 | NotFoundOrNoData | consentId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| VerifyConsentAsync-04 | BooleanFalsePath | Specific input for BooleanFalsePath with consentId, granteeDid | Returns false |
| VerifyConsentAsync-05 | BooleanTruePath | Specific input for BooleanTruePath with consentId, granteeDid | Returns true |
| VerifyConsentAsync-CONSENTID-EmptyString | InvalidInput | consentId = null or empty string | Returns validation error (400 or 422) |
| VerifyConsentAsync-GRANTEEDID-EmptyString | InvalidInput | granteeDid = null or empty string | Returns validation error (400 or 422) |

### GetPatientConsentsAsync

- Signature: Task<List<ConsentRecord>> GetPatientConsentsAsync(string patientDid);
- Return Type: Task<List<ConsentRecord>>
- Parameters: string patientDid

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetPatientConsentsAsync-01 | HappyPath | Valid patientDid provided | Returns success payload matching declared return type |
| GetPatientConsentsAsync-02 | InvalidInput | patientDid = null/empty | Returns validation error (400 or 422) or equivalent domain error |
| GetPatientConsentsAsync-03 | NotFoundOrNoData | Requested resource not found | Returns null, empty, false, or not-found response according to contract |
| GetPatientConsentsAsync-04 | EmptyCollection | Specific input for EmptyCollection with patientDid | Returns empty collection, not null |
| GetPatientConsentsAsync-PATIENTDID-EmptyString | InvalidInput | patientDid = null or empty string | Returns validation error (400 or 422) |

### GetConsentHistoryAsync

- Signature: Task<List<ConsentRecord>> GetConsentHistoryAsync(string consentId);
- Return Type: Task<List<ConsentRecord>>
- Parameters: string consentId

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetConsentHistoryAsync-01 | HappyPath | Valid consentId provided | Returns success payload matching declared return type |
| GetConsentHistoryAsync-02 | InvalidInput | consentId = null/empty | Returns validation error (400 or 422) or equivalent domain error |
| GetConsentHistoryAsync-03 | NotFoundOrNoData | consentId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| GetConsentHistoryAsync-04 | EmptyCollection | Specific input for EmptyCollection with consentId | Returns empty collection, not null |
| GetConsentHistoryAsync-CONSENTID-EmptyString | InvalidInput | consentId = null or empty string | Returns validation error (400 or 422) |

### CommitEhrHashAsync

- Signature: Task<BlockchainTransactionResult> CommitEhrHashAsync(EhrHashRecord record);
- Return Type: Task<BlockchainTransactionResult>
- Parameters: EhrHashRecord record

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| CommitEhrHashAsync-01 | HappyPath | Valid record provided | Returns success payload matching declared return type |
| CommitEhrHashAsync-02 | InvalidInput | Invalid record | Returns validation error (400 or 422) or equivalent domain error |

### GetEhrHashAsync

- Signature: Task<EhrHashRecord?> GetEhrHashAsync(string ehrId, int version);
- Return Type: Task<EhrHashRecord?>
- Parameters: string ehrId, int version

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetEhrHashAsync-01 | HappyPath | Valid ehrId, version provided | Returns success payload matching declared return type |
| GetEhrHashAsync-02 | InvalidInput | ehrId = null/empty OR version <= 0 | Returns validation error (400 or 422) or equivalent domain error |
| GetEhrHashAsync-03 | NotFoundOrNoData | ehrId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| GetEhrHashAsync-04 | NullableReturn | Valid input, but resource is naturally null | Returns null without throwing |
| GetEhrHashAsync-EHRID-EmptyString | InvalidInput | ehrId = null or empty string | Returns validation error (400 or 422) |

### GetEhrHistoryAsync

- Signature: Task<List<EhrHashRecord>> GetEhrHistoryAsync(string ehrId);
- Return Type: Task<List<EhrHashRecord>>
- Parameters: string ehrId

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetEhrHistoryAsync-01 | HappyPath | Valid ehrId provided | Returns success payload matching declared return type |
| GetEhrHistoryAsync-02 | InvalidInput | ehrId = null/empty | Returns validation error (400 or 422) or equivalent domain error |
| GetEhrHistoryAsync-03 | NotFoundOrNoData | ehrId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| GetEhrHistoryAsync-04 | EmptyCollection | Specific input for EmptyCollection with ehrId | Returns empty collection, not null |
| GetEhrHistoryAsync-EHRID-EmptyString | InvalidInput | ehrId = null or empty string | Returns validation error (400 or 422) |

### VerifyEhrIntegrityAsync

- Signature: Task<bool> VerifyEhrIntegrityAsync(string ehrId, int version, string currentHash);
- Return Type: Task<bool>
- Parameters: string ehrId, int version, string currentHash

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| VerifyEhrIntegrityAsync-01 | HappyPath | Valid ehrId, version, currentHash provided | Returns success payload matching declared return type |
| VerifyEhrIntegrityAsync-02 | InvalidInput | ehrId = null/empty OR version <= 0 OR currentHash = null/empty | Returns validation error (400 or 422) or equivalent domain error |
| VerifyEhrIntegrityAsync-03 | NotFoundOrNoData | ehrId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| VerifyEhrIntegrityAsync-04 | BooleanFalsePath | Specific input for BooleanFalsePath with ehrId, version, currentHash | Returns false |
| VerifyEhrIntegrityAsync-05 | BooleanTruePath | Specific input for BooleanTruePath with ehrId, version, currentHash | Returns true |
| VerifyEhrIntegrityAsync-EHRID-EmptyString | InvalidInput | ehrId = null or empty string | Returns validation error (400 or 422) |
| VerifyEhrIntegrityAsync-CURRENTHASH-EmptyString | InvalidInput | currentHash = null or empty string | Returns validation error (400 or 422) |

## Cryptography Classes

### EncryptString

- Signature: static string EncryptString(string plainText, byte[] key);
- Return Type: static string
- Parameters: string plainText, byte[] key

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| EncryptString-01 | HappyPath | Valid plainText, key provided | Returns success payload matching declared return type |
| EncryptString-02 | InvalidInput | plainText = null/empty OR Invalid key | Returns validation error (400 or 422) or equivalent domain error |
| EncryptString-03 | UnauthorizedOrForbidden | User lacks permission for this action on plainText, key | Returns unauthorized or forbidden response, or operation rejected by policy |
| EncryptString-PLAINTEXT-EmptyString | InvalidInput | plainText = null or empty string | Returns validation error (400 or 422) |

### DecryptString

- Signature: static string DecryptString(string cipherTextBase64, byte[] key);
- Return Type: static string
- Parameters: string cipherTextBase64, byte[] key

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| DecryptString-01 | HappyPath | Valid cipherTextBase64, key provided | Returns success payload matching declared return type |
| DecryptString-02 | InvalidInput | cipherTextBase64 = null/empty OR Invalid key | Returns validation error (400 or 422) or equivalent domain error |
| DecryptString-03 | NotFoundOrNoData | Requested resource not found | Returns null, empty, false, or not-found response according to contract |
| DecryptString-CIPHERTEXTBASE64-EmptyString | InvalidInput | cipherTextBase64 = null or empty string | Returns validation error (400 or 422) |

### GenerateKeyPair

- Signature: static (string PublicKey, string PrivateKey) GenerateKeyPair();
- Return Type: static (string PublicKey, string PrivateKey)
- Parameters: 

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GenerateKeyPair-01 | HappyPath | No parameters, valid state | Returns success payload matching declared return type |
| GenerateKeyPair-02 | InvalidInput | N/A | Returns validation error (400 or 422) or equivalent domain error |
| GenerateKeyPair-03 | TupleFlags | Specific conditions met | Tuple field values and message fields match scenario |

### WrapKey

- Signature: static string WrapKey(byte[] aesKey, string recipientPublicKeyBase64);
- Return Type: static string
- Parameters: byte[] aesKey, string recipientPublicKeyBase64

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| WrapKey-01 | HappyPath | Valid aesKey, recipientPublicKeyBase64 provided | Returns success payload matching declared return type |
| WrapKey-02 | InvalidInput | Invalid aesKey OR recipientPublicKeyBase64 = null/empty | Returns validation error (400 or 422) or equivalent domain error |
| WrapKey-RECIPIENTPUBLICKEYBASE64-EmptyString | InvalidInput | recipientPublicKeyBase64 = null or empty string | Returns validation error (400 or 422) |

### WrapKeyBase64

- Signature: static string WrapKeyBase64(string aesKeyBase64, string recipientPublicKeyBase64);
- Return Type: static string
- Parameters: string aesKeyBase64, string recipientPublicKeyBase64

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| WrapKeyBase64-01 | HappyPath | Valid aesKeyBase64, recipientPublicKeyBase64 provided | Returns success payload matching declared return type |
| WrapKeyBase64-02 | InvalidInput | aesKeyBase64 = null/empty OR recipientPublicKeyBase64 = null/empty | Returns validation error (400 or 422) or equivalent domain error |
| WrapKeyBase64-AESKEYBASE64-EmptyString | InvalidInput | aesKeyBase64 = null or empty string | Returns validation error (400 or 422) |
| WrapKeyBase64-RECIPIENTPUBLICKEYBASE64-EmptyString | InvalidInput | recipientPublicKeyBase64 = null or empty string | Returns validation error (400 or 422) |

### UnwrapKey

- Signature: static byte[] UnwrapKey(string wrappedKeyBase64, string ownerPrivateKeyBase64);
- Return Type: static byte[]
- Parameters: string wrappedKeyBase64, string ownerPrivateKeyBase64

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| UnwrapKey-01 | HappyPath | Valid wrappedKeyBase64, ownerPrivateKeyBase64 provided | Returns success payload matching declared return type |
| UnwrapKey-02 | InvalidInput | wrappedKeyBase64 = null/empty OR ownerPrivateKeyBase64 = null/empty | Returns validation error (400 or 422) or equivalent domain error |
| UnwrapKey-WRAPPEDKEYBASE64-EmptyString | InvalidInput | wrappedKeyBase64 = null or empty string | Returns validation error (400 or 422) |
| UnwrapKey-OWNERPRIVATEKEYBASE64-EmptyString | InvalidInput | ownerPrivateKeyBase64 = null or empty string | Returns validation error (400 or 422) |

### UnwrapKeyBase64

- Signature: static string UnwrapKeyBase64(string wrappedKeyBase64, string ownerPrivateKeyBase64);
- Return Type: static string
- Parameters: string wrappedKeyBase64, string ownerPrivateKeyBase64

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| UnwrapKeyBase64-01 | HappyPath | Valid wrappedKeyBase64, ownerPrivateKeyBase64 provided | Returns success payload matching declared return type |
| UnwrapKeyBase64-02 | InvalidInput | wrappedKeyBase64 = null/empty OR ownerPrivateKeyBase64 = null/empty | Returns validation error (400 or 422) or equivalent domain error |
| UnwrapKeyBase64-WRAPPEDKEYBASE64-EmptyString | InvalidInput | wrappedKeyBase64 = null or empty string | Returns validation error (400 or 422) |
| UnwrapKeyBase64-OWNERPRIVATEKEYBASE64-EmptyString | InvalidInput | ownerPrivateKeyBase64 = null or empty string | Returns validation error (400 or 422) |

### Encrypt

- Signature: static string Encrypt(string plainText);
- Return Type: static string
- Parameters: string plainText

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| Encrypt-01 | HappyPath | Valid plainText provided | Returns success payload matching declared return type |
| Encrypt-02 | InvalidInput | plainText = null/empty | Returns validation error (400 or 422) or equivalent domain error |
| Encrypt-03 | UnauthorizedOrForbidden | User lacks permission for this action on plainText | Returns unauthorized or forbidden response, or operation rejected by policy |
| Encrypt-PLAINTEXT-EmptyString | InvalidInput | plainText = null or empty string | Returns validation error (400 or 422) |

### Decrypt

- Signature: static string Decrypt(string cipherText);
- Return Type: static string
- Parameters: string cipherText

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| Decrypt-01 | HappyPath | Valid cipherText provided | Returns success payload matching declared return type |
| Decrypt-02 | InvalidInput | cipherText = null/empty | Returns validation error (400 or 422) or equivalent domain error |
| Decrypt-03 | NotFoundOrNoData | Requested resource not found | Returns null, empty, false, or not-found response according to contract |
| Decrypt-CIPHERTEXT-EmptyString | InvalidInput | cipherText = null or empty string | Returns validation error (400 or 422) |

### EncryptFile

- Signature: static bool EncryptFile(string inputFile, string password);
- Return Type: static bool
- Parameters: string inputFile, string password

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| EncryptFile-01 | HappyPath | Valid inputFile, password provided | Returns success payload matching declared return type |
| EncryptFile-02 | InvalidInput | inputFile = null/empty OR password = null/empty | Returns validation error (400 or 422) or equivalent domain error |
| EncryptFile-03 | UnauthorizedOrForbidden | User lacks permission for this action on inputFile, password | Returns unauthorized or forbidden response, or operation rejected by policy |
| EncryptFile-04 | BooleanFalsePath | Specific input for BooleanFalsePath with inputFile, password | Returns false |
| EncryptFile-05 | BooleanTruePath | Specific input for BooleanTruePath with inputFile, password | Returns true |
| EncryptFile-INPUTFILE-EmptyString | InvalidInput | inputFile = null or empty string | Returns validation error (400 or 422) |
| EncryptFile-PASSWORD-EmptyString | InvalidInput | password = null or empty string | Returns validation error (400 or 422) |

### DecryptFile

- Signature: static bool DecryptFile(string inputFile, string password);
- Return Type: static bool
- Parameters: string inputFile, string password

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| DecryptFile-01 | HappyPath | Valid inputFile, password provided | Returns success payload matching declared return type |
| DecryptFile-02 | InvalidInput | inputFile = null/empty OR password = null/empty | Returns validation error (400 or 422) or equivalent domain error |
| DecryptFile-03 | NotFoundOrNoData | Requested resource not found | Returns null, empty, false, or not-found response according to contract |
| DecryptFile-04 | BooleanFalsePath | Specific input for BooleanFalsePath with inputFile, password | Returns false |
| DecryptFile-05 | BooleanTruePath | Specific input for BooleanTruePath with inputFile, password | Returns true |
| DecryptFile-INPUTFILE-EmptyString | InvalidInput | inputFile = null or empty string | Returns validation error (400 or 422) |
| DecryptFile-PASSWORD-EmptyString | InvalidInput | password = null or empty string | Returns validation error (400 or 422) |

## IPFS Classes

### UploadFileAsync

- Signature: Task<IpfsUploadResponse?> UploadFileAsync(string filePath);
- Return Type: Task<IpfsUploadResponse?>
- Parameters: string filePath

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| UploadFileAsync-01 | HappyPath | Valid filePath provided | Returns success payload matching declared return type |
| UploadFileAsync-02 | InvalidInput | filePath = null/empty | Returns validation error (400 or 422) or equivalent domain error |
| UploadFileAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on filePath | Returns unauthorized or forbidden response, or operation rejected by policy |
| UploadFileAsync-04 | NullableReturn | Valid input, but resource is naturally null | Returns null without throwing |
| UploadFileAsync-FILEPATH-EmptyString | InvalidInput | filePath = null or empty string | Returns validation error (400 or 422) |

### RetrieveFileAsync

- Signature: Task<IpfsFileResponse> RetrieveFileAsync(string cid);
- Return Type: Task<IpfsFileResponse>
- Parameters: string cid

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| RetrieveFileAsync-01 | HappyPath | Valid cid provided | Returns success payload matching declared return type |
| RetrieveFileAsync-02 | InvalidInput | cid = null/empty | Returns validation error (400 or 422) or equivalent domain error |
| RetrieveFileAsync-CID-EmptyString | InvalidInput | cid = null or empty string | Returns validation error (400 or 422) |

### Dispose

- Signature: void Dispose();
- Return Type: void
- Parameters: 

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| Dispose-01 | HappyPath | No parameters, valid state | Returns success payload matching declared return type |
| Dispose-02 | InvalidInput | N/A | Returns validation error (400 or 422) or equivalent domain error |

### GetExtensionForContentType

- Signature: static string GetExtensionForContentType(string? contentType);
- Return Type: static string
- Parameters: string? contentType

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetExtensionForContentType-01 | HappyPath | Valid contentType provided | Returns success payload matching declared return type |
| GetExtensionForContentType-02 | InvalidInput | contentType = null/empty | Returns validation error (400 or 422) or equivalent domain error |
| GetExtensionForContentType-03 | NotFoundOrNoData | Requested resource not found | Returns null, empty, false, or not-found response according to contract |

### UploadAsync

- Signature: static Task<IpfsUploadResponse?> UploadAsync(string filePath, IpfsConfig? config = null);
- Return Type: static Task<IpfsUploadResponse?>
- Parameters: string filePath, IpfsConfig? config = null

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| UploadAsync-01 | HappyPath | Valid filePath, null provided | Returns success payload matching declared return type |
| UploadAsync-02 | InvalidInput | filePath = null/empty OR Invalid null | Returns validation error (400 or 422) or equivalent domain error |
| UploadAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on filePath, null | Returns unauthorized or forbidden response, or operation rejected by policy |
| UploadAsync-04 | NullableReturn | Valid input, but resource is naturally null | Returns null without throwing |
| UploadAsync-FILEPATH-EmptyString | InvalidInput | filePath = null or empty string | Returns validation error (400 or 422) |

### RetrieveAsync

- Signature: static Task<string> RetrieveAsync(string cid, string? outPath = null, IpfsConfig? config = null);
- Return Type: static Task<string>
- Parameters: string cid, string? outPath = null, IpfsConfig? config = null

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| RetrieveAsync-01 | HappyPath | Valid cid, null, null provided | Returns success payload matching declared return type |
| RetrieveAsync-02 | InvalidInput | cid = null/empty OR null = null/empty OR Invalid null | Returns validation error (400 or 422) or equivalent domain error |
| RetrieveAsync-CID-EmptyString | InvalidInput | cid = null or empty string | Returns validation error (400 or 422) |

### LoadConfig

- Signature: static IpfsConfig LoadConfig(string path = "appsettings.json");
- Return Type: static IpfsConfig
- Parameters: string path = "appsettings.json"

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| LoadConfig-01 | HappyPath | Valid "appsettings.json" provided | Returns success payload matching declared return type |
| LoadConfig-02 | InvalidInput | "appsettings.json" = null/empty | Returns validation error (400 or 422) or equivalent domain error |
| LoadConfig-PATH-EmptyString | InvalidInput | path = null or empty string | Returns validation error (400 or 422) |

### UploadAsync

- Signature: Task<IpfsUploadResponse?> UploadAsync(string filePath);
- Return Type: Task<IpfsUploadResponse?>
- Parameters: string filePath

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| UploadAsync-01 | HappyPath | Valid filePath provided | Returns success payload matching declared return type |
| UploadAsync-02 | InvalidInput | filePath = null/empty | Returns validation error (400 or 422) or equivalent domain error |
| UploadAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on filePath | Returns unauthorized or forbidden response, or operation rejected by policy |
| UploadAsync-04 | NullableReturn | Valid input, but resource is naturally null | Returns null without throwing |
| UploadAsync-FILEPATH-EmptyString | InvalidInput | filePath = null or empty string | Returns validation error (400 or 422) |

### EncryptAndUploadAsync

- Signature: Task<IpfsUploadResponse?> EncryptAndUploadAsync(string filePath, string password);
- Return Type: Task<IpfsUploadResponse?>
- Parameters: string filePath, string password

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| EncryptAndUploadAsync-01 | HappyPath | Valid filePath, password provided | Returns success payload matching declared return type |
| EncryptAndUploadAsync-02 | InvalidInput | filePath = null/empty OR password = null/empty | Returns validation error (400 or 422) or equivalent domain error |
| EncryptAndUploadAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on filePath, password | Returns unauthorized or forbidden response, or operation rejected by policy |
| EncryptAndUploadAsync-04 | NullableReturn | Valid input, but resource is naturally null | Returns null without throwing |
| EncryptAndUploadAsync-FILEPATH-EmptyString | InvalidInput | filePath = null or empty string | Returns validation error (400 or 422) |
| EncryptAndUploadAsync-PASSWORD-EmptyString | InvalidInput | password = null or empty string | Returns validation error (400 or 422) |

### EncryptFile

- Signature: bool EncryptFile(string filePath, string password);
- Return Type: bool
- Parameters: string filePath, string password

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| EncryptFile-01 | HappyPath | Valid filePath, password provided | Returns success payload matching declared return type |
| EncryptFile-02 | InvalidInput | filePath = null/empty OR password = null/empty | Returns validation error (400 or 422) or equivalent domain error |
| EncryptFile-03 | UnauthorizedOrForbidden | User lacks permission for this action on filePath, password | Returns unauthorized or forbidden response, or operation rejected by policy |
| EncryptFile-04 | BooleanFalsePath | Specific input for BooleanFalsePath with filePath, password | Returns false |
| EncryptFile-05 | BooleanTruePath | Specific input for BooleanTruePath with filePath, password | Returns true |
| EncryptFile-FILEPATH-EmptyString | InvalidInput | filePath = null or empty string | Returns validation error (400 or 422) |
| EncryptFile-PASSWORD-EmptyString | InvalidInput | password = null or empty string | Returns validation error (400 or 422) |

### DecryptFile

- Signature: bool DecryptFile(string encryptedFilePath, string password);
- Return Type: bool
- Parameters: string encryptedFilePath, string password

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| DecryptFile-01 | HappyPath | Valid encryptedFilePath, password provided | Returns success payload matching declared return type |
| DecryptFile-02 | InvalidInput | encryptedFilePath = null/empty OR password = null/empty | Returns validation error (400 or 422) or equivalent domain error |
| DecryptFile-03 | NotFoundOrNoData | Requested resource not found | Returns null, empty, false, or not-found response according to contract |
| DecryptFile-04 | BooleanFalsePath | Specific input for BooleanFalsePath with encryptedFilePath, password | Returns false |
| DecryptFile-05 | BooleanTruePath | Specific input for BooleanTruePath with encryptedFilePath, password | Returns true |
| DecryptFile-ENCRYPTEDFILEPATH-EmptyString | InvalidInput | encryptedFilePath = null or empty string | Returns validation error (400 or 422) |
| DecryptFile-PASSWORD-EmptyString | InvalidInput | password = null or empty string | Returns validation error (400 or 422) |

### RetrieveAsync

- Signature: Task<IpfsFileResponse> RetrieveAsync(string cid);
- Return Type: Task<IpfsFileResponse>
- Parameters: string cid

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| RetrieveAsync-01 | HappyPath | Valid cid provided | Returns success payload matching declared return type |
| RetrieveAsync-02 | InvalidInput | cid = null/empty | Returns validation error (400 or 422) or equivalent domain error |
| RetrieveAsync-CID-EmptyString | InvalidInput | cid = null or empty string | Returns validation error (400 or 422) |

### RetrieveAndDecryptAsync

- Signature: Task<string?> RetrieveAndDecryptAsync(string cid, string password, string? encryptedOutPath = null);
- Return Type: Task<string?>
- Parameters: string cid, string password, string? encryptedOutPath = null

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| RetrieveAndDecryptAsync-01 | HappyPath | Valid cid, password, null provided | Returns success payload matching declared return type |
| RetrieveAndDecryptAsync-02 | InvalidInput | cid = null/empty OR password = null/empty OR null = null/empty | Returns validation error (400 or 422) or equivalent domain error |
| RetrieveAndDecryptAsync-03 | NotFoundOrNoData | Requested resource not found | Returns null, empty, false, or not-found response according to contract |
| RetrieveAndDecryptAsync-04 | NullableReturn | Valid input, but resource is naturally null | Returns null without throwing |
| RetrieveAndDecryptAsync-CID-EmptyString | InvalidInput | cid = null or empty string | Returns validation error (400 or 422) |
| RetrieveAndDecryptAsync-PASSWORD-EmptyString | InvalidInput | password = null or empty string | Returns validation error (400 or 422) |
