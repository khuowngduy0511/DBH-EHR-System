#!/usr/bin/env bash
set -u

# =============================================================================
# DBH-EHR Seed Data Script (Idempotent - safe to re-run)
# Run after: docker compose up
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

API_STATUS=""
API_BODY=""

if ! command -v curl >/dev/null 2>&1; then
  echo "curl is required but not installed."
  exit 1
fi

if ! command -v jq >/dev/null 2>&1; then
  echo "jq is required but not installed. Install with: brew install jq"
  exit 1
fi

json_get() {
  local json="$1"
  local expr="$2"
  printf '%s' "$json" | jq -r "$expr // empty" 2>/dev/null
}

iso_day_offset() {
  local days="$1"
  if date -u -v+1d +"%Y-%m-%d" >/dev/null 2>&1; then
    date -u -v"${days}"d +"%Y-%m-%d"
  else
    date -u -d "${days} day" +"%Y-%m-%d"
  fi
}

api_call() {
  local method="$1"
  local url="$2"
  local body="${3:-}"
  local token="${4:-}"
  local extra_header="${5:-}"

  local args=( -sS -X "$method" "$url" -H "Content-Type: application/json" )

  if [[ -n "$token" ]]; then
    args+=( -H "Authorization: Bearer $token" )
  fi

  if [[ -n "$extra_header" ]]; then
    args+=( -H "$extra_header" )
  fi

  if [[ -n "$body" ]]; then
    args+=( -d "$body" )
  fi

  local raw
  if ! raw=$(curl "${args[@]}" -w "\n%{http_code}" 2>/dev/null); then
    API_STATUS="000"
    API_BODY='{}'
    echo "  [000] $method $url -> request failed"
    sleep 0.15
    return 0
  fi

  API_STATUS="${raw##*$'\n'}"
  API_BODY="${raw%$'\n'*}"

  if [[ ! "$API_STATUS" =~ ^2 ]]; then
    local msg
    msg=$(json_get "$API_BODY" '.message // .title // .error // empty')
    if [[ -z "$msg" ]]; then
      msg="$API_BODY"
    fi
    echo "  [$API_STATUS] $msg"
  fi

  sleep 0.15
  return 0
}

echo
printf '====== SEED DATA - DBH EHR System ======\n'

# =============================================================================
# 1. Register Admin + Update role
# =============================================================================
printf '\n--- 1. Admin Account ---\n'

api_call "POST" "$AUTH/register" "$(jq -n \
  --arg fullName "System Admin" \
  --arg email "admin@dbh.vn" \
  --arg password "Admin@123456" \
  --arg phone "0901000001" \
  '{fullName:$fullName,email:$email,password:$password,phone:$phone}')"

api_call "POST" "$AUTH/login" "$(jq -n --arg email "admin@dbh.vn" --arg password "Admin@123456" '{email:$email,password:$password}')"
admin_token="$(json_get "$API_BODY" '.token')"

api_call "GET" "$AUTH/me" "" "$admin_token"
admin_user_id="$(json_get "$API_BODY" '.userId')"
printf '  Admin: userId=%s\n' "$admin_user_id"

api_call "PUT" "$AUTH/updateRole" "$(jq -n --arg userId "$admin_user_id" --arg newRole "Admin" '{userId:$userId,newRole:$newRole}')" "$admin_token"
printf '  Admin role assigned\n'

api_call "POST" "$AUTH/login" "$(jq -n --arg email "admin@dbh.vn" --arg password "Admin@123456" '{email:$email,password:$password}')"
admin_token="$(json_get "$API_BODY" '.token')"

# =============================================================================
# 2. Organizations (2 hospitals, 1 clinic)
# =============================================================================
printf '\n--- 2. Organizations ---\n'

orgs=(
  "Benh vien Da khoa Trung uong|BVDKTU|HOSPITAL|BV-HCM-001|0301234567|{\"line\":[\"215 Hong Bang\"],\"city\":\"Ho Chi Minh\",\"district\":\"Quan 5\",\"country\":\"VN\"}|{\"phone\":\"028-3855-4269\",\"email\":\"contact@bvdktu.vn\"}|https://bvdktu.vn"
  "Benh vien Nhi Dong 1|BVND1|HOSPITAL|BV-HCM-002|0301234568|{\"line\":[\"341 Su Van Hanh\"],\"city\":\"Ho Chi Minh\",\"district\":\"Quan 10\",\"country\":\"VN\"}|{\"phone\":\"028-3927-1119\",\"email\":\"contact@bvnd1.vn\"}|https://bvnd1.vn"
  "Phong kham Da lieu Sai Gon|PKDLSG|CLINIC|PK-HCM-001|0301234569|{\"line\":[\"123 Nguyen Hue\"],\"city\":\"Ho Chi Minh\",\"district\":\"Quan 1\",\"country\":\"VN\"}|{\"phone\":\"028-3821-0000\",\"email\":\"contact@pkdlsg.vn\"}|https://pkdlsg.vn"
)

org_ids=()
org_names=()

for o in "${orgs[@]}"; do
  IFS='|' read -r org_name org_code org_type license tax_id address contact website <<< "$o"

  body="$(jq -n \
    --arg orgName "$org_name" \
    --arg orgCode "$org_code" \
    --arg orgType "$org_type" \
    --arg licenseNumber "$license" \
    --arg taxId "$tax_id" \
    --arg address "$address" \
    --arg contactInfo "$contact" \
    --arg website "$website" \
    '{orgName:$orgName,orgCode:$orgCode,orgType:$orgType,licenseNumber:$licenseNumber,taxId:$taxId,address:$address,contactInfo:$contactInfo,website:$website}')"

  api_call "POST" "$ORG" "$body" "$admin_token"
  org_id="$(json_get "$API_BODY" '.data.orgId // .orgId')"

  org_ids+=("$org_id")
  org_names+=("$org_name")

  printf '  Org: %s | orgId=%s\n' "$org_name" "$org_id"
done

for i in "${!org_ids[@]}"; do
  if [[ -n "${org_ids[$i]}" ]]; then
    api_call "POST" "$ORG/${org_ids[$i]}/verify?verifiedByUserId=$admin_user_id" "" "$admin_token"
    printf '  Verified: %s\n' "${org_names[$i]}"
  fi
done

hospital_org_id="${org_ids[0]}"

# =============================================================================
# 3. Register Patients (5 patients)
# =============================================================================
printf '\n--- 3. Patients ---\n'

patients=(
  "Nguyen Van An|patient.an@dbh.vn|0912000001|1990-05-15|A+"
  "Tran Thi Binh|patient.binh@dbh.vn|0912000002|1985-08-22|B+"
  "Le Van Cuong|patient.cuong@dbh.vn|0912000003|1978-12-01|O+"
  "Pham Thi Dung|patient.dung@dbh.vn|0912000004|1995-03-10|AB+"
  "Hoang Van Em|patient.em@dbh.vn|0912000005|2000-07-28|O-"
)

patient_names=()
patient_emails=()
patient_user_ids=()
patient_ids=()
patient_dids=()
patient_tokens=()

for p in "${patients[@]}"; do
  IFS='|' read -r full_name email phone dob blood <<< "$p"

  api_call "POST" "$AUTH/register" "$(jq -n \
    --arg fullName "$full_name" \
    --arg email "$email" \
    --arg password "Patient@123" \
    --arg phone "$phone" \
    '{fullName:$fullName,email:$email,password:$password,phone:$phone}')"

  api_call "POST" "$AUTH/login" "$(jq -n --arg email "$email" --arg password "Patient@123" '{email:$email,password:$password}')"
  token="$(json_get "$API_BODY" '.token')"

  api_call "GET" "$AUTH/me" "" "$token"
  user_id="$(json_get "$API_BODY" '.userId')"
  patient_id="$(json_get "$API_BODY" '.profiles.Patient.patientId // .profiles.patient.patientId')"
  did="did:dbh:patient:$user_id"

  patient_names+=("$full_name")
  patient_emails+=("$email")
  patient_user_ids+=("$user_id")
  patient_ids+=("$patient_id")
  patient_dids+=("$did")
  patient_tokens+=("$token")

  printf '  Patient: %s | userId=%s | patientId=%s\n' "$full_name" "$user_id" "$patient_id"
done

# =============================================================================
# 4. Register Doctors (4 doctors, different specialties)
# =============================================================================
printf '\n--- 4. Doctors ---\n'

doctors=(
  "BS. Tran Minh Hieu|dr.hieu@dbh.vn|0913000001|Noi khoa|VN-DOC-001"
  "BS. Nguyen Thi Lan|dr.lan@dbh.vn|0913000002|Tim mach|VN-DOC-002"
  "BS. Le Van Phuoc|dr.phuoc@dbh.vn|0913000003|Nhi khoa|VN-DOC-003"
  "BS. Pham Anh Tuan|dr.tuan@dbh.vn|0913000004|Ngoai khoa|VN-DOC-004"
)

doctor_names=()
doctor_emails=()
doctor_user_ids=()
doctor_ids=()
doctor_dids=()
doctor_tokens=()
doctor_specialties=()

for d in "${doctors[@]}"; do
  IFS='|' read -r full_name email phone specialty license <<< "$d"

  api_call "POST" "$AUTH/registerStaffDoctor" "$(jq -n \
    --arg fullName "$full_name" \
    --arg email "$email" \
    --arg password "Doctor@123" \
    --arg phone "$phone" \
    --arg organizationId "$hospital_org_id" \
    --arg role "Doctor" \
    '{fullName:$fullName,email:$email,password:$password,phone:$phone,organizationId:$organizationId,role:$role}')" "$admin_token"

  api_call "POST" "$AUTH/login" "$(jq -n --arg email "$email" --arg password "Doctor@123" '{email:$email,password:$password}')"
  token="$(json_get "$API_BODY" '.token')"

  api_call "GET" "$AUTH/me" "" "$token"
  user_id="$(json_get "$API_BODY" '.userId')"
  doctor_id="$(json_get "$API_BODY" '.profiles.Doctor.doctorId // .profiles.doctor.doctorId')"
  did="did:dbh:doctor:$user_id"

  doctor_names+=("$full_name")
  doctor_emails+=("$email")
  doctor_user_ids+=("$user_id")
  doctor_ids+=("$doctor_id")
  doctor_dids+=("$did")
  doctor_tokens+=("$token")
  doctor_specialties+=("$specialty")

  printf '  Doctor: %s (%s) | userId=%s | doctorId=%s\n' "$full_name" "$specialty" "$user_id" "$doctor_id"
done

# =============================================================================
# 5. Register Staff (2 nurses, 1 receptionist, 1 pharmacist)
# =============================================================================
printf '\n--- 5. Staff ---\n'

staffs=(
  "DD. Vo Thi Hoa|nurse.hoa@dbh.vn|0914000001|Nurse"
  "DD. Bui Van Khanh|nurse.khanh@dbh.vn|0914000002|Nurse"
  "LT. Do Van Minh|receptionist.minh@dbh.vn|0914000003|Receptionist"
  "DS. Ngo Thi Oanh|pharmacist.oanh@dbh.vn|0914000004|Pharmacist"
)

staff_names=()
staff_user_ids=()
staff_tokens=()

for s in "${staffs[@]}"; do
  IFS='|' read -r full_name email phone role <<< "$s"

  api_call "POST" "$AUTH/registerStaffDoctor" "$(jq -n \
    --arg fullName "$full_name" \
    --arg email "$email" \
    --arg password "Staff@123" \
    --arg phone "$phone" \
    --arg organizationId "$hospital_org_id" \
    --arg role "$role" \
    '{fullName:$fullName,email:$email,password:$password,phone:$phone,organizationId:$organizationId,role:$role}')" "$admin_token"

  api_call "POST" "$AUTH/login" "$(jq -n --arg email "$email" --arg password "Staff@123" '{email:$email,password:$password}')"
  token="$(json_get "$API_BODY" '.token')"

  api_call "GET" "$AUTH/me" "" "$token"
  user_id="$(json_get "$API_BODY" '.userId')"

  staff_names+=("$full_name")
  staff_user_ids+=("$user_id")
  staff_tokens+=("$token")

  printf '  Staff: %s | userId=%s\n' "$full_name" "$user_id"
done

# =============================================================================
# 6. Departments (for first hospital)
# =============================================================================
printf '\n--- 6. Departments ---\n'

depts=(
  "Khoa Noi tong hop|NTH|Khoa Noi tong hop kham va dieu tri|2|201-210"
  "Khoa Tim mach|TM|Khoa Tim mach chuyen sau|3|301-308"
  "Khoa Nhi|NHI|Khoa Nhi dieu tri tre em|4|401-412"
  "Khoa Ngoai tong hop|NGH|Khoa Ngoai tong hop phau thuat|5|501-510"
  "Phong cap cuu|CC|Phong cap cuu 24/7|1|101-106"
)

dept_ids=()
dept_names=()

for d in "${depts[@]}"; do
  IFS='|' read -r department_name department_code description floor room_numbers <<< "$d"

  body="$(jq -n \
    --arg orgId "$hospital_org_id" \
    --arg departmentName "$department_name" \
    --arg departmentCode "$department_code" \
    --arg description "$description" \
    --arg floor "$floor" \
    --arg roomNumbers "$room_numbers" \
    '{orgId:$orgId,departmentName:$departmentName,departmentCode:$departmentCode,description:$description,floor:$floor,roomNumbers:$roomNumbers}')"

  api_call "POST" "$DEPT" "$body" "$admin_token"
  dept_id="$(json_get "$API_BODY" '.data.departmentId // .departmentId')"

  dept_ids+=("$dept_id")
  dept_names+=("$department_name")

  printf '  Dept: %s | deptId=%s\n' "$department_name" "$dept_id"
done

# =============================================================================
# 7. Memberships (assign doctors & staff to hospital)
# =============================================================================
printf '\n--- 7. Memberships ---\n'

for i in "${!doctor_user_ids[@]}"; do
  dept_idx="$i"
  if (( dept_idx >= ${#dept_ids[@]} )); then
    dept_idx=$((${#dept_ids[@]} - 1))
  fi

  employee_id=$(printf 'EMP-DOC-%03d' "$((i + 1))")
  license_number=$(printf 'VN-DOC-%03d' "$((i + 1))")

  body="$(jq -n \
    --arg userId "${doctor_user_ids[$i]}" \
    --arg orgId "$hospital_org_id" \
    --arg departmentId "${dept_ids[$dept_idx]}" \
    --arg employeeId "$employee_id" \
    --arg jobTitle "Bac si ${doctor_specialties[$i]}" \
    --arg licenseNumber "$license_number" \
    --arg specialty "${doctor_specialties[$i]}" \
    --arg startDate "2024-01-01" \
    '{userId:$userId,orgId:$orgId,departmentId:$departmentId,employeeId:$employeeId,jobTitle:$jobTitle,licenseNumber:$licenseNumber,specialty:$specialty,startDate:$startDate}')"

  api_call "POST" "$MEMB" "$body" "$admin_token"
  mem_id="$(json_get "$API_BODY" '.data.membershipId // .membershipId')"
  printf '  Member: %s -> %s | memId=%s\n' "${doctor_names[$i]}" "${dept_names[$dept_idx]}" "$mem_id"
done

for i in "${!staff_user_ids[@]}"; do
  employee_id=$(printf 'EMP-STF-%03d' "$((i + 1))")

  body="$(jq -n \
    --arg userId "${staff_user_ids[$i]}" \
    --arg orgId "$hospital_org_id" \
    --arg employeeId "$employee_id" \
    --arg jobTitle "Nhan vien y te" \
    --arg startDate "2024-01-01" \
    '{userId:$userId,orgId:$orgId,employeeId:$employeeId,jobTitle:$jobTitle,startDate:$startDate}')"

  api_call "POST" "$MEMB" "$body" "$admin_token"
  mem_id="$(json_get "$API_BODY" '.data.membershipId // .membershipId')"
  printf '  Member: %s -> Hospital | memId=%s\n' "${staff_names[$i]}" "$mem_id"
done

# =============================================================================
# 8. Appointments (patients book with doctors)
# =============================================================================
printf '\n--- 8. Appointments ---\n'

appointment_ids=()
appointment_pat_idxs=()
appointment_doc_idxs=()
appointment_is_past=()

apt_pairs=(
  "0|0|1|0"
  "1|1|2|0"
  "2|2|3|0"
  "3|3|1|0"
  "0|1|5|0"
  "4|0|2|0"
  "0|0|-3|1"
  "1|1|-5|1"
)

for pair in "${apt_pairs[@]}"; do
  IFS='|' read -r pat_idx doc_idx days_from_now is_past <<< "$pair"
  sleep 0.4

  if [[ "$is_past" == "1" ]]; then
    create_day="$(iso_day_offset "+1")"
  else
    create_day="$(iso_day_offset "$days_from_now")"
  fi
  create_date="${create_day}T09:00:00Z"

  patient_token="${patient_tokens[$pat_idx]}"

  body="$(jq -n \
    --arg patientId "${patient_ids[$pat_idx]}" \
    --arg doctorId "${doctor_ids[$doc_idx]}" \
    --arg orgId "$hospital_org_id" \
    --arg scheduledAt "$create_date" \
    '{patientId:$patientId,doctorId:$doctorId,orgId:$orgId,scheduledAt:$scheduledAt}')"

  api_call "POST" "$APTU" "$body" "$patient_token"
  apt_id="$(json_get "$API_BODY" '.data.appointmentId // .appointmentId')"

  appointment_ids+=("$apt_id")
  appointment_pat_idxs+=("$pat_idx")
  appointment_doc_idxs+=("$doc_idx")
  appointment_is_past+=("$is_past")

  label=""
  if [[ "$is_past" == "1" ]]; then
    label="(simulated past)"
  fi
  printf '  Appointment: %s -> %s %s | aptId=%s\n' "${patient_names[$pat_idx]}" "${doctor_names[$doc_idx]}" "$label" "$apt_id"
done

for i in "${!appointment_ids[@]}"; do
  if [[ -n "${appointment_ids[$i]}" ]]; then
    doc_token="${doctor_tokens[${appointment_doc_idxs[$i]}]}"
    api_call "PUT" "$APTU/${appointment_ids[$i]}/confirm" "" "$doc_token"
  fi
done
printf '  Confirmed all appointments\n'

# =============================================================================
# 9. Encounters + Complete (for past appointments -> create EHR)
# =============================================================================
printf '\n--- 9. Encounters ---\n'

encounter_ids=()

for i in "${!appointment_ids[@]}"; do
  if [[ "${appointment_is_past[$i]}" == "1" && -n "${appointment_ids[$i]}" ]]; then
    pat_idx="${appointment_pat_idxs[$i]}"
    doc_idx="${appointment_doc_idxs[$i]}"

    doc_token="${doctor_tokens[$doc_idx]}"
    patient_id="${patient_ids[$pat_idx]}"
    doctor_id="${doctor_ids[$doc_idx]}"
    patient_name="${patient_names[$pat_idx]}"

    api_call "PUT" "$APTU/${appointment_ids[$i]}/check-in" "" "$doc_token"
    printf '  Checked in: %s\n' "$patient_name"

    body="$(jq -n \
      --arg patientId "$patient_id" \
      --arg doctorId "$doctor_id" \
      --arg appointmentId "${appointment_ids[$i]}" \
      --arg orgId "$hospital_org_id" \
      --arg notes "Kham benh cho $patient_name" \
      '{patientId:$patientId,doctorId:$doctorId,appointmentId:$appointmentId,orgId:$orgId,notes:$notes}')"

    api_call "POST" "$ENCU" "$body" "$doc_token"
    enc_id="$(json_get "$API_BODY" '.data.encounterId // .encounterId')"
    encounter_ids+=("$enc_id")
    printf '  Encounter: %s | encId=%s\n' "$patient_name" "$enc_id"

    if [[ -n "$enc_id" ]]; then
      complete_body="$(jq -n '
        {
          notes: "Hoan tat kham. Chan doan: Viem hong cap. Phac do: Amoxicillin 500mg x 7 ngay",
          ehrData: {
            resourceType: "Bundle",
            type: "document",
            entry: [
              {
                resource: {
                  resourceType: "Condition",
                  code: { text: "Viem hong cap" },
                  clinicalStatus: { coding: [ { code: "active" } ] }
                }
              },
              {
                resource: {
                  resourceType: "MedicationRequest",
                  medicationCodeableConcept: { text: "Amoxicillin 500mg" },
                  dosageInstruction: [ { text: "Uong 3 lan/ngay sau an" } ]
                }
              },
              {
                resource: {
                  resourceType: "Observation",
                  code: { text: "Vitals" },
                  component: [
                    { code: { text: "Nhiet do" }, valueQuantity: { value: 37.5, unit: "°C" } },
                    { code: { text: "Huyet ap" }, valueQuantity: { value: 120, unit: "mmHg" } },
                    { code: { text: "Nhip tim" }, valueQuantity: { value: 80, unit: "bpm" } }
                  ]
                }
              }
            ]
          }
        }')"

      api_call "PUT" "$ENCU/$enc_id/complete" "$complete_body" "$doc_token"
      printf '  Completed encounter with EHR\n'
    fi
  fi
done

# =============================================================================
# 10. Create additional standalone EHR records
# =============================================================================
printf '\n--- 10. EHR Records ---\n'

ehr_ids=()

for i in 0 1 2; do
  doc_token="${doctor_tokens[0]}"
  patient_id="${patient_ids[$i]}"
  doctor_id="${doctor_ids[0]}"

  ehr_body="$(jq -n \
    --arg patientId "$patient_id" \
    --arg orgId "$hospital_org_id" \
    --argjson bmi "$((22 + i))" \
    '{
      patientId: $patientId,
      orgId: $orgId,
      data: {
        resourceType: "Bundle",
        type: "document",
        entry: [
          {
            resource: {
              resourceType: "Condition",
              code: { text: "Kham suc khoe tong quat" },
              clinicalStatus: { coding: [ { code: "resolved" } ] }
            }
          },
          {
            resource: {
              resourceType: "Observation",
              code: { text: "BMI" },
              valueQuantity: { value: $bmi, unit: "kg/m2" }
            }
          }
        ]
      }
    }')"

  api_call "POST" "$EHRU" "$ehr_body" "$doc_token" "X-Doctor-Id: $doctor_id"
  ehr_id="$(json_get "$API_BODY" '.data.ehrId // .ehrId')"
  if [[ -n "$ehr_id" ]]; then
    ehr_ids+=("$ehr_id")
  fi

  printf '  EHR: %s | ehrId=%s\n' "${patient_names[$i]}" "$ehr_id"
done

# =============================================================================
# 11. Consents (patients grant access to doctors)
# =============================================================================
printf '\n--- 11. Consents ---\n'

for i in 0 1 2; do
  pat_token="${patient_tokens[$i]}"

  body="$(jq -n \
    --arg patientId "${patient_ids[$i]}" \
    --arg patientDid "${patient_dids[$i]}" \
    --arg granteeId "${doctor_ids[0]}" \
    --arg granteeDid "${doctor_dids[0]}" \
    --arg granteeType "DOCTOR" \
    --arg permission "READ" \
    --arg purpose "TREATMENT" \
    --argjson durationDays 90 \
    '{patientId:$patientId,patientDid:$patientDid,granteeId:$granteeId,granteeDid:$granteeDid,granteeType:$granteeType,permission:$permission,purpose:$purpose,durationDays:$durationDays}')"

  api_call "POST" "$CONSU" "$body" "$pat_token"
  consent_id="$(json_get "$API_BODY" '.data.consentId // .consentId')"
  printf '  Consent: %s -> %s | consentId=%s\n' "${patient_names[$i]}" "${doctor_names[0]}" "$consent_id"
done

pat_token="${patient_tokens[0]}"
body="$(jq -n \
  --arg patientId "${patient_ids[0]}" \
  --arg patientDid "${patient_dids[0]}" \
  --arg granteeId "$hospital_org_id" \
  --arg granteeDid "did:dbh:org:$hospital_org_id" \
  --arg granteeType "ORGANIZATION" \
  --arg permission "READ" \
  --arg purpose "TREATMENT" \
  --argjson durationDays 365 \
  '{patientId:$patientId,patientDid:$patientDid,granteeId:$granteeId,granteeDid:$granteeDid,granteeType:$granteeType,permission:$permission,purpose:$purpose,durationDays:$durationDays}')"
api_call "POST" "$CONSU" "$body" "$pat_token"
printf '  Consent: %s -> Hospital (org)\n' "${patient_names[0]}"

# =============================================================================
# 12. Audit Logs
# =============================================================================
printf '\n--- 12. Audit Logs ---\n'

audit_logs=(
  "${patient_dids[0]}|${patient_user_ids[0]}|PATIENT|LOGIN|USER|SUCCESS|||"
  "${doctor_dids[0]}|${doctor_user_ids[0]}|DOCTOR|VIEW|EHR|SUCCESS|${patient_dids[0]}|${patient_ids[0]}|"
  "${doctor_dids[0]}|${doctor_user_ids[0]}|DOCTOR|CREATE|EHR|SUCCESS|${patient_dids[0]}|${patient_ids[0]}|"
  "${patient_dids[0]}|${patient_user_ids[0]}|PATIENT|GRANT_CONSENT|CONSENT|SUCCESS|||"
  "did:dbh:system||SYSTEM|VIEW|SYSTEM|SUCCESS|||{\"event\":\"health_check\"}"
)

for log in "${audit_logs[@]}"; do
  IFS='|' read -r actor_did actor_user_id actor_type action target_type result patient_did patient_id metadata <<< "$log"

  body="$(jq -n \
    --arg actorDid "$actor_did" \
    --arg actorUserId "$actor_user_id" \
    --arg actorType "$actor_type" \
    --arg action "$action" \
    --arg targetType "$target_type" \
    --arg result "$result" \
    --arg patientDid "$patient_did" \
    --arg patientId "$patient_id" \
    --arg metadata "$metadata" \
    '{
      actorDid: $actorDid,
      actorUserId: (if $actorUserId == "" then null else $actorUserId end),
      actorType: $actorType,
      action: $action,
      targetType: $targetType,
      result: $result,
      patientDid: (if $patientDid == "" then null else $patientDid end),
      patientId: (if $patientId == "" then null else $patientId end),
      metadata: (if $metadata == "" then null else $metadata end)
    }')"

  api_call "POST" "$AUDIT" "$body" "$admin_token"
done
printf '  Created %s audit logs\n' "${#audit_logs[@]}"

# =============================================================================
# 13. Notifications
# =============================================================================
printf '\n--- 13. Notifications ---\n'

notifications=(
  "${patient_dids[0]}|${patient_user_ids[0]}|Lich hen sap toi|Ban co lich kham voi BS. Tran Minh Hieu vao ngay mai luc 9:00.|AppointmentReminder|Normal|InApp"
  "${patient_dids[1]}|${patient_user_ids[1]}|Ho so benh an da cap nhat|BS. Nguyen Thi Lan da cap nhat ho so benh an cua ban.|EhrUpdate|Normal|InApp"
  "${doctor_dids[0]}|${doctor_user_ids[0]}|Yeu cau truy cap ho so|Benh nhan Nguyen Van An da cap quyen truy cap ho so benh an.|ConsentGranted|High|InApp"
  "${patient_dids[2]}|${patient_user_ids[2]}|Nhac nho uong thuoc|Hay uong thuoc Amoxicillin 500mg sau bua trua.|System|Normal|InApp"
  "${patient_dids[0]}|${patient_user_ids[0]}|Canh bao bao mat|Tai khoan cua ban vua duoc dang nhap tu thiet bi moi.|SecurityAlert|High|InApp"
)

for n in "${notifications[@]}"; do
  IFS='|' read -r recipient_did recipient_user_id title body type priority channel <<< "$n"

  payload="$(jq -n \
    --arg recipientDid "$recipient_did" \
    --arg recipientUserId "$recipient_user_id" \
    --arg title "$title" \
    --arg body "$body" \
    --arg type "$type" \
    --arg priority "$priority" \
    --arg channel "$channel" \
    '{recipientDid:$recipientDid,recipientUserId:$recipientUserId,title:$title,body:$body,type:$type,priority:$priority,channel:$channel}')"

  api_call "POST" "$NOTIF" "$payload" "$admin_token"
done
printf '  Created %s notifications\n' "${#notifications[@]}"

# =============================================================================
# Summary
# =============================================================================
printf '\n====== SEED DATA COMPLETE ======\n'
printf '  Admin:         1 (admin@dbh.vn / Admin@123456)\n'
printf '  Patients:      %s (patient.an@dbh.vn ... / Patient@123)\n' "${#patient_ids[@]}"
printf '  Doctors:       %s (dr.hieu@dbh.vn ... / Doctor@123)\n' "${#doctor_ids[@]}"
printf '  Staff:         %s (nurse.hoa@dbh.vn ... / Staff@123)\n' "${#staff_user_ids[@]}"
printf '  Organizations: %s\n' "${#org_ids[@]}"
printf '  Departments:   %s\n' "${#dept_ids[@]}"
printf '  Appointments:  %s\n' "${#appointment_ids[@]}"
printf '  Encounters:    %s\n' "${#encounter_ids[@]}"
printf '  EHR Records:   %s (standalone)\n' "${#ehr_ids[@]}"
printf '  Consents:      4\n'
printf '  Audit Logs:    %s\n' "${#audit_logs[@]}"
printf '  Notifications: %s\n' "${#notifications[@]}"
printf '\n'
printf '  === Test Accounts ===\n'
printf '  Admin:    admin@dbh.vn      / Admin@123456\n'
printf '  Patient1: patient.an@dbh.vn / Patient@123\n'
printf '  Patient2: patient.binh@dbh.vn / Patient@123\n'
printf '  Patient3: patient.cuong@dbh.vn / Patient@123\n'
printf '  Patient4: patient.dung@dbh.vn / Patient@123\n'
printf '  Patient5: patient.em@dbh.vn / Patient@123\n'
printf '  Doctor1:  dr.hieu@dbh.vn    / Doctor@123\n'
printf '  Doctor2:  dr.lan@dbh.vn     / Doctor@123\n'
printf '  Doctor3:  dr.phuoc@dbh.vn   / Doctor@123\n'
printf '  Doctor4:  dr.tuan@dbh.vn    / Doctor@123\n'
printf '  Staff1:   nurse.hoa@dbh.vn  / Staff@123\n'
printf '  Staff2:   nurse.khanh@dbh.vn / Staff@123\n'
printf '  Staff3:   receptionist.minh@dbh.vn / Staff@123\n'
printf '  Staff4:   pharmacist.oanh@dbh.vn / Staff@123\n'
printf '\n'
