# Smoke check — Sprint 2 (sensor classify + C2 UI closeout)

**Date:** 2026-06-08  
**Build:** `main` @ `41dbd0031e3868e96a3242c37272b7583d8fff8c`  
**Mode:** sprint (automated headless gate; Agent D closeout)  
**Verdict:** **PASS**

## Commands (Sprint 2 scoped filters)

| Step | Command | Result |
|------|---------|--------|
| UnityAdapter (Sensor C2 + classify replay + PlayMode) | `dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests --filter "FullyQualifiedName~SensorC2\|FullyQualifiedName~ReplayGoldenBalticClassify\|FullyQualifiedName~PlayModeSmoke" -v minimal` | **12/12 PASS** |
| Sim (classify FSM) | `dotnet test src/ProjectAegis.Sim.Tests --filter "FullyQualifiedName~PdContactClassify" -v minimal` | **2/2 PASS** |
| Delegation (contact picture + panel binder) | `dotnet test src/ProjectAegis.Delegation.Tests --filter "FullyQualifiedName~ContactPicture\|FullyQualifiedName~SensorC2Panel" -v minimal` | **6/6 PASS** |

## Captured output

### UnityAdapter — Sensor C2 / classify replay / PlayMode smoke

```
  Determining projects to restore...
  All projects are up-to-date for restore.
  ProjectAegis.Data -> D:\MyCode\cmano-clone\src\ProjectAegis.Data\bin\Debug\net8.0\ProjectAegis.Data.dll
  ProjectAegis.Sim -> D:\MyCode\cmano-clone\src\ProjectAegis.Sim\bin\Debug\net8.0\ProjectAegis.Sim.dll
  ProjectAegis.Delegation -> D:\MyCode\cmano-clone\src\ProjectAegis.Delegation\bin\Debug\net8.0\ProjectAegis.Delegation.dll
  ProjectAegis.Delegation.UnityAdapter -> D:\MyCode\cmano-clone\src\ProjectAegis.Delegation.UnityAdapter\bin\Debug\net8.0\ProjectAegis.Delegation.UnityAdapter.dll
  ProjectAegis.Delegation.UnityAdapter.Tests -> D:\MyCode\cmano-clone\src\ProjectAegis.Delegation.UnityAdapter.Tests\bin\Debug\net8.0\ProjectAegis.Delegation.UnityAdapter.Tests.dll
Test run for D:\MyCode\cmano-clone\src\ProjectAegis.Delegation.UnityAdapter.Tests\bin\Debug\net8.0\ProjectAegis.Delegation.UnityAdapter.Tests.dll (.NETCoreApp,Version=v8.0)
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:    12, Skipped:     0, Total:    12, Duration: 508 ms - ProjectAegis.Delegation.UnityAdapter.Tests.dll (net8.0)
```

### Sim — PdContactClassify

```
  Determining projects to restore...
  All projects are up-to-date for restore.
  ProjectAegis.Data -> D:\MyCode\cmano-clone\src\ProjectAegis.Data\bin\Debug\net8.0\ProjectAegis.Data.dll
  ProjectAegis.Sim -> D:\MyCode\cmano-clone\src\ProjectAegis.Sim\bin\Debug\net8.0\ProjectAegis.Sim.dll
  ProjectAegis.Sim.Tests -> D:\MyCode\cmano-clone\src\ProjectAegis.Sim.Tests\bin\Debug\net8.0\ProjectAegis.Sim.Tests.dll
Test run for D:\MyCode\cmano-clone\src\ProjectAegis.Sim.Tests\bin\Debug\net8.0\ProjectAegis.Sim.Tests.dll (.NETCoreApp,Version=v8.0)
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:     2, Skipped:     0, Total:     2, Duration: 33 ms - ProjectAegis.Sim.Tests.dll (net8.0)
```

### Delegation — ContactPicture + SensorC2Panel

```
  Determining projects to restore...
  All projects are up-to-date for restore.
  ProjectAegis.Data -> D:\MyCode\cmano-clone\src\ProjectAegis.Data\bin\Debug\net8.0\ProjectAegis.Data.dll
  ProjectAegis.Sim -> D:\MyCode\cmano-clone\src\ProjectAegis.Sim\bin\Debug\net8.0\ProjectAegis.Sim.dll
  ProjectAegis.Delegation -> D:\MyCode\cmano-clone\src\ProjectAegis.Delegation\bin\Debug\net8.0\ProjectAegis.Delegation.dll
  ProjectAegis.Delegation.Tests -> D:\MyCode\cmano-clone\src\ProjectAegis.Delegation.Tests\bin\Debug\net8.0\ProjectAegis.Delegation.Tests.dll
Test run for D:\MyCode\cmano-clone\src\ProjectAegis.Delegation.Tests\bin\Debug\net8.0\ProjectAegis.Delegation.Tests.dll (.NETCoreApp,Version=v8.0)
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:     6, Skipped:     0, Total:     6, Duration: 34 ms - ProjectAegis.Delegation.Tests.dll (net8.0)
```

## Sprint 2 scope covered (automated)

| Story | Automated coverage |
|-------|-------------------|
| sensor-classify-slice / story-001 | `PdContactClassifyTests` (2), `ReplayGoldenBalticClassifyTests` (1) |
| sensor-c2-ui-slice / story-001 | `ContactPictureProjectionTests` (4), `SensorC2BridgeTests` (1), `PlayModeSmokeHarnessTests.Baltic_patrol_sensor_c2_snapshot_matches_harness_run` |
| sensor-c2-ui-slice / story-002 | `SensorC2PanelBinderTests` (2), `SensorC2PanelBinderIntegrationTests` (1); assets `SensorC2Panel.uxml` / `.uss`, host `SensorC2PanelHost.cs` |

## Blockers for QA hand-off / close

- Unity Editor visual confirmation of `SensorC2PanelHost` strip still requires local `unity/ProjectAegis` + Play Mode; headless binder/integration tests green. `PLAYMODE-SMOKE.md` documents general `bridgeHost` wiring; explicit Sensor C2 panel row optional for manual QA.
- No S1/S2 from automated gates.

## Recommendation

All Sprint 2 scoped filters green (**18/18**). Classify FSM, contact picture projection, Sensor C2 bridge, and UI Toolkit panel binder verified headless. Stories remain **Complete** with test traceability in epic story files.

*Generated during Sprint 2 closeout 2026-06-08 (Agent D).*