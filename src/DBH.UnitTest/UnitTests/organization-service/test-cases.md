# Organization Service Test Cases

This document defines scenario coverage for each function declared in the service interfaces.

## IOrganizationService

### CreateOrganizationAsync

- Signature: Task<ApiResponse<OrganizationResponse>> CreateOrganizationAsync(CreateOrganizationRequest request);
- Return Type: Task<ApiResponse<OrganizationResponse>>
- Parameters: CreateOrganizationRequest request

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| CreateOrganizationAsync-01 | HappyPath | Valid request provided | Returns success payload matching declared return type |
| CreateOrganizationAsync-02 | InvalidInput | request with missing required fields | Returns validation error (400 or 422) or equivalent domain error |
| CreateOrganizationAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on request | Returns unauthorized or forbidden response, or operation rejected by policy |

### GetOrganizationByIdAsync

- Signature: Task<ApiResponse<OrganizationResponse>> GetOrganizationByIdAsync(Guid orgId);
- Return Type: Task<ApiResponse<OrganizationResponse>>
- Parameters: Guid orgId

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetOrganizationByIdAsync-01 | HappyPath | Valid orgId provided | Returns success payload matching declared return type |
| GetOrganizationByIdAsync-02 | InvalidInput | orgId = Guid.Empty | Returns validation error (400 or 422) or equivalent domain error |
| GetOrganizationByIdAsync-03 | NotFoundOrNoData | orgId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| GetOrganizationByIdAsync-ORGID-EmptyGuid | InvalidInput | orgId = Guid.Empty | Returns validation error (400 or 422) |

### GetOrganizationsAsync

- Signature: Task<PagedResponse<OrganizationResponse>> GetOrganizationsAsync(int page = 1, int pageSize = 10, string? search = null);
- Return Type: Task<PagedResponse<OrganizationResponse>>
- Parameters: int page = 1, int pageSize = 10, string? search = null

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetOrganizationsAsync-01 | HappyPath | Valid 1, 10, null provided | Returns success payload matching declared return type |
| GetOrganizationsAsync-02 | InvalidInput | 1 <= 0 OR 10 <= 0 OR null = null/empty | Returns validation error (400 or 422) or equivalent domain error |
| GetOrganizationsAsync-03 | NotFoundOrNoData | Requested resource not found | Returns null, empty, false, or not-found response according to contract |
| GetOrganizationsAsync-04 | PagingBoundary | Large page number or page size out of bounds | Returns valid paging metadata; out-of-range page returns empty item set |
| GetOrganizationsAsync-PAGE-ZeroOrNegative | InvalidInput | page <= 0 | Returns validation error (400 or 422) |
| GetOrganizationsAsync-PAGESIZE-ZeroOrNegative | InvalidInput | pageSize <= 0 | Returns validation error (400 or 422) |

### UpdateOrganizationAsync

- Signature: Task<ApiResponse<OrganizationResponse>> UpdateOrganizationAsync(Guid orgId, UpdateOrganizationRequest request);
- Return Type: Task<ApiResponse<OrganizationResponse>>
- Parameters: Guid orgId, UpdateOrganizationRequest request

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| UpdateOrganizationAsync-01 | HappyPath | Valid orgId, request provided | Returns success payload matching declared return type |
| UpdateOrganizationAsync-02 | InvalidInput | orgId = Guid.Empty OR request with missing required fields | Returns validation error (400 or 422) or equivalent domain error |
| UpdateOrganizationAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on orgId, request | Returns unauthorized or forbidden response, or operation rejected by policy |
| UpdateOrganizationAsync-ORGID-EmptyGuid | InvalidInput | orgId = Guid.Empty | Returns validation error (400 or 422) |

### DeleteOrganizationAsync

- Signature: Task<ApiResponse<bool>> DeleteOrganizationAsync(Guid orgId);
- Return Type: Task<ApiResponse<bool>>
- Parameters: Guid orgId

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| DeleteOrganizationAsync-01 | HappyPath | Valid orgId provided | Returns success payload matching declared return type |
| DeleteOrganizationAsync-02 | InvalidInput | orgId = Guid.Empty | Returns validation error (400 or 422) or equivalent domain error |
| DeleteOrganizationAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on orgId | Returns unauthorized or forbidden response, or operation rejected by policy |
| DeleteOrganizationAsync-ORGID-EmptyGuid | InvalidInput | orgId = Guid.Empty | Returns validation error (400 or 422) |

### VerifyOrganizationAsync

- Signature: Task<ApiResponse<OrganizationResponse>> VerifyOrganizationAsync(Guid orgId, Guid verifiedByUserId);
- Return Type: Task<ApiResponse<OrganizationResponse>>
- Parameters: Guid orgId, Guid verifiedByUserId

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| VerifyOrganizationAsync-01 | HappyPath | Valid orgId, verifiedByUserId provided | Returns success payload matching declared return type |
| VerifyOrganizationAsync-02 | InvalidInput | orgId = Guid.Empty OR verifiedByUserId = Guid.Empty | Returns validation error (400 or 422) or equivalent domain error |
| VerifyOrganizationAsync-03 | NotFoundOrNoData | orgId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| VerifyOrganizationAsync-ORGID-EmptyGuid | InvalidInput | orgId = Guid.Empty | Returns validation error (400 or 422) |
| VerifyOrganizationAsync-VERIFIEDBYUSERID-EmptyGuid | InvalidInput | verifiedByUserId = Guid.Empty | Returns validation error (400 or 422) |

### CreateDepartmentAsync

- Signature: Task<ApiResponse<DepartmentResponse>> CreateDepartmentAsync(CreateDepartmentRequest request);
- Return Type: Task<ApiResponse<DepartmentResponse>>
- Parameters: CreateDepartmentRequest request

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| CreateDepartmentAsync-01 | HappyPath | Valid request provided | Returns success payload matching declared return type |
| CreateDepartmentAsync-02 | InvalidInput | request with missing required fields | Returns validation error (400 or 422) or equivalent domain error |
| CreateDepartmentAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on request | Returns unauthorized or forbidden response, or operation rejected by policy |

### GetDepartmentByIdAsync

- Signature: Task<ApiResponse<DepartmentResponse>> GetDepartmentByIdAsync(Guid departmentId);
- Return Type: Task<ApiResponse<DepartmentResponse>>
- Parameters: Guid departmentId

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetDepartmentByIdAsync-01 | HappyPath | Valid departmentId provided | Returns success payload matching declared return type |
| GetDepartmentByIdAsync-02 | InvalidInput | departmentId = Guid.Empty | Returns validation error (400 or 422) or equivalent domain error |
| GetDepartmentByIdAsync-03 | NotFoundOrNoData | departmentId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| GetDepartmentByIdAsync-DEPARTMENTID-EmptyGuid | InvalidInput | departmentId = Guid.Empty | Returns validation error (400 or 422) |

### GetDepartmentsByOrgAsync

- Signature: Task<PagedResponse<DepartmentResponse>> GetDepartmentsByOrgAsync(Guid orgId, int page = 1, int pageSize = 10);
- Return Type: Task<PagedResponse<DepartmentResponse>>
- Parameters: Guid orgId, int page = 1, int pageSize = 10

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetDepartmentsByOrgAsync-01 | HappyPath | Valid orgId, 1, 10 provided | Returns success payload matching declared return type |
| GetDepartmentsByOrgAsync-02 | InvalidInput | orgId = Guid.Empty OR 1 <= 0 OR 10 <= 0 | Returns validation error (400 or 422) or equivalent domain error |
| GetDepartmentsByOrgAsync-03 | NotFoundOrNoData | orgId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| GetDepartmentsByOrgAsync-04 | PagingBoundary | Large page number or page size out of bounds | Returns valid paging metadata; out-of-range page returns empty item set |
| GetDepartmentsByOrgAsync-ORGID-EmptyGuid | InvalidInput | orgId = Guid.Empty | Returns validation error (400 or 422) |
| GetDepartmentsByOrgAsync-PAGE-ZeroOrNegative | InvalidInput | page <= 0 | Returns validation error (400 or 422) |
| GetDepartmentsByOrgAsync-PAGESIZE-ZeroOrNegative | InvalidInput | pageSize <= 0 | Returns validation error (400 or 422) |

### UpdateDepartmentAsync

- Signature: Task<ApiResponse<DepartmentResponse>> UpdateDepartmentAsync(Guid departmentId, UpdateDepartmentRequest request);
- Return Type: Task<ApiResponse<DepartmentResponse>>
- Parameters: Guid departmentId, UpdateDepartmentRequest request

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| UpdateDepartmentAsync-01 | HappyPath | Valid departmentId, request provided | Returns success payload matching declared return type |
| UpdateDepartmentAsync-02 | InvalidInput | departmentId = Guid.Empty OR request with missing required fields | Returns validation error (400 or 422) or equivalent domain error |
| UpdateDepartmentAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on departmentId, request | Returns unauthorized or forbidden response, or operation rejected by policy |
| UpdateDepartmentAsync-DEPARTMENTID-EmptyGuid | InvalidInput | departmentId = Guid.Empty | Returns validation error (400 or 422) |

### DeleteDepartmentAsync

- Signature: Task<ApiResponse<bool>> DeleteDepartmentAsync(Guid departmentId);
- Return Type: Task<ApiResponse<bool>>
- Parameters: Guid departmentId

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| DeleteDepartmentAsync-01 | HappyPath | Valid departmentId provided | Returns success payload matching declared return type |
| DeleteDepartmentAsync-02 | InvalidInput | departmentId = Guid.Empty | Returns validation error (400 or 422) or equivalent domain error |
| DeleteDepartmentAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on departmentId | Returns unauthorized or forbidden response, or operation rejected by policy |
| DeleteDepartmentAsync-DEPARTMENTID-EmptyGuid | InvalidInput | departmentId = Guid.Empty | Returns validation error (400 or 422) |

### CreateMembershipAsync

- Signature: Task<ApiResponse<MembershipResponse>> CreateMembershipAsync(CreateMembershipRequest request);
- Return Type: Task<ApiResponse<MembershipResponse>>
- Parameters: CreateMembershipRequest request

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| CreateMembershipAsync-01 | HappyPath | Valid request provided | Returns success payload matching declared return type |
| CreateMembershipAsync-02 | InvalidInput | request with missing required fields | Returns validation error (400 or 422) or equivalent domain error |
| CreateMembershipAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on request | Returns unauthorized or forbidden response, or operation rejected by policy |

### GetMembershipByIdAsync

- Signature: Task<ApiResponse<MembershipResponse>> GetMembershipByIdAsync(Guid membershipId);
- Return Type: Task<ApiResponse<MembershipResponse>>
- Parameters: Guid membershipId

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetMembershipByIdAsync-01 | HappyPath | Valid membershipId provided | Returns success payload matching declared return type |
| GetMembershipByIdAsync-02 | InvalidInput | membershipId = Guid.Empty | Returns validation error (400 or 422) or equivalent domain error |
| GetMembershipByIdAsync-03 | NotFoundOrNoData | membershipId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| GetMembershipByIdAsync-MEMBERSHIPID-EmptyGuid | InvalidInput | membershipId = Guid.Empty | Returns validation error (400 or 422) |

### GetMembershipsByOrgAsync

- Signature: Task<PagedResponse<MembershipResponse>> GetMembershipsByOrgAsync(Guid orgId, int page = 1, int pageSize = 10);
- Return Type: Task<PagedResponse<MembershipResponse>>
- Parameters: Guid orgId, int page = 1, int pageSize = 10

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetMembershipsByOrgAsync-01 | HappyPath | Valid orgId, 1, 10 provided | Returns success payload matching declared return type |
| GetMembershipsByOrgAsync-02 | InvalidInput | orgId = Guid.Empty OR 1 <= 0 OR 10 <= 0 | Returns validation error (400 or 422) or equivalent domain error |
| GetMembershipsByOrgAsync-03 | NotFoundOrNoData | orgId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| GetMembershipsByOrgAsync-04 | PagingBoundary | Large page number or page size out of bounds | Returns valid paging metadata; out-of-range page returns empty item set |
| GetMembershipsByOrgAsync-ORGID-EmptyGuid | InvalidInput | orgId = Guid.Empty | Returns validation error (400 or 422) |
| GetMembershipsByOrgAsync-PAGE-ZeroOrNegative | InvalidInput | page <= 0 | Returns validation error (400 or 422) |
| GetMembershipsByOrgAsync-PAGESIZE-ZeroOrNegative | InvalidInput | pageSize <= 0 | Returns validation error (400 or 422) |

### GetMembershipsByUserAsync

- Signature: Task<PagedResponse<MembershipResponse>> GetMembershipsByUserAsync(Guid userId, int page = 1, int pageSize = 10);
- Return Type: Task<PagedResponse<MembershipResponse>>
- Parameters: Guid userId, int page = 1, int pageSize = 10

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetMembershipsByUserAsync-01 | HappyPath | Valid userId, 1, 10 provided | Returns success payload matching declared return type |
| GetMembershipsByUserAsync-02 | InvalidInput | userId = Guid.Empty OR 1 <= 0 OR 10 <= 0 | Returns validation error (400 or 422) or equivalent domain error |
| GetMembershipsByUserAsync-03 | NotFoundOrNoData | userId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| GetMembershipsByUserAsync-04 | PagingBoundary | Large page number or page size out of bounds | Returns valid paging metadata; out-of-range page returns empty item set |
| GetMembershipsByUserAsync-USERID-EmptyGuid | InvalidInput | userId = Guid.Empty | Returns validation error (400 or 422) |
| GetMembershipsByUserAsync-PAGE-ZeroOrNegative | InvalidInput | page <= 0 | Returns validation error (400 or 422) |
| GetMembershipsByUserAsync-PAGESIZE-ZeroOrNegative | InvalidInput | pageSize <= 0 | Returns validation error (400 or 422) |

### SearchDoctorsAsync

- Signature: Task<PagedResponse<MembershipResponse>> SearchDoctorsAsync(SearchDoctorsRequest request);
- Return Type: Task<PagedResponse<MembershipResponse>>
- Parameters: SearchDoctorsRequest request

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| SearchDoctorsAsync-01 | HappyPath | Valid request provided | Returns success payload matching declared return type |
| SearchDoctorsAsync-02 | InvalidInput | request with missing required fields | Returns validation error (400 or 422) or equivalent domain error |
| SearchDoctorsAsync-03 | NotFoundOrNoData | Requested resource not found | Returns null, empty, false, or not-found response according to contract |
| SearchDoctorsAsync-04 | PagingBoundary | Large page number or page size out of bounds | Returns valid paging metadata; out-of-range page returns empty item set |

### UpdateMembershipAsync

- Signature: Task<ApiResponse<MembershipResponse>> UpdateMembershipAsync(Guid membershipId, UpdateMembershipRequest request);
- Return Type: Task<ApiResponse<MembershipResponse>>
- Parameters: Guid membershipId, UpdateMembershipRequest request

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| UpdateMembershipAsync-01 | HappyPath | Valid membershipId, request provided | Returns success payload matching declared return type |
| UpdateMembershipAsync-02 | InvalidInput | membershipId = Guid.Empty OR request with missing required fields | Returns validation error (400 or 422) or equivalent domain error |
| UpdateMembershipAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on membershipId, request | Returns unauthorized or forbidden response, or operation rejected by policy |
| UpdateMembershipAsync-MEMBERSHIPID-EmptyGuid | InvalidInput | membershipId = Guid.Empty | Returns validation error (400 or 422) |

### DeleteMembershipAsync

- Signature: Task<ApiResponse<bool>> DeleteMembershipAsync(Guid membershipId);
- Return Type: Task<ApiResponse<bool>>
- Parameters: Guid membershipId

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| DeleteMembershipAsync-01 | HappyPath | Valid membershipId provided | Returns success payload matching declared return type |
| DeleteMembershipAsync-02 | InvalidInput | membershipId = Guid.Empty | Returns validation error (400 or 422) or equivalent domain error |
| DeleteMembershipAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on membershipId | Returns unauthorized or forbidden response, or operation rejected by policy |
| DeleteMembershipAsync-MEMBERSHIPID-EmptyGuid | InvalidInput | membershipId = Guid.Empty | Returns validation error (400 or 422) |

### ConfigurePaymentAsync

- Signature: Task<ApiResponse<PaymentConfigStatusResponse>> ConfigurePaymentAsync(Guid orgId, ConfigurePaymentRequest request);
- Return Type: Task<ApiResponse<PaymentConfigStatusResponse>>
- Parameters: Guid orgId, ConfigurePaymentRequest request

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| ConfigurePaymentAsync-01 | HappyPath | Valid orgId, request provided | Returns success payload matching declared return type |
| ConfigurePaymentAsync-02 | InvalidInput | orgId = Guid.Empty OR request with missing required fields | Returns validation error (400 or 422) or equivalent domain error |
| ConfigurePaymentAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on orgId, request | Returns unauthorized or forbidden response, or operation rejected by policy |
| ConfigurePaymentAsync-ORGID-EmptyGuid | InvalidInput | orgId = Guid.Empty | Returns validation error (400 or 422) |

### UpdatePaymentConfigAsync

- Signature: Task<ApiResponse<PaymentConfigStatusResponse>> UpdatePaymentConfigAsync(Guid orgId, ConfigurePaymentRequest request);
- Return Type: Task<ApiResponse<PaymentConfigStatusResponse>>
- Parameters: Guid orgId, ConfigurePaymentRequest request

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| UpdatePaymentConfigAsync-01 | HappyPath | Valid orgId, request provided | Returns success payload matching declared return type |
| UpdatePaymentConfigAsync-02 | InvalidInput | orgId = Guid.Empty OR request with missing required fields | Returns validation error (400 or 422) or equivalent domain error |
| UpdatePaymentConfigAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on orgId, request | Returns unauthorized or forbidden response, or operation rejected by policy |
| UpdatePaymentConfigAsync-ORGID-EmptyGuid | InvalidInput | orgId = Guid.Empty | Returns validation error (400 or 422) |

### GetPaymentConfigStatusAsync

- Signature: Task<ApiResponse<PaymentConfigStatusResponse>> GetPaymentConfigStatusAsync(Guid orgId);
- Return Type: Task<ApiResponse<PaymentConfigStatusResponse>>
- Parameters: Guid orgId

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetPaymentConfigStatusAsync-01 | HappyPath | Valid orgId provided | Returns success payload matching declared return type |
| GetPaymentConfigStatusAsync-02 | InvalidInput | orgId = Guid.Empty | Returns validation error (400 or 422) or equivalent domain error |
| GetPaymentConfigStatusAsync-03 | NotFoundOrNoData | orgId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| GetPaymentConfigStatusAsync-04 | UnauthorizedOrForbidden | User lacks permission for this action on orgId | Returns unauthorized or forbidden response, or operation rejected by policy |
| GetPaymentConfigStatusAsync-ORGID-EmptyGuid | InvalidInput | orgId = Guid.Empty | Returns validation error (400 or 422) |

### GetPaymentKeysAsync

- Signature: Task<ApiResponse<PaymentKeysResponse>> GetPaymentKeysAsync(Guid orgId);
- Return Type: Task<ApiResponse<PaymentKeysResponse>>
- Parameters: Guid orgId

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetPaymentKeysAsync-01 | HappyPath | Valid orgId provided | Returns success payload matching declared return type |
| GetPaymentKeysAsync-02 | InvalidInput | orgId = Guid.Empty | Returns validation error (400 or 422) or equivalent domain error |
| GetPaymentKeysAsync-03 | NotFoundOrNoData | orgId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| GetPaymentKeysAsync-04 | UnauthorizedOrForbidden | User lacks permission for this action on orgId | Returns unauthorized or forbidden response, or operation rejected by policy |
| GetPaymentKeysAsync-ORGID-EmptyGuid | InvalidInput | orgId = Guid.Empty | Returns validation error (400 or 422) |

## IAuthUserClient

### GetDoctorByUserIdInMyOrganizationAsync

- Signature: Task<DoctorUserInfoDto?> GetDoctorByUserIdInMyOrganizationAsync(string bearerToken, Guid orgId, Guid userId);
- Return Type: Task<DoctorUserInfoDto?>
- Parameters: string bearerToken, Guid orgId, Guid userId

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetDoctorByUserIdInMyOrganizationAsync-01 | HappyPath | Valid bearerToken, orgId, userId provided | Returns success payload matching declared return type |
| GetDoctorByUserIdInMyOrganizationAsync-02 | InvalidInput | bearerToken = null/empty OR orgId = Guid.Empty OR userId = Guid.Empty | Returns validation error (400 or 422) or equivalent domain error |
| GetDoctorByUserIdInMyOrganizationAsync-03 | NotFoundOrNoData | orgId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| GetDoctorByUserIdInMyOrganizationAsync-04 | NullableReturn | Valid orgId but no associated resource | Returns null without throwing |
| GetDoctorByUserIdInMyOrganizationAsync-BEARERTOKEN-EmptyString | InvalidInput | bearerToken = null or empty string | Returns validation error (400 or 422) |
| GetDoctorByUserIdInMyOrganizationAsync-ORGID-EmptyGuid | InvalidInput | orgId = Guid.Empty | Returns validation error (400 or 422) |
| GetDoctorByUserIdInMyOrganizationAsync-USERID-EmptyGuid | InvalidInput | userId = Guid.Empty | Returns validation error (400 or 422) |

### GetUserIdByPatientIdAsync

- Signature: Task<Guid?> GetUserIdByPatientIdAsync(string bearerToken, Guid patientId);
- Return Type: Task<Guid?>
- Parameters: string bearerToken, Guid patientId

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetUserIdByPatientIdAsync-01 | HappyPath | Valid bearerToken, patientId provided | Returns success payload matching declared return type |
| GetUserIdByPatientIdAsync-02 | InvalidInput | bearerToken = null/empty OR patientId = Guid.Empty | Returns validation error (400 or 422) or equivalent domain error |
| GetUserIdByPatientIdAsync-03 | NotFoundOrNoData | patientId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| GetUserIdByPatientIdAsync-04 | NullableReturn | Valid patientId but no associated resource | Returns null without throwing |
| GetUserIdByPatientIdAsync-BEARERTOKEN-EmptyString | InvalidInput | bearerToken = null or empty string | Returns validation error (400 or 422) |
| GetUserIdByPatientIdAsync-PATIENTID-EmptyGuid | InvalidInput | patientId = Guid.Empty | Returns validation error (400 or 422) |

### GetUserIdByDoctorIdAsync

- Signature: Task<Guid?> GetUserIdByDoctorIdAsync(string bearerToken, Guid doctorId);
- Return Type: Task<Guid?>
- Parameters: string bearerToken, Guid doctorId

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetUserIdByDoctorIdAsync-01 | HappyPath | Valid bearerToken, doctorId provided | Returns success payload matching declared return type |
| GetUserIdByDoctorIdAsync-02 | InvalidInput | bearerToken = null/empty OR doctorId = Guid.Empty | Returns validation error (400 or 422) or equivalent domain error |
| GetUserIdByDoctorIdAsync-03 | NotFoundOrNoData | doctorId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| GetUserIdByDoctorIdAsync-04 | NullableReturn | Valid doctorId but no associated resource | Returns null without throwing |
| GetUserIdByDoctorIdAsync-BEARERTOKEN-EmptyString | InvalidInput | bearerToken = null or empty string | Returns validation error (400 or 422) |
| GetUserIdByDoctorIdAsync-DOCTORID-EmptyGuid | InvalidInput | doctorId = Guid.Empty | Returns validation error (400 or 422) |

### GetUserProfileDetailAsync

- Signature: Task<AuthUserProfileDetailDto?> GetUserProfileDetailAsync(string bearerToken, Guid userId);
- Return Type: Task<AuthUserProfileDetailDto?>
- Parameters: string bearerToken, Guid userId

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetUserProfileDetailAsync-01 | HappyPath | Valid bearerToken, userId provided | Returns success payload matching declared return type |
| GetUserProfileDetailAsync-02 | InvalidInput | bearerToken = null/empty OR userId = Guid.Empty | Returns validation error (400 or 422) or equivalent domain error |
| GetUserProfileDetailAsync-03 | NotFoundOrNoData | userId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| GetUserProfileDetailAsync-04 | NullableReturn | Valid userId but no associated resource | Returns null without throwing |
| GetUserProfileDetailAsync-BEARERTOKEN-EmptyString | InvalidInput | bearerToken = null or empty string | Returns validation error (400 or 422) |
| GetUserProfileDetailAsync-USERID-EmptyGuid | InvalidInput | userId = Guid.Empty | Returns validation error (400 or 422) |

