# Notification Service Test Cases

This document defines scenario coverage for each function declared in the service interfaces.

## INotificationService

### SendNotificationAsync

- Signature: Task<ApiResponse<NotificationResponse>> SendNotificationAsync(SendNotificationRequest request);
- Return Type: Task<ApiResponse<NotificationResponse>>
- Parameters: SendNotificationRequest request

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| SendNotificationAsync-01 | HappyPath | Valid request provided | Returns success payload matching declared return type |
| SendNotificationAsync-02 | InvalidInput | request with missing required fields | Returns validation error (400 or 422) or equivalent domain error |
| SendNotificationAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on request | Returns unauthorized or forbidden response, or operation rejected by policy |

### BroadcastNotificationAsync

- Signature: Task<ApiResponse<int>> BroadcastNotificationAsync(BroadcastNotificationRequest request);
- Return Type: Task<ApiResponse<int>>
- Parameters: BroadcastNotificationRequest request

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| BroadcastNotificationAsync-01 | HappyPath | Valid request provided | Returns success payload matching declared return type |
| BroadcastNotificationAsync-02 | InvalidInput | request with missing required fields | Returns validation error (400 or 422) or equivalent domain error |

### GetNotificationsByUserAsync

- Signature: Task<PagedResponse<NotificationResponse>> GetNotificationsByUserAsync(string userDid, int page, int pageSize);
- Return Type: Task<PagedResponse<NotificationResponse>>
- Parameters: string userDid, int page, int pageSize

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetNotificationsByUserAsync-01 | HappyPath | Valid userDid, page, pageSize provided | Returns success payload matching declared return type |
| GetNotificationsByUserAsync-02 | InvalidInput | userDid = null/empty OR page <= 0 OR pageSize <= 0 | Returns validation error (400 or 422) or equivalent domain error |
| GetNotificationsByUserAsync-03 | NotFoundOrNoData | Requested resource not found | Returns null, empty, false, or not-found response according to contract |
| GetNotificationsByUserAsync-04 | PagingBoundary | page = 9999, pageSize = 10 (out of range) | Returns valid paging metadata; out-of-range page returns empty item set |
| GetNotificationsByUserAsync-USERDID-EmptyString | InvalidInput | userDid = null or empty string | Returns validation error (400 or 422) |
| GetNotificationsByUserAsync-PAGE-ZeroOrNegative | InvalidInput | page <= 0 | Returns validation error (400 or 422) |
| GetNotificationsByUserAsync-PAGESIZE-ZeroOrNegative | InvalidInput | pageSize <= 0 | Returns validation error (400 or 422) |

### GetUnreadNotificationsAsync

- Signature: Task<PagedResponse<NotificationResponse>> GetUnreadNotificationsAsync(string userDid, int page, int pageSize);
- Return Type: Task<PagedResponse<NotificationResponse>>
- Parameters: string userDid, int page, int pageSize

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetUnreadNotificationsAsync-01 | HappyPath | Valid userDid, page, pageSize provided | Returns success payload matching declared return type |
| GetUnreadNotificationsAsync-02 | InvalidInput | userDid = null/empty OR page <= 0 OR pageSize <= 0 | Returns validation error (400 or 422) or equivalent domain error |
| GetUnreadNotificationsAsync-03 | NotFoundOrNoData | Requested resource not found | Returns null, empty, false, or not-found response according to contract |
| GetUnreadNotificationsAsync-04 | PagingBoundary | page = 9999, pageSize = 10 (out of range) | Returns valid paging metadata; out-of-range page returns empty item set |
| GetUnreadNotificationsAsync-USERDID-EmptyString | InvalidInput | userDid = null or empty string | Returns validation error (400 or 422) |
| GetUnreadNotificationsAsync-PAGE-ZeroOrNegative | InvalidInput | page <= 0 | Returns validation error (400 or 422) |
| GetUnreadNotificationsAsync-PAGESIZE-ZeroOrNegative | InvalidInput | pageSize <= 0 | Returns validation error (400 or 422) |

### MarkAsReadAsync

- Signature: Task<ApiResponse<int>> MarkAsReadAsync(string userDid, MarkReadRequest request);
- Return Type: Task<ApiResponse<int>>
- Parameters: string userDid, MarkReadRequest request

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| MarkAsReadAsync-01 | HappyPath | Valid userDid, request provided | Returns success payload matching declared return type |
| MarkAsReadAsync-02 | InvalidInput | userDid = null/empty OR request with missing required fields | Returns validation error (400 or 422) or equivalent domain error |
| MarkAsReadAsync-USERDID-EmptyString | InvalidInput | userDid = null or empty string | Returns validation error (400 or 422) |

### MarkAllAsReadAsync

- Signature: Task<ApiResponse<int>> MarkAllAsReadAsync(string userDid);
- Return Type: Task<ApiResponse<int>>
- Parameters: string userDid

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| MarkAllAsReadAsync-01 | HappyPath | Valid userDid provided | Returns success payload matching declared return type |
| MarkAllAsReadAsync-02 | InvalidInput | userDid = null/empty | Returns validation error (400 or 422) or equivalent domain error |
| MarkAllAsReadAsync-USERDID-EmptyString | InvalidInput | userDid = null or empty string | Returns validation error (400 or 422) |

### DeleteNotificationAsync

- Signature: Task<ApiResponse<bool>> DeleteNotificationAsync(Guid notificationId);
- Return Type: Task<ApiResponse<bool>>
- Parameters: Guid notificationId

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| DeleteNotificationAsync-01 | HappyPath | Valid notificationId provided | Returns success payload matching declared return type |
| DeleteNotificationAsync-02 | InvalidInput | notificationId = Guid.Empty | Returns validation error (400 or 422) or equivalent domain error |
| DeleteNotificationAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on notificationId | Returns unauthorized or forbidden response, or operation rejected by policy |
| DeleteNotificationAsync-NOTIFICATIONID-EmptyGuid | InvalidInput | notificationId = Guid.Empty | Returns validation error (400 or 422) |

### GetUnreadCountAsync

- Signature: Task<int> GetUnreadCountAsync(string userDid);
- Return Type: Task<int>
- Parameters: string userDid

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetUnreadCountAsync-01 | HappyPath | Valid userDid provided | Returns success payload matching declared return type |
| GetUnreadCountAsync-02 | InvalidInput | userDid = null/empty | Returns validation error (400 or 422) or equivalent domain error |
| GetUnreadCountAsync-03 | NotFoundOrNoData | Requested resource not found | Returns null, empty, false, or not-found response according to contract |
| GetUnreadCountAsync-USERDID-EmptyString | InvalidInput | userDid = null or empty string | Returns validation error (400 or 422) |

### RegisterDeviceAsync

- Signature: Task<ApiResponse<DeviceTokenResponse>> RegisterDeviceAsync(RegisterDeviceRequest request);
- Return Type: Task<ApiResponse<DeviceTokenResponse>>
- Parameters: RegisterDeviceRequest request

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| RegisterDeviceAsync-01 | HappyPath | Valid request provided | Returns success payload matching declared return type |
| RegisterDeviceAsync-02 | InvalidInput | request with missing required fields | Returns validation error (400 or 422) or equivalent domain error |
| RegisterDeviceAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on request | Returns unauthorized or forbidden response, or operation rejected by policy |
| RegisterDeviceAsync-InvalidEmail | InvalidInput | Email = "invalid-email-format" | Returns validation error (400 or 422) |
| RegisterDeviceAsync-WeakPassword | InvalidInput | Password = "weak" (does not meet complexity requirements) | Returns validation error (400 or 422) |
| RegisterDeviceAsync-DuplicateEmail | InvalidInput | Email already exists in system | Returns validation error (400 or 422) |

### GetUserDevicesAsync

- Signature: Task<List<DeviceTokenResponse>> GetUserDevicesAsync(string userDid);
- Return Type: Task<List<DeviceTokenResponse>>
- Parameters: string userDid

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetUserDevicesAsync-01 | HappyPath | Valid userDid provided | Returns success payload matching declared return type |
| GetUserDevicesAsync-02 | InvalidInput | userDid = null/empty | Returns validation error (400 or 422) or equivalent domain error |
| GetUserDevicesAsync-03 | NotFoundOrNoData | Requested resource not found | Returns null, empty, false, or not-found response according to contract |
| GetUserDevicesAsync-04 | EmptyCollection | Specific input for EmptyCollection with userDid | Returns empty collection, not null |
| GetUserDevicesAsync-USERDID-EmptyString | InvalidInput | userDid = null or empty string | Returns validation error (400 or 422) |

### DeactivateDeviceAsync

- Signature: Task<ApiResponse<bool>> DeactivateDeviceAsync(Guid deviceTokenId);
- Return Type: Task<ApiResponse<bool>>
- Parameters: Guid deviceTokenId

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| DeactivateDeviceAsync-01 | HappyPath | Valid deviceTokenId provided | Returns success payload matching declared return type |
| DeactivateDeviceAsync-02 | InvalidInput | deviceTokenId = Guid.Empty | Returns validation error (400 or 422) or equivalent domain error |
| DeactivateDeviceAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on deviceTokenId | Returns unauthorized or forbidden response, or operation rejected by policy |
| DeactivateDeviceAsync-DEVICETOKENID-EmptyGuid | InvalidInput | deviceTokenId = Guid.Empty | Returns validation error (400 or 422) |

### DeactivateAllDevicesAsync

- Signature: Task<ApiResponse<bool>> DeactivateAllDevicesAsync(string userDid);
- Return Type: Task<ApiResponse<bool>>
- Parameters: string userDid

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| DeactivateAllDevicesAsync-01 | HappyPath | Valid userDid provided | Returns success payload matching declared return type |
| DeactivateAllDevicesAsync-02 | InvalidInput | userDid = null/empty | Returns validation error (400 or 422) or equivalent domain error |
| DeactivateAllDevicesAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on userDid | Returns unauthorized or forbidden response, or operation rejected by policy |
| DeactivateAllDevicesAsync-USERDID-EmptyString | InvalidInput | userDid = null or empty string | Returns validation error (400 or 422) |

### GetPreferencesAsync

- Signature: Task<PreferencesResponse> GetPreferencesAsync(string userDid);
- Return Type: Task<PreferencesResponse>
- Parameters: string userDid

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetPreferencesAsync-01 | HappyPath | Valid userDid provided | Returns success payload matching declared return type |
| GetPreferencesAsync-02 | InvalidInput | userDid = null/empty | Returns validation error (400 or 422) or equivalent domain error |
| GetPreferencesAsync-03 | NotFoundOrNoData | Requested resource not found | Returns null, empty, false, or not-found response according to contract |
| GetPreferencesAsync-USERDID-EmptyString | InvalidInput | userDid = null or empty string | Returns validation error (400 or 422) |

### UpdatePreferencesAsync

- Signature: Task<ApiResponse<PreferencesResponse>> UpdatePreferencesAsync(string userDid, UpdatePreferencesRequest request);
- Return Type: Task<ApiResponse<PreferencesResponse>>
- Parameters: string userDid, UpdatePreferencesRequest request

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| UpdatePreferencesAsync-01 | HappyPath | Valid userDid, request provided | Returns success payload matching declared return type |
| UpdatePreferencesAsync-02 | InvalidInput | userDid = null/empty OR request with missing required fields | Returns validation error (400 or 422) or equivalent domain error |
| UpdatePreferencesAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on userDid, request | Returns unauthorized or forbidden response, or operation rejected by policy |
| UpdatePreferencesAsync-USERDID-EmptyString | InvalidInput | userDid = null or empty string | Returns validation error (400 or 422) |

## IDeviceTokenService

### RegisterDeviceAsync

- Signature: Task<ApiResponse<DeviceTokenResponse>> RegisterDeviceAsync(RegisterDeviceRequest request);
- Return Type: Task<ApiResponse<DeviceTokenResponse>>
- Parameters: RegisterDeviceRequest request

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| RegisterDeviceAsync-01 | HappyPath | Valid request provided | Returns success payload matching declared return type |
| RegisterDeviceAsync-02 | InvalidInput | request with missing required fields | Returns validation error (400 or 422) or equivalent domain error |
| RegisterDeviceAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on request | Returns unauthorized or forbidden response, or operation rejected by policy |
| RegisterDeviceAsync-InvalidEmail | InvalidInput | Email = "invalid-email-format" | Returns validation error (400 or 422) |
| RegisterDeviceAsync-WeakPassword | InvalidInput | Password = "weak" (does not meet complexity requirements) | Returns validation error (400 or 422) |
| RegisterDeviceAsync-DuplicateEmail | InvalidInput | Email already exists in system | Returns validation error (400 or 422) |

### GetUserDevicesAsync

- Signature: Task<List<DeviceTokenResponse>> GetUserDevicesAsync(string userDid);
- Return Type: Task<List<DeviceTokenResponse>>
- Parameters: string userDid

<!-- 
| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetUserDevicesAsync-01 | HappyPath | Valid userDid provided | Returns success payload matching declared return type |
| GetUserDevicesAsync-02 | InvalidInput | userDid = null/empty | Returns validation error (400 or 422) or equivalent domain error |
| GetUserDevicesAsync-03 | NotFoundOrNoData | Requested resource not found | Returns null, empty, false, or not-found response according to contract |
| GetUserDevicesAsync-04 | EmptyCollection | Specific input for EmptyCollection with userDid | Returns empty collection, not null |
| DeactivateDeviceAsync-DEVICETOKENID-EmptyGuid | InvalidInput | deviceTokenId = Guid.Empty | Returns validation error (400 or 422) |
### DeactivateDeviceAsync

- Signature: Task<ApiResponse<bool>> DeactivateDeviceAsync(Guid deviceTokenId);
- Return Type: Task<ApiResponse<bool>>
- Parameters: Guid deviceTokenId

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| DeactivateDeviceAsync-01 | HappyPath | Valid deviceTokenId provided | Returns success payload matching declared return type |
| DeactivateDeviceAsync-02 | InvalidInput | deviceTokenId = Guid.Empty | Returns validation error (400 or 422) or equivalent domain error |
| DeactivateDeviceAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on deviceTokenId | Returns unauthorized or forbidden response, or operation rejected by policy |

### DeactivateAllDevicesAsync

- Signature: Task<ApiResponse<bool>> DeactivateAllDevicesAsync(string userDid);
- Return Type: Task<ApiResponse<bool>>
- Parameters: string userDid

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| DeactivateAllDevicesAsync-01 | HappyPath | Valid userDid provided | Returns success payload matching declared return type |
| DeactivateAllDevicesAsync-02 | InvalidInput | userDid = null/empty | Returns validation error (400 or 422) or equivalent domain error |
| DeactivateAllDevicesAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on userDid | Returns unauthorized or forbidden response, or operation rejected by policy |
| DeactivateAllDevicesAsync-USERDID-EmptyString | InvalidInput | userDid = null or empty string | Returns validation error (400 or 422) |

## IPreferencesService

### GetPreferencesAsync

- Signature: Task<PreferencesResponse> GetPreferencesAsync(string userDid);
- Return Type: Task<PreferencesResponse>
- Parameters: string userDid

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| GetPreferencesAsync-01 | HappyPath | Valid userDid provided | Returns success payload matching declared return type |
| GetPreferencesAsync-02 | InvalidInput | userDid = null/empty | Returns validation error (400 or 422) or equivalent domain error |
| GetPreferencesAsync-03 | NotFoundOrNoData | Requested resource not found | Returns null, empty, false, or not-found response according to contract |
| GetPreferencesAsync-USERDID-EmptyString | InvalidInput | userDid = null or empty string | Returns validation error (400 or 422) |

### UpdatePreferencesAsync

- Signature: Task<ApiResponse<PreferencesResponse>> UpdatePreferencesAsync(string userDid, UpdatePreferencesRequest request);
- Return Type: Task<ApiResponse<PreferencesResponse>>
- Parameters: string userDid, UpdatePreferencesRequest request

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| UpdatePreferencesAsync-01 | HappyPath | Valid userDid, request provided | Returns success payload matching declared return type |
| UpdatePreferencesAsync-02 | InvalidInput | userDid = null/empty OR request with missing required fields | Returns validation error (400 or 422) or equivalent domain error |
| UpdatePreferencesAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on userDid, request | Returns unauthorized or forbidden response, or operation rejected by policy |
| UpdatePreferencesAsync-USERDID-EmptyString | InvalidInput | userDid = null or empty string | Returns validation error (400 or 422) |

## IPushNotificationService

### SendPushAsync

- Signature: Task<bool> SendPushAsync(string fcmToken, string title, string body, Dictionary<string, string>? data = null);
- Return Type: Task<bool>
- Parameters: string fcmToken, string title, string body, Dictionary<string, string>? data = null

| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| SendPushAsync-01 | HappyPath | Valid fcmToken, title, body, Dictionary<string, null provided | Returns success payload matching declared return type |
| SendPushAsync-02 | InvalidInput | fcmToken = null/empty OR title = null/empty OR body = null/empty OR Invalid Dictionary<string OR null = null/empty | Returns validation error (400 or 422) or equivalent domain error |
| SendPushAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on fcmToken, title, body, Dictionary<string, null | Returns unauthorized or forbidden response, or operation rejected by policy |
| SendPushAsync-04 | BooleanFalsePath | Specific input for BooleanFalsePath with fcmToken, title, body, Dictionary<string, null | Returns false |
| SendPushAsync-05 | BooleanTruePath | Specific input for BooleanTruePath with fcmToken, title, body, Dictionary<string, null | Returns true |
| SendPushAsync-FCMTOKEN-EmptyString | InvalidInput | fcmToken = null or empty string | Returns validation error (400 or 422) |
| SendPushAsync-TITLE-EmptyString | InvalidInput | title = null or empty string | Returns validation error (400 or 422) |
| SendPushAsync-BODY-EmptyString | InvalidInput | body = null or empty string | Returns validation error (400 or 422) |

### SendMulticastAsync

- Signature: Task<int> SendMulticastAsync(List<string> fcmTokens, string title, string body, Dictionary<string, string>? data = null);
- Return Type: Task<int>
- Parameters: List<string> fcmTokens, string title, string body, Dictionary<string, string>? data = null

 -->
| TestCaseID | Condition | Input | Expected Return |
|---|---|---|---|
| SendMulticastAsync-01 | HappyPath | Valid fcmTokens, title, body, Dictionary<string, null provided | Returns success payload matching declared return type |
| SendMulticastAsync-02 | InvalidInput | fcmTokens = null/empty OR title = null/empty OR body = null/empty OR Invalid Dictionary<string OR null = null/empty | Returns validation error (400 or 422) or equivalent domain error |
| SendMulticastAsync-03 | UnauthorizedOrForbidden | User lacks permission for this action on fcmTokens, title, body, Dictionary<string, null | Returns unauthorized or forbidden response, or operation rejected by policy |
| SendMulticastAsync-TITLE-EmptyString | InvalidInput | title = null or empty string | Returns validation error (400 or 422) |
| SendMulticastAsync-BODY-EmptyString | InvalidInput | body = null or empty string | Returns validation error (400 or 422) |
