# Consent Service Test Cases

This document defines scenario coverage for each function declared in the service interfaces.

## IConsentService

### GrantConsentAsync

- Signature: Task<ApiResponse<ConsentResponse>> GrantConsentAsync(GrantConsentRequest request);
- Return Type: Task<ApiResponse<ConsentResponse>>
- Parameters: GrantConsentRequest request

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GrantConsentAsync-01 | HappyPath | Valid request provided | Returns success payload matching declared return type |
| GrantConsentAsync-02 | InvalidInput | request with missing required fields | Returns validation error (400 or 422) or equivalent domain error |
| GrantConsentAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on request | Returns unauthorized or forbidden response, or operation rejected by policy |

### GetConsentByIdAsync

- Signature: Task<ApiResponse<ConsentResponse>> GetConsentByIdAsync(Guid consentId);
- Return Type: Task<ApiResponse<ConsentResponse>>
- Parameters: Guid consentId

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetConsentByIdAsync-01 | HappyPath | Valid consentId provided | Returns success payload matching declared return type |
| GetConsentByIdAsync-02 | InvalidInput | consentId = Guid.Empty | Returns validation error (400 or 422) or equivalent domain error |
| GetConsentByIdAsync-03 | NotFoundOrNoData | consentId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| GetConsentByIdAsync-CONSENTID-EmptyGuid | InvalidInput | consentId = Guid.Empty | Returns validation error (400 or 422) |

### GetConsentsByPatientAsync

- Signature: Task<PagedResponse<ConsentResponse>> GetConsentsByPatientAsync(Guid patientId, int page = 1, int pageSize = 10);
- Return Type: Task<PagedResponse<ConsentResponse>>
- Parameters: Guid patientId, int page = 1, int pageSize = 10

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetConsentsByPatientAsync-01 | HappyPath | Valid patientId, 1, 10 provided | Returns success payload matching declared return type |
| GetConsentsByPatientAsync-02 | InvalidInput | patientId = Guid.Empty OR 1 <= 0 OR 10 <= 0 | Returns validation error (400 or 422) or equivalent domain error |
| GetConsentsByPatientAsync-03 | NotFoundOrNoData | patientId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| GetConsentsByPatientAsync-04 | PagingBoundary | Large page number or page size out of bounds | Returns valid paging metadata; out-of-range page returns empty item set |
| GetConsentsByPatientAsync-PATIENTID-EmptyGuid | InvalidInput | patientId = Guid.Empty | Returns validation error (400 or 422) |
| GetConsentsByPatientAsync-PAGE-ZeroOrNegative | InvalidInput | page <= 0 | Returns validation error (400 or 422) |
| GetConsentsByPatientAsync-PAGESIZE-ZeroOrNegative | InvalidInput | pageSize <= 0 | Returns validation error (400 or 422) |

### GetConsentsByGranteeAsync

- Signature: Task<PagedResponse<ConsentResponse>> GetConsentsByGranteeAsync(Guid granteeId, int page = 1, int pageSize = 10);
- Return Type: Task<PagedResponse<ConsentResponse>>
- Parameters: Guid granteeId, int page = 1, int pageSize = 10

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetConsentsByGranteeAsync-01 | HappyPath | Valid granteeId, 1, 10 provided | Returns success payload matching declared return type |
| GetConsentsByGranteeAsync-02 | InvalidInput | granteeId = Guid.Empty OR 1 <= 0 OR 10 <= 0 | Returns validation error (400 or 422) or equivalent domain error |
| GetConsentsByGranteeAsync-03 | NotFoundOrNoData | granteeId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| GetConsentsByGranteeAsync-04 | UnauthorizedOrForbidden | User lacks permission for this action on granteeId, 1, 10 | Returns unauthorized or forbidden response, or operation rejected by policy |
| GetConsentsByGranteeAsync-05 | PagingBoundary | Large page number or page size out of bounds | Returns valid paging metadata; out-of-range page returns empty item set |
| GetConsentsByGranteeAsync-GRANTEEID-EmptyGuid | InvalidInput | granteeId = Guid.Empty | Returns validation error (400 or 422) |
| GetConsentsByGranteeAsync-PAGE-ZeroOrNegative | InvalidInput | page <= 0 | Returns validation error (400 or 422) |
| GetConsentsByGranteeAsync-PAGESIZE-ZeroOrNegative | InvalidInput | pageSize <= 0 | Returns validation error (400 or 422) |

### SearchConsentsAsync

- Signature: Task<PagedResponse<ConsentResponse>> SearchConsentsAsync(ConsentQueryParams query);
- Return Type: Task<PagedResponse<ConsentResponse>>
- Parameters: ConsentQueryParams query

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| SearchConsentsAsync-01 | HappyPath | Valid query provided | Returns success payload matching declared return type |
| SearchConsentsAsync-02 | InvalidInput | query with missing required fields | Returns validation error (400 or 422) or equivalent domain error |
| SearchConsentsAsync-03 | NotFoundOrNoData | Requested resource not found | Returns null, empty, false, or not-found response according to contract |
| SearchConsentsAsync-04 | PagingBoundary | Large page number or page size out of bounds | Returns valid paging metadata; out-of-range page returns empty item set |

### RevokeConsentAsync

- Signature: Task<ApiResponse<ConsentResponse>> RevokeConsentAsync(Guid consentId, RevokeConsentRequest request);
- Return Type: Task<ApiResponse<ConsentResponse>>
- Parameters: Guid consentId, RevokeConsentRequest request

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| RevokeConsentAsync-01 | HappyPath | Valid consentId, request provided | Returns success payload matching declared return type |
| RevokeConsentAsync-02 | InvalidInput | consentId = Guid.Empty OR request with missing required fields | Returns validation error (400 or 422) or equivalent domain error |
| RevokeConsentAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on consentId, request | Returns unauthorized or forbidden response, or operation rejected by policy |
| RevokeConsentAsync-CONSENTID-EmptyGuid | InvalidInput | consentId = Guid.Empty | Returns validation error (400 or 422) |

### VerifyConsentAsync

- Signature: Task<VerifyConsentResponse> VerifyConsentAsync(VerifyConsentRequest request);
- Return Type: Task<VerifyConsentResponse>
- Parameters: VerifyConsentRequest request

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| VerifyConsentAsync-01 | HappyPath | Valid request provided | Returns success payload matching declared return type |
| VerifyConsentAsync-02 | InvalidInput | request with missing required fields | Returns validation error (400 or 422) or equivalent domain error |
| VerifyConsentAsync-03 | NotFoundOrNoData | Requested resource not found | Returns null, empty, false, or not-found response according to contract |

### SyncFromBlockchainAsync

- Signature: Task<ApiResponse<ConsentResponse>> SyncFromBlockchainAsync(string blockchainConsentId);
- Return Type: Task<ApiResponse<ConsentResponse>>
- Parameters: string blockchainConsentId

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| SyncFromBlockchainAsync-01 | HappyPath | Valid blockchainConsentId provided | Returns success payload matching declared return type |
| SyncFromBlockchainAsync-02 | InvalidInput | blockchainConsentId = null/empty | Returns validation error (400 or 422) or equivalent domain error |
| SyncFromBlockchainAsync-BLOCKCHAINCONSENTID-EmptyString | InvalidInput | blockchainConsentId = null or empty string | Returns validation error (400 or 422) |

### CreateAccessRequestAsync

- Signature: Task<ApiResponse<AccessRequestResponse>> CreateAccessRequestAsync(CreateAccessRequestDto request);
- Return Type: Task<ApiResponse<AccessRequestResponse>>
- Parameters: CreateAccessRequestDto request

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| CreateAccessRequestAsync-01 | HappyPath | Valid request provided | Returns success payload matching declared return type |
| CreateAccessRequestAsync-02 | InvalidInput | request with missing required fields | Returns validation error (400 or 422) or equivalent domain error |
| CreateAccessRequestAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on request | Returns unauthorized or forbidden response, or operation rejected by policy |

### GetAccessRequestByIdAsync

- Signature: Task<ApiResponse<AccessRequestResponse>> GetAccessRequestByIdAsync(Guid requestId);
- Return Type: Task<ApiResponse<AccessRequestResponse>>
- Parameters: Guid requestId

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetAccessRequestByIdAsync-01 | HappyPath | Valid requestId provided | Returns success payload matching declared return type |
| GetAccessRequestByIdAsync-02 | InvalidInput | requestId = Guid.Empty | Returns validation error (400 or 422) or equivalent domain error |
| GetAccessRequestByIdAsync-03 | NotFoundOrNoData | requestId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| GetAccessRequestByIdAsync-REQUESTID-EmptyGuid | InvalidInput | requestId = Guid.Empty | Returns validation error (400 or 422) |

### GetAccessRequestsByPatientAsync

- Signature: Task<PagedResponse<AccessRequestResponse>> GetAccessRequestsByPatientAsync(Guid patientId, AccessRequestStatus? status = null, int page = 1, int pageSize = 10);
- Return Type: Task<PagedResponse<AccessRequestResponse>>
- Parameters: Guid patientId, AccessRequestStatus? status = null, int page = 1, int pageSize = 10

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetAccessRequestsByPatientAsync-01 | HappyPath | Valid patientId, null, 1, 10 provided | Returns success payload matching declared return type |
| GetAccessRequestsByPatientAsync-02 | InvalidInput | patientId = Guid.Empty OR null with missing required fields OR 1 <= 0 OR 10 <= 0 | Returns validation error (400 or 422) or equivalent domain error |
| GetAccessRequestsByPatientAsync-03 | NotFoundOrNoData | patientId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| GetAccessRequestsByPatientAsync-04 | PagingBoundary | Large page number or page size out of bounds | Returns valid paging metadata; out-of-range page returns empty item set |
| GetAccessRequestsByPatientAsync-PATIENTID-EmptyGuid | InvalidInput | patientId = Guid.Empty | Returns validation error (400 or 422) |
| GetAccessRequestsByPatientAsync-PAGE-ZeroOrNegative | InvalidInput | page <= 0 | Returns validation error (400 or 422) |
| GetAccessRequestsByPatientAsync-PAGESIZE-ZeroOrNegative | InvalidInput | pageSize <= 0 | Returns validation error (400 or 422) |

### GetAccessRequestsByRequesterAsync

- Signature: Task<PagedResponse<AccessRequestResponse>> GetAccessRequestsByRequesterAsync(Guid requesterId, AccessRequestStatus? status = null, int page = 1, int pageSize = 10);
- Return Type: Task<PagedResponse<AccessRequestResponse>>
- Parameters: Guid requesterId, AccessRequestStatus? status = null, int page = 1, int pageSize = 10

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetAccessRequestsByRequesterAsync-01 | HappyPath | Valid requesterId, null, 1, 10 provided | Returns success payload matching declared return type |
| GetAccessRequestsByRequesterAsync-02 | InvalidInput | requesterId = Guid.Empty OR null with missing required fields OR 1 <= 0 OR 10 <= 0 | Returns validation error (400 or 422) or equivalent domain error |
| GetAccessRequestsByRequesterAsync-03 | NotFoundOrNoData | requesterId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| GetAccessRequestsByRequesterAsync-04 | PagingBoundary | Large page number or page size out of bounds | Returns valid paging metadata; out-of-range page returns empty item set |
| GetAccessRequestsByRequesterAsync-REQUESTERID-EmptyGuid | InvalidInput | requesterId = Guid.Empty | Returns validation error (400 or 422) |
| GetAccessRequestsByRequesterAsync-PAGE-ZeroOrNegative | InvalidInput | page <= 0 | Returns validation error (400 or 422) |
| GetAccessRequestsByRequesterAsync-PAGESIZE-ZeroOrNegative | InvalidInput | pageSize <= 0 | Returns validation error (400 or 422) |

### RespondToAccessRequestAsync

- Signature: Task<ApiResponse<AccessRequestResponse>> RespondToAccessRequestAsync(Guid requestId, RespondAccessRequestDto response);
- Return Type: Task<ApiResponse<AccessRequestResponse>>
- Parameters: Guid requestId, RespondAccessRequestDto response

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| RespondToAccessRequestAsync-01 | HappyPath | Valid requestId, response provided | Returns success payload matching declared return type |
| RespondToAccessRequestAsync-02 | InvalidInput | requestId = Guid.Empty OR response with missing required fields | Returns validation error (400 or 422) or equivalent domain error |
| RespondToAccessRequestAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on requestId, response | Returns unauthorized or forbidden response, or operation rejected by policy |
| RespondToAccessRequestAsync-REQUESTID-EmptyGuid | InvalidInput | requestId = Guid.Empty | Returns validation error (400 or 422) |

### CancelAccessRequestAsync

- Signature: Task<ApiResponse<bool>> CancelAccessRequestAsync(Guid requestId);
- Return Type: Task<ApiResponse<bool>>
- Parameters: Guid requestId

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| CancelAccessRequestAsync-01 | HappyPath | Valid requestId provided | Returns success payload matching declared return type |
| CancelAccessRequestAsync-02 | InvalidInput | requestId = Guid.Empty | Returns validation error (400 or 422) or equivalent domain error |
| CancelAccessRequestAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on requestId | Returns unauthorized or forbidden response, or operation rejected by policy |
| CancelAccessRequestAsync-REQUESTID-EmptyGuid | InvalidInput | requestId = Guid.Empty | Returns validation error (400 or 422) |

