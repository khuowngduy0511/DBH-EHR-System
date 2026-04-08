##############################################
# Test Auth Service - Full Flow             #
# Base URL: http://localhost:5000 (Gateway) #
##############################################

$BASE = "http://localhost:5000"
$pass = 0; $fail = 0; $total = 0
$script:adminToken = ""; $script:doctorToken = ""; $script:patientToken = ""
$script:staffToken = ""
$script:newUserId = ""; $script:newPatientId = ""; $script:newDoctorId = ""
$script:refreshToken = ""

function Test($num, $name, $method, $url, $body, $token, $expectedStatus) {
    $script:total++
    $headers = @{ "Content-Type" = "application/json" }
    if ($token) { $headers["Authorization"] = "Bearer $token" }
    try {
        $params = @{ Method = $method; Uri = "$BASE$url"; Headers = $headers; ErrorAction = "Stop" }
        if ($body) { $params["Body"] = ($body | ConvertTo-Json -Depth 5) }
        $resp = Invoke-WebRequest @params
        $code = $resp.StatusCode
    } catch {
        $code = [int]$_.Exception.Response.StatusCode
        $resp = $null
        if ($_.ErrorDetails -and $_.ErrorDetails.Message) { try { $resp = $_.ErrorDetails.Message | ConvertFrom-Json } catch {} }
    }
    if ($code -eq $expectedStatus) {
        Write-Host "[$num] $name [PASS $code]" -ForegroundColor Green
        $script:pass++
    } else {
        Write-Host "[$num] $name [FAIL expected=$expectedStatus got=$code]" -ForegroundColor Red
        $script:fail++
    }
    if ($resp -and $resp.Content) {
        try { return $resp.Content | ConvertFrom-Json } catch { return $resp.Content }
    }
    if ($resp -and $resp.message) { return $resp }
    return $null
}

Write-Host "`n====== AUTH SERVICE - FULL FLOW TEST ======`n" -ForegroundColor Cyan

# =============================================
# 1. LOGIN (existing seed accounts)
# =============================================
Write-Host "=== 1. LOGIN ===" -ForegroundColor Yellow

$r = Test 1 "Login Admin" "POST" "/api/v1/auth/login" @{email="admin@dbh.vn";password="Admin@123456"} $null 200
if ($r) { $script:adminToken = $r.token; Write-Host "  AdminId=$($r.userId)" }

$r = Test 2 "Login Doctor (dr.hieu)" "POST" "/api/v1/auth/login" @{email="dr.hieu@dbh.vn";password="Doctor@123"} $null 200
if ($r) { $script:doctorToken = $r.token; Write-Host "  DoctorUserId=$($r.userId)" }

$r = Test 3 "Login Patient (patient.an)" "POST" "/api/v1/auth/login" @{email="patient.an@dbh.vn";password="Patient@123"} $null 200
if ($r) { $script:patientToken = $r.token; $script:refreshToken = $r.refreshToken; Write-Host "  PatientUserId=$($r.userId)" }

$r = Test 4 "Login Staff (nurse.hoa)" "POST" "/api/v1/auth/login" @{email="nurse.hoa@dbh.vn";password="Staff@123"} $null 200
if ($r) { $script:staffToken = $r.token; Write-Host "  StaffUserId=$($r.userId)" }

$r = Test 5 "Login Invalid Password (401)" "POST" "/api/v1/auth/login" @{email="admin@dbh.vn";password="wrongpassword"} $null 401

$r = Test 6 "Login Non-existent User (401)" "POST" "/api/v1/auth/login" @{email="nobody@dbh.vn";password="Test@123"} $null 401

# =============================================
# 2. GET /me (Profile)
# =============================================
Write-Host "`n=== 2. GET /me PROFILE ===" -ForegroundColor Yellow

$r = Test 7 "Get Admin Profile" "GET" "/api/v1/auth/me" $null $script:adminToken 200
if ($r) { Write-Host "  Admin: $($r.fullName) | Roles: $($r.roles -join ',')" }

$r = Test 8 "Get Doctor Profile" "GET" "/api/v1/auth/me" $null $script:doctorToken 200
if ($r) { 
    Write-Host "  Doctor: $($r.fullName) | Roles: $($r.roles -join ',')"
    if ($r.profiles.Doctor) { Write-Host "  DoctorId=$($r.profiles.Doctor.doctorId) | Specialty=$($r.profiles.Doctor.specialty)" }
}

$r = Test 9 "Get Patient Profile" "GET" "/api/v1/auth/me" $null $script:patientToken 200
if ($r) { 
    Write-Host "  Patient: $($r.fullName) | Roles: $($r.roles -join ',')"
    if ($r.profiles.Patient) { Write-Host "  PatientId=$($r.profiles.Patient.patientId)" }
}

$r = Test 10 "Get Staff Profile" "GET" "/api/v1/auth/me" $null $script:staffToken 200
if ($r) { 
    Write-Host "  Staff: $($r.fullName) | Roles: $($r.roles -join ',')"
}

$r = Test 11 "Get Profile No Token (401)" "GET" "/api/v1/auth/me" $null $null 401

# =============================================
# 3. REGISTER (Patient - public)
# =============================================
Write-Host "`n=== 3. REGISTER PATIENT ===" -ForegroundColor Yellow

$ts = (Get-Date).ToString("HHmmss")
$newEmail = "testuser.$ts@dbh.vn"

$r = Test 12 "Register New Patient" "POST" "/api/v1/auth/register" @{
    fullName="Test User $ts"
    email=$newEmail
    password="Test@12345"
    phone="0909$ts"
} $null 200
if ($r -and $r.success) { 
    $script:newUserId = $r.userId
    Write-Host "  NewUserId=$($r.userId)" 
}

$r = Test 13 "Register Duplicate Email (400)" "POST" "/api/v1/auth/register" @{
    fullName="Dup User"
    email=$newEmail
    password="Test@12345"
    phone="0909000000"
} $null 400

# Login with new account
$r = Test 14 "Login New Patient" "POST" "/api/v1/auth/login" @{email=$newEmail;password="Test@12345"} $null 200
$newPatientToken = ""
if ($r) { $newPatientToken = $r.token; Write-Host "  Logged in as new patient" }

$r = Test 15 "Verify New Patient Profile" "GET" "/api/v1/auth/me" $null $newPatientToken 200
if ($r) { Write-Host "  Name=$($r.fullName) | Roles=$($r.roles -join ',')" }

# =============================================
# 4. REGISTER STAFF/DOCTOR (Admin only)
# =============================================
Write-Host "`n=== 4. REGISTER STAFF/DOCTOR ===" -ForegroundColor Yellow

$docEmail = "testdoc.$ts@dbh.vn"
$r = Test 16 "Register Doctor (Admin)" "POST" "/api/v1/auth/registerStaffDoctor" @{
    fullName="BS. Test Doctor $ts"
    email=$docEmail
    password="Doctor@12345"
    phone="0808$ts"
    role="Doctor"
    gender="Male"
    address="Test Address"
} $script:adminToken 200
if ($r -and $r.success) { Write-Host "  NewDoctorUserId=$($r.userId)" }

$nurseEmail = "testnurse.$ts@dbh.vn"
$r = Test 17 "Register Nurse (Admin)" "POST" "/api/v1/auth/registerStaffDoctor" @{
    fullName="DD. Test Nurse $ts"
    email=$nurseEmail
    password="Nurse@12345"
    phone="0707$ts"
    role="Nurse"
} $script:adminToken 200
if ($r -and $r.success) { Write-Host "  NewNurseUserId=$($r.userId)" }

$r = Test 18 "Register Staff - No Auth (401)" "POST" "/api/v1/auth/registerStaffDoctor" @{
    fullName="Unauthorized"
    email="unauth@dbh.vn"
    password="Test@12345"
    phone="0000000000"
    role="Doctor"
} $null 401

$r = Test 19 "Register Staff - Patient Token (403)" "POST" "/api/v1/auth/registerStaffDoctor" @{
    fullName="Patient Trying Admin"
    email="patientadmin@dbh.vn"
    password="Test@12345"
    phone="0000000001"
    role="Doctor"
} $script:patientToken 403

# =============================================
# 5. UPDATE PROFILE
# =============================================
Write-Host "`n=== 5. UPDATE PROFILE ===" -ForegroundColor Yellow

$r = Test 20 "Update Profile (Patient)" "PUT" "/api/v1/auth/me/profile" @{
    phone="0999888777"
    gender="Male"
    address="123 Test Street, HCM"
    dateOfBirth="1990-05-15T00:00:00Z"
} $script:patientToken 200
if ($r) { Write-Host "  $($r.message)" }

$r = Test 21 "Verify Updated Profile" "GET" "/api/v1/auth/me" $null $script:patientToken 200
if ($r) { Write-Host "  Phone=$($r.phone) | Gender=$($r.gender) | Address=$($r.address)" }

$r = Test 22 "Update Profile No Auth (401)" "PUT" "/api/v1/auth/me/profile" @{phone="000"} $null 401

# =============================================
# 6. UPDATE ROLE
# =============================================
Write-Host "`n=== 6. UPDATE ROLE ===" -ForegroundColor Yellow

if ($script:newUserId) {
    $r = Test 23 "Update Role to Doctor" "PUT" "/api/v1/auth/updateRole" @{
        userId=$script:newUserId
        newRole="Doctor"
    } $null 200
    if ($r) { Write-Host "  $($r.message)" }
    
    # Revert back
    $r = Test 24 "Update Role back to Patient" "PUT" "/api/v1/auth/updateRole" @{
        userId=$script:newUserId
        newRole="Patient"
    } $null 200
    if ($r) { Write-Host "  $($r.message)" }
} else {
    Write-Host "[23] Update Role to Doctor [SKIP - no newUserId]" -ForegroundColor DarkYellow
    Write-Host "[24] Update Role back to Patient [SKIP]" -ForegroundColor DarkYellow
}

$r = Test 25 "Update Role Invalid (400)" "PUT" "/api/v1/auth/updateRole" @{
    userId=[Guid]::NewGuid().ToString()
    newRole="InvalidRole"
} $null 400

# =============================================
# 7. REFRESH TOKEN
# =============================================
Write-Host "`n=== 7. REFRESH TOKEN ===" -ForegroundColor Yellow

if ($script:refreshToken) {
    $r = Test 26 "Refresh Token" "POST" "/api/v1/auth/refresh-token" @{
        refreshToken=$script:refreshToken
    } $null 200
    if ($r -and $r.token) { 
        Write-Host "  New token received"
        $script:patientToken = $r.token
        $script:refreshToken = $r.refreshToken
    }
} else {
    Write-Host "[26] Refresh Token [SKIP - no refresh token]" -ForegroundColor DarkYellow
}

$r = Test 27 "Refresh Token Invalid (400)" "POST" "/api/v1/auth/refresh-token" @{
    refreshToken="invalid-refresh-token-value"
} $null 400

# =============================================
# 8. REVOKE TOKEN
# =============================================
Write-Host "`n=== 8. REVOKE TOKEN ===" -ForegroundColor Yellow

# Login new patient again to get fresh token for revoke test
$r2 = Test 28 "Login for Revoke Test" "POST" "/api/v1/auth/login" @{email=$newEmail;password="Test@12345"} $null 200
$revokeToken = ""
if ($r2) { $revokeToken = $r2.token }

if ($revokeToken) {
    $r = Test 29 "Revoke Token" "POST" "/api/v1/auth/revoke-token" $null $revokeToken 200
} else {
    Write-Host "[29] Revoke Token [SKIP]" -ForegroundColor DarkYellow
}

$r = Test 30 "Revoke Token No Auth (401)" "POST" "/api/v1/auth/revoke-token" $null $null 401

# =============================================
# 9. GET USER BY ID
# =============================================
Write-Host "`n=== 9. USER LOOKUP ===" -ForegroundColor Yellow

$r = Test 31 "Get User Profile by UserId" "GET" "/api/v1/auth/users/$($script:newUserId)" $null $script:adminToken 200
if ($r) { Write-Host "  Found: $($r.fullName)" }

# Get patientId from seed data
$patMe = Test 32 "Get Patient /me for patientId" "GET" "/api/v1/auth/me" $null $script:patientToken 200
$seedPatientId = ""
if ($patMe -and $patMe.profiles.Patient) { $seedPatientId = $patMe.profiles.Patient.patientId }

if ($seedPatientId) {
    $r = Test 33 "Get UserId by PatientId" "GET" "/api/v1/auth/user-id?patientId=$seedPatientId" $null $script:adminToken 200
    if ($r) { Write-Host "  UserId=$($r.userId)" }
} else {
    Write-Host "[33] Get UserId by PatientId [SKIP]" -ForegroundColor DarkYellow
}

$r = Test 34 "Get UserId - No Params (400)" "GET" "/api/v1/auth/user-id" $null $script:adminToken 400

$r = Test 35 "Get UserId - Both Params (400)" "GET" "/api/v1/auth/user-id?patientId=$([Guid]::NewGuid())&doctorId=$([Guid]::NewGuid())" $null $script:adminToken 400

# =============================================
# 10. PATIENTS CRUD (via /api/v1/patients)
# =============================================
Write-Host "`n=== 10. PATIENTS API ===" -ForegroundColor Yellow

$r = Test 36 "List All Patients" "GET" "/api/v1/patients" $null $script:adminToken 200
if ($r) { Write-Host "  Count=$($r.Count)" }

if ($seedPatientId) {
    $r = Test 37 "Get Patient by ID" "GET" "/api/v1/patients/$seedPatientId" $null $script:adminToken 200
    if ($r) { Write-Host "  PatientId=$($r.patientId) | Name=$($r.fullName)" }
} else {
    Write-Host "[37] Get Patient by ID [SKIP]" -ForegroundColor DarkYellow
}

# =============================================
# 11. DOCTORS CRUD (via /api/v1/doctors)
# =============================================
Write-Host "`n=== 11. DOCTORS API ===" -ForegroundColor Yellow

$r = Test 38 "List All Doctors (Admin)" "GET" "/api/v1/doctors" $null $script:adminToken 200
if ($r) { 
    Write-Host "  Count=$($r.Count)"
    if ($r.Count -gt 0) { $script:newDoctorId = $r[0].doctorId; Write-Host "  First DoctorId=$($script:newDoctorId)" }
}

if ($script:newDoctorId) {
    $r = Test 39 "Get Doctor by ID (Admin)" "GET" "/api/v1/doctors/$($script:newDoctorId)" $null $script:adminToken 200
    if ($r) { Write-Host "  DoctorId=$($r.doctorId) | Specialty=$($r.specialty)" }
} else {
    Write-Host "[39] Get Doctor by ID [SKIP]" -ForegroundColor DarkYellow
}

# Re-login doctor for fresh token & get orgId
$dr = Invoke-RestMethod -Method POST -Uri "$BASE/api/v1/auth/login" -ContentType "application/json" -Body '{"email":"dr.hieu@dbh.vn","password":"Doctor@123"}'
if ($dr.token) { $script:doctorToken = $dr.token }

# Doctor token has empty OrganizationId, use orgId query param fallback
$r = Test 40 "Get Doctors in My Org (orgId)" "GET" "/api/v1/doctors/organization/me?orgId=38b767ce-a981-4937-9b54-fa124f9cc27d" $null $script:doctorToken 200
if ($r) { Write-Host "  Doctors in org: $($r.Count)" }

# =============================================
# 12. STAFF CRUD (via /api/v1/staff - Admin only)
# =============================================
Write-Host "`n=== 12. STAFF API ===" -ForegroundColor Yellow

$r = Test 41 "List All Staff (Admin)" "GET" "/api/v1/staff" $null $script:adminToken 200
if ($r) { Write-Host "  Count=$($r.Count)" }

$r = Test 42 "List Staff - No Auth (401)" "GET" "/api/v1/staff" $null $null 401

$r = Test 43 "List Staff - Patient (403)" "GET" "/api/v1/staff" $null $script:patientToken 403

# =============================================
# SUMMARY
# =============================================
Write-Host "`n============================================" -ForegroundColor Cyan
Write-Host " TOTAL: $total tests | PASS: $pass | FAIL: $fail" -ForegroundColor $(if ($fail -eq 0) { "Green" } else { "Red" })
Write-Host "============================================`n" -ForegroundColor Cyan
