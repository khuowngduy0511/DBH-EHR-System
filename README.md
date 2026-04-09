# Setup Guide

## PHASE 0 — Dọn dẹp hoàn toàn

```bash
# Terminal 1: Từ thư mục gốc project
cd /d/DBH-EHR-Backend

# Tắt & xóa infra containers + volumes
docker compose -f docker-compose.dev.yml down -v

# Tắt & xóa blockchain network
cd src/DBH.Blockchain.Network
./network.sh down

cd /d/DBH-EHR-Backend
```

---

## PHASE 1 — Fix line endings (Windows, cần làm sau mỗi git pull)

```bash
cd /d/DBH-EHR-Backend/src/DBH.Blockchain.Network

find . -type f -name "*.sh" -exec sed -i 's/\r$//' {} +
chmod +x network.sh \
         organizations/ccp-generate.sh \
         scripts/*.sh \
         explorer/setup.sh \
         explorer/explorer.sh
```

---

## PHASE 2 — Khởi động Infrastructure

```bash
cd /d/DBH-EHR-Backend

COMPOSE_PARALLEL_LIMIT=2 docker compose -f docker-compose.dev.yml up -d --build
```

---

## PHASE 3 — Khởi động Blockchain

```bash
cd /d/DBH-EHR-Backend/src/DBH.Blockchain.Network

./network.sh up -s couchdb
```

---

## PHASE 4 — Seed Data

```bash
cd /d/DBH-EHR-Backend

bash scripts/seed-data.sh
```
