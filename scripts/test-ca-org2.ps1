# Quick test to verify CA enrollment uses correct organization
$token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJhYWFhYWFhMi1hYWFhLWFhYWEtYWFhYS1hYWFhYWFhYWFhYWEiLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1laWRlbnRpZmllciI6ImFhYWFhYWEyLWFhYWEtYWFhYS1hYWFhLWFhYWFhYWFhYWFhYSIsImVtYWlsIjoiYWRtaW4yQGRiaC5jb20iLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1lIjoiQWRtaW4gVXNlciAyIiwianRpIjoiYWM0ZjI5MTYtMjQ2My00MTg2LWIxZTktODFhZjMzNjY0NDJjIiwiaHR0cDovL3NjaGVtYXMubWljcm9zb2Z0LmNvbS93cy8yMDA4LzA2L2lkZW50aXR5L2NsYWltcy9ncm91cHNpZCI6IjExMTExMTExLTExMTEtMTExMS0xMTExLTExMTExMTExMTEwMiIsInRva2VuX2lzc3VlZF9hdF9tcyI6MTc4MDM4NzUxNTEyOSwiaHR0cDovL3NjaGVtYXMubWljcm9zb2Z0LmNvbS93cy8yMDA4LzA2L2lkZW50aXR5L2NsYWltcy9yb2xlIjoiQWRtaW4iLCJleHAiOjE3ODAzOTExMTUsImlzcyI6IkRCSC5BdXRoLlNlcnZpY2UiLCJhdWQiOiJEQkguRUhSLlN5c3RlbSJ9.JGuUhTy8SDkYVkAMFHaqBmF1oe-xNB7sWK5KWsceO40"

$headers = @{
    "Content-Type" = "application/json"
    "Authorization" = "Bearer $token"
}

$body = @{
    enrollmentId = "test-org2-ca"
    username = "TestOrg2User"
    role = "Admin"
    enrollmentSecret = "testpw"
} | ConvertTo-Json

Write-Host "Sending request to blockchain service..."
try {
    $response = Invoke-RestMethod -Method POST -Uri "http://127.0.0.1:5090/api/v1/blockchain/accounts/login" -Headers $headers -Body $body
    $response | ConvertTo-Json | Write-Host
} catch {
    Write-Host "Error: $_"
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $reader.ReadToEnd() | Write-Host
        $reader.Close()
    }
}

Write-Host "Done. Check docker logs for [DEBUG-CA] entries."