#!/usr/bin/env bash
set -euo pipefail

base_url="${1:-http://localhost:8888}"
base_url="${base_url%/}"

if curl -fsS --max-time 5 "$base_url/v1/default/banks" >/dev/null; then
  echo "Hindsight OK: $base_url"
else
  status=$?
  echo "Hindsight unreachable at $base_url - run tools/hindsight/start-hindsight-server.sh --detach or check firewall." >&2
  exit "$status"
fi
