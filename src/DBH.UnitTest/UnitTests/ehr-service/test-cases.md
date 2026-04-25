# EHR Service Test Cases

This document defines scenario coverage for each function declared in the service interfaces.

## IEhrService

### CreateEhrRecordAsync

- Signature: Task<CreateEhrRecordResponseDto> CreateEhrRecordAsync(CreateEhrRecordDto request);
- Return Type: Task<CreateEhrRecordResponseDto>
- Parameters: CreateEhrRecordDto request

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| CreateEhrRecordAsync-01 | HappyPath | Valid request provided | Returns success payload matching declared return type |
| CreateEhrRecordAsync-02 | InvalidInput | Invalid request | Returns validation error (400 or 422) or equivalent domain error |
| CreateEhrRecordAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on request | Returns unauthorized or forbidden response, or operation rejected by policy |

### GetEhrRecordAsync

- Signature: Task<EhrRecordResponseDto?> GetEhrRecordAsync(Guid ehrId);
- Return Type: Task<EhrRecordResponseDto?>
- Parameters: Guid ehrId

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetEhrRecordAsync-01 | HappyPath | Valid ehrId provided | Returns success payload matching declared return type |
| GetEhrRecordAsync-02 | InvalidInput | ehrId = Guid.Empty | Returns validation error (400 or 422) or equivalent domain error |
| GetEhrRecordAsync-03 | NotFoundOrNoData | ehrId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| GetEhrRecordAsync-04 | NullableReturn | Valid ehrId but no associated resource | Returns null without throwing |
| GetEhrRecordAsync-EHRID-EmptyGuid | InvalidInput | ehrId = Guid.Empty | Returns validation error (400 or 422) |

### GetEhrRecordWithConsentCheckAsync

- Signature: Task<(EhrRecordResponseDto? Record, bool ConsentDenied, string? DenyMessage)> GetEhrRecordWithConsentCheckAsync(Guid ehrId, Guid requesterId);
- Return Type: Task<(EhrRecordResponseDto? Record, bool ConsentDenied, string? DenyMessage)>
- Parameters: Guid ehrId, Guid requesterId

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetEhrRecordWithConsentCheckAsync-01 | HappyPath | Valid ehrId, requesterId provided | Returns success payload matching declared return type |
| GetEhrRecordWithConsentCheckAsync-02 | InvalidInput | ehrId = Guid.Empty OR requesterId = Guid.Empty | Returns validation error (400 or 422) or equivalent domain error |
| GetEhrRecordWithConsentCheckAsync-03 | NotFoundOrNoData | ehrId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| GetEhrRecordWithConsentCheckAsync-04 | TupleFlags | Specific input for TupleFlags with ehrId, requesterId | Tuple field values and message fields match scenario |
| GetEhrRecordWithConsentCheckAsync-EHRID-EmptyGuid | InvalidInput | ehrId = Guid.Empty | Returns validation error (400 or 422) |
| GetEhrRecordWithConsentCheckAsync-REQUESTERID-EmptyGuid | InvalidInput | requesterId = Guid.Empty | Returns validation error (400 or 422) |

### GetEhrDocumentAsync

- Signature: Task<(string? DecryptedData, bool ConsentDenied, string? DenyMessage)> GetEhrDocumentAsync(Guid ehrId, Guid requesterId);
- Return Type: Task<(string? DecryptedData, bool ConsentDenied, string? DenyMessage)>
- Parameters: Guid ehrId, Guid requesterId

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetEhrDocumentAsync-01 | HappyPath | Valid ehrId, requesterId provided | Returns success payload matching declared return type |
| GetEhrDocumentAsync-02 | InvalidInput | ehrId = Guid.Empty OR requesterId = Guid.Empty | Returns validation error (400 or 422) or equivalent domain error |
| GetEhrDocumentAsync-03 | NotFoundOrNoData | ehrId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| GetEhrDocumentAsync-04 | TupleFlags | Specific input for TupleFlags with ehrId, requesterId | Tuple field values and message fields match scenario |
| GetEhrDocumentAsync-EHRID-EmptyGuid | InvalidInput | ehrId = Guid.Empty | Returns validation error (400 or 422) |
| GetEhrDocumentAsync-REQUESTERID-EmptyGuid | InvalidInput | requesterId = Guid.Empty | Returns validation error (400 or 422) |

### GetEhrDocumentForCurrentUserAsync

- Signature: Task<(string? DecryptedData, bool Forbidden, string? Message)> GetEhrDocumentForCurrentUserAsync(Guid ehrId);
- Return Type: Task<(string? DecryptedData, bool Forbidden, string? Message)>
- Parameters: Guid ehrId

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetEhrDocumentForCurrentUserAsync-01 | HappyPath | Valid ehrId provided | Returns success payload matching declared return type |
| GetEhrDocumentForCurrentUserAsync-02 | InvalidInput | ehrId = Guid.Empty | Returns validation error (400 or 422) or equivalent domain error |
| GetEhrDocumentForCurrentUserAsync-03 | NotFoundOrNoData | ehrId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| GetEhrDocumentForCurrentUserAsync-04 | TupleFlags | Specific input for TupleFlags with ehrId | Tuple field values and message fields match scenario |
| GetEhrDocumentForCurrentUserAsync-EHRID-EmptyGuid | InvalidInput | ehrId = Guid.Empty | Returns validation error (400 or 422) |

### DownloadIpfsRawAsync

- Signature: Task<string?> DownloadIpfsRawAsync(string ipfsCid);
- Return Type: Task<string?>
- Parameters: string ipfsCid

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| DownloadIpfsRawAsync-01 | HappyPath | Valid ipfsCid provided | Returns success payload matching declared return type |
| DownloadIpfsRawAsync-02 | InvalidInput | ipfsCid = null/empty | Returns validation error (400 or 422) or equivalent domain error |
| DownloadIpfsRawAsync-03 | NotFoundOrNoData | Requested resource not found | Returns null, empty, false, or not-found response according to contract |
| DownloadIpfsRawAsync-04 | NullableReturn | Valid input, but resource is naturally null | Returns null without throwing |
| DownloadIpfsRawAsync-IPFSCID-EmptyString | InvalidInput | ipfsCid = null or empty string | Returns validation error (400 or 422) |

### DownloadLatestIpfsRawByEhrIdAsync

- Signature: Task<IpfsRawDownloadResponseDto?> DownloadLatestIpfsRawByEhrIdAsync(Guid ehrId);
- Return Type: Task<IpfsRawDownloadResponseDto?>
- Parameters: Guid ehrId

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| DownloadLatestIpfsRawByEhrIdAsync-01 | HappyPath | Valid ehrId provided | Returns success payload matching declared return type |
| DownloadLatestIpfsRawByEhrIdAsync-02 | InvalidInput | ehrId = Guid.Empty | Returns validation error (400 or 422) or equivalent domain error |
| DownloadLatestIpfsRawByEhrIdAsync-03 | NotFoundOrNoData | ehrId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| DownloadLatestIpfsRawByEhrIdAsync-04 | NullableReturn | Valid ehrId but no associated resource | Returns null without throwing |
| DownloadLatestIpfsRawByEhrIdAsync-EHRID-EmptyGuid | InvalidInput | ehrId = Guid.Empty | Returns validation error (400 or 422) |

### EncryptToIpfsForCurrentUserAsync

- Signature: Task<EncryptIpfsPayloadResponseDto?> EncryptToIpfsForCurrentUserAsync(EncryptIpfsPayloadRequestDto request);
- Return Type: Task<EncryptIpfsPayloadResponseDto?>
- Parameters: EncryptIpfsPayloadRequestDto request

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| EncryptToIpfsForCurrentUserAsync-01 | HappyPath | Valid request provided | Returns success payload matching declared return type |
| EncryptToIpfsForCurrentUserAsync-02 | InvalidInput | request with missing required fields | Returns validation error (400 or 422) or equivalent domain error |
| EncryptToIpfsForCurrentUserAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on request | Returns unauthorized or forbidden response, or operation rejected by policy |
| EncryptToIpfsForCurrentUserAsync-04 | NullableReturn | Valid input, but resource is naturally null | Returns null without throwing |

### DecryptIpfsForCurrentUserAsync

- Signature: Task<string?> DecryptIpfsForCurrentUserAsync(DecryptIpfsPayloadRequestDto request);
- Return Type: Task<string?>
- Parameters: DecryptIpfsPayloadRequestDto request

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| DecryptIpfsForCurrentUserAsync-01 | HappyPath | Valid request provided | Returns success payload matching declared return type |
| DecryptIpfsForCurrentUserAsync-02 | InvalidInput | request with missing required fields | Returns validation error (400 or 422) or equivalent domain error |
| DecryptIpfsForCurrentUserAsync-03 | NotFoundOrNoData | Requested resource not found | Returns null, empty, false, or not-found response according to contract |
| DecryptIpfsForCurrentUserAsync-04 | NullableReturn | Valid input, but resource is naturally null | Returns null without throwing |

### GetPatientEhrRecordsAsync

- Signature: Task<IEnumerable<EhrRecordResponseDto>> GetPatientEhrRecordsAsync(Guid patientId);
- Return Type: Task<IEnumerable<EhrRecordResponseDto>>
- Parameters: Guid patientId

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetPatientEhrRecordsAsync-01 | HappyPath | Valid patientId provided | Returns success payload matching declared return type |
| GetPatientEhrRecordsAsync-02 | InvalidInput | patientId = Guid.Empty | Returns validation error (400 or 422) or equivalent domain error |
| GetPatientEhrRecordsAsync-03 | NotFoundOrNoData | patientId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| GetPatientEhrRecordsAsync-04 | EmptyCollection | Specific input for EmptyCollection with patientId | Returns empty collection, not null |
| GetPatientEhrRecordsAsync-PATIENTID-EmptyGuid | InvalidInput | patientId = Guid.Empty | Returns validation error (400 or 422) |

### GetOrgEhrRecordsAsync

- Signature: Task<IEnumerable<EhrRecordResponseDto>> GetOrgEhrRecordsAsync(Guid orgId);
- Return Type: Task<IEnumerable<EhrRecordResponseDto>>
- Parameters: Guid orgId

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetOrgEhrRecordsAsync-01 | HappyPath | Valid orgId provided | Returns success payload matching declared return type |
| GetOrgEhrRecordsAsync-02 | InvalidInput | orgId = Guid.Empty | Returns validation error (400 or 422) or equivalent domain error |
| GetOrgEhrRecordsAsync-03 | NotFoundOrNoData | orgId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| GetOrgEhrRecordsAsync-04 | EmptyCollection | Specific input for EmptyCollection with orgId | Returns empty collection, not null |
| GetOrgEhrRecordsAsync-ORGID-EmptyGuid | InvalidInput | orgId = Guid.Empty | Returns validation error (400 or 422) |

### GetEhrVersionsAsync

- Signature: Task<IEnumerable<EhrVersionDto>> GetEhrVersionsAsync(Guid ehrId);
- Return Type: Task<IEnumerable<EhrVersionDto>>
- Parameters: Guid ehrId

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetEhrVersionsAsync-01 | HappyPath | Valid ehrId provided | Returns success payload matching declared return type |
| GetEhrVersionsAsync-02 | InvalidInput | ehrId = Guid.Empty | Returns validation error (400 or 422) or equivalent domain error |
| GetEhrVersionsAsync-03 | NotFoundOrNoData | ehrId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| GetEhrVersionsAsync-04 | EmptyCollection | Specific input for EmptyCollection with ehrId | Returns empty collection, not null |
| GetEhrVersionsAsync-EHRID-EmptyGuid | InvalidInput | ehrId = Guid.Empty | Returns validation error (400 or 422) |

### GetEhrFilesAsync

- Signature: Task<IEnumerable<EhrFileDto>> GetEhrFilesAsync(Guid ehrId);
- Return Type: Task<IEnumerable<EhrFileDto>>
- Parameters: Guid ehrId

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetEhrFilesAsync-01 | HappyPath | Valid ehrId provided | Returns success payload matching declared return type |
| GetEhrFilesAsync-02 | InvalidInput | ehrId = Guid.Empty | Returns validation error (400 or 422) or equivalent domain error |
| GetEhrFilesAsync-03 | NotFoundOrNoData | ehrId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| GetEhrFilesAsync-04 | EmptyCollection | Specific input for EmptyCollection with ehrId | Returns empty collection, not null |
| GetEhrFilesAsync-EHRID-EmptyGuid | InvalidInput | ehrId = Guid.Empty | Returns validation error (400 or 422) |

### UpdateEhrRecordAsync

- Signature: Task<EhrRecordResponseDto?> UpdateEhrRecordAsync(Guid ehrId, UpdateEhrRecordDto request);
- Return Type: Task<EhrRecordResponseDto?>
- Parameters: Guid ehrId, UpdateEhrRecordDto request

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| UpdateEhrRecordAsync-01 | HappyPath | Valid ehrId, request provided | Returns success payload matching declared return type |
| UpdateEhrRecordAsync-02 | InvalidInput | ehrId = Guid.Empty OR Invalid request | Returns validation error (400 or 422) or equivalent domain error |
| UpdateEhrRecordAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on ehrId, request | Returns unauthorized or forbidden response, or operation rejected by policy |
| UpdateEhrRecordAsync-04 | NullableReturn | Valid ehrId but no associated resource | Returns null without throwing |
| UpdateEhrRecordAsync-EHRID-EmptyGuid | InvalidInput | ehrId = Guid.Empty | Returns validation error (400 or 422) |

### GetVersionByIdAsync

- Signature: Task<EhrVersionDetailDto?> GetVersionByIdAsync(Guid ehrId, Guid versionId);
- Return Type: Task<EhrVersionDetailDto?>
- Parameters: Guid ehrId, Guid versionId

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetVersionByIdAsync-01 | HappyPath | Valid ehrId, versionId provided | Returns success payload matching declared return type |
| GetVersionByIdAsync-02 | InvalidInput | ehrId = Guid.Empty OR versionId = Guid.Empty | Returns validation error (400 or 422) or equivalent domain error |
| GetVersionByIdAsync-03 | NotFoundOrNoData | ehrId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| GetVersionByIdAsync-04 | NullableReturn | Valid ehrId but no associated resource | Returns null without throwing |
| GetVersionByIdAsync-EHRID-EmptyGuid | InvalidInput | ehrId = Guid.Empty | Returns validation error (400 or 422) |
| GetVersionByIdAsync-VERSIONID-EmptyGuid | InvalidInput | versionId = Guid.Empty | Returns validation error (400 or 422) |

### AddFileAsync

- Signature: Task<EhrFileDto?> AddFileAsync(Guid ehrId, Stream fileStream, string fileName);
- Return Type: Task<EhrFileDto?>
- Parameters: Guid ehrId, Stream fileStream, string fileName

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| AddFileAsync-01 | HappyPath | Valid ehrId, fileStream, fileName provided | Returns success payload matching declared return type |
| AddFileAsync-02 | InvalidInput | ehrId = Guid.Empty OR Invalid fileStream OR fileName = null/empty | Returns validation error (400 or 422) or equivalent domain error |
| AddFileAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on ehrId, fileStream, fileName | Returns unauthorized or forbidden response, or operation rejected by policy |
| AddFileAsync-04 | NullableReturn | Valid ehrId but no associated resource | Returns null without throwing |
| AddFileAsync-EHRID-EmptyGuid | InvalidInput | ehrId = Guid.Empty | Returns validation error (400 or 422) |
| AddFileAsync-FILENAME-EmptyString | InvalidInput | fileName = null or empty string | Returns validation error (400 or 422) |

### DeleteFileAsync

- Signature: Task<bool> DeleteFileAsync(Guid ehrId, Guid fileId);
- Return Type: Task<bool>
- Parameters: Guid ehrId, Guid fileId

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| DeleteFileAsync-01 | HappyPath | Valid ehrId, fileId provided | Returns success payload matching declared return type |
| DeleteFileAsync-02 | InvalidInput | ehrId = Guid.Empty OR fileId = Guid.Empty | Returns validation error (400 or 422) or equivalent domain error |
| DeleteFileAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on ehrId, fileId | Returns unauthorized or forbidden response, or operation rejected by policy |
| DeleteFileAsync-04 | BooleanFalsePath | Specific input for BooleanFalsePath with ehrId, fileId | Returns false |
| DeleteFileAsync-05 | BooleanTruePath | Specific input for BooleanTruePath with ehrId, fileId | Returns true |
| DeleteFileAsync-EHRID-EmptyGuid | InvalidInput | ehrId = Guid.Empty | Returns validation error (400 or 422) |
| DeleteFileAsync-FILEID-EmptyGuid | InvalidInput | fileId = Guid.Empty | Returns validation error (400 or 422) |

## IAuthServiceClient

### GetUserIdByPatientIdAsync

- Signature: Task<Guid?> GetUserIdByPatientIdAsync(Guid patientId, string bearerToken);
- Return Type: Task<Guid?>
- Parameters: Guid patientId, string bearerToken

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetUserIdByPatientIdAsync-01 | HappyPath | Valid patientId, bearerToken provided | Returns success payload matching declared return type |
| GetUserIdByPatientIdAsync-02 | InvalidInput | patientId = Guid.Empty OR bearerToken = null/empty | Returns validation error (400 or 422) or equivalent domain error |
| GetUserIdByPatientIdAsync-03 | NotFoundOrNoData | patientId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| GetUserIdByPatientIdAsync-04 | NullableReturn | Valid patientId but no associated resource | Returns null without throwing |
| GetUserIdByPatientIdAsync-PATIENTID-EmptyGuid | InvalidInput | patientId = Guid.Empty | Returns validation error (400 or 422) |
| GetUserIdByPatientIdAsync-BEARERTOKEN-EmptyString | InvalidInput | bearerToken = null or empty string | Returns validation error (400 or 422) |

### GetUserIdByDoctorIdAsync

- Signature: Task<Guid?> GetUserIdByDoctorIdAsync(Guid doctorId, string bearerToken);
- Return Type: Task<Guid?>
- Parameters: Guid doctorId, string bearerToken

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetUserIdByDoctorIdAsync-01 | HappyPath | Valid doctorId, bearerToken provided | Returns success payload matching declared return type |
| GetUserIdByDoctorIdAsync-02 | InvalidInput | doctorId = Guid.Empty OR bearerToken = null/empty | Returns validation error (400 or 422) or equivalent domain error |
| GetUserIdByDoctorIdAsync-03 | NotFoundOrNoData | doctorId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| GetUserIdByDoctorIdAsync-04 | NullableReturn | Valid doctorId but no associated resource | Returns null without throwing |
| GetUserIdByDoctorIdAsync-DOCTORID-EmptyGuid | InvalidInput | doctorId = Guid.Empty | Returns validation error (400 or 422) |
| GetUserIdByDoctorIdAsync-BEARERTOKEN-EmptyString | InvalidInput | bearerToken = null or empty string | Returns validation error (400 or 422) |

### GetUserProfileDetailAsync

- Signature: Task<AuthUserProfileDetailDto?> GetUserProfileDetailAsync(Guid userId, string bearerToken);
- Return Type: Task<AuthUserProfileDetailDto?>
- Parameters: Guid userId, string bearerToken

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetUserProfileDetailAsync-01 | HappyPath | Valid userId, bearerToken provided | Returns success payload matching declared return type |
| GetUserProfileDetailAsync-02 | InvalidInput | userId = Guid.Empty OR bearerToken = null/empty | Returns validation error (400 or 422) or equivalent domain error |
| GetUserProfileDetailAsync-03 | NotFoundOrNoData | userId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| GetUserProfileDetailAsync-04 | NullableReturn | Valid userId but no associated resource | Returns null without throwing |
| GetUserProfileDetailAsync-USERID-EmptyGuid | InvalidInput | userId = Guid.Empty | Returns validation error (400 or 422) |
| GetUserProfileDetailAsync-BEARERTOKEN-EmptyString | InvalidInput | bearerToken = null or empty string | Returns validation error (400 or 422) |

## IEhrRecordRepository

### CreateAsync

- Signature: Task<EhrRecord> CreateAsync(EhrRecord record);
- Return Type: Task<EhrRecord>
- Parameters: EhrRecord record

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| CreateAsync-01 | HappyPath | Valid record provided | Returns success payload matching declared return type |
| CreateAsync-02 | InvalidInput | Invalid record | Returns validation error (400 or 422) or equivalent domain error |
| CreateAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on record | Returns unauthorized or forbidden response, or operation rejected by policy |

### CreateVersionAsync

- Signature: Task<EhrVersion> CreateVersionAsync(EhrVersion version);
- Return Type: Task<EhrVersion>
- Parameters: EhrVersion version

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| CreateVersionAsync-01 | HappyPath | Valid version provided | Returns success payload matching declared return type |
| CreateVersionAsync-02 | InvalidInput | Invalid version | Returns validation error (400 or 422) or equivalent domain error |
| CreateVersionAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on version | Returns unauthorized or forbidden response, or operation rejected by policy |

### CreateFileAsync

- Signature: Task<EhrFile> CreateFileAsync(EhrFile file);
- Return Type: Task<EhrFile>
- Parameters: EhrFile file

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| CreateFileAsync-01 | HappyPath | Valid file provided | Returns success payload matching declared return type |
| CreateFileAsync-02 | InvalidInput | Invalid file | Returns validation error (400 or 422) or equivalent domain error |
| CreateFileAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on file | Returns unauthorized or forbidden response, or operation rejected by policy |

### UpdateAsync

- Signature: Task<EhrRecord> UpdateAsync(EhrRecord record);
- Return Type: Task<EhrRecord>
- Parameters: EhrRecord record

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| UpdateAsync-01 | HappyPath | Valid record provided | Returns success payload matching declared return type |
| UpdateAsync-02 | InvalidInput | Invalid record | Returns validation error (400 or 422) or equivalent domain error |
| UpdateAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on record | Returns unauthorized or forbidden response, or operation rejected by policy |

### UpdateVersionAsync

- Signature: Task<EhrVersion> UpdateVersionAsync(EhrVersion version);
- Return Type: Task<EhrVersion>
- Parameters: EhrVersion version

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| UpdateVersionAsync-01 | HappyPath | Valid version provided | Returns success payload matching declared return type |
| UpdateVersionAsync-02 | InvalidInput | Invalid version | Returns validation error (400 or 422) or equivalent domain error |
| UpdateVersionAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on version | Returns unauthorized or forbidden response, or operation rejected by policy |

### GetByIdAsync

- Signature: Task<EhrRecord?> GetByIdAsync(Guid ehrId);
- Return Type: Task<EhrRecord?>
- Parameters: Guid ehrId

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetByIdAsync-01 | HappyPath | Valid ehrId provided | Returns success payload matching declared return type |
| GetByIdAsync-02 | InvalidInput | ehrId = Guid.Empty | Returns validation error (400 or 422) or equivalent domain error |
| GetByIdAsync-03 | NotFoundOrNoData | ehrId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| GetByIdAsync-04 | NullableReturn | Valid ehrId but no associated resource | Returns null without throwing |
| GetByIdAsync-EHRID-EmptyGuid | InvalidInput | ehrId = Guid.Empty | Returns validation error (400 or 422) |

### GetByIdWithVersionsAsync

- Signature: Task<EhrRecord?> GetByIdWithVersionsAsync(Guid ehrId);
- Return Type: Task<EhrRecord?>
- Parameters: Guid ehrId

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetByIdWithVersionsAsync-01 | HappyPath | Valid ehrId provided | Returns success payload matching declared return type |
| GetByIdWithVersionsAsync-02 | InvalidInput | ehrId = Guid.Empty | Returns validation error (400 or 422) or equivalent domain error |
| GetByIdWithVersionsAsync-03 | NotFoundOrNoData | ehrId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| GetByIdWithVersionsAsync-04 | NullableReturn | Valid ehrId but no associated resource | Returns null without throwing |
| GetByIdWithVersionsAsync-EHRID-EmptyGuid | InvalidInput | ehrId = Guid.Empty | Returns validation error (400 or 422) |

### GetByPatientIdAsync

- Signature: Task<IEnumerable<EhrRecord>> GetByPatientIdAsync(Guid patientId);
- Return Type: Task<IEnumerable<EhrRecord>>
- Parameters: Guid patientId

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetByPatientIdAsync-01 | HappyPath | Valid patientId provided | Returns success payload matching declared return type |
| GetByPatientIdAsync-02 | InvalidInput | patientId = Guid.Empty | Returns validation error (400 or 422) or equivalent domain error |
| GetByPatientIdAsync-03 | NotFoundOrNoData | patientId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| GetByPatientIdAsync-04 | EmptyCollection | Specific input for EmptyCollection with patientId | Returns empty collection, not null |
| GetByPatientIdAsync-PATIENTID-EmptyGuid | InvalidInput | patientId = Guid.Empty | Returns validation error (400 or 422) |

### GetByOrgIdAsync

- Signature: Task<IEnumerable<EhrRecord>> GetByOrgIdAsync(Guid orgId);
- Return Type: Task<IEnumerable<EhrRecord>>
- Parameters: Guid orgId

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetByOrgIdAsync-01 | HappyPath | Valid orgId provided | Returns success payload matching declared return type |
| GetByOrgIdAsync-02 | InvalidInput | orgId = Guid.Empty | Returns validation error (400 or 422) or equivalent domain error |
| GetByOrgIdAsync-03 | NotFoundOrNoData | orgId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| GetByOrgIdAsync-04 | EmptyCollection | Specific input for EmptyCollection with orgId | Returns empty collection, not null |
| GetByOrgIdAsync-ORGID-EmptyGuid | InvalidInput | orgId = Guid.Empty | Returns validation error (400 or 422) |

### GetLatestVersionAsync

- Signature: Task<EhrVersion?> GetLatestVersionAsync(Guid ehrId);
- Return Type: Task<EhrVersion?>
- Parameters: Guid ehrId

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetLatestVersionAsync-01 | HappyPath | Valid ehrId provided | Returns success payload matching declared return type |
| GetLatestVersionAsync-02 | InvalidInput | ehrId = Guid.Empty | Returns validation error (400 or 422) or equivalent domain error |
| GetLatestVersionAsync-03 | NotFoundOrNoData | ehrId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| GetLatestVersionAsync-04 | NullableReturn | Valid ehrId but no associated resource | Returns null without throwing |
| GetLatestVersionAsync-EHRID-EmptyGuid | InvalidInput | ehrId = Guid.Empty | Returns validation error (400 or 422) |

### GetVersionsAsync

- Signature: Task<IEnumerable<EhrVersion>> GetVersionsAsync(Guid ehrId);
- Return Type: Task<IEnumerable<EhrVersion>>
- Parameters: Guid ehrId

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetVersionsAsync-01 | HappyPath | Valid ehrId provided | Returns success payload matching declared return type |
| GetVersionsAsync-02 | InvalidInput | ehrId = Guid.Empty | Returns validation error (400 or 422) or equivalent domain error |
| GetVersionsAsync-03 | NotFoundOrNoData | ehrId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| GetVersionsAsync-04 | EmptyCollection | Specific input for EmptyCollection with ehrId | Returns empty collection, not null |
| GetVersionsAsync-EHRID-EmptyGuid | InvalidInput | ehrId = Guid.Empty | Returns validation error (400 or 422) |

### GetFilesAsync

- Signature: Task<IEnumerable<EhrFile>> GetFilesAsync(Guid ehrId);
- Return Type: Task<IEnumerable<EhrFile>>
- Parameters: Guid ehrId

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetFilesAsync-01 | HappyPath | Valid ehrId provided | Returns success payload matching declared return type |
| GetFilesAsync-02 | InvalidInput | ehrId = Guid.Empty | Returns validation error (400 or 422) or equivalent domain error |
| GetFilesAsync-03 | NotFoundOrNoData | ehrId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| GetFilesAsync-04 | EmptyCollection | Specific input for EmptyCollection with ehrId | Returns empty collection, not null |
| GetFilesAsync-EHRID-EmptyGuid | InvalidInput | ehrId = Guid.Empty | Returns validation error (400 or 422) |

### GetVersionByIdAsync

- Signature: Task<EhrVersion?> GetVersionByIdAsync(Guid ehrId, Guid versionId);
- Return Type: Task<EhrVersion?>
- Parameters: Guid ehrId, Guid versionId

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetVersionByIdAsync-01 | HappyPath | Valid ehrId, versionId provided | Returns success payload matching declared return type |
| GetVersionByIdAsync-02 | InvalidInput | ehrId = Guid.Empty OR versionId = Guid.Empty | Returns validation error (400 or 422) or equivalent domain error |
| GetVersionByIdAsync-03 | NotFoundOrNoData | ehrId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| GetVersionByIdAsync-04 | NullableReturn | Valid ehrId but no associated resource | Returns null without throwing |
| GetVersionByIdAsync-EHRID-EmptyGuid | InvalidInput | ehrId = Guid.Empty | Returns validation error (400 or 422) |
| GetVersionByIdAsync-VERSIONID-EmptyGuid | InvalidInput | versionId = Guid.Empty | Returns validation error (400 or 422) |

### GetFileByIdAsync

- Signature: Task<EhrFile?> GetFileByIdAsync(Guid ehrId, Guid fileId);
- Return Type: Task<EhrFile?>
- Parameters: Guid ehrId, Guid fileId

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetFileByIdAsync-01 | HappyPath | Valid ehrId, fileId provided | Returns success payload matching declared return type |
| GetFileByIdAsync-02 | InvalidInput | ehrId = Guid.Empty OR fileId = Guid.Empty | Returns validation error (400 or 422) or equivalent domain error |
| GetFileByIdAsync-03 | NotFoundOrNoData | ehrId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| GetFileByIdAsync-04 | NullableReturn | Valid ehrId but no associated resource | Returns null without throwing |
| GetFileByIdAsync-EHRID-EmptyGuid | InvalidInput | ehrId = Guid.Empty | Returns validation error (400 or 422) |
| GetFileByIdAsync-FILEID-EmptyGuid | InvalidInput | fileId = Guid.Empty | Returns validation error (400 or 422) |

### DeleteFileAsync

- Signature: Task DeleteFileAsync(EhrFile file);
- Return Type: Task
- Parameters: EhrFile file

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| DeleteFileAsync-01 | HappyPath | Valid file provided | Returns success payload matching declared return type |
| DeleteFileAsync-02 | InvalidInput | Invalid file | Returns validation error (400 or 422) or equivalent domain error |
| DeleteFileAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on file | Returns unauthorized or forbidden response, or operation rejected by policy |

