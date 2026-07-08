# Scenario Editor Phase 2.1 — Scope Boundary

**Date:** 2026-07-08  
**Branch:** `feat/scenario-editor-p2-1`  
**Design:** `docs/superpowers/specs/2026-07-08-scenario-editor-phase2-design.md`  
**Plan:** `docs/superpowers/plans/2026-07-08-scenario-editor-phase2-1-plan.md`  
**Status:** In progress (Task 0 spike complete)

---

## Task 0 — Map / C2 reuse spike (2026-07-08)

1. **Reuse:** APP-6 glyph contracts (`App6GlyphAtlas`, `App6Sidc`, `CesiumBillboardProjection` / map tests under `UnityAdapter.Tests/Map/`) for affiliation/frame IDs when rendering ORBAT markers later; lat/lon live on `ScenarioOrbatUnitDto` / `ScenarioWaypointDto` (degrees).
2. **Do not reuse as authoring host:** `C2PresentationController` is runtime selection bound to `ISimWorldSnapshot` / `DelegationBridge` for unit detail — wrong dependency for map authoring commits.
3. **Do not overload:** Runtime UI hosts (`C2LeftDrawerPanelHost`, `MapPlaceholderPanelHost`, `OobTreePanelHost`, `CesiumGlobeHost`) are Play Mode C2 — keep separate from Edit Mode session/bus.
4. **Existing Editor scripts:** batch runners / smoke scene builders only — no scenario map authoring window yet.
5. **Decision (locked):** New headless namespace `ProjectAegis.Delegation.UnityAdapter.Authoring` for map surface, edit FSM, findings presenter; optional thin `unity/.../Editor/ScenarioMapAuthoringWindow.cs` later (Task 9).
6. **Canonical mutations:** remain in `ProjectAegis.Data` `ScenarioDocumentEditor` + command bus — Unity never owns geometry truth.

---

## Invariants

| Invariant | Rule |
|-----------|------|
| Test floor | ≥1232 monotonic |
| ReplayGolden | 6/6 |
| Hash | `17144800277401907079` |
| PlayModeSmoke | ≥18/18 |
| DelegationBridge | ZERO hotpath production edits |
| CatalogWriteGate | Extend-only |
| Stage | Release |

---

## Gate (fill at Task 10)

```bash
dotnet build ProjectAegis.sln
dotnet test ProjectAegis.sln -v minimal
# … ReplayGolden, PlayModeSmoke, hash grep, bridge diff
```
