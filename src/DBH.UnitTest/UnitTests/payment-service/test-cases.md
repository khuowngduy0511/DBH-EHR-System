# Payment Service Test Cases

This document defines scenario coverage for each function declared in the service interfaces.

## IInvoiceService

### CreateInvoiceAsync

- Signature: Task<ApiResponse<InvoiceResponse>> CreateInvoiceAsync(CreateInvoiceRequest request);
- Return Type: Task<ApiResponse<InvoiceResponse>>
- Parameters: CreateInvoiceRequest request

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| CreateInvoiceAsync-01 | HappyPath | Valid request provided | Returns success payload matching declared return type |
| CreateInvoiceAsync-02 | InvalidInput | request with missing required fields | Returns validation error (400 or 422) or equivalent domain error |
| CreateInvoiceAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on request | Returns unauthorized or forbidden response, or operation rejected by policy |

### GetInvoiceByIdAsync

- Signature: Task<ApiResponse<InvoiceResponse>> GetInvoiceByIdAsync(Guid invoiceId);
- Return Type: Task<ApiResponse<InvoiceResponse>>
- Parameters: Guid invoiceId

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetInvoiceByIdAsync-01 | HappyPath | Valid invoiceId provided | Returns success payload matching declared return type |
| GetInvoiceByIdAsync-02 | InvalidInput | invoiceId = Guid.Empty | Returns validation error (400 or 422) or equivalent domain error |
| GetInvoiceByIdAsync-03 | NotFoundOrNoData | invoiceId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| GetInvoiceByIdAsync-INVOICEID-EmptyGuid | InvalidInput | invoiceId = Guid.Empty | Returns validation error (400 or 422) |

### GetInvoicesByPatientAsync

- Signature: Task<PagedResponse<InvoiceResponse>> GetInvoicesByPatientAsync(Guid patientId, int page = 1, int pageSize = 10);
- Return Type: Task<PagedResponse<InvoiceResponse>>
- Parameters: Guid patientId, int page = 1, int pageSize = 10

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetInvoicesByPatientAsync-01 | HappyPath | Valid patientId, 1, 10 provided | Returns success payload matching declared return type |
| GetInvoicesByPatientAsync-02 | InvalidInput | patientId = Guid.Empty OR 1 <= 0 OR 10 <= 0 | Returns validation error (400 or 422) or equivalent domain error |
| GetInvoicesByPatientAsync-03 | NotFoundOrNoData | patientId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| GetInvoicesByPatientAsync-04 | PagingBoundary | Large page number or page size out of bounds | Returns valid paging metadata; out-of-range page returns empty item set |
| GetInvoicesByPatientAsync-PATIENTID-EmptyGuid | InvalidInput | patientId = Guid.Empty | Returns validation error (400 or 422) |
| GetInvoicesByPatientAsync-PAGE-ZeroOrNegative | InvalidInput | page <= 0 | Returns validation error (400 or 422) |
| GetInvoicesByPatientAsync-PAGESIZE-ZeroOrNegative | InvalidInput | pageSize <= 0 | Returns validation error (400 or 422) |

### GetInvoicesByOrgAsync

- Signature: Task<PagedResponse<InvoiceResponse>> GetInvoicesByOrgAsync(Guid orgId, int page = 1, int pageSize = 10);
- Return Type: Task<PagedResponse<InvoiceResponse>>
- Parameters: Guid orgId, int page = 1, int pageSize = 10

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetInvoicesByOrgAsync-01 | HappyPath | Valid orgId, 1, 10 provided | Returns success payload matching declared return type |
| GetInvoicesByOrgAsync-02 | InvalidInput | orgId = Guid.Empty OR 1 <= 0 OR 10 <= 0 | Returns validation error (400 or 422) or equivalent domain error |
| GetInvoicesByOrgAsync-03 | NotFoundOrNoData | orgId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| GetInvoicesByOrgAsync-04 | PagingBoundary | Large page number or page size out of bounds | Returns valid paging metadata; out-of-range page returns empty item set |
| GetInvoicesByOrgAsync-ORGID-EmptyGuid | InvalidInput | orgId = Guid.Empty | Returns validation error (400 or 422) |
| GetInvoicesByOrgAsync-PAGE-ZeroOrNegative | InvalidInput | page <= 0 | Returns validation error (400 or 422) |
| GetInvoicesByOrgAsync-PAGESIZE-ZeroOrNegative | InvalidInput | pageSize <= 0 | Returns validation error (400 or 422) |

### CancelInvoiceAsync

- Signature: Task<ApiResponse<InvoiceResponse>> CancelInvoiceAsync(Guid invoiceId);
- Return Type: Task<ApiResponse<InvoiceResponse>>
- Parameters: Guid invoiceId

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| CancelInvoiceAsync-01 | HappyPath | Valid invoiceId provided | Returns success payload matching declared return type |
| CancelInvoiceAsync-02 | InvalidInput | invoiceId = Guid.Empty | Returns validation error (400 or 422) or equivalent domain error |
| CancelInvoiceAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on invoiceId | Returns unauthorized or forbidden response, or operation rejected by policy |
| CancelInvoiceAsync-INVOICEID-EmptyGuid | InvalidInput | invoiceId = Guid.Empty | Returns validation error (400 or 422) |

## IPaymentProcessingService

### CreatePayOSCheckoutAsync

- Signature: Task<ApiResponse<CheckoutResponse>> CreatePayOSCheckoutAsync(Guid invoiceId, CheckoutRequest? request);
- Return Type: Task<ApiResponse<CheckoutResponse>>
- Parameters: Guid invoiceId, CheckoutRequest? request

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| CreatePayOSCheckoutAsync-01 | HappyPath | Valid invoiceId, request provided | Returns success payload matching declared return type |
| CreatePayOSCheckoutAsync-02 | InvalidInput | invoiceId = Guid.Empty OR request with missing required fields | Returns validation error (400 or 422) or equivalent domain error |
| CreatePayOSCheckoutAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on invoiceId, request | Returns unauthorized or forbidden response, or operation rejected by policy |
| CreatePayOSCheckoutAsync-INVOICEID-EmptyGuid | InvalidInput | invoiceId = Guid.Empty | Returns validation error (400 or 422) |

### PayCashAsync

- Signature: Task<ApiResponse<PaymentResponse>> PayCashAsync(Guid invoiceId, PayCashRequest? request);
- Return Type: Task<ApiResponse<PaymentResponse>>
- Parameters: Guid invoiceId, PayCashRequest? request

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| PayCashAsync-01 | HappyPath | Valid invoiceId, request provided | Returns success payload matching declared return type |
| PayCashAsync-02 | InvalidInput | invoiceId = Guid.Empty OR request with missing required fields | Returns validation error (400 or 422) or equivalent domain error |
| PayCashAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on invoiceId, request | Returns unauthorized or forbidden response, or operation rejected by policy |
| PayCashAsync-INVOICEID-EmptyGuid | InvalidInput | invoiceId = Guid.Empty | Returns validation error (400 or 422) |

### GetPaymentByIdAsync

- Signature: Task<ApiResponse<PaymentResponse>> GetPaymentByIdAsync(Guid paymentId);
- Return Type: Task<ApiResponse<PaymentResponse>>
- Parameters: Guid paymentId

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetPaymentByIdAsync-01 | HappyPath | Valid paymentId provided | Returns success payload matching declared return type |
| GetPaymentByIdAsync-02 | InvalidInput | paymentId = Guid.Empty | Returns validation error (400 or 422) or equivalent domain error |
| GetPaymentByIdAsync-03 | NotFoundOrNoData | paymentId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| GetPaymentByIdAsync-04 | UnauthorizedOrForbidden | User lacks permission for this action on paymentId | Returns unauthorized or forbidden response, or operation rejected by policy |
| GetPaymentByIdAsync-PAYMENTID-EmptyGuid | InvalidInput | paymentId = Guid.Empty | Returns validation error (400 or 422) |

### HandleWebhookAsync

- Signature: Task<ApiResponse<bool>> HandleWebhookAsync(PayOSWebhookRequest webhookRequest);
- Return Type: Task<ApiResponse<bool>>
- Parameters: PayOSWebhookRequest webhookRequest

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| HandleWebhookAsync-01 | HappyPath | Valid webhookRequest provided | Returns success payload matching declared return type |
| HandleWebhookAsync-02 | InvalidInput | webhookRequest with missing required fields | Returns validation error (400 or 422) or equivalent domain error |
| HandleWebhookAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on webhookRequest | Returns unauthorized or forbidden response, or operation rejected by policy |

### VerifyPaymentAsync

- Signature: Task<ApiResponse<PaymentResponse>> VerifyPaymentAsync(Guid paymentId);
- Return Type: Task<ApiResponse<PaymentResponse>>
- Parameters: Guid paymentId

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| VerifyPaymentAsync-01 | HappyPath | Valid paymentId provided | Returns success payload matching declared return type |
| VerifyPaymentAsync-02 | InvalidInput | paymentId = Guid.Empty | Returns validation error (400 or 422) or equivalent domain error |
| VerifyPaymentAsync-03 | NotFoundOrNoData | paymentId does not exist in DB | Returns null, empty, false, or not-found response according to contract |
| VerifyPaymentAsync-04 | UnauthorizedOrForbidden | User lacks permission for this action on paymentId | Returns unauthorized or forbidden response, or operation rejected by policy |
| VerifyPaymentAsync-PAYMENTID-EmptyGuid | InvalidInput | paymentId = Guid.Empty | Returns validation error (400 or 422) |

