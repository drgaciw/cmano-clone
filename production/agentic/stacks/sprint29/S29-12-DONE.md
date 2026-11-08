# S29-12 story-done — CI/Local Gate Refresh

**Story:** `production/epics/sprint-29-closeout-devops/story-029-12-ci-hygiene.md`  
**Status:** Complete  
**Completed:** 2026-06-18  
**Producer:** permanent local-gate advisory (carried from S27-12 → S28-12)

## Verdict: COMPLETE

| AC | Evidence | Status |
|----|----------|--------|
| Evidence doc | `production/qa/sprint-29-ci-hygiene-2026-06-18.md` | COVERED |
| Buildkite merge authority | doc §Buildkite + pipeline refs | COVERED |
| Local gate fallback (≥847 baseline; ReplayGolden step) | `tools/verify-ci-local.ps1` + bash parity documented | COVERED |
| Non-blocking closeout | documentation-only; no workflow changes | COVERED |
| ZERO touch DelegationBridge | no code changes outside doc/script comment | COVERED |

## Verify

```bash
export PATH="/home/username01/.dotnet:$PATH"
dotnet test ProjectAegis.sln -v minimal
# 847/847 PASS @ 465cb65

bash tools/buildkite/dotnet-ci.sh
# Release: 847/847; ReplayGolden 6/6; PlayModeSmoke 17/17; === PASS ===
```

**Test count @ `465cb65`:** **847/847** (default solution test); **847/847** (Release CI parity).

**Blockers:** None. `pwsh` not installed on verification host — bash parity script used for local-gate evidence (equivalent steps).

## Files changed

| File | Change |
|------|--------|
| `tools/verify-ci-local.ps1` | Policy pointer S29-12; baseline ≥847; PlayModeSmoke 17/17 |
| `tools/buildkite/dotnet-ci.sh` | Policy pointer S29-12; baseline comment added |
| `production/qa/sprint-29-ci-hygiene-2026-06-18.md` | New evidence doc |
| `production/agentic/stacks/sprint29/S29-12-DONE.md` | This file |