$ErrorActionPreference = "Continue"
$secret = "your-super-secret-key-for-jwt-token-generation-min-32-chars"
$header = '{"alg":"HS256","typ":"JWT"}'
$hB = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($header)).TrimEnd('=').Replace('+','-').Replace('/','_')
$now = [int][double]::Parse((Get-Date -UFormat %s))
$pl = @{sub=[guid]::NewGuid().ToString();iss="DBH.Auth.Service";aud="DBH.EHR.System";iat=$now;exp=($now+86400);"http://schemas.microsoft.com/ws/2008/06/identity/claims/role"="Doctor"} | ConvertTo-Json -Compress
$pB = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($pl)).TrimEnd('=').Replace('+','-').Replace('/','_')
$hmac = New-Object System.Security.Cryptography.HMACSHA256
$hmac.Key = [Text.Encoding]::UTF8.GetBytes($secret)
$sig = [Convert]::ToBase64String($hmac.ComputeHash([Text.Encoding]::UTF8.GetBytes("$hB.$pB"))).TrimEnd('=').Replace('+','-').Replace('/','_')
$token = "$hB.$pB.$sig"

$base = "http://localhost:5002"
$patientId = [guid]::NewGuid()

# Health check (no auth needed)
Write-Host "=== Test: Health Check ==="
$r = Invoke-RestMethod -Uri "$base/health" -Method GET
Write-Host "Status: healthy = $($r.status)" -ForegroundColor Green

# Create EHR
Write-Host "`n=== Test: Create EHR ==="
$body = '{"patientId":"' + $patientId + '","data":{"diagnosis":"Test","bp":"120/80"}}'
$r = Invoke-RestMethod -Uri "$base/api/v1/ehr/records" -Method POST -Body $body -ContentType "application/json" -Headers @{Authorization="Bearer $token"}
$ehrId = $r.ehrId
$versionId = $r.versionId
$fileId = $r.fileId
Write-Host "EhrId: $ehrId" -ForegroundColor Green
Write-Host "IpfsCid: $($r.ipfsCid)" -ForegroundColor Green
Write-Host "DataHash: $($r.dataHash)" -ForegroundColor Green

# File upload via curl
Write-Host "`n=== Test: Upload File (multipart) ==="
$tmpFile = "$env:TEMP\test-ehr-file.txt"
Set-Content -Path $tmpFile -Value "Lab results: WBC 7.5, RBC 4.8, Hemoglobin 14.2"
$curlOut = & curl.exe -s -o - -w "`n%{http_code}" -X POST "$base/api/v1/ehr/records/$ehrId/files" -H "Authorization: Bearer $token" -F "file=@$tmpFile;type=text/plain" 2>&1
$lines = $curlOut -split "`n"
$httpCode = $lines[-1].Trim()
$respBody = ($lines[0..($lines.Count-2)] -join "`n").Trim()
Write-Host "Status: $httpCode"
if ($httpCode -eq "201") {
    Write-Host "PASS - File uploaded successfully" -ForegroundColor Green
    $fileData = $respBody | ConvertFrom-Json
    Write-Host "FileId: $($fileData.fileId), FileHash: $($fileData.fileHash)" -ForegroundColor Green
    $uploadedFileId = $fileData.fileId
} else {
    Write-Host "FAIL - $respBody" -ForegroundColor Red
    $uploadedFileId = $null
}
Remove-Item $tmpFile -ErrorAction SilentlyContinue

# Get files
Write-Host "`n=== Test: Get Files ==="
$r = Invoke-RestMethod -Uri "$base/api/v1/ehr/records/$ehrId/files" -Method GET -Headers @{Authorization="Bearer $token"}
Write-Host "File count: $($r.Count)" -ForegroundColor Green

# Delete uploaded file
if ($uploadedFileId) {
    Write-Host "`n=== Test: Delete File ==="
    try {
        $null = Invoke-WebRequest -Uri "$base/api/v1/ehr/records/$ehrId/files/$uploadedFileId" -Method DELETE -Headers @{Authorization="Bearer $token"}
        Write-Host "PASS - File deleted (204)" -ForegroundColor Green
    } catch {
        $st = [int]$_.Exception.Response.StatusCode
        if ($st -eq 204) { Write-Host "PASS - File deleted (204)" -ForegroundColor Green }
        else { Write-Host "FAIL - Status $st" -ForegroundColor Red }
    }
}

Write-Host "`n=== ALL TESTS COMPLETE ===" -ForegroundColor Cyan
