# =============================================================================
# DBH-EHR Seed Data Script (Idempotent - safe to re-run)
# Run after: docker compose up
# =============================================================================

$BASE  = "http://localhost:5000"
$AUTH  = "$BASE/api/v1/auth"
$ORG   = "$BASE/api/v1/organizations"
$DEPT  = "$BASE/api/v1/departments"
$MEMB  = "$BASE/api/v1/memberships"
$APTU  = "$BASE/api/v1/appointments"
$ENCU  = "$BASE/api/v1/encounters"
$EHRU  = "$BASE/api/v1/ehr/records"
$CONSU = "$BASE/api/v1/consents"
$AUDIT = "$BASE/api/v1/audit"
$NOTIF = "$BASE/api/v1/notifications"

$headers = @{ "Content-Type" = "application/json" }

function Api($method, $url, $body = $null, $token = $null, $extraHeaders = @{}) {
    $h = @{ "Content-Type" = "application/json" }
    if ($token) { $h["Authorization"] = "Bearer $token" }
    foreach ($k in $extraHeaders.Keys) { $h[$k] = $extraHeaders[$k] }
    
    $params = @{ Method = $method; Uri = $url; Headers = $h; ErrorAction = 'SilentlyContinue' }
    if ($body) { 
        $json = $body | ConvertTo-Json -Depth 10
        $params["Body"] = $json
    }
    try {
        $resp = Invoke-RestMethod @params
        Start-Sleep -Milliseconds 150
        return $resp
    } catch {
        $code = $_.Exception.Response.StatusCode.value__
        $errMsg = $_.ErrorDetails.Message
        if ($errMsg) {
            try {
                $errBody = $errMsg | ConvertFrom-Json
                $msg = if ($errBody.message) { $errBody.message } elseif ($errBody.title) { $errBody.title } else { $errMsg }
                Write-Host "  [$code] $msg" -ForegroundColor Yellow
                Start-Sleep -Milliseconds 150
                return $errBody
            } catch {
                Write-Host "  [$code] $errMsg" -ForegroundColor Yellow
            }
        } else {
            $exMsg = $_.Exception.Message
            Write-Host "  [$code] $($method) $url -> $exMsg" -ForegroundColor Red
        }
        Start-Sleep -Milliseconds 150
        return $null
    }
}

Write-Host "`n====== SEED DATA - DBH EHR System ======" -ForegroundColor Cyan

# =============================================================================
# 1. Register Admin + Update role
# =============================================================================
Write-Host "`n--- 1. Admin Account ---" -ForegroundColor Green

# Register (may already exist on re-run — that's OK)
Api POST "$AUTH/register" @{
    fullName = "System Admin"
    email    = "admin@dbh.vn"
    password = "Admin@123456"
    phone    = "0901000001"
} | Out-Null

# Login to get token
$loginAdmin = Api POST "$AUTH/login" @{ email = "admin@dbh.vn"; password = "Admin@123456" }
$adminToken = $loginAdmin.token

# Get userId from profile (reliable on re-run)
$adminMe = Api GET "$AUTH/me" $null $adminToken
$adminUserId = $adminMe.userId
Write-Host "  Admin: userId=$adminUserId"

# Update to Admin role (idempotent)
Api PUT "$AUTH/updateRole" @{ userId = "$adminUserId"; newRole = "Admin" } $adminToken | Out-Null
Write-Host "  Admin role assigned"

# Re-login to get admin claims
$loginAdmin = Api POST "$AUTH/login" @{ email = "admin@dbh.vn"; password = "Admin@123456" }
$adminToken = $loginAdmin.token

# =============================================================================
# 2. Register Patients (5 patients)
# =============================================================================
Write-Host "`n--- 2. Patients ---" -ForegroundColor Green

$patients = @(
    @{ fullName = "Nguyen Van An"; email = "patient.an@dbh.vn"; phone = "0912000001"; dob = "1990-05-15"; bloodType = "A+" },
    @{ fullName = "Tran Thi Binh"; email = "patient.binh@dbh.vn"; phone = "0912000002"; dob = "1985-08-22"; bloodType = "B+" },
    @{ fullName = "Le Van Cuong"; email = "patient.cuong@dbh.vn"; phone = "0912000003"; dob = "1978-12-01"; bloodType = "O+" },
    @{ fullName = "Pham Thi Dung"; email = "patient.dung@dbh.vn"; phone = "0912000004"; dob = "1995-03-10"; bloodType = "AB+" },
    @{ fullName = "Hoang Van Em"; email = "patient.em@dbh.vn"; phone = "0912000005"; dob = "2000-07-28"; bloodType = "O-" }
)

$patientData = @()
foreach ($p in $patients) {
    # Register (may already exist)
    Api POST "$AUTH/register" @{ fullName = $p.fullName; email = $p.email; password = "Patient@123"; phone = $p.phone } | Out-Null
    
    # Login to get token
    $login = Api POST "$AUTH/login" @{ email = $p.email; password = "Patient@123" }
    $token = $login.token
    
    # Get profile to get userId, patientId
    $me = Api GET "$AUTH/me" $null $token
    $userId = $me.userId
    $patientId = $me.profiles.Patient.patientId
    $userDid = "did:dbh:patient:$userId"
    
    $patientData += @{
        userId    = $userId
        patientId = $patientId
        did       = $userDid
        token     = $token
        name      = $p.fullName
        email     = $p.email
    }
    Write-Host "  Patient: $($p.fullName) | userId=$userId | patientId=$patientId"
}

# =============================================================================
# 3. Register Doctors (4 doctors, different specialties)
# =============================================================================
Write-Host "`n--- 3. Doctors ---" -ForegroundColor Green

$doctors = @(
    @{ fullName = "BS. Tran Minh Hieu"; email = "dr.hieu@dbh.vn"; phone = "0913000001"; specialty = "Noi khoa"; license = "VN-DOC-001" },
    @{ fullName = "BS. Nguyen Thi Lan"; email = "dr.lan@dbh.vn"; phone = "0913000002"; specialty = "Tim mach"; license = "VN-DOC-002" },
    @{ fullName = "BS. Le Van Phuoc"; email = "dr.phuoc@dbh.vn"; phone = "0913000003"; specialty = "Nhi khoa"; license = "VN-DOC-003" },
    @{ fullName = "BS. Pham Anh Tuan"; email = "dr.tuan@dbh.vn"; phone = "0913000004"; specialty = "Ngoai khoa"; license = "VN-DOC-004" }
)

$doctorData = @()
foreach ($d in $doctors) {
    # Register (may already exist)
    Api POST "$AUTH/registerStaffDoctor" @{
        fullName = $d.fullName; email = $d.email; password = "Doctor@123"; phone = $d.phone; role = "Doctor"
    } $adminToken | Out-Null
    
    # Login to get token
    $login = Api POST "$AUTH/login" @{ email = $d.email; password = "Doctor@123" }
    $token = $login.token
    
    # Get profile to get userId, doctorId
    $me = Api GET "$AUTH/me" $null $token
    $userId = $me.userId
    $doctorId = $me.profiles.Doctor.doctorId
    $userDid = "did:dbh:doctor:$userId"
    
    $doctorData += @{
        userId   = $userId
        doctorId = $doctorId
        did      = $userDid
        token    = $token
        name     = $d.fullName
        email    = $d.email
        specialty = $d.specialty
    }
    Write-Host "  Doctor: $($d.fullName) ($($d.specialty)) | userId=$userId | doctorId=$doctorId"
}

# =============================================================================
# 4. Register Staff (2 nurses, 1 receptionist, 1 pharmacist)
# =============================================================================
Write-Host "`n--- 4. Staff ---" -ForegroundColor Green

$staffList = @(
    @{ fullName = "DD. Vo Thi Hoa"; email = "nurse.hoa@dbh.vn"; phone = "0914000001"; role = "Nurse" },
    @{ fullName = "DD. Bui Van Khanh"; email = "nurse.khanh@dbh.vn"; phone = "0914000002"; role = "Nurse" },
    @{ fullName = "LT. Do Van Minh"; email = "receptionist.minh@dbh.vn"; phone = "0914000003"; role = "Receptionist" },
    @{ fullName = "DS. Ngo Thi Oanh"; email = "pharmacist.oanh@dbh.vn"; phone = "0914000004"; role = "Pharmacist" }
)

$staffData = @()
foreach ($s in $staffList) {
    # Register (may already exist)
    Api POST "$AUTH/registerStaffDoctor" @{
        fullName = $s.fullName; email = $s.email; password = "Staff@123"; phone = $s.phone; role = $s.role
    } $adminToken | Out-Null
    
    # Login to get token and userId
    $login = Api POST "$AUTH/login" @{ email = $s.email; password = "Staff@123" }
    $token = $login.token
    $me = Api GET "$AUTH/me" $null $token
    $userId = $me.userId
    
    $staffData += @{
        userId = $userId
        token  = $token
        name   = $s.fullName
    }
    Write-Host "  Staff: $($s.fullName) | userId=$userId"
}

# =============================================================================
# 5. Organizations (2 hospitals, 1 clinic)
# =============================================================================
Write-Host "`n--- 5. Organizations ---" -ForegroundColor Green

$orgs = @(
    @{
        orgName       = "Benh vien Da khoa Trung uong"
        orgCode       = "BVDKTU"
        orgType       = "HOSPITAL"
        licenseNumber = "BV-HCM-001"
        taxId         = "0301234567"
        address       = '{"line":["215 Hong Bang"],"city":"Ho Chi Minh","district":"Quan 5","country":"VN"}'
        contactInfo   = '{"phone":"028-3855-4269","email":"contact@bvdktu.vn"}'
        website       = "https://bvdktu.vn"
    },
    @{
        orgName       = "Benh vien Nhi Dong 1"
        orgCode       = "BVND1"
        orgType       = "HOSPITAL"
        licenseNumber = "BV-HCM-002"
        taxId         = "0301234568"
        address       = '{"line":["341 Su Van Hanh"],"city":"Ho Chi Minh","district":"Quan 10","country":"VN"}'
        contactInfo   = '{"phone":"028-3927-1119","email":"contact@bvnd1.vn"}'
        website       = "https://bvnd1.vn"
    },
    @{
        orgName       = "Phong kham Da lieu Sai Gon"
        orgCode       = "PKDLSG"
        orgType       = "CLINIC"
        licenseNumber = "PK-HCM-001"
        taxId         = "0301234569"
        address       = '{"line":["123 Nguyen Hue"],"city":"Ho Chi Minh","district":"Quan 1","country":"VN"}'
        contactInfo   = '{"phone":"028-3821-0000","email":"contact@pkdlsg.vn"}'
        website       = "https://pkdlsg.vn"
    }
)

$orgData = @()
foreach ($o in $orgs) {
    $created = Api POST $ORG $o $adminToken
    $orgId = if ($created.data.orgId) { $created.data.orgId } else { $created.orgId }
    $orgData += @{ orgId = $orgId; name = $o.orgName }
    Write-Host "  Org: $($o.orgName) | orgId=$orgId"
}

# Verify organizations
foreach ($od in $orgData) {
    if ($od.orgId) {
        Api POST "$ORG/$($od.orgId)/verify?verifiedByUserId=$adminUserId" $null $adminToken | Out-Null
        Write-Host "  Verified: $($od.name)"
    }
}

# =============================================================================
# 6. Departments (for first hospital)
# =============================================================================
Write-Host "`n--- 6. Departments ---" -ForegroundColor Green

$hospitalOrgId = $orgData[0].orgId

$departments = @(
    @{ orgId = $hospitalOrgId; departmentName = "Khoa Noi tong hop"; departmentCode = "NTH"; description = "Khoa Noi tong hop kham va dieu tri"; floor = "2"; roomNumbers = "201-210" },
    @{ orgId = $hospitalOrgId; departmentName = "Khoa Tim mach"; departmentCode = "TM"; description = "Khoa Tim mach chuyen sau"; floor = "3"; roomNumbers = "301-308" },
    @{ orgId = $hospitalOrgId; departmentName = "Khoa Nhi"; departmentCode = "NHI"; description = "Khoa Nhi dieu tri tre em"; floor = "4"; roomNumbers = "401-412" },
    @{ orgId = $hospitalOrgId; departmentName = "Khoa Ngoai tong hop"; departmentCode = "NGH"; description = "Khoa Ngoai tong hop phau thuat"; floor = "5"; roomNumbers = "501-510" },
    @{ orgId = $hospitalOrgId; departmentName = "Phong cap cuu"; departmentCode = "CC"; description = "Phong cap cuu 24/7"; floor = "1"; roomNumbers = "101-106" }
)

$deptData = @()
foreach ($d in $departments) {
    $created = Api POST $DEPT $d $adminToken
    $deptId = if ($created.data.departmentId) { $created.data.departmentId } else { $created.departmentId }
    $deptData += @{ deptId = $deptId; name = $d.departmentName }
    Write-Host "  Dept: $($d.departmentName) | deptId=$deptId"
}

# =============================================================================
# 7. Memberships (assign doctors & staff to hospital)
# =============================================================================
Write-Host "`n--- 7. Memberships ---" -ForegroundColor Green

# Doctor memberships
for ($i = 0; $i -lt $doctorData.Count; $i++) {
    $deptIdx = [Math]::Min($i, $deptData.Count - 1)
    $membership = @{
        userId       = $doctorData[$i].userId
        orgId        = $hospitalOrgId
        departmentId = $deptData[$deptIdx].deptId
        employeeId   = "EMP-DOC-$(($i+1).ToString('D3'))"
        jobTitle     = "Bac si $($doctorData[$i].specialty)"
        licenseNumber = "VN-DOC-$(($i+1).ToString('D3'))"
        specialty    = $doctorData[$i].specialty
        startDate    = "2024-01-01"
    }
    $created = Api POST $MEMB $membership $adminToken
    $memId = if ($created.data.membershipId) { $created.data.membershipId } else { $created.membershipId }
    Write-Host "  Member: $($doctorData[$i].name) -> $($deptData[$deptIdx].name) | memId=$memId"
}

# Staff memberships
for ($i = 0; $i -lt $staffData.Count; $i++) {
    $membership = @{
        userId     = $staffData[$i].userId
        orgId      = $hospitalOrgId
        employeeId = "EMP-STF-$(($i+1).ToString('D3'))"
        jobTitle   = "Nhan vien y te"
        startDate  = "2024-01-01"
    }
    $created = Api POST $MEMB $membership $adminToken
    $memId = if ($created.data.membershipId) { $created.data.membershipId } else { $created.membershipId }
    Write-Host "  Member: $($staffData[$i].name) -> Hospital | memId=$memId"
}

# =============================================================================
# 8. Appointments (patients book with doctors)
# =============================================================================
Write-Host "`n--- 8. Appointments ---" -ForegroundColor Green

$appointmentData = @()
$aptPairs = @(
    @{ patIdx = 0; docIdx = 0; daysFromNow = 1 },   # An -> Dr Hieu, tomorrow
    @{ patIdx = 1; docIdx = 1; daysFromNow = 2 },   # Binh -> Dr Lan
    @{ patIdx = 2; docIdx = 2; daysFromNow = 3 },   # Cuong -> Dr Phuoc
    @{ patIdx = 3; docIdx = 3; daysFromNow = 1 },   # Dung -> Dr Tuan
    @{ patIdx = 0; docIdx = 1; daysFromNow = 5 },   # An -> Dr Lan (2nd appointment)
    @{ patIdx = 4; docIdx = 0; daysFromNow = 2 },   # Em -> Dr Hieu
    # Past appointments (simulated — created with future date, then full lifecycle)
    @{ patIdx = 0; docIdx = 0; daysFromNow = -3; isPastSimulated = $true },  # An -> Dr Hieu (past)
    @{ patIdx = 1; docIdx = 1; daysFromNow = -5; isPastSimulated = $true }   # Binh -> Dr Lan (past)
)

foreach ($pair in $aptPairs) {
    Start-Sleep -Milliseconds 400  # extra delay to avoid rate limiting
    # API rejects past dates, so create with a future date 
    $createDate = if ($pair.isPastSimulated) {
        (Get-Date).AddDays(1).AddHours(9).ToString("yyyy-MM-ddTHH:mm:ssZ")
    } else {
        (Get-Date).AddDays($pair.daysFromNow).AddHours(9).ToString("yyyy-MM-ddTHH:mm:ssZ")
    }
    $patientToken = $patientData[$pair.patIdx].token
    
    $aptResult = Api POST $APTU @{
        patientId   = $patientData[$pair.patIdx].patientId
        doctorId    = $doctorData[$pair.docIdx].doctorId
        orgId       = $hospitalOrgId
        scheduledAt = $createDate
    } $patientToken
    
    $aptId = if ($aptResult.data.appointmentId) { $aptResult.data.appointmentId } else { $aptResult.appointmentId }
    $appointmentData += @{
        aptId   = $aptId
        patIdx  = $pair.patIdx
        docIdx  = $pair.docIdx
        isPast  = [bool]$pair.isPastSimulated
    }
    $label = if ($pair.isPastSimulated) { "(simulated past)" } else { "" }
    Write-Host "  Appointment: $($patientData[$pair.patIdx].name) -> $($doctorData[$pair.docIdx].name) $label | aptId=$aptId"
}

# Confirm ALL appointments (doctor confirms) — required before check-in
foreach ($aptItem in $appointmentData) {
    if ($aptItem.aptId) {
        $docToken = $doctorData[$aptItem.docIdx].token
        Api PUT "$APTU/$($aptItem.aptId)/confirm" $null $docToken | Out-Null
    }
}
Write-Host "  Confirmed all appointments"

# =============================================================================
# 9. Encounters + Complete (for past appointments -> create EHR)
# =============================================================================
Write-Host "`n--- 9. Encounters ---" -ForegroundColor Green

$encounterData = @()
$ehrIds = @()

foreach ($aptItem in $appointmentData) {
    if ($aptItem.isPast -and $aptItem.aptId) {
        $docToken = $doctorData[$aptItem.docIdx].token
        $patientId = $patientData[$aptItem.patIdx].patientId
        $doctorId = $doctorData[$aptItem.docIdx].doctorId
        $patientName = $patientData[$aptItem.patIdx].name
        
        # Check-in (appointment is already CONFIRMED from step 8)
        Api PUT "$APTU/$($aptItem.aptId)/check-in" $null $docToken | Out-Null
        Write-Host "  Checked in: $patientName"
        
        # Create encounter
        $encResult = Api POST $ENCU @{
            patientId     = "$patientId"
            doctorId      = "$doctorId"
            appointmentId = "$($aptItem.aptId)"
            orgId         = "$hospitalOrgId"
            notes         = "Kham benh cho $patientName"
        } $docToken
        
        $encId = if ($encResult.data.encounterId) { $encResult.data.encounterId } else { $encResult.encounterId }
        $encounterData += @{ encId = $encId; aptIdx = $appointmentData.IndexOf($aptItem) }
        Write-Host "  Encounter: $patientName | encId=$encId"
        
        # Complete encounter with EHR data
        $ehrData = @{
            resourceType = "Bundle"
            type = "document"
            entry = @(
                @{
                    resource = @{
                        resourceType = "Condition"
                        code = @{ text = "Viem hong cap" }
                        clinicalStatus = @{ coding = @(@{ code = "active" }) }
                    }
                },
                @{
                    resource = @{
                        resourceType = "MedicationRequest"
                        medicationCodeableConcept = @{ text = "Amoxicillin 500mg" }
                        dosageInstruction = @(@{ text = "Uong 3 lan/ngay sau an" })
                    }
                },
                @{
                    resource = @{
                        resourceType = "Observation"
                        code = @{ text = "Vitals" }
                        component = @(
                            @{ code = @{ text = "Nhiet do" }; valueQuantity = @{ value = 37.5; unit = "°C" } },
                            @{ code = @{ text = "Huyet ap" }; valueQuantity = @{ value = 120; unit = "mmHg" } },
                            @{ code = @{ text = "Nhip tim" }; valueQuantity = @{ value = 80; unit = "bpm" } }
                        )
                    }
                }
            )
        }
        
        if ($encId) {
            $complete = Api PUT "$ENCU/$encId/complete" @{
                notes   = "Hoan tat kham. Chan doan: Viem hong cap. Phac do: Amoxicillin 500mg x 7 ngay"
                ehrData = $ehrData
            } $docToken
            Write-Host "  Completed encounter with EHR"
        }
    }
}

# =============================================================================
# 10. Create additional standalone EHR records
# =============================================================================
Write-Host "`n--- 10. EHR Records ---" -ForegroundColor Green

# Doctor creates EHR for patients
for ($i = 0; $i -lt 3; $i++) {
    $docToken = $doctorData[0].token
    $patientId = $patientData[$i].patientId
    $doctorId = $doctorData[0].doctorId
    
    $ehrReq = @{
        patientId = "$patientId"
        orgId     = "$hospitalOrgId"
        data      = @{
            resourceType = "Bundle"
            type = "document"
            entry = @(
                @{
                    resource = @{
                        resourceType = "Condition"
                        code = @{ text = "Kham suc khoe tong quat" }
                        clinicalStatus = @{ coding = @(@{ code = "resolved" }) }
                    }
                },
                @{
                    resource = @{
                        resourceType = "Observation"
                        code = @{ text = "BMI" }
                        valueQuantity = @{ value = (22 + $i); unit = "kg/m2" }
                    }
                }
            )
        }
    }
    
    $ehrResult = Api POST $EHRU $ehrReq $docToken @{ "X-Doctor-Id" = "$doctorId" }
    $ehrId = if ($ehrResult.data.ehrId) { $ehrResult.data.ehrId } else { $ehrResult.ehrId }
    if ($ehrId) { $ehrIds += $ehrId }
    Write-Host "  EHR: $($patientData[$i].name) | ehrId=$ehrId"
}

# =============================================================================
# 11. Consents (patients grant access to doctors)
# =============================================================================
Write-Host "`n--- 11. Consents ---" -ForegroundColor Green

# Patient grants consent to Dr Hieu
for ($i = 0; $i -lt 3; $i++) {
    $patToken = $patientData[$i].token
    $patientId = $patientData[$i].patientId
    $patientDid = $patientData[$i].did
    $granteeId = $doctorData[0].doctorId
    $granteeDid = $doctorData[0].did
    
    $consent = Api POST $CONSU @{
        patientId    = "$patientId"
        patientDid   = "$patientDid"
        granteeId    = "$granteeId"
        granteeDid   = "$granteeDid"
        granteeType  = "DOCTOR"
        permission   = "READ"
        purpose      = "TREATMENT"
        durationDays = 90
    } $patToken
    $consentId = if ($consent.data.consentId) { $consent.data.consentId } else { $consent.consentId }
    Write-Host "  Consent: $($patientData[$i].name) -> $($doctorData[0].name) | consentId=$consentId"
}

# Patient grants consent to organization
$patToken = $patientData[0].token
$consent = Api POST $CONSU @{
    patientId    = "$($patientData[0].patientId)"
    patientDid   = "$($patientData[0].did)"
    granteeId    = "$hospitalOrgId"
    granteeDid   = "did:dbh:org:$hospitalOrgId"
    granteeType  = "ORGANIZATION"
    permission   = "READ"
    purpose      = "TREATMENT"
    durationDays = 365
} $patToken
Write-Host "  Consent: $($patientData[0].name) -> Hospital (org)"

# =============================================================================
# 12. Audit Logs
# =============================================================================
Write-Host "`n--- 12. Audit Logs ---" -ForegroundColor Green

$auditLogs = @(
    @{ actorDid = $patientData[0].did; actorUserId = $patientData[0].userId; actorType = "PATIENT"; action = "LOGIN"; targetType = "USER"; result = "SUCCESS" },
    @{ actorDid = $doctorData[0].did; actorUserId = $doctorData[0].userId; actorType = "DOCTOR"; action = "VIEW"; targetType = "EHR"; result = "SUCCESS"; patientDid = $patientData[0].did; patientId = $patientData[0].patientId },
    @{ actorDid = $doctorData[0].did; actorUserId = $doctorData[0].userId; actorType = "DOCTOR"; action = "CREATE"; targetType = "EHR"; result = "SUCCESS"; patientDid = $patientData[0].did; patientId = $patientData[0].patientId },
    @{ actorDid = $patientData[0].did; actorUserId = $patientData[0].userId; actorType = "PATIENT"; action = "GRANT_CONSENT"; targetType = "CONSENT"; result = "SUCCESS" },
    @{ actorDid = "did:dbh:system"; actorType = "SYSTEM"; action = "VIEW"; targetType = "SYSTEM"; result = "SUCCESS"; metadata = '{"event":"health_check"}' }
)

foreach ($log in $auditLogs) {
    Api POST "$AUDIT" $log $adminToken | Out-Null
}
Write-Host "  Created $($auditLogs.Count) audit logs"

# =============================================================================
# 13. Notifications
# =============================================================================
Write-Host "`n--- 13. Notifications ---" -ForegroundColor Green

$notifications = @(
    @{
        recipientDid    = $patientData[0].did
        recipientUserId = $patientData[0].userId
        title           = "Lich hen sap toi"
        body            = "Ban co lich kham voi BS. Tran Minh Hieu vao ngay mai luc 9:00."
        type            = "AppointmentReminder"
        priority        = "Normal"
        channel         = "InApp"
    },
    @{
        recipientDid    = $patientData[1].did
        recipientUserId = $patientData[1].userId
        title           = "Ho so benh an da cap nhat"
        body            = "BS. Nguyen Thi Lan da cap nhat ho so benh an cua ban."
        type            = "EhrUpdate"
        priority        = "Normal"
        channel         = "InApp"
    },
    @{
        recipientDid    = $doctorData[0].did
        recipientUserId = $doctorData[0].userId
        title           = "Yeu cau truy cap ho so"
        body            = "Benh nhan Nguyen Van An da cap quyen truy cap ho so benh an."
        type            = "ConsentGranted"
        priority        = "High"
        channel         = "InApp"
    },
    @{
        recipientDid    = $patientData[2].did
        recipientUserId = $patientData[2].userId
        title           = "Nhac nho uong thuoc"
        body            = "Hay uong thuoc Amoxicillin 500mg sau bua trua."
        type            = "System"
        priority        = "Normal"
        channel         = "InApp"
    },
    @{
        recipientDid    = $patientData[0].did
        recipientUserId = $patientData[0].userId
        title           = "Canh bao bao mat"
        body            = "Tai khoan cua ban vua duoc dang nhap tu thiet bi moi."
        type            = "SecurityAlert"
        priority        = "High"
        channel         = "InApp"
    }
)

foreach ($n in $notifications) {
    Api POST $NOTIF $n $adminToken | Out-Null
}
Write-Host "  Created $($notifications.Count) notifications"

# =============================================================================
# Summary
# =============================================================================
Write-Host "`n====== SEED DATA COMPLETE ======" -ForegroundColor Cyan
Write-Host "  Admin:         1 (admin@dbh.vn / Admin@123456)"
Write-Host "  Patients:      $($patientData.Count) (patient.an@dbh.vn ... / Patient@123)"
Write-Host "  Doctors:       $($doctorData.Count) (dr.hieu@dbh.vn ... / Doctor@123)"
Write-Host "  Staff:         $($staffData.Count) (nurse.hoa@dbh.vn ... / Staff@123)"
Write-Host "  Organizations: $($orgData.Count)"
Write-Host "  Departments:   $($deptData.Count)"
Write-Host "  Appointments:  $($appointmentData.Count)"
Write-Host "  Encounters:    $($encounterData.Count)"
Write-Host "  EHR Records:   $($ehrIds.Count) (standalone)"
Write-Host "  Consents:      4"
Write-Host "  Audit Logs:    $($auditLogs.Count)"
Write-Host "  Notifications: $($notifications.Count)"
Write-Host ""
Write-Host "  === Test Accounts ===" -ForegroundColor Yellow
Write-Host "  Admin:    admin@dbh.vn      / Admin@123456"
Write-Host "  Patient1: patient.an@dbh.vn / Patient@123"
Write-Host "  Patient2: patient.binh@dbh.vn / Patient@123"
Write-Host "  Patient3: patient.cuong@dbh.vn / Patient@123"
Write-Host "  Patient4: patient.dung@dbh.vn / Patient@123"
Write-Host "  Patient5: patient.em@dbh.vn / Patient@123"
Write-Host "  Doctor1:  dr.hieu@dbh.vn    / Doctor@123"
Write-Host "  Doctor2:  dr.lan@dbh.vn     / Doctor@123"
Write-Host "  Doctor3:  dr.phuoc@dbh.vn   / Doctor@123"
Write-Host "  Doctor4:  dr.tuan@dbh.vn    / Doctor@123"
Write-Host "  Staff1:   nurse.hoa@dbh.vn  / Staff@123"
Write-Host "  Staff2:   nurse.khanh@dbh.vn / Staff@123"
Write-Host "  Staff3:   receptionist.minh@dbh.vn / Staff@123"
Write-Host "  Staff4:   pharmacist.oanh@dbh.vn / Staff@123"
Write-Host ""
