# Sprint 35 Perf Re-Profile — Raw Command Output

**Story:** S35-17 — Perf Re-Profile Appendix (Post P0)  
**Date:** 2026-06-19  
**Owner:** perf-profile  
**Baseline:** `production/perf/perf-profile-polish-baseline-2026-06-19.md`  
**Dependencies merged:** S35-05 (detection P0), S35-10 (DecisionLog/Datalink P1)

---

## Environment

| Item | Value |
|------|-------|
| OS | Linux |
| `PATH` | `/home/username01/.dotnet:$PATH` |
| Host | Local dev machine (same as pre-merge baseline) |
| Unity C2 frame | **UNKNOWN** — Linux headless; cross-ref `production/perf/unity-c2-frame-baseline-s35-2026-06-19.md` |

---

## Command 1 — ReplayGoldenSuiteTests

```bash
export PATH="/home/username01/.dotnet:$PATH"
/usr/bin/time -f 'elapsed %e' dotnet test \
  src/ProjectAegis.Delegation.UnityAdapter.Tests \
  --filter "ReplayGoldenSuiteTests" -v minimal
```

```
  Determining projects to restore...
  All projects are up-to-date for restore.
  ProjectAegis.Data -> .../bin/Debug/net8.0/ProjectAegis.Data.dll
  ProjectAegis.Sim -> .../bin/Debug/net8.0/ProjectAegis.Sim.dll
  ProjectAegis.Delegation -> .../bin/Debug/net8.0/ProjectAegis.Delegation.dll
  ProjectAegis.Delegation.UnityAdapter -> .../bin/Debug/net8.0/ProjectAegis.Delegation.UnityAdapter.dll
  ProjectAegis.Delegation.UnityAdapter.Tests -> .../bin/Debug/net8.0/ProjectAegis.Delegation.UnityAdapter.Tests.dll
Test run for .../ProjectAegis.Delegation.UnityAdapter.Tests.dll (.NETCoreApp,Version=v8.0)
VSTest version 17.11.1 (x64)

Starting test execution, please wait...
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:     6, Skipped:     0, Total:     6, Duration: 166 ms - ProjectAegis.Delegation.UnityAdapter.Tests.dll (net8.0)
elapsed 3.63
```

---

## Command 2 — Full solution Release

```bash
export PATH="/home/username01/.dotnet:$PATH"
/usr/bin/time -f 'elapsed %e' dotnet test ProjectAegis.sln -c Release -v minimal
```

```
  Determining projects to restore...
  All projects are up-to-date for restore.
  [... Release build output ...]

Passed!  - Failed:     0, Passed:   279, Skipped:     0, Total:   279, Duration: 252 ms - ProjectAegis.Sim.Tests.dll (net8.0)
Passed!  - Failed:     0, Passed:     5, Skipped:     0, Total:     5, Duration: 116 ms - ProjectAegis.Data.Excel.Tests.dll (net8.0)
Passed!  - Failed:     0, Passed:   235, Skipped:     0, Total:   235, Duration: 499 ms - ProjectAegis.Delegation.Tests.dll (net8.0)
Passed!  - Failed:     0, Passed:    42, Skipped:     0, Total:    42, Duration: 260 ms - ProjectAegis.MissionEditor.Cli.Tests.dll (net8.0)
Passed!  - Failed:     0, Passed:   245, Skipped:     0, Total:   245, Duration: 875 ms - ProjectAegis.Delegation.UnityAdapter.Tests.dll (net8.0)
Passed!  - Failed:     0, Passed:   398, Skipped:     0, Total:   398, Duration: 3 s - ProjectAegis.Data.Tests.dll (net8.0)
elapsed 11.70
```

**Total:** 279 + 5 + 235 + 42 + 245 + 398 = **1204** tests, all PASS.

---

## Summary table (vs pre-S35-05)

| Metric | Pre | Post | Δ |
|--------|-----|------|---|
| ReplayGolden pass | 6/6 | 6/6 | — |
| ReplayGolden Duration | 179 ms | 166 ms | −13 ms (−7.3%) |
| ReplayGolden elapsed | 3.36 s | 3.63 s | +0.27 s (noise) |
| Full sln pass | 1193 | 1204 | +11 tests |
| Full sln elapsed | 9.44 s | 11.70 s | +2.26 s (suite growth + rebuild) |
| ms/tick (54 iter) | 3.31 | 3.07 | −0.24 ms (−7.3%) |

**Appendix written to:** `production/perf/perf-profile-polish-baseline-2026-06-19.md` §Benchmarks — Post-S35-05 / S35-10