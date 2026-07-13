#!/usr/bin/env bash
set -euo pipefail

host="127.0.0.1"
port="8888"
data_dir=".hindsight-local"
detach=0
dry_run=0

usage() {
  cat <<'EOF'
Usage:
  tools/hindsight/start-hindsight-server.sh [--host 127.0.0.1] [--port 8888] [--data-dir .hindsight-local] [--detach] [--dry-run]

Starts the project-local Hindsight-compatible server. This path does not use
Docker and does not send OPENAI_API_KEY to a container. Memories are stored as
JSONL under .hindsight-local by default.
EOF
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --host)
      host="${2:-}"
      shift 2
      ;;
    --port)
      port="${2:-}"
      shift 2
      ;;
    --data-dir)
      data_dir="${2:-}"
      shift 2
      ;;
    --detach)
      detach=1
      shift
      ;;
    --dry-run)
      dry_run=1
      shift
      ;;
    --help|-h)
      usage
      exit 0
      ;;
    *)
      echo "Unknown argument: $1" >&2
      usage >&2
      exit 2
      ;;
  esac
done

if ! command -v python3 >/dev/null 2>&1; then
  echo "python3 is required to run the project-local Hindsight server." >&2
  exit 127
fi

script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd -- "$script_dir/../.." && pwd)"
server_py="$script_dir/local_server.py"

cmd=(
  python3 "$server_py"
  --host "$host"
  --port "$port"
  --data-dir "$repo_root/$data_dir"
)

if [[ "$dry_run" -eq 1 ]]; then
  printf 'Would start project-local Hindsight server:\n'
  printf '%q ' "${cmd[@]}"
  printf '\n'
  exit 0
fi

mkdir -p "$repo_root/$data_dir"

if [[ "$detach" -eq 1 ]]; then
  log_file="$repo_root/$data_dir/server.log"
  pid_file="$repo_root/$data_dir/server.pid"
  if command -v setsid >/dev/null 2>&1; then
    setsid nohup "${cmd[@]}" >"$log_file" 2>&1 < /dev/null &
  else
    nohup "${cmd[@]}" >"$log_file" 2>&1 < /dev/null &
  fi
  pid=$!
  printf '%s\n' "$pid" >"$pid_file"
  echo "Hindsight local server starting at http://$host:$port"
  echo "PID: $pid"
  echo "Log: $log_file"
  exit 0
fi

exec "${cmd[@]}"
