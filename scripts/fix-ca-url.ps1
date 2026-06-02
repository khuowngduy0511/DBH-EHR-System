$ErrorActionPreference = 'Stop'

# Login as admin2 to get token
$loginBody = @{
    email = "admin2@dbh.com"
    password = "admin123"
} | ConvertTo-Json

$loginResponse = Invoke-RestMethod -Method POST -Uri "http://127.0.0.1:5000/api/v1/auth/login" `
    -Headers @{"Content-Type"="application/json"} -Body $loginBody
$token = $loginResponse.token
Write-Host "Got admin2 token"

# Update hospital2 fabric_ca_url to use HTTPS
$updateBody = @{
    fabricCaUrl = "https://ca_hospital2:8054"
} | ConvertTo-Json

$headers = @{
    "Content-Type" = "application/json"
    "Authorization" = "Bearer $token"
}

try {
    $response = Invoke-RestMethod -Method PUT `
        -Uri "http://127.0.0.1:5000/api/v1/organizations/11111111-1111-1111-1111-111111111102/fabric-config" `
        -Headers $headers -Body $updateBody
    Write-Host "Updated hospital2 fabricCaUrl: $($response.data.fabricCaUrl)"
    Write-Host "Success!"
} catch {
    Write-Host "Error: $_"
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $reader.ReadToEnd() | Write-Host
        $reader.Close()
    }
}