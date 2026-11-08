# Codebase Review Recommendations — Status

**Date:** 2026-07-20  
**Source of truth (actions):** [unity-integration-review-2026-07-07.md](unity-integration-review-2026-07-07.md) §5 Prioritized Action List  
**This file:** status pointer + evidence for the §5 table (path requested as `codebase-review-recommendations.md`).

| # | Action | Status | Evidence |
|---|--------|--------|----------|
| 1 | Add Runtime/Editor/Tests `.asmdef`s | **done** | `unity/ProjectAegis/Assets/Scripts/Runtime/ProjectAegis.Unity.Runtime.asmdef`, `Assets/Editor/ProjectAegis.Unity.Editor.asmdef`, `Assets/Tests/ProjectAegis.Unity.Tests.asmdef` (+ Cesium asmdef) |
| 2 | Automate PlayMode smoke in CI | **done** | Headless `PlayModeSmokeHarnessTests` + `PlayModeSmokeOrbatSeeder`; Unity `C2DelegationSmokeTests`; CI filter in `tools/buildkite/dotnet-ci.sh`; batch `C2PlayModeSignoffBatchRunner` |
| 3 | Dirty-flag + pool `MapPlaceholderPanelHost` | **done** | `MapPlaceholderPanelHost.IsDirty` / `CaptureDirtyState` + `MapSymbolPool.Sync`; `MapSymbolPoolTests` |
| 4 | Harden `BuildPlayer.cs` for game-ci | **done** | `Assets/Editor/BuildPlayer.cs` — CLI args, Mono/IL2CPP, non-zero batch exit |
| 5 | Remove `com.unity.entities` + `entities.graphics` | **done** | Absent from live `Packages/manifest.json` + lock; ADR-005 superseded; template cleaned 2026-07-20 |
| 6 | IL2CPP backend + Input System migration | **partial** | IL2CPP via `BuildPlayer` done; Input System **deferred** (no package; legacy Input Manager by policy — needs human approval) |
| 7 | Headless entity-scale benchmark (INF-5.1) | **done** | `src/ProjectAegis.Sim.Benchmark`, `docs/reports/sim-entity-scale-benchmark-2026-07-08.md`, `SimBenchmarkTests` |
| 8 | Realign ADR-005 / doc 08 §4 / VERSION.md | **done** | ADR-005 superseded + VERSION.md managed-first; doc 08 realigned 2026-07-20 (vision, §2, §4 table, §5 hosting, deferrals) |

**Deferred without approval:** full Input System package migration (action 6 remainder).  
**Do not re-add Entities** without human package approval.
