# S32-12 story-done — CI/Local Gate Refresh

**Story:** `production/epics/sprint-32-closeout-devops/story-032-12-ci-hygiene.md`  
**Status:** Complete  
**Completed:** 2026-06-19  
**Producer:** permanent local-gate advisory (carried from S27-12 → S30-12; S31-12 carryover)

## Verdict: COMPLETE

| AC | Evidence | Status |
|----|----------|--------|
| Evidence doc | `production/qa/sprint-32-ci-hygiene-2026-06-19.md` | COVERED |
| Buildkite merge authority | doc §Buildkite + pipeline refs | COVERED |
| Local gate fallback (day-1 ≥1006; closeout target ≥1046; ReplayGolden step) | `tools/verify-ci-local.ps1` + bash parity documented | COVERED |
| S31-12 carryover / deferral rationale | doc §S31-12 carryover | COVERED |
| Bash fallback when `pwsh` unavailable | doc + story verify command | COVERED |
| Non-blocking closeout | documentation-only; no workflow changes | COVERED |
| ZERO touch DelegationBridge | empty diff on `DelegationBridge.cs` | COVERED |

## Verify

```bash
export PATH="/home/username01/.dotnet:$PATH"
dotnet test ProjectAegis.sln -v minimal
# 1073/1073 PASS @ d3db76db working tree

pwsh -File tools/verify-ci-local.ps1 2>/dev/null || bash tools/buildkite/dotnet-ci.sh
# Release: 1073/1073; ReplayGolden 6/6; PlayModeSmoke 17/17; === PASS ===

git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
# (empty)
```

**Test count @ S32-12 verify:** **1073/1073** (default solution test); **1073/1073** (Release CI parity).

**Policy thresholds:** day-1 **≥1006** (S32-01 @ `d3db76db`); closeout target **≥1046** (S32-13).

**S31-12 deferral:** Sprint 31 deferred S31-12 (nice-to-have); S32-12 completes carryover with refreshed thresholds. Third deferral to S33-12 remains acceptable per cut line but not required.

**Blockers:** None. `pwsh` not installed on verification host — bash parity script used for local-gate evidence (equivalent steps).

## Files changed

| File | Change |
|------|--------|
| `tools/verify-ci-local.ps1` | Policy pointer S32-12; day-1 baseline ≥1006; closeout target ≥1046; bash fallback note |
| `tools/buildkite/dotnet-ci.sh` | Policy pointer S32-12; baseline comment refreshed |
| `production/qa/sprint-32-ci-hygiene-2026-06-19.md` | New evidence doc |
| `production/epics/sprint-32-closeout-devops/story-032-12-ci-hygiene.md` | Status Complete; AC checked |
| `production/sprint-status.yaml` | 32-12 → done |
| `production/agentic/stacks/sprint32/S32-12-DONE.md` | This file |