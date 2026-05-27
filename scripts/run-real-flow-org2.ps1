$ErrorActionPreference = 'Stop'

$base = 'http://127.0.0.1:5000'
$logFile = Join-Path $PSScriptRoot 'real-flow-org2-api.log'

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

function TryLogin {
    param(
        [string]$Email,
        [string]$Password
    )

    try {
        return ApiJson -Method 'POST' -Url "$base/api/v1/auth/login" -Body @{email=$Email;password=$Password}
    }
    catch {
        return $null
    }
}

function EnsureAdminUser {
    param(
        [string]$Email,
        [string]$Password,
        [string]$OrgId,
        [string]$SeedAdminToken
    )

    $login = TryLogin -Email $Email -Password $Password
    if (-not $login) {
        $null = ApiJson -Method 'POST' -Url "$base/api/v1/auth/register" -Body @{
            fullName='Admin Org2'
            email=$Email
            password=$Password
            phone='0911111111'
        }
        $login = TryLogin -Email $Email -Password $Password
    }

    if (-not $login) {
        throw "Unable to login as $Email after registration."
    }

    $userId = [Guid]$login.userId

    $null = ApiJson -Method 'PUT' -Url "$base/api/v1/auth/updateRole" -Token $SeedAdminToken -Body @{
        userId=$userId
        newRole='Admin'
    }

    $null = ApiJson -Method 'PUT' -Url "$base/api/v1/auth/users/$userId" -Token $SeedAdminToken -Body @{
        organizationId=$OrgId
    }

    $login = ApiJson -Method 'POST' -Url "$base/api/v1/auth/login" -Body @{email=$Email;password=$Password}
    return @{ login=$login; userId=$userId }
}

function EnsureDoctorUser {
    param(
        [string]$Email,
        [string]$Password,
        [string]$Phone,
        [string]$OrgId,
        [string]$AdminToken
    )

    $login = TryLogin -Email $Email -Password $Password
    if (-not $login) {
        $null = ApiJson -Method 'POST' -Url "$base/api/v1/auth/registerStaffDoctor" -Token $AdminToken -Body @{
            fullName='Doctor Org2'
            email=$Email
            password=$Password
            phone=$Phone
            organizationId=$OrgId
            role='Doctor'
        }
        $login = TryLogin -Email $Email -Password $Password
    }

    if (-not $login) {
        throw "Unable to login as $Email after registration."
    }

    return $login
}

function EnsurePatientUser {
    param(
        [string]$Email,
        [string]$Password,
        [string]$Phone,
        [string]$OrgId,
        [string]$AdminToken
    )

    $login = TryLogin -Email $Email -Password $Password
    if (-not $login) {
        $null = ApiJson -Method 'POST' -Url "$base/api/v1/auth/register" -Body @{
            fullName='Patient Org2'
            email=$Email
            password=$Password
            phone=$Phone
        }
        $login = TryLogin -Email $Email -Password $Password
    }

    if (-not $login) {
        throw "Unable to login as $Email after registration."
    }

    $userId = [Guid]$login.userId
    $null = ApiJson -Method 'PUT' -Url "$base/api/v1/auth/users/$userId" -Token $AdminToken -Body @{
        organizationId=$OrgId
    }

    $login = ApiJson -Method 'POST' -Url "$base/api/v1/auth/login" -Body @{email=$Email;password=$Password}
    return @{ login=$login; userId=$userId }
}

$orgId = '11111111-1111-1111-1111-111111111102'
$orgName = 'Benh vien Nhi Dong 1'

$stamp = Get-Date -Format 'yyyyMMddHHmmss'

$admin2Email = 'admin2@dbh.com'
$admin2Password = 'admin123'
$doctor2Email = "doctor.$stamp@dbh.vn"
$doctor2Password = 'Doctor@123'
$doctor2Phone = '0929' + $stamp.Substring(8, 6)
$patient2Email = "patient.$stamp@dbh.vn"
$patient2Password = 'Patient@123'
$patient2Phone = '0939' + $stamp.Substring(8, 6)

$admin2Login = ApiJson -Method 'POST' -Url "$base/api/v1/auth/login" -Body @{email=$admin2Email;password=$admin2Password}
$admin2Token = $admin2Login.token
$admin2UserId = [Guid]$admin2Login.userId

$org2Response = ApiJson -Method 'GET' -Url "$base/api/v1/organizations/$orgId" -Token $admin2Token
$org2Data = $org2Response.data
if ($null -eq $org2Data) {
    throw "Organization $orgId ($orgName) was not found."
}

$doctorLogin = EnsureDoctorUser -Email $doctor2Email -Password $doctor2Password -Phone $doctor2Phone -OrgId $orgId -AdminToken $admin2Token
$doctorToken = $doctorLogin.token
$doctorMe = ApiJson -Method 'GET' -Url "$base/api/v1/auth/me" -Token $doctorToken
$doctorUserId = [Guid]$doctorMe.userId
$doctorId = [Guid]$doctorMe.profiles.Doctor.doctorId

$patientLogin = EnsurePatientUser -Email $patient2Email -Password $patient2Password -Phone $patient2Phone -OrgId $orgId -AdminToken $admin2Token
$patientToken = $patientLogin.login.token
$patientMe = ApiJson -Method 'GET' -Url "$base/api/v1/auth/me" -Token $patientToken
$patientUserId = [Guid]$patientMe.userId
$patientId = [Guid]$patientMe.profiles.Patient.patientId

$patientResolved = ApiJson -Method 'GET' -Url "$base/api/v1/auth/user-id?patientId=$patientId" -Token $admin2Token
$patientResolvedUserId = [Guid]$patientResolved.userId
$patientKeys = ApiJson -Method 'GET' -Url "$base/api/v1/auth/$patientResolvedUserId/keys" -Token $admin2Token

if ([string]::IsNullOrWhiteSpace($patientKeys.publicKey) -or [string]::IsNullOrWhiteSpace($patientKeys.encryptedPrivateKey)) {
    throw "Patient auth keys are missing for user $patientResolvedUserId. Registration did not initialize the keypair correctly."
}

$null = ApiJson -Method 'POST' -Url "$base/api/v1/memberships" -Token $admin2Token -Body @{
    userId = $doctorUserId
    orgId = $orgId
    employeeId = "EMP-DOC2-$($stamp.Substring(8,6))"
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

Write-FlowLog "=============================================================================="
$ehr = ApiJson -Method 'POST' -Url "$base/api/v1/ehr/records" -Token $doctorToken -Headers @{'X-Doctor-Id'=$doctorId.ToString()} -Body $ehrPayload
if ($null -ne $ehr -and $null -ne $ehr.data) {
    Write-FlowLog ("Created EHR record with ehrId={0}" -f $ehr.data.ehrId)
    $ehrId = [Guid]$ehr.data.ehrId
    Write-FlowLog ("EHR result summary: ehrId={0}; ipfsCid={1}; dataHash={2}; fileId={3}; versionId={4}" -f $ehr.data.ehrId, $ehr.data.ipfsCid, $ehr.data.dataHash, $ehr.data.fileId, $ehr.data.versionId)
} else {
    Write-FlowLog "Created EHR record: unexpected response shape"
    Write-FlowLog (($ehr | ConvertTo-Json -Depth 5) )
    throw "Unexpected API response when creating EHR record"
}

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
    ehrId=$ehrId; permission='FULL_ACCESS'; purpose='TREATMENT'; reason='Need EHR access for treatment'
}
$requestId = [Guid]$req.data.requestId

$null = ApiJson -Method 'POST' -Url "$base/api/v1/access-requests/$requestId/respond" -Token $patientToken -Body @{approve=$true; responseReason='Approved for treatment'}

$after = ApiJson -Method 'GET' -Url "$base/api/v1/ehr/records/$ehrId" -Token $doctorToken -Headers @{'X-Requester-Id'=$doctorUserId.ToString()}
$afterOk = [bool]$after.ehrId

$afterDocument = ApiJson -Method 'GET' -Url "$base/api/v1/ehr/records/$ehrId/document" -Token $doctorToken -Headers @{'X-Requester-Id'=$doctorUserId.ToString()}
$afterDocumentSuccess = $null -ne $afterDocument

$updateDocumentSuccess = $false
$updateDocumentStatus = ''
$afterUpdateDocumentSuccess = $false
$afterUpdateDocumentStatus = ''

if ($afterDocumentSuccess) {
    $flowTimestamp = Get-Date -Format 'yyyyMMddHHmmss'
    Write-FlowLog "Updating EHR record $ehrId"
    $ehrUpdatePayload = @{
        data = @{
            resourceType='Bundle'
            type='document'
            entry=@(
                @{ resource = @{ resourceType='Condition'; code=@{text="Updated treatment [$flowTimestamp]"}; clinicalStatus=@{coding=@(@{code='resolved'})} } }
            )
        }
    }
    try {
        $null = ApiJson -Method 'PUT' -Url "$base/api/v1/ehr/records/$ehrId" -Token $doctorToken -Headers @{'X-Doctor-Id'=$doctorId.ToString()} -Body $ehrUpdatePayload
        $updateDocumentSuccess = $true
        $updateDocumentStatus = 200
    }
    catch {
        $updateDocumentSuccess = $false
        if ($_.Exception.Response) {
            $updateDocumentStatus = $_.Exception.Response.StatusCode.value__
        }
        else {
            $updateDocumentStatus = 'client-error'
        }
    }

    if ($updateDocumentSuccess) {
        Start-Sleep -Seconds 10
        Write-FlowLog "Getting updated EHR document $ehrId"
        try {
            $updatedDocument = ApiJson -Method 'GET' -Url "$base/api/v1/ehr/records/$ehrId/document" -Token $doctorToken -Headers @{'X-Requester-Id'=$doctorUserId.ToString()}
            $afterUpdateDocumentSuccess = $null -ne $updatedDocument
            $afterUpdateDocumentStatus = 200
        }
        catch {
            $afterUpdateDocumentSuccess = $false
            if ($_.Exception.Response) {
                $afterUpdateDocumentStatus = $_.Exception.Response.StatusCode.value__
            }
            else {
                $afterUpdateDocumentStatus = 'client-error'
            }
        }
    }
}

$result = [ordered]@{
    orgId = $orgId
    orgName = $org2Data.orgName
    orgDid = $org2Data.orgDid
    orgFabricMspId = $org2Data.fabricMspId
    orgFabricCaUrl = $org2Data.fabricCaUrl
    admin2Email = $admin2Email
    admin2UserId = $admin2UserId
    doctorEmail = $doctor2Email
    doctorUserId = $doctorUserId
    doctorId = $doctorId
    doctorToken = $doctorToken
    patientEmail = $patient2Email
    patientUserId = $patientUserId
    patientId = $patientId
    patientToken = $patientToken
    patientResolvedUserId = $patientResolvedUserId
    appointmentId = $appointmentId
    ehrId = $ehrId
    requestId = $requestId
    viewBeforeConsentStatus = $beforeStatus
    documentBeforeConsentStatus = $beforeDocumentStatus
    viewAfterConsentSuccess = $afterOk
    documentAfterConsentSuccess = $afterDocumentSuccess
    updateDocumentStatus = $updateDocumentStatus
    updateDocumentSuccess = $updateDocumentSuccess
    documentAfterUpdateStatus = $afterUpdateDocumentStatus
    documentAfterUpdateSuccess = $afterUpdateDocumentSuccess
}

$result | ConvertTo-Json -Depth 10 | Tee-Object -FilePath "$PSScriptRoot\..\real-flow-org2-result.json"
