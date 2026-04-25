# Auth Service Test Cases

This document defines scenario coverage for each function declared in the service interfaces.

## IAuthService

### RegisterAsync

- Signature: Task<AuthResponse> RegisterAsync(RegisterRequest request);
- Return Type: Task<AuthResponse>
- Parameters: RegisterRequest request

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| RegisterAsync-01 | HappyPath | Valid request provided | Returns success payload matching declared return type |
| RegisterAsync-02 | InvalidInput | request with missing required fields | Returns validation error (400 or 422) or equivalent domain error |
| RegisterAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on request | Returns unauthorized or forbidden response, or operation rejected by policy |
| RegisterAsync-InvalidEmail | InvalidInput | Email = "invalid-email-format" | Returns validation error (400 or 422) |
| RegisterAsync-WeakPassword | InvalidInput | Password = "weak" (does not meet complexity requirements) | Returns validation error (400 or 422) |
| RegisterAsync-DuplicateEmail | InvalidInput | Email already exists in system | Returns validation error (400 or 422) |

### RegisterDoctorAsync

- Signature: Task<AuthResponse> RegisterDoctorAsync(RegisterDoctorRequest request);
- Return Type: Task<AuthResponse>
- Parameters: RegisterDoctorRequest request

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| RegisterDoctorAsync-01 | HappyPath | Valid request provided | Returns success payload matching declared return type |
| RegisterDoctorAsync-02 | InvalidInput | request with missing required fields | Returns validation error (400 or 422) or equivalent domain error |
| RegisterDoctorAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on request | Returns unauthorized or forbidden response, or operation rejected by policy |
| RegisterDoctorAsync-InvalidEmail | InvalidInput | Email = "invalid-email-format" | Returns validation error (400 or 422) |
| RegisterDoctorAsync-WeakPassword | InvalidInput | Password = "weak" (does not meet complexity requirements) | Returns validation error (400 or 422) |
| RegisterDoctorAsync-DuplicateEmail | InvalidInput | Email already exists in system | Returns validation error (400 or 422) |

### RegisterStaffAsync

- Signature: Task<AuthResponse> RegisterStaffAsync(RegisterStaffRequest request);
- Return Type: Task<AuthResponse>
- Parameters: RegisterStaffRequest request

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| RegisterStaffAsync-01 | HappyPath | Valid request provided | Returns success payload matching declared return type |
| RegisterStaffAsync-02 | InvalidInput | request with missing required fields | Returns validation error (400 or 422) or equivalent domain error |
| RegisterStaffAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on request | Returns unauthorized or forbidden response, or operation rejected by policy |
| RegisterStaffAsync-InvalidEmail | InvalidInput | Email = "invalid-email-format" | Returns validation error (400 or 422) |
| RegisterStaffAsync-WeakPassword | InvalidInput | Password = "weak" (does not meet complexity requirements) | Returns validation error (400 or 422) |
| RegisterStaffAsync-DuplicateEmail | InvalidInput | Email already exists in system | Returns validation error (400 or 422) |

### RegisterStaffDoctorAsync

- Signature: Task<AuthResponse> RegisterStaffDoctorAsync(RegisterStaffDoctorRequest request);
- Return Type: Task<AuthResponse>
- Parameters: RegisterStaffDoctorRequest request

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| RegisterStaffDoctorAsync-01 | HappyPath | Valid request provided | Returns success payload matching declared return type |
| RegisterStaffDoctorAsync-02 | InvalidInput | request with missing required fields | Returns validation error (400 or 422) or equivalent domain error |
| RegisterStaffDoctorAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on request | Returns unauthorized or forbidden response, or operation rejected by policy |
| RegisterStaffDoctorAsync-InvalidEmail | InvalidInput | Email = "invalid-email-format" | Returns validation error (400 or 422) |
| RegisterStaffDoctorAsync-WeakPassword | InvalidInput | Password = "weak" (does not meet complexity requirements) | Returns validation error (400 or 422) |
| RegisterStaffDoctorAsync-DuplicateEmail | InvalidInput | Email already exists in system | Returns validation error (400 or 422) |

### VerifyDoctorAsync

- Signature: Task<AuthResponse> VerifyDoctorAsync(Guid doctorId);
- Return Type: Task<AuthResponse>
- Parameters: Guid doctorId

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| VerifyDoctorAsync-01 | HappyPath | Valid doctorId provided | Returns success payload matching declared return type |
| VerifyDoctorAsync-02 | InvalidInput | doctorId = Guid.Empty | Returns validation error (400 or 422) or equivalent domain error |
| VerifyDoctorAsync-03 | NotFoundOrNoData | doctorId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| VerifyDoctorAsync-DOCTORID-EmptyGuid | InvalidInput | doctorId = Guid.Empty | Returns validation error (400 or 422) |

### VerifyStaffAsync

- Signature: Task<AuthResponse> VerifyStaffAsync(Guid staffId);
- Return Type: Task<AuthResponse>
- Parameters: Guid staffId

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| VerifyStaffAsync-01 | HappyPath | Valid staffId provided | Returns success payload matching declared return type |
| VerifyStaffAsync-02 | InvalidInput | staffId = Guid.Empty | Returns validation error (400 or 422) or equivalent domain error |
| VerifyStaffAsync-03 | NotFoundOrNoData | staffId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| VerifyStaffAsync-STAFFID-EmptyGuid | InvalidInput | staffId = Guid.Empty | Returns validation error (400 or 422) |

### UpdateRoleAsync

- Signature: Task<AuthResponse> UpdateRoleAsync(UpdateRoleRequest request);
- Return Type: Task<AuthResponse>
- Parameters: UpdateRoleRequest request

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| UpdateRoleAsync-01 | HappyPath | Valid request provided | Returns success payload matching declared return type |
| UpdateRoleAsync-02 | InvalidInput | request with missing required fields | Returns validation error (400 or 422) or equivalent domain error |
| UpdateRoleAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on request | Returns unauthorized or forbidden response, or operation rejected by policy |

### UpdateUserAsync

- Signature: Task<AuthResponse> UpdateUserAsync(Guid userId, AdminUpdateUserRequest request);
- Return Type: Task<AuthResponse>
- Parameters: Guid userId, AdminUpdateUserRequest request

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| UpdateUserAsync-01 | HappyPath | Valid userId, request provided | Returns success payload matching declared return type |
| UpdateUserAsync-02 | InvalidInput | userId = Guid.Empty OR request with missing required fields | Returns validation error (400 or 422) or equivalent domain error |
| UpdateUserAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on userId, request | Returns unauthorized or forbidden response, or operation rejected by policy |
| UpdateUserAsync-USERID-EmptyGuid | InvalidInput | userId = Guid.Empty | Returns validation error (400 or 422) |

### ChangePasswordAsync

- Signature: Task<AuthResponse> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, bool isAdminOverride = false);
- Return Type: Task<AuthResponse>
- Parameters: Guid userId, ChangePasswordRequest request, bool isAdminOverride = false

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| ChangePasswordAsync-01 | HappyPath | Valid userId, request, false provided | Returns success payload matching declared return type |
| ChangePasswordAsync-02 | InvalidInput | userId = Guid.Empty OR request with missing required fields OR Invalid false | Returns validation error (400 or 422) or equivalent domain error |
| ChangePasswordAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on userId, request, false | Returns unauthorized or forbidden response, or operation rejected by policy |
| ChangePasswordAsync-USERID-EmptyGuid | InvalidInput | userId = Guid.Empty | Returns validation error (400 or 422) |

### AdminChangePasswordAsync

- Signature: Task<AuthResponse> AdminChangePasswordAsync(Guid userId, AdminChangePasswordRequest request);
- Return Type: Task<AuthResponse>
- Parameters: Guid userId, AdminChangePasswordRequest request

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| AdminChangePasswordAsync-01 | HappyPath | Valid userId, request provided | Returns success payload matching declared return type |
| AdminChangePasswordAsync-02 | InvalidInput | userId = Guid.Empty OR request with missing required fields | Returns validation error (400 or 422) or equivalent domain error |
| AdminChangePasswordAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on userId, request | Returns unauthorized or forbidden response, or operation rejected by policy |
| AdminChangePasswordAsync-USERID-EmptyGuid | InvalidInput | userId = Guid.Empty | Returns validation error (400 or 422) |

### LoginAsync

- Signature: Task<AuthResponse> LoginAsync(LoginRequest request, string ipAddress);
- Return Type: Task<AuthResponse>
- Parameters: LoginRequest request, string ipAddress

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| LoginAsync-01 | HappyPath | Valid request, ipAddress provided | Returns success payload matching declared return type |
| LoginAsync-02 | InvalidInput | request with missing required fields OR ipAddress = null/empty | Returns validation error (400 or 422) or equivalent domain error |
| LoginAsync-IPADDRESS-EmptyString | InvalidInput | ipAddress = null or empty string | Returns validation error (400 or 422) |
| LoginAsync-InvalidCredentials | InvalidInput | Invalid email or password | Returns authentication error |
| LoginAsync-InactiveAccount | InvalidInput | Account is deactivated | Returns authentication error |

### RefreshTokenAsync

- Signature: Task<AuthResponse> RefreshTokenAsync(string refreshToken);
- Return Type: Task<AuthResponse>
- Parameters: string refreshToken

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| RefreshTokenAsync-01 | HappyPath | Valid refreshToken provided | Returns success payload matching declared return type |
| RefreshTokenAsync-02 | InvalidInput | refreshToken = null/empty | Returns validation error (400 or 422) or equivalent domain error |
| RefreshTokenAsync-03 | NotFoundOrNoData | Requested resource not found | Returns null, empty, false, or not-found response according to contract |
| RefreshTokenAsync-REFRESHTOKEN-EmptyString | InvalidInput | refreshToken = null or empty string | Returns validation error (400 or 422) |

### RevokeTokenAsync

- Signature: Task<bool> RevokeTokenAsync(Guid userId);
- Return Type: Task<bool>
- Parameters: Guid userId

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| RevokeTokenAsync-01 | HappyPath | Valid userId provided | Returns success payload matching declared return type |
| RevokeTokenAsync-02 | InvalidInput | userId = Guid.Empty | Returns validation error (400 or 422) or equivalent domain error |
| RevokeTokenAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on userId | Returns unauthorized or forbidden response, or operation rejected by policy |
| RevokeTokenAsync-04 | BooleanFalsePath | Specific input for BooleanFalsePath with userId | Returns false |
| RevokeTokenAsync-05 | BooleanTruePath | Specific input for BooleanTruePath with userId | Returns true |
| RevokeTokenAsync-USERID-EmptyGuid | InvalidInput | userId = Guid.Empty | Returns validation error (400 or 422) |

### GetMyProfileAsync

- Signature: Task<UserProfileResponse?> GetMyProfileAsync(Guid userId);
- Return Type: Task<UserProfileResponse?>
- Parameters: Guid userId

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetMyProfileAsync-01 | HappyPath | Valid userId provided | Returns success payload matching declared return type |
| GetMyProfileAsync-02 | InvalidInput | userId = Guid.Empty | Returns validation error (400 or 422) or equivalent domain error |
| GetMyProfileAsync-03 | NotFoundOrNoData | userId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| GetMyProfileAsync-04 | NullableReturn | Valid userId but no associated resource | Returns null without throwing |
| GetMyProfileAsync-USERID-EmptyGuid | InvalidInput | userId = Guid.Empty | Returns validation error (400 or 422) |

### GetProfileByContactAsync

- Signature: Task<UserProfileResponse?> GetProfileByContactAsync(string? email, string? phone);
- Return Type: Task<UserProfileResponse?>
- Parameters: string? email, string? phone

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetProfileByContactAsync-01 | HappyPath | Valid email, phone provided | Returns success payload matching declared return type |
| GetProfileByContactAsync-02 | InvalidInput | email = null/empty OR phone = null/empty | Returns validation error (400 or 422) or equivalent domain error |
| GetProfileByContactAsync-03 | NotFoundOrNoData | Requested resource not found | Returns null, empty, false, or not-found response according to contract |
| GetProfileByContactAsync-04 | NullableReturn | Valid input, but resource is naturally null | Returns null without throwing |

### UpdateProfileAsync

- Signature: Task<AuthResponse> UpdateProfileAsync(Guid userId, UpdateProfileRequest request);
- Return Type: Task<AuthResponse>
- Parameters: Guid userId, UpdateProfileRequest request

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| UpdateProfileAsync-01 | HappyPath | Valid userId, request provided | Returns success payload matching declared return type |
| UpdateProfileAsync-02 | InvalidInput | userId = Guid.Empty OR request with missing required fields | Returns validation error (400 or 422) or equivalent domain error |
| UpdateProfileAsync-03 | NotFoundOrNoData | userId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| UpdateProfileAsync-04 | UnauthorizedOrForbidden | User lacks permission for this action on userId, request | Returns unauthorized or forbidden response, or operation rejected by policy |
| UpdateProfileAsync-USERID-EmptyGuid | InvalidInput | userId = Guid.Empty | Returns validation error (400 or 422) |

### GetUserIdByProfileIdAsync

- Signature: Task<Guid?> GetUserIdByProfileIdAsync(Guid? patientId, Guid? doctorId);
- Return Type: Task<Guid?>
- Parameters: Guid? patientId, Guid? doctorId

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetUserIdByProfileIdAsync-01 | HappyPath | Valid patientId, doctorId provided | Returns success payload matching declared return type |
| GetUserIdByProfileIdAsync-02 | InvalidInput | patientId = Guid.Empty OR doctorId = Guid.Empty | Returns validation error (400 or 422) or equivalent domain error |
| GetUserIdByProfileIdAsync-03 | NotFoundOrNoData | patientId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| GetUserIdByProfileIdAsync-04 | NullableReturn | Valid patientId but no associated resource | Returns null without throwing |
| GetUserIdByProfileIdAsync-PATIENTID-EmptyGuid | InvalidInput | patientId = Guid.Empty | Returns validation error (400 or 422) |
| GetUserIdByProfileIdAsync-DOCTORID-EmptyGuid | InvalidInput | doctorId = Guid.Empty | Returns validation error (400 or 422) |

### GetUserKeysAsync

- Signature: Task<UserKeysDto?> GetUserKeysAsync(Guid userId);
- Return Type: Task<UserKeysDto?>
- Parameters: Guid userId

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetUserKeysAsync-01 | HappyPath | Valid userId provided | Returns success payload matching declared return type |
| GetUserKeysAsync-02 | InvalidInput | userId = Guid.Empty | Returns validation error (400 or 422) or equivalent domain error |
| GetUserKeysAsync-03 | NotFoundOrNoData | userId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| GetUserKeysAsync-04 | NullableReturn | Valid userId but no associated resource | Returns null without throwing |
| GetUserKeysAsync-USERID-EmptyGuid | InvalidInput | userId = Guid.Empty | Returns validation error (400 or 422) |

### GetAllUsersAsync

- Signature: Task<PagedResponse<UserProfileResponse>> GetAllUsersAsync(GetAllUsersQuery query, bool isAdminActor);
- Return Type: Task<PagedResponse<UserProfileResponse>>
- Parameters: GetAllUsersQuery query, bool isAdminActor



### DeactivateAccountAsync

- Signature: Task<AuthResponse> DeactivateAccountAsync(Guid userId);
- Return Type: Task<AuthResponse>
- Parameters: Guid userId

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| DeactivateAccountAsync-01 | HappyPath | Valid userId provided | Returns success payload matching declared return type |
| DeactivateAccountAsync-02 | InvalidInput | userId = Guid.Empty | Returns validation error (400 or 422) or equivalent domain error |
| DeactivateAccountAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on userId | Returns unauthorized or forbidden response, or operation rejected by policy |
| DeactivateAccountAsync-USERID-EmptyGuid | InvalidInput | userId = Guid.Empty | Returns validation error (400 or 422) |

## ITokenService

### GenerateToken

- Signature: string GenerateToken(Guid userId, string email, string fullName, string organizationId, IEnumerable<string> roles);
- Return Type: string
- Parameters: Guid userId, string email, string fullName, string organizationId, IEnumerable<string> roles

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GenerateToken-01 | HappyPath | Valid userId, email, fullName, organizationId, roles provided | Returns success payload matching declared return type |
| GenerateToken-02 | InvalidInput | userId = Guid.Empty OR email = null/empty OR fullName = null/empty OR organizationId = null/empty OR roles = null/empty | Returns validation error (400 or 422) or equivalent domain error |
| GenerateToken-USERID-EmptyGuid | InvalidInput | userId = Guid.Empty | Returns validation error (400 or 422) |
| GenerateToken-EMAIL-EmptyString | InvalidInput | email = null or empty string | Returns validation error (400 or 422) |
| GenerateToken-FULLNAME-EmptyString | InvalidInput | fullName = null or empty string | Returns validation error (400 or 422) |
| GenerateToken-ORGANIZATIONID-EmptyString | InvalidInput | organizationId = null or empty string | Returns validation error (400 or 422) |

### GenerateRefreshToken

- Signature: string GenerateRefreshToken();
- Return Type: string
- Parameters: 

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GenerateRefreshToken-01 | HappyPath | No parameters, valid state | Returns success payload matching declared return type |
| GenerateRefreshToken-02 | InvalidInput | N/A | Returns validation error (400 or 422) or equivalent domain error |
| GenerateRefreshToken-03 | NotFoundOrNoData | Data not present in DB | Returns null, empty, false, or not-found response according to contract |

### GetPrincipalFromExpiredToken

- Signature: ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
- Return Type: ClaimsPrincipal
- Parameters: string token

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetPrincipalFromExpiredToken-01 | HappyPath | Valid token provided | Returns success payload matching declared return type |
| GetPrincipalFromExpiredToken-02 | InvalidInput | token = null/empty | Returns validation error (400 or 422) or equivalent domain error |
| GetPrincipalFromExpiredToken-03 | NotFoundOrNoData | Requested resource not found | Returns null, empty, false, or not-found response according to contract |
| GetPrincipalFromExpiredToken-TOKEN-EmptyString | InvalidInput | token = null or empty string | Returns validation error (400 or 422) |

## IOrganizationServiceClient

### CreateMembershipAsync

- Signature: Task<OrganizationServiceResponse<CreateMembershipResponse>> CreateMembershipAsync(Guid userId, Guid organizationId, Guid? departmentId = null, string? jobTitle = null);
- Return Type: Task<OrganizationServiceResponse<CreateMembershipResponse>>
- Parameters: Guid userId, Guid organizationId, Guid? departmentId = null, string? jobTitle = null

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| CreateMembershipAsync-01 | HappyPath | Valid userId, organizationId, null, null provided | Returns success payload matching declared return type |
| CreateMembershipAsync-02 | InvalidInput | userId = Guid.Empty OR organizationId = Guid.Empty OR null = Guid.Empty OR null = null/empty | Returns validation error (400 or 422) or equivalent domain error |
| CreateMembershipAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on userId, organizationId, null, null | Returns unauthorized or forbidden response, or operation rejected by policy |
| CreateMembershipAsync-USERID-EmptyGuid | InvalidInput | userId = Guid.Empty | Returns validation error (400 or 422) |
| CreateMembershipAsync-ORGANIZATIONID-EmptyGuid | InvalidInput | organizationId = Guid.Empty | Returns validation error (400 or 422) |
| CreateMembershipAsync-DEPARTMENTID-EmptyGuid | InvalidInput | departmentId = Guid.Empty | Returns validation error (400 or 422) |

## IGenericRepository<T>

### GetAllAsync

- Signature: Task<IEnumerable<T>> GetAllAsync();
- Return Type: Task<IEnumerable<T>>
- Parameters: 

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetAllAsync-01 | HappyPath | No parameters, valid state | Returns success payload matching declared return type |
| GetAllAsync-02 | InvalidInput | N/A | Returns validation error (400 or 422) or equivalent domain error |
| GetAllAsync-03 | NotFoundOrNoData | Data not present in DB | Returns null, empty, false, or not-found response according to contract |
| GetAllAsync-04 | EmptyCollection | Specific conditions met | Returns empty collection, not null |

### GetByIdAsync

- Signature: Task<T?> GetByIdAsync(object id);
- Return Type: Task<T?>
- Parameters: object id

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetByIdAsync-01 | HappyPath | Valid id provided | Returns success payload matching declared return type |
| GetByIdAsync-02 | InvalidInput | Invalid id | Returns validation error (400 or 422) or equivalent domain error |
| GetByIdAsync-03 | NotFoundOrNoData | Requested resource not found | Returns null, empty, false, or not-found response according to contract |
| GetByIdAsync-04 | NullableReturn | Valid input, but resource is naturally null | Returns null without throwing |

### FindAsync

- Signature: Task<T?> FindAsync(Expression<Func<T, bool>> predicate);
- Return Type: Task<T?>
- Parameters: Expression<Func<T, bool>> predicate

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| FindAsync-01 | HappyPath | Valid Expression<Func<T, predicate provided | Returns success payload matching declared return type |
| FindAsync-02 | InvalidInput | Invalid Expression<Func<T OR Invalid predicate | Returns validation error (400 or 422) or equivalent domain error |
| FindAsync-03 | NotFoundOrNoData | Requested resource not found | Returns null, empty, false, or not-found response according to contract |
| FindAsync-04 | NullableReturn | Valid input, but resource is naturally null | Returns null without throwing |

### FindManyAsync

- Signature: Task<IEnumerable<T>> FindManyAsync(Expression<Func<T, bool>> predicate);
- Return Type: Task<IEnumerable<T>>
- Parameters: Expression<Func<T, bool>> predicate

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| FindManyAsync-01 | HappyPath | Valid Expression<Func<T, predicate provided | Returns success payload matching declared return type |
| FindManyAsync-02 | InvalidInput | Invalid Expression<Func<T OR Invalid predicate | Returns validation error (400 or 422) or equivalent domain error |
| FindManyAsync-03 | NotFoundOrNoData | Requested resource not found | Returns null, empty, false, or not-found response according to contract |
| FindManyAsync-04 | EmptyCollection | Specific input for EmptyCollection with Expression<Func<T, predicate | Returns empty collection, not null |

### AddAsync

- Signature: Task AddAsync(T entity);
- Return Type: Task
- Parameters: T entity

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| AddAsync-01 | HappyPath | Valid entity provided | Returns success payload matching declared return type |
| AddAsync-02 | InvalidInput | Invalid entity | Returns validation error (400 or 422) or equivalent domain error |
| AddAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on entity | Returns unauthorized or forbidden response, or operation rejected by policy |

### UpdateAsync

- Signature: Task UpdateAsync(T entity);
- Return Type: Task
- Parameters: T entity

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| UpdateAsync-01 | HappyPath | Valid entity provided | Returns success payload matching declared return type |
| UpdateAsync-02 | InvalidInput | Invalid entity | Returns validation error (400 or 422) or equivalent domain error |
| UpdateAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on entity | Returns unauthorized or forbidden response, or operation rejected by policy |

### DeleteAsync

- Signature: Task DeleteAsync(T entity);
- Return Type: Task
- Parameters: T entity

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| DeleteAsync-01 | HappyPath | Valid entity provided | Returns success payload matching declared return type |
| DeleteAsync-02 | InvalidInput | Invalid entity | Returns validation error (400 or 422) or equivalent domain error |
| DeleteAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on entity | Returns unauthorized or forbidden response, or operation rejected by policy |

### ExistsAsync

- Signature: Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);
- Return Type: Task<bool>
- Parameters: Expression<Func<T, bool>> predicate

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| ExistsAsync-01 | HappyPath | Valid Expression<Func<T, predicate provided | Returns success payload matching declared return type |
| ExistsAsync-02 | InvalidInput | Invalid Expression<Func<T OR Invalid predicate | Returns validation error (400 or 422) or equivalent domain error |
| ExistsAsync-03 | BooleanFalsePath | Specific input for BooleanFalsePath with Expression<Func<T, predicate | Returns false |
| ExistsAsync-04 | BooleanTruePath | Specific input for BooleanTruePath with Expression<Func<T, predicate | Returns true |

## IUserRepository

### GetByEmailWithRolesAsync

- Signature: Task<User?> GetByEmailWithRolesAsync(string email);
- Return Type: Task<User?>
- Parameters: string email

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetByEmailWithRolesAsync-01 | HappyPath | Valid email provided | Returns success payload matching declared return type |
| GetByEmailWithRolesAsync-02 | InvalidInput | email = null/empty | Returns validation error (400 or 422) or equivalent domain error |
| GetByEmailWithRolesAsync-03 | NotFoundOrNoData | Requested resource not found | Returns null, empty, false, or not-found response according to contract |
| GetByEmailWithRolesAsync-04 | NullableReturn | Valid input, but resource is naturally null | Returns null without throwing |
| GetByEmailWithRolesAsync-EMAIL-EmptyString | InvalidInput | email = null or empty string | Returns validation error (400 or 422) |

### GetByIdWithProfileAsync

- Signature: Task<User?> GetByIdWithProfileAsync(Guid userId);
- Return Type: Task<User?>
- Parameters: Guid userId

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetByIdWithProfileAsync-01 | HappyPath | Valid userId provided | Returns success payload matching declared return type |
| GetByIdWithProfileAsync-02 | InvalidInput | userId = Guid.Empty | Returns validation error (400 or 422) or equivalent domain error |
| GetByIdWithProfileAsync-03 | NotFoundOrNoData | userId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| GetByIdWithProfileAsync-04 | NullableReturn | Valid userId but no associated resource | Returns null without throwing |
| GetByIdWithProfileAsync-USERID-EmptyGuid | InvalidInput | userId = Guid.Empty | Returns validation error (400 or 422) |

### GetByEmailWithProfileAsync

- Signature: Task<User?> GetByEmailWithProfileAsync(string email);
- Return Type: Task<User?>
- Parameters: string email

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetByEmailWithProfileAsync-01 | HappyPath | Valid email provided | Returns success payload matching declared return type |
| GetByEmailWithProfileAsync-02 | InvalidInput | email = null/empty | Returns validation error (400 or 422) or equivalent domain error |
| GetByEmailWithProfileAsync-03 | NotFoundOrNoData | Requested resource not found | Returns null, empty, false, or not-found response according to contract |
| GetByEmailWithProfileAsync-04 | NullableReturn | Valid input, but resource is naturally null | Returns null without throwing |
| GetByEmailWithProfileAsync-EMAIL-EmptyString | InvalidInput | email = null or empty string | Returns validation error (400 or 422) |

### GetByPhoneWithProfileAsync

- Signature: Task<User?> GetByPhoneWithProfileAsync(string phone);
- Return Type: Task<User?>
- Parameters: string phone

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetByPhoneWithProfileAsync-01 | HappyPath | Valid phone provided | Returns success payload matching declared return type |
| GetByPhoneWithProfileAsync-02 | InvalidInput | phone = null/empty | Returns validation error (400 or 422) or equivalent domain error |
| GetByPhoneWithProfileAsync-03 | NotFoundOrNoData | Requested resource not found | Returns null, empty, false, or not-found response according to contract |
| GetByPhoneWithProfileAsync-04 | NullableReturn | Valid input, but resource is naturally null | Returns null without throwing |
| GetByPhoneWithProfileAsync-PHONE-EmptyString | InvalidInput | phone = null or empty string | Returns validation error (400 or 422) |

### GetDoctorsByOrganizationAsync

- Signature: Task<List<User>> GetDoctorsByOrganizationAsync(string organizationId);
- Return Type: Task<List<User>>
- Parameters: string organizationId

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetDoctorsByOrganizationAsync-01 | HappyPath | Valid organizationId provided | Returns success payload matching declared return type |
| GetDoctorsByOrganizationAsync-02 | InvalidInput | organizationId = null/empty | Returns validation error (400 or 422) or equivalent domain error |
| GetDoctorsByOrganizationAsync-03 | NotFoundOrNoData | organizationId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| GetDoctorsByOrganizationAsync-04 | EmptyCollection | Specific input for EmptyCollection with organizationId | Returns empty collection, not null |
| GetDoctorsByOrganizationAsync-ORGANIZATIONID-EmptyString | InvalidInput | organizationId = null or empty string | Returns validation error (400 or 422) |

### GetDoctorByUserIdAndOrganizationAsync

- Signature: Task<User?> GetDoctorByUserIdAndOrganizationAsync(Guid userId, string organizationId);
- Return Type: Task<User?>
- Parameters: Guid userId, string organizationId

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetDoctorByUserIdAndOrganizationAsync-01 | HappyPath | Valid userId, organizationId provided | Returns success payload matching declared return type |
| GetDoctorByUserIdAndOrganizationAsync-02 | InvalidInput | userId = Guid.Empty OR organizationId = null/empty | Returns validation error (400 or 422) or equivalent domain error |
| GetDoctorByUserIdAndOrganizationAsync-03 | NotFoundOrNoData | userId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| GetDoctorByUserIdAndOrganizationAsync-04 | NullableReturn | Valid userId but no associated resource | Returns null without throwing |
| GetDoctorByUserIdAndOrganizationAsync-USERID-EmptyGuid | InvalidInput | userId = Guid.Empty | Returns validation error (400 or 422) |
| GetDoctorByUserIdAndOrganizationAsync-ORGANIZATIONID-EmptyString | InvalidInput | organizationId = null or empty string | Returns validation error (400 or 422) |

