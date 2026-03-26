# DBH-EHR System - Decentralized Blockchain Healthcare

Build
docker-compose -f docker-compose.dev.yml up -d --build

Check log
docker-compose -f docker-compose.dev.yml logs -f

Build specific service
docker compose -f docker-compose.dev.yml up -d --build --no-deps auth_service

Remove docker from begin
docker compose -f docker-compose.dev.yml down -v