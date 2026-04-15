#!/usr/bin/env bash
set -u

# =============================================================================
# DBH-EHR Full Seed Data Script (Bash version - matches seed-data.ps1)
# Run after: docker compose -f docker-compose.dev.yml up -d
# Idempotent: safe to re-run (existing records will return error and continue)
# NOTE: Does NOT seed PayOS payment config - manual setup required
#
# Coverage: Auth, Organization, Department, Membership, Appointment (full lifecycle),
#   Encounter, EHR (create + version), Consent (grant + revoke), Access Requests,
#   Audit Logs, Notifications (individual + broadcast + preferences + device tokens),
#   Invoices (create + pay cash + cancel) across 3 organizations
# =============================================================================

BASE="http://localhost:5000"
AUTH="$BASE/api/v1/auth"
ORG="$BASE/api/v1/organizations"
DEPT="$BASE/api/v1/departments"
MEMB="$BASE/api/v1/memberships"
APTU="$BASE/api/v1/appointments"
ENCU="$BASE/api/v1/encounters"
EHRU="$BASE/api/v1/ehr/records"
CONSU="$BASE/api/v1/consents"
AUDIT="$BASE/api/v1/audit"
NOTIF="$BASE/api/v1/notifications"
INVOICE="$BASE/api/v1/invoices"

# ---------------------------------------------------------------------------
# Helper: api METHOD URL [BODY] [TOKEN]
# ---------------------------------------------------------------------------
api() {
    local method="$1" url="$2" body="${3:-}" token="${4:-}"
    local -a hdr=(-H "Content-Type: application/json")
    [ -n "$token" ] && hdr+=(-H "Authorization: Bearer $token")

    local -a args=(-s -w "\n%{http_code}" -X "$method" "${hdr[@]}" --max-time 15)
    [ -n "$body" ] && args+=(-d "$body")
    args+=("$url")

    local raw resp code
    raw=$(curl "${args[@]}" 2>/dev/null) || true
    code=$(echo "$raw" | tail -1)
    resp=$(echo "$raw" | sed '$d')

    if [[ "$code" =~ ^2 ]]; then
        echo "$resp"
    else
        local msg
        msg=$(echo "$resp" | jq -r '.message // .title // empty' 2>/dev/null)
        [ -z "$msg" ] && msg="$resp"
        echo "  [$code] $msg" >&2
        echo "$resp"
    fi
    sleep 0.2
}

# Helper: api with extra header
api_h() {
    local method="$1" url="$2" body="${3:-}" token="${4:-}" extraKey="$5" extraVal="$6"
    local -a hdr=(-H "Content-Type: application/json")
    [ -n "$token" ] && hdr+=(-H "Authorization: Bearer $token")
    hdr+=(-H "$extraKey: $extraVal")

    local -a args=(-s -w "\n%{http_code}" -X "$method" "${hdr[@]}" --max-time 15)
    [ -n "$body" ] && args+=(-d "$body")
    args+=("$url")

    local raw resp code
    raw=$(curl "${args[@]}" 2>/dev/null) || true
    code=$(echo "$raw" | tail -1)
    resp=$(echo "$raw" | sed '$d')

    if [[ "$code" =~ ^2 ]]; then
        echo "$resp"
    else
        local msg
        msg=$(echo "$resp" | jq -r '.message // .title // empty' 2>/dev/null)
        [ -z "$msg" ] && msg="$resp"
        echo "  [$code] $msg" >&2
        echo "$resp"
    fi
    sleep 0.2
}

# Helper: extract JSON field
jval() { echo "$1" | jq -r "$2 // empty" 2>/dev/null; }

# Helper: portable future date (works on GNU, macOS, BusyBox/Alpine)
# Usage: future_date <days> <hours> <minutes>
future_date() {
    local d="${1:-0}" h="${2:-0}" m="${3:-0}"
    local offset=$(( d*86400 + h*3600 + m*60 ))
    local epoch=$(( $(date +%s) + offset ))
    # GNU coreutils / BusyBox
    date -u -d "@$epoch" '+%Y-%m-%dT%H:%M:%SZ' 2>/dev/null \
        || date -u -r "$epoch" '+%Y-%m-%dT%H:%M:%SZ'   # macOS
}

echo ""
echo "====== FULL SEED DATA - DBH EHR System ======"
echo "  Started at: $(date '+%Y-%m-%d %H:%M:%S')"

# =============================================================================
# 1. ADMIN ACCOUNT
# =============================================================================
echo ""
echo "--- 1. Admin Account ---"

bootstrapLogin=$(api POST "$AUTH/login" '{"email":"admin@dbh.com","password":"admin123"}')
bootstrapToken=$(jval "$bootstrapLogin" '.token')
if [ -z "$bootstrapToken" ]; then
    echo "  FATAL: Cannot login as bootstrap admin (admin@dbh.com). Ensure DB is migrated."
    exit 1
fi
echo "  Bootstrap admin (admin@dbh.com) logged in"

api POST "$AUTH/register" '{"fullName":"System Admin","email":"admin@dbh.vn","password":"Admin@123456","phone":"0901000001"}' >/dev/null

loginAdmin=$(api POST "$AUTH/login" '{"email":"admin@dbh.vn","password":"Admin@123456"}')
adminToken=$(jval "$loginAdmin" '.token')

adminMe=$(api GET "$AUTH/me" "" "$adminToken")
adminUserId=$(jval "$adminMe" '.userId')
echo "  Admin: userId=$adminUserId"

api PUT "$AUTH/updateRole" "{\"userId\":\"$adminUserId\",\"newRole\":\"Admin\"}" "$bootstrapToken" >/dev/null
echo "  Role -> Admin (via bootstrap)"

loginAdmin=$(api POST "$AUTH/login" '{"email":"admin@dbh.vn","password":"Admin@123456"}')
adminToken=$(jval "$loginAdmin" '.token')

api PUT "$AUTH/me/profile" '{"fullName":"System Admin","phone":"0901000001","gender":"Male","dateOfBirth":"1985-06-15T00:00:00Z","address":"100 Nguyen Du, Quan 1, TP.HCM"}' "$adminToken" >/dev/null
echo "  Profile updated (all fields)"

# =============================================================================
# 2. PATIENTS (5 patients - all fields filled)
# =============================================================================
echo ""
echo "--- 2. Patients ---"

# Arrays for patient data
declare -a pat_fullName=("Nguyen Van An" "Tran Thi Binh" "Le Van Cuong" "Pham Thi Dung" "Hoang Van Em")
declare -a pat_email=("patient.an@dbh.vn" "patient.binh@dbh.vn" "patient.cuong@dbh.vn" "patient.dung@dbh.vn" "patient.em@dbh.vn")
declare -a pat_phone=("0912000001" "0912000002" "0912000003" "0912000004" "0912000005")
declare -a pat_gender=("Male" "Female" "Male" "Female" "Male")
declare -a pat_dob=("1990-05-15" "1985-08-22" "1978-12-01" "1995-03-10" "2000-07-28")
declare -a pat_address=("12 Le Lai, Quan 1, TP.HCM" "45 Tran Hung Dao, Quan 5, TP.HCM" "78 Hai Ba Trung, Quan 3, TP.HCM" "200 Vo Van Tan, Quan 3, TP.HCM" "33 Nguyen Trai, Quan 1, TP.HCM")
declare -a pat_blood=("A+" "B+" "O+" "AB+" "O-")

declare -a pat_userId=() pat_patientId=() pat_did=() pat_token=()

for i in 0 1 2 3 4; do
    api POST "$AUTH/register" "{\"fullName\":\"${pat_fullName[$i]}\",\"email\":\"${pat_email[$i]}\",\"password\":\"Patient@123\",\"phone\":\"${pat_phone[$i]}\"}" >/dev/null

    login=$(api POST "$AUTH/login" "{\"email\":\"${pat_email[$i]}\",\"password\":\"Patient@123\"}")
    token=$(jval "$login" '.token')

    api PUT "$AUTH/me/profile" "{\"fullName\":\"${pat_fullName[$i]}\",\"phone\":\"${pat_phone[$i]}\",\"gender\":\"${pat_gender[$i]}\",\"dateOfBirth\":\"${pat_dob[$i]}T00:00:00Z\",\"address\":\"${pat_address[$i]}\"}" "$token" >/dev/null

    me=$(api GET "$AUTH/me" "" "$token")
    userId=$(jval "$me" '.userId')
    patientId=$(jval "$me" '.profiles.Patient.patientId')
    userDid="did:dbh:patient:$userId"

    pat_userId+=("$userId")
    pat_patientId+=("$patientId")
    pat_did+=("$userDid")
    pat_token+=("$token")
    echo "  Patient: ${pat_fullName[$i]} | userId=$userId | patientId=$patientId"
done

# =============================================================================
# 3. DOCTORS (4 doctors - all fields filled)
# =============================================================================
echo ""
echo "--- 3. Doctors ---"

declare -a doc_fullName=("BS. Tran Minh Hieu" "BS. Nguyen Thi Lan" "BS. Le Van Phuoc" "BS. Pham Anh Tuan")
declare -a doc_email=("dr.hieu@dbh.vn" "dr.lan@dbh.vn" "dr.phuoc@dbh.vn" "dr.tuan@dbh.vn")
declare -a doc_phone=("0913000001" "0913000002" "0913000003" "0913000004")
declare -a doc_gender=("Male" "Female" "Male" "Male")
declare -a doc_dob=("1980-03-20" "1982-07-11" "1979-11-05" "1983-01-30")
declare -a doc_address=("50 Pasteur, Quan 1, TP.HCM" "99 Nam Ky Khoi Nghia, Quan 3, TP.HCM" "15 Cach Mang Thang 8, Quan 10, TP.HCM" "88 Ly Tu Trong, Quan 1, TP.HCM")
declare -a doc_specialty=("Noi khoa" "Tim mach" "Nhi khoa" "Ngoai khoa")
declare -a doc_license=("VN-DOC-001" "VN-DOC-002" "VN-DOC-003" "VN-DOC-004")

declare -a doc_userId=() doc_doctorId=() doc_did=() doc_token=()

for i in 0 1 2 3; do
    api POST "$AUTH/registerStaffDoctor" "{\"fullName\":\"${doc_fullName[$i]}\",\"email\":\"${doc_email[$i]}\",\"password\":\"Doctor@123\",\"phone\":\"${doc_phone[$i]}\",\"role\":\"Doctor\",\"gender\":\"${doc_gender[$i]}\",\"dateOfBirth\":\"${doc_dob[$i]}T00:00:00Z\",\"address\":\"${doc_address[$i]}\"}" "$adminToken" >/dev/null

    login=$(api POST "$AUTH/login" "{\"email\":\"${doc_email[$i]}\",\"password\":\"Doctor@123\"}")
    token=$(jval "$login" '.token')

    api PUT "$AUTH/me/profile" "{\"fullName\":\"${doc_fullName[$i]}\",\"phone\":\"${doc_phone[$i]}\",\"gender\":\"${doc_gender[$i]}\",\"dateOfBirth\":\"${doc_dob[$i]}T00:00:00Z\",\"address\":\"${doc_address[$i]}\"}" "$token" >/dev/null

    me=$(api GET "$AUTH/me" "" "$token")
    userId=$(jval "$me" '.userId')
    doctorId=$(jval "$me" '.profiles.Doctor.doctorId')

    doc_userId+=("$userId")
    doc_doctorId+=("$doctorId")
    doc_did+=("did:dbh:doctor:$userId")
    doc_token+=("$token")
    echo "  Doctor: ${doc_fullName[$i]} (${doc_specialty[$i]}) | userId=$userId | doctorId=$doctorId"
done

# =============================================================================
# 4. STAFF (2 nurses, 1 receptionist, 1 pharmacist, 1 labtech)
# =============================================================================
echo ""
echo "--- 4. Staff ---"

declare -a stf_fullName=("DD. Vo Thi Hoa" "DD. Bui Van Khanh" "LT. Do Van Minh" "DS. Ngo Thi Oanh" "KTV. Nguyen Minh Tuan")
declare -a stf_email=("nurse.hoa@dbh.vn" "nurse.khanh@dbh.vn" "receptionist.minh@dbh.vn" "pharmacist.oanh@dbh.vn" "labtech.tuan@dbh.vn")
declare -a stf_phone=("0914000001" "0914000002" "0914000003" "0914000004" "0914000005")
declare -a stf_gender=("Female" "Male" "Male" "Female" "Male")
declare -a stf_dob=("1992-04-18" "1990-09-25" "1994-06-12" "1991-02-08" "1993-10-14")
declare -a stf_address=("22 Le Van Sy, Quan 3, TP.HCM" "67 Nguyen Dinh Chieu, Quan 3, TP.HCM" "34 Pham Ngoc Thach, Quan 3, TP.HCM" "56 Dien Bien Phu, Quan Binh Thanh" "89 Xo Viet Nghe Tinh, Binh Thanh")
declare -a stf_role=("Nurse" "Nurse" "Receptionist" "Pharmacist" "LabTech")

declare -a stf_userId=() stf_token=() stf_did=()

for i in 0 1 2 3 4; do
    api POST "$AUTH/registerStaffDoctor" "{\"fullName\":\"${stf_fullName[$i]}\",\"email\":\"${stf_email[$i]}\",\"password\":\"Staff@123\",\"phone\":\"${stf_phone[$i]}\",\"role\":\"${stf_role[$i]}\",\"gender\":\"${stf_gender[$i]}\",\"dateOfBirth\":\"${stf_dob[$i]}T00:00:00Z\",\"address\":\"${stf_address[$i]}\"}" "$adminToken" >/dev/null

    login=$(api POST "$AUTH/login" "{\"email\":\"${stf_email[$i]}\",\"password\":\"Staff@123\"}")
    token=$(jval "$login" '.token')

    api PUT "$AUTH/me/profile" "{\"fullName\":\"${stf_fullName[$i]}\",\"phone\":\"${stf_phone[$i]}\",\"gender\":\"${stf_gender[$i]}\",\"dateOfBirth\":\"${stf_dob[$i]}T00:00:00Z\",\"address\":\"${stf_address[$i]}\"}" "$token" >/dev/null

    me=$(api GET "$AUTH/me" "" "$token")
    userId=$(jval "$me" '.userId')

    stf_userId+=("$userId")
    stf_token+=("$token")
    stf_did+=("did:dbh:staff:$userId")
    echo "  Staff: ${stf_fullName[$i]} (${stf_role[$i]}) | userId=$userId"
done
# =============================================================================
# 5. ORGANIZATIONS (2 hospitals, 1 clinic - ALL fields filled)
# =============================================================================
echo ""
echo "--- 5. Organizations ---"

declare -a org_name=("Benh vien Da khoa Trung uong" "Benh vien Nhi Dong 1" "Phong kham Da lieu Sai Gon")
declare -a org_code=("BVDKTU" "BVND1" "PKDLSG")
declare -a org_type=("HOSPITAL" "HOSPITAL" "CLINIC")
declare -a org_license=("BV-HCM-001" "BV-HCM-002" "PK-HCM-001")
declare -a org_taxId=("0301234567" "0301234568" "0301234569")
declare -a org_address=(
    '{"line":["215 Hong Bang"],"city":"Ho Chi Minh","district":"Quan 5","country":"VN","postalCode":"700000"}'
    '{"line":["341 Su Van Hanh"],"city":"Ho Chi Minh","district":"Quan 10","country":"VN","postalCode":"700000"}'
    '{"line":["123 Nguyen Hue"],"city":"Ho Chi Minh","district":"Quan 1","country":"VN","postalCode":"700000"}'
)
declare -a org_contactInfo=(
    '{"phone":"028-3855-4269","fax":"028-3855-4270","email":"contact@bvdktu.vn","hotline":"1900-1234"}'
    '{"phone":"028-3927-1119","fax":"028-3927-1120","email":"contact@bvnd1.vn","hotline":"1900-5678"}'
    '{"phone":"028-3821-0000","fax":"028-3821-0001","email":"contact@pkdlsg.vn","hotline":"1900-9012"}'
)
declare -a org_website=("https://bvdktu.vn" "https://bvnd1.vn" "https://pkdlsg.vn")

declare -a org_orgId=()
for i in 0 1 2; do
    body=$(jq -n \
        --arg n "${org_name[$i]}" --arg c "${org_code[$i]}" --arg t "${org_type[$i]}" \
        --arg l "${org_license[$i]}" --arg tx "${org_taxId[$i]}" \
        --arg w "${org_website[$i]}" \
        --argjson a "${org_address[$i]}" --argjson ci "${org_contactInfo[$i]}" \
        '{orgName:$n,orgCode:$c,orgType:$t,licenseNumber:$l,taxId:$tx,address:($a|tostring),contactInfo:($ci|tostring),website:$w}')
    created=$(api POST "$ORG" "$body" "$adminToken")
    orgId=$(jval "$created" '.data.orgId // .orgId')

    # Fallback: if create failed (duplicate), search
    if [ -z "$orgId" ]; then
        existing=$(api GET "$ORG?search=${org_code[$i]}&pageSize=50" "" "$adminToken")
        orgId=$(echo "$existing" | jq -r --arg c "${org_code[$i]}" '(.data // .) | if type=="array" then .[] else empty end | select(.orgCode==$c) | .orgId // empty' 2>/dev/null | head -1)
    fi

    org_orgId+=("$orgId")
    echo "  Org: ${org_name[$i]} | orgId=$orgId"
done

# Verify all organizations
for i in 0 1 2; do
    if [ -n "${org_orgId[$i]}" ]; then
        api POST "$ORG/${org_orgId[$i]}/verify?verifiedByUserId=$adminUserId" "" "$adminToken" >/dev/null
        echo "  Verified: ${org_name[$i]}"
    fi
done

hospitalAId="${org_orgId[0]}"
hospitalBId="${org_orgId[1]}"
clinicId="${org_orgId[2]}"

# =============================================================================
# 6. DEPARTMENTS (Hospital A: 5, Hospital B: 3, Clinic: 2)
# =============================================================================
echo ""
echo "--- 6. Departments ---"

declare -a dept_orgId=("$hospitalAId" "$hospitalAId" "$hospitalAId" "$hospitalAId" "$hospitalAId" "$hospitalBId" "$hospitalBId" "$hospitalBId" "$clinicId" "$clinicId")
declare -a dept_name=("Khoa Noi tong hop" "Khoa Tim mach" "Khoa Nhi" "Khoa Ngoai tong hop" "Phong cap cuu" "Khoa Nhi tong hop" "Khoa Nhi so sinh" "Khoa Cap cuu Nhi" "Phong kham Da lieu" "Phong kham Tham my")
declare -a dept_code=("NTH" "TM" "NHI" "NGH" "CC" "NTH-B" "NSS-B" "CCN-B" "DL" "TM-C")
declare -a dept_desc=(
    "Khoa Noi tong hop kham va dieu tri cac benh noi khoa"
    "Khoa Tim mach chuyen sau chan doan va dieu tri benh tim"
    "Khoa Nhi chuyen dieu tri benh tre em tu so sinh den 16 tuoi"
    "Khoa Ngoai tong hop phau thuat va dieu tri ngoai khoa"
    "Phong cap cuu 24/7 tiep nhan benh nhan khan cap"
    "Khoa Nhi tong hop kham va dieu tri tre em"
    "Khoa Nhi so sinh cham soc tre so sinh non thang"
    "Khoa Cap cuu Nhi tiep nhan tre em khan cap"
    "Phong kham Da lieu chuyen tri mun, nam, vay nen"
    "Phong kham Tham my da bang laser va cong nghe cao"
)
declare -a dept_floor=("2" "3" "4" "5" "1" "2" "3" "1" "1" "2")
declare -a dept_rooms=("201-210" "301-308" "401-412" "501-510" "101-106" "B201-B210" "B301-B306" "B101-B104" "C101-C104" "C201-C203")
declare -a dept_ext=("2001" "3001" "4001" "5001" "1001" "2101" "3101" "1101" "101" "201")

declare -a dept_deptId=()
for i in $(seq 0 9); do
    body=$(jq -n \
        --arg o "${dept_orgId[$i]}" --arg n "${dept_name[$i]}" --arg c "${dept_code[$i]}" \
        --arg d "${dept_desc[$i]}" --arg f "${dept_floor[$i]}" --arg r "${dept_rooms[$i]}" --arg e "${dept_ext[$i]}" \
        '{orgId:$o,departmentName:$n,departmentCode:$c,description:$d,floor:$f,roomNumbers:$r,phoneExtension:$e}')
    created=$(api POST "$DEPT" "$body" "$adminToken")
    deptId=$(jval "$created" '.data.departmentId // .departmentId')

    if [ -z "$deptId" ] && [ -n "${dept_orgId[$i]}" ]; then
        existing=$(api GET "$DEPT/by-organization/${dept_orgId[$i]}?pageSize=50" "" "$adminToken")
        deptId=$(echo "$existing" | jq -r --arg c "${dept_code[$i]}" '(.data // .) | if type=="array" then .[] else empty end | select(.departmentCode==$c) | .departmentId // empty' 2>/dev/null | head -1)
    fi

    dept_deptId+=("$deptId")
    echo "  Dept: ${dept_name[$i]} | deptId=$deptId"
done
# =============================================================================
# 7. MEMBERSHIPS (assign doctors & staff to hospitals - ALL fields filled)
# =============================================================================
echo ""
echo "--- 7. Memberships ---"

create_membership() {
    local userId="$1" orgId="$2" deptId="$3" empId="$4" jobTitle="$5" license="$6" specialty="$7" qualifications="$8" startDate="$9" notes="${10}"
    body=$(jq -n \
        --arg u "$userId" --arg o "$orgId" --arg d "$deptId" --arg e "$empId" --arg j "$jobTitle" \
        --arg l "$license" --arg s "$specialty" --arg q "$qualifications" --arg sd "$startDate" --arg n "$notes" \
        '{userId:$u,orgId:$o,departmentId:$d,employeeId:$e,jobTitle:$j,licenseNumber:$l,specialty:$s,qualifications:$q,startDate:$sd,orgPermissions:"[\"VIEW_PATIENTS\",\"CREATE_RECORDS\"]",notes:$n}')
    created=$(api POST "$MEMB" "$body" "$adminToken")
    memId=$(jval "$created" '.data.membershipId // .membershipId')
    echo "  Member: $orgId | empId=$empId | memId=$memId"
}

# Hospital A memberships - Doctors 0,1 and Staff 0,2,3
create_membership "${doc_userId[0]}" "$hospitalAId" "${dept_deptId[0]}" "EMP-DOC-001" "Bac si dieu tri Noi khoa"    "${doc_license[0]}" "${doc_specialty[0]}" '["Dai hoc Y Duoc TP.HCM","Thac si Noi khoa"]'    "2024-01-15" "Bac si chinh Khoa Noi"
create_membership "${doc_userId[1]}" "$hospitalAId" "${dept_deptId[1]}" "EMP-DOC-002" "Bac si chuyen khoa Tim mach" "${doc_license[1]}" "${doc_specialty[1]}" '["Dai hoc Y Ha Noi","Tien si Tim mach"]'          "2024-01-15" "Truong khoa Tim mach"
create_membership "${stf_userId[0]}" "$hospitalAId" "${dept_deptId[0]}" "EMP-STF-001" "Dieu duong truong Khoa Noi"  "DD-001"            "Dieu duong"          '["Cu nhan Dieu duong","Chung chi ICU"]'           "2024-01-15" "Dieu duong truong"
create_membership "${stf_userId[2]}" "$hospitalAId" "${dept_deptId[4]}" "EMP-STF-003" "Nhan vien tiep nhan"         "LT-001"            "Tiep nhan"           '["Trung cap Y","Chung chi tiep nhan benh nhan"]'  "2024-01-15" "Le tan phong cap cuu"
create_membership "${stf_userId[3]}" "$hospitalAId" "${dept_deptId[0]}" "EMP-STF-004" "Duoc si lam sang"            "DS-001"            "Duoc lam sang"       '["Dai hoc Duoc","Chung chi Duoc lam sang"]'       "2024-01-15" "Duoc si Khoa Noi"

# Hospital B memberships - Doctors 2,3 and Staff 1,4
create_membership "${doc_userId[2]}" "$hospitalBId" "${dept_deptId[5]}" "EMP-DOC-B01" "Bac si dieu tri Nhi khoa"   "${doc_license[2]}" "${doc_specialty[2]}" '["Dai hoc Y Duoc TP.HCM","Chuyen khoa I Nhi"]'   "2024-02-01" "Bac si chinh Khoa Nhi"
create_membership "${doc_userId[3]}" "$hospitalBId" "${dept_deptId[7]}" "EMP-DOC-B02" "Bac si phau thuat Nhi"      "${doc_license[3]}" "${doc_specialty[3]}" '["Dai hoc Y Ha Noi","Chuyen khoa II Ngoai Nhi"]'  "2024-02-01" "Truong khoa Cap cuu Nhi"
create_membership "${stf_userId[1]}" "$hospitalBId" "${dept_deptId[5]}" "EMP-STF-B01" "Dieu duong Nhi khoa"        "DD-B01"            "Dieu duong Nhi"      '["Cu nhan Dieu duong","Chung chi Nhi khoa"]'      "2024-02-01" "Dieu duong Khoa Nhi"
create_membership "${stf_userId[4]}" "$hospitalBId" "${dept_deptId[6]}" "EMP-STF-B02" "Ky thuat vien xet nghiem"   "KTV-B01"           "Xet nghiem"          '["Cu nhan Xet nghiem","Chung chi Huyet hoc"]'     "2024-02-01" "KTV Khoa So sinh"

# Clinic memberships
create_membership "${doc_userId[0]}" "$clinicId" "${dept_deptId[8]}" "EMP-DOC-C01" "Bac si tu van Da lieu"  "${doc_license[0]}" "Da lieu"  '["Dai hoc Y Duoc TP.HCM","Chung chi Da lieu co ban"]' "2024-03-01" "Bac si ban thoi gian tai phong kham"
create_membership "${stf_userId[2]}" "$clinicId" "${dept_deptId[8]}" "EMP-STF-C01" "Nhan vien tiep nhan"    "LT-C01"            "Tiep nhan" '["Trung cap Y"]'                                     "2024-03-01" "Le tan phong kham"

echo "  Total memberships: 11 (5 HospA + 4 HospB + 2 Clinic)"

# =============================================================================
# 8. APPOINTMENTS (various pairings across all organizations)
# =============================================================================
echo ""
echo "--- 8. Appointments ---"

# Appointment pairs: patIdx docIdx orgId daysFromNow isPast
declare -a apt_patIdx=(0 1 3 0 4 2 4 3 1 0 1 2)
declare -a apt_docIdx=(0 1 0 1 0 2 3 0 0 0 1 2)
declare -a apt_orgVar=("hospitalAId" "hospitalAId" "hospitalAId" "hospitalAId" "hospitalAId" "hospitalBId" "hospitalBId" "clinicId" "clinicId" "hospitalAId" "hospitalAId" "hospitalBId")
declare -a apt_days=(1 2 3 5 2 1 3 4 6 -3 -5 -2)
declare -a apt_isPast=(0 0 0 0 0 0 0 0 0 1 1 1)

declare -a apt_aptId=() apt_patIdxArr=() apt_docIdxArr=() apt_orgIdArr=() apt_isPastArr=()

for idx in $(seq 0 11); do
    # Resolve orgId
    case "${apt_orgVar[$idx]}" in
        hospitalAId) orgId="$hospitalAId" ;;
        hospitalBId) orgId="$hospitalBId" ;;
        clinicId)    orgId="$clinicId" ;;
    esac

    pi=${apt_patIdx[$idx]}
    di=${apt_docIdx[$idx]}
    days=${apt_days[$idx]}
    isPast=${apt_isPast[$idx]}
    minuteOffset=$((idx * 15))

    sleep 0.4
    if [ "$isPast" -eq 1 ]; then
        createDate=$(future_date 1 14 $minuteOffset)
    else
        createDate=$(future_date $days 9 $minuteOffset)
    fi

    patientToken="${pat_token[$pi]}"
    body=$(jq -n --arg p "${pat_patientId[$pi]}" --arg d "${doc_doctorId[$di]}" --arg o "$orgId" --arg s "$createDate" \
        '{patientId:$p,doctorId:$d,orgId:$o,scheduledAt:$s}')
    aptResult=$(api POST "$APTU" "$body" "$patientToken")
    aptId=$(jval "$aptResult" '.data.appointmentId // .appointmentId')

    # Fallback
    if [ -z "$aptId" ] && [ -n "$patientToken" ]; then
        existing=$(api GET "$APTU?patientId=${pat_patientId[$pi]}&doctorId=${doc_doctorId[$di]}&pageSize=20" "" "$patientToken")
        aptId=$(echo "$existing" | jq -r '(.data // .) | if type=="array" then .[0].appointmentId else empty end' 2>/dev/null)
    fi

    apt_aptId+=("$aptId")
    apt_patIdxArr+=("$pi")
    apt_docIdxArr+=("$di")
    apt_orgIdArr+=("$orgId")
    apt_isPastArr+=("$isPast")

    label=""
    [ "$isPast" -eq 1 ] && label="(simulated past)"
    echo "  Appointment: ${pat_fullName[$pi]} -> ${doc_fullName[$di]} $label | aptId=$aptId"
done

# Confirm future appointments
confirmCount=0
for ci in $(seq 0 11); do
    if [ -n "${apt_aptId[$ci]}" ] && [ "${apt_isPast[$ci]}" -eq 0 ]; then
        di=${apt_docIdxArr[$ci]}
        api PUT "$APTU/${apt_aptId[$ci]}/confirm" "" "${doc_token[$di]}" >/dev/null
        confirmCount=$((confirmCount + 1))
    fi
done
# Confirm past appointments for encounter flow
for ci in $(seq 0 11); do
    if [ -n "${apt_aptId[$ci]}" ] && [ "${apt_isPast[$ci]}" -eq 1 ]; then
        di=${apt_docIdxArr[$ci]}
        api PUT "$APTU/${apt_aptId[$ci]}/confirm" "" "${doc_token[$di]}" >/dev/null
    fi
done
echo "  Confirmed $confirmCount future + 3 past appointments"

# =============================================================================
# 8b. APPOINTMENT LIFECYCLE (reject, cancel, reschedule)
# =============================================================================
echo ""
echo "--- 8b. Appointment Lifecycle ---"

# Reject appointment index 4: Em -> Dr Hieu
if [ -n "${apt_aptId[4]}" ]; then
    di=${apt_docIdxArr[4]}
    api PUT "$APTU/${apt_aptId[4]}/reject" '{"reason":"Bac si bi trung lich hoc thuong xuyen vao ngay nay. Vui long dat lai lich khac."}' "${doc_token[$di]}" >/dev/null
    echo "  Rejected: ${pat_fullName[${apt_patIdxArr[4]}]} -> ${doc_fullName[$di]} (schedule conflict)"
fi

# Cancel appointment index 3: An -> Dr Lan 2nd
if [ -n "${apt_aptId[3]}" ]; then
    pi=${apt_patIdxArr[3]}
    api PUT "$APTU/${apt_aptId[3]}/cancel" '{"reason":"Benh nhan co viec dot xuat, xin huy lich hen."}' "${pat_token[$pi]}" >/dev/null
    echo "  Cancelled: ${pat_fullName[$pi]} -> ${doc_fullName[${apt_docIdxArr[3]}]} (patient request)"
fi

# Reschedule appointment index 1: Binh -> Dr Lan
if [ -n "${apt_aptId[1]}" ]; then
    pi=${apt_patIdxArr[1]}
    newDate=$(future_date 7 10 0)
    api PUT "$APTU/${apt_aptId[1]}/reschedule?newDate=$newDate" "" "${pat_token[$pi]}" >/dev/null
    echo "  Rescheduled: ${pat_fullName[$pi]} -> ${doc_fullName[${apt_docIdxArr[1]}]} (moved +7 days)"
fi
# =============================================================================
# 9. ENCOUNTERS & EHR (for past appointments)
# =============================================================================
echo ""
echo "--- 9. Encounters & EHR ---"

declare -a enc_encId=()
declare -a ehr_ids=()

# EHR data templates
declare -a tpl_condition=("Viem hong cap tinh" "Tang huyet ap giai doan 1" "Viem phe quan cap o tre em")
declare -a tpl_condCode=("J02.9" "I10" "J20.9")
declare -a tpl_medication=("Amoxicillin 500mg" "Amlodipine 5mg" "Salbutamol khi dung 2.5mg")
declare -a tpl_medDose=("Uong 3 lan/ngay sau an, 7 ngay" "Uong 1 lan/ngay buoi sang" "Khi dung 3 lan/ngay")
declare -a tpl_temp=(37.5 36.8 38.2)
declare -a tpl_bp=(120 145 90)
declare -a tpl_hr=(80 88 110)
declare -a tpl_spO2=(98 97 95)
declare -a tpl_weight=(65 72 18)
declare -a tpl_height=(170 165 105)
declare -a tpl_notes=(
    "Hoan tat kham. Chan doan: Viem hong cap. Phac do: Amoxicillin 500mg x 7 ngay. Tai kham sau 7 ngay."
    "Hoan tat kham. Chan doan: Tang huyet ap giai doan 1. Ke don Amlodipine 5mg. Hen tai kham 1 thang."
    "Hoan tat kham. Chan doan: Viem phe quan cap. Khi dung Salbutamol + theo doi SpO2. Tai kham 3 ngay."
)

pastIdx=0
for ci in $(seq 0 11); do
    if [ "${apt_isPast[$ci]}" -eq 1 ] && [ -n "${apt_aptId[$ci]}" ]; then
        di=${apt_docIdxArr[$ci]}
        pi=${apt_patIdxArr[$ci]}
        docToken="${doc_token[$di]}"
        patientId="${pat_patientId[$pi]}"
        doctorId="${doc_doctorId[$di]}"
        patientName="${pat_fullName[$pi]}"
        ti=$((pastIdx % 3))

        # Check-in
        api PUT "$APTU/${apt_aptId[$ci]}/check-in" "" "$docToken" >/dev/null
        echo "  Checked in: $patientName"

        # Create encounter
        encBody=$(jq -n --arg p "$patientId" --arg d "$doctorId" --arg a "${apt_aptId[$ci]}" --arg o "${apt_orgIdArr[$ci]}" --arg n "Kham benh cho $patientName - ${tpl_condition[$ti]}" \
            '{patientId:$p,doctorId:$d,appointmentId:$a,orgId:$o,notes:$n}')
        encResult=$(api POST "$ENCU" "$encBody" "$docToken")
        encId=$(jval "$encResult" '.data.encounterId // .encounterId')
        enc_encId+=("$encId")
        echo "  Encounter: $patientName | encId=$encId"

        # Complete encounter with rich EHR data
        if [ -n "$encId" ]; then
            ehrData=$(cat <<EHRJSON
{
  "notes": "${tpl_notes[$ti]}",
  "ehrData": {
    "resourceType": "Bundle",
    "type": "document",
    "entry": [
      {
        "resource": {
          "resourceType": "Condition",
          "code": {"text": "${tpl_condition[$ti]}", "coding": [{"system": "http://hl7.org/fhir/sid/icd-10", "code": "${tpl_condCode[$ti]}", "display": "${tpl_condition[$ti]}"}]},
          "clinicalStatus": {"coding": [{"system": "http://terminology.hl7.org/CodeSystem/condition-clinical", "code": "active", "display": "Active"}]},
          "severity": {"coding": [{"system": "http://snomed.info/sct", "code": "24484000", "display": "Severe"}]},
          "note": [{"text": "Benh nhan den kham voi trieu chung ro rang"}]
        }
      },
      {
        "resource": {
          "resourceType": "MedicationRequest",
          "status": "active",
          "intent": "order",
          "medicationCodeableConcept": {"text": "${tpl_medication[$ti]}", "coding": [{"system": "http://www.nlm.nih.gov/research/umls/rxnorm", "display": "${tpl_medication[$ti]}"}]},
          "dosageInstruction": [{"text": "${tpl_medDose[$ti]}", "timing": {"repeat": {"frequency": 3, "period": 1, "periodUnit": "d"}}, "route": {"text": "Uong"}}],
          "dispenseRequest": {"numberOfRepeatsAllowed": 0, "quantity": {"value": 21, "unit": "vien"}, "expectedSupplyDuration": {"value": 7, "unit": "ngay"}}
        }
      },
      {
        "resource": {
          "resourceType": "Observation",
          "status": "final",
          "code": {"text": "Vital Signs"},
          "component": [
            {"code": {"text": "Nhiet do co the"}, "valueQuantity": {"value": ${tpl_temp[$ti]}, "unit": "°C", "system": "http://unitsofmeasure.org", "code": "Cel"}},
            {"code": {"text": "Huyet ap tam thu"}, "valueQuantity": {"value": ${tpl_bp[$ti]}, "unit": "mmHg", "system": "http://unitsofmeasure.org", "code": "mm[Hg]"}},
            {"code": {"text": "Nhip tim"}, "valueQuantity": {"value": ${tpl_hr[$ti]}, "unit": "bpm", "system": "http://unitsofmeasure.org", "code": "/min"}},
            {"code": {"text": "SpO2"}, "valueQuantity": {"value": ${tpl_spO2[$ti]}, "unit": "%", "system": "http://unitsofmeasure.org", "code": "%"}},
            {"code": {"text": "Can nang"}, "valueQuantity": {"value": ${tpl_weight[$ti]}, "unit": "kg", "system": "http://unitsofmeasure.org", "code": "kg"}},
            {"code": {"text": "Chieu cao"}, "valueQuantity": {"value": ${tpl_height[$ti]}, "unit": "cm", "system": "http://unitsofmeasure.org", "code": "cm"}}
          ]
        }
      },
      {
        "resource": {
          "resourceType": "AllergyIntolerance",
          "clinicalStatus": {"coding": [{"code": "active"}]},
          "type": "allergy",
          "category": ["medication"],
          "code": {"text": "Penicillin"},
          "reaction": [{"manifestation": [{"text": "Phat ban da"}], "severity": "mild"}]
        }
      }
    ]
  }
}
EHRJSON
)
            api PUT "$ENCU/$encId/complete" "$ehrData" "$docToken" >/dev/null
            echo "  Completed encounter with EHR"
        fi

        pastIdx=$((pastIdx + 1))
    fi
done
# =============================================================================
# 10. STANDALONE EHR RECORDS (additional records with rich data)
# =============================================================================
echo ""
echo "--- 10. Standalone EHR Records ---"

declare -a sehr_patIdx=(0 1 2 3 4)
declare -a sehr_docIdx=(0 1 2 0 3)
declare -a sehr_orgVar=("hospitalAId" "hospitalAId" "hospitalBId" "hospitalAId" "hospitalBId")
declare -a sehr_condition=("Kham suc khoe tong quat" "Kham tim mach dinh ky" "Kham Nhi tong quat" "Kham noi khoa dinh ky" "Kham truoc phau thuat")
declare -a sehr_bmi=(22.5 25.1 16.2 21.8 20.3)
declare -a sehr_bp=(118 130 88 115 122)
declare -a sehr_hr=(72 85 95 68 76)

for i in 0 1 2 3 4; do
    di=${sehr_docIdx[$i]}
    pi=${sehr_patIdx[$i]}
    docToken="${doc_token[$di]}"
    patientId="${pat_patientId[$pi]}"
    doctorId="${doc_doctorId[$di]}"

    case "${sehr_orgVar[$i]}" in
        hospitalAId) orgId="$hospitalAId" ;;
        hospitalBId) orgId="$hospitalBId" ;;
        clinicId)    orgId="$clinicId" ;;
    esac

    if [ -z "$docToken" ]; then
        echo "  SKIP EHR: ${pat_fullName[$pi]} - no doctor token"
        continue
    fi
    if [ -z "$orgId" ]; then
        echo "  SKIP EHR: ${pat_fullName[$pi]} - no orgId"
        continue
    fi

    ehrBody=$(cat <<EHRJSON2
{
  "patientId": "$patientId",
  "orgId": "$orgId",
  "data": {
    "resourceType": "Bundle",
    "type": "document",
    "entry": [
      {
        "resource": {
          "resourceType": "Condition",
          "code": {"text": "${sehr_condition[$i]}"},
          "clinicalStatus": {"coding": [{"code": "resolved", "display": "Resolved"}]},
          "note": [{"text": "Ket qua kham binh thuong"}]
        }
      },
      {
        "resource": {
          "resourceType": "Observation",
          "status": "final",
          "code": {"text": "BMI"},
          "valueQuantity": {"value": ${sehr_bmi[$i]}, "unit": "kg/m2", "system": "http://unitsofmeasure.org", "code": "kg/m2"}
        }
      },
      {
        "resource": {
          "resourceType": "Observation",
          "status": "final",
          "code": {"text": "Vital Signs"},
          "component": [
            {"code": {"text": "Huyet ap"}, "valueQuantity": {"value": ${sehr_bp[$i]}, "unit": "mmHg"}},
            {"code": {"text": "Nhip tim"}, "valueQuantity": {"value": ${sehr_hr[$i]}, "unit": "bpm"}}
          ]
        }
      }
    ]
  }
}
EHRJSON2
)
    ehrResult=$(api_h POST "$EHRU" "$ehrBody" "$docToken" "X-Doctor-Id" "$doctorId")
    ehrId=$(jval "$ehrResult" '.data.ehrId // .ehrId')
    [ -n "$ehrId" ] && ehr_ids+=("$ehrId")
    echo "  EHR: ${pat_fullName[$pi]} - ${sehr_condition[$i]} | ehrId=$ehrId"
done

# =============================================================================
# 10b. EHR VERSION UPDATES (update existing records to create versions)
# =============================================================================
echo ""
echo "--- 10b. EHR Version Updates ---"

if [ ${#ehr_ids[@]} -gt 0 ]; then
    updateEhrId="${ehr_ids[0]}"
    updateBody=$(cat <<'EHRUPD1'
{
  "data": {
    "resourceType": "Bundle",
    "type": "document",
    "entry": [
      {
        "resource": {
          "resourceType": "Condition",
          "code": {"text": "Kham suc khoe tong quat - Tai kham"},
          "clinicalStatus": {"coding": [{"code": "resolved", "display": "Resolved"}]},
          "note": [{"text": "Tai kham lan 2: Ket qua on dinh, khong phat hien bat thuong"}]
        }
      },
      {
        "resource": {
          "resourceType": "Observation",
          "status": "final",
          "code": {"text": "Vital Signs - Tai kham"},
          "component": [
            {"code": {"text": "Huyet ap"}, "valueQuantity": {"value": 115, "unit": "mmHg"}},
            {"code": {"text": "Nhip tim"}, "valueQuantity": {"value": 70, "unit": "bpm"}},
            {"code": {"text": "Can nang"}, "valueQuantity": {"value": 64, "unit": "kg"}},
            {"code": {"text": "BMI"}, "valueQuantity": {"value": 22.1, "unit": "kg/m2"}}
          ]
        }
      },
      {
        "resource": {
          "resourceType": "DiagnosticReport",
          "status": "final",
          "code": {"text": "Xet nghiem mau tong quat"},
          "conclusion": "Chi so mau trong gioi han binh thuong. WBC: 7.2, RBC: 4.8, Hb: 14.5, Plt: 250"
        }
      }
    ]
  }
}
EHRUPD1
)
    api PUT "$EHRU/$updateEhrId" "$updateBody" "${doc_token[0]}" >/dev/null
    echo "  Updated EHR (v2): $updateEhrId - Tai kham + Xet nghiem mau"

    if [ ${#ehr_ids[@]} -gt 1 ]; then
        updateEhrId2="${ehr_ids[1]}"
        updateBody2=$(cat <<'EHRUPD2'
{
  "data": {
    "resourceType": "Bundle",
    "type": "document",
    "entry": [
      {
        "resource": {
          "resourceType": "Condition",
          "code": {"text": "Tang huyet ap - Dieu chinh thuoc"},
          "clinicalStatus": {"coding": [{"code": "active", "display": "Active"}]},
          "note": [{"text": "Tang lieu Amlodipine 5mg -> 10mg do huyet ap chua on dinh"}]
        }
      },
      {
        "resource": {
          "resourceType": "MedicationRequest",
          "status": "active",
          "intent": "order",
          "medicationCodeableConcept": {"text": "Amlodipine 10mg", "coding": [{"system": "http://www.nlm.nih.gov/research/umls/rxnorm", "display": "Amlodipine 10mg"}]},
          "dosageInstruction": [{"text": "Uong 1 lan/ngay buoi sang, sau an", "route": {"text": "Uong"}}]
        }
      }
    ]
  }
}
EHRUPD2
)
        api PUT "$EHRU/$updateEhrId2" "$updateBody2" "${doc_token[1]}" >/dev/null
        echo "  Updated EHR (v2): $updateEhrId2 - Dieu chinh thuoc tang huyet ap"
    fi
else
    echo "  SKIP: No EHR IDs available for version update"
fi
# =============================================================================
# 11. CONSENTS (patients grant access to doctors and orgs)
# =============================================================================
echo ""
echo "--- 11. Consents ---"

# Patients 0,1,2 grant consent to Dr Hieu for TREATMENT
for i in 0 1 2; do
    patToken="${pat_token[$i]}"
    if [ -z "$patToken" ]; then
        echo "  SKIP Consent: ${pat_fullName[$i]} - no token"
        continue
    fi
    body=$(jq -n \
        --arg pi "${pat_patientId[$i]}" --arg pd "${pat_did[$i]}" \
        --arg gi "${doc_doctorId[0]}" --arg gd "${doc_did[0]}" \
        '{patientId:$pi,patientDid:$pd,granteeId:$gi,granteeDid:$gd,granteeType:"DOCTOR",permission:"READ",purpose:"TREATMENT",durationDays:90}')
    consent=$(api POST "$CONSU" "$body" "$patToken")
    consentId=$(jval "$consent" '.data.consentId // .consentId')
    echo "  Consent: ${pat_fullName[$i]} -> ${doc_fullName[0]} (READ/TREATMENT) | consentId=$consentId"
done

# Patient 0 grants FULL_ACCESS to Dr Lan for RESEARCH
body=$(jq -n \
    --arg pi "${pat_patientId[0]}" --arg pd "${pat_did[0]}" \
    --arg gi "${doc_doctorId[1]}" --arg gd "${doc_did[1]}" \
    '{patientId:$pi,patientDid:$pd,granteeId:$gi,granteeDid:$gd,granteeType:"DOCTOR",permission:"FULL_ACCESS",purpose:"RESEARCH",durationDays:365}')
api POST "$CONSU" "$body" "${pat_token[0]}" >/dev/null
echo "  Consent: ${pat_fullName[0]} -> ${doc_fullName[1]} (FULL_ACCESS/RESEARCH)"

# Patient 0 grants consent to Hospital A (org)
body=$(jq -n \
    --arg pi "${pat_patientId[0]}" --arg pd "${pat_did[0]}" \
    --arg gi "$hospitalAId" --arg gd "did:dbh:org:$hospitalAId" \
    '{patientId:$pi,patientDid:$pd,granteeId:$gi,granteeDid:$gd,granteeType:"ORGANIZATION",permission:"READ",purpose:"TREATMENT",durationDays:365}')
api POST "$CONSU" "$body" "${pat_token[0]}" >/dev/null
echo "  Consent: ${pat_fullName[0]} -> Hospital A (ORG/READ)"

# Patient 2 grants consent to Dr Phuoc
body=$(jq -n \
    --arg pi "${pat_patientId[2]}" --arg pd "${pat_did[2]}" \
    --arg gi "${doc_doctorId[2]}" --arg gd "${doc_did[2]}" \
    '{patientId:$pi,patientDid:$pd,granteeId:$gi,granteeDid:$gd,granteeType:"DOCTOR",permission:"READ",purpose:"TREATMENT",durationDays:180}')
api POST "$CONSU" "$body" "${pat_token[2]}" >/dev/null
echo "  Consent: ${pat_fullName[2]} -> ${doc_fullName[2]} (READ/TREATMENT)"

# Patient 4 grants consent to Hospital B
body=$(jq -n \
    --arg pi "${pat_patientId[4]}" --arg pd "${pat_did[4]}" \
    --arg gi "$hospitalBId" --arg gd "did:dbh:org:$hospitalBId" \
    '{patientId:$pi,patientDid:$pd,granteeId:$gi,granteeDid:$gd,granteeType:"ORGANIZATION",permission:"READ",purpose:"TREATMENT",durationDays:365}')
api POST "$CONSU" "$body" "${pat_token[4]}" >/dev/null
echo "  Consent: ${pat_fullName[4]} -> Hospital B (ORG/READ)"

# =============================================================================
# 11b. ACCESS REQUESTS (doctor requests access -> patient responds)
# =============================================================================
echo ""
echo "--- 11b. Access Requests ---"

ACCESS_REQ="$BASE/api/v1/access-requests"

# Dr Tuan requests access to Patient Dung
body=$(jq -n \
    --arg pi "${pat_patientId[3]}" --arg pd "${pat_did[3]}" \
    --arg ri "${doc_doctorId[3]}" --arg rd "${doc_did[3]}" --arg oi "$hospitalBId" \
    '{patientId:$pi,patientDid:$pd,requesterId:$ri,requesterDid:$rd,requesterType:"DOCTOR",organizationId:$oi,permission:"READ",purpose:"TREATMENT",reason:"Can xem ho so benh nhan de chuan bi phau thuat. Benh nhan chuyen tu BV khac.",requestedDurationDays:30}')
ar1=$(api POST "$ACCESS_REQ" "$body" "${doc_token[3]}")
ar1Id=$(jval "$ar1" '.data.accessRequestId // .data.id // .accessRequestId')
echo "  AccessRequest: ${doc_fullName[3]} -> ${pat_fullName[3]} (TREATMENT) | id=$ar1Id"

if [ -n "$ar1Id" ]; then
    api POST "$ACCESS_REQ/$ar1Id/respond" '{"approve":true,"responseReason":"Dong y cho bac si xem ho so de chuan bi mo."}' "${pat_token[3]}" >/dev/null
    echo "  Approved: ${pat_fullName[3]} approved ${doc_fullName[3]}'s request"
fi

# Dr Lan requests access to Patient Cuong (research)
body=$(jq -n \
    --arg pi "${pat_patientId[2]}" --arg pd "${pat_did[2]}" \
    --arg ri "${doc_doctorId[1]}" --arg rd "${doc_did[1]}" --arg oi "$hospitalAId" \
    '{patientId:$pi,patientDid:$pd,requesterId:$ri,requesterDid:$rd,requesterType:"DOCTOR",organizationId:$oi,permission:"READ",purpose:"RESEARCH",reason:"Nghien cuu lam sang ve benh phe quan o tre em. Can truy cap du lieu ket qua dieu tri.",requestedDurationDays:90}')
ar2=$(api POST "$ACCESS_REQ" "$body" "${doc_token[1]}")
ar2Id=$(jval "$ar2" '.data.accessRequestId // .data.id // .accessRequestId')
echo "  AccessRequest: ${doc_fullName[1]} -> ${pat_fullName[2]} (RESEARCH) | id=$ar2Id"

if [ -n "$ar2Id" ]; then
    api POST "$ACCESS_REQ/$ar2Id/respond" '{"approve":false,"responseReason":"Toi khong dong y chia se ho so cho muc dich nghien cuu."}' "${pat_token[2]}" >/dev/null
    echo "  Denied: ${pat_fullName[2]} denied ${doc_fullName[1]}'s research request"
fi

# Nurse Hoa requests access to Patient An
body=$(jq -n \
    --arg pi "${pat_patientId[0]}" --arg pd "${pat_did[0]}" \
    --arg ri "${stf_userId[0]}" --arg rd "${stf_did[0]}" --arg oi "$hospitalAId" \
    '{patientId:$pi,patientDid:$pd,requesterId:$ri,requesterDid:$rd,requesterType:"NURSE",organizationId:$oi,permission:"READ",purpose:"TREATMENT",reason:"Can xem ho so benh nhan de theo doi sinh hieu va cham soc dieu duong.",requestedDurationDays:14}')
ar3=$(api POST "$ACCESS_REQ" "$body" "${stf_token[0]}")
ar3Id=$(jval "$ar3" '.data.accessRequestId // .data.id // .accessRequestId')
echo "  AccessRequest: ${stf_fullName[0]} -> ${pat_fullName[0]} (NURSING) | id=$ar3Id"

if [ -n "$ar3Id" ]; then
    api POST "$ACCESS_REQ/$ar3Id/respond" '{"approve":true,"responseReason":"Dong y cho dieu duong xem ho so."}' "${pat_token[0]}" >/dev/null
    echo "  Approved: ${pat_fullName[0]} approved ${stf_fullName[0]}'s request"
fi

# =============================================================================
# 11c. CONSENT REVOCATION (revoke one consent for lifecycle demo)
# =============================================================================
echo ""
echo "--- 11c. Consent Revocation ---"

patientAnConsents=$(api GET "$CONSU/by-patient/${pat_patientId[0]}" "" "${pat_token[0]}")
revokeId=$(echo "$patientAnConsents" | jq -r '(.data // .) | if type=="array" then .[] else empty end | select(.purpose=="RESEARCH" and .status!="REVOKED") | .consentId // empty' 2>/dev/null | head -1)
if [ -n "$revokeId" ]; then
    api POST "$CONSU/$revokeId/revoke" '{"revokeReason":"Benh nhan thay doi y dinh, khong muon tham gia nghien cuu nua."}' "${pat_token[0]}" >/dev/null
    echo "  Revoked: ${pat_fullName[0]} revoked RESEARCH consent to ${doc_fullName[1]} | consentId=$revokeId"
else
    echo "  SKIP: No RESEARCH consent found to revoke"
fi
# =============================================================================
# 12. AUDIT LOGS (comprehensive - all fields filled)
# =============================================================================
echo ""
echo "--- 12. Audit Logs ---"

auditCount=0

create_audit() {
    api POST "$AUDIT" "$1" "$adminToken" >/dev/null
    auditCount=$((auditCount + 1))
}

create_audit "{\"actorDid\":\"${pat_did[0]}\",\"actorUserId\":\"${pat_userId[0]}\",\"actorType\":\"PATIENT\",\"action\":\"LOGIN\",\"targetType\":\"USER\",\"result\":\"SUCCESS\",\"ipAddress\":\"192.168.1.100\",\"userAgent\":\"Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/120.0\",\"metadata\":\"{\\\"device\\\":\\\"Desktop\\\",\\\"browser\\\":\\\"Chrome\\\",\\\"os\\\":\\\"Windows 10\\\"}\"}"

create_audit "{\"actorDid\":\"${doc_did[0]}\",\"actorUserId\":\"${doc_userId[0]}\",\"actorType\":\"DOCTOR\",\"action\":\"LOGIN\",\"targetType\":\"USER\",\"result\":\"SUCCESS\",\"ipAddress\":\"192.168.1.50\",\"userAgent\":\"Mozilla/5.0 (Macintosh; Intel Mac OS X) Safari/605.1\",\"metadata\":\"{\\\"device\\\":\\\"Macbook\\\",\\\"browser\\\":\\\"Safari\\\",\\\"os\\\":\\\"macOS\\\"}\"}"

create_audit "{\"actorDid\":\"${doc_did[0]}\",\"actorUserId\":\"${doc_userId[0]}\",\"actorType\":\"DOCTOR\",\"action\":\"VIEW\",\"targetType\":\"EHR\",\"result\":\"SUCCESS\",\"patientDid\":\"${pat_did[0]}\",\"patientId\":\"${pat_patientId[0]}\",\"organizationId\":\"$hospitalAId\",\"ipAddress\":\"192.168.1.50\",\"userAgent\":\"DBH-EHR-App/1.0\",\"metadata\":\"{\\\"ehrId\\\":\\\"viewed\\\",\\\"action\\\":\\\"read_full_record\\\"}\"}"

create_audit "{\"actorDid\":\"${doc_did[0]}\",\"actorUserId\":\"${doc_userId[0]}\",\"actorType\":\"DOCTOR\",\"action\":\"CREATE\",\"targetType\":\"EHR\",\"result\":\"SUCCESS\",\"patientDid\":\"${pat_did[0]}\",\"patientId\":\"${pat_patientId[0]}\",\"organizationId\":\"$hospitalAId\",\"ipAddress\":\"192.168.1.50\",\"userAgent\":\"DBH-EHR-App/1.0\",\"metadata\":\"{\\\"action\\\":\\\"create_ehr_record\\\",\\\"condition\\\":\\\"Viem hong cap\\\"}\"}"

create_audit "{\"actorDid\":\"${pat_did[0]}\",\"actorUserId\":\"${pat_userId[0]}\",\"actorType\":\"PATIENT\",\"action\":\"GRANT_CONSENT\",\"targetType\":\"CONSENT\",\"result\":\"SUCCESS\",\"patientDid\":\"${pat_did[0]}\",\"patientId\":\"${pat_patientId[0]}\",\"ipAddress\":\"192.168.1.100\",\"userAgent\":\"DBH-Mobile/2.0 Android\",\"metadata\":\"{\\\"grantee\\\":\\\"dr.hieu\\\",\\\"permission\\\":\\\"READ\\\",\\\"purpose\\\":\\\"TREATMENT\\\"}\"}"

create_audit "{\"actorDid\":\"${doc_did[1]}\",\"actorUserId\":\"${doc_userId[1]}\",\"actorType\":\"DOCTOR\",\"action\":\"VIEW\",\"targetType\":\"EHR\",\"result\":\"DENIED\",\"patientDid\":\"${pat_did[3]}\",\"patientId\":\"${pat_patientId[3]}\",\"organizationId\":\"$hospitalAId\",\"ipAddress\":\"192.168.1.51\",\"userAgent\":\"DBH-EHR-App/1.0\",\"errorMessage\":\"Khong co quyen truy cap ho so benh nhan\",\"metadata\":\"{\\\"reason\\\":\\\"no_consent\\\"}\"}"

create_audit "{\"actorDid\":\"did:dbh:system\",\"actorType\":\"SYSTEM\",\"action\":\"VIEW\",\"targetType\":\"SYSTEM\",\"result\":\"SUCCESS\",\"ipAddress\":\"127.0.0.1\",\"userAgent\":\"DBH-System-Monitor/1.0\",\"metadata\":\"{\\\"event\\\":\\\"health_check\\\",\\\"services_healthy\\\":8,\\\"uptime_hours\\\":24}\"}"

create_audit "{\"actorDid\":\"${pat_did[1]}\",\"actorUserId\":\"${pat_userId[1]}\",\"actorType\":\"PATIENT\",\"action\":\"DOWNLOAD\",\"targetType\":\"EHR\",\"result\":\"SUCCESS\",\"patientDid\":\"${pat_did[1]}\",\"patientId\":\"${pat_patientId[1]}\",\"ipAddress\":\"10.0.0.55\",\"userAgent\":\"DBH-Mobile/2.0 iOS\",\"metadata\":\"{\\\"format\\\":\\\"PDF\\\",\\\"records_count\\\":2}\"}"

create_audit "{\"actorDid\":\"${doc_did[2]}\",\"actorUserId\":\"${doc_userId[2]}\",\"actorType\":\"DOCTOR\",\"action\":\"CREATE\",\"targetType\":\"EHR\",\"result\":\"SUCCESS\",\"patientDid\":\"${pat_did[2]}\",\"patientId\":\"${pat_patientId[2]}\",\"organizationId\":\"$hospitalBId\",\"ipAddress\":\"192.168.2.30\",\"userAgent\":\"DBH-EHR-App/1.0\",\"metadata\":\"{\\\"action\\\":\\\"create_pediatric_record\\\",\\\"hospital\\\":\\\"Nhi Dong 1\\\"}\"}"

create_audit "{\"actorDid\":\"${stf_did[0]}\",\"actorUserId\":\"${stf_userId[0]}\",\"actorType\":\"NURSE\",\"action\":\"UPDATE\",\"targetType\":\"EHR\",\"result\":\"SUCCESS\",\"patientDid\":\"${pat_did[0]}\",\"patientId\":\"${pat_patientId[0]}\",\"organizationId\":\"$hospitalAId\",\"ipAddress\":\"192.168.1.60\",\"userAgent\":\"DBH-Nursing-Station/1.0\",\"metadata\":\"{\\\"action\\\":\\\"update_vitals\\\",\\\"temperature\\\":37.2,\\\"bp\\\":\\\"120/80\\\"}\"}"

echo "  Created $auditCount audit logs (all fields populated)"
# =============================================================================
# 13. NOTIFICATIONS (all fields filled)
# =============================================================================
echo ""
echo "--- 13. Notifications ---"

notifCount=0
create_notif() {
    api POST "$NOTIF" "$1" "$adminToken" >/dev/null
    notifCount=$((notifCount + 1))
}

create_notif "{\"recipientDid\":\"${pat_did[0]}\",\"recipientUserId\":\"${pat_userId[0]}\",\"title\":\"Lich hen sap toi\",\"body\":\"Ban co lich kham voi BS. Tran Minh Hieu vao ngay mai luc 9:00 tai Khoa Noi, Phong 205, Tang 2.\",\"type\":\"AppointmentReminder\",\"priority\":\"High\",\"channel\":\"InApp\",\"referenceId\":\"apt-001\",\"referenceType\":\"Appointment\",\"actionUrl\":\"/appointments/upcoming\",\"data\":\"{\\\"doctorName\\\":\\\"BS. Tran Minh Hieu\\\",\\\"department\\\":\\\"Khoa Noi\\\",\\\"room\\\":\\\"205\\\",\\\"time\\\":\\\"09:00\\\"}\"}"

create_notif "{\"recipientDid\":\"${pat_did[1]}\",\"recipientUserId\":\"${pat_userId[1]}\",\"title\":\"Ho so benh an da cap nhat\",\"body\":\"BS. Nguyen Thi Lan da cap nhat ho so benh an cua ban voi ket qua kham Tim mach ngay hom nay.\",\"type\":\"EhrUpdate\",\"priority\":\"Normal\",\"channel\":\"InApp\",\"referenceId\":\"ehr-update-001\",\"referenceType\":\"EHR\",\"actionUrl\":\"/ehr/my-records\",\"data\":\"{\\\"doctorName\\\":\\\"BS. Nguyen Thi Lan\\\",\\\"updateType\\\":\\\"new_diagnosis\\\",\\\"condition\\\":\\\"Tang huyet ap\\\"}\"}"

create_notif "{\"recipientDid\":\"${doc_did[0]}\",\"recipientUserId\":\"${doc_userId[0]}\",\"title\":\"Yeu cau truy cap ho so\",\"body\":\"Benh nhan Nguyen Van An da cap quyen truy cap ho so benh an cho ban. Quyen: Xem (READ), Muc dich: Dieu tri.\",\"type\":\"ConsentGranted\",\"priority\":\"High\",\"channel\":\"InApp\",\"referenceId\":\"consent-001\",\"referenceType\":\"Consent\",\"actionUrl\":\"/doctor/consents\",\"data\":\"{\\\"patientName\\\":\\\"Nguyen Van An\\\",\\\"permission\\\":\\\"READ\\\",\\\"purpose\\\":\\\"TREATMENT\\\",\\\"duration\\\":\\\"90 ngay\\\"}\"}"

create_notif "{\"recipientDid\":\"${pat_did[2]}\",\"recipientUserId\":\"${pat_userId[2]}\",\"title\":\"Nhac nho uong thuoc\",\"body\":\"Hay uong thuoc Salbutamol khi dung theo chi dinh cua BS. Le Van Phuoc. Lieu: 3 lan/ngay.\",\"type\":\"System\",\"priority\":\"Normal\",\"channel\":\"InApp\",\"referenceId\":\"med-reminder-001\",\"referenceType\":\"MedicationReminder\",\"actionUrl\":\"/medications\",\"data\":\"{\\\"medication\\\":\\\"Salbutamol\\\",\\\"dosage\\\":\\\"3 lan/ngay\\\",\\\"doctorName\\\":\\\"BS. Le Van Phuoc\\\"}\"}"

create_notif "{\"recipientDid\":\"${pat_did[0]}\",\"recipientUserId\":\"${pat_userId[0]}\",\"title\":\"Canh bao bao mat\",\"body\":\"Tai khoan cua ban vua duoc dang nhap tu thiet bi moi: Windows Desktop, Chrome 120.0. Neu khong phai ban, hay doi mat khau ngay.\",\"type\":\"SecurityAlert\",\"priority\":\"Urgent\",\"channel\":\"InApp\",\"referenceId\":\"security-001\",\"referenceType\":\"Security\",\"actionUrl\":\"/settings/security\",\"data\":\"{\\\"device\\\":\\\"Windows Desktop\\\",\\\"browser\\\":\\\"Chrome 120.0\\\",\\\"ip\\\":\\\"192.168.1.100\\\",\\\"location\\\":\\\"TP.HCM, VN\\\"}\"}"

create_notif "{\"recipientDid\":\"${doc_did[1]}\",\"recipientUserId\":\"${doc_userId[1]}\",\"title\":\"Benh nhan moi check-in\",\"body\":\"Benh nhan Tran Thi Binh da check-in tai Phong 305, Khoa Tim mach. Vui long tien hanh kham.\",\"type\":\"AppointmentCheckedIn\",\"priority\":\"High\",\"channel\":\"InApp\",\"referenceId\":\"checkin-001\",\"referenceType\":\"Appointment\",\"actionUrl\":\"/doctor/appointments/today\",\"data\":\"{\\\"patientName\\\":\\\"Tran Thi Binh\\\",\\\"room\\\":\\\"305\\\",\\\"department\\\":\\\"Khoa Tim mach\\\"}\"}"

create_notif "{\"recipientDid\":\"${pat_did[3]}\",\"recipientUserId\":\"${pat_userId[3]}\",\"title\":\"Ket qua xet nghiem\",\"body\":\"Ket qua xet nghiem mau cua ban da co. Vui long lien he BS. Tran Minh Hieu de duoc tu van chi tiet.\",\"type\":\"EhrUpdate\",\"priority\":\"Normal\",\"channel\":\"InApp\",\"referenceId\":\"lab-result-001\",\"referenceType\":\"LabResult\",\"actionUrl\":\"/ehr/lab-results\",\"data\":\"{\\\"testType\\\":\\\"Xet nghiem cong thuc mau\\\",\\\"doctorName\\\":\\\"BS. Tran Minh Hieu\\\",\\\"status\\\":\\\"completed\\\"}\"}"

create_notif "{\"recipientDid\":\"${pat_did[4]}\",\"recipientUserId\":\"${pat_userId[4]}\",\"title\":\"Lich hen da xac nhan\",\"body\":\"Lich hen kham voi BS. Pham Anh Tuan tai BV Nhi Dong 1, Khoa Cap cuu Nhi da duoc xac nhan.\",\"type\":\"AppointmentCreated\",\"priority\":\"Normal\",\"channel\":\"InApp\",\"referenceId\":\"apt-confirm-001\",\"referenceType\":\"Appointment\",\"actionUrl\":\"/appointments\",\"data\":\"{\\\"doctorName\\\":\\\"BS. Pham Anh Tuan\\\",\\\"hospital\\\":\\\"BV Nhi Dong 1\\\",\\\"department\\\":\\\"Khoa Cap cuu Nhi\\\"}\"}"

echo "  Created $notifCount notifications (all fields populated)"
# =============================================================================
# 13b. NOTIFICATION PREFERENCES (user notification settings)
# =============================================================================
echo ""
echo "--- 13b. Notification Preferences ---"

PREF="$BASE/api/v1/notifications/preferences"

# Patient An: enable all, set quiet hours
api PUT "$PREF/by-user/${pat_did[0]}" '{"ehrAccessEnabled":true,"consentRequestEnabled":true,"ehrUpdateEnabled":true,"appointmentReminderEnabled":true,"securityAlertEnabled":true,"systemNotificationEnabled":true,"pushEnabled":true,"emailEnabled":true,"smsEnabled":false,"quietHoursEnabled":true,"quietHoursStart":2200,"quietHoursEnd":700}' "${pat_token[0]}" >/dev/null
echo "  Preferences: ${pat_fullName[0]} - all ON, quiet 22:00-07:00"

# Patient Binh: disable push, keep email
api PUT "$PREF/by-user/${pat_did[1]}" '{"ehrAccessEnabled":true,"consentRequestEnabled":true,"ehrUpdateEnabled":true,"appointmentReminderEnabled":true,"securityAlertEnabled":true,"systemNotificationEnabled":true,"pushEnabled":false,"emailEnabled":true,"smsEnabled":false,"quietHoursEnabled":false}' "${pat_token[1]}" >/dev/null
echo "  Preferences: ${pat_fullName[1]} - push OFF, email ON"

# Dr Hieu: all on, quiet hours during surgery
api PUT "$PREF/by-user/${doc_did[0]}" '{"ehrAccessEnabled":true,"consentRequestEnabled":true,"ehrUpdateEnabled":true,"appointmentReminderEnabled":true,"securityAlertEnabled":true,"systemNotificationEnabled":true,"pushEnabled":true,"emailEnabled":true,"smsEnabled":true,"quietHoursEnabled":true,"quietHoursStart":2300,"quietHoursEnd":600}' "${doc_token[0]}" >/dev/null
echo "  Preferences: ${doc_fullName[0]} - all ON + SMS, quiet 23:00-06:00"

# Dr Lan: minimal notifications
api PUT "$PREF/by-user/${doc_did[1]}" '{"ehrAccessEnabled":true,"consentRequestEnabled":true,"ehrUpdateEnabled":false,"appointmentReminderEnabled":true,"securityAlertEnabled":true,"systemNotificationEnabled":false,"pushEnabled":true,"emailEnabled":false,"smsEnabled":false,"quietHoursEnabled":false}' "${doc_token[1]}" >/dev/null
echo "  Preferences: ${doc_fullName[1]} - essential only"

# =============================================================================
# 13c. DEVICE TOKENS (push notification devices)
# =============================================================================
echo ""
echo "--- 13c. Device Tokens ---"

DEVICE="$BASE/api/v1/notifications/device-tokens"
TODAY=$(date '+%Y%m%d')

deviceCount=0
reg_device() {
    api POST "$DEVICE" "$1" "$2" >/dev/null
    deviceCount=$((deviceCount + 1))
}

reg_device "{\"userDid\":\"${pat_did[0]}\",\"userId\":\"${pat_userId[0]}\",\"fcmToken\":\"fcm_patient_an_android_${TODAY}\",\"deviceType\":\"Android\",\"deviceName\":\"Samsung Galaxy S24\",\"osVersion\":\"Android 14\",\"appVersion\":\"2.1.0\"}" "${pat_token[0]}"
reg_device "{\"userDid\":\"${pat_did[0]}\",\"userId\":\"${pat_userId[0]}\",\"fcmToken\":\"fcm_patient_an_web_${TODAY}\",\"deviceType\":\"Web\",\"deviceName\":\"Chrome Desktop\",\"osVersion\":\"Windows 11\",\"appVersion\":\"2.1.0\"}" "${pat_token[0]}"
reg_device "{\"userDid\":\"${pat_did[1]}\",\"userId\":\"${pat_userId[1]}\",\"fcmToken\":\"fcm_patient_binh_ios_${TODAY}\",\"deviceType\":\"iOS\",\"deviceName\":\"iPhone 15 Pro\",\"osVersion\":\"iOS 17.4\",\"appVersion\":\"2.1.0\"}" "${pat_token[1]}"
reg_device "{\"userDid\":\"${doc_did[0]}\",\"userId\":\"${doc_userId[0]}\",\"fcmToken\":\"fcm_dr_hieu_ipad_${TODAY}\",\"deviceType\":\"iOS\",\"deviceName\":\"iPad Pro 12.9\",\"osVersion\":\"iPadOS 17.4\",\"appVersion\":\"2.1.0\"}" "${doc_token[0]}"
reg_device "{\"userDid\":\"${doc_did[0]}\",\"userId\":\"${doc_userId[0]}\",\"fcmToken\":\"fcm_dr_hieu_macbook_${TODAY}\",\"deviceType\":\"Web\",\"deviceName\":\"Safari MacBook Pro\",\"osVersion\":\"macOS 14 Sonoma\",\"appVersion\":\"2.1.0\"}" "${doc_token[0]}"
reg_device "{\"userDid\":\"${stf_did[0]}\",\"userId\":\"${stf_userId[0]}\",\"fcmToken\":\"fcm_nurse_hoa_android_${TODAY}\",\"deviceType\":\"Android\",\"deviceName\":\"Nursing Station Tab\",\"osVersion\":\"Android 13\",\"appVersion\":\"2.0.5\"}" "${stf_token[0]}"

echo "  Registered $deviceCount device tokens"

# =============================================================================
# 13d. BROADCAST NOTIFICATION (admin system-wide announcement)
# =============================================================================
echo ""
echo "--- 13d. Broadcast Notification ---"

# Build allDids JSON array
allDids="["
first=true
for d in "${pat_did[@]}" "${doc_did[@]}" "${stf_did[@]}"; do
    $first || allDids+=","
    allDids+="\"$d\""
    first=false
done
allDids+="]"
allCount=$(echo "$allDids" | jq 'length')

body=$(jq -n --argjson r "$allDids" '{recipientDids:$r,title:"Thong bao bao tri he thong",body:"He thong DBH-EHR se duoc bao tri vao Chu Nhat 23:00-01:00. Trong thoi gian nay, mot so tinh nang co the tam ngung hoat dong. Vui long luu cong viec truoc 23:00. Xin cam on.",type:"System",priority:"High"}')
api POST "$NOTIF/broadcast" "$body" "$adminToken" >/dev/null
echo "  Broadcast: System maintenance notice -> $allCount recipients"

# Doctors-only broadcast
docDids="["
first=true
for d in "${doc_did[@]}"; do
    $first || docDids+=","
    docDids+="\"$d\""
    first=false
done
docDids+="]"

body=$(jq -n --argjson r "$docDids" '{recipientDids:$r,title:"Cap nhat huong dan lam sang moi",body:"Bo Y te da ban hanh huong dan lam sang moi ve dieu tri tang huyet ap 2024. Vui long xem tai muc Tai lieu -> Huong dan lam sang.",type:"System",priority:"Normal"}')
api POST "$NOTIF/broadcast" "$body" "$adminToken" >/dev/null
echo "  Broadcast: Clinical guidelines update -> ${#doc_did[@]} doctors"
# =============================================================================
# 14. INVOICES (for both hospitals - NO PayOS config needed for cash payments)
# =============================================================================
echo ""
echo "--- 14. Invoices ---"

TODAY_D=$(date '+%Y%m%d')

# Invoice 1: Hospital A, Dr Hieu -> Patient An
inv1=""
if [ -n "$hospitalAId" ] && [ -n "${doc_token[0]}" ] && [ -n "${pat_patientId[0]}" ]; then
    body=$(jq -n --arg p "${pat_patientId[0]}" --arg o "$hospitalAId" \
        '{patientId:$p,orgId:$o,notes:"Hoa don kham Noi khoa - Benh nhan Nguyen Van An",items:[{description:"Phi kham benh noi khoa",quantity:1,amount:200000},{description:"Xet nghiem cong thuc mau",quantity:1,amount:150000},{description:"Xet nghiem sinh hoa mau",quantity:1,amount:180000},{description:"Thuoc Amoxicillin 500mg x 21v",quantity:1,amount:63000}]}')
    inv1Result=$(api POST "$INVOICE" "$body" "${doc_token[0]}")
    inv1=$(jval "$inv1Result" '.data.invoiceId // .invoiceId')
fi
echo "  Invoice 1 (Hospital A): ${pat_fullName[0]} | 593,000 VND | id=$inv1"

# Invoice 2: Hospital A, Dr Lan -> Patient Binh
inv2=""
if [ -n "$hospitalAId" ] && [ -n "${doc_token[1]}" ] && [ -n "${pat_patientId[1]}" ]; then
    body=$(jq -n --arg p "${pat_patientId[1]}" --arg o "$hospitalAId" \
        '{patientId:$p,orgId:$o,notes:"Hoa don kham Tim mach - Benh nhan Tran Thi Binh",items:[{description:"Phi kham chuyen khoa Tim mach",quantity:1,amount:350000},{description:"Dien tam do (ECG)",quantity:1,amount:200000},{description:"Sieu am tim (Echocardiogram)",quantity:1,amount:500000},{description:"Thuoc Amlodipine 5mg x 30v",quantity:1,amount:90000}]}')
    inv2Result=$(api POST "$INVOICE" "$body" "${doc_token[1]}")
    inv2=$(jval "$inv2Result" '.data.invoiceId // .invoiceId')
fi
echo "  Invoice 2 (Hospital A): ${pat_fullName[1]} | 1,140,000 VND | id=$inv2"

# Invoice 3: Hospital B, Dr Phuoc -> Patient Cuong
inv3=""
if [ -n "$hospitalBId" ] && [ -n "${doc_token[2]}" ] && [ -n "${pat_patientId[2]}" ]; then
    body=$(jq -n --arg p "${pat_patientId[2]}" --arg o "$hospitalBId" \
        '{patientId:$p,orgId:$o,notes:"Hoa don kham Nhi khoa - Benh nhan Le Van Cuong",items:[{description:"Phi kham Nhi khoa",quantity:1,amount:250000},{description:"Khi dung Salbutamol",quantity:3,amount:50000},{description:"Chup X-quang phoi",quantity:1,amount:180000},{description:"Thuoc ho Dextromethorphan",quantity:1,amount:45000}]}')
    inv3Result=$(api POST "$INVOICE" "$body" "${doc_token[2]}")
    inv3=$(jval "$inv3Result" '.data.invoiceId // .invoiceId')
fi
echo "  Invoice 3 (Hospital B): ${pat_fullName[2]} | 625,000 VND | id=$inv3"

# Invoice 4: Hospital B, Dr Tuan -> Patient Em
inv4=""
if [ -n "$hospitalBId" ] && [ -n "${doc_token[3]}" ] && [ -n "${pat_patientId[4]}" ]; then
    body=$(jq -n --arg p "${pat_patientId[4]}" --arg o "$hospitalBId" \
        '{patientId:$p,orgId:$o,notes:"Hoa don kham truoc phau thuat - Benh nhan Hoang Van Em",items:[{description:"Phi kham truoc phau thuat",quantity:1,amount:300000},{description:"Xet nghiem mau tong quat",quantity:1,amount:250000},{description:"Xet nghiem dong mau",quantity:1,amount:200000},{description:"Dien tam do (ECG)",quantity:1,amount:200000},{description:"Chup X-quang nguc",quantity:1,amount:180000}]}')
    inv4Result=$(api POST "$INVOICE" "$body" "${doc_token[3]}")
    inv4=$(jval "$inv4Result" '.data.invoiceId // .invoiceId')
fi
echo "  Invoice 4 (Hospital B): ${pat_fullName[4]} | 1,130,000 VND | id=$inv4"

# Pay Invoice 1 with CASH
if [ -n "$inv1" ]; then
    api POST "$INVOICE/$inv1/pay-cash" "{\"transactionRef\":\"CASH-BVDKTU-${TODAY_D}-001\"}" "$adminToken" >/dev/null
    echo "  Paid Invoice 1 (CASH): $inv1"
fi

# Invoice 5: Clinic, Dr Hieu -> Patient Dung
inv5=""
if [ -n "$clinicId" ] && [ -n "${doc_token[0]}" ] && [ -n "${pat_patientId[3]}" ]; then
    body=$(jq -n --arg p "${pat_patientId[3]}" --arg o "$clinicId" \
        '{patientId:$p,orgId:$o,notes:"Hoa don kham Da lieu - Benh nhan Pham Thi Dung",items:[{description:"Phi kham Da lieu",quantity:1,amount:300000},{description:"Soi da bang dermoscopy",quantity:1,amount:200000},{description:"Thuoc boi da Tretinoin 0.05%",quantity:1,amount:85000}]}')
    inv5Result=$(api POST "$INVOICE" "$body" "${doc_token[0]}")
    inv5=$(jval "$inv5Result" '.data.invoiceId // .invoiceId')
fi
echo "  Invoice 5 (Clinic): ${pat_fullName[3]} | 585,000 VND | id=$inv5"

# Pay Invoice 5 with CASH
if [ -n "$inv5" ]; then
    api POST "$INVOICE/$inv5/pay-cash" "{\"transactionRef\":\"CASH-PKDLSG-${TODAY_D}-001\"}" "$adminToken" >/dev/null
    echo "  Paid Invoice 5 (CASH): $inv5"
fi

# Cancel Invoice 4
if [ -n "$inv4" ]; then
    api POST "$INVOICE/$inv4/cancel" "" "$adminToken" >/dev/null
    echo "  Cancelled Invoice 4: $inv4 (patient cancelled appointment)"
fi

echo "  Invoices 2,3 left UNPAID (for PayOS testing)"
# =============================================================================
# SUMMARY
# =============================================================================
echo ""
echo "====== FULL SEED DATA COMPLETE ======"
echo "  Finished at: $(date '+%Y-%m-%d %H:%M:%S')"
echo ""
echo "  === Data Summary ==="
echo "  Admin:               1"
echo "  Patients:            ${#pat_userId[@]}"
echo "  Doctors:             ${#doc_userId[@]}"
echo "  Staff:               ${#stf_userId[@]} (2 nurses, 1 receptionist, 1 pharmacist, 1 labtech)"
echo "  Organizations:       ${#org_orgId[@]} (2 hospitals, 1 clinic)"
echo "  Departments:         ${#dept_deptId[@]} (5 HospA + 3 HospB + 2 Clinic)"
echo "  Memberships:         11 (5 HospA + 4 HospB + 2 Clinic)"
echo "  Appointments:        ${#apt_aptId[@]} (9 future + 3 past, incl 1 rejected + 1 cancelled + 1 rescheduled)"
echo "  Encounters:          ${#enc_encId[@]} (completed with EHR)"
echo "  EHR Records:         ${#ehr_ids[@]} standalone + EHR version updates"
echo "  Consents:            7 (incl 1 REVOKED)"
echo "  Access Requests:     3 (2 approved, 1 denied)"
echo "  Audit Logs:          $auditCount"
echo "  Notifications:       $notifCount + 2 broadcasts"
echo "  Notif Preferences:   4 users configured"
echo "  Device Tokens:       $deviceCount"
echo "  Invoices:            5 (2 PAID cash, 1 CANCELLED, 2 UNPAID for PayOS)"
echo ""
echo "  === Test Accounts ==="
echo "  Admin:         admin@dbh.vn              / Admin@123456"
echo "  Patient 1:     patient.an@dbh.vn         / Patient@123"
echo "  Patient 2:     patient.binh@dbh.vn       / Patient@123"
echo "  Patient 3:     patient.cuong@dbh.vn      / Patient@123"
echo "  Patient 4:     patient.dung@dbh.vn       / Patient@123"
echo "  Patient 5:     patient.em@dbh.vn         / Patient@123"
echo "  Doctor 1:      dr.hieu@dbh.vn            / Doctor@123  (Noi khoa, Hospital A + Clinic)"
echo "  Doctor 2:      dr.lan@dbh.vn             / Doctor@123  (Tim mach, Hospital A)"
echo "  Doctor 3:      dr.phuoc@dbh.vn           / Doctor@123  (Nhi khoa, Hospital B)"
echo "  Doctor 4:      dr.tuan@dbh.vn            / Doctor@123  (Ngoai khoa, Hospital B)"
echo "  Nurse 1:       nurse.hoa@dbh.vn          / Staff@123   (Hospital A)"
echo "  Nurse 2:       nurse.khanh@dbh.vn        / Staff@123   (Hospital B)"
echo "  Receptionist:  receptionist.minh@dbh.vn  / Staff@123   (Hospital A + Clinic)"
echo "  Pharmacist:    pharmacist.oanh@dbh.vn    / Staff@123   (Hospital A)"
echo "  LabTech:       labtech.tuan@dbh.vn       / Staff@123   (Hospital B)"
echo ""
echo "  === Organizations ==="
echo "  Hospital A:  ${org_name[0]} | orgId=$hospitalAId"
echo "  Hospital B:  ${org_name[1]} | orgId=$hospitalBId"
echo "  Clinic:      ${org_name[2]} | orgId=$clinicId"
echo ""
echo "  === PayOS Note ==="
echo "  PayOS chua duoc cau hinh. De test thanh toan online:"
echo "  1. Dang ky tai khoan PayOS sandbox tai https://payos.vn"
echo "  2. Cau hinh PayOS cho Hospital A:"
echo "     POST /api/v1/organizations/$hospitalAId/payment-config"
echo "     Body: { clientId, apiKey, checksumKey }"
echo "  3. Cau hinh PayOS cho Hospital B:"
echo "     POST /api/v1/organizations/$hospitalBId/payment-config"
echo "     Body: { clientId, apiKey, checksumKey }"
echo "  4. Test checkout: POST /api/v1/invoices/{invoiceId}/checkout"
echo ""
echo "  === Unpaid Invoices (for PayOS testing) ==="
echo "  Invoice 2 (Hospital A): $inv2 | 1,140,000 VND"
echo "  Invoice 3 (Hospital B): $inv3 | 625,000 VND"
echo ""
echo "  === Cancelled Invoice ==="
echo "  Invoice 4 (Hospital B): $inv4 | 1,130,000 VND (CANCELLED)"
echo ""
echo "  === Paid Invoices ==="
echo "  Invoice 1 (Hospital A): $inv1 | 593,000 VND (CASH)"
echo "  Invoice 5 (Clinic):     $inv5 | 585,000 VND (CASH)"
echo ""