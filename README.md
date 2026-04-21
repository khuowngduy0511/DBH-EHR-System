# 🏥 Decentralized Blockchain Healthcare (DBH) EHR System

Chào mừng đến với hệ thống Quản lý Bệnh án Điện tử phi tập trung tiên tiến, kết hợp sức mạnh của **Microservices (C# .NET)**, **Blockchain (Hyperledger Fabric)**, và nền tảng **Trợ lý AI (LightRAG n8n)**.

Hệ thống cung cấp kiến trúc toàn diện phục vụ quản trị bệnh viện, bảo mật dữ liệu cấp cao qua IPFS & Blockchain, và nâng tầm trải nghiệm người bệnh thông qua Chatbot thông minh.

---

## 🏗️ Tổng quan Hệ sinh thái (Ecosystem)

Hệ thống được vận hành đóng gói 100% bằng Docker Compose (`docker-compose.dev.yml`), tích hợp:
- **API Gateway (YARP):** Chịu trách nhiệm định tuyến cổng giao tiếp nội bộ (`:5000`).
- **Core Microservices (.NET Core):** Auth, EHR, Organization, Consent, Notification, Audit, Appointment, Payment, và Blockchain Bridge.
- **Data Layers:** PostgreSQL đa cơ sở dữ liệu (`pg_primary`), Redis, RabbitMQ (Message Queue), IPFS (Storage).
- **AI RAG Subsystem:** Nền tảng chatbot tự động `dbh_lightrag` kết nối API OpenAI OpenRouter và CSDL nhúng Supabase.
- **Workflow Orchestration:** `dbh_n8n` điều phối logic tự động (Smart Routing).

---

## 🚀 Setup Guide (Khởi chạy Môi trường Phát triển)

### PHASE 0 — Dọn dẹp hoàn toàn
Đảm bảo bạn có một nền tảng sạch trước khi bắt đầu.
```bash
# Từ thư mục gốc project (DBH-EHR-System)

# Tắt & xóa infra containers + volumes cũ
docker compose -f docker-compose.dev.yml down -v

# Tắt & xóa mạng Blockchain Network (Fabric)
cd src/DBH.Blockchain.Network
./network.sh down
cd ../..
```

### PHASE 1 — Fix Line Endings (Cho người dùng Windows)
Mã bash script của Blockchain yêu cầu Unix EOF. Hãy chạy đoạn này sau mỗi lần `git pull`.
```bash
cd src/DBH.Blockchain.Network

find . -type f -name "*.sh" -exec sed -i 's/\r$//' {} +
chmod +x network.sh \
         organizations/ccp-generate.sh \
         scripts/*.sh \
         explorer/setup.sh \
         explorer/explorer.sh

cd ../..
```

### PHASE 2 — Khởi động Vòng lặp Infrastructure & Microservices
Khởi tạo tự động Gateway, Databases, RabbitMQ, n8n, AI Knowledge Server (LightRAG) và toàn bộ Core .NET APIs.
```bash
COMPOSE_PARALLEL_LIMIT=4 docker compose -f docker-compose.dev.yml up -d --build
```
> 💡 *Note: LightRAG (Port: 9621) và các Core API sẽ tự động liên kết với DB của chúng.*

### PHASE 3 — Khởi động Hyperledger Fabric Blockchain
Khởi tạo hệ thống sổ cái mã hóa phi tập trung để đồng bộ trạng thái sức khỏe và Payment.
```bash
cd src/DBH.Blockchain.Network
./network.sh up -s couchdb
cd ../..
```

### PHASE 4 — Sinh dữ liệu mẫu (Seed Data)
Nạp dữ liệu cơ bản (Admin, Orgs, Policies) vào PostgreSQL và mạng nội bộ.
```bash
C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe -ExecutionPolicy Bypass -File scripts\seed-data.ps1
```

---

## 🛠️ Quản trị Component Riêng lẻ
Nếu bạn chỉ phát triển hoặc fix bug một dịch vụ (ví dụ: `appointment_service`), bạn có thể chỉ force-build lại riêng nó để tiết kiệm thời gian:
```bash
docker compose -f docker-compose.dev.yml up -d --build --no-deps appointment_service
```

## 🧠 Phân hệ AI & Chatbot Workflow
Nếu bạn muốn tùy chỉnh kịch bản RAG Chatbot:
- **Tài liệu nạp (Docs):** Sửa trong thư mục kế cận `../light-rag-DBH-system/knowledge_base/docs/`. Mọi dữ liệu sẽ tự ánh xạ vào Supabase khi reindex.
- **n8n Builder:** Vào `http://localhost:5678` để thấy và chỉnh sửa `LightRAG_Chatbot_Workflow.json`.
