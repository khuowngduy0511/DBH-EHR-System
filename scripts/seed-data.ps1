# =============================================================================
# DBH-EHR Full Seed Data Script (All fields populated - NO nulls)
# Run after: docker compose -f docker-compose.dev.yml up -d
# Idempotent: safe to re-run (existing records will return error and continue)
# NOTE: Does NOT seed PayOS payment config - manual setup required
#
# Coverage: Auth, Organization, Department, Membership, Appointment (full lifecycle),
#   Encounter, EHR (create + version), Consent (grant + revoke), Access Requests,
#   Audit Logs, Notifications (individual + broadcast + preferences + device tokens),
#   Invoices (create + pay cash + cancel) across 3 organizations
# =============================================================================

$BASE  = "http://127.0.0.1:5000"
$AUTH   = "$BASE/api/v1/auth"
$ORG    = "$BASE/api/v1/organizations"
$DEPT   = "$BASE/api/v1/departments"
$MEMB   = "$BASE/api/v1/memberships"
$APTU   = "$BASE/api/v1/appointments"
$ENCU   = "$BASE/api/v1/encounters"
$EHRU   = "$BASE/api/v1/ehr/records"
$CONSU  = "$BASE/api/v1/consents"
$AUDIT  = "$BASE/api/v1/audit"
$NOTIF  = "$BASE/api/v1/notifications"
$INVOICE = "$BASE/api/v1/invoices"

function Api($method, $url, $body = $null, $token = $null, $extraHeaders = @{}) {
    $h = @{ "Content-Type" = "application/json" }
    if ($token) { $h["Authorization"] = "Bearer $token" }
    foreach ($k in $extraHeaders.Keys) { $h[$k] = $extraHeaders[$k] }

    $params = @{ Method = $method; Uri = $url; Headers = $h; ErrorAction = 'SilentlyContinue'; TimeoutSec = 15 }
    if ($body) {
        $json = $body | ConvertTo-Json -Depth 10
        $params["Body"] = $json
    }
    try {
        $resp = Invoke-RestMethod @params
        Start-Sleep -Milliseconds 200
        return $resp
    } catch {
        $code = $_.Exception.Response.StatusCode.value__
        $errMsg = $_.ErrorDetails.Message
        if ($errMsg) {
            try {
                $errBody = $errMsg | ConvertFrom-Json
                $msg = if ($errBody.message) { $errBody.message } elseif ($errBody.title) { $errBody.title } else { $errMsg }
                Write-Host "  [$code] $msg" -ForegroundColor Yellow
                Start-Sleep -Milliseconds 200
                return $errBody
            } catch {
                Write-Host "  [$code] $errMsg" -ForegroundColor Yellow
            }
        } else {
            $exMsg = $_.Exception.Message
            Write-Host "  [$code] $($method) $url -> $exMsg" -ForegroundColor Red
        }
        Start-Sleep -Milliseconds 200
        return $null
    }
}

Write-Host "`n====== FULL SEED DATA - DBH EHR System ======" -ForegroundColor Cyan
Write-Host "  Started at: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor DarkGray

# =============================================================================
# 1. ADMIN ACCOUNT
# =============================================================================
Write-Host "`n--- 1. Admin Account ---" -ForegroundColor Green

# Bootstrap: login with DB-seeded admin to get Admin token
$bootstrapLogin = Api POST "$AUTH/login" @{ email = "admin@dbh.com"; password = "admin123" }
$bootstrapToken = $bootstrapLogin.token
if (-not $bootstrapToken) {
    Write-Host "  FATAL: Cannot login as bootstrap admin (admin@dbh.com). Ensure DB is migrated." -ForegroundColor Red
    exit 1
}
Write-Host "  Bootstrap admin (admin@dbh.com) logged in"

# Register custom admin account
Api POST "$AUTH/register" @{
    fullName = "System Admin"
    email    = "admin@dbh.vn"
    password = "Admin@123456"
    phone    = "0901000001"
} | Out-Null

$loginAdmin = Api POST "$AUTH/login" @{ email = "admin@dbh.vn"; password = "Admin@123456" }
$adminToken = $loginAdmin.token

$adminMe = Api GET "$AUTH/me" $null $adminToken
$adminUserId = $adminMe.userId
Write-Host "  Admin: userId=$adminUserId"

# Use bootstrap token to promote admin@dbh.vn to Admin role
Api PUT "$AUTH/updateRole" @{ userId = "$adminUserId"; newRole = "Admin" } $bootstrapToken | Out-Null
Write-Host "  Role -> Admin (via bootstrap)"

# Re-login to refresh claims with Admin role
$loginAdmin = Api POST "$AUTH/login" @{ email = "admin@dbh.vn"; password = "Admin@123456" }
$adminToken = $loginAdmin.token

# Update admin profile (fill ALL fields)
Api PUT "$AUTH/me/profile" @{
    fullName    = "System Admin"
    phone       = "0901000001"
    gender      = "Male"
    dateOfBirth = "1985-06-15T00:00:00Z"
    address     = "100 Nguyen Du, Quan 1, TP.HCM"
} $adminToken | Out-Null
Write-Host "  Profile updated (all fields)"

# =============================================================================
# 2. PATIENTS (5 patients - all fields filled)
# =============================================================================
Write-Host "`n--- 2. Patients ---" -ForegroundColor Green

$patients = @(
    @{ fullName = "Nguyen Van An";   email = "patient.an@dbh.vn";   phone = "0912000001"; gender = "Male";   dob = "1990-05-15"; address = "12 Le Lai, Quan 1, TP.HCM";          bloodType = "A+" },
    @{ fullName = "Tran Thi Binh";   email = "patient.binh@dbh.vn"; phone = "0912000002"; gender = "Female"; dob = "1985-08-22"; address = "45 Tran Hung Dao, Quan 5, TP.HCM";    bloodType = "B+" },
    @{ fullName = "Le Van Cuong";    email = "patient.cuong@dbh.vn"; phone = "0912000003"; gender = "Male";   dob = "1978-12-01"; address = "78 Hai Ba Trung, Quan 3, TP.HCM";     bloodType = "O+" },
    @{ fullName = "Pham Thi Dung";   email = "patient.dung@dbh.vn";  phone = "0912000004"; gender = "Female"; dob = "1995-03-10"; address = "200 Vo Van Tan, Quan 3, TP.HCM";      bloodType = "AB+" },
    @{ fullName = "Hoang Van Em";    email = "patient.em@dbh.vn";    phone = "0912000005"; gender = "Male";   dob = "2000-07-28"; address = "33 Nguyen Trai, Quan 1, TP.HCM";      bloodType = "O-" }
)

$patientData = @()
foreach ($p in $patients) {
    Api POST "$AUTH/register" @{
        fullName = $p.fullName
        email    = $p.email
        password = "Patient@123"
        phone    = $p.phone
    } | Out-Null

    $login = Api POST "$AUTH/login" @{ email = $p.email; password = "Patient@123" }
    $token = $login.token

    # Update profile with ALL fields
    Api PUT "$AUTH/me/profile" @{
        fullName    = $p.fullName
        phone       = $p.phone
        gender      = $p.gender
        dateOfBirth = "$($p.dob)T00:00:00Z"
        address     = $p.address
    } $token | Out-Null

    $me = Api GET "$AUTH/me" $null $token
    $userId    = $me.userId
    $patientId = $me.profiles.Patient.patientId
    $userDid   = "did:dbh:patient:$userId"

    $patientData += @{
        userId    = $userId
        patientId = $patientId
        did       = $userDid
        token     = $token
        name      = $p.fullName
        email     = $p.email
        bloodType = $p.bloodType
    }
    Write-Host "  Patient: $($p.fullName) | userId=$userId | patientId=$patientId"
}

# =============================================================================
# 3. DOCTORS (4 doctors - all fields filled)
# =============================================================================
Write-Host "`n--- 3. Doctors ---" -ForegroundColor Green

$doctors = @(
    @{ fullName = "BS. Tran Minh Hieu";  email = "dr.hieu@dbh.vn";  phone = "0913000001"; gender = "Male";   dob = "1980-03-20"; address = "50 Pasteur, Quan 1, TP.HCM";           specialty = "Noi khoa";  license = "VN-DOC-001" },
    @{ fullName = "BS. Nguyen Thi Lan";  email = "dr.lan@dbh.vn";   phone = "0913000002"; gender = "Female"; dob = "1982-07-11"; address = "99 Nam Ky Khoi Nghia, Quan 3, TP.HCM"; specialty = "Tim mach"; license = "VN-DOC-002" },
    @{ fullName = "BS. Le Van Phuoc";    email = "dr.phuoc@dbh.vn"; phone = "0913000003"; gender = "Male";   dob = "1979-11-05"; address = "15 Cach Mang Thang 8, Quan 10, TP.HCM"; specialty = "Nhi khoa"; license = "VN-DOC-003" },
    @{ fullName = "BS. Pham Anh Tuan";   email = "dr.tuan@dbh.vn";  phone = "0913000004"; gender = "Male";   dob = "1983-01-30"; address = "88 Ly Tu Trong, Quan 1, TP.HCM";       specialty = "Ngoai khoa"; license = "VN-DOC-004" }
)

$doctorData = @()
foreach ($d in $doctors) {
    Api POST "$AUTH/registerStaffDoctor" @{
        fullName       = $d.fullName
        email          = $d.email
        password       = "Doctor@123"
        phone          = $d.phone
        role           = "Doctor"
        gender         = $d.gender
        dateOfBirth    = "$($d.dob)T00:00:00Z"
        address        = $d.address
    } $adminToken | Out-Null

    $login = Api POST "$AUTH/login" @{ email = $d.email; password = "Doctor@123" }
    $token = $login.token

    # Update profile to ensure ALL fields
    Api PUT "$AUTH/me/profile" @{
        fullName    = $d.fullName
        phone       = $d.phone
        gender      = $d.gender
        dateOfBirth = "$($d.dob)T00:00:00Z"
        address     = $d.address
    } $token | Out-Null

    $me = Api GET "$AUTH/me" $null $token
    $userId   = $me.userId
    $doctorId = $me.profiles.Doctor.doctorId

    $doctorData += @{
        userId    = $userId
        doctorId  = $doctorId
        did       = "did:dbh:doctor:$userId"
        token     = $token
        name      = $d.fullName
        email     = $d.email
        specialty = $d.specialty
        license   = $d.license
    }
    Write-Host "  Doctor: $($d.fullName) ($($d.specialty)) | userId=$userId | doctorId=$doctorId"
}

# =============================================================================
# 4. STAFF (2 nurses, 1 receptionist, 1 pharmacist, 1 labtech)
# =============================================================================
Write-Host "`n--- 4. Staff ---" -ForegroundColor Green

$staffList = @(
    @{ fullName = "DD. Vo Thi Hoa";       email = "nurse.hoa@dbh.vn";         phone = "0914000001"; gender = "Female"; dob = "1992-04-18"; address = "22 Le Van Sy, Quan 3, TP.HCM";        role = "Nurse" },
    @{ fullName = "DD. Bui Van Khanh";     email = "nurse.khanh@dbh.vn";       phone = "0914000002"; gender = "Male";   dob = "1990-09-25"; address = "67 Nguyen Dinh Chieu, Quan 3, TP.HCM"; role = "Nurse" },
    @{ fullName = "LT. Do Van Minh";       email = "receptionist.minh@dbh.vn"; phone = "0914000003"; gender = "Male";   dob = "1994-06-12"; address = "34 Pham Ngoc Thach, Quan 3, TP.HCM";   role = "Receptionist" },
    @{ fullName = "DS. Ngo Thi Oanh";      email = "pharmacist.oanh@dbh.vn";   phone = "0914000004"; gender = "Female"; dob = "1991-02-08"; address = "56 Dien Bien Phu, Quan Binh Thanh";    role = "Pharmacist" },
    @{ fullName = "KTV. Nguyen Minh Tuan"; email = "labtech.tuan@dbh.vn";      phone = "0914000005"; gender = "Male";   dob = "1993-10-14"; address = "89 Xo Viet Nghe Tinh, Binh Thanh";    role = "LabTech" }
)

$staffData = @()
foreach ($s in $staffList) {
    Api POST "$AUTH/registerStaffDoctor" @{
        fullName       = $s.fullName
        email          = $s.email
        password       = "Staff@123"
        phone          = $s.phone
        role           = $s.role
        gender         = $s.gender
        dateOfBirth    = "$($s.dob)T00:00:00Z"
        address        = $s.address
    } $adminToken | Out-Null

    $login = Api POST "$AUTH/login" @{ email = $s.email; password = "Staff@123" }
    $token = $login.token

    # Update profile
    Api PUT "$AUTH/me/profile" @{
        fullName    = $s.fullName
        phone       = $s.phone
        gender      = $s.gender
        dateOfBirth = "$($s.dob)T00:00:00Z"
        address     = $s.address
    } $token | Out-Null

    $me = Api GET "$AUTH/me" $null $token
    $userId = $me.userId

    $staffData += @{
        userId = $userId
        token  = $token
        name   = $s.fullName
        role   = $s.role
        did    = "did:dbh:staff:$userId"
    }
    Write-Host "  Staff: $($s.fullName) ($($s.role)) | userId=$userId"
}

# =============================================================================
# 5. ORGANIZATIONS (2 hospitals, 1 clinic - ALL fields filled)
# =============================================================================
Write-Host "`n--- 5. Organizations ---" -ForegroundColor Green

$orgs = @(
    @{
        orgName       = "Benh vien Da khoa Trung uong"
        orgCode       = "BVDKTU"
        orgType       = "HOSPITAL"
        licenseNumber = "BV-HCM-001"
        taxId         = "0301234567"
        address       = '{"line":["215 Hong Bang"],"city":"Ho Chi Minh","district":"Quan 5","country":"VN","postalCode":"700000"}'
        contactInfo   = '{"phone":"028-3855-4269","fax":"028-3855-4270","email":"contact@bvdktu.vn","hotline":"1900-1234"}'
        website       = "https://bvdktu.vn"
    },
    @{
        orgName       = "Benh vien Nhi Dong 1"
        orgCode       = "BVND1"
        orgType       = "HOSPITAL"
        licenseNumber = "BV-HCM-002"
        taxId         = "0301234568"
        address       = '{"line":["341 Su Van Hanh"],"city":"Ho Chi Minh","district":"Quan 10","country":"VN","postalCode":"700000"}'
        contactInfo   = '{"phone":"028-3927-1119","fax":"028-3927-1120","email":"contact@bvnd1.vn","hotline":"1900-5678"}'
        website       = "https://bvnd1.vn"
    },
    @{
        orgName       = "Phong kham Da lieu Sai Gon"
        orgCode       = "PKDLSG"
        orgType       = "CLINIC"
        licenseNumber = "PK-HCM-001"
        taxId         = "0301234569"
        address       = '{"line":["123 Nguyen Hue"],"city":"Ho Chi Minh","district":"Quan 1","country":"VN","postalCode":"700000"}'
        contactInfo   = '{"phone":"028-3821-0000","fax":"028-3821-0001","email":"contact@pkdlsg.vn","hotline":"1900-9012"}'
        website       = "https://pkdlsg.vn"
    }
)

$orgData = @()
foreach ($o in $orgs) {
    $created = Api POST $ORG $o $adminToken
    $orgId = if ($created.data.orgId) { $created.data.orgId } elseif ($created.orgId) { $created.orgId } else { $null }

    # Fallback: if create failed (duplicate), fetch existing org by code
    if (-not $orgId) {
        $existingOrgs = Api GET "$ORG`?search=$($o.orgCode)&pageSize=50" $null $adminToken
        $orgList = if ($existingOrgs.data) { $existingOrgs.data } elseif ($existingOrgs -is [array]) { $existingOrgs } else { @() }
        $match = $orgList | Where-Object { $_.orgCode -eq $o.orgCode } | Select-Object -First 1
        if ($match) { $orgId = $match.orgId }
    }

    $orgData += @{ orgId = $orgId; name = $o.orgName; code = $o.orgCode }
    Write-Host "  Org: $($o.orgName) | orgId=$orgId"
}

# Verify ALL organizations
foreach ($od in $orgData) {
    if ($od.orgId) {
        Api POST "$ORG/$($od.orgId)/verify?verifiedByUserId=$adminUserId" $null $adminToken | Out-Null
        Write-Host "  Verified: $($od.name)"
    }
}

$hospitalAId = $orgData[0].orgId
$hospitalBId = $orgData[1].orgId
$clinicId    = $orgData[2].orgId

# =============================================================================
# 6. DEPARTMENTS (Hospital A: 5 depts, Hospital B: 3 depts, Clinic: 2 depts)
# =============================================================================
Write-Host "`n--- 6. Departments ---" -ForegroundColor Green

$departments = @(
    # Hospital A departments
    @{ orgId = $hospitalAId; departmentName = "Khoa Noi tong hop";    departmentCode = "NTH"; description = "Khoa Noi tong hop kham va dieu tri cac benh noi khoa";       floor = "2"; roomNumbers = "201-210"; phoneExtension = "2001" },
    @{ orgId = $hospitalAId; departmentName = "Khoa Tim mach";        departmentCode = "TM";  description = "Khoa Tim mach chuyen sau chan doan va dieu tri benh tim";    floor = "3"; roomNumbers = "301-308"; phoneExtension = "3001" },
    @{ orgId = $hospitalAId; departmentName = "Khoa Nhi";             departmentCode = "NHI"; description = "Khoa Nhi chuyen dieu tri benh tre em tu so sinh den 16 tuoi"; floor = "4"; roomNumbers = "401-412"; phoneExtension = "4001" },
    @{ orgId = $hospitalAId; departmentName = "Khoa Ngoai tong hop";  departmentCode = "NGH"; description = "Khoa Ngoai tong hop phau thuat va dieu tri ngoai khoa";      floor = "5"; roomNumbers = "501-510"; phoneExtension = "5001" },
    @{ orgId = $hospitalAId; departmentName = "Phong cap cuu";        departmentCode = "CC";  description = "Phong cap cuu 24/7 tiep nhan benh nhan khan cap";            floor = "1"; roomNumbers = "101-106"; phoneExtension = "1001" },
    # Hospital B departments
    @{ orgId = $hospitalBId; departmentName = "Khoa Nhi tong hop";    departmentCode = "NTH-B"; description = "Khoa Nhi tong hop kham va dieu tri tre em";                 floor = "2"; roomNumbers = "B201-B210"; phoneExtension = "2101" },
    @{ orgId = $hospitalBId; departmentName = "Khoa Nhi so sinh";     departmentCode = "NSS-B"; description = "Khoa Nhi so sinh cham soc tre so sinh non thang";           floor = "3"; roomNumbers = "B301-B306"; phoneExtension = "3101" },
    @{ orgId = $hospitalBId; departmentName = "Khoa Cap cuu Nhi";     departmentCode = "CCN-B"; description = "Khoa Cap cuu Nhi tiep nhan tre em khan cap";                floor = "1"; roomNumbers = "B101-B104"; phoneExtension = "1101" },
    # Clinic departments
    @{ orgId = $clinicId;    departmentName = "Phong kham Da lieu";    departmentCode = "DL";  description = "Phong kham Da lieu chuyen tri mun, nam, vay nen";            floor = "1"; roomNumbers = "C101-C104"; phoneExtension = "101" },
    @{ orgId = $clinicId;    departmentName = "Phong kham Tham my";    departmentCode = "TM-C"; description = "Phong kham Tham my da bang laser va cong nghe cao";          floor = "2"; roomNumbers = "C201-C203"; phoneExtension = "201" }
)

$deptData = @()
$existingDeptCache = @{}
foreach ($d in $departments) {
    $created = Api POST $DEPT $d $adminToken
    $deptId = if ($created.data.departmentId) { $created.data.departmentId } elseif ($created.departmentId) { $created.departmentId } else { $null }

    # Fallback: if create failed (duplicate), fetch existing dept by org + code
    if (-not $deptId -and $d.orgId) {
        $cacheKey = "$($d.orgId)"
        if (-not $existingDeptCache.ContainsKey($cacheKey)) {
            $existingDepts = Api GET "$DEPT/by-organization/$($d.orgId)?pageSize=50" $null $adminToken
            $existingDeptCache[$cacheKey] = if ($existingDepts.data) { $existingDepts.data } elseif ($existingDepts -is [array]) { $existingDepts } else { @() }
        }
        $match = $existingDeptCache[$cacheKey] | Where-Object { $_.departmentCode -eq $d.departmentCode } | Select-Object -First 1
        if ($match) { $deptId = $match.departmentId }
    }

    $deptData += @{ deptId = $deptId; name = $d.departmentName; orgId = $d.orgId }
    Write-Host "  Dept: $($d.departmentName) | deptId=$deptId"
}

# =============================================================================
# 7. MEMBERSHIPS (assign doctors & staff to hospitals - ALL fields filled)
# =============================================================================
Write-Host "`n--- 7. Memberships ---" -ForegroundColor Green

# Hospital A memberships - Doctors 0,1 and Staff 0,2,3
$hospitalAMemberships = @(
    @{ userId = $doctorData[0].userId; deptIdx = 0; empId = "EMP-DOC-001"; jobTitle = "Bac si dieu tri Noi khoa";    license = $doctorData[0].license; specialty = $doctorData[0].specialty; qualifications = '["Dai hoc Y Duoc TP.HCM","Thac si Noi khoa"]';    notes = "Bac si chinh Khoa Noi" },
    @{ userId = $doctorData[1].userId; deptIdx = 1; empId = "EMP-DOC-002"; jobTitle = "Bac si chuyen khoa Tim mach"; license = $doctorData[1].license; specialty = $doctorData[1].specialty; qualifications = '["Dai hoc Y Ha Noi","Tien si Tim mach"]';          notes = "Truong khoa Tim mach" },
    @{ userId = $staffData[0].userId;  deptIdx = 0; empId = "EMP-STF-001"; jobTitle = "Dieu duong truong Khoa Noi";  license = "DD-001";               specialty = "Dieu duong";             qualifications = '["Cu nhan Dieu duong","Chung chi ICU"]';          notes = "Dieu duong truong" },
    @{ userId = $staffData[2].userId;  deptIdx = 4; empId = "EMP-STF-003"; jobTitle = "Nhan vien tiep nhan";         license = "LT-001";               specialty = "Tiep nhan";              qualifications = '["Trung cap Y","Chung chi tiep nhan benh nhan"]'; notes = "Le tan phong cap cuu" },
    @{ userId = $staffData[3].userId;  deptIdx = 0; empId = "EMP-STF-004"; jobTitle = "Duoc si lam sang";            license = "DS-001";               specialty = "Duoc lam sang";          qualifications = '["Dai hoc Duoc","Chung chi Duoc lam sang"]';     notes = "Duoc si Khoa Noi" }
)

foreach ($m in $hospitalAMemberships) {
    $deptId = if ($m.deptIdx -ne $null -and $m.deptIdx -lt $deptData.Count) { $deptData[$m.deptIdx].deptId } else { $null }
    $membership = @{
        userId         = $m.userId
        orgId          = $hospitalAId
        departmentId   = $deptId
        employeeId     = $m.empId
        jobTitle       = $m.jobTitle
        licenseNumber  = $m.license
        specialty      = $m.specialty
        qualifications = $m.qualifications
        startDate      = "2024-01-15"
        orgPermissions = '["VIEW_PATIENTS","CREATE_RECORDS"]'
        notes          = $m.notes
    }
    $created = Api POST $MEMB $membership $adminToken
    $memId = if ($created.data.membershipId) { $created.data.membershipId } elseif ($created.membershipId) { $created.membershipId } else { $null }
    Write-Host "  Member: Hospital A | empId=$($m.empId) | memId=$memId"
}

# Hospital B memberships - Doctors 2,3 and Staff 1,4
$hospitalBMemberships = @(
    @{ userId = $doctorData[2].userId; deptIdx = 5; empId = "EMP-DOC-B01"; jobTitle = "Bac si dieu tri Nhi khoa";   license = $doctorData[2].license; specialty = $doctorData[2].specialty; qualifications = '["Dai hoc Y Duoc TP.HCM","Chuyen khoa I Nhi"]';  notes = "Bac si chinh Khoa Nhi" },
    @{ userId = $doctorData[3].userId; deptIdx = 7; empId = "EMP-DOC-B02"; jobTitle = "Bac si phau thuat Nhi";      license = $doctorData[3].license; specialty = $doctorData[3].specialty; qualifications = '["Dai hoc Y Ha Noi","Chuyen khoa II Ngoai Nhi"]'; notes = "Truong khoa Cap cuu Nhi" },
    @{ userId = $staffData[1].userId;  deptIdx = 5; empId = "EMP-STF-B01"; jobTitle = "Dieu duong Nhi khoa";        license = "DD-B01";               specialty = "Dieu duong Nhi";         qualifications = '["Cu nhan Dieu duong","Chung chi Nhi khoa"]';    notes = "Dieu duong Khoa Nhi" },
    @{ userId = $staffData[4].userId;  deptIdx = 6; empId = "EMP-STF-B02"; jobTitle = "Ky thuat vien xet nghiem";   license = "KTV-B01";              specialty = "Xet nghiem";             qualifications = '["Cu nhan Xet nghiem","Chung chi Huyet hoc"]';   notes = "KTV Khoa So sinh" }
)

foreach ($m in $hospitalBMemberships) {
    $deptId = if ($m.deptIdx -ne $null -and $m.deptIdx -lt $deptData.Count) { $deptData[$m.deptIdx].deptId } else { $null }
    $membership = @{
        userId         = $m.userId
        orgId          = $hospitalBId
        departmentId   = $deptId
        employeeId     = $m.empId
        jobTitle       = $m.jobTitle
        licenseNumber  = $m.license
        specialty      = $m.specialty
        qualifications = $m.qualifications
        startDate      = "2024-02-01"
        orgPermissions = '["VIEW_PATIENTS","CREATE_RECORDS"]'
        notes          = $m.notes
    }
    $created = Api POST $MEMB $membership $adminToken
    $memId = if ($created.data.membershipId) { $created.data.membershipId } elseif ($created.membershipId) { $created.membershipId } else { $null }
    Write-Host "  Member: Hospital B | empId=$($m.empId) | memId=$memId"
}

# Clinic memberships (Dr Hieu part-time + Receptionist)
$clinicMemberships = @(
    @{ userId = $doctorData[0].userId; deptIdx = 8; empId = "EMP-DOC-C01"; jobTitle = "Bac si tu van Da lieu";  license = $doctorData[0].license; specialty = "Da lieu";  qualifications = '["Dai hoc Y Duoc TP.HCM","Chung chi Da lieu co ban"]'; notes = "Bac si ban thoi gian tai phong kham" },
    @{ userId = $staffData[2].userId;  deptIdx = 8; empId = "EMP-STF-C01"; jobTitle = "Nhan vien tiep nhan";  license = "LT-C01";              specialty = "Tiep nhan"; qualifications = '["Trung cap Y"]';                                           notes = "Le tan phong kham" }
)

foreach ($m in $clinicMemberships) {
    $deptId = if ($m.deptIdx -ne $null -and $m.deptIdx -lt $deptData.Count) { $deptData[$m.deptIdx].deptId } else { $null }
    $membership = @{
        userId         = $m.userId
        orgId          = $clinicId
        departmentId   = $deptId
        employeeId     = $m.empId
        jobTitle       = $m.jobTitle
        licenseNumber  = $m.license
        specialty      = $m.specialty
        qualifications = $m.qualifications
        startDate      = "2024-03-01"
        orgPermissions = '["VIEW_PATIENTS","CREATE_RECORDS"]'
        notes          = $m.notes
    }
    $created = Api POST $MEMB $membership $adminToken
    $memId = if ($created.data.membershipId) { $created.data.membershipId } elseif ($created.membershipId) { $created.membershipId } else { $null }
    Write-Host "  Member: Clinic | empId=$($m.empId) | memId=$memId"
}
Write-Host "  Total memberships: 11 (5 HospA + 4 HospB + 2 Clinic)"

# =============================================================================
# 8. APPOINTMENTS (various pairings across all organizations)
# =============================================================================
Write-Host "`n--- 8. Appointments ---" -ForegroundColor Green

$appointmentData = @()
$aptPairs = @(
    # Hospital A appointments (doctors 0,1)
    @{ patIdx = 0; docIdx = 0; orgId = $hospitalAId; daysFromNow = 1;  isPast = $false },  # An -> Dr Hieu
    @{ patIdx = 1; docIdx = 1; orgId = $hospitalAId; daysFromNow = 2;  isPast = $false },  # Binh -> Dr Lan
    @{ patIdx = 3; docIdx = 0; orgId = $hospitalAId; daysFromNow = 3;  isPast = $false },  # Dung -> Dr Hieu
    @{ patIdx = 0; docIdx = 1; orgId = $hospitalAId; daysFromNow = 5;  isPast = $false },  # An -> Dr Lan (2nd)
    @{ patIdx = 4; docIdx = 0; orgId = $hospitalAId; daysFromNow = 2;  isPast = $false },  # Em -> Dr Hieu
    # Hospital B appointments (doctors 2,3)
    @{ patIdx = 2; docIdx = 2; orgId = $hospitalBId; daysFromNow = 1;  isPast = $false },  # Cuong -> Dr Phuoc
    @{ patIdx = 4; docIdx = 3; orgId = $hospitalBId; daysFromNow = 3;  isPast = $false },  # Em -> Dr Tuan
    # Clinic appointments (Dr Hieu part-time)
    @{ patIdx = 3; docIdx = 0; orgId = $clinicId;    daysFromNow = 4;  isPast = $false },  # Dung -> Dr Hieu @Clinic
    @{ patIdx = 1; docIdx = 0; orgId = $clinicId;    daysFromNow = 6;  isPast = $false },  # Binh -> Dr Hieu @Clinic
    # Past simulated (for encounters/EHR)
    @{ patIdx = 0; docIdx = 0; orgId = $hospitalAId; daysFromNow = -3; isPast = $true },   # An -> Dr Hieu (past)
    @{ patIdx = 1; docIdx = 1; orgId = $hospitalAId; daysFromNow = -5; isPast = $true },   # Binh -> Dr Lan (past)
    @{ patIdx = 2; docIdx = 2; orgId = $hospitalBId; daysFromNow = -2; isPast = $true }    # Cuong -> Dr Phuoc (past at Hospital B)
)

$aptIdx = 0
foreach ($pair in $aptPairs) {
    Start-Sleep -Milliseconds 400
    # Use 14:00 for "past" appointments, 9:00+ for future (avoid time conflicts on same day)
    $minuteOffset = $aptIdx * 15
    $createDate = if ($pair.isPast) {
        (Get-Date).AddDays(1).AddHours(14).AddMinutes($minuteOffset).ToString("yyyy-MM-ddTHH:mm:ssZ")
    } else {
        (Get-Date).AddDays($pair.daysFromNow).AddHours(9).AddMinutes($minuteOffset).ToString("yyyy-MM-ddTHH:mm:ssZ")
    }
    $patientToken = $patientData[$pair.patIdx].token

    $aptResult = Api POST $APTU @{
        patientId   = $patientData[$pair.patIdx].patientId
        doctorId    = $doctorData[$pair.docIdx].doctorId
        orgId       = $pair.orgId
        scheduledAt = $createDate
    } $patientToken

    $aptId = if ($aptResult.data.appointmentId) { $aptResult.data.appointmentId } elseif ($aptResult.appointmentId) { $aptResult.appointmentId } else { $null }

    # Fallback: if create failed (re-run), find existing appointment for this patient-doctor pair
    if (-not $aptId -and $patientToken) {
        $existingApts = Api GET "$APTU`?patientId=$($patientData[$pair.patIdx].patientId)&doctorId=$($doctorData[$pair.docIdx].doctorId)&pageSize=20" $null $patientToken
        $aptList = if ($existingApts.data) { $existingApts.data } elseif ($existingApts -is [array]) { $existingApts } else { @() }
        if ($aptList.Count -gt 0) {
            # Pick the first matching appointment (prefer unfinished ones)
            $aptId = $aptList[0].appointmentId
        }
    }

    $appointmentData += @{
        aptId  = $aptId
        patIdx = $pair.patIdx
        docIdx = $pair.docIdx
        orgId  = $pair.orgId
        isPast = $pair.isPast
    }
    $label = if ($pair.isPast) { "(simulated past)" } else { "" }
    Write-Host "  Appointment: $($patientData[$pair.patIdx].name) -> $($doctorData[$pair.docIdx].name) $label | aptId=$aptId"
    $aptIdx++
}

# Confirm most appointments (skip index 4 = Em->Dr Hieu kept PENDING for reject demo)
$confirmCount = 0
for ($ci = 0; $ci -lt $appointmentData.Count; $ci++) {
    $aptItem = $appointmentData[$ci]
    if ($aptItem.aptId -and -not $aptItem.isPast -and $ci -ne 4) {
        $docToken = $doctorData[$aptItem.docIdx].token
        Api PUT "$APTU/$($aptItem.aptId)/confirm" $null $docToken | Out-Null
        $confirmCount++
    }
}
# Also confirm past appointments for encounter flow
foreach ($aptItem in $appointmentData) {
    if ($aptItem.aptId -and $aptItem.isPast) {
        $docToken = $doctorData[$aptItem.docIdx].token
        Api PUT "$APTU/$($aptItem.aptId)/confirm" $null $docToken | Out-Null
    }
}
Write-Host "  Confirmed $confirmCount future + 3 past appointments"

# =============================================================================
# 8b. APPOINTMENT LIFECYCLE (reject, cancel, reschedule)
# =============================================================================
Write-Host "`n--- 8b. Appointment Lifecycle ---" -ForegroundColor Green

# Reject appointment: Em -> Dr Hieu (index 4) - doctor rejects due to schedule conflict
$rejectApt = $appointmentData[4]
if ($rejectApt.aptId) {
    Api PUT "$APTU/$($rejectApt.aptId)/reject" @{ reason = "Bac si bi trung lich hoc thuong xuyen vao ngay nay. Vui long dat lai lich khac." } $doctorData[$rejectApt.docIdx].token | Out-Null
    Write-Host "  Rejected: $($patientData[$rejectApt.patIdx].name) -> $($doctorData[$rejectApt.docIdx].name) (schedule conflict)"
}

# Cancel appointment: An -> Dr Lan 2nd (index 3) - patient cancels
$cancelApt = $appointmentData[3]
if ($cancelApt.aptId) {
    Api PUT "$APTU/$($cancelApt.aptId)/cancel" @{ reason = "Benh nhan co viec dot xuat, xin huy lich hen." } $patientData[$cancelApt.patIdx].token | Out-Null
    Write-Host "  Cancelled: $($patientData[$cancelApt.patIdx].name) -> $($doctorData[$cancelApt.docIdx].name) (patient request)"
}

# Reschedule appointment: Binh -> Dr Lan (index 1) - reschedule to +7 days
$reschedApt = $appointmentData[1]
if ($reschedApt.aptId) {
    $newDate = (Get-Date).AddDays(7).AddHours(10).ToString("yyyy-MM-ddTHH:mm:ssZ")
    Api PUT "$APTU/$($reschedApt.aptId)/reschedule?newDate=$newDate" $null $patientData[$reschedApt.patIdx].token | Out-Null
    Write-Host "  Rescheduled: $($patientData[$reschedApt.patIdx].name) -> $($doctorData[$reschedApt.docIdx].name) (moved +7 days)"
}

# =============================================================================
# 9. ENCOUNTERS & EHR (for past appointments)
# =============================================================================
Write-Host "`n--- 9. Encounters & EHR ---" -ForegroundColor Green

$encounterData = @()
$ehrIds = @()

$ehrDataTemplates = @(
    @{
        condition = "Viem hong cap tinh"
        condCode  = "J02.9"
        medication = "Amoxicillin 500mg"
        medDose   = "Uong 3 lan/ngay sau an, 7 ngay"
        temp      = 37.5
        bp        = 120
        hr        = 80
        spO2      = 98
        weight    = 65
        height    = 170
        notes     = "Hoan tat kham. Chan doan: Viem hong cap. Phac do: Amoxicillin 500mg x 7 ngay. Tai kham sau 7 ngay."
    },
    @{
        condition = "Tang huyet ap giai doan 1"
        condCode  = "I10"
        medication = "Amlodipine 5mg"
        medDose   = "Uong 1 lan/ngay buoi sang"
        temp      = 36.8
        bp        = 145
        hr        = 88
        spO2      = 97
        weight    = 72
        height    = 165
        notes     = "Hoan tat kham. Chan doan: Tang huyet ap giai doan 1. Ke don Amlodipine 5mg. Hen tai kham 1 thang."
    },
    @{
        condition = "Viem phe quan cap o tre em"
        condCode  = "J20.9"
        medication = "Salbutamol khí dung 2.5mg"
        medDose   = "Khi dung 3 lan/ngay"
        temp      = 38.2
        bp        = 90
        hr        = 110
        spO2      = 95
        weight    = 18
        height    = 105
        notes     = "Hoan tat kham. Chan doan: Viem phe quan cap. Khi dung Salbutamol + theo doi SpO2. Tai kham 3 ngay."
    }
)

$pastIdx = 0
foreach ($aptItem in $appointmentData) {
    if ($aptItem.isPast -and $aptItem.aptId) {
        $docToken  = $doctorData[$aptItem.docIdx].token
        $patientId = $patientData[$aptItem.patIdx].patientId
        $doctorId  = $doctorData[$aptItem.docIdx].doctorId
        $patientName = $patientData[$aptItem.patIdx].name
        $tpl = $ehrDataTemplates[$pastIdx % $ehrDataTemplates.Count]

        # Check-in
        Api PUT "$APTU/$($aptItem.aptId)/check-in" $null $docToken | Out-Null
        Write-Host "  Checked in: $patientName"

        # Create encounter
        $encResult = Api POST $ENCU @{
            patientId     = "$patientId"
            doctorId      = "$doctorId"
            appointmentId = "$($aptItem.aptId)"
            orgId         = "$($aptItem.orgId)"
            notes         = "Kham benh cho $patientName - $($tpl.condition)"
        } $docToken

        $encId = if ($encResult.data.encounterId) { $encResult.data.encounterId } elseif ($encResult.encounterId) { $encResult.encounterId } else { $null }
        $encounterData += @{ encId = $encId; aptItem = $aptItem }
        Write-Host "  Encounter: $patientName | encId=$encId"

        # Complete encounter with rich EHR data
        $ehrData = @{
            resourceType = "Bundle"
            type = "document"
            entry = @(
                @{
                    resource = @{
                        resourceType   = "Condition"
                        code           = @{ text = $tpl.condition; coding = @(@{ system = "http://hl7.org/fhir/sid/icd-10"; code = $tpl.condCode; display = $tpl.condition }) }
                        clinicalStatus = @{ coding = @(@{ system = "http://terminology.hl7.org/CodeSystem/condition-clinical"; code = "active"; display = "Active" }) }
                        severity       = @{ coding = @(@{ system = "http://snomed.info/sct"; code = "24484000"; display = "Severe" }) }
                        note           = @(@{ text = "Benh nhan den kham voi trieu chung ro rang" })
                    }
                },
                @{
                    resource = @{
                        resourceType               = "MedicationRequest"
                        status                     = "active"
                        intent                     = "order"
                        medicationCodeableConcept  = @{ text = $tpl.medication; coding = @(@{ system = "http://www.nlm.nih.gov/research/umls/rxnorm"; display = $tpl.medication }) }
                        dosageInstruction           = @(@{ text = $tpl.medDose; timing = @{ repeat = @{ frequency = 3; period = 1; periodUnit = "d" } }; route = @{ text = "Uong" } })
                        dispenseRequest             = @{ numberOfRepeatsAllowed = 0; quantity = @{ value = 21; unit = "vien" }; expectedSupplyDuration = @{ value = 7; unit = "ngay" } }
                    }
                },
                @{
                    resource = @{
                        resourceType = "Observation"
                        status       = "final"
                        code         = @{ text = "Vital Signs" }
                        component    = @(
                            @{ code = @{ text = "Nhiet do co the" };    valueQuantity = @{ value = $tpl.temp;   unit = "°C";   system = "http://unitsofmeasure.org"; code = "Cel" } },
                            @{ code = @{ text = "Huyet ap tam thu" };   valueQuantity = @{ value = $tpl.bp;     unit = "mmHg"; system = "http://unitsofmeasure.org"; code = "mm[Hg]" } },
                            @{ code = @{ text = "Nhip tim" };           valueQuantity = @{ value = $tpl.hr;     unit = "bpm";  system = "http://unitsofmeasure.org"; code = "/min" } },
                            @{ code = @{ text = "SpO2" };               valueQuantity = @{ value = $tpl.spO2;   unit = "%";    system = "http://unitsofmeasure.org"; code = "%" } },
                            @{ code = @{ text = "Can nang" };           valueQuantity = @{ value = $tpl.weight; unit = "kg";   system = "http://unitsofmeasure.org"; code = "kg" } },
                            @{ code = @{ text = "Chieu cao" };          valueQuantity = @{ value = $tpl.height; unit = "cm";   system = "http://unitsofmeasure.org"; code = "cm" } }
                        )
                    }
                },
                @{
                    resource = @{
                        resourceType = "AllergyIntolerance"
                        clinicalStatus = @{ coding = @(@{ code = "active" }) }
                        type         = "allergy"
                        category     = @("medication")
                        code         = @{ text = "Penicillin" }
                        reaction     = @(@{ manifestation = @(@{ text = "Phat ban da" }); severity = "mild" })
                    }
                }
            )
        }

        if ($encId) {
            $complete = Api PUT "$ENCU/$encId/complete" @{
                notes   = $tpl.notes
                ehrData = $ehrData
            } $docToken
            Write-Host "  Completed encounter with EHR"
        }

        $pastIdx++
    }
}

# =============================================================================
# 10. STANDALONE EHR RECORDS (additional records with rich data)
# =============================================================================
Write-Host "`n--- 10. Standalone EHR Records ---" -ForegroundColor Green

$standaloneEhr = @(
    @{ patIdx = 0; docIdx = 0; orgId = $hospitalAId; condition = "Kham suc khoe tong quat"; bmi = 22.5; bp = 118; hr = 72 },
    @{ patIdx = 1; docIdx = 1; orgId = $hospitalAId; condition = "Kham tim mach dinh ky";   bmi = 25.1; bp = 130; hr = 85 },
    @{ patIdx = 2; docIdx = 2; orgId = $hospitalBId; condition = "Kham Nhi tong quat";       bmi = 16.2; bp = 88;  hr = 95 },
    @{ patIdx = 3; docIdx = 0; orgId = $hospitalAId; condition = "Kham noi khoa dinh ky";   bmi = 21.8; bp = 115; hr = 68 },
    @{ patIdx = 4; docIdx = 3; orgId = $hospitalBId; condition = "Kham truoc phau thuat";    bmi = 20.3; bp = 122; hr = 76 }
)

foreach ($ehr in $standaloneEhr) {
    $docToken  = $doctorData[$ehr.docIdx].token
    $patientId = $patientData[$ehr.patIdx].patientId
    $doctorId  = $doctorData[$ehr.docIdx].doctorId

    if (-not $docToken) {
        Write-Host "  SKIP EHR: $($patientData[$ehr.patIdx].name) - no doctor token" -ForegroundColor Yellow
        continue
    }
    if (-not $ehr.orgId) {
        Write-Host "  SKIP EHR: $($patientData[$ehr.patIdx].name) - no orgId" -ForegroundColor Yellow
        continue
    }

    $ehrReq = @{
        patientId = "$patientId"
        orgId     = "$($ehr.orgId)"
        data      = @{
            resourceType = "Bundle"
            type = "document"
            entry = @(
                @{
                    resource = @{
                        resourceType   = "Condition"
                        code           = @{ text = $ehr.condition }
                        clinicalStatus = @{ coding = @(@{ code = "resolved"; display = "Resolved" }) }
                        note           = @(@{ text = "Ket qua kham binh thuong" })
                    }
                },
                @{
                    resource = @{
                        resourceType = "Observation"
                        status = "final"
                        code = @{ text = "BMI" }
                        valueQuantity = @{ value = $ehr.bmi; unit = "kg/m2"; system = "http://unitsofmeasure.org"; code = "kg/m2" }
                    }
                },
                @{
                    resource = @{
                        resourceType = "Observation"
                        status = "final"
                        code = @{ text = "Vital Signs" }
                        component = @(
                            @{ code = @{ text = "Huyet ap" }; valueQuantity = @{ value = $ehr.bp; unit = "mmHg" } },
                            @{ code = @{ text = "Nhip tim" }; valueQuantity = @{ value = $ehr.hr; unit = "bpm" } }
                        )
                    }
                }
            )
        }
    }

    $ehrResult = Api POST $EHRU $ehrReq $docToken @{ "X-Doctor-Id" = "$doctorId" }
    $ehrId = if ($ehrResult.data.ehrId) { $ehrResult.data.ehrId } elseif ($ehrResult.ehrId) { $ehrResult.ehrId } else { $null }
    if ($ehrId) { $ehrIds += $ehrId }
    Write-Host "  EHR: $($patientData[$ehr.patIdx].name) - $($ehr.condition) | ehrId=$ehrId"
}

# =============================================================================
# 10b. EHR VERSION UPDATES (update existing records to create versions)
# =============================================================================
Write-Host "`n--- 10b. EHR Version Updates ---" -ForegroundColor Green

if ($ehrIds.Count -gt 0) {
    # Update first standalone EHR to create version 2
    $updateEhrId = $ehrIds[0]
    $ehrUpdate = Api PUT "$EHRU/$updateEhrId" @{
        data = @{
            resourceType = "Bundle"
            type = "document"
            entry = @(
                @{
                    resource = @{
                        resourceType   = "Condition"
                        code           = @{ text = "Kham suc khoe tong quat - Tai kham" }
                        clinicalStatus = @{ coding = @(@{ code = "resolved"; display = "Resolved" }) }
                        note           = @(@{ text = "Tai kham lan 2: Ket qua on dinh, khong phat hien bat thuong" })
                    }
                },
                @{
                    resource = @{
                        resourceType = "Observation"
                        status = "final"
                        code = @{ text = "Vital Signs - Tai kham" }
                        component = @(
                            @{ code = @{ text = "Huyet ap" }; valueQuantity = @{ value = 115; unit = "mmHg" } },
                            @{ code = @{ text = "Nhip tim" }; valueQuantity = @{ value = 70; unit = "bpm" } },
                            @{ code = @{ text = "Can nang" }; valueQuantity = @{ value = 64; unit = "kg" } },
                            @{ code = @{ text = "BMI" };      valueQuantity = @{ value = 22.1; unit = "kg/m2" } }
                        )
                    }
                },
                @{
                    resource = @{
                        resourceType = "DiagnosticReport"
                        status       = "final"
                        code         = @{ text = "Xet nghiem mau tong quat" }
                        conclusion   = "Chi so mau trong gioi han binh thuong. WBC: 7.2, RBC: 4.8, Hb: 14.5, Plt: 250"
                    }
                }
            )
        }
    } $doctorData[0].token
    Write-Host "  Updated EHR (v2): $updateEhrId - Tai kham + Xet nghiem mau"

    # Update second standalone EHR
    if ($ehrIds.Count -gt 1) {
        $updateEhrId2 = $ehrIds[1]
        $ehrUpdate2 = Api PUT "$EHRU/$updateEhrId2" @{
            data = @{
                resourceType = "Bundle"
                type = "document"
                entry = @(
                    @{
                        resource = @{
                            resourceType   = "Condition"
                            code           = @{ text = "Tang huyet ap - Dieu chinh thuoc" }
                            clinicalStatus = @{ coding = @(@{ code = "active"; display = "Active" }) }
                            note           = @(@{ text = "Tang lieu Amlodipine 5mg -> 10mg do huyet ap chua on dinh" })
                        }
                    },
                    @{
                        resource = @{
                            resourceType = "MedicationRequest"
                            status       = "active"
                            intent       = "order"
                            medicationCodeableConcept = @{ text = "Amlodipine 10mg"; coding = @(@{ system = "http://www.nlm.nih.gov/research/umls/rxnorm"; display = "Amlodipine 10mg" }) }
                            dosageInstruction = @(@{ text = "Uong 1 lan/ngay buoi sang, sau an"; route = @{ text = "Uong" } })
                        }
                    }
                )
            }
        } $doctorData[1].token
        Write-Host "  Updated EHR (v2): $updateEhrId2 - Dieu chinh thuoc tang huyet ap"
    }
} else {
    Write-Host "  SKIP: No EHR IDs available for version update" -ForegroundColor Yellow
}

# =============================================================================
# 11. CONSENTS (patients grant access to doctors and orgs)
# =============================================================================
Write-Host "`n--- 11. Consents ---" -ForegroundColor Green

# Patient 0,1,2 grant consent to Dr Hieu (Hospital A) for TREATMENT
for ($i = 0; $i -lt 3; $i++) {
    $patToken = $patientData[$i].token
    if (-not $patToken) {
        Write-Host "  SKIP Consent: $($patientData[$i].name) - no token" -ForegroundColor Yellow
        continue
    }
    $consent = Api POST $CONSU @{
        patientId    = "$($patientData[$i].patientId)"
        patientDid   = "$($patientData[$i].did)"
        granteeId    = "$($doctorData[0].doctorId)"
        granteeDid   = "$($doctorData[0].did)"
        granteeType  = "DOCTOR"
        permission   = "READ"
        purpose      = "TREATMENT"
        durationDays = 90
    } $patToken
    $consentId = if ($consent.data.consentId) { $consent.data.consentId } elseif ($consent.consentId) { $consent.consentId } else { $null }
    Write-Host "  Consent: $($patientData[$i].name) -> $($doctorData[0].name) (READ/TREATMENT) | consentId=$consentId"
}

# Patient 0 grants FULL_ACCESS to Dr Lan for RESEARCH
$consent = Api POST $CONSU @{
    patientId    = "$($patientData[0].patientId)"
    patientDid   = "$($patientData[0].did)"
    granteeId    = "$($doctorData[1].doctorId)"
    granteeDid   = "$($doctorData[1].did)"
    granteeType  = "DOCTOR"
    permission   = "FULL_ACCESS"
    purpose      = "RESEARCH"
    durationDays = 365
} $patientData[0].token
Write-Host "  Consent: $($patientData[0].name) -> $($doctorData[1].name) (FULL_ACCESS/RESEARCH)"

# Patient 0 grants consent to Hospital A (org)
$consent = Api POST $CONSU @{
    patientId    = "$($patientData[0].patientId)"
    patientDid   = "$($patientData[0].did)"
    granteeId    = "$hospitalAId"
    granteeDid   = "did:dbh:org:$hospitalAId"
    granteeType  = "ORGANIZATION"
    permission   = "READ"
    purpose      = "TREATMENT"
    durationDays = 365
} $patientData[0].token
Write-Host "  Consent: $($patientData[0].name) -> Hospital A (ORG/READ)"

# Patient 2 grants consent to Dr Phuoc (Hospital B)
$consent = Api POST $CONSU @{
    patientId    = "$($patientData[2].patientId)"
    patientDid   = "$($patientData[2].did)"
    granteeId    = "$($doctorData[2].doctorId)"
    granteeDid   = "$($doctorData[2].did)"
    granteeType  = "DOCTOR"
    permission   = "READ"
    purpose      = "TREATMENT"
    durationDays = 180
} $patientData[2].token
Write-Host "  Consent: $($patientData[2].name) -> $($doctorData[2].name) (READ/TREATMENT)"

# Patient 4 grants consent to Hospital B
$consent = Api POST $CONSU @{
    patientId    = "$($patientData[4].patientId)"
    patientDid   = "$($patientData[4].did)"
    granteeId    = "$hospitalBId"
    granteeDid   = "did:dbh:org:$hospitalBId"
    granteeType  = "ORGANIZATION"
    permission   = "READ"
    purpose      = "TREATMENT"
    durationDays = 365
} $patientData[4].token
Write-Host "  Consent: $($patientData[4].name) -> Hospital B (ORG/READ)"

# =============================================================================
# 11b. ACCESS REQUESTS (doctor requests access -> patient responds)
# =============================================================================
Write-Host "`n--- 11b. Access Requests ---" -ForegroundColor Green

$ACCESS_REQ = "$BASE/api/v1/access-requests"

# Dr Tuan requests access to Patient Dung's records (no existing consent)
$ar1 = Api POST $ACCESS_REQ @{
    patientId           = "$($patientData[3].patientId)"
    patientDid          = "$($patientData[3].did)"
    requesterId         = "$($doctorData[3].doctorId)"
    requesterDid        = "$($doctorData[3].did)"
    requesterType       = "DOCTOR"
    organizationId      = "$hospitalBId"
    permission          = "READ"
    purpose             = "TREATMENT"
    reason              = "Can xem ho so benh nhan de chuan bi phau thuat. Benh nhan chuyen tu BV khac."
    requestedDurationDays = 30
} $doctorData[3].token
$ar1Id = if ($ar1.data.accessRequestId) { $ar1.data.accessRequestId } elseif ($ar1.data.id) { $ar1.data.id } elseif ($ar1.accessRequestId) { $ar1.accessRequestId } else { $null }
Write-Host "  AccessRequest: $($doctorData[3].name) -> $($patientData[3].name) (TREATMENT) | id=$ar1Id"

# Patient Dung approves the request
if ($ar1Id) {
    Api POST "$ACCESS_REQ/$ar1Id/respond" @{
        approve        = $true
        responseReason = "Dong y cho bac si xem ho so de chuan bi mo."
    } $patientData[3].token | Out-Null
    Write-Host "  Approved: $($patientData[3].name) approved $($doctorData[3].name)'s request"
}

# Dr Lan requests access to Patient Cuong (research)
$ar2 = Api POST $ACCESS_REQ @{
    patientId           = "$($patientData[2].patientId)"
    patientDid          = "$($patientData[2].did)"
    requesterId         = "$($doctorData[1].doctorId)"
    requesterDid        = "$($doctorData[1].did)"
    requesterType       = "DOCTOR"
    organizationId      = "$hospitalAId"
    permission          = "READ"
    purpose             = "RESEARCH"
    reason              = "Nghien cuu lam sang ve benh phe quan o tre em. Can truy cap du lieu ket qua dieu tri."
    requestedDurationDays = 90
} $doctorData[1].token
$ar2Id = if ($ar2.data.accessRequestId) { $ar2.data.accessRequestId } elseif ($ar2.data.id) { $ar2.data.id } elseif ($ar2.accessRequestId) { $ar2.accessRequestId } else { $null }
Write-Host "  AccessRequest: $($doctorData[1].name) -> $($patientData[2].name) (RESEARCH) | id=$ar2Id"

# Patient Cuong denies the research request
if ($ar2Id) {
    Api POST "$ACCESS_REQ/$ar2Id/respond" @{
        approve        = $false
        responseReason = "Toi khong dong y chia se ho so cho muc dich nghien cuu."
    } $patientData[2].token | Out-Null
    Write-Host "  Denied: $($patientData[2].name) denied $($doctorData[1].name)'s research request"
}

# Nurse Hoa requests access to Patient An (nursing care)
$ar3 = Api POST $ACCESS_REQ @{
    patientId           = "$($patientData[0].patientId)"
    patientDid          = "$($patientData[0].did)"
    requesterId         = "$($staffData[0].userId)"
    requesterDid        = "$($staffData[0].did)"
    requesterType       = "NURSE"
    organizationId      = "$hospitalAId"
    permission          = "READ"
    purpose             = "TREATMENT"
    reason              = "Can xem ho so benh nhan de theo doi sinh hieu va cham soc dieu duong."
    requestedDurationDays = 14
} $staffData[0].token
$ar3Id = if ($ar3.data.accessRequestId) { $ar3.data.accessRequestId } elseif ($ar3.data.id) { $ar3.data.id } elseif ($ar3.accessRequestId) { $ar3.accessRequestId } else { $null }
Write-Host "  AccessRequest: $($staffData[0].name) -> $($patientData[0].name) (NURSING) | id=$ar3Id"

# Patient An approves nurse access
if ($ar3Id) {
    Api POST "$ACCESS_REQ/$ar3Id/respond" @{
        approve        = $true
        responseReason = "Dong y cho dieu duong xem ho so."
    } $patientData[0].token | Out-Null
    Write-Host "  Approved: $($patientData[0].name) approved $($staffData[0].name)'s request"
}

# =============================================================================
# 11c. CONSENT REVOCATION (revoke one consent for lifecycle demo)
# =============================================================================
Write-Host "`n--- 11c. Consent Revocation ---" -ForegroundColor Green

# Patient An revokes the RESEARCH consent given to Dr Lan
$patientAnConsents = Api GET "$CONSU/by-patient/$($patientData[0].patientId)" $null $patientData[0].token
$consentList = if ($patientAnConsents.data) { $patientAnConsents.data } elseif ($patientAnConsents -is [array]) { $patientAnConsents } else { @() }
$researchConsent = $consentList | Where-Object { $_.purpose -eq "RESEARCH" -and $_.status -ne "REVOKED" } | Select-Object -First 1
if ($researchConsent) {
    $revokeId = $researchConsent.consentId
    Api POST "$CONSU/$revokeId/revoke" @{
        revokeReason = "Benh nhan thay doi y dinh, khong muon tham gia nghien cuu nua."
    } $patientData[0].token | Out-Null
    Write-Host "  Revoked: $($patientData[0].name) revoked RESEARCH consent to $($doctorData[1].name) | consentId=$revokeId"
} else {
    Write-Host "  SKIP: No RESEARCH consent found to revoke" -ForegroundColor Yellow
}

# =============================================================================
# 12. AUDIT LOGS (comprehensive - all fields filled)
# =============================================================================
Write-Host "`n--- 12. Audit Logs ---" -ForegroundColor Green

$auditLogs = @(
    @{
        actorDid       = $patientData[0].did;    actorUserId = $patientData[0].userId; actorType = "PATIENT"; action = "LOGIN";          targetType = "USER";         result = "SUCCESS"
        ipAddress      = "192.168.1.100"; userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/120.0"; metadata = '{"device":"Desktop","browser":"Chrome","os":"Windows 10"}'
    },
    @{
        actorDid       = $doctorData[0].did;     actorUserId = $doctorData[0].userId;  actorType = "DOCTOR";  action = "LOGIN";          targetType = "USER";         result = "SUCCESS"
        ipAddress      = "192.168.1.50";  userAgent = "Mozilla/5.0 (Macintosh; Intel Mac OS X) Safari/605.1"; metadata = '{"device":"Macbook","browser":"Safari","os":"macOS"}'
    },
    @{
        actorDid       = $doctorData[0].did;     actorUserId = $doctorData[0].userId;  actorType = "DOCTOR";  action = "VIEW";           targetType = "EHR";          result = "SUCCESS"
        patientDid     = $patientData[0].did;    patientId = $patientData[0].patientId; organizationId = $hospitalAId
        ipAddress      = "192.168.1.50";  userAgent = "DBH-EHR-App/1.0"; metadata = '{"ehrId":"viewed","action":"read_full_record"}'
    },
    @{
        actorDid       = $doctorData[0].did;     actorUserId = $doctorData[0].userId;  actorType = "DOCTOR";  action = "CREATE";         targetType = "EHR";          result = "SUCCESS"
        patientDid     = $patientData[0].did;    patientId = $patientData[0].patientId; organizationId = $hospitalAId
        ipAddress      = "192.168.1.50";  userAgent = "DBH-EHR-App/1.0"; metadata = '{"action":"create_ehr_record","condition":"Viem hong cap"}'
    },
    @{
        actorDid       = $patientData[0].did;    actorUserId = $patientData[0].userId; actorType = "PATIENT"; action = "GRANT_CONSENT";  targetType = "CONSENT";      result = "SUCCESS"
        patientDid     = $patientData[0].did;    patientId = $patientData[0].patientId
        ipAddress      = "192.168.1.100"; userAgent = "DBH-Mobile/2.0 Android"; metadata = '{"grantee":"dr.hieu","permission":"READ","purpose":"TREATMENT"}'
    },
    @{
        actorDid       = $doctorData[1].did;     actorUserId = $doctorData[1].userId;  actorType = "DOCTOR";  action = "VIEW";           targetType = "EHR";          result = "DENIED"
        patientDid     = $patientData[3].did;    patientId = $patientData[3].patientId; organizationId = $hospitalAId
        ipAddress      = "192.168.1.51";  userAgent = "DBH-EHR-App/1.0"; errorMessage = "Khong co quyen truy cap ho so benh nhan"; metadata = '{"reason":"no_consent"}'
    },
    @{
        actorDid       = "did:dbh:system";       actorType = "SYSTEM";  action = "VIEW";           targetType = "SYSTEM";       result = "SUCCESS"
        ipAddress      = "127.0.0.1";    userAgent = "DBH-System-Monitor/1.0"; metadata = '{"event":"health_check","services_healthy":8,"uptime_hours":24}'
    },
    @{
        actorDid       = $patientData[1].did;    actorUserId = $patientData[1].userId; actorType = "PATIENT"; action = "DOWNLOAD";       targetType = "EHR";          result = "SUCCESS"
        patientDid     = $patientData[1].did;    patientId = $patientData[1].patientId
        ipAddress      = "10.0.0.55";    userAgent = "DBH-Mobile/2.0 iOS"; metadata = '{"format":"PDF","records_count":2}'
    },
    @{
        actorDid       = $doctorData[2].did;     actorUserId = $doctorData[2].userId;  actorType = "DOCTOR";  action = "CREATE";         targetType = "EHR";          result = "SUCCESS"
        patientDid     = $patientData[2].did;    patientId = $patientData[2].patientId; organizationId = $hospitalBId
        ipAddress      = "192.168.2.30"; userAgent = "DBH-EHR-App/1.0"; metadata = '{"action":"create_pediatric_record","hospital":"Nhi Dong 1"}'
    },
    @{
        actorDid       = $staffData[0].did;      actorUserId = $staffData[0].userId;   actorType = "NURSE";   action = "UPDATE";         targetType = "EHR";          result = "SUCCESS"
        patientDid     = $patientData[0].did;    patientId = $patientData[0].patientId; organizationId = $hospitalAId
        ipAddress      = "192.168.1.60"; userAgent = "DBH-Nursing-Station/1.0"; metadata = '{"action":"update_vitals","temperature":37.2,"bp":"120/80"}'
    }
)

foreach ($log in $auditLogs) {
    Api POST "$AUDIT" $log $adminToken | Out-Null
}
Write-Host "  Created $($auditLogs.Count) audit logs (all fields populated)"

# =============================================================================
# 13. NOTIFICATIONS (all fields filled)
# =============================================================================
Write-Host "`n--- 13. Notifications ---" -ForegroundColor Green

$notifications = @(
    @{
        recipientDid    = $patientData[0].did
        recipientUserId = $patientData[0].userId
        title           = "Lich hen sap toi"
        body            = "Ban co lich kham voi BS. Tran Minh Hieu vao ngay mai luc 9:00 tai Khoa Noi, Phong 205, Tang 2."
        type            = "AppointmentReminder"
        priority        = "High"
        channel         = "InApp"
        referenceId     = "apt-001"
        referenceType   = "Appointment"
        actionUrl       = "/appointments/upcoming"
        data            = '{"doctorName":"BS. Tran Minh Hieu","department":"Khoa Noi","room":"205","time":"09:00"}'
    },
    @{
        recipientDid    = $patientData[1].did
        recipientUserId = $patientData[1].userId
        title           = "Ho so benh an da cap nhat"
        body            = "BS. Nguyen Thi Lan da cap nhat ho so benh an cua ban voi ket qua kham Tim mach ngay hom nay."
        type            = "EhrUpdate"
        priority        = "Normal"
        channel         = "InApp"
        referenceId     = "ehr-update-001"
        referenceType   = "EHR"
        actionUrl       = "/ehr/my-records"
        data            = '{"doctorName":"BS. Nguyen Thi Lan","updateType":"new_diagnosis","condition":"Tang huyet ap"}'
    },
    @{
        recipientDid    = $doctorData[0].did
        recipientUserId = $doctorData[0].userId
        title           = "Yeu cau truy cap ho so"
        body            = "Benh nhan Nguyen Van An da cap quyen truy cap ho so benh an cho ban. Quyen: Xem (READ), Muc dich: Dieu tri."
        type            = "ConsentGranted"
        priority        = "High"
        channel         = "InApp"
        referenceId     = "consent-001"
        referenceType   = "Consent"
        actionUrl       = "/doctor/consents"
        data            = '{"patientName":"Nguyen Van An","permission":"READ","purpose":"TREATMENT","duration":"90 ngay"}'
    },
    @{
        recipientDid    = $patientData[2].did
        recipientUserId = $patientData[2].userId
        title           = "Nhac nho uong thuoc"
        body            = "Hay uong thuoc Salbutamol khi dung theo chi dinh cua BS. Le Van Phuoc. Lieu: 3 lan/ngay."
        type            = "System"
        priority        = "Normal"
        channel         = "InApp"
        referenceId     = "med-reminder-001"
        referenceType   = "MedicationReminder"
        actionUrl       = "/medications"
        data            = '{"medication":"Salbutamol","dosage":"3 lan/ngay","doctorName":"BS. Le Van Phuoc"}'
    },
    @{
        recipientDid    = $patientData[0].did
        recipientUserId = $patientData[0].userId
        title           = "Canh bao bao mat"
        body            = "Tai khoan cua ban vua duoc dang nhap tu thiet bi moi: Windows Desktop, Chrome 120.0. Neu khong phai ban, hay doi mat khau ngay."
        type            = "SecurityAlert"
        priority        = "Urgent"
        channel         = "InApp"
        referenceId     = "security-001"
        referenceType   = "Security"
        actionUrl       = "/settings/security"
        data            = '{"device":"Windows Desktop","browser":"Chrome 120.0","ip":"192.168.1.100","location":"TP.HCM, VN"}'
    },
    @{
        recipientDid    = $doctorData[1].did
        recipientUserId = $doctorData[1].userId
        title           = "Benh nhan moi check-in"
        body            = "Benh nhan Tran Thi Binh da check-in tai Phong 305, Khoa Tim mach. Vui long tien hanh kham."
        type            = "AppointmentCheckedIn"
        priority        = "High"
        channel         = "InApp"
        referenceId     = "checkin-001"
        referenceType   = "Appointment"
        actionUrl       = "/doctor/appointments/today"
        data            = '{"patientName":"Tran Thi Binh","room":"305","department":"Khoa Tim mach"}'
    },
    @{
        recipientDid    = $patientData[3].did
        recipientUserId = $patientData[3].userId
        title           = "Ket qua xet nghiem"
        body            = "Ket qua xet nghiem mau cua ban da co. Vui long lien he BS. Tran Minh Hieu de duoc tu van chi tiet."
        type            = "EhrUpdate"
        priority        = "Normal"
        channel         = "InApp"
        referenceId     = "lab-result-001"
        referenceType   = "LabResult"
        actionUrl       = "/ehr/lab-results"
        data            = '{"testType":"Xet nghiem cong thuc mau","doctorName":"BS. Tran Minh Hieu","status":"completed"}'
    },
    @{
        recipientDid    = $patientData[4].did
        recipientUserId = $patientData[4].userId
        title           = "Lich hen da xac nhan"
        body            = "Lich hen kham voi BS. Pham Anh Tuan tai BV Nhi Dong 1, Khoa Cap cuu Nhi da duoc xac nhan."
        type            = "AppointmentCreated"
        priority        = "Normal"
        channel         = "InApp"
        referenceId     = "apt-confirm-001"
        referenceType   = "Appointment"
        actionUrl       = "/appointments"
        data            = '{"doctorName":"BS. Pham Anh Tuan","hospital":"BV Nhi Dong 1","department":"Khoa Cap cuu Nhi"}'
    }
)

foreach ($n in $notifications) {
    Api POST $NOTIF $n $adminToken | Out-Null
}
Write-Host "  Created $($notifications.Count) notifications (all fields populated)"

# =============================================================================
# 13b. NOTIFICATION PREFERENCES (user notification settings)
# =============================================================================
Write-Host "`n--- 13b. Notification Preferences ---" -ForegroundColor Green

$PREF = "$BASE/api/v1/notifications/preferences"

# Patient An: enable all, set quiet hours
Api PUT "$PREF/by-user/$($patientData[0].did)" @{
    ehrAccessEnabled            = $true
    consentRequestEnabled       = $true
    ehrUpdateEnabled            = $true
    appointmentReminderEnabled  = $true
    securityAlertEnabled        = $true
    systemNotificationEnabled   = $true
    pushEnabled                 = $true
    emailEnabled                = $true
    smsEnabled                  = $false
    quietHoursEnabled           = $true
    quietHoursStart             = 2200
    quietHoursEnd               = 700
} $patientData[0].token | Out-Null
Write-Host "  Preferences: $($patientData[0].name) - all ON, quiet 22:00-07:00"

# Patient Binh: disable push, keep email
Api PUT "$PREF/by-user/$($patientData[1].did)" @{
    ehrAccessEnabled            = $true
    consentRequestEnabled       = $true
    ehrUpdateEnabled            = $true
    appointmentReminderEnabled  = $true
    securityAlertEnabled        = $true
    systemNotificationEnabled   = $true
    pushEnabled                 = $false
    emailEnabled                = $true
    smsEnabled                  = $false
    quietHoursEnabled           = $false
} $patientData[1].token | Out-Null
Write-Host "  Preferences: $($patientData[1].name) - push OFF, email ON"

# Dr Hieu: all on, quiet hours during surgery
Api PUT "$PREF/by-user/$($doctorData[0].did)" @{
    ehrAccessEnabled            = $true
    consentRequestEnabled       = $true
    ehrUpdateEnabled            = $true
    appointmentReminderEnabled  = $true
    securityAlertEnabled        = $true
    systemNotificationEnabled   = $true
    pushEnabled                 = $true
    emailEnabled                = $true
    smsEnabled                  = $true
    quietHoursEnabled           = $true
    quietHoursStart             = 2300
    quietHoursEnd               = 600
} $doctorData[0].token | Out-Null
Write-Host "  Preferences: $($doctorData[0].name) - all ON + SMS, quiet 23:00-06:00"

# Dr Lan: minimal notifications
Api PUT "$PREF/by-user/$($doctorData[1].did)" @{
    ehrAccessEnabled            = $true
    consentRequestEnabled       = $true
    ehrUpdateEnabled            = $false
    appointmentReminderEnabled  = $true
    securityAlertEnabled        = $true
    systemNotificationEnabled   = $false
    pushEnabled                 = $true
    emailEnabled                = $false
    smsEnabled                  = $false
    quietHoursEnabled           = $false
} $doctorData[1].token | Out-Null
Write-Host "  Preferences: $($doctorData[1].name) - essential only"

# =============================================================================
# 13c. DEVICE TOKENS (push notification devices)
# =============================================================================
Write-Host "`n--- 13c. Device Tokens ---" -ForegroundColor Green

$DEVICE = "$BASE/api/v1/notifications/device-tokens"

$deviceTokens = @(
    @{ userDid = $patientData[0].did; userId = $patientData[0].userId; token = $patientData[0].token; fcmToken = "fcm_patient_an_android_$(Get-Date -Format 'yyyyMMdd')";  deviceType = "Android"; deviceName = "Samsung Galaxy S24";  osVersion = "Android 14";     appVersion = "2.1.0" },
    @{ userDid = $patientData[0].did; userId = $patientData[0].userId; token = $patientData[0].token; fcmToken = "fcm_patient_an_web_$(Get-Date -Format 'yyyyMMdd')";      deviceType = "Web";     deviceName = "Chrome Desktop";     osVersion = "Windows 11";     appVersion = "2.1.0" },
    @{ userDid = $patientData[1].did; userId = $patientData[1].userId; token = $patientData[1].token; fcmToken = "fcm_patient_binh_ios_$(Get-Date -Format 'yyyyMMdd')";   deviceType = "iOS";     deviceName = "iPhone 15 Pro";      osVersion = "iOS 17.4";       appVersion = "2.1.0" },
    @{ userDid = $doctorData[0].did;  userId = $doctorData[0].userId;  token = $doctorData[0].token;  fcmToken = "fcm_dr_hieu_ipad_$(Get-Date -Format 'yyyyMMdd')";       deviceType = "iOS";     deviceName = "iPad Pro 12.9";      osVersion = "iPadOS 17.4";    appVersion = "2.1.0" },
    @{ userDid = $doctorData[0].did;  userId = $doctorData[0].userId;  token = $doctorData[0].token;  fcmToken = "fcm_dr_hieu_macbook_$(Get-Date -Format 'yyyyMMdd')";    deviceType = "Web";     deviceName = "Safari MacBook Pro"; osVersion = "macOS 14 Sonoma"; appVersion = "2.1.0" },
    @{ userDid = $staffData[0].did;   userId = $staffData[0].userId;   token = $staffData[0].token;   fcmToken = "fcm_nurse_hoa_android_$(Get-Date -Format 'yyyyMMdd')"; deviceType = "Android"; deviceName = "Nursing Station Tab"; osVersion = "Android 13";    appVersion = "2.0.5" }
)

foreach ($dt in $deviceTokens) {
    Api POST $DEVICE @{
        userDid    = $dt.userDid
        userId     = $dt.userId
        fcmToken   = $dt.fcmToken
        deviceType = $dt.deviceType
        deviceName = $dt.deviceName
        osVersion  = $dt.osVersion
        appVersion = $dt.appVersion
    } $dt.token | Out-Null
}
Write-Host "  Registered $($deviceTokens.Count) device tokens"

# =============================================================================
# 13d. BROADCAST NOTIFICATION (admin system-wide announcement)
# =============================================================================
Write-Host "`n--- 13d. Broadcast Notification ---" -ForegroundColor Green

$allDids = @()
$allDids += $patientData | ForEach-Object { $_.did }
$allDids += $doctorData  | ForEach-Object { $_.did }
$allDids += $staffData   | ForEach-Object { $_.did }

Api POST "$NOTIF/broadcast" @{
    recipientDids = $allDids
    title         = "Thong bao bao tri he thong"
    body          = "He thong DBH-EHR se duoc bao tri vao Chu Nhat 23:00-01:00. Trong thoi gian nay, mot so tinh nang co the tam ngung hoat dong. Vui long luu cong viec truoc 23:00. Xin cam on."
    type          = "System"
    priority      = "High"
} $adminToken | Out-Null
Write-Host "  Broadcast: System maintenance notice -> $($allDids.Count) recipients"

Api POST "$NOTIF/broadcast" @{
    recipientDids = ($doctorData | ForEach-Object { $_.did })
    title         = "Cap nhat huong dan lam sang moi"
    body          = "Bo Y te da ban hanh huong dan lam sang moi ve dieu tri tang huyet ap 2024. Vui long xem tai muc Tai lieu -> Huong dan lam sang."
    type          = "System"
    priority      = "Normal"
} $adminToken | Out-Null
Write-Host "  Broadcast: Clinical guidelines update -> $($doctorData.Count) doctors"

# =============================================================================
# 14. INVOICES (for both hospitals - NO PayOS config needed for cash payments)
# =============================================================================
Write-Host "`n--- 14. Invoices ---" -ForegroundColor Green

# Invoice at Hospital A (by Dr Hieu for Patient An)
if ($hospitalAId -and $doctorData[0].token -and $patientData[0].patientId) {
$inv1 = Api POST $INVOICE @{
    patientId = "$($patientData[0].patientId)"
    orgId     = "$hospitalAId"
    notes     = "Hoa don kham Noi khoa - Benh nhan Nguyen Van An"
    items     = @(
        @{ description = "Phi kham benh noi khoa";         quantity = 1; amount = 200000 },
        @{ description = "Xet nghiem cong thuc mau";       quantity = 1; amount = 150000 },
        @{ description = "Xet nghiem sinh hoa mau";        quantity = 1; amount = 180000 },
        @{ description = "Thuoc Amoxicillin 500mg x 21v";  quantity = 1; amount = 63000 }
    )
} $doctorData[0].token
} else { $inv1 = $null }
$inv1Id = if ($inv1.data.invoiceId) { $inv1.data.invoiceId } elseif ($inv1.invoiceId) { $inv1.invoiceId } else { $null }
Write-Host "  Invoice 1 (Hospital A): $($patientData[0].name) | 593,000 VND | id=$inv1Id"

# Invoice at Hospital A (by Dr Lan for Patient Binh)
if ($hospitalAId -and $doctorData[1].token -and $patientData[1].patientId) {
$inv2 = Api POST $INVOICE @{
    patientId = "$($patientData[1].patientId)"
    orgId     = "$hospitalAId"
    notes     = "Hoa don kham Tim mach - Benh nhan Tran Thi Binh"
    items     = @(
        @{ description = "Phi kham chuyen khoa Tim mach";  quantity = 1; amount = 350000 },
        @{ description = "Dien tam do (ECG)";              quantity = 1; amount = 200000 },
        @{ description = "Sieu am tim (Echocardiogram)";   quantity = 1; amount = 500000 },
        @{ description = "Thuoc Amlodipine 5mg x 30v";     quantity = 1; amount = 90000 }
    )
} $doctorData[1].token
} else { $inv2 = $null }
$inv2Id = if ($inv2.data.invoiceId) { $inv2.data.invoiceId } elseif ($inv2.invoiceId) { $inv2.invoiceId } else { $null }
Write-Host "  Invoice 2 (Hospital A): $($patientData[1].name) | 1,140,000 VND | id=$inv2Id"

# Invoice at Hospital B (by Dr Phuoc for Patient Cuong)
if ($hospitalBId -and $doctorData[2].token -and $patientData[2].patientId) {
$inv3 = Api POST $INVOICE @{
    patientId = "$($patientData[2].patientId)"
    orgId     = "$hospitalBId"
    notes     = "Hoa don kham Nhi khoa - Benh nhan Le Van Cuong"
    items     = @(
        @{ description = "Phi kham Nhi khoa";              quantity = 1; amount = 250000 },
        @{ description = "Khi dung Salbutamol";            quantity = 3; amount = 50000 },
        @{ description = "Chup X-quang phoi";              quantity = 1; amount = 180000 },
        @{ description = "Thuoc ho Dextromethorphan";       quantity = 1; amount = 45000 }
    )
} $doctorData[2].token
} else { $inv3 = $null }
$inv3Id = if ($inv3.data.invoiceId) { $inv3.data.invoiceId } elseif ($inv3.invoiceId) { $inv3.invoiceId } else { $null }
Write-Host "  Invoice 3 (Hospital B): $($patientData[2].name) | 625,000 VND | id=$inv3Id"

# Invoice at Hospital B (by Dr Tuan for Patient Em)
if ($hospitalBId -and $doctorData[3].token -and $patientData[4].patientId) {
$inv4 = Api POST $INVOICE @{
    patientId = "$($patientData[4].patientId)"
    orgId     = "$hospitalBId"
    notes     = "Hoa don kham truoc phau thuat - Benh nhan Hoang Van Em"
    items     = @(
        @{ description = "Phi kham truoc phau thuat";      quantity = 1; amount = 300000 },
        @{ description = "Xet nghiem mau tong quat";       quantity = 1; amount = 250000 },
        @{ description = "Xet nghiem dong mau";            quantity = 1; amount = 200000 },
        @{ description = "Dien tam do (ECG)";              quantity = 1; amount = 200000 },
        @{ description = "Chup X-quang nguc";              quantity = 1; amount = 180000 }
    )
} $doctorData[3].token
} else { $inv4 = $null }
$inv4Id = if ($inv4.data.invoiceId) { $inv4.data.invoiceId } elseif ($inv4.invoiceId) { $inv4.invoiceId } else { $null }
Write-Host "  Invoice 4 (Hospital B): $($patientData[4].name) | 1,130,000 VND | id=$inv4Id"

# Pay Invoice 1 with CASH (no PayOS needed)
if ($inv1Id) {
    $cashPay = Api POST "$INVOICE/$inv1Id/pay-cash" @{
        transactionRef = "CASH-BVDKTU-$(Get-Date -Format 'yyyyMMdd')-001"
    } $adminToken
    Write-Host "  Paid Invoice 1 (CASH): $inv1Id"
}

# Invoice at Clinic (by Dr Hieu part-time for Patient Dung)
if ($clinicId -and $doctorData[0].token -and $patientData[3].patientId) {
$inv5 = Api POST $INVOICE @{
    patientId = "$($patientData[3].patientId)"
    orgId     = "$clinicId"
    notes     = "Hoa don kham Da lieu - Benh nhan Pham Thi Dung"
    items     = @(
        @{ description = "Phi kham Da lieu";               quantity = 1; amount = 300000 },
        @{ description = "Soi da bang dermoscopy";          quantity = 1; amount = 200000 },
        @{ description = "Thuoc boi da Tretinoin 0.05%";    quantity = 1; amount = 85000 }
    )
} $doctorData[0].token
} else { $inv5 = $null }
$inv5Id = if ($inv5.data.invoiceId) { $inv5.data.invoiceId } elseif ($inv5.invoiceId) { $inv5.invoiceId } else { $null }
Write-Host "  Invoice 5 (Clinic): $($patientData[3].name) | 585,000 VND | id=$inv5Id"

# Pay Invoice 5 with CASH
if ($inv5Id) {
    Api POST "$INVOICE/$inv5Id/pay-cash" @{
        transactionRef = "CASH-PKDLSG-$(Get-Date -Format 'yyyyMMdd')-001"
    } $adminToken | Out-Null
    Write-Host "  Paid Invoice 5 (CASH): $inv5Id"
}

# Cancel Invoice 4 (patient cancelled appointment)
if ($inv4Id) {
    Api POST "$INVOICE/$inv4Id/cancel" $null $adminToken | Out-Null
    Write-Host "  Cancelled Invoice 4: $inv4Id (patient cancelled appointment)"
}

# Leave invoices 2, 3 UNPAID for testing PayOS checkout later
Write-Host "  Invoices 2,3 left UNPAID (for PayOS testing)"

# =============================================================================
# SUMMARY
# =============================================================================
Write-Host "`n====== FULL SEED DATA COMPLETE ======" -ForegroundColor Cyan
Write-Host "  Finished at: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor DarkGray
Write-Host ""
Write-Host "  === Data Summary ===" -ForegroundColor Yellow
Write-Host "  Admin:               1"
Write-Host "  Patients:            $($patientData.Count)"
Write-Host "  Doctors:             $($doctorData.Count)"
Write-Host "  Staff:               $($staffData.Count) (2 nurses, 1 receptionist, 1 pharmacist, 1 labtech)"
Write-Host "  Organizations:       $($orgData.Count) (2 hospitals, 1 clinic)"
Write-Host "  Departments:         $($deptData.Count) (5 HospA + 3 HospB + 2 Clinic)"
Write-Host "  Memberships:         11 (5 HospA + 4 HospB + 2 Clinic)"
Write-Host "  Appointments:        $($appointmentData.Count) (9 future + 3 past, incl 1 rejected + 1 cancelled + 1 rescheduled)"
Write-Host "  Encounters:          $($encounterData.Count) (completed with EHR)"
Write-Host "  EHR Records:         $($ehrIds.Count) standalone + EHR version updates"
Write-Host "  Consents:            7 (incl 1 REVOKED)"
Write-Host "  Access Requests:     3 (2 approved, 1 denied)"
Write-Host "  Audit Logs:          $($auditLogs.Count)"
Write-Host "  Notifications:       $($notifications.Count) + 2 broadcasts"
Write-Host "  Notif Preferences:   4 users configured"
Write-Host "  Device Tokens:       $($deviceTokens.Count)"
Write-Host "  Invoices:            5 (2 PAID cash, 1 CANCELLED, 2 UNPAID for PayOS)"
Write-Host ""
Write-Host "  === Test Accounts ===" -ForegroundColor Yellow
Write-Host "  Admin:         admin@dbh.vn              / Admin@123456"
Write-Host "  Patient 1:     patient.an@dbh.vn         / Patient@123"
Write-Host "  Patient 2:     patient.binh@dbh.vn       / Patient@123"
Write-Host "  Patient 3:     patient.cuong@dbh.vn      / Patient@123"
Write-Host "  Patient 4:     patient.dung@dbh.vn       / Patient@123"
Write-Host "  Patient 5:     patient.em@dbh.vn         / Patient@123"
Write-Host "  Doctor 1:      dr.hieu@dbh.vn            / Doctor@123  (Noi khoa, Hospital A + Clinic)"
Write-Host "  Doctor 2:      dr.lan@dbh.vn             / Doctor@123  (Tim mach, Hospital A)"
Write-Host "  Doctor 3:      dr.phuoc@dbh.vn           / Doctor@123  (Nhi khoa, Hospital B)"
Write-Host "  Doctor 4:      dr.tuan@dbh.vn            / Doctor@123  (Ngoai khoa, Hospital B)"
Write-Host "  Nurse 1:       nurse.hoa@dbh.vn          / Staff@123   (Hospital A)"
Write-Host "  Nurse 2:       nurse.khanh@dbh.vn        / Staff@123   (Hospital B)"
Write-Host "  Receptionist:  receptionist.minh@dbh.vn  / Staff@123   (Hospital A + Clinic)"
Write-Host "  Pharmacist:    pharmacist.oanh@dbh.vn    / Staff@123   (Hospital A)"
Write-Host "  LabTech:       labtech.tuan@dbh.vn       / Staff@123   (Hospital B)"
Write-Host ""
Write-Host "  === Organizations ===" -ForegroundColor Yellow
Write-Host "  Hospital A:  $($orgData[0].name) | orgId=$hospitalAId"
Write-Host "  Hospital B:  $($orgData[1].name) | orgId=$hospitalBId"
Write-Host "  Clinic:      $($orgData[2].name) | orgId=$clinicId"
Write-Host ""
Write-Host "  === PayOS Note ===" -ForegroundColor Magenta
Write-Host "  PayOS chua duoc cau hinh. De test thanh toan online:"
Write-Host "  1. Dang ky tai khoan PayOS sandbox tai https://payos.vn"
Write-Host "  2. Cau hinh PayOS cho Hospital A:"
Write-Host "     POST /api/v1/organizations/$hospitalAId/payment-config"
Write-Host "     Body: { clientId, apiKey, checksumKey }"
Write-Host "  3. Cau hinh PayOS cho Hospital B:"
Write-Host "     POST /api/v1/organizations/$hospitalBId/payment-config"
Write-Host "     Body: { clientId, apiKey, checksumKey }"
Write-Host "  4. Test checkout: POST /api/v1/invoices/{invoiceId}/checkout"
Write-Host ""
Write-Host "  === Unpaid Invoices (for PayOS testing) ===" -ForegroundColor Yellow
Write-Host "  Invoice 2 (Hospital A): $inv2Id | 1,140,000 VND"
Write-Host "  Invoice 3 (Hospital B): $inv3Id | 625,000 VND"
Write-Host ""
Write-Host "  === Cancelled Invoice ===" -ForegroundColor Yellow
Write-Host "  Invoice 4 (Hospital B): $inv4Id | 1,130,000 VND (CANCELLED)"
Write-Host ""
Write-Host "  === Paid Invoices ===" -ForegroundColor Yellow
Write-Host "  Invoice 1 (Hospital A): $inv1Id | 593,000 VND (CASH)"
Write-Host "  Invoice 5 (Clinic):     $inv5Id | 585,000 VND (CASH)"
Write-Host ""
