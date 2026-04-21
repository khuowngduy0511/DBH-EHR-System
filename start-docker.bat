@echo off
cd /d "%~dp0"
echo ======================================================================
echo          KHOI DONG HE THONG DBH-EHR (DOCKER) VA AI CHATBOT
echo ======================================================================
echo.
echo Dang khoi tao cac dich vu...
echo Vui long doi trong it phut...
set COMPOSE_PARALLEL_LIMIT=4
docker compose -f docker-compose.dev.yml up -d --build
echo.
echo ======================================================================
echo XONG! He thong Docker da chay ngam (Background).
echo ======================================================================
pause
