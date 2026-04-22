# 🧪 Kế Hoạch Test Toàn Hệ Thống DBH-EHR

> **Mục tiêu:** Kiểm thử thủ công (Manual Test) toàn bộ 4 luồng nghiệp vụ chính theo thứ tự vai trò người dùng, bao gồm xác minh Frontend, Backend API, Blockchain (Hyperledger Fabric) và IPFS.

---

## Điều Kiện Tiên Quyết

- [ ] Docker Desktop đang chạy, tất cả container `Healthy`
- [ ] Đã seed data bằng `seed-data.ps1`
- [ ] Hyperledger Fabric Network đang hoạt động (Kiểm tra: `docker ps | grep peer`)
- [ ] Frontend đang chạy tại `http://localhost:3000`

---

## 👥 Tài Khoản Test

| Vai trò | Email | Mật khẩu |
|---|---|---|
| Admin | `admin@dbh.vn` | `Admin@123456` |
| Bệnh nhân | `patient.an@dbh.vn` | `Patient@123` |
| Bác sĩ (Nội khoa) | `dr.hieu@dbh.vn` | `Doctor@123` |
| Bác sĩ (Tim mạch) | `dr.lan@dbh.vn` | `Doctor@123` |
| Lễ tân | `receptionist.minh@dbh.vn` | `Staff@123` |
| Điều dưỡng | `nurse.hoa@dbh.vn` | `Staff@123` |
| Dược sĩ | `pharmacist.oanh@dbh.vn` | `Staff@123` |
| Kỹ thuật viên | `labtech.tuan@dbh.vn` | `Staff@123` |

---

## 🔵 LUỒNG 1: Quy Trình Khám Bệnh & Tạo EHR

**Mục tiêu:** Chuỗi từ đặt lịch → tiếp nhận → khám → tạo hồ sơ EHR được ghi lên Blockchain qua IPFS

---

### Bước 1.1 — [Patient] Đặt Lịch Khám

| # | Hành động | Kết quả mong đợi | ✅/❌ |
|---|---|---|---|
| 1 | Đăng nhập `patient.an@dbh.vn` | Dashboard Patient hiện ra | |
| 2 | Vào trang Đặt lịch, tìm BS. Trần Minh Hiếu - Nội khoa | Hiện danh sách bác sĩ | |
| 3 | Chọn ngày mai, giờ 09:00 | Lịch hẹn được tạo | |
| 4 | Xác nhận đặt lịch | Trạng thái `PENDING` trong DB | |

---

### Bước 1.2 — [Receptionist] Check-in

| # | Hành động | Kết quả mong đợi | ✅/❌ |
|---|---|---|---|
| 1 | Đăng nhập `receptionist.minh@dbh.vn` | Dashboard Lễ tân | |
| 2 | Vào "Bệnh nhân hôm nay", tìm lịch của An | Thấy lịch trạng thái PENDING | |
| 3 | Bấm **Check-in** | Trạng thái → `CHECKED_IN` | |

---

### Bước 1.3 — [Doctor] Khám & Tạo EHR → Blockchain

| # | Hành động | Kết quả mong đợi | ✅/❌ |
|---|---|---|---|
| 1 | Đăng nhập `dr.hieu@dbh.vn` | Dashboard Doctor | |
| 2 | Vào "Khám bệnh", thấy bệnh nhân An chờ khám | Hiện tên + trạng thái CHECKED_IN | |
| 3 | Nhập chẩn đoán: "Viêm họng cấp", kê thuốc | Form nhập được | |
| 4 | Bấm **Lưu hồ sơ / Hoàn thành** | Hồ sơ EHR tạo thành công, trạng thái `COMPLETED` | |
| 5 | **Xác minh Blockchain:** Kiểm tra log container `dbh_blockchain_service` | Log hiện `CreateEHR` transaction hash | |
| 6 | **Xác minh IPFS:** Kiểm tra log `dbh_ipfs` hoặc gửi API GET `/api/v1/ehr/records/{id}/files` | Tệp EHR được ghim lên IPFS, trả về IPFS CID | |

---

### Bước 1.4 — [Patient] Xem Hồ Sơ Bệnh Án

| # | Hành động | Kết quả mong đợi | ✅/❌ |
|---|---|---|---|
| 1 | Đăng nhập lại `patient.an@dbh.vn` | Dashboard Patient | |
| 2 | Vào trang Hồ sơ bệnh án | Hiện EHR vừa tạo (chẩn đoán Viêm họng cấp) | |
| 3 | Gọi API `GET /api/v1/ehr/records/patient/{patientId}` | JSON trả về đúng dữ liệu | |

---

## 🟢 LUỒNG 2: Consent (Đồng Ý Chia Sẻ) & Blockchain Xác Thực

**Mục tiêu:** Bệnh nhân cấp quyền xem hồ sơ cho bác sĩ khác, Consent được ghi lên Hyperledger Fabric và có thể thu hồi

---

### Bước 2.1 — [Patient] Cấp Quyền Consent

| # | Hành động | Kết quả mong đợi | ✅/❌ |
|---|---|---|---|
| 1 | Đăng nhập bệnh nhân An | Dashboard Patient | |
| 2 | Vào "Quản lý Quyền truy cập" / Consent Management | Danh sách Consent hiện ra | |
| 3 | Cấp quyền cho BS. Nguyễn Thị Lan (Tim mạch) xem hồ sơ | Consent được tạo trạng thái `ACTIVE` | |
| 4 | **Xác minh Blockchain:** Gọi API `GET /api/v1/blockchain/consent/patient/{patientDid}` | Consent ghi trên Fabric với `status: ACTIVE` | |

---

### Bước 2.2 — [Doctor Lan] Đọc Hồ Sơ Với Consent

| # | Hành động | Kết quả mong đợi | ✅/❌ |
|---|---|---|---|
| 1 | Đăng nhập `dr.lan@dbh.vn` | Dashboard Doctor Tim mạch | |
| 2 | Tra cứu bệnh nhân An, xem hồ sơ (có Consent) | Đọc được hồ sơ của bệnh nhân An | |
| 3 | **Xác minh Audit Log Blockchain:** Gọi `GET /api/v1/blockchain/audit/record/{recordId}` | Ghi lại hành động `READ_WITH_CONSENT` + timestamp | |

---

### Bước 2.3 — [Patient] Thu Hồi Consent → Verify Bị Chặn

| # | Hành động | Kết quả mong đợi | ✅/❌ |
|---|---|---|---|
| 1 | Đăng nhập bệnh nhân An | Dashboard Patient | |
| 2 | Thu hồi quyền của BS. Lan | Consent → `REVOKED` trên cả DB và Blockchain | |
| 3 | **Verify Blockchain:** `GET /api/v1/blockchain/consent/{consentId}` | JSON trả về `status: REVOKED` | |
| 4 | **Verify Access:** BS. Lan cố đọc lại hồ sơ An | Trả về `403 Forbidden` | |

---

## 🟡 LUỒNG 3: IPFS Upload & LabTech + Dược

**Mục tiêu:** Xét nghiệm tạo ra → file tải lên IPFS phi tập trung → CID ghi lên Blockchain

---

### Bước 3.1 — [LabTech] Upload Kết Quả Xét Nghiệm lên IPFS

| # | Hành động | Kết quả mong đợi | ✅/❌ |
|---|---|---|---|
| 1 | Đăng nhập `labtech.tuan@dbh.vn` | Dashboard Lab | |
| 2 | Vào Danh sách Xét nghiệm, tìm lệnh xét nghiệm của An | Thấy lệnh từ Luồng 1 | |
| 3 | Upload file PDF kết quả xét nghiệm | File tải lên, nhận IPFS CID | |
| 4 | **Xác minh:** Gọi `GET /api/v1/ehr/records/{ehrId}/files` | File xuất hiện cùng IPFS CID | |
| 5 | **Xác minh Blockchain:** Kiểm tra `dbh_blockchain_service` log | Log ghi `UpdateEHR` với IPFS CID mới | |
| 6 | **Xem lịch sử phiên bản:** `GET /api/v1/ehr/records/{ehrId}/versions` | Hiện 2 version: ban đầu + sau khi thêm file | |

---

### Bước 3.2 — [Pharmacist] Phát Thuốc

| # | Hành động | Kết quả mong đợi | ✅/❌ |
|---|---|---|---|
| 1 | Đăng nhập `pharmacist.oanh@dbh.vn` | Dashboard Pharmacy | |
| 2 | Tra cứu toa thuốc của bệnh nhân An | Thấy Paracetamol từ Bước 1.3 | |
| 3 | Bấm **Xác nhận phát thuốc** | Trạng thái đơn thuốc → `DISPENSED` | |

---

## 🔴 LUỒNG 4: Admin & Emergency Access Blockchain

**Mục tiêu:** Admin có quyền truy cập khẩn cấp (Emergency Access) được ghi bất biến lên Blockchain

---

### Bước 4.1 — [Admin] Quản Trị Tổng Quan

| # | Hành động | Kết quả mong đợi | ✅/❌ |
|---|---|---|---|
| 1 | Đăng nhập `admin@dbh.vn` | Dashboard Admin | |
| 2 | Vào "Quản lý Người dùng" | Danh sách tất cả tài khoản | |
| 3 | Vào "Quản lý Tổ chức / Bệnh viện" | Thấy cơ sở đang hoạt động | |
| 4 | Vào "Nhật ký Audit" | Thấy toàn bộ hành động từ Luồng 1-3 | |

---

### Bước 4.2 — [Admin] Emergency Access (Truy Cập Khẩn Cấp → Ghi Blockchain)

| # | Hành động | API | Kết quả mong đợi | ✅/❌ |
|---|---|---|---|---|
| 1 | Gọi API tạo Emergency Access | `POST /api/v1/blockchain/emergency-access` | TxHash trả về, ghi lên Fabric | |
| 2 | Kiểm tra log truy cập khẩn cấp theo record | `GET /api/v1/blockchain/emergency-access/record/{recordDid}` | Hiện lịch sử emergency access | |
| 3 | Admin xem toàn bộ emergency access | `GET /api/v1/blockchain/emergency-access` | Danh sách đầy đủ (Admin only) | |
| 4 | Verify kết nối Fabric Gateway | `GET /api/v1/blockchain/connection` | `fabricConnected: true` | |

---

### Bước 4.3 — [Admin] Tạo Blockchain Account (Fabric CA)

| # | Hành động | API | Kết quả mong đợi | ✅/❌ |
|---|---|---|---|---|
| 1 | Tạo tài khoản Fabric cho bác sĩ mới | `POST /api/v1/blockchain/accounts` | EnrollmentId + Secret trả về | |
| 2 | Đăng nhập vào mạng Blockchain | `POST /api/v1/blockchain/accounts/login` | Xác thực thành công | |

---

## 🔧 Kiểm Tra Blockchain Trực Tiếp (Nâng Cao - Qua Peer CLI)

```bash
# Kiểm tra các EHR đã được ghi lên sổ cái
peer chaincode query -C ehr-channel -n ehrcc -c '{"Args":["GetAllEHRs"]}'

# Xem lịch sử truy cập của 1 record
peer chaincode query -C ehr-channel -n ehrcc -c '{"Args":["GetAccessLogsByRecord","EHR007"]}'

# Kiểm tra Consent của bệnh nhân
peer chaincode query -C ehr-channel -n ehrcc -c '{"Args":["GetConsentsByPatient","PAT001"]}'

# Kiểm tra sức khỏe CouchDB (Hospital1)
curl http://localhost:5984/_all_dbs
```

---

## 📊 Tổng Kết Kết Quả

| Luồng | Số bước | Passed | Failed | Ghi chú |
|---|---|---|---|---|
| Luồng 1: Khám bệnh + EHR → Blockchain | 14 | | | |
| Luồng 2: Consent Blockchain | 9 | | | |
| Luồng 3: IPFS + Dược | 9 | | | |
| Luồng 4: Admin + Emergency Access | 9 | | | |
| **Tổng** | **41** | | | |

---

## ⚠️ Các Lỗi Hay Gặp & Cách Fix

| Lỗi | Nguyên nhân | Cách xử lý |
|---|---|---|
| `fabricConnected: false` | Fabric Network chưa khởi động | `cd src/DBH.Blockchain.Network && ./network.sh up -s couchdb` |
| `500 Internal Server Error` trên `/api/v1/blockchain/*` | Fabric Gateway mất kết nối | Restart `dbh_blockchain_service`: `docker compose restart blockchain_service` |
| EHR tạo xong nhưng không có TxHash | Blockchain Service không liên lạc được Peer | Kiểm tra log `dbh_blockchain_service` |
| IPFS CID không trả về | Container `dbh_ipfs` không chạy | `docker compose up -d ipfs` |
| Consent `REVOKED` nhưng Doctor vẫn vào được | Cache JWT token cũ | Đăng xuất Doctor, đăng nhập lại |
| `Profile not found` | Token cũ sau reset DB | F12 → Application → Xóa Cookies → Đăng nhập lại |
