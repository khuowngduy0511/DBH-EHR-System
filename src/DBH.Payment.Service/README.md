Let me query live data from the running system: 

Run in Terminal

The terminal tool is disabled. I can pull the data directly from the seed script output that was printed during the last seed run. Here are all the IDs:

---

## Hospital A — Bệnh viện Đa khoa Trung ương

| Field | Value |
|-------|-------|
| **orgId** | `6aea5c54-2925-4011-a2e8-2f6a8d5cc0de` |

---

## Test accounts

### Admin
| Field | Value |
|-------|-------|
| Email | `admin@dbh.vn` |
| Password | `Admin@123456` |

### Doctor (Hospital A)
| Field | Value |
|-------|-------|
| Email | `dr.hieu@dbh.vn` |
| Password | `Doctor@123` |
| doctorId | `594638d7-ceea-4641-b3d5-5d8b9a4628a5` |
| Specialty | Nội khoa |

### Patient 1
| Field | Value |
|-------|-------|
| Email | `patient.an@dbh.vn` |
| Password | `Patient@123` |
| patientId | `6aca630e-7eed-47bb-ab42-824e7fe11eab` |
| Name | Nguyễn Văn An |

### Patient 2
| Field | Value |
|-------|-------|
| Email | `patient.binh@dbh.vn` |
| Password | `Patient@123` |
| patientId | `326966ba-a0c1-4137-b679-d76e8fce1fe7` |
| Name | Trần Thị Bình |

---

## Invoices đã có sẵn (UNPAID, sẵn sàng test PayOS)

| Invoice | Patient | Amount | invoiceId |
|---------|---------|--------|-----------|
| Invoice 2 | Trần Thị Bình | 1,140,000 VND | `b51d77c1-7dc6-410d-88f4-12a75a81ef93` |

---

## Luồng test trên Swagger (http://localhost:5000/swagger → chọn "Payment Service")

**Bước 1:** Authorize — dùng token từ `POST /api/v1/auth/login` (email: `dr.hieu@dbh.vn`)

**Bước 2:** Tạo invoice mới (`POST /api/v1/invoices`):
```json
{
  "patientId": "6aca630e-7eed-47bb-ab42-824e7fe11eab",
  "orgId": "6aea5c54-2925-4011-a2e8-2f6a8d5cc0de",
  "notes": "Hóa đơn test",
  "items": [
    { "description": "Phí khám", "quantity": 1, "amount": 200000 }
  ]
}
```

**Bước 3 (cash):** `POST /api/v1/invoices/{invoiceId}/pay-cash` bằng admin token:
```json
{ "transactionRef": "CASH-TEST-001" }
```

**Bước 3 (PayOS):** `POST /api/v1/invoices/{invoiceId}/checkout` — cần setup PayOS config trước cho orgId trên.

{
  "returnUrl": "http://localhost:5000/payment/success",
  "cancelUrl": "http://localhost:5000/payment/cancel"
}