# Appointment Service Test Cases

This document defines scenario coverage for each function declared in the service interfaces.

## IAppointmentService

### CreateAppointmentAsync

- Signature: Task<ApiResponse<AppointmentResponse>> CreateAppointmentAsync(CreateAppointmentRequest request);
- Return Type: Task<ApiResponse<AppointmentResponse>>
- Parameters: CreateAppointmentRequest request

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| CreateAppointmentAsync-01 | HappyPath | Valid request provided | Returns success payload matching declared return type |
| CreateAppointmentAsync-02 | InvalidInput | request with missing required fields | Returns validation error (400 or 422) or equivalent domain error |
| CreateAppointmentAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on request | Returns unauthorized or forbidden response, or operation rejected by policy |
| CreateAppointmentAsync-PastDate | InvalidInput | ScheduledAt = DateTime.UtcNow.AddDays(-1) (past date) | Returns validation error (400 or 422) |
| CreateAppointmentAsync-MissingPatientId | InvalidInput | PatientId = Guid.Empty | Returns validation error (400 or 422) |
| CreateAppointmentAsync-MissingDoctorId | InvalidInput | DoctorId = Guid.Empty | Returns validation error (400 or 422) |
| CreateAppointmentAsync-MissingOrgId | InvalidInput | OrgId = Guid.Empty | Returns validation error (400 or 422) |

### GetAppointmentByIdAsync

- Signature: Task<ApiResponse<AppointmentResponse>> GetAppointmentByIdAsync(Guid appointmentId);
- Return Type: Task<ApiResponse<AppointmentResponse>>
- Parameters: Guid appointmentId

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetAppointmentByIdAsync-01 | HappyPath | Valid appointmentId provided | Returns success payload matching declared return type |
| GetAppointmentByIdAsync-02 | InvalidInput | appointmentId = Guid.Empty | Returns validation error (400 or 422) or equivalent domain error |
| GetAppointmentByIdAsync-03 | NotFoundOrNoData | appointmentId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| GetAppointmentByIdAsync-APPOINTMENTID-EmptyGuid | InvalidInput | appointmentId = Guid.Empty | Returns validation error (400 or 422) |

### GetAppointmentsAsync

- Signature: Task<PagedResponse<AppointmentResponse>> GetAppointmentsAsync(Guid? patientId, Guid? doctorId, Guid? orgId, AppointmentStatus? status, int page = 1, int pageSize = 10);
- Return Type: Task<PagedResponse<AppointmentResponse>>
- Parameters: Guid? patientId, Guid? doctorId, Guid? orgId, AppointmentStatus? status, int page = 1, int pageSize = 10

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetAppointmentsAsync-01 | HappyPath | Valid patientId, doctorId, orgId, status, 1, 10 provided | Returns success payload matching declared return type |
| GetAppointmentsAsync-02 | InvalidInput | patientId = Guid.Empty OR doctorId = Guid.Empty OR orgId = Guid.Empty OR status <= 0 OR 1 <= 0 OR 10 <= 0 | Returns validation error (400 or 422) or equivalent domain error |
| GetAppointmentsAsync-03 | NotFoundOrNoData | patientId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| GetAppointmentsAsync-04 | PagingBoundary | Large page number or page size out of bounds | Returns valid paging metadata; out-of-range page returns empty item set |
| GetAppointmentsAsync-PATIENTID-EmptyGuid | InvalidInput | patientId = Guid.Empty | Returns validation error (400 or 422) |
| GetAppointmentsAsync-DOCTORID-EmptyGuid | InvalidInput | doctorId = Guid.Empty | Returns validation error (400 or 422) |
| GetAppointmentsAsync-ORGID-EmptyGuid | InvalidInput | orgId = Guid.Empty | Returns validation error (400 or 422) |
| GetAppointmentsAsync-PAGE-ZeroOrNegative | InvalidInput | page <= 0 | Returns validation error (400 or 422) |
| GetAppointmentsAsync-PAGESIZE-ZeroOrNegative | InvalidInput | pageSize <= 0 | Returns validation error (400 or 422) |

### UpdateAppointmentStatusAsync

- Signature: Task<ApiResponse<AppointmentResponse>> UpdateAppointmentStatusAsync(Guid appointmentId, AppointmentStatus status);
- Return Type: Task<ApiResponse<AppointmentResponse>>
- Parameters: Guid appointmentId, AppointmentStatus status

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| UpdateAppointmentStatusAsync-01 | HappyPath | Valid appointmentId, status provided | Returns success payload matching declared return type |
| UpdateAppointmentStatusAsync-02 | InvalidInput | appointmentId = Guid.Empty OR status <= 0 | Returns validation error (400 or 422) or equivalent domain error |
| UpdateAppointmentStatusAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on appointmentId, status | Returns unauthorized or forbidden response, or operation rejected by policy |
| UpdateAppointmentStatusAsync-APPOINTMENTID-EmptyGuid | InvalidInput | appointmentId = Guid.Empty | Returns validation error (400 or 422) |

### RescheduleAppointmentAsync

- Signature: Task<ApiResponse<AppointmentResponse>> RescheduleAppointmentAsync(Guid appointmentId, DateTime newDate);
- Return Type: Task<ApiResponse<AppointmentResponse>>
- Parameters: Guid appointmentId, DateTime newDate

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| RescheduleAppointmentAsync-01 | HappyPath | Valid appointmentId, newDate provided | Returns success payload matching declared return type |
| RescheduleAppointmentAsync-02 | InvalidInput | appointmentId = Guid.Empty OR Invalid newDate | Returns validation error (400 or 422) or equivalent domain error |
| RescheduleAppointmentAsync-APPOINTMENTID-EmptyGuid | InvalidInput | appointmentId = Guid.Empty | Returns validation error (400 or 422) |

### ConfirmAppointmentAsync

- Signature: Task<ApiResponse<AppointmentResponse>> ConfirmAppointmentAsync(Guid appointmentId);
- Return Type: Task<ApiResponse<AppointmentResponse>>
- Parameters: Guid appointmentId

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| ConfirmAppointmentAsync-01 | HappyPath | Valid appointmentId provided | Returns success payload matching declared return type |
| ConfirmAppointmentAsync-02 | InvalidInput | appointmentId = Guid.Empty | Returns validation error (400 or 422) or equivalent domain error |
| ConfirmAppointmentAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on appointmentId | Returns unauthorized or forbidden response, or operation rejected by policy |
| ConfirmAppointmentAsync-APPOINTMENTID-EmptyGuid | InvalidInput | appointmentId = Guid.Empty | Returns validation error (400 or 422) |

### RejectAppointmentAsync

- Signature: Task<ApiResponse<AppointmentResponse>> RejectAppointmentAsync(Guid appointmentId, string reason);
- Return Type: Task<ApiResponse<AppointmentResponse>>
- Parameters: Guid appointmentId, string reason

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| RejectAppointmentAsync-01 | HappyPath | Valid appointmentId, reason provided | Returns success payload matching declared return type |
| RejectAppointmentAsync-02 | InvalidInput | appointmentId = Guid.Empty OR reason = null/empty | Returns validation error (400 or 422) or equivalent domain error |
| RejectAppointmentAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on appointmentId, reason | Returns unauthorized or forbidden response, or operation rejected by policy |
| RejectAppointmentAsync-APPOINTMENTID-EmptyGuid | InvalidInput | appointmentId = Guid.Empty | Returns validation error (400 or 422) |
| RejectAppointmentAsync-REASON-EmptyString | InvalidInput | reason = null or empty string | Returns validation error (400 or 422) |

### CancelAppointmentAsync

- Signature: Task<ApiResponse<AppointmentResponse>> CancelAppointmentAsync(Guid appointmentId, string reason);
- Return Type: Task<ApiResponse<AppointmentResponse>>
- Parameters: Guid appointmentId, string reason

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| CancelAppointmentAsync-01 | HappyPath | Valid appointmentId, reason provided | Returns success payload matching declared return type |
| CancelAppointmentAsync-02 | InvalidInput | appointmentId = Guid.Empty OR reason = null/empty | Returns validation error (400 or 422) or equivalent domain error |
| CancelAppointmentAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on appointmentId, reason | Returns unauthorized or forbidden response, or operation rejected by policy |
| CancelAppointmentAsync-APPOINTMENTID-EmptyGuid | InvalidInput | appointmentId = Guid.Empty | Returns validation error (400 or 422) |
| CancelAppointmentAsync-REASON-EmptyString | InvalidInput | reason = null or empty string | Returns validation error (400 or 422) |

### CheckInAsync

- Signature: Task<ApiResponse<AppointmentResponse>> CheckInAsync(Guid appointmentId);
- Return Type: Task<ApiResponse<AppointmentResponse>>
- Parameters: Guid appointmentId

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| CheckInAsync-01 | HappyPath | Valid appointmentId provided | Returns success payload matching declared return type |
| CheckInAsync-02 | InvalidInput | appointmentId = Guid.Empty | Returns validation error (400 or 422) or equivalent domain error |
| CheckInAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on appointmentId | Returns unauthorized or forbidden response, or operation rejected by policy |
| CheckInAsync-APPOINTMENTID-EmptyGuid | InvalidInput | appointmentId = Guid.Empty | Returns validation error (400 or 422) |

### SearchDoctorsAsync

- Signature: Task<PagedResponse<DoctorSearchResult>> SearchDoctorsAsync(SearchDoctorQuery query);
- Return Type: Task<PagedResponse<DoctorSearchResult>>
- Parameters: SearchDoctorQuery query

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| SearchDoctorsAsync-01 | HappyPath | Valid query provided | Returns success payload matching declared return type |
| SearchDoctorsAsync-02 | InvalidInput | query with missing required fields | Returns validation error (400 or 422) or equivalent domain error |
| SearchDoctorsAsync-03 | NotFoundOrNoData | Requested resource not found | Returns null, empty, false, or not-found response according to contract |
| SearchDoctorsAsync-04 | PagingBoundary | Large page number or page size out of bounds | Returns valid paging metadata; out-of-range page returns empty item set |

### CreateEncounterAsync

- Signature: Task<ApiResponse<EncounterResponse>> CreateEncounterAsync(CreateEncounterRequest request);
- Return Type: Task<ApiResponse<EncounterResponse>>
- Parameters: CreateEncounterRequest request

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| CreateEncounterAsync-01 | HappyPath | Valid request provided | Returns success payload matching declared return type |
| CreateEncounterAsync-02 | InvalidInput | request with missing required fields | Returns validation error (400 or 422) or equivalent domain error |
| CreateEncounterAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on request | Returns unauthorized or forbidden response, or operation rejected by policy |
| CreateEncounterAsync-InvalidAppointmentStatus | InvalidInput | Appointment not in CHECKED_IN status | Returns validation error (400 or 422) |

### GetEncounterByIdAsync

- Signature: Task<ApiResponse<EncounterResponse>> GetEncounterByIdAsync(Guid encounterId);
- Return Type: Task<ApiResponse<EncounterResponse>>
- Parameters: Guid encounterId

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetEncounterByIdAsync-01 | HappyPath | Valid encounterId provided | Returns success payload matching declared return type |
| GetEncounterByIdAsync-02 | InvalidInput | encounterId = Guid.Empty | Returns validation error (400 or 422) or equivalent domain error |
| GetEncounterByIdAsync-03 | NotFoundOrNoData | encounterId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| GetEncounterByIdAsync-ENCOUNTERID-EmptyGuid | InvalidInput | encounterId = Guid.Empty | Returns validation error (400 or 422) |

### GetEncountersByAppointmentIdAsync

- Signature: Task<PagedResponse<EncounterResponse>> GetEncountersByAppointmentIdAsync(Guid appointmentId, int page = 1, int pageSize = 10);
- Return Type: Task<PagedResponse<EncounterResponse>>
- Parameters: Guid appointmentId, int page = 1, int pageSize = 10

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetEncountersByAppointmentIdAsync-01 | HappyPath | Valid appointmentId, 1, 10 provided | Returns success payload matching declared return type |
| GetEncountersByAppointmentIdAsync-02 | InvalidInput | appointmentId = Guid.Empty OR 1 <= 0 OR 10 <= 0 | Returns validation error (400 or 422) or equivalent domain error |
| GetEncountersByAppointmentIdAsync-03 | NotFoundOrNoData | appointmentId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| GetEncountersByAppointmentIdAsync-04 | PagingBoundary | Large page number or page size out of bounds | Returns valid paging metadata; out-of-range page returns empty item set |
| GetEncountersByAppointmentIdAsync-APPOINTMENTID-EmptyGuid | InvalidInput | appointmentId = Guid.Empty | Returns validation error (400 or 422) |
| GetEncountersByAppointmentIdAsync-PAGE-ZeroOrNegative | InvalidInput | page <= 0 | Returns validation error (400 or 422) |
| GetEncountersByAppointmentIdAsync-PAGESIZE-ZeroOrNegative | InvalidInput | pageSize <= 0 | Returns validation error (400 or 422) |

### GetEncountersByPatientIdAsync

- Signature: Task<PagedResponse<EncounterResponse>> GetEncountersByPatientIdAsync(Guid patientId, int page = 1, int pageSize = 10);
- Return Type: Task<PagedResponse<EncounterResponse>>
- Parameters: Guid patientId, int page = 1, int pageSize = 10

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetEncountersByPatientIdAsync-01 | HappyPath | Valid patientId, 1, 10 provided | Returns success payload matching declared return type |
| GetEncountersByPatientIdAsync-02 | InvalidInput | patientId = Guid.Empty OR 1 <= 0 OR 10 <= 0 | Returns validation error (400 or 422) or equivalent domain error |
| GetEncountersByPatientIdAsync-03 | NotFoundOrNoData | patientId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| GetEncountersByPatientIdAsync-04 | PagingBoundary | Large page number or page size out of bounds | Returns valid paging metadata; out-of-range page returns empty item set |
| GetEncountersByPatientIdAsync-PATIENTID-EmptyGuid | InvalidInput | patientId = Guid.Empty | Returns validation error (400 or 422) |
| GetEncountersByPatientIdAsync-PAGE-ZeroOrNegative | InvalidInput | page <= 0 | Returns validation error (400 or 422) |
| GetEncountersByPatientIdAsync-PAGESIZE-ZeroOrNegative | InvalidInput | pageSize <= 0 | Returns validation error (400 or 422) |

### UpdateEncounterAsync

- Signature: Task<ApiResponse<EncounterResponse>> UpdateEncounterAsync(Guid encounterId, UpdateEncounterRequest request);
- Return Type: Task<ApiResponse<EncounterResponse>>
- Parameters: Guid encounterId, UpdateEncounterRequest request

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| UpdateEncounterAsync-01 | HappyPath | Valid encounterId, request provided | Returns success payload matching declared return type |
| UpdateEncounterAsync-02 | InvalidInput | encounterId = Guid.Empty OR request with missing required fields | Returns validation error (400 or 422) or equivalent domain error |
| UpdateEncounterAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on encounterId, request | Returns unauthorized or forbidden response, or operation rejected by policy |
| UpdateEncounterAsync-ENCOUNTERID-EmptyGuid | InvalidInput | encounterId = Guid.Empty | Returns validation error (400 or 422) |

### CompleteEncounterAsync

- Signature: Task<ApiResponse<EncounterResponse>> CompleteEncounterAsync(Guid encounterId, CompleteEncounterRequest request);
- Return Type: Task<ApiResponse<EncounterResponse>>
- Parameters: Guid encounterId, CompleteEncounterRequest request

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| CompleteEncounterAsync-01 | HappyPath | Valid encounterId, request provided | Returns success payload matching declared return type |
| CompleteEncounterAsync-02 | InvalidInput | encounterId = Guid.Empty OR request with missing required fields | Returns validation error (400 or 422) or equivalent domain error |
| CompleteEncounterAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on encounterId, request | Returns unauthorized or forbidden response, or operation rejected by policy |
| CompleteEncounterAsync-ENCOUNTERID-EmptyGuid | InvalidInput | encounterId = Guid.Empty | Returns validation error (400 or 422) |

### GetPatientsByDoctorAsync

- Signature: Task<PagedResponse<DoctorPatientResponse>> GetPatientsByDoctorAsync(Guid doctorId, int page = 1, int pageSize = 10);
- Return Type: Task<PagedResponse<DoctorPatientResponse>>
- Parameters: Guid doctorId, int page = 1, int pageSize = 10

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetPatientsByDoctorAsync-01 | HappyPath | Valid doctorId, 1, 10 provided | Returns success payload matching declared return type |
| GetPatientsByDoctorAsync-02 | InvalidInput | doctorId = Guid.Empty OR 1 <= 0 OR 10 <= 0 | Returns validation error (400 or 422) or equivalent domain error |
| GetPatientsByDoctorAsync-03 | NotFoundOrNoData | doctorId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| GetPatientsByDoctorAsync-04 | PagingBoundary | Large page number or page size out of bounds | Returns valid paging metadata; out-of-range page returns empty item set |
| GetPatientsByDoctorAsync-DOCTORID-EmptyGuid | InvalidInput | doctorId = Guid.Empty | Returns validation error (400 or 422) |
| GetPatientsByDoctorAsync-PAGE-ZeroOrNegative | InvalidInput | page <= 0 | Returns validation error (400 or 422) |
| GetPatientsByDoctorAsync-PAGESIZE-ZeroOrNegative | InvalidInput | pageSize <= 0 | Returns validation error (400 or 422) |

## IAuthServiceClient

### GetUserIdByPatientIdAsync

- Signature: Task<Guid?> GetUserIdByPatientIdAsync(Guid patientId);
- Return Type: Task<Guid?>
- Parameters: Guid patientId

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetUserIdByPatientIdAsync-01 | HappyPath | Valid patientId provided | Returns success payload matching declared return type |
| GetUserIdByPatientIdAsync-02 | InvalidInput | patientId = Guid.Empty | Returns validation error (400 or 422) or equivalent domain error |
| GetUserIdByPatientIdAsync-03 | NotFoundOrNoData | patientId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| GetUserIdByPatientIdAsync-04 | NullableReturn | Valid patientId but no associated resource | Returns null without throwing |
| GetUserIdByPatientIdAsync-PATIENTID-EmptyGuid | InvalidInput | patientId = Guid.Empty | Returns validation error (400 or 422) |

### GetUserIdByDoctorIdAsync

- Signature: Task<Guid?> GetUserIdByDoctorIdAsync(Guid doctorId);
- Return Type: Task<Guid?>
- Parameters: Guid doctorId

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetUserIdByDoctorIdAsync-01 | HappyPath | Valid doctorId provided | Returns success payload matching declared return type |
| GetUserIdByDoctorIdAsync-02 | InvalidInput | doctorId = Guid.Empty | Returns validation error (400 or 422) or equivalent domain error |
| GetUserIdByDoctorIdAsync-03 | NotFoundOrNoData | doctorId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| GetUserIdByDoctorIdAsync-04 | NullableReturn | Valid doctorId but no associated resource | Returns null without throwing |
| GetUserIdByDoctorIdAsync-DOCTORID-EmptyGuid | InvalidInput | doctorId = Guid.Empty | Returns validation error (400 or 422) |

### GetUserProfileDetailAsync

- Signature: Task<AuthUserProfileDetailDto?> GetUserProfileDetailAsync(Guid userId);
- Return Type: Task<AuthUserProfileDetailDto?>
- Parameters: Guid userId

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetUserProfileDetailAsync-01 | HappyPath | Valid userId provided | Returns success payload matching declared return type |
| GetUserProfileDetailAsync-02 | InvalidInput | userId = Guid.Empty | Returns validation error (400 or 422) or equivalent domain error |
| GetUserProfileDetailAsync-03 | NotFoundOrNoData | userId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| GetUserProfileDetailAsync-04 | NullableReturn | Valid userId but no associated resource | Returns null without throwing |
| GetUserProfileDetailAsync-USERID-EmptyGuid | InvalidInput | userId = Guid.Empty | Returns validation error (400 or 422) |

