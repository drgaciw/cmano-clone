#!/usr/bin/env bash
# Phase 0 verification for scenario-editor branch integration (S81-02 / S88 post-ack).
#
# Authority: production/agentic/scenario-editor-branch-integration-plan-2026-07-04.md
#            docs/reports/roadmap-execute-plan-07042026.md §5 Phase 0
#            production/scenario-editor-scope-boundary-2026-07-04.md
#
# Usage:
#   bash tools/ci/smoke-scenario-editor-phase0.sh          # full (includes solution test)
#   bash tools/ci/smoke-scenario-editor-phase0.sh --quick   # build + subset gates (xUnit wrapper)
#
# Exit 0 = all checks PASS; non-zero = first failure.

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$REPO_ROOT"

QUICK=0
SKIP_BUILD=0
for arg in "$@"; do
  case "$arg" in
    --quick) QUICK=1 ;;
    --skip-build) SKIP_BUILD=1 ;;
  esac
done

if ! command -v dotnet >/dev/null 2>&1 && [[ -x "$HOME/.dotnet/dotnet" ]]; then
  export PATH="$HOME/.dotnet:$PATH"
fi

if ! command -v dotnet >/dev/null 2>&1; then
  echo "FAIL: dotnet SDK not found on PATH" >&2
  exit 1
fi

UA_PROJ="src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj"
DATA_PROJ="src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj"
EDITOR_FILTER="ScenarioDocumentEditor|ScenarioValidation|SaveVsExport|DoctrineInheritance|EventDebugger|SchemaConformance|StubScope|DerivedOnly"

echo "=== Scenario editor Phase 0 smoke (quick=$QUICK) ==="
echo "Repo: $REPO_ROOT"
echo "HEAD: $(git rev-parse --short HEAD 2>/dev/null || echo unknown)"

# --- GitNexus pre (best-effort; full impacts only in non-quick mode) ---
if command -v node >/dev/null 2>&1 && [[ -f "$REPO_ROOT/.gitnexus/run.cjs" ]]; then
  echo
  echo "== GitNexus status =="
  timeout 30 node .gitnexus/run.cjs status || echo "WARN: gitnexus status timed out or failed (non-fatal in --quick)"
  if [[ "$QUICK" -eq 0 ]]; then
    echo
    echo "== GitNexus impact summary (CRITICAL/HIGH editor symbols) =="
    for sym in CatalogWriteGate ScenarioDocumentEditor ScenarioValidationEngine DelegationBridge BalticReplayHarness PatrolCandidateEngagePolicy; do
      timeout 30 node .gitnexus/run.cjs impact "$sym" --direction upstream --summary-only \
        | grep -E '"impactedCount"|"risk"' || true
    done
  fi
else
  echo "SKIP: GitNexus CLI not available (node or .gitnexus/run.cjs missing)"
fi

# --- Build ---
echo
if [[ "$SKIP_BUILD" -eq 1 ]]; then
  echo "== dotnet build (skipped --skip-build) =="
  NOBUILD=(--no-build)
else
  echo "== dotnet build =="
  dotnet build ProjectAegis.sln -v minimal
  NOBUILD=()
fi

# --- Tests ---
if [[ "$QUICK" -eq 0 ]]; then
  echo
  echo "== dotnet test (full solution) =="
  dotnet test ProjectAegis.sln -v minimal
else
  echo
  echo "== dotnet test (quick: skipped full solution; use without --quick for full suite) =="
fi

echo
echo "== ReplayGolden 6/6 =="
dotnet test "$UA_PROJ" "${NOBUILD[@]}" --filter "FullyQualifiedName~ReplayGoldenSuiteTests" -v minimal

echo
echo "== PlayMode smoke (floor >= 18) =="
C2_OUT="$(dotnet test "$UA_PROJ" "${NOBUILD[@]}" --filter PlayModeSmokeHarnessTests -v minimal 2>&1)"
echo "$C2_OUT"
C2_PASS="$(echo "$C2_OUT" | grep -Eo 'Passed:[[:space:]]+[0-9]+' | tail -1 | grep -Eo '[0-9]+' || echo 0)"
if [[ "${C2_PASS:-0}" -lt 18 ]]; then
  echo "FAIL: PlayMode smoke passed count $C2_PASS < 18" >&2
  exit 1
fi

echo
echo "== Editor subset =="
dotnet test "$DATA_PROJ" "${NOBUILD[@]}" --filter "$EDITOR_FILTER" -v minimal

echo
echo "== Baltic production hash present =="
if command -v rg >/dev/null 2>&1; then
  HASH_COUNT="$(rg -l '17144800277401907079' tests/regression/ data/ 2>/dev/null | wc -l | tr -d ' ')"
else
  HASH_COUNT="$(grep -rFl '17144800277401907079' tests/regression/ data/ 2>/dev/null | wc -l | tr -d ' ')"
fi
echo "hash file count: $HASH_COUNT"
if [[ "${HASH_COUNT:-0}" -lt 18 ]]; then
  echo "FAIL: expected >= 18 files containing production hash, got $HASH_COUNT" >&2
  exit 1
fi

echo
echo "== AC-6 smoke =="
bash tools/ci/smoke-ac6.sh

echo
echo "== DelegationBridge.cs additive-only vs merge-base main =="
if git rev-parse --verify main >/dev/null 2>&1; then
  MB="$(git merge-base main HEAD 2>/dev/null || true)"
  if [[ -n "$MB" ]]; then
    # req 20 rev 2 Phase 2b / ADR-019: DelegationBridge may gain ADDITIVE API surface
    # (new members / usings only). Existing methods must stay byte-for-byte untouched.
    # Guard:
    #  1) zero deletion/modification lines (include deleted empty lines; exclude --- headers)
    #  2) pure-insertion hunks must start at member/using level — reject mid-method inserts
    #     that would otherwise pass a deletions-only check (Codex P1 on PR #259).
    BRIDGE_PATH="src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs"
    BRIDGE_DIFF="$(git diff "$MB"..HEAD -- "$BRIDGE_PATH" || true)"
    BRIDGE_DEL="$(printf '%s\n' "$BRIDGE_DIFF" | grep -E -c '^-([^-]|$)' || true)"
    echo "merge-base: $(git rev-parse --short "$MB") bridge deletion/modification lines vs merge-base: $BRIDGE_DEL"
    if [[ "${BRIDGE_DEL:-0}" -ne 0 ]]; then
      echo "FAIL: DelegationBridge.cs modifies/removes existing code vs merge-base (additive-only invariant)" >&2
      exit 1
    fi

    # Walk unified-diff addition runs. The first non-blank '+' line of each contiguous run
    # must be a file-level using or a class-member introduction (4-space indent + docs/
    # attributes/accessors). Deeper first lines imply insertion inside an existing method body.
    BRIDGE_MID_METHOD=0
    in_run=0
    while IFS= read -r line || [[ -n "$line" ]]; do
      case "$line" in
        +++*|---*)
          in_run=0
          continue
          ;;
        +*)
          content="${line#+}"
          if [[ "$in_run" -eq 0 ]]; then
            # skip blank starters inside a new run
            if [[ -z "${content//[[:space:]]/}" ]]; then
              continue
            fi
            in_run=1
            if [[ "$content" =~ ^using[[:space:]] ]] || \
               [[ "$content" =~ ^[[:space:]]{4}(///|//|\[|#region|#endregion|public[[:space:]]|private[[:space:]]|internal[[:space:]]|protected[[:space:]]|static[[:space:]]|sealed[[:space:]]|readonly[[:space:]]|partial[[:space:]]|class[[:space:]]|struct[[:space:]]|enum[[:space:]]|record[[:space:]]|\}) ]]; then
              :
            else
              BRIDGE_MID_METHOD=1
              echo "FAIL: non-member-level insertion into DelegationBridge.cs: ${content:0:120}" >&2
              break
            fi
          fi
          ;;
        *)
          in_run=0
          ;;
      esac
    done <<< "$BRIDGE_DIFF"

    if [[ "${BRIDGE_MID_METHOD:-0}" -ne 0 ]]; then
      echo "FAIL: DelegationBridge.cs inserts into existing method bodies (additive member-level only)" >&2
      exit 1
    fi
    echo "bridge additive-only: deletions=0, no mid-method insertions"
  else
    echo "SKIP: could not compute merge-base with main"
  fi
else
  echo "SKIP: main branch not found locally"
fi

echo
echo "PHASE0 SMOKE: PASS"
