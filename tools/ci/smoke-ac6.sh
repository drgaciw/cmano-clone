#!/usr/bin/env bash
# AC-6 smoke test — scenario document format is byte-stable and git-diff-friendly.
#
# QA plan: production/qa/qa-plan-scenario-editor-2026-07-01.md, unit #6 (AC-6).
# Requirement: Game-Requirements/requirements/11-Agentic-Mission-Editor.md (AC-6).
#
# What this actually tests, and why (read before "fixing" a failure):
#
#   The CLI has no "no-op save" verb. `scenario_validate` and `scenario_publish`
#   only *read* the scenario file (they emit a report/manifest, they never
#   rewrite scenario.json). Every mutating verb (`mission_add_patrol`,
#   `mission_update_patrol`, ...) calls ScenarioDocumentEditor.CommitMutation(),
#   which unconditionally bumps metadata.editVersion. So there is no way to
#   "load an unedited scenario and save it twice" through the CLI surface and
#   get a no-op diff — a version bump is baked into every write path.
#
#   Given that, this script approximates AC-6 honestly:
#
#   (a) Byte-stability: run `scenario_create` + `mission_add_patrol` TWICE, in
#       two independent OS processes, with byte-for-byte identical arguments,
#       and assert the two resulting files are SHA-256 identical. This proves
#       ScenarioDocumentJsonWriter's serialization (property order, casing,
#       number formatting, whitespace) is deterministic across process
#       boundaries — i.e. no timestamps, GUIDs, hash salts, or unordered
#       collection enumeration have crept into scenario.json. That is the
#       property AC-6a is actually there to protect, even though it is not
#       literally "the same document saved twice".
#
#   (b) Minimal-diff: because every mutation both changes the target field
#       AND bumps editVersion, a single-field content change necessarily
#       touches TWO locations in the file: the metadata.editVersion line, and
#       the changed field itself. So this script asserts the diff is exactly
#       that — at most 2 hunks, at most 2 removed lines, no key reordering —
#       rather than literally "1 hunk". Two focused, non-overlapping hunks
#       with no full-file rewrite and no reordering is what "git-diff-friendly"
#       means in practice for a reviewer; that is what we assert and report.
#
# Exit code: 0 = pass, non-zero = fail (message printed to stderr).

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
CLI_PROJ="$REPO_ROOT/src/ProjectAegis.MissionEditor.Cli/ProjectAegis.MissionEditor.Cli.csproj"

# Make sure `dotnet` is on PATH even in shells that don't source it (matches
# this repo's observed local install layout; harmless if already on PATH).
if ! command -v dotnet >/dev/null 2>&1 && [[ -x "$HOME/.dotnet/dotnet" ]]; then
  export PATH="$HOME/.dotnet:$PATH"
fi

if ! command -v dotnet >/dev/null 2>&1; then
  echo "FAIL: dotnet SDK not found on PATH (checked \$HOME/.dotnet)" >&2
  exit 1
fi

WORK_DIR="$(mktemp -d "${TMPDIR:-/tmp}/ac6-smoke.XXXXXX")"
cleanup() {
  cd "$REPO_ROOT"
  rm -rf "$WORK_DIR"
}
trap cleanup EXIT

# find returns non-zero when bin/Release is missing; with pipefail that would exit
# before the Release build below. Swallow pipeline status so empty → build path runs.
CLI_DLL="$(find "$REPO_ROOT/src/ProjectAegis.MissionEditor.Cli/bin/Release" -name "ProjectAegis.MissionEditor.Cli.dll" 2>/dev/null | sort | tail -1 || true)"
if [[ -z "$CLI_DLL" ]]; then
  echo "== AC-6 smoke: building CLI dependency chain (Release) =="
  dotnet build "$CLI_PROJ" -c Release -v minimal -m:1
  CLI_DLL="$(find "$REPO_ROOT/src/ProjectAegis.MissionEditor.Cli/bin/Release" -name "ProjectAegis.MissionEditor.Cli.dll" 2>/dev/null | sort | tail -1 || true)"
fi
if [[ -z "$CLI_DLL" ]]; then
  echo "FAIL: could not locate built ProjectAegis.MissionEditor.Cli.dll under bin/Release" >&2
  exit 1
fi
echo "== AC-6 smoke: using CLI (Release) =="
echo "Using CLI: $CLI_DLL"

run_cli() {
  dotnet "$CLI_DLL" "$@"
}

cd "$WORK_DIR"
FAIL=0

# ---------------------------------------------------------------------------
# (a) Byte-stability
# ---------------------------------------------------------------------------
echo
echo "== (a) byte-stability: two independent create+patrol runs, identical args =="

for n in 1 2; do
  run_cli scenario_create --out "scenario_$n.json" \
    --db-ref baltic_patrol --policy-id baltic-patrol-catalog --seed 42 >/dev/null
  run_cli mission_add_patrol --path "scenario_$n.json" --edit-version 1 --id patrol-1 \
    --unit unit-1 --unit unit-2 \
    --wp 55.0,15.0 --wp 55.1,15.1 --wp 55.2,15.0 >/dev/null
done

HASH1="$(sha256sum scenario_1.json | awk '{print $1}')"
HASH2="$(sha256sum scenario_2.json | awk '{print $1}')"

if [[ "$HASH1" == "$HASH2" ]]; then
  echo "PASS: scenario_1.json and scenario_2.json are byte-identical (sha256 $HASH1)"
else
  echo "FAIL: two runs with identical CLI args produced different bytes" >&2
  diff -u scenario_1.json scenario_2.json >&2 || true
  FAIL=1
fi

# ---------------------------------------------------------------------------
# (b) Minimal-diff: one waypoint lat changed
# ---------------------------------------------------------------------------
echo
echo "== (b) minimal-diff: one waypoint lat changed =="

cp scenario_1.json baseline.json

run_cli mission_update_patrol --path scenario_1.json --edit-version 2 --id patrol-1 \
  --unit unit-1 --unit unit-2 \
  --wp 55.5,15.0 --wp 55.1,15.1 --wp 55.2,15.0 >/dev/null

DIFF_OUT="$(diff -u baseline.json scenario_1.json || true)"
echo "$DIFF_OUT"
HUNKS="$(printf '%s\n' "$DIFF_OUT" | grep -c '^@@' || true)"
REMOVED="$(printf '%s\n' "$DIFF_OUT" | grep -c '^-[^-]' || true)"
echo "hunks=$HUNKS removed_lines=$REMOVED"

if [[ "$HUNKS" -ge 1 && "$HUNKS" -le 2 && "$REMOVED" -le 2 ]]; then
  echo "PASS: single-waypoint-lat change produced $HUNKS hunk(s) / $REMOVED removed line(s)" \
       "(editVersion bump + the one changed field) — not a full-file rewrite"
else
  echo "FAIL: expected <=2 hunks / <=2 removed lines for a single-field change," \
       "got hunks=$HUNKS removed=$REMOVED" >&2
  FAIL=1
fi

KEYS_BEFORE="$(grep -oE '"[a-zA-Z]+":' baseline.json)"
KEYS_AFTER="$(grep -oE '"[a-zA-Z]+":' scenario_1.json)"
if [[ "$KEYS_BEFORE" == "$KEYS_AFTER" ]]; then
  echo "PASS: JSON key order unchanged after mutation (no reordering)"
else
  echo "FAIL: JSON key order changed after mutation" >&2
  FAIL=1
fi

# ---------------------------------------------------------------------------
# (b) edge case: adding a new mission is a clean insertion
# ---------------------------------------------------------------------------
echo
echo "== (b) edge case: adding a new mission is a clean insertion =="

cp scenario_1.json before_add.json

run_cli mission_add_patrol --path scenario_1.json --edit-version 3 --id patrol-2 \
  --unit unit-3 --wp 56.0,16.0 --wp 56.1,16.1 --wp 56.2,16.0 >/dev/null

ADD_DIFF="$(diff -u before_add.json scenario_1.json || true)"
echo "$ADD_DIFF"
ADD_HUNKS="$(printf '%s\n' "$ADD_DIFF" | grep -c '^@@' || true)"
ADD_REMOVED="$(printf '%s\n' "$ADD_DIFF" | grep -c '^-[^-]' || true)"
echo "add_hunks=$ADD_HUNKS removed_lines=$ADD_REMOVED"

# Expect: editVersion bump (1 removed/added pair) + the old mission-array
# closing "]" becoming "}," followed by the new mission object as pure
# insertion (1 more removed/added pair). No other existing content may be
# touched.
if [[ "$ADD_HUNKS" -ge 1 && "$ADD_HUNKS" -le 2 && "$ADD_REMOVED" -le 2 ]]; then
  echo "PASS: adding a mission is a clean insertion ($ADD_HUNKS hunk(s), $ADD_REMOVED removed line(s))"
else
  echo "FAIL: adding a mission touched more than expected (hunks=$ADD_HUNKS removed=$ADD_REMOVED)" >&2
  FAIL=1
fi

echo
if [[ "$FAIL" -ne 0 ]]; then
  echo "AC-6 SMOKE: FAIL" >&2
  exit 1
fi

echo "AC-6 SMOKE: PASS"
exit 0
