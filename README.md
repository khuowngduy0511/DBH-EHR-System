# DBH-EHR System - Decentralized Blockchain Healthcare
## Full lệnh chạy từ đầu + blockchain
docker-compose -f docker-compose.dev.yml up -d --build
cd src/DBH.Blockchain.Network

Xong rồi chạy wsl hoặc git bash để kích hoạt linux 

find . -type f -name "*.sh" -exec sed -i 's/\r$//' {} +
chmod +x organizations/ccp-generate.sh
chmod +x scripts/\*.sh
chmod +x network.sh
chmod +x explorer/setup.sh
./network.sh up
//////////////////////////////////////
Build
docker-compose -f docker-compose.dev.yml up -d --build

Check log
docker-compose -f docker-compose.dev.yml logs -f

Build specific service
docker compose -f docker-compose.dev.yml up -d --build --no-deps auth_service

Run unit tests
dotnet test .\src\src.sln -c Debug

Remove docker from begin
docker compose -f docker-compose.dev.yml down -v