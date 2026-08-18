#!/usr/bin/env bash
# QA Gauntlet ladder driver — run-id gauntlet-20260728-2016
# Usage: run-ladder.sh <tier-label> <ticks> <scenario-csv-list>
set -uo pipefail

export PATH="$HOME/.dotnet:$PATH"
export DOTNET_CLI_TELEMETRY_OPTOUT=1

ROOT="/home/username01/cmano-clone"
RUN_DIR="$ROOT/production/qa/gauntlet/gauntlet-20260728-2016"
SEEDS="42,7,123"

TIER="$1"; TICKS="$2"; SCENARIOS="$3"
TDIR="$RUN_DIR/$TIER"
mkdir -p "$TDIR"

# Stage the tier's policies so gauntlet_oracle_eval --policy-dir sees exactly this tier.
IFS=',' read -ra IDS <<< "$SCENARIOS"
for id in "${IDS[@]}"; do
  cp "$ROOT/data/scenarios/$id.policy.json" "$TDIR/" || { echo "MISSING POLICY $id"; exit 3; }
done

echo "=== $TIER ticks=$TICKS scenarios=$SCENARIOS ==="

cd "$ROOT"
dotnet run --project src/ProjectAegis.Delegation.Demo --no-build -- --batch \
  --scenarios "$SCENARIOS" --seeds "$SEEDS" --ticks "$TICKS" \
  --csv-out "$TDIR/results.csv" > "$TDIR/run.log" 2>&1
BATCH_EXIT=$?
echo "BATCH_EXIT=$BATCH_EXIT"

if [ "$BATCH_EXIT" -ne 0 ]; then
  echo "BATCH_FAIL $TIER exit=$BATCH_EXIT"
  tail -20 "$TDIR/run.log"
  exit "$BATCH_EXIT"
fi

dotnet run --project src/ProjectAegis.MissionEditor.Cli --no-build -- gauntlet_oracle_eval \
  --policy-dir "$TDIR" \
  --csv "$TDIR/results.csv" \
  --out "$TDIR/oracle-eval.json" > "$TDIR/oracle.log" 2>&1
ORACLE_EXIT=$?
echo "ORACLE_EXIT=$ORACLE_EXIT"
cat "$TDIR/oracle.log"
exit "$ORACLE_EXIT"
