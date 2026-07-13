# S35-01 — Full-Solution Re-Baseline Smoke

**Date:** 2026-06-19  
**Story:** S35-01  
**Indexed commit:** 8de98b150da515b205358106852eb75376ddba5f (S34 closeout @ main)

## Gate

```bash
dotnet build ProjectAegis.sln -v minimal
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests \
  --filter "ReplayGoldenSuiteTests" -v minimal
```

## Result

| Metric | Target | Actual |
|--------|--------|--------|
| Build | 0 errors | **PASS** (7 warnings, pre-existing) |
| Full solution tests | 1193/1193 | **1193/1193 PASS** (6.38 s wall) |
| ReplayGoldenSuiteTests | 6/6 | **6/6 PASS** (270 ms) |
| DelegationBridge.cs | ZERO diff @ trunk | **PASS** |
| Production Baltic world hash | `17144800277401907079` | **UNCHANGED** (golden pin) |
| GitNexus @ HEAD | indexed | **16,508 nodes / 33,453 edges** |

## Per-assembly counts

| Assembly | Passed |
|----------|--------|
| ProjectAegis.Sim.Tests | 276 |
| ProjectAegis.Delegation.Tests | 235 |
| ProjectAegis.MissionEditor.Cli.Tests | 42 |
| ProjectAegis.Data.Excel.Tests | 5 |
| ProjectAegis.Delegation.UnityAdapter.Tests | 240 |
| ProjectAegis.Data.Tests | 395 |
| **Total** | **1193** |

## Notes

- Sprint 35 day-1 baseline holds green; blocks S35-02+ feature waves
- GitNexus evidence: `production/agentic/sprint-35-gitnexus-2026-06-19.md`
- Authority: `production/polish-scope-boundary-2026-06-19.md`; gate r2 CONCERNS uplifted