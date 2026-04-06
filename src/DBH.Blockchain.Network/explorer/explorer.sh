#!/usr/bin/env bash
set -euo pipefail

ACTION="${1:-start}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

cd "$SCRIPT_DIR"

run_setup() {
  echo "[explorer] Validating crypto volume and refreshing connection profile key path..."
  bash ./setup.sh
}

usage() {
  cat <<'EOF'
Usage: ./explorer.sh [action]

Actions:
  start   Validate crypto volume/profile and start explorer stack (default)
  reset   Validate crypto volume/profile, recreate stack, clear wallet/db volumes
  status  Show docker compose status
  logs    Tail explorer service logs
  down    Stop explorer stack
EOF
}

case "$ACTION" in
  start)
    run_setup
    echo "[explorer] Starting services..."
    docker compose up -d
    echo "[explorer] Explorer started (default port: 8070)."
    ;;

  reset)
    run_setup
    echo "[explorer] Recreating services and clearing wallet/db volumes..."
    docker compose down -v
    docker volume rm explorer_walletstore explorer_pgdata >/dev/null 2>&1 || true
    docker compose up -d
    echo "[explorer] Reset complete."
    ;;

  status)
    docker compose ps
    ;;

  logs)
    docker compose logs -f explorer.mynetwork.com
    ;;

  down)
    docker compose down
    ;;

  -h|--help|help)
    usage
    ;;

  *)
    echo "[explorer] Unknown action: $ACTION"
    usage
    exit 1
    ;;
esac
