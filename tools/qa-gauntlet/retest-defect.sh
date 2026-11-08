#!/usr/bin/env bash
# Gauntlet hindsight defect re-test loop.
# Usage:
#   tools/qa-gauntlet/retest-defect.sh <defect-id> [--registry path] [--out-dir path]
# Reads production/qa/gauntlet-defect-registry.json, re-runs the scenario×seed via
# Demo --batch, optionally re-evaluates oracle, and writes a retest log.
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
export PATH="${HOME}/.dotnet:${PATH}"
cd "$ROOT"

DEFECT_ID="${1:-}"
shift || true
REGISTRY="production/qa/gauntlet-defect-registry.json"
OUT_DIR="${TMPDIR:-/tmp}/gauntlet-retest"
while [[ $# -gt 0 ]]; do
  case "$1" in
    --registry) REGISTRY="$2"; shift 2 ;;
    --out-dir) OUT_DIR="$2"; shift 2 ;;
    *) echo "Unknown arg: $1" >&2; exit 2 ;;
  esac
done

if [[ -z "$DEFECT_ID" ]]; then
  echo "Usage: $0 <defect-id> [--registry path] [--out-dir path]" >&2
  exit 2
fi

mkdir -p "$OUT_DIR"
LOG="$OUT_DIR/retest-${DEFECT_ID}.log"
CSV="$OUT_DIR/retest-${DEFECT_ID}.csv"
ORACLE="$OUT_DIR/retest-${DEFECT_ID}-oracle.json"

python3 - "$REGISTRY" "$DEFECT_ID" "$OUT_DIR" <<'PY' > "$OUT_DIR/retest-meta.json"
import json, sys
reg_path, defect_id, out_dir = sys.argv[1:4]
reg = json.load(open(reg_path))
defects = reg.get("defects") or []
match = next((d for d in defects if d.get("id") == defect_id), None)
if not match:
    print(json.dumps({"ok": False, "error": f"defect not found: {defect_id}"}))
    sys.exit(1)
print(json.dumps({"ok": True, "defect": match}, indent=2))
PY

SCENARIO=$(python3 -c "import json;print(json.load(open('$OUT_DIR/retest-meta.json'))['defect']['scenarioId'])")
SEED=$(python3 -c "import json;print(json.load(open('$OUT_DIR/retest-meta.json'))['defect']['seed'])")
TICKS=$(python3 -c "import json;d=json.load(open('$OUT_DIR/retest-meta.json'))['defect'];print(d.get('ticks',10))")
POLICY=$(python3 -c "import json;d=json.load(open('$OUT_DIR/retest-meta.json'))['defect'];print(d.get('policyPath',''))")

{
  echo "=== Gauntlet defect re-test ==="
  echo "defect_id=$DEFECT_ID"
  echo "scenario=$SCENARIO seed=$SEED ticks=$TICKS"
  echo "registry=$REGISTRY"
  date -u +%Y-%m-%dT%H:%M:%SZ
  echo
  echo "--- batch ---"
  dotnet run --project src/ProjectAegis.Delegation.Demo -- --batch \
    --scenarios "$SCENARIO" --seeds "$SEED" --ticks "$TICKS" \
    --csv-out "$CSV"
  echo
  if [[ -n "$POLICY" && -f "$POLICY" ]]; then
    echo "--- oracle ---"
    dotnet run --project src/ProjectAegis.MissionEditor.Cli -- gauntlet_oracle_eval \
      --policy "$POLICY" --csv "$CSV" --out "$ORACLE" || true
  fi
  echo
  echo "--- failure mode check ---"
  PRIOR=$(python3 -c "import json;print(json.load(open('$OUT_DIR/retest-meta.json'))['defect'].get('priorFailureMode',''))")
  if [[ -n "$PRIOR" ]]; then
    if rg -q --fixed-strings "$PRIOR" "$CSV" 2>/dev/null; then
      echo "PRIOR_FAILURE_STILL_PRESENT: $PRIOR"
      echo "retest_status=FAIL"
      exit 1
    else
      echo "PRIOR_FAILURE_ABSENT: $PRIOR"
      echo "retest_status=PASS"
    fi
  else
    echo "No priorFailureMode string; batch completed."
    echo "retest_status=PASS"
  fi
} | tee "$LOG"

echo "Wrote $LOG"
