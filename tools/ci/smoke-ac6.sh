#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
TMP="$(mktemp -d)"
trap 'rm -rf "$TMP"' EXIT

SCENARIO="$TMP/scenario.json"
dotnet run --project "$ROOT/src/ProjectAegis.MissionEditor.Cli" --no-build -- scenario_create --out "$SCENARIO" >/dev/null

FIRST_HASH="$(sha256sum "$SCENARIO" | awk '{print $1}')"
dotnet run --project "$ROOT/src/ProjectAegis.MissionEditor.Cli" --no-build -- scenario_save --path "$SCENARIO" --edit-version 1 >/dev/null
SECOND_HASH="$(sha256sum "$SCENARIO" | awk '{print $1}')"

if [[ "$FIRST_HASH" != "$SECOND_HASH" ]]; then
  echo "AC-6 FAIL: double-save hash mismatch"
  exit 1
fi

echo "AC-6 PASS: byte-identical double-save ($FIRST_HASH)"
