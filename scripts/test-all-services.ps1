#!/usr/bin/env pwsh
# =============================================================================
# Full System API Test — All 8 Services via Gateway (port 5000)
# =============================================================================
$ErrorActionPreference = "Continue"
$BASE = "http://localhost:5000"
$passed = 0; $failed = 0; $total = 0

function Test($name, $method, $url, $body = $null, $token = $null, $expected = 200, $extra = @{}) {
    $script:total++
    Write-Host "[$script:total] $name " -NoNewline
    try {
        $h = @{}
        if ($token) { $h["Authorization"] = "Bearer $token" }
        foreach ($k in $extra.Keys) { $h[$k] = $extra[$k] }
        $p = @{ Method = $method; Uri = $url; Headers = $h; ErrorAction = "Stop" }
        if ($body) { $p["Body"] = ($body | ConvertTo-Json -Depth 10); $p["ContentType"] = "application/json" }
        $r = Invoke-WebRequest @p
        $code = $r.StatusCode
        if ($code -eq $expected) {
            Write-Host "[PASS $code]" -ForegroundColor Green
            $script:passed++
            return ($r.Content | ConvertFrom-Json -ErrorAction SilentlyContinue)
        } else {
            Write-Host "[FAIL exp=$expected got=$code]" -ForegroundColor Red
            Write-Host "  Body: $($r.Content.Substring(0,[Math]::Min(200,$r.Content.Length)))" -ForegroundColor DarkYellow
            $script:failed++; return $null
        }
    } catch {
        $es = if ($_.Exception.Response) { [int]$_.Exception.Response.StatusCode } else { 0 }
        if ($es -eq $expected) {
            Write-Host "[PASS $es]" -ForegroundColor Green
            $script:passed++; return $null
        }
        # Try to read error body
        $errBody = ""
        try {
            $sr = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $errBody = $sr.ReadToEnd(); $sr.Close()
            $errBody = $errBody.Substring(0,[Math]::Min(200,$errBody.Length))
        } catch {}
        Write-Host "[FAIL err=$es]" -ForegroundColor Red
        if ($errBody) { Write-Host "  Body: $errBody" -ForegroundColor DarkYellow }
        $script:failed++; return $null
    }
}

# =============================================================================
Write-Host "`n=== SYSTEM HEALTH CHECKS ===" -ForegroundColor Cyan
# =============================================================================
$services = @(
    @{ name = "Gateway";       url = "$BASE/health" },
    @{ name = "Auth";          url = "http://localhost:5101/health" },
    @{ name = "Organization";  url = "http://localhost:5002/health" },
    @{ name = "EHR";           url = "http://localhost:5003/health" },
    @{ name = "Consent";       url = "http://localhost:5004/health" },
    @{ name = "Audit";         url = "http://localhost:5005/health" },
    @{ name = "Notification";  url = "http://localhost:5006/health" },
    @{ name = "Appointment";   url = "http://localhost:5007/health" }
)
foreach ($s in $services) { Test "Health: $($s.name)" "GET" $s.url }

# =============================================================================
Write-Host "`n=== AUTH SERVICE ===" -ForegroundColor Cyan
# =============================================================================

# Login Admin
$adminLogin = Test "Auth: Login Admin" "POST" "$BASE/api/v1/auth/login" @{ email="admin@dbh.vn"; password="Admin@123456" }
$adminToken = $adminLogin.token

# Login Doctor
$docLogin = Test "Auth: Login Doctor" "POST" "$BASE/api/v1/auth/login" @{ email="dr.hieu@dbh.vn"; password="Doctor@123" }
$docToken = $docLogin.token

# Login Patient
$patLogin = Test "Auth: Login Patient" "POST" "$BASE/api/v1/auth/login" @{ email="patient.an@dbh.vn"; password="Patient@123" }
$patToken = $patLogin.token

# Get profile
$adminMe = Test "Auth: GET /me (Admin)" "GET" "$BASE/api/v1/auth/me" -token $adminToken
$docMe   = Test "Auth: GET /me (Doctor)" "GET" "$BASE/api/v1/auth/me" -token $docToken
$patMe   = Test "Auth: GET /me (Patient)" "GET" "$BASE/api/v1/auth/me" -token $patToken

$adminUserId = $adminMe.userId
$docUserId   = $docMe.userId
$docDoctorId = $docMe.profiles.Doctor.doctorId
$patUserId   = $patMe.userId
$patPatientId = $patMe.profiles.Patient.patientId

Write-Host "  Admin=$adminUserId, Doc=$docUserId($docDoctorId), Pat=$patUserId($patPatientId)" -ForegroundColor DarkGray

# Auth: Invalid login
Test "Auth: Invalid Login (401)" "POST" "$BASE/api/v1/auth/login" @{ email="bad@bad.com"; password="wrong" } -expected 401

# Auth: Unauthorized access
Test "Auth: No Token (401)" "GET" "$BASE/api/v1/auth/me" -expected 401

# =============================================================================
Write-Host "`n=== ORGANIZATION SERVICE ===" -ForegroundColor Cyan
# =============================================================================

$orgs = Test "Org: List All" "GET" "$BASE/api/v1/organizations" -token $adminToken
if ($orgs) {
    $orgList = if ($orgs.data) { $orgs.data } else { $orgs }
    # Find the hospital (BVDKTU) where doctors are members — pick the one with most members
    $hospitalOrg = $orgList | Where-Object { $_.orgCode -eq "BVDKTU" } | Select-Object -First 1
    if (-not $hospitalOrg) { $hospitalOrg = $orgList | Sort-Object -Property memberCount -Descending | Select-Object -First 1 }
    if (-not $hospitalOrg) { $hospitalOrg = $orgList[0] }
    $hospitalOrgId = $hospitalOrg.orgId
    Write-Host "  Hospital: $($hospitalOrg.orgName) [$($hospitalOrg.orgCode)] | OrgId: $hospitalOrgId | Members: $($hospitalOrg.memberCount)" -ForegroundColor DarkGray
}

Test "Org: Get By ID" "GET" "$BASE/api/v1/organizations/$hospitalOrgId" -token $adminToken

$depts = Test "Org: List Departments" "GET" "$BASE/api/v1/departments/by-organization/$hospitalOrgId" -token $adminToken

$membs = Test "Org: List Memberships" "GET" "$BASE/api/v1/memberships/by-organization/$hospitalOrgId" -token $adminToken

# =============================================================================
Write-Host "`n=== EHR SERVICE ===" -ForegroundColor Cyan
# =============================================================================

# Create EHR (Doctor role)
$ehrBody = @{
    patientId = "$patPatientId"
    orgId     = "$hospitalOrgId"
    data      = @{
        resourceType = "Bundle"
        type = "document"
        entry = @(
            @{ resource = @{ resourceType = "Condition"; code = @{ text = "System test - tang huyet ap" } } },
            @{ resource = @{ resourceType = "Observation"; code = @{ text = "HA" }; valueQuantity = @{ value = 150; unit = "mmHg" } } }
        )
    }
}
$ehrResult = Test "EHR: Create Record" "POST" "$BASE/api/v1/ehr/records" $ehrBody $docToken -expected 201
$testEhrId = $ehrResult.ehrId
$testVersionId = $ehrResult.versionId
Write-Host "  EhrId=$testEhrId, IpfsCid=$($ehrResult.ipfsCid)" -ForegroundColor DarkGray

# Get EHR
Test "EHR: Get Record" "GET" "$BASE/api/v1/ehr/records/$testEhrId" -token $docToken

# Get Patient EHRs
Test "EHR: Patient Records" "GET" "$BASE/api/v1/ehr/records/patient/$patPatientId" -token $docToken

# Get Org EHRs
Test "EHR: Org Records" "GET" "$BASE/api/v1/ehr/records/org/$hospitalOrgId" -token $docToken

# Update EHR
$updateBody = @{
    data = @{
        resourceType = "Bundle"
        type = "document"
        entry = @(
            @{ resource = @{ resourceType = "Condition"; code = @{ text = "Tang huyet ap - On dinh" } } },
            @{ resource = @{ resourceType = "Observation"; code = @{ text = "HA" }; valueQuantity = @{ value = 130; unit = "mmHg" } } }
        )
    }
}
Test "EHR: Update Record (v2)" "PUT" "$BASE/api/v1/ehr/records/$testEhrId" $updateBody $docToken

# Versions
$versions = Test "EHR: List Versions" "GET" "$BASE/api/v1/ehr/records/$testEhrId/versions" -token $docToken
if ($versions) { Write-Host "  Versions: $($versions.Count)" -ForegroundColor DarkGray }

Test "EHR: Get Version Detail" "GET" "$BASE/api/v1/ehr/records/$testEhrId/versions/$testVersionId" -token $docToken

# Files
Test "EHR: List Files" "GET" "$BASE/api/v1/ehr/records/$testEhrId/files" -token $docToken

# Upload file via curl
$script:total++
Write-Host "[$script:total] EHR: Upload File (multipart) " -NoNewline
$tmpFile = "$env:TEMP\test-system-file.txt"
Set-Content -Path $tmpFile -Value "Lab report: CBC normal ranges"
$curlOut = & curl.exe -s -o - -w "`n%{http_code}" -X POST "$BASE/api/v1/ehr/records/$testEhrId/files" -H "Authorization: Bearer $docToken" -F "file=@$tmpFile;type=text/plain" 2>&1
$lines = $curlOut -split "`n"
$httpCode = $lines[-1].Trim()
if ($httpCode -eq "201") {
    Write-Host "[PASS 201]" -ForegroundColor Green
    $script:passed++
    $fileData = ($lines[0..($lines.Count-2)] -join "`n") | ConvertFrom-Json -ErrorAction SilentlyContinue
    $uploadedFileId = $fileData.fileId
    Write-Host "  FileId=$uploadedFileId" -ForegroundColor DarkGray
} else {
    Write-Host "[FAIL $httpCode]" -ForegroundColor Red
    $script:failed++
    $uploadedFileId = $null
}
Remove-Item $tmpFile -ErrorAction SilentlyContinue

# Delete file
if ($uploadedFileId) {
    Test "EHR: Delete File" "DELETE" "$BASE/api/v1/ehr/records/$testEhrId/files/$uploadedFileId" -token $docToken -expected 204
}

# 404 test
Test "EHR: Not Found (404)" "GET" "$BASE/api/v1/ehr/records/$([guid]::NewGuid())" -token $docToken -expected 404

# =============================================================================
Write-Host "`n=== CONSENT SERVICE ===" -ForegroundColor Cyan
# =============================================================================

# Login patient (Dung — no existing consent to dr.hieu)
$pat2Login = Test "Auth: Login Patient2" "POST" "$BASE/api/v1/auth/login" @{ email="patient.dung@dbh.vn"; password="Patient@123" }
$pat2Token = $pat2Login.token
$pat2Me = Test "Auth: GET /me (Patient2)" "GET" "$BASE/api/v1/auth/me" -token $pat2Token
$pat2PatientId = $pat2Me.profiles.Patient.patientId

# Login a second doctor (Lan) to avoid duplicate consent
$doc2Login = Test "Auth: Login Doctor2" "POST" "$BASE/api/v1/auth/login" @{ email="dr.lan@dbh.vn"; password="Doctor@123" }
$doc2Token = $doc2Login.token
$doc2Me = Test "Auth: GET /me (Doctor2)" "GET" "$BASE/api/v1/auth/me" -token $doc2Token
$doc2DoctorId = $doc2Me.profiles.Doctor.doctorId

# Create consent (Dung -> Dr Lan — unique combo)
$consentBody = @{
    patientId   = "$pat2PatientId"
    patientDid  = "did:dbh:patient:$($pat2Me.userId)"
    granteeId   = "$doc2DoctorId"
    granteeDid  = "did:dbh:doctor:$($doc2Me.userId)"
    granteeType = "DOCTOR"
    permission  = "READ"
    purpose     = "TREATMENT"
    durationDays = 30
}
$consentResult = Test "Consent: Create" "POST" "$BASE/api/v1/consents" $consentBody $pat2Token -expected 201
$consentId = if ($consentResult.data.consentId) { $consentResult.data.consentId } else { $consentResult.consentId }
Write-Host "  ConsentId=$consentId" -ForegroundColor DarkGray

# List consents
Test "Consent: List by Patient" "GET" "$BASE/api/v1/consents/by-patient/$pat2PatientId" -token $pat2Token

# Revoke consent
if ($consentId) {
    Test "Consent: Revoke" "POST" "$BASE/api/v1/consents/$consentId/revoke" @{ revokeReason = "System test revoke" } $pat2Token
}

# =============================================================================
Write-Host "`n=== APPOINTMENT SERVICE ===" -ForegroundColor Cyan
# =============================================================================

# Create appointment
$aptDate = (Get-Date).AddDays(7).AddHours(10).ToString("yyyy-MM-ddTHH:mm:ssZ")
$aptBody = @{
    patientId   = "$patPatientId"
    doctorId    = "$docDoctorId"
    orgId       = "$hospitalOrgId"
    scheduledAt = $aptDate
}
$aptResult = Test "Apt: Create" "POST" "$BASE/api/v1/appointments" $aptBody $patToken -expected 201
$aptId = if ($aptResult.data.appointmentId) { $aptResult.data.appointmentId } else { $aptResult.appointmentId }
Write-Host "  AptId=$aptId" -ForegroundColor DarkGray

# Get appointment
if ($aptId) {
    Test "Apt: Get By ID" "GET" "$BASE/api/v1/appointments/$aptId" -token $patToken
}

# Confirm appointment (doctor)
if ($aptId) {
    Test "Apt: Confirm (Doctor)" "PUT" "$BASE/api/v1/appointments/$aptId/confirm" -token $docToken
}

# List patient appointments
Test "Apt: List Patient" "GET" "$BASE/api/v1/appointments?patientId=$patPatientId" -token $patToken

# =============================================================================
Write-Host "`n=== AUDIT SERVICE ===" -ForegroundColor Cyan
# =============================================================================

$auditBody = @{
    actorDid     = "did:dbh:doctor:$docUserId"
    actorUserId  = "$docUserId"
    actorType    = "DOCTOR"
    action       = "VIEW"
    targetType   = "EHR"
    result       = "SUCCESS"
    patientDid   = "did:dbh:patient:$patUserId"
    patientId    = "$patPatientId"
}
Test "Audit: Create Log" "POST" "$BASE/api/v1/audit" $auditBody $adminToken -expected 200

Test "Audit: List (search)" "GET" "$BASE/api/v1/audit/search" -token $adminToken

Test "Audit: By Patient" "GET" "$BASE/api/v1/audit/by-patient/$patPatientId" -token $adminToken

# =============================================================================
Write-Host "`n=== NOTIFICATION SERVICE ===" -ForegroundColor Cyan
# =============================================================================

$notifBody = @{
    recipientDid    = "did:dbh:patient:$patUserId"
    recipientUserId = "$patUserId"
    title           = "System Test Notification"
    body            = "This is a test notification from the system test script."
    type            = "System"
    priority        = "Normal"
    channel         = "InApp"
}
Test "Notif: Create" "POST" "$BASE/api/v1/notifications" $notifBody $adminToken -expected 200

$patDid = "did:dbh:patient:$patUserId"
Test "Notif: List by DID" "GET" "$BASE/api/v1/notifications/by-user/$patDid" -token $patToken

Test "Notif: Unread Count" "GET" "$BASE/api/v1/notifications/by-user/$patDid/unread-count" -token $patToken

# =============================================================================
Write-Host "`n=== GATEWAY ROUTING ===" -ForegroundColor Cyan
# =============================================================================

# Test that gateway routes correctly by hitting service-specific endpoints
Test "Gateway: Auth via GW" "GET" "$BASE/api/v1/auth/me" -token $adminToken
Test "Gateway: Org via GW" "GET" "$BASE/api/v1/organizations" -token $adminToken
Test "Gateway: EHR via GW" "GET" "$BASE/api/v1/ehr/records/patient/$patPatientId" -token $docToken
Test "Gateway: Consent via GW" "GET" "$BASE/api/v1/consents/by-patient/$patPatientId" -token $patToken
Test "Gateway: Audit via GW" "GET" "$BASE/api/v1/audit/search" -token $adminToken

# =============================================================================
# SUMMARY
# =============================================================================
Write-Host "`n============================================" -ForegroundColor Cyan
Write-Host " TOTAL: $total tests | PASS: $passed | FAIL: $failed" -ForegroundColor $(if ($failed -eq 0) { "Green" } else { "Red" })
Write-Host "============================================" -ForegroundColor Cyan
