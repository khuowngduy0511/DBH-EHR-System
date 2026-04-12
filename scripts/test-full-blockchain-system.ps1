#!/usr/bin/env powershell
# =============================================================================
# DBH-EHR Full System API Test - All Services + Blockchain APIs
# Gateway: http://localhost:5000
#
# Blockchain APIs marked: [BLOCKCHAIN] [IPFS]
# =============================================================================
$ErrorActionPreference = "Continue"
$BASE   = "http://localhost:5000"
$passed = 0; $failed = 0; $total = 0
$Results = [System.Collections.Generic.List[object]]::new()

function Test {
    param(
        [string]$Name,
        [string]$Method,
        [string]$Url,
        $Body    = $null,
        [string]$Token   = $null,
        [int]   $Expect  = 200,
        [hashtable]$Extra = @{},
        [int[]] $AlsoOk  = @()
    )
    $script:total++
    $label = "[$script:total]"
    Write-Host "$label $Name " -NoNewline
    try {
        $h = @{}
        if ($Token) { $h["Authorization"] = "Bearer $Token" }
        foreach ($k in $Extra.Keys) { $h[$k] = $Extra[$k] }
        $p = @{ Method = $Method; Uri = $Url; Headers = $h; ErrorAction = "Stop" }
        if ($Body) { $p["Body"] = ($Body | ConvertTo-Json -Depth 15); $p["ContentType"] = "application/json" }

        $r    = Invoke-WebRequest @p
        $code = [int]$r.StatusCode
        $obj  = $r.Content | ConvertFrom-Json -ErrorAction SilentlyContinue

        if ($code -eq $Expect -or $AlsoOk -contains $code) {
            Write-Host "[PASS $code]" -ForegroundColor Green
            $script:passed++
            $script:Results.Add([pscustomobject]@{ Test=$Name; Result="PASS"; Code=$code })
            return $obj
        } else {
            Write-Host "[FAIL exp=$Expect got=$code]" -ForegroundColor Red
            $snippet = $r.Content.Substring(0,[Math]::Min(250,$r.Content.Length))
            Write-Host "  Body: $snippet" -ForegroundColor DarkYellow
            $script:failed++
            $script:Results.Add([pscustomobject]@{ Test=$Name; Result="FAIL(got=$code)"; Code=$code })
            return $null
        }
    } catch {
        $es = if ($_.Exception.Response) { [int]$_.Exception.Response.StatusCode } else { 0 }
        $errBody = ""
        try {
            $sr = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $errBody = $sr.ReadToEnd(); $sr.Close()
        } catch {}
        if ($es -eq $Expect -or $AlsoOk -contains $es) {
            Write-Host "[PASS $es]" -ForegroundColor Green
            $script:passed++
            $script:Results.Add([pscustomobject]@{ Test=$Name; Result="PASS($es)"; Code=$es })
            return ($errBody | ConvertFrom-Json -ErrorAction SilentlyContinue)
        }
        Write-Host "[FAIL err=$es]" -ForegroundColor Red
        if ($errBody) { Write-Host "  Body: $($errBody.Substring(0,[Math]::Min(250,$errBody.Length)))" -ForegroundColor DarkYellow }
        $script:failed++
        $script:Results.Add([pscustomobject]@{ Test=$Name; Result="FAIL(err=$es)"; Code=$es })
        return $null
    }
}

function TestUpload {
    param([string]$Name, [string]$Url, [string]$FilePath, [string]$Token, [int]$Expect = 201)
    $script:total++
    $label = "[$script:total]"
    Write-Host "$label $Name " -NoNewline
    $curlOut = & curl.exe -s -o - -w "`n%{http_code}" -X POST "$Url" -H "Authorization: Bearer $Token" -F "file=@$FilePath;type=text/plain" 2>&1
    $lines   = $curlOut -split "`n"
    $code    = [int]($lines[-1].Trim())
    $rawBody = ($lines[0..($lines.Count-2)] -join "`n")
    if ($code -eq $Expect) {
        Write-Host "[PASS $code]" -ForegroundColor Green
        $script:passed++
        $script:Results.Add([pscustomobject]@{ Test=$Name; Result="PASS"; Code=$code })
        return ($rawBody | ConvertFrom-Json -ErrorAction SilentlyContinue)
    } else {
        Write-Host "[FAIL exp=$Expect got=$code]" -ForegroundColor Red
        if ($rawBody) { Write-Host "  Body: $($rawBody.Substring(0,[Math]::Min(200,$rawBody.Length)))" -ForegroundColor DarkYellow }
        $script:failed++
        $script:Results.Add([pscustomobject]@{ Test=$Name; Result="FAIL(got=$code)"; Code=$code })
        return $null
    }
}

function Sep([string]$Title) {
    Write-Host "`n$("="*70)" -ForegroundColor Cyan
    Write-Host "  $Title" -ForegroundColor Cyan
    Write-Host "$("="*70)" -ForegroundColor Cyan
}

function Note([string]$msg) { Write-Host "  >> $msg" -ForegroundColor DarkGray }

# =============================================================================
Sep "1. HEALTH CHECKS"
# =============================================================================
$svcHealth = @(
    @{ name="Gateway";      url="$BASE/health" },
    @{ name="Auth";         url="$BASE/api/v1/auth/me"; expect=401 },
    @{ name="Organization"; url="http://localhost:5002/health" },
    @{ name="EHR";          url="$BASE/api/v1/ehr/records/patient/00000000-0000-0000-0000-000000000000"; expect=401 },
    @{ name="Consent";      url="$BASE/api/v1/consents/search"; expect=401 },
    @{ name="Audit";        url="http://localhost:5005/health" },
    @{ name="Notification"; url="http://localhost:5006/health" },
    @{ name="Appointment";  url="http://localhost:5007/health" }
)
foreach ($s in $svcHealth) {
    if ($s.expect) { Test "Health: $($s.name)" GET $s.url -Expect $s.expect }
    else           { Test "Health: $($s.name)" GET $s.url }
}

# =============================================================================
Sep "2. AUTH SERVICE"
# =============================================================================

$adminLogin = Test "Auth: Login Admin"    POST "$BASE/api/v1/auth/login" @{ email="admin@dbh.vn";        password="Admin@123456" }
$docLogin   = Test "Auth: Login Doctor"   POST "$BASE/api/v1/auth/login" @{ email="dr.hieu@dbh.vn";      password="Doctor@123"   }
$patLogin   = Test "Auth: Login Patient"  POST "$BASE/api/v1/auth/login" @{ email="patient.an@dbh.vn";   password="Patient@123"  }
$pat2Login  = Test "Auth: Login Patient2" POST "$BASE/api/v1/auth/login" @{ email="patient.dung@dbh.vn"; password="Patient@123"  }
$doc2Login  = Test "Auth: Login Doctor2"  POST "$BASE/api/v1/auth/login" @{ email="dr.lan@dbh.vn";       password="Doctor@123"   }

$adminTok = $adminLogin.token
$docTok   = $docLogin.token
$patTok   = $patLogin.token
$pat2Tok  = $pat2Login.token
$doc2Tok  = $doc2Login.token

$adminMe = Test "Auth: GET /me (Admin)"    GET "$BASE/api/v1/auth/me" -Token $adminTok
$docMe   = Test "Auth: GET /me (Doctor)"   GET "$BASE/api/v1/auth/me" -Token $docTok
$patMe   = Test "Auth: GET /me (Patient)"  GET "$BASE/api/v1/auth/me" -Token $patTok
$pat2Me  = Test "Auth: GET /me (Patient2)" GET "$BASE/api/v1/auth/me" -Token $pat2Tok
$doc2Me  = Test "Auth: GET /me (Doctor2)"  GET "$BASE/api/v1/auth/me" -Token $doc2Tok

$adminUserId   = [Guid]$adminMe.userId
$docUserId     = [Guid]$docMe.userId
$docDoctorId   = [Guid]$docMe.profiles.Doctor.doctorId
$patUserId     = [Guid]$patMe.userId
$patPatientId  = [Guid]$patMe.profiles.Patient.patientId
$pat2UserId    = [Guid]$pat2Me.userId
$pat2PatientId = [Guid]$pat2Me.profiles.Patient.patientId
$doc2UserId    = [Guid]$doc2Me.userId
$doc2DoctorId  = [Guid]$doc2Me.profiles.Doctor.doctorId

Note "Admin=$adminUserId | Doc=$docUserId($docDoctorId) | Pat=$patUserId($patPatientId)"
Note "Pat2=$pat2PatientId | Doc2=$doc2DoctorId"

Test "Auth: GET /users/{id}"       GET  "$BASE/api/v1/auth/users/$docUserId" -Token $adminTok
Test "Auth: PUT /me/profile"       PUT  "$BASE/api/v1/auth/me/profile" @{ fullName="Nguyen Van An (test)" } $patTok
Test "Auth: Invalid Login (401)"   POST "$BASE/api/v1/auth/login" @{ email="nobody@x.com"; password="wrong" } -Expect 401
Test "Auth: No Token (401)"        GET  "$BASE/api/v1/auth/me" -Expect 401

$stamp = Get-Date -Format "yyyyMMddHHmmss"
$newPatEmail = "test.auto.$stamp@dbh.vn"
Test "Auth: Register New Patient"  POST "$BASE/api/v1/auth/register" @{
    fullName="AutoTest $stamp"; email=$newPatEmail; password="AutoTest@123"; phone="09$($stamp.Substring(4,8))"
}

if ($patLogin.refreshToken) {
    Test "Auth: Refresh Token" POST "$BASE/api/v1/auth/refresh-token" @{ refreshToken=$patLogin.refreshToken }
}

# Fresh login so revoke always has a valid token (previous Refresh may invalidate old token)
$revokeLogin = Test "Auth: Login (pre-revoke)" POST "$BASE/api/v1/auth/login" @{ email="patient.an@dbh.vn"; password="Patient@123" }
# Use curl for revoke — Invoke-WebRequest in PS5.1 drops the text/plain 200 response
$script:total++
$rvLabel = "[$script:total]"
Write-Host "$rvLabel Auth: Revoke Token " -NoNewline
$rvOut = & curl.exe -s -w "`n%{http_code}" -X POST "$BASE/api/v1/auth/revoke-token" -H "Authorization: Bearer $($revokeLogin.token)" -H "Content-Type: application/json" -d "{}" 2>&1
$rvLines = $rvOut -split "`n"; $rvCode = [int]($rvLines[-1].Trim())
if ($rvCode -eq 200) {
    Write-Host "[PASS $rvCode]" -ForegroundColor Green; $script:passed++
    $script:Results.Add([pscustomobject]@{ Test="Auth: Revoke Token"; Result="PASS"; Code=$rvCode })
} else {
    Write-Host "[FAIL code=$rvCode]" -ForegroundColor Red; $script:failed++
    $script:Results.Add([pscustomobject]@{ Test="Auth: Revoke Token"; Result="FAIL(code=$rvCode)"; Code=$rvCode })
}
$patLogin2 = Test "Auth: Re-Login Patient" POST "$BASE/api/v1/auth/login" @{ email="patient.an@dbh.vn"; password="Patient@123" }
$patTok    = $patLogin2.token

# =============================================================================
Sep "3. ORGANIZATION SERVICE"
# =============================================================================

$orgList   = Test "Org: List All"   GET "$BASE/api/v1/organizations" -Token $adminTok
$allOrgs   = if ($orgList.data) { $orgList.data } else { $orgList }
$hospitalOrg = $allOrgs | Where-Object { $_.orgCode -eq "BVDKTU" } | Select-Object -First 1
if (-not $hospitalOrg) { $hospitalOrg = $allOrgs | Select-Object -First 1 }
$hospitalOrgId = [Guid]$hospitalOrg.orgId
Note "Hospital OrgId=$hospitalOrgId ($($hospitalOrg.orgName))"

Test "Org: Get By ID"          GET  "$BASE/api/v1/organizations/$hospitalOrgId"  -Token $adminTok
Test "Org: Get By ID (401)"    GET  "$BASE/api/v1/organizations/$hospitalOrgId"  -Expect 401

$depts    = Test "Dept: List by Org" GET "$BASE/api/v1/departments/by-organization/$hospitalOrgId" -Token $adminTok
$deptList = if ($depts.data) { $depts.data } else { $depts }
$firstDeptId = if ($deptList -and $deptList.Count -gt 0) { [Guid]$deptList[0].departmentId } else { $null }
if ($firstDeptId) {
    Test "Dept: Get By ID" GET "$BASE/api/v1/departments/$firstDeptId" -Token $adminTok
}

$membs    = Test "Memb: List by Org" GET "$BASE/api/v1/memberships/by-organization/$hospitalOrgId" -Token $adminTok
$membList = if ($membs.data) { $membs.data } else { $membs }
$firstMembId = if ($membList -and $membList.Count -gt 0) { [Guid]$membList[0].membershipId } else { $null }
if ($firstMembId) {
    Test "Memb: Get By ID" GET "$BASE/api/v1/memberships/$firstMembId" -Token $adminTok
}
Test "Memb: By User"           GET  "$BASE/api/v1/memberships/by-user/$docUserId"         -Token $docTok
Test "Memb: Search Doctors"    POST "$BASE/api/v1/memberships/doctors/search" @{ orgId="$hospitalOrgId" } $adminTok

# =============================================================================
Sep "4. EHR SERVICE - Create/Update  [IPFS+BLOCKCHAIN]"
# =============================================================================

Write-Host "`n  [BLOCKCHAIN] Create EHR -> save IPFS + write blockchain audit" -ForegroundColor Yellow
$ehrBody = @{
    patientId = "$patPatientId"
    orgId     = "$hospitalOrgId"
    data      = @{
        resourceType = "Bundle"
        type         = "document"
        entry        = @(
            @{ resource = @{ resourceType = "Condition";   code = @{ text="Hypertension - Blockchain Test" } } },
            @{ resource = @{ resourceType = "Observation"; code = @{ text="Blood Pressure" };
                             valueQuantity = @{ value=150; unit="mmHg" } } },
            @{ resource = @{ resourceType = "MedicationRequest"; medicationCodeableConcept = @{ text="Amlodipine 5mg" } } }
        )
    }
}
$ehrResult   = Test "EHR: Create Record [IPFS+BLOCKCHAIN]" POST "$BASE/api/v1/ehr/records" $ehrBody $docTok -Expect 201
$testEhrId   = if ($ehrResult.ehrId)     { [Guid]$ehrResult.ehrId }     else { $null }
$testVerId   = if ($ehrResult.versionId) { [Guid]$ehrResult.versionId } else { $null }
$testCid     = $ehrResult.ipfsCid
Note "EhrId=$testEhrId | VersionId=$testVerId | CID=$testCid"

Test "EHR: Get Record"             GET "$BASE/api/v1/ehr/records/$testEhrId"                      -Token $docTok
Test "EHR: Get Record (404)"       GET "$BASE/api/v1/ehr/records/$([guid]::NewGuid())"             -Token $docTok -Expect 404
Test "EHR: Patient Records"        GET "$BASE/api/v1/ehr/records/patient/$patPatientId"            -Token $docTok
Test "EHR: Org Records"            GET "$BASE/api/v1/ehr/records/org/$hospitalOrgId"               -Token $docTok

Write-Host "`n  [BLOCKCHAIN] Update EHR -> new version on IPFS" -ForegroundColor Yellow
$updateBody  = @{
    data = @{
        resourceType = "Bundle"
        type         = "document"
        entry        = @(
            @{ resource = @{ resourceType = "Condition";   code = @{ text="Hypertension - Stable" } } },
            @{ resource = @{ resourceType = "Observation"; code = @{ text="BP (updated)" };
                             valueQuantity = @{ value=125; unit="mmHg" } } }
        )
    }
}
$updateResult = Test "EHR: Update Record v2 [IPFS+BLOCKCHAIN]" PUT "$BASE/api/v1/ehr/records/$testEhrId" $updateBody $docTok
Note "Updated CID=$($updateResult.ipfsCid)"

$versions = Test "EHR: List Versions"       GET "$BASE/api/v1/ehr/records/$testEhrId/versions"          -Token $docTok
Note "Total versions: $($versions.Count)"
Test "EHR: Get Version Detail"              GET "$BASE/api/v1/ehr/records/$testEhrId/versions/$testVerId" -Token $docTok
Test "EHR: List Files"                      GET "$BASE/api/v1/ehr/records/$testEhrId/files"              -Token $docTok

$tmpFile = "$env:TEMP\ehr-test-$stamp.txt"
Set-Content -Path $tmpFile -Value "Lab CBC result: WBC=6.5 RBC=4.8 - AutoTest $stamp"
$uploadedFile   = TestUpload "EHR: Upload File (multipart)" "$BASE/api/v1/ehr/records/$testEhrId/files" $tmpFile $docTok -Expect 201
$uploadedFileId = if ($uploadedFile.fileId) { [Guid]$uploadedFile.fileId } else { $null }
Remove-Item $tmpFile -ErrorAction SilentlyContinue
Note "UploadedFileId=$uploadedFileId"
if ($uploadedFileId) {
    Test "EHR: Delete File" DELETE "$BASE/api/v1/ehr/records/$testEhrId/files/$uploadedFileId" -Token $docTok -Expect 204
}

# =============================================================================
Sep "4b. EHR IPFS - Raw Upload/Download  [IPFS]"
# =============================================================================

Write-Host "`n  [IPFS] Download raw encrypted payload from IPFS (by ehrId)" -ForegroundColor Yellow
$ipfsRaw = Test "EHR: Download IPFS Raw (by ehrId) [IPFS]" GET "$BASE/api/v1/ehr/records/$testEhrId/ipfs/download" -Token $docTok
Note "CID=$($ipfsRaw.ipfsCid) | EncryptedLen=$($ipfsRaw.encryptedData.Length)"

if ($testCid) {
    Write-Host "`n  [IPFS] Download raw by CID directly" -ForegroundColor Yellow
    $ipfsByCid = Test "EHR: Download IPFS by CID [IPFS]" GET "$BASE/api/v1/ehr/ipfs/$testCid/download" -Token $docTok
    Note "EncryptedLen=$($ipfsByCid.encryptedData.Length)"
}

Write-Host "`n  [IPFS] Encrypt payload and upload to IPFS" -ForegroundColor Yellow
$encPayload = @{ resourceType="Observation"; code=@{text="IPFS Encrypt Test $stamp"}; valueString="test-value" }
$encryptBody = @{ Data = ($encPayload | ConvertTo-Json -Depth 5 -Compress) }
$encryptResult = Test "EHR: Encrypt to IPFS [IPFS]" POST "$BASE/api/v1/ehr/ipfs/encrypt" $encryptBody $docTok
$encryptCid = $encryptResult.ipfsCid
Note "Encrypted CID=$encryptCid"

if ($encryptCid) {
    Write-Host "`n  [IPFS] Decrypt payload from IPFS" -ForegroundColor Yellow
    $decryptBody = @{ IpfsCid=$encryptCid; WrappedAesKey=$encryptResult.wrappedAesKey }
    $decryptResult = Test "EHR: Decrypt from IPFS [IPFS]" POST "$BASE/api/v1/ehr/ipfs/decrypt" $decryptBody $docTok
    Note "Decrypted len=$($decryptResult.data.Length)"
}

# =============================================================================
Sep "5. CONSENT SERVICE  [BLOCKCHAIN]"
# =============================================================================
Write-Host "`n  [BLOCKCHAIN] Consent = write to Hyperledger Fabric chaincode" -ForegroundColor Yellow

$consentBody = @{
    patientId    = "$pat2PatientId"
    patientDid   = "did:dbh:patient:$pat2UserId"
    granteeId    = "$doc2DoctorId"
    granteeDid   = "did:dbh:doctor:$doc2UserId"
    granteeType  = "DOCTOR"
    permission   = "READ"
    purpose      = "TREATMENT"
    durationDays = 30
}
$consentResult = Test "Consent: Grant (Patient2->Doctor2) [BLOCKCHAIN]" POST "$BASE/api/v1/consents" $consentBody $pat2Tok -Expect 201
$consentId = if ($consentResult.data.consentId) { [Guid]$consentResult.data.consentId }
             elseif ($consentResult.consentId)  { [Guid]$consentResult.consentId }
             else { $null }
$bcConsentId = if ($consentResult.data.blockchainConsentId) { $consentResult.data.blockchainConsentId }
               elseif ($consentResult.blockchainConsentId)  { $consentResult.blockchainConsentId }
               else { $null }
Note "ConsentId=$consentId | BlockchainId=$bcConsentId"

if ($consentId) {
    Test "Consent: Get By ID"           GET "$BASE/api/v1/consents/$consentId"             -Token $pat2Tok
}
Test "Consent: By Patient"              GET "$BASE/api/v1/consents/by-patient/$pat2PatientId" -Token $pat2Tok
Test "Consent: By Grantee (Doctor2)"    GET "$BASE/api/v1/consents/by-grantee/$doc2DoctorId"  -Token $doc2Tok
Test "Consent: Search (Admin)"          GET "$BASE/api/v1/consents/search"                    -Token $adminTok

Write-Host "`n  [BLOCKCHAIN] Verify consent -> query Hyperledger Fabric" -ForegroundColor Yellow
$verifyBody = @{ patientId="$pat2PatientId"; granteeId="$doc2DoctorId"; granteeType="DOCTOR"; permission="READ" }
Test "Consent: Verify (Doctor2 has access) [BLOCKCHAIN]" POST "$BASE/api/v1/consents/verify" $verifyBody $doc2Tok

if ($bcConsentId) {
    Write-Host "`n  [BLOCKCHAIN] Sync consent from Hyperledger Fabric" -ForegroundColor Yellow
    Test "Consent: Sync from Blockchain [BLOCKCHAIN]" POST "$BASE/api/v1/consents/sync/$bcConsentId" -Token $adminTok
}

# =============================================================================
Sep "6. EHR DOCUMENT WITH CONSENT CHECK  [BLOCKCHAIN+IPFS]"
# =============================================================================
Write-Host "`n  Waiting 6s for async blockchain AES-key commits (Create + Update) ..." -ForegroundColor Yellow
Start-Sleep -Seconds 6
Write-Host "  [BLOCKCHAIN+IPFS] GET document -> verify consent on blockchain -> decrypt from IPFS" -ForegroundColor Yellow

$consentForDocBody = @{
    patientId    = "$patPatientId"
    patientDid   = "did:dbh:patient:$patUserId"
    granteeId    = "$docDoctorId"
    granteeDid   = "did:dbh:doctor:$docUserId"
    granteeType  = "DOCTOR"
    permission   = "READ"
    purpose      = "TREATMENT"
    durationDays = 90
    ehrId        = "$testEhrId"
}
$consentForDoc   = Test "Consent: Grant Patient->Doctor (for EHR doc) [BLOCKCHAIN]" POST "$BASE/api/v1/consents" $consentForDocBody $patTok -Expect 201 -AlsoOk @(400)
$consentForDocId = if ($consentForDoc -and $consentForDoc.data.consentId) { [Guid]$consentForDoc.data.consentId }
                   elseif ($consentForDoc -and $consentForDoc.consentId)  { [Guid]$consentForDoc.consentId }
                   else { $null }
# If grant returned null (e.g. 400 already exists), look up the existing active consent
if (-not $consentForDocId) {
    Write-Host "  [INFO] Consent may already exist - looking up existing active consent..." -ForegroundColor Cyan
    $existingConsents = Invoke-WebRequest -Method GET -Uri "$BASE/api/v1/consents/by-patient/$patPatientId" `
        -Headers @{ "Authorization" = "Bearer $patTok" } -ErrorAction SilentlyContinue
    if ($existingConsents) {
        $existingObj = $existingConsents.Content | ConvertFrom-Json -ErrorAction SilentlyContinue
        $items = if ($existingObj.data) { $existingObj.data } elseif ($existingObj -is [array]) { $existingObj } else { @() }
        $match = $items | Where-Object { $_.granteeId -eq "$docDoctorId" -and $_.status -eq "ACTIVE" } | Select-Object -First 1
        if ($match) { $consentForDocId = [Guid]$match.consentId; Write-Host "  [INFO] Found existing consent $consentForDocId" -ForegroundColor Cyan }
    }
}
Note "ConsentForDocId=$consentForDocId"

Write-Host "`n  [BLOCKCHAIN+IPFS] GET /document with X-Requester-Id" -ForegroundColor Yellow
$docContent = Test "EHR: GET /document (X-Requester-Id) [BLOCKCHAIN+IPFS]" `
    GET "$BASE/api/v1/ehr/records/$testEhrId/document" `
    -Token $docTok `
    -Extra @{ "X-Requester-Id" = "$docDoctorId" }
if ($docContent) { Note "Document len=$($docContent.ToString().Length)" } else { Note "Document: (no content returned)" }

Write-Host "`n  [BLOCKCHAIN+IPFS] GET /document/self - patient reads own EHR" -ForegroundColor Yellow
Test "EHR: GET /document/self (Patient) [BLOCKCHAIN+IPFS]" `
    GET "$BASE/api/v1/ehr/records/$testEhrId/document/self" -Token $patTok

Test "EHR: GET /document (no X-Requester-Id -> 400)" `
    GET "$BASE/api/v1/ehr/records/$testEhrId/document" -Token $docTok -Expect 400

# Doc2 (dr.lan) has no consent for patient.an's EHR -> should return 403
$ehrBod2 = @{
    patientId = "$pat2PatientId"
    orgId     = "$hospitalOrgId"
    data      = @{ resourceType="Bundle"; type="document"; entry=@(@{ resource=@{ resourceType="Condition"; code=@{text="Test consent block"} } }) }
}
$ehr2 = Test "EHR: Create Record (Patient2) [IPFS]" POST "$BASE/api/v1/ehr/records" $ehrBod2 $docTok -Expect 201
$ehr2Id = if ($ehr2.ehrId) { [Guid]$ehr2.ehrId } else { $null }

Write-Host "`n  [BLOCKCHAIN] doc2 accesses patient.an EHR without consent -> 403 (or 200 if stale consent from prev run)" -ForegroundColor Yellow
# Revoke any existing (stale) consent from patient.an -> doc2 before this test
# NOTE: consents are stored by userId, so query by patUserId (not patPatientId)
foreach ($lookupId in @("$patUserId", "$patPatientId")) {
    $staleConsents = Invoke-WebRequest -Method GET -Uri "$BASE/api/v1/consents/by-patient/$lookupId" `
        -Headers @{ "Authorization" = "Bearer $patTok" } -ErrorAction SilentlyContinue
    if ($staleConsents) {
        $staleObj  = $staleConsents.Content | ConvertFrom-Json -ErrorAction SilentlyContinue
        $staleList = if ($staleObj.data) { $staleObj.data } elseif ($staleObj -is [array]) { $staleObj } else { @() }
        $existing  = $staleList | Where-Object { ($_.granteeId -eq "$doc2DoctorId" -or $_.granteeId -eq "$doc2UserId") -and $_.status -eq "ACTIVE" }
        foreach ($sc in $existing) {
            Invoke-WebRequest -Method POST -Uri "$BASE/api/v1/consents/$($sc.consentId)/revoke" `
                -ContentType "application/json" -Body '{"revokeReason":"cleanup stale test consent"}' `
                -Headers @{ "Authorization" = "Bearer $patTok" } -ErrorAction SilentlyContinue | Out-Null
            Write-Host "  [CLEANUP] Revoked stale consent $($sc.consentId) grantee=$($sc.granteeId) (pat->doc2)" -ForegroundColor DarkGray
        }
    }
}
Test "EHR: GET /document (no consent -> 403) [BLOCKCHAIN]" `
    GET "$BASE/api/v1/ehr/records/$testEhrId/document" `
    -Token $doc2Tok `
    -Extra @{ "X-Requester-Id" = "$doc2DoctorId" } -Expect 403

# =============================================================================
Sep "7. CONSENT REVOKE  [BLOCKCHAIN]"
# =============================================================================
Write-Host "`n  [BLOCKCHAIN] Revoke -> update status on Hyperledger Fabric" -ForegroundColor Yellow
if ($consentId) {
    Test "Consent: Revoke (Patient2 revokes Doctor2) [BLOCKCHAIN]" `
        POST "$BASE/api/v1/consents/$consentId/revoke" `
        @{ revokeReason = "Test revoke $stamp" } `
        $pat2Tok

    Test "Consent: Verify after Revoke (should deny) [BLOCKCHAIN]" `
        POST "$BASE/api/v1/consents/verify" `
        @{ patientId="$pat2PatientId"; granteeId="$doc2DoctorId"; granteeType="DOCTOR"; permission="READ" } `
        $doc2Tok
}

# =============================================================================
Sep "8. AUDIT SERVICE  [BLOCKCHAIN]"
# =============================================================================
Write-Host "`n  [BLOCKCHAIN] Create audit log -> write to Hyperledger Fabric" -ForegroundColor Yellow
$auditBody = @{
    actorDid       = "did:dbh:doctor:$docUserId"
    actorUserId    = "$docUserId"
    actorType      = "DOCTOR"
    action         = "VIEW"
    targetType     = "EHR"
    targetId       = "$testEhrId"
    result         = "SUCCESS"
    patientDid     = "did:dbh:patient:$patUserId"
    patientId      = "$patPatientId"
    organizationId = "$hospitalOrgId"
}
$auditResult   = Test "Audit: Create Log [BLOCKCHAIN]" POST "$BASE/api/v1/audit" $auditBody $adminTok
$auditId       = if ($auditResult.data.auditLogId)      { [Guid]$auditResult.data.auditLogId }
                 elseif ($auditResult.auditLogId)        { [Guid]$auditResult.auditLogId }
                 else { $null }
$bcAuditId     = if ($auditResult.data.blockchainAuditId) { $auditResult.data.blockchainAuditId }
                 elseif ($auditResult.blockchainAuditId)  { $auditResult.blockchainAuditId }
                 else { $null }
Note "AuditId=$auditId | BlockchainId=$bcAuditId"

if ($auditId) {
    Test "Audit: Get By ID"         GET "$BASE/api/v1/audit/$auditId"                  -Token $adminTok
}
Test "Audit: Search"                GET "$BASE/api/v1/audit/search"                    -Token $adminTok
Test "Audit: By Patient"            GET "$BASE/api/v1/audit/by-patient/$patPatientId"  -Token $adminTok
Test "Audit: By Actor (Doctor)"     GET "$BASE/api/v1/audit/by-actor/$docUserId"        -Token $docTok
Test "Audit: Stats (Admin)"         GET "$BASE/api/v1/audit/stats"                     -Token $adminTok
Test "Audit: By Target (EHR)"       GET "$BASE/api/v1/audit/by-target/$testEhrId`?targetType=EHR" -Token $docTok

if ($bcAuditId) {
    Write-Host "`n  [BLOCKCHAIN] Sync audit from Hyperledger Fabric" -ForegroundColor Yellow
    Test "Audit: Sync from Blockchain [BLOCKCHAIN]" POST "$BASE/api/v1/audit/sync/$bcAuditId" -Token $adminTok
}

# =============================================================================
Sep "9. APPOINTMENT SERVICE"
# =============================================================================

$aptDate  = (Get-Date).AddDays(7).AddHours(9).ToString("yyyy-MM-ddTHH:mm:ssZ")
$aptBody  = @{ patientId="$patPatientId"; doctorId="$docDoctorId"; orgId="$hospitalOrgId"; scheduledAt=$aptDate; note="AutoTest $stamp" }
$aptResult = Test "Apt: Create" POST "$BASE/api/v1/appointments" $aptBody $patTok -Expect 201
$aptId    = if ($aptResult.data.appointmentId) { [Guid]$aptResult.data.appointmentId }
            elseif ($aptResult.appointmentId)  { [Guid]$aptResult.appointmentId }
            else { $null }
Note "AppointmentId=$aptId"

if ($aptId) {
    Test "Apt: Get By ID"          GET "$BASE/api/v1/appointments/$aptId"          -Token $patTok
    Test "Apt: Confirm (Doctor)"   PUT "$BASE/api/v1/appointments/$aptId/confirm"  -Token $docTok
    Test "Apt: Check-In"           PUT "$BASE/api/v1/appointments/$aptId/check-in" -Token $patTok
}
Test "Apt: List by Patient"        GET "$BASE/api/v1/appointments?patientId=$patPatientId" -Token $patTok
Test "Apt: List by Doctor"         GET "$BASE/api/v1/appointments?doctorId=$docDoctorId"   -Token $docTok
Test "Apt: Search Doctors"         GET "$BASE/api/v1/appointments/doctors/search?orgId=$hospitalOrgId" -Token $patTok

$aptDate2  = (Get-Date).AddDays(14).AddHours(14).ToString("yyyy-MM-ddTHH:mm:ssZ")
$apt2      = Test "Apt: Create (reject test)" POST "$BASE/api/v1/appointments" @{
    patientId="$patPatientId"; doctorId="$docDoctorId"; orgId="$hospitalOrgId"; scheduledAt=$aptDate2
} $patTok -Expect 201
$apt2Id = if ($apt2.data.appointmentId) { [Guid]$apt2.data.appointmentId }
          elseif ($apt2.appointmentId)  { [Guid]$apt2.appointmentId }
          else { $null }
if ($apt2Id) {
    Test "Apt: Reject (Doctor)" PUT "$BASE/api/v1/appointments/$apt2Id/reject" @{ reason="Test reject" } $docTok
}

$aptDate3  = (Get-Date).AddDays(21).ToString("yyyy-MM-ddTHH:mm:ssZ")
$apt3      = Test "Apt: Create (cancel test)" POST "$BASE/api/v1/appointments" @{
    patientId="$patPatientId"; doctorId="$docDoctorId"; orgId="$hospitalOrgId"; scheduledAt=$aptDate3
} $patTok -Expect 201
$apt3Id = if ($apt3.data.appointmentId) { [Guid]$apt3.data.appointmentId }
          elseif ($apt3.appointmentId)  { [Guid]$apt3.appointmentId }
          else { $null }
if ($apt3Id) {
    Test "Apt: Cancel"           PUT "$BASE/api/v1/appointments/$apt3Id/cancel"    @{ reason="Test cancel" } $patTok
    Test "Apt: Get (cancelled)"  GET "$BASE/api/v1/appointments/$apt3Id"           -Token $patTok
}

# =============================================================================
Sep "10. NOTIFICATION SERVICE"
# =============================================================================

$patDid     = "did:dbh:patient:$patUserId"
$notifBody  = @{
    recipientDid    = $patDid
    recipientUserId = "$patUserId"
    title           = "Blockchain Test Notification $stamp"
    body            = "System notification test - blockchain EHR"
    type            = "System"
    priority        = "Normal"
    channel         = "InApp"
}
$notifResult = Test "Notif: Send"              POST "$BASE/api/v1/notifications" $notifBody $adminTok
$notifId = if ($notifResult.data.notificationId) { [Guid]$notifResult.data.notificationId }
           elseif ($notifResult.notificationId)  { [Guid]$notifResult.notificationId }
           else { $null }
Note "NotificationId=$notifId"

Test "Notif: List by User"                     GET  "$BASE/api/v1/notifications/by-user/$patDid"               -Token $patTok
Test "Notif: Unread"                           GET  "$BASE/api/v1/notifications/by-user/$patDid/unread"        -Token $patTok
Test "Notif: Unread Count"                     GET  "$BASE/api/v1/notifications/by-user/$patDid/unread-count"  -Token $patTok
Test "Notif: Mark All Read"                    POST "$BASE/api/v1/notifications/by-user/$patDid/mark-all-read" -Token $patTok

if ($notifId) {
    Test "Notif: Mark Read (single)"           POST "$BASE/api/v1/notifications/by-user/$patDid/mark-read" @{ notificationIds=@("$notifId") } $patTok
    Test "Notif: Delete"                       DELETE "$BASE/api/v1/notifications/$notifId" -Token $patTok
}

Test "Notif: Broadcast (Admin)"                POST "$BASE/api/v1/notifications/broadcast" @{
    title="Broadcast $stamp"; body="System broadcast test"; type="System"; priority="Low"; channel="InApp"
} $adminTok

# =============================================================================
Sep "11. GATEWAY ROUTING SMOKE TEST"
# =============================================================================

Test "GW: Auth -> /me"              GET "$BASE/api/v1/auth/me"                                  -Token $adminTok
Test "GW: Org -> /organizations"    GET "$BASE/api/v1/organizations"                            -Token $adminTok
Test "GW: EHR -> /records/patient"  GET "$BASE/api/v1/ehr/records/patient/$patPatientId"        -Token $docTok
Test "GW: Consent -> /by-patient"   GET "$BASE/api/v1/consents/by-patient/$patPatientId"        -Token $patTok
Test "GW: Audit -> /search"         GET "$BASE/api/v1/audit/search"                             -Token $adminTok
Test "GW: Apt -> /appointments"     GET "$BASE/api/v1/appointments?patientId=$patPatientId"     -Token $patTok
Test "GW: Notif -> /by-user"        GET "$BASE/api/v1/notifications/by-user/$patDid"            -Token $patTok

# =============================================================================
# SUMMARY
# =============================================================================
Write-Host "`n$("="*70)" -ForegroundColor Cyan
$summColor = if ($failed -eq 0) { "Green" } else { "Yellow" }
Write-Host " TOTAL: $total   |   PASS: $passed   |   FAIL: $failed" -ForegroundColor $summColor
Write-Host "$("="*70)" -ForegroundColor Cyan

if ($failed -gt 0) {
    Write-Host "`nFailed tests:" -ForegroundColor Red
    $Results | Where-Object { $_.Result -notlike "PASS*" } | ForEach-Object {
        Write-Host "  - $($_.Test)  [$($_.Result)]" -ForegroundColor Red
    }
}

Write-Host "`nBlockchain/IPFS APIs tested:" -ForegroundColor Cyan
$Results | Where-Object { $_.Test -like "*BLOCKCHAIN*" -or $_.Test -like "*IPFS*" } | ForEach-Object {
    $color = if ($_.Result -like "PASS*") { "Green" } else { "Red" }
    Write-Host "  $($_.Result.PadRight(12)) $($_.Test)" -ForegroundColor $color
}