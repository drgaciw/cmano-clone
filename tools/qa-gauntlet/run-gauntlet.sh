#!/usr/bin/env bash
# Canonical QA Gauntlet ladder driver — oracles as code.
# Spec: docs/superpowers/specs/2026-07-28-qa-gauntlet-effectiveness-design.md
# Usage: run-gauntlet.sh --run-id <id> [--tiers "1 2 3 4 5 extra"] [--seeds 42,7,123]
#                        [--roving 2] [--out-root production/qa/gauntlet]
set -uo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT"

# dotnet resolution: PATH, then ~/.dotnet, then fail loud.
if command -v dotnet >/dev/null 2>&1; then DOTNET=dotnet;
elif [ -x "$HOME/.dotnet/dotnet" ]; then DOTNET="$HOME/.dotnet/dotnet";
else echo "FATAL: dotnet not found on PATH or at ~/.dotnet/dotnet" >&2; exit 3; fi
export DOTNET_CLI_TELEMETRY_OPTOUT=1

RUN_ID=""; TIERS="1 2 3 4 5 extra"; ANCHOR_SEEDS="42,7,123"; ROVING=2
OUT_ROOT="production/qa/gauntlet"
while [ $# -gt 0 ]; do case "$1" in
  --run-id) RUN_ID="$2"; shift 2;;
  --tiers) TIERS="$2"; shift 2;;
  --seeds) ANCHOR_SEEDS="$2"; shift 2;;
  --roving) ROVING="$2"; shift 2;;
  --out-root) OUT_ROOT="$2"; shift 2;;
  *) echo "unknown arg: $1" >&2; exit 3;;
esac; done
[ -n "$RUN_ID" ] || { echo "FATAL: --run-id required" >&2; exit 3; }

RUN_DIR="$OUT_ROOT/$RUN_ID"; mkdir -p "$RUN_DIR"
GOLDENS="tools/qa-gauntlet/goldens/anchors.json"
EXPECTED="tools/qa-gauntlet/expected-tokens.json"

# Deterministic roving seeds from run-id (recorded for reproducibility).
ROVING_SEEDS=""
if [ "$ROVING" -gt 0 ]; then
  ROVING_SEEDS=$(python3 - "$RUN_ID" "$ROVING" <<'PY'
import hashlib, sys
run_id, n = sys.argv[1], int(sys.argv[2])
print(",".join(str(int(hashlib.sha256(f"{run_id}:{k}".encode()).hexdigest()[:8], 16) % 90000 + 10000)
               for k in range(n)))
PY
)
  echo "$ROVING_SEEDS" > "$RUN_DIR/roving-seeds.txt"
fi
ALL_SEEDS="$ANCHOR_SEEDS${ROVING_SEEDS:+,$ROVING_SEEDS}"

EVAL_PY="tools/qa-gauntlet/evaluate_run.py"
LADDER_YAML="tools/qa-gauntlet/ladder.yaml"

OVERALL=0
TIER_NAMES=""
for t in $TIERS; do
  TIER="tier-$t"; TDIR="$RUN_DIR/$TIER"; mkdir -p "$TDIR"
  TIER_NAMES="${TIER_NAMES:+$TIER_NAMES,}$TIER"
  SCEN=$(python3 "$EVAL_PY" ladder --ladder "$LADDER_YAML" --tier "$t" --field scenarios) \
    || { echo "FATAL: unknown tier $t" >&2; exit 3; }
  TICKS=$(python3 "$EVAL_PY" ladder --ladder "$LADDER_YAML" --tier "$t" --field ticks) \
    || { echo "FATAL: unknown tier $t" >&2; exit 3; }
  echo "=== $TIER ticks=$TICKS seeds=$ALL_SEEDS ==="
  IFS=',' read -ra IDS <<< "$SCEN"
  for id in "${IDS[@]}"; do cp "data/scenarios/$id.policy.json" "$TDIR/"; done

  "$DOTNET" run --project src/ProjectAegis.Delegation.Demo --no-build -- --batch \
    --scenarios "$SCEN" --seeds "$ALL_SEEDS" --ticks "$TICKS" \
    --csv-out "$TDIR/results.csv" > "$TDIR/run.log" 2>&1 \
    || { echo "BATCH_FAIL $TIER"; OVERALL=1; continue; }
  "$DOTNET" run --project src/ProjectAegis.Delegation.Demo --no-build -- --batch \
    --scenarios "$SCEN" --seeds "$ALL_SEEDS" --ticks "$TICKS" \
    --csv-out "$TDIR/results-repeat.csv" > "$TDIR/run-repeat.log" 2>&1 \
    || { echo "REPEAT_FAIL $TIER"; OVERALL=1; continue; }

  # Strict gate: anchor rows only. Envelope bounds are calibrated on anchor seeds
  # (tools/qa-gauntlet/README-expect-regen.md), so roving rows are evaluated
  # separately below and surfaced as warnings (roving_observe), never as the gate.
  python3 "$EVAL_PY" filter-seeds \
    --in "$TDIR/results.csv" --out "$TDIR/results-anchors.csv" \
    --seeds "$ANCHOR_SEEDS"
  "$DOTNET" run --project src/ProjectAegis.MissionEditor.Cli --no-build -- gauntlet_oracle_eval \
    --policy-dir "$TDIR" --csv "$TDIR/results-anchors.csv" \
    --out "$TDIR/oracle-eval.json" > "$TDIR/oracle.log" 2>&1
  # exit code intentionally not consumed here — evaluate_run.py reads oracle-eval.json (fail-closed)
  if [ -n "$ROVING_SEEDS" ]; then
    "$DOTNET" run --project src/ProjectAegis.MissionEditor.Cli --no-build -- gauntlet_oracle_eval \
      --policy-dir "$TDIR" --csv "$TDIR/results.csv" \
      --out "$TDIR/oracle-eval-roving.json" > "$TDIR/oracle-roving.log" 2>&1 || true
  fi

  python3 tools/qa-gauntlet/evaluate_run.py tier \
    --tier-dir "$TDIR" --scenarios "$SCEN" \
    --anchor-seeds "$ANCHOR_SEEDS" --roving-seeds "$ROVING_SEEDS" \
    --goldens "$GOLDENS" || OVERALL=1
done

python3 tools/qa-gauntlet/evaluate_run.py run \
  --run-dir "$RUN_DIR" --tiers "$TIER_NAMES" \
  --expected-tokens "$EXPECTED" --anchor-seeds "$ANCHOR_SEEDS" \
  --out "$RUN_DIR/verdict.json" || OVERALL=1

echo "RUN_VERDICT exit=$OVERALL run_dir=$RUN_DIR"
exit "$OVERALL"
