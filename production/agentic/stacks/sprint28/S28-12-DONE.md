# S28-12 story-done — CI/Local Gate Refresh

**Story:** `production/epics/sprint-28-closeout-devops/story-028-12-ci-hygiene.md`  
**Status:** Complete  
**Completed:** 2026-06-18  
**Producer:** permanent local-gate advisory (carried from S27-12)

## Verdict: COMPLETE

| AC | Evidence | Status |
|----|----------|--------|
| Evidence doc | `production/qa/sprint-28-ci-hygiene-2026-06-18.md` | COVERED |
| Buildkite merge authority | doc §Buildkite + pipeline refs | COVERED |
| Local gate fallback (≥787 baseline; ReplayGolden step) | `tools/verify-ci-local.ps1` + bash parity documented | COVERED |
| Non-blocking closeout | documentation-only; no workflow changes | COVERED |
| ZERO touch DelegationBridge | no code changes outside doc/script comment | COVERED |

## Verify

```bash
export PATH="/home/username01/.dotnet:$PATH"
dotnet test ProjectAegis.sln -v minimal
# 787/787 PASS @ d210d3d

bash tools/buildkite/dotnet-ci.sh
# Release: 794/794; ReplayGolden 6/6; PlayModeSmoke 15/15; === PASS ===
```

**Test count @ `d210d3d`:** **787/787** (default solution test); **794/794** (Release CI parity).

**Blockers:** None. `pwsh` not installed on verification host — bash parity script used for local-gate evidence (equivalent steps).