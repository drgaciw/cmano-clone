#!/usr/bin/env bash
# Oracle-2 determinism: re-run each tier into a repeat CSV and diff against the first run.
# Identical (scenario, seed) MUST produce an identical fingerprint.
set -uo pipefail
export PATH="$HOME/.dotnet:$PATH"
export DOTNET_CLI_TELEMETRY_OPTOUT=1

ROOT="/home/username01/cmano-clone"
RUN_DIR="$ROOT/production/qa/gauntlet/gauntlet-20260728-2000"
SEEDS="42,7,123"
cd "$ROOT"

run_tier() {
  local TIER="$1" TICKS="$2" SCEN="$3"
  local TDIR="$RUN_DIR/$TIER"
  dotnet run --project src/ProjectAegis.Delegation.Demo --no-build -- --batch \
    --scenarios "$SCEN" --seeds "$SEEDS" --ticks "$TICKS" \
    --csv-out "$TDIR/results-repeat.csv" > "$TDIR/run-repeat.log" 2>&1
  local E=$?
  if [ $E -ne 0 ]; then echo "REPEAT_BATCH_FAIL $TIER exit=$E"; return $E; fi
  # Sort both so row ordering differences don't masquerade as divergence.
  sort "$TDIR/results.csv" > "$TDIR/.a.csv"
  sort "$TDIR/results-repeat.csv" > "$TDIR/.b.csv"
  if diff -q "$TDIR/.a.csv" "$TDIR/.b.csv" > /dev/null; then
    echo "DETERMINISM $TIER PASS"
  else
    echo "DETERMINISM $TIER FAIL"
    diff "$TDIR/.a.csv" "$TDIR/.b.csv" | head -20 > "$TDIR/determinism-diff.txt"
    cat "$TDIR/determinism-diff.txt"
  fi
  rm -f "$TDIR/.a.csv" "$TDIR/.b.csv"
}

run_tier tier-1 6  "gauntlet-t1-patrol-a,gauntlet-t1-patrol-b,gauntlet-t1-patrol-c,gauntlet-t1-patrol-d"
run_tier tier-2 10 "gauntlet-t2-escort-a,gauntlet-t2-escort-passive,gauntlet-t2-strike-a,gauntlet-t2-strike-event"
run_tier tier-3 16 "gauntlet-t3-escort-strike,gauntlet-t3-emcon-phases,gauntlet-t3-id-roe,gauntlet-t3-event-chain"
run_tier tier-4 24 "gauntlet-t4-multi-mission,gauntlet-t4-weighted,gauntlet-t4-asymm-roe,gauntlet-t4-random-inject"
run_tier tier-5 40 "gauntlet-t5-cascade,gauntlet-t5-theater,gauntlet-t5-dynamic-obj,gauntlet-t5-roe-change"
run_tier tier-extra 12 "gauntlet-joint-orbat-smoke,gauntlet-multidomain-shooters"
