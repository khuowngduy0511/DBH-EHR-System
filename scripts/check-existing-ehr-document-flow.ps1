param(
    [string]$BaseUrl = 'http://localhost:5000',
    [string]$Email = 'dr.hieu@dbh.vn',
    [string]$Password = 'Doctor@123',
    [string]$EhrId = '3e66c148-660f-4eb9-9077-e28e1b5c1cb7',
    [string]$Token = $env:EHR_CHECK_TOKEN
)

$ErrorActionPreference = 'Stop'

$logFile = Join-Path $PSScriptRoot 'check-existing-ehr-document-flow.log'
$resultFile = Join-Path $PSScriptRoot '..\check-existing-ehr-document-result.json'

if (Test-Path $logFile) {
    Remove-Item $logFile -Force
}

function Write-FlowLog {
    param([string]$Message)

    $entry = "[{0}] {1}" -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss.fff'), $Message
    Add-Content -Path $logFile -Value $entry
    Write-Host $entry
}

function Invoke-ApiJson {
    param(
        [string]$Method,
        [string]$Url,
        $Body = $null,
        [string]$Token = $null,
        [hashtable]$Headers = @{}
    )

    $requestHeaders = @{}
    if ($Token) {
        $requestHeaders['Authorization'] = "Bearer $Token"
    }
    foreach ($key in $Headers.Keys) {
        $requestHeaders[$key] = $Headers[$key]
    }

    try {
        $requestBody = $null
        if ($null -ne $Body) {
            $requestBody = $Body | ConvertTo-Json -Depth 30
            Write-FlowLog ("==> {0} {1}" -f $Method, $Url)
            Write-FlowLog ("RequestBody: {0}" -f $requestBody)
            $response = Invoke-RestMethod -Method $Method -Uri $Url -Headers $requestHeaders -ContentType 'application/json' -Body $requestBody
        }
        else {
            Write-FlowLog ("==> {0} {1}" -f $Method, $Url)
            $response = Invoke-RestMethod -Method $Method -Uri $Url -Headers $requestHeaders
        }

        $responseText = if ($response -is [string]) { $response } else { $response | ConvertTo-Json -Depth 30 }
        Write-FlowLog ("Response: {0}" -f $responseText)

        return [pscustomobject]@{
            Success = $true
            StatusCode = 200
            Data = $response
            RawBody = $responseText
        }
    }
    catch {
        $statusCode = 0
        $errorBody = ''

        if ($_.Exception.Response) {
            try {
                $statusCode = [int]$_.Exception.Response.StatusCode
            }
            catch {}

            try {
                $stream = $_.Exception.Response.GetResponseStream()
                if ($stream) {
                    $reader = New-Object System.IO.StreamReader($stream)
                    $errorBody = $reader.ReadToEnd()
                }
            }
            catch {}
        }

        if (-not $errorBody) {
            $errorBody = $_.Exception.Message
        }

        Write-FlowLog ("FAILED: {0} {1}" -f $Method, $Url)
        Write-FlowLog ("StatusCode: {0}" -f $statusCode)
        Write-FlowLog ("ErrorBody: {0}" -f $errorBody)

        return [pscustomobject]@{
            Success = $false
            StatusCode = $statusCode
            Data = $null
            RawBody = $errorBody
        }
    }
}

$parsedEhrId = [Guid]::Empty
if (-not [Guid]::TryParse($EhrId, [ref]$parsedEhrId)) {
    throw "Invalid EhrId: $EhrId"
}

Write-FlowLog ("Starting existing-account EHR document check flow")
Write-FlowLog ("BaseUrl={0}; Email={1}; EhrId={2}" -f $BaseUrl, $Email, $EhrId)

if (-not [string]::IsNullOrWhiteSpace($Token)) {
    Write-FlowLog ("Using provided bearer token (login step skipped)")
    $token = $Token
}
else {
    $login = Invoke-ApiJson -Method 'POST' -Url "$BaseUrl/api/v1/auth/login" -Body @{ email = $Email; password = $Password }
    if (-not $login.Success) {
        throw "Login failed with status $($login.StatusCode). See $logFile"
    }

    $token = $login.Data.token
    if ([string]::IsNullOrWhiteSpace($token)) {
        throw "Login succeeded but token was empty. See $logFile"
    }
}

$me = Invoke-ApiJson -Method 'GET' -Url "$BaseUrl/api/v1/auth/me" -Token $token
if (-not $me.Success) {
    throw "Cannot load current user profile. Status $($me.StatusCode). See $logFile"
}

$requesterId = [Guid]$me.Data.userId

$document = Invoke-ApiJson -Method 'GET' -Url "$BaseUrl/api/v1/ehr/records/$EhrId/document" -Token $token -Headers @{'X-Requester-Id' = $requesterId.ToString()}

$documentSuccess = $document.Success -and $document.StatusCode -eq 200

$result = [ordered]@{
    baseUrl = $BaseUrl
    ehrId = $EhrId
    accountEmail = $Email
    requesterId = $requesterId
    success = $documentSuccess
    statusCode = $document.StatusCode
    responseSnippet = if ($document.RawBody.Length -gt 1000) { $document.RawBody.Substring(0, 1000) } else { $document.RawBody }
    checkedAtUtc = (Get-Date).ToUniversalTime().ToString('o')
}

$result | ConvertTo-Json -Depth 10 | Tee-Object -FilePath $resultFile

if (-not $documentSuccess) {
    throw "EHR document check failed with status $($document.StatusCode). See $logFile"
}

Write-FlowLog ("Flow completed successfully")