$ErrorActionPreference = 'Stop'

$base = 'http://localhost:5000'
$logFile = Join-Path $PSScriptRoot 'real-flow-api.log'

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
        $requestBodyText = $null
        if ($null -ne $Body) {
            $json = $Body | ConvertTo-Json -Depth 30
            $requestBodyText = $json
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

$stamp = Get-Date -Format 'yyyyMMddHHmmss'

$adminLogin = ApiJson -Method 'POST' -Url "$base/api/v1/auth/login" -Body @{email='admin@dbh.vn';password='Admin@123456'}
$adminToken = $adminLogin.token

$orgs = ApiJson -Method 'GET' -Url "$base/api/v1/organizations?page=1&pageSize=50" -Token $adminToken
$orgId = [Guid]$orgs.data[0].orgId

$receptionEmail = "reception.$stamp@dbh.vn"
$doctorEmail = "doctor.$stamp@dbh.vn"
$patientEmail = "patient.$stamp@dbh.vn"

$null = ApiJson -Method 'POST' -Url "$base/api/v1/auth/registerStaffDoctor" -Token $adminToken -Body @{
    fullName='Reception Flow'; email=$receptionEmail; password='Reception@123';
    phone='0919' + $stamp.Substring(8,6); organizationId=$orgId; role='Receptionist'
}

$null = ApiJson -Method 'POST' -Url "$base/api/v1/auth/registerStaffDoctor" -Token $adminToken -Body @{
    fullName='Doctor Flow'; email=$doctorEmail; password='Doctor@123';
    phone='0929' + $stamp.Substring(8,6); organizationId=$orgId; role='Doctor'
}

$receptionLogin = ApiJson -Method 'POST' -Url "$base/api/v1/auth/login" -Body @{email=$receptionEmail;password='Reception@123'}
$receptionToken = $receptionLogin.token
$receptionMe = ApiJson -Method 'GET' -Url "$base/api/v1/auth/me" -Token $receptionToken
$receptionUserId = [Guid]$receptionMe.userId

$null = ApiJson -Method 'POST' -Url "$base/api/v1/auth/register" -Token $receptionToken -Body @{
    fullName='Patient Flow'; email=$patientEmail; password='Patient@123'; phone='0939' + $stamp.Substring(8,6)
}

$patientLogin = ApiJson -Method 'POST' -Url "$base/api/v1/auth/login" -Body @{email=$patientEmail;password='Patient@123'}
$patientToken = $patientLogin.token
$patientMe = ApiJson -Method 'GET' -Url "$base/api/v1/auth/me" -Token $patientToken
$patientUserId = [Guid]$patientMe.userId
$patientId = [Guid]$patientMe.profiles.Patient.patientId

$doctorLogin = ApiJson -Method 'POST' -Url "$base/api/v1/auth/login" -Body @{email=$doctorEmail;password='Doctor@123'}
$doctorToken = $doctorLogin.token
$doctorMe = ApiJson -Method 'GET' -Url "$base/api/v1/auth/me" -Token $doctorToken
$doctorUserId = [Guid]$doctorMe.userId
$doctorId = [Guid]$doctorMe.profiles.Doctor.doctorId

$patientResolved = ApiJson -Method 'GET' -Url "$base/api/v1/auth/user-id?patientId=$patientId" -Token $adminToken
$patientResolvedUserId = [Guid]$patientResolved.userId
$patientKeys = ApiJson -Method 'GET' -Url "$base/api/v1/auth/$patientResolvedUserId/keys" -Token $adminToken

if ([string]::IsNullOrWhiteSpace($patientKeys.publicKey) -or [string]::IsNullOrWhiteSpace($patientKeys.encryptedPrivateKey)) {
    throw "Patient auth keys are missing for user $patientResolvedUserId. Registration did not initialize the keypair correctly."
}

# Ensure staff memberships exist in organization service so appointment validation passes.
$null = ApiJson -Method 'POST' -Url "$base/api/v1/memberships" -Token $adminToken -Body @{
    userId = $receptionUserId
    orgId = $orgId
    employeeId = "EMP-REC-$($stamp.Substring(8,6))"
    jobTitle = 'Receptionist'
    startDate = '2024-01-01'
}

$null = ApiJson -Method 'POST' -Url "$base/api/v1/memberships" -Token $adminToken -Body @{
    userId = $doctorUserId
    orgId = $orgId
    employeeId = "EMP-DOC-$($stamp.Substring(8,6))"
    jobTitle = 'Doctor'
    startDate = '2024-01-01'
}

$scheduled = (Get-Date).ToUniversalTime().AddDays(1).ToString('yyyy-MM-ddTHH:mm:ssZ')
$apt = ApiJson -Method 'POST' -Url "$base/api/v1/appointments" -Token $patientToken -Body @{
    patientId=$patientId; doctorId=$doctorId; orgId=$orgId; scheduledAt=$scheduled
}
$appointmentId = [Guid]$apt.data.appointmentId

$null = ApiJson -Method 'PUT' -Url "$base/api/v1/appointments/$appointmentId/confirm" -Token $doctorToken
$null = ApiJson -Method 'PUT' -Url "$base/api/v1/appointments/$appointmentId/check-in" -Token $doctorToken

$ehrPayload = @{
    patientId = $patientId
    orgId = $orgId
    data = @{
        resourceType='Bundle'
        type='document'
        entry=@(
            @{ resource = @{ resourceType='Condition'; code=@{text='Follow-up treatment required'}; clinicalStatus=@{coding=@(@{code='active'})} } }
        )
    }
}

$ehr = ApiJson -Method 'POST' -Url "$base/api/v1/ehr/records" -Token $doctorToken -Headers @{'X-Doctor-Id'=$doctorId.ToString()} -Body $ehrPayload
$ehrId = [Guid]$ehr.ehrId
Write-FlowLog ("EHR result summary: ehrId={0}; ipfsCid={1}; dataHash={2}; fileId={3}; versionId={4}" -f $ehr.ehrId, $ehr.ipfsCid, $ehr.dataHash, $ehr.fileId, $ehr.versionId)

$beforeStatus = ''
try {
    $null = ApiJson -Method 'GET' -Url "$base/api/v1/ehr/records/$ehrId" -Token $doctorToken -Headers @{'X-Requester-Id'=$doctorUserId.ToString()}
    $beforeStatus = 'unexpected-success'
}
catch {
    $beforeStatus = $_.Exception.Response.StatusCode.value__
}

$beforeDocumentStatus = ''
try {
    $null = ApiJson -Method 'GET' -Url "$base/api/v1/ehr/records/$ehrId/document" -Token $doctorToken -Headers @{'X-Requester-Id'=$doctorUserId.ToString()}
    $beforeDocumentStatus = 'unexpected-success'
}
catch {
    $beforeDocumentStatus = $_.Exception.Response.StatusCode.value__
}

$req = ApiJson -Method 'POST' -Url "$base/api/v1/access-requests" -Token $doctorToken -Body @{
    patientId=$patientId; patientDid="did:fabric:patient:$patientId";
    requesterId=$doctorUserId; requesterDid="did:fabric:doctor:$doctorUserId"; requesterType='DOCTOR';
    ehrId=$ehrId; permission='READ'; purpose='TREATMENT'; reason='Need EHR access for treatment'
}
$requestId = [Guid]$req.data.requestId

$null = ApiJson -Method 'POST' -Url "$base/api/v1/access-requests/$requestId/respond" -Token $patientToken -Body @{approve=$true; responseReason='Approved for treatment'}

$after = ApiJson -Method 'GET' -Url "$base/api/v1/ehr/records/$ehrId" -Token $doctorToken -Headers @{'X-Requester-Id'=$doctorUserId.ToString()}
$afterOk = [bool]$after.ehrId

$afterDocument = ApiJson -Method 'GET' -Url "$base/api/v1/ehr/records/$ehrId/document" -Token $doctorToken -Headers @{'X-Requester-Id'=$doctorUserId.ToString()}
$afterDocumentSuccess = $null -ne $afterDocument

$result = [ordered]@{
    orgId = $orgId
    receptionEmail = $receptionEmail
    receptionUserId = $receptionUserId
    doctorEmail = $doctorEmail
    doctorUserId = $doctorUserId
    doctorId = $doctorId
    patientEmail = $patientEmail
    patientUserId = $patientUserId
    patientId = $patientId
    patientResolvedUserId = $patientResolvedUserId
    appointmentId = $appointmentId
    ehrId = $ehrId
    requestId = $requestId
    viewBeforeConsentStatus = $beforeStatus
    documentBeforeConsentStatus = $beforeDocumentStatus
    viewAfterConsentSuccess = $afterOk
    documentAfterConsentSuccess = $afterDocumentSuccess
}

$result | ConvertTo-Json -Depth 10 | Tee-Object -FilePath "$PSScriptRoot\..\real-flow-result.json"
