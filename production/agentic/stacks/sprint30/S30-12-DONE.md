# S30-12 story-done — CI/Local Gate Refresh

**Story:** `production/epics/sprint-30-closeout-devops/story-030-12-ci-hygiene.md`  
**Status:** Complete  
**Completed:** 2026-06-18  
**Producer:** permanent local-gate advisory (carried from S27-12 → S28-12 → S29-12)

## Verdict: COMPLETE

| AC | Evidence | Status |
|----|----------|--------|
| Evidence doc | `production/qa/sprint-30-ci-hygiene-2026-06-18.md` | COVERED |
| Buildkite merge authority | doc §Buildkite + pipeline refs | COVERED |
| Local gate fallback (day-1 ≥878; closeout target ≥918; ReplayGolden step) | `tools/verify-ci-local.ps1` + bash parity documented | COVERED |
| Non-blocking closeout | documentation-only; no workflow changes | COVERED |
| ZERO touch DelegationBridge | no code changes outside doc/script comment | COVERED |

## Verify

```bash
export PATH="/home/username01/.dotnet:$PATH"
dotnet test ProjectAegis.sln -v minimal
# 878/878 PASS @ 3406bc4

bash tools/buildkite/dotnet-ci.sh
# Release: 878/878; ReplayGolden 6/6; PlayModeSmoke 17/17; === PASS ===
```

**Test count @ `3406bc4`:** **878/878** (default solution test); **878/878** (Release CI parity).

**Closeout target (S30-13):** **≥918/918** — policy documented; current day-1 baseline unchanged.

**Blockers:** None. `pwsh` not installed on verification host — bash parity script used for local-gate evidence (equivalent steps).

## Files changed

| File | Change |
|------|--------|
| `tools/verify-ci-local.ps1` | Policy pointer S30-12; day-1 baseline ≥878 @ 3406bc4; closeout target ≥918 |
| `tools/buildkite/dotnet-ci.sh` | Policy pointer S30-12; baseline comment refreshed |
| `production/qa/sprint-30-ci-hygiene-2026-06-18.md` | New evidence doc |
| `production/epics/sprint-30-closeout-devops/story-030-12-ci-hygiene.md` | Status Complete; AC checked |
| `production/sprint-status.yaml` | 30-12 → done |
| `production/agentic/stacks/sprint30/S30-12-DONE.md` | This file |