param(
    [string]$BaseUrl = 'http://localhost:5000',
    [string]$DoctorEmail = 'doctor.20260407223316@dbh.vn',
    [string]$DoctorPassword = 'Doctor@123',
    [string]$PatientEmail = 'patient.20260407234050@dbh.vn',
    [string]$PatientPassword = 'Patient@123'
)

$ErrorActionPreference = 'Stop'

$logFile = Join-Path $PSScriptRoot 'real-flow-existing-accounts-api.log'
$resultFile = Join-Path $PSScriptRoot '..\real-flow-existing-accounts-result.json'

if (Test-Path $logFile) {
    Remove-Item $logFile -Force
}

function Write-FlowLog {
    param(
        [string]$Message
    )

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

    $h = @{'Content-Type'='application/json'}
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

Write-FlowLog ("Running flow with existing accounts")
Write-FlowLog ("DoctorEmail={0}; PatientEmail={1}" -f $DoctorEmail, $PatientEmail)

$flowTimestamp = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ss.fffZ')

$adminLogin = ApiJson -Method 'POST' -Url "$BaseUrl/api/v1/auth/login" -Body @{email='admin@dbh.vn';password='Admin@123456'}
$adminToken = $adminLogin.token

$orgs = ApiJson -Method 'GET' -Url "$BaseUrl/api/v1/organizations?page=1&pageSize=50" -Token $adminToken
$orgId = [Guid]$orgs.data[0].orgId

$patientLogin = ApiJson -Method 'POST' -Url "$BaseUrl/api/v1/auth/login" -Body @{email=$PatientEmail;password=$PatientPassword}
$patientToken = $patientLogin.token
$patientMe = ApiJson -Method 'GET' -Url "$BaseUrl/api/v1/auth/me" -Token $patientToken
$patientUserId = [Guid]$patientMe.userId
$patientId = [Guid]$patientMe.profiles.Patient.patientId

$doctorLogin = ApiJson -Method 'POST' -Url "$BaseUrl/api/v1/auth/login" -Body @{email=$DoctorEmail;password=$DoctorPassword}
$doctorToken = $doctorLogin.token
$doctorMe = ApiJson -Method 'GET' -Url "$BaseUrl/api/v1/auth/me" -Token $doctorToken
$doctorUserId = [Guid]$doctorMe.userId
$doctorId = [Guid]$doctorMe.profiles.Doctor.doctorId

$patientResolved = ApiJson -Method 'GET' -Url "$BaseUrl/api/v1/auth/user-id?patientId=$patientId" -Token $adminToken
$patientResolvedUserId = [Guid]$patientResolved.userId
$patientKeys = ApiJson -Method 'GET' -Url "$BaseUrl/api/v1/auth/$patientResolvedUserId/keys" -Token $adminToken

if ([string]::IsNullOrWhiteSpace($patientKeys.publicKey) -or [string]::IsNullOrWhiteSpace($patientKeys.encryptedPrivateKey)) {
    throw "Patient auth keys are missing for user $patientResolvedUserId."
}

$ehrPayload = @{
    patientId = $patientId
    orgId = $orgId
    data = @{
        resourceType='Bundle'
        type='document'
        entry=@(
            @{ resource = @{ resourceType='Condition'; code=@{text="Follow-up treatment required [$flowTimestamp]"}; clinicalStatus=@{coding=@(@{code='active'})} } }
        )
    }
}

$ehr = ApiJson -Method 'POST' -Url "$BaseUrl/api/v1/ehr/records" -Token $doctorToken -Headers @{'X-Doctor-Id'=$doctorId.ToString()} -Body $ehrPayload
$ehrId = [Guid]$ehr.ehrId
Write-FlowLog ("EHR result summary: ehrId={0}; ipfsCid={1}; dataHash={2}; fileId={3}; versionId={4}" -f $ehr.ehrId, $ehr.ipfsCid, $ehr.dataHash, $ehr.fileId, $ehr.versionId)

$beforeStatus = ''
try {
    $null = ApiJson -Method 'GET' -Url "$BaseUrl/api/v1/ehr/records/$ehrId" -Token $doctorToken -Headers @{'X-Requester-Id'=$doctorUserId.ToString()}
    $beforeStatus = 'unexpected-success'
}
catch {
    $beforeStatus = $_.Exception.Response.StatusCode.value__
}

$beforeDocumentStatus = ''
try {
    $null = ApiJson -Method 'GET' -Url "$BaseUrl/api/v1/ehr/records/$ehrId/document" -Token $doctorToken -Headers @{'X-Requester-Id'=$doctorUserId.ToString()}
    $beforeDocumentStatus = 'unexpected-success'
}
catch {
    $beforeDocumentStatus = $_.Exception.Response.StatusCode.value__
}

$req = ApiJson -Method 'POST' -Url "$BaseUrl/api/v1/access-requests" -Token $doctorToken -Body @{
    patientId=$patientId; patientDid="did:fabric:patient:$patientId";
    requesterId=$doctorUserId; requesterDid="did:fabric:doctor:$doctorUserId"; requesterType='DOCTOR';
    ehrId=$ehrId; permission='READ'; purpose='TREATMENT'; reason='Need EHR access for treatment'
}
$requestId = [Guid]$req.data.requestId

$null = ApiJson -Method 'POST' -Url "$BaseUrl/api/v1/access-requests/$requestId/respond" -Token $patientToken -Body @{approve=$true; responseReason='Approved for treatment'}

$afterOk = $false
try {
    $after = ApiJson -Method 'GET' -Url "$BaseUrl/api/v1/ehr/records/$ehrId" -Token $doctorToken -Headers @{'X-Requester-Id'=$doctorUserId.ToString()}
    $afterOk = [bool]$after.ehrId
}
catch {
    $afterOk = $false
}

$afterDocumentSuccess = $false
$afterDocumentStatus = ''
try {
    $afterDocument = ApiJson -Method 'GET' -Url "$BaseUrl/api/v1/ehr/records/$ehrId/document" -Token $doctorToken -Headers @{'X-Requester-Id'=$doctorUserId.ToString()}
    $afterDocumentSuccess = $null -ne $afterDocument
    $afterDocumentStatus = 200
}
catch {
    $afterDocumentSuccess = $false
    $afterDocumentStatus = $_.Exception.Response.StatusCode.value__
}

$result = [ordered]@{
    orgId = $orgId
    doctorEmail = $DoctorEmail
    doctorUserId = $doctorUserId
    doctorId = $doctorId
    patientEmail = $PatientEmail
    patientUserId = $patientUserId
    patientId = $patientId
    patientResolvedUserId = $patientResolvedUserId
    ehrId = $ehrId
    requestId = $requestId
    viewBeforeConsentStatus = $beforeStatus
    documentBeforeConsentStatus = $beforeDocumentStatus
    viewAfterConsentSuccess = $afterOk
    documentAfterConsentStatus = $afterDocumentStatus
    documentAfterConsentSuccess = $afterDocumentSuccess
}

$result | ConvertTo-Json -Depth 10 | Tee-Object -FilePath $resultFile