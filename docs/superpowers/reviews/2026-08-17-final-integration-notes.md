# Final integration notes — 2026-08-17

**Role:** Final integration verifier (Phase 1 + Track B kinematics)  
**Workspace:** `/home/username01/cmano-clone` (`main`, uncommitted working tree)  
**Forbidden this pass:** new features · `DelegationBridge.Tick` redesign · `CatalogWriteGate` · commits · REQ-20 append

Phase 1 (DRG-162, Track A telemetry, Track A toast/compression, Track C VFX) was already verified clean. This pass checks that Track B kinematics did not revert those surfaces and did not leave compile/integration breaks.

---

## Verdict

**CLEAN.** Phase 1 and Track B coexist in source. No merge-conflict markers, no missing braces, no compile breaks, no code fixes applied.

`dotnet build ProjectAegis.sln` — **0 errors, 0 warnings.** Focused filters — **all green** (matrix below).

Editor Game View was **not** run (headless). Pixel ACs stay **UNKNOWN** until the owner checklist below.

`DelegationBridge.cs` and `CatalogWriteGate` have **zero** diff this pass. Replay golden hash `17144800277401907079` remains in `tests/regression/`.

---

## Capability coexistence (source)

| Track | Required surface | Status | Where |
|-------|------------------|--------|-------|
| **DRG-162** | Catalog bind so datalink edges exist | **Present** | `DelegationBridgeHost.Awake`: `CatalogReader ??= Session?.CatalogReader ?? CatalogReaderFactory.TryCreateBalticPatrolReader() ?? InMemoryCatalogReader.BalticPatrolFixture()` |
| **A telemetry** | `AdvanceDecisionLog` + `PolicyUpdate` (plus AgentDecision / Mission / Event / Damage / Controller) | **Present** | `SimplePlayModeSimHost.Update` after `RunTick`; `MessageLogProjection.TryProject` switch arms |
| **A UI** | Toast + compression APIs | **Present** | `C2ClockCommand`; host `TrySetTimeAcceleration` / `TryPauseSim` / `TryResumeSim` / `RefreshAttentionToast` / `TrySeedDemoWatchAttention`; `AttentionToastPanelHost`; top-bar −/+ / PAUSE; scene builder `AttentionToast` |
| **C** | Combat VFX projection + transient apply | **Present** | `CombatVfxProjection.Project` → `LastCombatVfx` in `RunTick`; `MapPlaceholderPanelHost.ApplyTransientCombatVfx` → `MapCanvasTransientEffectsRenderer` |
| **B kinematics** | Pose on snapshot + course layer | **Present** | `ISimWorldSnapshot.TryGetKinematicPose` / `GetPlottedCourse` (defaults keep hash); `PlayModeKinematicMover` in stub host; `MapPictureProjection.Project` optional pose map; `LastMapCourses` + `MapCanvasCourseOverlayRenderer` (`map-overlay-course-layer`) |

`RunTick` still calls `Bridge.Tick` then **presentation-only** refresh (`LastMessageLog`, `LastMapSymbols`, `LastMapCourses`, `LastCombatVfx`, top bar, comms, roster). No Tick body rewrite.

---

## Overlapping files (Track B vs Phase 1)

| File | Phase 1 kept | Track B added | Collision? |
|------|--------------|---------------|------------|
| `unity/.../DelegationBridgeHost.cs` | Catalog bind, clock/toast façade, `LastCombatVfx` after tick | `LastMapCourses = MapPictureBridge.BuildCourses(...)` adjacent to VFX | **None** — additive members; VFX still after courses |
| `unity/.../SimplePlayModeSimHost.cs` | Pause early-return, 1–8× accel loop, `AdvanceDecisionLog` after tick | `_kinematics.Advance(simTimeStep)` **inside** the accel loop before `RunTick`; `TryGetKinematicPose` / `GetPlottedCourse`; plot/halt on `Move`/`Hold` | **None** — Phase 1 cadence risks honored |
| `unity/.../MapPlaceholderPanelHost.cs` | Overlay HUD + `CatalogReader`; `ApplyTransientCombatVfx` | Course renderer + lerp; `ApplyCourseOverlays` between overlay counts and VFX | **None** — three layers, three dirty refs |
| `src/.../ISimWorldSnapshot.cs` | Existing members + defaulted extras | Additive default methods (hash fallback) | **None** — stubs compile without overrides |
| `src/.../MapPictureProjection.cs` | Hash `Project(oob, contacts, seed)` | Optional `poses` arg; `ProjectCourses` | **None** — existing callers still compile |

No `<<<<<<<` / `=======` / `>>>>>>>` in `*.cs` / `*.uxml` / `*.uss`.

---

## GitNexus

| Call | Result |
|------|--------|
| `impact(DelegationBridgeHost, upstream, summaryOnly)` | **LOW** — 0 direct callers, 0 processes (Unity hosts often report empty upstream) |
| `impact(MapPictureProjection, upstream, summaryOnly)` | **LOW** — same |
| `impact(ISimWorldSnapshot, upstream)` | Index miss (stale); interface defaults mean existing implementers need no edit |
| `impact(MapSymbolEntry constructor)` | **HIGH** candidate count (hub record) — **not edited this pass**; Track B only appended optional pose fields |
| `detect_changes({scope:"all"})` | **LOW** — 36 changed files, 0 affected processes |

No HIGH / CRITICAL on symbols this verifier would have patched. Do not treat empty Unity upstream as proof of no Editor dependents.

---

## Fixes applied

**None.** Track B composed onto Phase 1 without compile or structural damage:

- Kinematic advance runs inside the existing pause/accel loop; toast pause still freezes motion.
- `AdvanceDecisionLog` stays after `RunTick` (not inside `DelegationBridge`).
- Catalog bind in `Awake` is unchanged (`DATALINKS` still require a non-null reader).
- Course polylines use `map-overlay-course-layer` after rings/edges and before Track C fire lines/impacts.
- Pose updates `MapSymbolEntry` via `MapPictureBridge.Build` — one position authority for rings, VFX, and lerp.

---

## Test matrix (RUN+READ)

| Filter / fixture | Assembly | Result |
|------------------|----------|--------|
| `dotnet build ProjectAegis.sln` | sln | **0/0** errors/warnings |
| `FullyQualifiedName~PlayModeSmokeHarnessTests` | UnityAdapter.Tests | **24/24** |
| `FullyQualifiedName~MessageLog` | Delegation.Tests | **33/33** |
| `FullyQualifiedName~MessageLog` | UnityAdapter.Tests | **13/13** |
| `FullyQualifiedName~CombatVfx` | Delegation.Tests | **8/8** |
| `CombatVfx` / `TransientEffects` / `MapCanvasTransient` | UnityAdapter.Tests | **2/2** |
| `Drg162OverlaySignoff` + `TacticalOverlayProjection` + `MapCanvasOverlayGeometry` | Delegation.Tests | **17/17** (was 16 in Phase 1; +1 `ProjectCourseSegments_*`) |
| `CatalogBind` + `MapPlaceholderPanelHostContract` + `MapCanvasOverlay` | UnityAdapter.Tests | **3/3** |
| `FullyQualifiedName~AttentionToast` | Delegation.Tests | **7/7** |
| `AttentionToast` / `C2Clock` | UnityAdapter.Tests | **11/11** |
| `MapPictureProjectionTests` + `MapSymbolPresentationLerpTests` | Delegation.Tests | **11/11** |
| `PlayModeKinematicMover` + `MapPictureBridge` + `MapCanvasCourseOverlay` | UnityAdapter.Tests | **15/15** |

**Focused total: 144/144 passed, 0 failed.**

Commands:

```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet build ProjectAegis.sln
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter FullyQualifiedName~PlayModeSmokeHarnessTests
dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj --filter FullyQualifiedName~MessageLog
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter FullyQualifiedName~MessageLog
dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj --filter FullyQualifiedName~CombatVfx
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter "FullyQualifiedName~CombatVfx|FullyQualifiedName~TransientEffects|FullyQualifiedName~MapCanvasTransient"
dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj --filter "FullyQualifiedName~Drg162OverlaySignoffProjectionTests|FullyQualifiedName~TacticalOverlayProjectionTests|FullyQualifiedName~MapCanvasOverlayGeometryTests"
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter "FullyQualifiedName~DelegationBridgeHostCatalogBindContractTests|FullyQualifiedName~MapPlaceholderPanelHostContractTests|FullyQualifiedName~MapCanvasOverlay"
dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj --filter FullyQualifiedName~AttentionToast
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter "FullyQualifiedName~AttentionToast|FullyQualifiedName~C2Clock"
dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj --filter "FullyQualifiedName~MapPictureProjectionTests|FullyQualifiedName~MapSymbolPresentationLerpTests"
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter "FullyQualifiedName~PlayModeKinematicMoverTests|FullyQualifiedName~MapPictureBridgeTests|FullyQualifiedName~MapCanvasCourseOverlayRendererContractTests"
```

Unity scripts under `#if UNITY_5_3_OR_NEWER` are **not** compiled by `ProjectAegis.sln`. Headless coverage for those hosts is source-contract tests.

---

## Owner Editor checklist

Plugins in `unity/ProjectAegis/Assets/Plugins/ProjectAegis` are **stale until copied**. Headless green does not update Editor Play Mode.

1. **Copy plugins** (from repo root):

   ```bash
   ./tools/copy-delegation-assemblies.sh
   ```

   Windows/pwsh: `./tools/copy-delegation-assemblies.ps1`

2. **Rebuild DelegationSmoke** so toast + map hosts match current wiring:  
   Unity menu **Project Aegis → Build DelegationSmoke Scene (comms QA)**  
   (or **Ensure UI Maturity Hosts (open scene)** if the scene is already open).  
   Confirm `AttentionToast` host exists. Panel Settings: **Project Aegis → Fix UIDocument PanelSettings** if Game view is empty sky.

3. **Enter Play Mode** on `Assets/Scenes/DelegationSmoke.unity`.

4. **ACK toast first.** Demo watch auto-pauses (`TIME: PAUSED`). Motion, rings, and VFX look frozen until ACK + **RESUME**. That is Phase 1 clock authority, not a kinematics bug.

5. **Watch after RESUME:**

   | Look for | Pass |
   |----------|------|
   | **Motion** | `u1` / `hostile-1` icons slide (hash start, then pose). Plot Course / Move draws a course polyline; Hold clears it and stops. |
   | **Rings / datalinks** | Envelope rings follow the selected unit; `DATALINKS` > 0 (catalog bind). |
   | **VFX** | Fire lines / impacts stay on the transient layer (not the course layer). Stub `ActiveEngagementCount => 0` — live Baltic firehose may stay empty. |
   | **Log** | Message log keeps growing (`PolicyUpdate` and other seed categories after the first post-tick step). |

6. **Compression:** − / + walks 1x → 2x → 4x → 8x; motion speeds with the accel loop. PAUSE freezes kinematics again.

7. **Do not** append REQ-20 or change Baltic v2 goldens from this checklist.

---

## Residual (not this verifier)

- Owner Game View signoff (table above) — pixel ACs UNKNOWN until plugins + scene rebuild.
- Live Baltic classify/engage firehose in Editor (stub `ActiveEngagementCount => 0`).
- BalticReplayHarness / ECS still do not publish lat/lon (hash unless a snapshot implementer opts in).
- Plot Course destination is a deterministic lookahead, not a player-clicked waypoint list.
- CMD-39 REQ-20 append (owner-gated).
- Cesium globe world-anchor unchanged (Phase B later).
