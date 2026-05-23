#!/usr/bin/env powershell
# =============================================================================
# DBH-EHR System -- Blockchain Scenario Integration Tests
# =============================================================================
# Tests:
#   1. Blockchain OFF -> Create EHR -> expect blockchain-down message
#   2. Blockchain ON  -> Create EHR -> expect NO blockchain-down message
#   3. Stop 2/3 peers (majority lost) -> Create EHR -> expect blockchain error
#   4. Consent-based document access -> expect success
#   5. Patient self-download document -> expect success
#   6. Corrupt IPFS CID in DB -> GET document -> expect Tampering Detected
# =============================================================================

$ErrorActionPreference = 'Continue'

$base = 'http://127.0.0.1:5000'
$logFile = Join-Path $PSScriptRoot 'test-ehr-blockchain-scenarios.log'
$resultFile = Join-Path $PSScriptRoot 'test-ehr-blockchain-scenarios-result.log'

if (Test-Path $logFile) { Remove-Item $logFile -Force }

# =============================================================================
# Helpers
# =============================================================================

function Write-FlowLog {
    param([string]$Message)
    $entry = "[{0}] {1}" -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss.fff'), $Message
    Add-Content -Path $logFile -Value $entry
    Write-Host $entry
}

function ApiJson {
    param(
        [string]$Method,
        [string]$Url,
        $Body = $null,
        [string]$Token = $null,
        [hashtable]$Headers = @{}
    )

    $h = @{ 'Content-Type' = 'application/json' }
    if ($Token) { $h['Authorization'] = "Bearer $Token" }
    foreach ($k in $Headers.Keys) { $h[$k] = $Headers[$k] }

    Write-FlowLog ("==> {0} {1}" -f $Method, $Url)
    if ($Headers.Count -gt 0) {
        Write-FlowLog ("Headers: {0}" -f (($Headers.GetEnumerator() | Sort-Object Key | ForEach-Object { "{0}={1}" -f $_.Key, $_.Value }) -join '; '))
    }

    try {
        if ($null -ne $Body) {
            $json = $Body | ConvertTo-Json -Depth 30
            Write-FlowLog ("RequestBody: {0}" -f $json)
            $response = Invoke-RestMethod -Method $Method -Uri $Url -Headers $h -Body $json
        } else {
            $response = Invoke-RestMethod -Method $Method -Uri $Url -Headers $h
        }

        $responseText = if ($response -is [string]) { $response } else { $response | ConvertTo-Json -Depth 30 }
        Write-FlowLog ("Response: {0}" -f $responseText)
        return $response
    }
    catch {
        Write-FlowLog ("FAILED: {0} {1}" -f $Method, $Url)
        if ($_.Exception.Response) {
            try {
                $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
                $bodyText = $reader.ReadToEnd()
                if ($bodyText) { Write-FlowLog ("ErrorBody: {0}" -f $bodyText) }
            } catch {}
        }
        Write-FlowLog ("Exception: {0}" -f $_.Exception.Message)
        throw
    }
}

# Same as ApiJson but does NOT throw -- returns @{ Response=$obj; StatusCode=$int; ErrorBody=$string; Success=$bool }
function ApiJsonSafe {
    param(
        [string]$Method,
        [string]$Url,
        $Body = $null,
        [string]$Token = $null,
        [hashtable]$Headers = @{}
    )

    $h = @{ 'Content-Type' = 'application/json' }
    if ($Token) { $h['Authorization'] = "Bearer $Token" }
    foreach ($k in $Headers.Keys) { $h[$k] = $Headers[$k] }

    Write-FlowLog ("==> {0} {1}" -f $Method, $Url)
    if ($Headers.Count -gt 0) {
        Write-FlowLog ("Headers: {0}" -f (($Headers.GetEnumerator() | Sort-Object Key | ForEach-Object { "{0}={1}" -f $_.Key, $_.Value }) -join '; '))
    }

    try {
        $requestBodyText = $null
        if ($null -ne $Body) {
            $json = $Body | ConvertTo-Json -Depth 30
            $requestBodyText = $json
            Write-FlowLog ("RequestBody: {0}" -f $json)
            $response = Invoke-WebRequest -Method $Method -Uri $Url -Headers $h -Body $json -ErrorAction Stop
        } else {
            $response = Invoke-WebRequest -Method $Method -Uri $Url -Headers $h -ErrorAction Stop
        }

        $statusCode = [int]$response.StatusCode
        $obj = $response.Content | ConvertFrom-Json -ErrorAction SilentlyContinue
        $responseText = $response.Content
        Write-FlowLog ("Response [{0}]: {1}" -f $statusCode, $responseText)
        return @{ Response = $obj; StatusCode = $statusCode; ErrorBody = $null; Success = $true; RawContent = $responseText }
    }
    catch {
        $statusCode = 0
        $errorBody = ''
        if ($_.Exception.Response) {
            $statusCode = [int]$_.Exception.Response.StatusCode
            try {
                $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
                $errorBody = $reader.ReadToEnd()
                $reader.Close()
            } catch {}
        }
        Write-FlowLog ("FAILED [{0}]: {1} {2}" -f $statusCode, $Method, $Url)
        if ($errorBody) { Write-FlowLog ("ErrorBody: {0}" -f $errorBody) }
        Write-FlowLog ("Exception: {0}" -f $_.Exception.Message)
        $errObj = $errorBody | ConvertFrom-Json -ErrorAction SilentlyContinue
        return @{ Response = $errObj; StatusCode = $statusCode; ErrorBody = $errorBody; Success = $false; RawContent = $errorBody }
    }
}

function Sep([string]$Title) {
    $line = "=" * 80
    Write-FlowLog $line
    Write-FlowLog "  $Title"
    Write-FlowLog $line
}

# =============================================================================
# Docker helper: stop/start Fabric containers
# =============================================================================

$fabricPeers = @(
    'peer0.hospital1.ehr.com',
    'peer0.hospital2.ehr.com',
    'peer0.clinic.ehr.com'
)
$fabricOrderer = 'orderer.ehr.com'
$fabricAllNodes = $fabricPeers + @($fabricOrderer)

function Stop-FabricAll {
    Write-FlowLog "DOCKER: Stopping ALL Fabric nodes: $($fabricAllNodes -join ', ')"
    foreach ($c in $fabricAllNodes) {
        docker stop $c 2>&1 | Out-Null
        Write-FlowLog "  Stopped $c"
    }
}

function Start-FabricAll {
    param([int]$WaitSeconds = 30)
    Write-FlowLog "DOCKER: Starting ALL Fabric nodes: $($fabricAllNodes -join ', ')"
    foreach ($c in $fabricAllNodes) {
        docker start $c 2>&1 | Out-Null
        Write-FlowLog "  Started $c"
    }
    Write-FlowLog "  Waiting ${WaitSeconds}s for Fabric nodes to become ready..."
    Start-Sleep -Seconds $WaitSeconds
}

function Stop-FabricMinorityPeers {
    # Stop Hospital2 + Clinic peers -- keeps Hospital1 (primary) + Orderer running
    # This breaks the endorsement majority (2 of 3 orgs down)
    $toStop = @('peer0.hospital2.ehr.com', 'peer0.clinic.ehr.com')
    Write-FlowLog "DOCKER: Stopping minority peers (majority broken): $($toStop -join ', ')"
    foreach ($c in $toStop) {
        docker stop $c 2>&1 | Out-Null
        Write-FlowLog "  Stopped $c"
    }
}

function Start-FabricMinorityPeers {
    param([int]$WaitSeconds = 30)
    $toStart = @('peer0.hospital2.ehr.com', 'peer0.clinic.ehr.com')
    Write-FlowLog "DOCKER: Starting minority peers: $($toStart -join ', ')"
    foreach ($c in $toStart) {
        docker start $c 2>&1 | Out-Null
        Write-FlowLog "  Started $c"
    }
    Write-FlowLog "  Waiting ${WaitSeconds}s for peers to rejoin..."
    Start-Sleep -Seconds $WaitSeconds
}

# =============================================================================
# Test results collector
# =============================================================================

$testResults = [ordered]@{}
$passed = 0
$failed = 0

function Record-Test {
    param(
        [string]$Name,
        [bool]$Pass,
        [string]$Detail = ''
    )
    $status = if ($Pass) { 'PASS' } else { 'FAIL' }
    $color = if ($Pass) { 'Green' } else { 'Red' }
    Write-Host "  [$status] $Name" -ForegroundColor $color
    Write-FlowLog ("TEST [{0}] {1} -- {2}" -f $status, $Name, $Detail)
    $script:testResults[$Name] = @{ Status = $status; Detail = $Detail }
    if ($Pass) { $script:passed++ } else { $script:failed++ }
}

# =============================================================================
# SETUP: Login, create accounts, memberships, appointment
# =============================================================================

Sep "SETUP: Login and create fresh test accounts"

$stamp = Get-Date -Format 'yyyyMMddHHmmss'
Write-FlowLog "Test run stamp: $stamp"

# Admin login
$adminLogin = ApiJson -Method 'POST' -Url "$base/api/v1/auth/login" -Body @{ email = 'admin@dbh.com'; password = 'admin123' }
$adminToken = $adminLogin.token

# Get first org
$orgs = ApiJson -Method 'GET' -Url "$base/api/v1/organizations?page=1&pageSize=50" -Token $adminToken
$orgId = [Guid]$orgs.data[0].orgId
Write-FlowLog "OrgId: $orgId"

# Create fresh doctor
$doctorEmail = "bc.doctor.$stamp@dbh.vn"
$null = ApiJson -Method 'POST' -Url "$base/api/v1/auth/registerStaffDoctor" -Token $adminToken -Body @{
    fullName = "BCTest Doctor $stamp"; email = $doctorEmail; password = 'Doctor@123'
    phone = "0921$($stamp.Substring(8,6))"; organizationId = $orgId; role = 'Doctor'
}

# Create fresh patient (via reception flow: first create a receptionist, then register the patient)
$receptionEmail = "bc.reception.$stamp@dbh.vn"
$null = ApiJson -Method 'POST' -Url "$base/api/v1/auth/registerStaffDoctor" -Token $adminToken -Body @{
    fullName = "BCTest Reception $stamp"; email = $receptionEmail; password = 'Reception@123'
    phone = "0911$($stamp.Substring(8,6))"; organizationId = $orgId; role = 'Receptionist'
}

$receptionLogin = ApiJson -Method 'POST' -Url "$base/api/v1/auth/login" -Body @{ email = $receptionEmail; password = 'Reception@123' }
$receptionToken = $receptionLogin.token
$receptionMe = ApiJson -Method 'GET' -Url "$base/api/v1/auth/me" -Token $receptionToken
$receptionUserId = [Guid]$receptionMe.userId

$patientEmail = "bc.patient.$stamp@dbh.vn"
$null = ApiJson -Method 'POST' -Url "$base/api/v1/auth/register" -Token $receptionToken -Body @{
    fullName = "BCTest Patient $stamp"; email = $patientEmail; password = 'Patient@123'
    phone = "0931$($stamp.Substring(8,6))"
}

# Login all
$doctorLogin = ApiJson -Method 'POST' -Url "$base/api/v1/auth/login" -Body @{ email = $doctorEmail; password = 'Doctor@123' }
$doctorToken = $doctorLogin.token
$doctorMe = ApiJson -Method 'GET' -Url "$base/api/v1/auth/me" -Token $doctorToken
$doctorUserId = [Guid]$doctorMe.userId
$doctorId = [Guid]$doctorMe.profiles.Doctor.doctorId

$patientLogin = ApiJson -Method 'POST' -Url "$base/api/v1/auth/login" -Body @{ email = $patientEmail; password = 'Patient@123' }
$patientToken = $patientLogin.token
$patientMe = ApiJson -Method 'GET' -Url "$base/api/v1/auth/me" -Token $patientToken
$patientUserId = [Guid]$patientMe.userId
$patientId = [Guid]$patientMe.profiles.Patient.patientId

Write-FlowLog "Doctor: userId=$doctorUserId, doctorId=$doctorId, email=$doctorEmail"
Write-FlowLog "Patient: userId=$patientUserId, patientId=$patientId, email=$patientEmail"

# Verify patient keys
$patientResolved = ApiJson -Method 'GET' -Url "$base/api/v1/auth/user-id?patientId=$patientId" -Token $adminToken
$patientResolvedUserId = [Guid]$patientResolved.userId
$patientKeys = ApiJson -Method 'GET' -Url "$base/api/v1/auth/$patientResolvedUserId/keys" -Token $adminToken

if ([string]::IsNullOrWhiteSpace($patientKeys.publicKey) -or [string]::IsNullOrWhiteSpace($patientKeys.encryptedPrivateKey)) {
    throw "Patient auth keys are missing for user $patientResolvedUserId. Registration did not initialize the keypair correctly."
}
Write-FlowLog "Patient keys verified OK"

# Create memberships
$null = ApiJson -Method 'POST' -Url "$base/api/v1/memberships" -Token $adminToken -Body @{
    userId = $receptionUserId; orgId = $orgId
    employeeId = "EMP-REC-$($stamp.Substring(8,6))"; jobTitle = 'Receptionist'; startDate = '2024-01-01'
}
$null = ApiJson -Method 'POST' -Url "$base/api/v1/memberships" -Token $adminToken -Body @{
    userId = $doctorUserId; orgId = $orgId
    employeeId = "EMP-DOC-$($stamp.Substring(8,6))"; jobTitle = 'Doctor'; startDate = '2024-01-01'
}

# Create appointment (required for EHR creation flow)
$scheduled = (Get-Date).ToUniversalTime().AddDays(1).ToString('yyyy-MM-ddTHH:mm:ssZ')
$apt = ApiJson -Method 'POST' -Url "$base/api/v1/appointments" -Token $patientToken -Body @{
    patientId = $patientId; doctorId = $doctorId; orgId = $orgId; scheduledAt = $scheduled
}
$appointmentId = [Guid]$apt.data.appointmentId
$null = ApiJson -Method 'PUT' -Url "$base/api/v1/appointments/$appointmentId/confirm" -Token $doctorToken
$null = ApiJson -Method 'PUT' -Url "$base/api/v1/appointments/$appointmentId/check-in" -Token $doctorToken

Write-FlowLog "Setup complete: appointment=$appointmentId"

# Common EHR payload builder
function New-EhrPayload {
    param([string]$Label = 'Blockchain Test')
    return @{
        patientId = $patientId
        orgId     = $orgId
        data      = @{
            resourceType = 'Bundle'
            type         = 'document'
            entry        = @(
                @{ resource = @{ resourceType = 'Condition'; code = @{ text = "$Label - $stamp" }; clinicalStatus = @{ coding = @(@{ code = 'active' }) } } }
            )
        }
    }
}

# =============================================================================
# SCENARIO 1: Blockchain OFF -> Create EHR -> blockchain-down message expected
# =============================================================================

Sep "SCENARIO 1: Blockchain OFF -> Create EHR"

Stop-FabricAll
Start-Sleep -Seconds 5

Write-FlowLog "Creating EHR with blockchain stopped..."
$ehr1Payload = New-EhrPayload -Label 'Scenario1-BlockchainOFF'
$ehr1Result = ApiJsonSafe -Method 'POST' -Url "$base/api/v1/ehr/records" -Token $doctorToken `
    -Headers @{ 'X-Doctor-Id' = $doctorId.ToString() } -Body $ehr1Payload

$ehr1Created = $ehr1Result.StatusCode -eq 201
$ehr1Message = ''
if ($ehr1Result.Response) {
    $ehr1Message = if ($ehr1Result.Response.message) { $ehr1Result.Response.message } else { '' }
}

# The message should contain blockchain-down indicators
$blockchainDownIndicators = @('Blockchain', 'blockchain', 'commit', 'dead-letter', 'DLQ', 'FALLBACK')
$ehr1HasBlockchainMsg = $false
foreach ($indicator in $blockchainDownIndicators) {
    if ($ehr1Message -match [regex]::Escape($indicator)) {
        $ehr1HasBlockchainMsg = $true
        break
    }
}

Record-Test -Name "Scenario1: EHR created successfully (HTTP 201)" -Pass $ehr1Created `
    -Detail "StatusCode=$($ehr1Result.StatusCode)"
Record-Test -Name "Scenario1: Response contains blockchain-down message" -Pass ($ehr1Created -and $ehr1HasBlockchainMsg) `
    -Detail "Message='$ehr1Message'"

$ehr1Id = $null
if ($ehr1Result.Response -and $ehr1Result.Response.data) {
    $ehr1Id = $ehr1Result.Response.data.ehrId
}
Write-FlowLog "Scenario 1 EHR ID: $ehr1Id"

# =============================================================================
# SCENARIO 2: Blockchain ON -> Create EHR -> NO blockchain-down message
# =============================================================================

Sep "SCENARIO 2: Blockchain ON -> Create EHR"

Start-FabricAll -WaitSeconds 30

Write-FlowLog "Creating EHR with blockchain running..."
$ehr2Payload = New-EhrPayload -Label 'Scenario2-BlockchainON'
$ehr2Result = ApiJsonSafe -Method 'POST' -Url "$base/api/v1/ehr/records" -Token $doctorToken `
    -Headers @{ 'X-Doctor-Id' = $doctorId.ToString() } -Body $ehr2Payload

$ehr2Created = $ehr2Result.StatusCode -eq 201
$ehr2Message = ''
if ($ehr2Result.Response) {
    $ehr2Message = if ($ehr2Result.Response.message) { $ehr2Result.Response.message } else { '' }
}

$ehr2HasBlockchainMsg = $false
foreach ($indicator in $blockchainDownIndicators) {
    if ($ehr2Message -match [regex]::Escape($indicator)) {
        $ehr2HasBlockchainMsg = $true
        break
    }
}

Record-Test -Name "Scenario2: EHR created successfully (HTTP 201)" -Pass $ehr2Created `
    -Detail "StatusCode=$($ehr2Result.StatusCode)"
Record-Test -Name "Scenario2: Response does NOT contain blockchain-down message" -Pass ($ehr2Created -and (-not $ehr2HasBlockchainMsg)) `
    -Detail "Message='$ehr2Message'"

$ehr2Id = $null
if ($ehr2Result.Response -and $ehr2Result.Response.data) {
    $ehr2Id = $ehr2Result.Response.data.ehrId
}
Write-FlowLog "Scenario 2 EHR ID: $ehr2Id"

# =============================================================================
# SCENARIO 3: Stop 2/3 peers (majority broken) -> Create EHR -> blockchain error
# =============================================================================

Sep "SCENARIO 3: Majority broken (2/3 peers down) -> Create EHR"

Stop-FabricMinorityPeers
Start-Sleep -Seconds 5

Write-FlowLog "Creating EHR with majority of peers down (Hospital2 + Clinic stopped)..."
$ehr3Payload = New-EhrPayload -Label 'Scenario3-MajorityDown'
$ehr3Result = ApiJsonSafe -Method 'POST' -Url "$base/api/v1/ehr/records" -Token $doctorToken `
    -Headers @{ 'X-Doctor-Id' = $doctorId.ToString() } -Body $ehr3Payload

$ehr3Created = $ehr3Result.StatusCode -eq 201
$ehr3Message = ''
if ($ehr3Result.Response) {
    $ehr3Message = if ($ehr3Result.Response.message) { $ehr3Result.Response.message } else { '' }
}

$ehr3HasBlockchainMsg = $false
foreach ($indicator in $blockchainDownIndicators) {
    if ($ehr3Message -match [regex]::Escape($indicator)) {
        $ehr3HasBlockchainMsg = $true
        break
    }
}

Record-Test -Name "Scenario3: EHR created (HTTP 201 -- data saved to DB)" -Pass $ehr3Created `
    -Detail "StatusCode=$($ehr3Result.StatusCode)"
Record-Test -Name "Scenario3: Response contains blockchain error (majority broken)" -Pass ($ehr3Created -and $ehr3HasBlockchainMsg) `
    -Detail "Message='$ehr3Message'"

$ehr3Id = $null
if ($ehr3Result.Response -and $ehr3Result.Response.data) {
    $ehr3Id = $ehr3Result.Response.data.ehrId
}
Write-FlowLog "Scenario 3 EHR ID: $ehr3Id"

# Restore all peers for remaining scenarios
Start-FabricMinorityPeers -WaitSeconds 30

# =============================================================================
# SCENARIO 4: Consent-based document access -> success
# =============================================================================

Sep "SCENARIO 4: Consent-based document access"

# We use ehr2Id (created with blockchain ON) for document tests
$testEhrId = $ehr2Id
Write-FlowLog "Using EHR $testEhrId for consent / document tests"

# Wait for async blockchain AES-key commit
Write-FlowLog "Waiting 10s for async blockchain key commits..."
Start-Sleep -Seconds 10

# Step 4a: Doctor tries to read document WITHOUT consent -- should fail (403)
Write-FlowLog "Doctor tries GET /document WITHOUT consent..."
$noConsentResult = ApiJsonSafe -Method 'GET' -Url "$base/api/v1/ehr/records/$testEhrId/document" `
    -Token $doctorToken -Headers @{ 'X-Requester-Id' = $doctorUserId.ToString() }

Record-Test -Name "Scenario4a: Doctor denied without consent (403)" -Pass ($noConsentResult.StatusCode -eq 403) `
    -Detail "StatusCode=$($noConsentResult.StatusCode)"

# Step 4b: Create access request and approve
Write-FlowLog "Creating access request from doctor to patient..."
$accessReq = ApiJson -Method 'POST' -Url "$base/api/v1/access-requests" -Token $doctorToken -Body @{
    patientId    = $patientId
    patientDid   = "did:fabric:patient:$patientId"
    requesterId  = $doctorUserId
    requesterDid = "did:fabric:doctor:$doctorUserId"
    requesterType = 'DOCTOR'
    ehrId        = $testEhrId
    permission   = 'FULL_ACCESS'
    purpose      = 'TREATMENT'
    reason       = 'BCTest -- need EHR access for treatment'
}
$requestId = [Guid]$accessReq.data.requestId

Write-FlowLog "Patient approving access request $requestId..."
$null = ApiJson -Method 'POST' -Url "$base/api/v1/access-requests/$requestId/respond" -Token $patientToken `
    -Body @{ approve = $true; responseReason = 'Approved for BCTest' }

# Wait for consent blockchain sync
Write-FlowLog "Waiting 10s for consent blockchain sync..."
Start-Sleep -Seconds 10

# Step 4c: Doctor reads document WITH consent -- should succeed
Write-FlowLog "Doctor tries GET /document WITH consent..."
$withConsentResult = ApiJsonSafe -Method 'GET' -Url "$base/api/v1/ehr/records/$testEhrId/document" `
    -Token $doctorToken -Headers @{ 'X-Requester-Id' = $doctorUserId.ToString() }

$docContent4 = $withConsentResult.RawContent
$hasDocContent4 = ($withConsentResult.StatusCode -eq 200) -and (-not [string]::IsNullOrWhiteSpace($docContent4))

Record-Test -Name "Scenario4b: Doctor reads document with consent (200)" -Pass $hasDocContent4 `
    -Detail "StatusCode=$($withConsentResult.StatusCode), ContentLen=$($docContent4.Length)"

# =============================================================================
# SCENARIO 5: Patient self-download document -> success
# =============================================================================

Sep "SCENARIO 5: Patient self-download document"

Write-FlowLog "Patient tries GET /document/self..."
$selfDocResult = ApiJsonSafe -Method 'GET' -Url "$base/api/v1/ehr/records/$testEhrId/document/self" -Token $patientToken

$hasDocContent5 = ($selfDocResult.StatusCode -eq 200) -and (-not [string]::IsNullOrWhiteSpace($selfDocResult.RawContent))

Record-Test -Name "Scenario5: Patient self-download document (200)" -Pass $hasDocContent5 `
    -Detail "StatusCode=$($selfDocResult.StatusCode), ContentLen=$($selfDocResult.RawContent.Length)"

# =============================================================================
# SCENARIO 6: Corrupt IPFS CID in DB -> Tampering Detected
# =============================================================================

Sep "SCENARIO 6: Tamper IPFS CID in database -> Tampering Detected"

# Update the ipfs_cid in ehr_versions table to a fake CID
$fakeCid = 'QmFAKETAMPERED1234567890abcdefghijklmnop'
$sqlUpdate = "UPDATE ehr_versions SET ipfs_cid = '$fakeCid' WHERE ehr_id = '$testEhrId';"
Write-FlowLog "Executing SQL to corrupt IPFS CID for EHR $testEhrId..."
Write-FlowLog "SQL: $sqlUpdate"

$sqlOutput = docker exec dbh_pg_primary psql -U admin -d dbh_ehr -c "$sqlUpdate" 2>&1
Write-FlowLog "SQL output: $($sqlOutput -join ' ')"

# Try to get the document -- should fail with Tampering Detected
Write-FlowLog "Patient tries GET /document/self after CID tampering..."
$tamperResult = ApiJsonSafe -Method 'GET' -Url "$base/api/v1/ehr/records/$testEhrId/document/self" -Token $patientToken

$tamperStatusOk = $tamperResult.StatusCode -eq 500
$tamperMessage = ''
if ($tamperResult.Response) {
    $tamperMessage = if ($tamperResult.Response.message) { $tamperResult.Response.message } else { '' }
}
$tamperDetected = $tamperMessage -match 'Tampering' -or $tamperMessage -match 'thay đổi trái phép'

Record-Test -Name "Scenario6: Tampered CID returns 500" -Pass $tamperStatusOk `
    -Detail "StatusCode=$($tamperResult.StatusCode)"
Record-Test -Name "Scenario6: Response contains 'Tampering Detected' message" -Pass ($tamperStatusOk -and $tamperDetected) `
    -Detail "Message='$tamperMessage'"

# Also test via doctor with consent (same should fail)
Write-FlowLog "Doctor tries GET /document after CID tampering..."
$tamperResult2 = ApiJsonSafe -Method 'GET' -Url "$base/api/v1/ehr/records/$testEhrId/document" `
    -Token $doctorToken -Headers @{ 'X-Requester-Id' = $doctorUserId.ToString() }

$tamperMessage2 = ''
if ($tamperResult2.Response) {
    $tamperMessage2 = if ($tamperResult2.Response.message) { $tamperResult2.Response.message } else { '' }
}
$tamperDetected2 = $tamperMessage2 -match 'Tampering' -or $tamperMessage2 -match 'thay đổi trái phép'

Record-Test -Name "Scenario6: Doctor also gets Tampering Detected" -Pass ($tamperResult2.StatusCode -eq 500 -and $tamperDetected2) `
    -Detail "StatusCode=$($tamperResult2.StatusCode), Message='$tamperMessage2'"

# =============================================================================
# SUMMARY
# =============================================================================

Sep "TEST RESULTS SUMMARY"

$totalTests = $passed + $failed
Write-Host ""
$summColor = if ($failed -eq 0) { 'Green' } else { 'Yellow' }
Write-Host " TOTAL: $totalTests   |   PASS: $passed   |   FAIL: $failed" -ForegroundColor $summColor
Write-Host ""

if ($failed -gt 0) {
    Write-Host "Failed tests:" -ForegroundColor Red
    foreach ($kv in $testResults.GetEnumerator()) {
        if ($kv.Value.Status -eq 'FAIL') {
            Write-Host "  - $($kv.Key): $($kv.Value.Detail)" -ForegroundColor Red
        }
    }
    Write-Host ""
}

Write-Host "All tests:" -ForegroundColor Cyan
foreach ($kv in $testResults.GetEnumerator()) {
    $color = if ($kv.Value.Status -eq 'PASS') { 'Green' } else { 'Red' }
    Write-Host ("  [{0}] {1}" -f $kv.Value.Status, $kv.Key) -ForegroundColor $color
}

# Output result JSON
$resultObj = [ordered]@{
    timestamp               = (Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
    stamp                   = $stamp
    totalTests              = $totalTests
    passed                  = $passed
    failed                  = $failed
    adminToken              = $adminToken
    orgId                   = $orgId.ToString()
    doctorEmail             = $doctorEmail
    doctorUserId            = $doctorUserId.ToString()
    doctorId                = $doctorId.ToString()
    patientEmail            = $patientEmail
    patientUserId           = $patientUserId.ToString()
    patientId               = $patientId.ToString()
    ehr1Id_blockchainOff    = if ($ehr1Id) { $ehr1Id.ToString() } else { $null }
    ehr2Id_blockchainOn     = if ($ehr2Id) { $ehr2Id.ToString() } else { $null }
    ehr3Id_majorityDown     = if ($ehr3Id) { $ehr3Id.ToString() } else { $null }
    tests                   = $testResults
}

$resultObj | ConvertTo-Json -Depth 10 | Tee-Object -FilePath $resultFile

Write-FlowLog "Results written to: $resultFile"
Write-FlowLog "Full log at: $logFile"


