#!/usr/bin/env bash
# Pin Unity-MCP (com.ivanmurzak.unity.mcp ≥0.86) to local Custom mode on :8080.
# Idempotent. Does not mint tokens. Matches Project Aegis client mcp.json convention.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
UNITY_PROJECT="${1:-$ROOT/unity/ProjectAegis}"
USER_SETTINGS="$UNITY_PROJECT/UserSettings"
CONFIG="$USER_SETTINGS/AI-Game-Developer-Config.json"
HOST="http://localhost:8080"

mkdir -p "$USER_SETTINGS"

python3 - "$CONFIG" "$HOST" <<'PY'
import json, sys
from pathlib import Path

path = Path(sys.argv[1])
host = sys.argv[2]
data = {}
if path.exists():
    try:
        data = json.loads(path.read_text(encoding="utf-8"))
        if not isinstance(data, dict):
            data = {}
    except json.JSONDecodeError:
        data = {}

# Only patch pin fields — preserve tools/prompts/resources/token if present.
data["connectionMode"] = "Custom"
data["host"] = host
data["keepServerRunning"] = True
data["keepConnected"] = True
data["authOption"] = "none"
data.setdefault("transportMethod", "streamableHttp")
data.setdefault("logLevel", "Warning")
data.setdefault("timeoutMs", 10000)

path.write_text(json.dumps(data, indent=2) + "\n", encoding="utf-8")
print(f"Pinned Unity-MCP to Custom + {host}")
print(f"Wrote {path}")
PY

echo
echo "Next:"
echo "  1. Open Unity Editor on $UNITY_PROJECT (6000.3.14f1)"
echo "  2. Confirm Window > AI Game Developer shows Custom / $HOST"
echo "  3. curl -sS -o /dev/null -w '%{http_code}\\n' --max-time 5 $HOST"
echo "  4. Restart Cursor MCP if ai-game-developer was already loaded"
