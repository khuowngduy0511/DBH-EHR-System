# Setup Guide

## PHASE 0 — Dọn dẹp hoàn toàn

```bash
# Terminal: Từ thư mục gốc project (DBH-EHR-Backend)

# Tắt & xóa infra containers + volumes
docker compose -f docker-compose.dev.yml down -v

# Tắt & xóa blockchain network
cd src/DBH.Blockchain.Network
./network.sh down

cd ../..
```

---

## PHASE 1 — Fix line endings (Windows, cần làm sau mỗi git pull)

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

---

## PHASE 2 — Khởi động Infrastructure 

```bash
COMPOSE_PARALLEL_LIMIT=2 docker compose -f docker-compose.dev.yml up -d --build
```
---

## PHASE 3 — Khởi động Blockchain

```bash
cd src/DBH.Blockchain.Network

./network.sh up -s couchdb

cd ../..
```

---

## PHASE 4 — Seed Data

```bash
C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe -ExecutionPolicy Bypass -File scripts\seed-data.ps1
```

Chỉ build 1 cái
docker compose -f docker-compose.dev.yml up -d --build --no-deps appointment_service

