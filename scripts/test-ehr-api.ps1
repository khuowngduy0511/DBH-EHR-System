#!/usr/bin/env pwsh
# =============================================================================
# EHR Service API Test Script
# =============================================================================
$ErrorActionPreference = "Continue"
$baseUrl = "http://localhost:5002"

# ---- Generate JWT Token ----
$secret = "your-super-secret-key-for-jwt-token-generation-min-32-chars"
$issuer = "DBH.Auth.Service"
$audience = "DBH.EHR.System"

$patientId = [guid]::NewGuid()
$doctorId  = [guid]::NewGuid()
$orgId     = [guid]::NewGuid()
$encounterId = [guid]::NewGuid()

function New-Jwt($claims, $secret, $issuer, $audience) {
    $header = '{"alg":"HS256","typ":"JWT"}'
    $headerB64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($header)).TrimEnd('=').Replace('+','-').Replace('/','_')
    
    $now = [int][double]::Parse((Get-Date -UFormat %s))
    $claims["iss"] = $issuer
    $claims["aud"] = $audience
    $claims["iat"] = $now
    $claims["exp"] = $now + 86400
    $payload = $claims | ConvertTo-Json -Compress
    $payloadB64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($payload)).TrimEnd('=').Replace('+','-').Replace('/','_')

    $hmac = New-Object System.Security.Cryptography.HMACSHA256
    $hmac.Key = [Text.Encoding]::UTF8.GetBytes($secret)
    $sigBytes = $hmac.ComputeHash([Text.Encoding]::UTF8.GetBytes("$headerB64.$payloadB64"))
    $sigB64 = [Convert]::ToBase64String($sigBytes).TrimEnd('=').Replace('+','-').Replace('/','_')
    return "$headerB64.$payloadB64.$sigB64"
}

$token = New-Jwt -claims @{
    sub = $doctorId.ToString()
    "http://schemas.microsoft.com/ws/2008/06/identity/claims/role" = "Doctor"
} -secret $secret -issuer $issuer -audience $audience

$headers = @{ Authorization = "Bearer $token"; "Content-Type" = "application/json" }

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  EHR Service API Test" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "PatientId   : $patientId"
Write-Host "DoctorId    : $doctorId"
Write-Host "OrgId       : $orgId"
Write-Host "EncounterId : $encounterId"
Write-Host ""

$passed = 0
$failed = 0
$total  = 0

function Test-Api($name, $method, $url, $body = $null, $extraHeaders = @{}, $expectedStatus = 200, $formFile = $null) {
    $script:total++
    Write-Host "[$script:total] $name " -NoNewline
    try {
        $params = @{
            Method = $method
            Uri = $url
            Headers = $headers + $extraHeaders
            ErrorAction = "Stop"
        }
        if ($body) {
            $params["Body"] = $body
            $params["ContentType"] = "application/json"
        }
        if ($formFile) {
            $params.Remove("ContentType") | Out-Null
            $params["Form"] = $formFile
            $params["Headers"] = @{ Authorization = "Bearer $token" } + $extraHeaders
        }
        
        $response = Invoke-WebRequest @params
        $status = $response.StatusCode
        
        if ($status -eq $expectedStatus -or ($expectedStatus -eq 201 -and $status -eq 201) -or ($expectedStatus -eq 200 -and $status -eq 200) -or ($expectedStatus -eq 204 -and $status -eq 204)) {
            Write-Host "[PASS] Status=$status" -ForegroundColor Green
            $script:passed++
            return ($response.Content | ConvertFrom-Json -ErrorAction SilentlyContinue)
        } else {
            Write-Host "[FAIL] Expected=$expectedStatus Got=$status" -ForegroundColor Red
            $script:failed++
            return $null
        }
    }
    catch {
        $errorStatus = $null
        if ($_.Exception.Response) {
            $errorStatus = [int]$_.Exception.Response.StatusCode
        }
        if ($errorStatus -eq $expectedStatus) {
            Write-Host "[PASS] Status=$errorStatus (expected error)" -ForegroundColor Green
            $script:passed++
            return $null
        }
        Write-Host "[FAIL] Error: $($_.Exception.Message)" -ForegroundColor Red
        if ($errorStatus) { Write-Host "       Status: $errorStatus" -ForegroundColor Yellow }
        $script:failed++
        return $null
    }
}

# ===== Test 0: Health Check =====
Test-Api "GET /health" "GET" "$baseUrl/health" -extraHeaders @{ Authorization = $null }

# ===== Test 1: Create EHR Record =====
$createBody = @{
    patientId = $patientId.ToString()
    encounterId = $encounterId.ToString()
    orgId = $orgId.ToString()
    data = @{
        diagnosis = "Hypertension Stage 2"
        bloodPressure = "160/100"
        medications = @("Amlodipine 10mg", "Losartan 50mg")
        notes = "Patient needs follow-up in 2 weeks"
    }
} | ConvertTo-Json -Depth 5

$createResult = Test-Api "POST /api/v1/ehr/records (Create EHR)" "POST" "$baseUrl/api/v1/ehr/records" -body $createBody -expectedStatus 201
$ehrId = $null
$versionId = $null
$fileId = $null

if ($createResult) {
    $ehrId = $createResult.ehrId
    $versionId = $createResult.versionId
    $fileId = $createResult.fileId
    Write-Host "  -> EhrId=$ehrId, VersionId=$versionId, FileId=$fileId" -ForegroundColor DarkGray
    Write-Host "  -> IpfsCid=$($createResult.ipfsCid), DataHash=$($createResult.dataHash)" -ForegroundColor DarkGray
}

if (-not $ehrId) {
    Write-Host "`n[ABORT] Cannot continue without EhrId from Create." -ForegroundColor Red
    exit 1
}

# ===== Test 2: Get EHR Record =====
$getResult = Test-Api "GET /api/v1/ehr/records/{ehrId}" "GET" "$baseUrl/api/v1/ehr/records/$ehrId"
if ($getResult) {
    Write-Host "  -> PatientId=$($getResult.patientId), OrgId=$($getResult.orgId)" -ForegroundColor DarkGray
}

# ===== Test 3: Get Patient EHR Records =====
$patientRecords = Test-Api "GET /api/v1/ehr/records/patient/{patientId}" "GET" "$baseUrl/api/v1/ehr/records/patient/$patientId"

# ===== Test 4: Get Org EHR Records =====
$orgRecords = Test-Api "GET /api/v1/ehr/records/org/{orgId}" "GET" "$baseUrl/api/v1/ehr/records/org/$orgId"

# ===== Test 5: Update EHR Record (Create New Version) =====
$updateBody = @{
    data = @{
        diagnosis = "Hypertension Stage 2 - Controlled"
        bloodPressure = "140/85"
        medications = @("Amlodipine 10mg", "Losartan 50mg", "Hydrochlorothiazide 12.5mg")
        notes = "Blood pressure improving with added diuretic"
    }
} | ConvertTo-Json -Depth 5

$updateResult = Test-Api "PUT /api/v1/ehr/records/{ehrId} (Update EHR)" "PUT" "$baseUrl/api/v1/ehr/records/$ehrId" -body $updateBody

# ===== Test 6: Get EHR Versions =====
$versions = Test-Api "GET /api/v1/ehr/records/{ehrId}/versions" "GET" "$baseUrl/api/v1/ehr/records/$ehrId/versions"
if ($versions) {
    Write-Host "  -> Version count: $($versions.Count)" -ForegroundColor DarkGray
}

# ===== Test 7: Get Version by ID =====
if ($versionId) {
    $versionDetail = Test-Api "GET /api/v1/ehr/records/{ehrId}/versions/{versionId}" "GET" "$baseUrl/api/v1/ehr/records/$ehrId/versions/$versionId"
    if ($versionDetail) {
        Write-Host "  -> VersionNumber=$($versionDetail.versionNumber), IpfsCid=$($versionDetail.ipfsCid)" -ForegroundColor DarkGray
    }
}

# ===== Test 8: Get EHR Files =====
$files = Test-Api "GET /api/v1/ehr/records/{ehrId}/files" "GET" "$baseUrl/api/v1/ehr/records/$ehrId/files"
if ($files) {
    Write-Host "  -> File count: $($files.Count)" -ForegroundColor DarkGray
}

# ===== Test 9: Add File to EHR (multipart/form-data) =====
$tempFile = [System.IO.Path]::GetTempFileName()
[System.IO.File]::WriteAllText($tempFile, "Test medical report content - lab results for patient")
$fileResult = $null
try {
    $script:total++
    Write-Host "[$script:total] POST /api/v1/ehr/records/{ehrId}/files (Upload File) " -NoNewline
    
    # Use .NET HttpClient for multipart upload
    Add-Type -AssemblyName System.Net.Http
    $client = New-Object System.Net.Http.HttpClient
    $client.DefaultRequestHeaders.Authorization = New-Object System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", $token)
    
    $content = New-Object System.Net.Http.MultipartFormDataContent
    $fileBytes = [System.IO.File]::ReadAllBytes($tempFile)
    $byteContent = New-Object System.Net.Http.ByteArrayContent($fileBytes)
    $byteContent.Headers.ContentType = New-Object System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream")
    $content.Add($byteContent, "file", "test-report.txt")
    
    $response = $client.PostAsync("$baseUrl/api/v1/ehr/records/$ehrId/files", $content).Result
    $responseBody = $response.Content.ReadAsStringAsync().Result
    $status = [int]$response.StatusCode
    
    if ($status -eq 201) {
        Write-Host "[PASS] Status=$status" -ForegroundColor Green
        $script:passed++
        $fileResult = $responseBody | ConvertFrom-Json -ErrorAction SilentlyContinue
        if ($fileResult) {
            Write-Host "  -> FileId=$($fileResult.fileId), FileHash=$($fileResult.fileHash)" -ForegroundColor DarkGray
        }
    } else {
        Write-Host "[FAIL] Status=$status Body=$responseBody" -ForegroundColor Red
        $script:failed++
    }
    $client.Dispose()
}
catch {
    Write-Host "[FAIL] Error: $($_.Exception.Message)" -ForegroundColor Red
    $script:failed++
}
finally {
    Remove-Item $tempFile -ErrorAction SilentlyContinue
}

# ===== Test 10: Get Files after upload =====
$filesAfter = Test-Api "GET /api/v1/ehr/records/{ehrId}/files (after add)" "GET" "$baseUrl/api/v1/ehr/records/$ehrId/files"
if ($filesAfter) {
    Write-Host "  -> File count: $($filesAfter.Count)" -ForegroundColor DarkGray
}

# ===== Test 11: Delete File =====
$deleteFileId = if ($fileResult) { $fileResult.fileId } else { $fileId }
if ($deleteFileId) {
    Test-Api "DELETE /api/v1/ehr/records/{ehrId}/files/{fileId}" "DELETE" "$baseUrl/api/v1/ehr/records/$ehrId/files/$deleteFileId" -expectedStatus 204
}

# ===== Test 12: Get Files after delete =====
$filesAfterDelete = Test-Api "GET /api/v1/ehr/records/{ehrId}/files (after delete)" "GET" "$baseUrl/api/v1/ehr/records/$ehrId/files"
if ($filesAfterDelete) {
    Write-Host "  -> File count: $($filesAfterDelete.Count)" -ForegroundColor DarkGray
}

# ===== Test 13: 404 for non-existent EHR =====
$fakeEhrId = [guid]::NewGuid()
Test-Api "GET /api/v1/ehr/records/{fakeId} (404)" "GET" "$baseUrl/api/v1/ehr/records/$fakeEhrId" -expectedStatus 404

# ===== Summary =====
Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  Results: $passed/$total PASSED, $failed FAILED" -ForegroundColor $(if ($failed -eq 0) { "Green" } else { "Red" })
Write-Host "============================================" -ForegroundColor Cyan
