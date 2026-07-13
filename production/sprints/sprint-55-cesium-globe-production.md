# Sprint 55 — Cesium Globe Production (E4 / cloud track)

**Date:** 2026-06-21  
**Track:** E4 Cesium globe (parallel to Hypersonic UI + Editor PNG shadow evidence)  
**After:** S54  
**Cites:**  
- `production/release-enablement-scope-boundary-2026-06-20.md` (Req 20; Cesium/globe production; post-MVP Track B boundary)  
- `docs/reports/future-sprint-roadpmap.md` §10 S55 (roadmap)  
- `production/polish-scope-boundary-2026-06-19.md` (handoff #1: Globe map + Cesium production)  
- `docs/architecture/adr-007-c2-map-presentation.md` (Phase B)  
- `Game-Requirements/requirements/20-Command-And-Control-UI.md` (P0 WGS84 globe, pan/zoom/rotate, milsim UX, integration)  
- `design/ux/c2-command-post.md`, `design/ux/c2-map-placeholder.md`  
- `docs/engineering/cesium-phase-b-spike-checklist.md`, `cesium-unity-package-pin.md`  
- `production/sprint-status.yaml` (wt update)  

**Superpowers enforced:** systematic (preflight, impact), verification-before-completion, dispatching-parallel (this isolated wt), using-git-worktrees (this wt).

## Context / Prior
- S20: Cesium foundation (pin, CesiumGlobeBridge + Host stubs, GetCurrentPositions/GetBillboardMarkers, Baltic demo, Editor visual gate via spike checklist).
- S21/S24/S25: data polish, APP-6 billboards (CesiumBillboardProjection + App6Sidc), depth/occlusion, selection sync evidence (useGlobeMap flag; CesiumSpike.unity separate from DelegationSmoke).
- Spike is functional in Editor (primitives + anchors) but not full production integration (live from snapshot in main C2 flow, full milsim controls, bidirectional panel sync).
- Req 20 tracker: **Partial**; next = "Globe map".
- Placeholder (MapPlaceholderPanelHost + MapPanelBinder + MapPictureProjection) remains default (CI safe via flag).

## Goal (narrow: Cesium globe only)
Production-ready additive integration:
- Globe view driven by Cesium (WGS84 real positions projected from sim snapshot via shared projections).
- Entity sync: live anchors/billboards from LastMapSymbols / MapPicture data (friendly + contacts).
- Camera/controls per milsim UX (req20 P0: pan, zoom, rotate; mouse+keyboard; telegraph friendly).
- Integration with C2 panels: globe click → C2PresentationController selection (same as placeholder); selection drives OOB/right panel sync.
- No change to sim, DelegationBridge, replay hash, headless tests, placeholder default, useGlobeMap=false for smoke.

## Must
- Enhance CesiumGlobeBridge: direct DelegationBridgeHost wiring + live refresh (OnEnable + Update/LateUpdate poll symbols).
- New: GlobeCameraController (milsim: LMB drag rotate/pan view, wheel zoom height, arrow keys, double-click center on symbol if possible).
- Selection: globe entities forward clicks to bridgeHost.SelectUnit/SelectContact.
- Update CesiumGlobeHost + comments for production (ion note, camera hookup).
- Keep additive: #if CESIUM_FOR_UNITY + UNITY guards; useGlobeMap contract unchanged.
- GitNexus impact + detect (preflight done; LOW/MEDIUM on presentation symbols only).
- Evidence: build logs, projection tests, sprint-status update, this plan.

## Verification gates (before claim)
- `dotnet build ProjectAegis.sln`
- `dotnet test ProjectAegis.sln -v minimal` (or filtered C2/Projection)
- PlayMode smoke harness filter (covers projections used by bridge)
- Read full outputs.
- No edits to DelegationBridge.cs (ZERO-touch invariant).
- Isolation in this wt only.

## Non-goals (narrow scope)
- Full CesiumSpike.unity edits or new main scene (use existing wiring pattern).
- Hypersonic UI (parallel).
- Editor PNG evidence (shadow/local later).
- APP-6 3D models or LOD (future).
- Sim world positions (use projection until DOTS lat/lon).

## GitNexus preflight summary (executed)
- C2PresentationController: MEDIUM risk, 12 impacted (8 direct), Presentation/Runtime/Tests modules; callers: DelegationBridgeHost, RightUnitPanelHost, tests. No sim impact.
- MapPictureProjection / CesiumBillboardProjection: LOW (0 upstream).
- Edit blast contained to presentation layer (safe per ADR-007/010).

## Changes (additive)
- production/sprints/sprint-55-cesium-globe-production.md (this)
- unity/ProjectAegis/Assets/Scripts/Runtime/CesiumGlobeBridge.cs (live sync + bridge host wiring + selection)
- unity/ProjectAegis/Assets/Scripts/Runtime/CesiumGlobeHost.cs (prod notes)
- NEW: unity/ProjectAegis/Assets/Scripts/Runtime/GlobeCameraController.cs (milsim controls)
- production/sprint-status.yaml (S55 entry + evidence note)
- production/qa/cesium-s55-production-evidence.md (stub + build excerpts)
- (any minor comment hygiene in related Unity Runtime if needed; preflight checked)

## Acceptance
- Globe renders + live updates from sim tick symbols when useGlobeMap (Editor).
- Camera provides milsim UX without breaking other panels.
- Click on globe marker selects and syncs to right panel / OOB (via shared controller).
- All builds/tests green; no regression to smoke proxy (18/18) or replay.
- "S55 Cesium complete" (or documented gaps).

**Owner:** S55 Cesium globe production subagent (E4).  
**Coord:** Parallel tracks ok; this wt isolated.
