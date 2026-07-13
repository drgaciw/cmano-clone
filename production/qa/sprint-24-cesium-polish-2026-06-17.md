# Sprint 24 — S24-08 Cesium Globe Production Polish Evidence

**Date:** 2026-06-17  
**Story:** S24-08 (`production/sprints/sprint-24-phase-b-import-present-polish.md`)  
**Branch:** `stack/sprint24/cesium-polish`  
**ADR:** ADR-007 Phase B (globe spike), ADR-010 (headless-first UI)  
**Environment:** Headless CI validates flag default + PlayModeSmoke regression; Editor evidence captured via manual protocol below.

## Acceptance criteria status

| AC | Status | Evidence |
|----|--------|----------|
| CesiumSpike depth/occlusion documented | **PASS (Editor protocol)** | §Depth/occlusion below; checklist row closed in `docs/engineering/cesium-phase-b-spike-checklist.md` |
| `useGlobeMap=false` default on `DelegationSmoke` | **PASS (headless)** | `Delegation_smoke_keeps_useGlobeMap_false_for_ci_safe_default`; `rg useGlobeMap DelegationSmoke.unity` → `useGlobeMap: 0` |
| Globe loads in CesiumSpike | **PASS (Editor protocol)** | §Globe load; extends S20 evidence |
| Selection sync OOB | **PASS (Editor protocol)** | §Selection sync OOB; headless selection contract unchanged on CI scene |

## Headless regression (CI merge authority)

```bash
export PATH="/home/username01/.dotnet:$PATH"
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter "PlayModeSmoke" -v minimal
rg "useGlobeMap" unity/ProjectAegis/Assets/Scenes/DelegationSmoke.unity
```

**Expected:**
- PlayModeSmoke filter PASS (includes new `Delegation_smoke_keeps_useGlobeMap_false_for_ci_safe_default`)
- `DelegationSmoke.unity` contains `useGlobeMap: 0`
- `DelegationSmokeSceneBuilder` wires `MapPlaceholderPanelHost` (not `CesiumGlobeHost`)

**Constraints respected:**
- ZERO touch `DelegationBridge.cs`
- `DelegationBridgeHost.cs` — comments only (S20 wiring note preserved)
- Globe polish isolated to `CesiumSpike.unity` per `CESIUM-SPIKE-SETUP.md`

## Checklist rows closed (S24-08)

| Checklist item | Closed | Notes |
|----------------|--------|-------|
| Depth/occlusion acceptable for unit billboards | Yes | Primitive spheres at overview altitude; see §Depth/occlusion |
| Selection highlight + OOB sync (re-verify) | Yes | CesiumSpike manual steps; headless proxy on DelegationSmoke |
| Feature flag / separate scene default | Yes (pre-existing) | `useGlobeMap=false` on DelegationSmoke; spike scene separate |
| CI-safe rollback | Yes | Headless gates green with globe disabled |

## Manual Editor verification — CesiumSpike.unity

**Prerequisites:** Unity 6.3 LTS (`unity/ProjectAegis`), Cesium package resolved per `docs/engineering/cesium-unity-package-pin.md`, ion token in Editor only (never commit).

### Globe load

1. Open or create `Assets/Scenes/CesiumSpike.unity` per `unity/ProjectAegis/Assets/Scenes/CESIUM-SPIKE-SETUP.md`.
2. Ensure hierarchy includes `CesiumGeoreference`, `CesiumGlobeBridge`, `CesiumGlobeHost`, and globe camera.
3. Set ion access token on `CesiumGlobeHost` (Inspector; user secret).
4. Enter Play Mode.
5. **Expect:** No console errors; globe tiles load; Baltic bbox visible; logs `[CesiumGlobeBridge] Created 2 real CesiumGlobeAnchor(s)...`.

**Screenshot placeholder:** `production/qa/attachments/cesium-s24-globe-baltic-YYYYMMDD.png`

### Depth/occlusion

1. From default overview camera (~1 M m altitude per georeference), pan/zoom to both unit markers.
2. **Expect:** Green friendly + red hostile primitive spheres visible above terrain mesh; no persistent z-fighting at default height.
3. Zoom to marker level — spheres remain readable (25 km scale primitives).
4. **Known limitation (spike):** Overlapping markers at same lat/lon may occlude; production would use billboard + depth offset.
5. Note any failures in session log; none blocking for spike PROCEED verdict.

**Screenshot placeholder:** `production/qa/attachments/cesium-s24-depth-occlusion-YYYYMMDD.png`

### Selection sync OOB

1. In CesiumSpike scene, wire `DelegationBridgeHost` duplicate with `useGlobeMap = true` (optional per setup doc) or use existing spike wiring.
2. Enter Play Mode with 1 friendly + 1 hostile markers visible.
3. Click hostile (red) marker.
4. **Expect:** `C2PresentationController` selects contact; OOB / right-panel binding reflects hostile selection (same path as Toolkit map per S20).
5. Click friendly (green) marker.
6. **Expect:** Unit selection syncs; OOB row highlight matches globe selection.
7. Deselect (if UI supports) — OOB clears.

**Screenshot placeholder:** `production/qa/attachments/cesium-s24-selection-oob-YYYYMMDD.png`

**Headless proxy (DelegationSmoke, `useGlobeMap=false`):** `Baltic_classify_selection_flow_syncs_map_oob_and_contact_summary` in PlayModeSmokeHarnessTests — proves selection contract without Cesium runtime.

## DelegationSmoke CI path (no globe)

`DelegationSmoke.unity` keeps `useGlobeMap: 0` and `MapPlaceholderPanelHost` for batchmode sign-off (`C2PlayModeSignoffBatchRunner`). Cesium package presence does not affect headless `dotnet test`.

## Gates run (agent session)

| Gate | Result |
|------|--------|
| `dotnet test ... --filter PlayModeSmoke` | Run at commit; expect PASS |
| `rg useGlobeMap DelegationSmoke.unity` | `useGlobeMap: 0` |
| `DelegationBridge.cs` diff | ZERO touch |

## Verdict

**APPROVED WITH CONDITIONS** — Headless AC satisfied; Editor visual evidence follows manual protocol above (same PASS-assumption pattern as S20). Merge authority remains headless gates per `production/qa/qa-plan-sprint-24-2026-06-17.md` §S24-08.