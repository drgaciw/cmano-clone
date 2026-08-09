#!/usr/bin/env bash
# Production stress-axis proof gate (DRG-63 / S110-02).
#
# Invokes the verify_axis production caller over a collected evidence JSON map.
# Fail (non-zero) when a declared non-config-only axis is unproven.
# Config-only axes (logistics / GAP-13) are reported unproven but do not hard-fail.
#
# Usage:
#   tools/qa-gauntlet/run-stress-proof-gate.sh --evidence PATH [--out PATH] [--axes PATH]
#   STRESS_PROOF_EVIDENCE=path tools/qa-gauntlet/run-stress-proof-gate.sh
#
# Evidence JSON: { "weapons": { "stressed": [...], "control": [...] }, "ew": {...}, ... }
# See tools/qa-gauntlet/README-stress-axes.md § Production proof gate.
set -uo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT"

EVIDENCE="${STRESS_PROOF_EVIDENCE:-}"
AXES=""
OUT=""
while [ $# -gt 0 ]; do
  case "$1" in
    --evidence) EVIDENCE="$2"; shift 2;;
    --axes) AXES="$2"; shift 2;;
    --out) OUT="$2"; shift 2;;
    -h|--help)
      sed -n '2,16p' "$0" | sed 's/^# \?//'
      exit 0
      ;;
    *) echo "unknown arg: $1" >&2; exit 2;;
  esac
done

if [ -z "$EVIDENCE" ]; then
  echo "FATAL: --evidence PATH or STRESS_PROOF_EVIDENCE required" >&2
  exit 2
fi

ARGS=(--evidence "$EVIDENCE")
[ -n "$AXES" ] && ARGS+=(--axes "$AXES")
[ -n "$OUT" ] && ARGS+=(--out "$OUT")

exec python3 tools/qa-gauntlet/gate_stress_proof.py "${ARGS[@]}"
