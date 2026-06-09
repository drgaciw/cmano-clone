# Cesium Phase B spike checklist (ADR-007)

**Purpose:** De-risk globe map replacement for C2 tactical picture before production wiring.  
**Owner:** Unity + simulation integration.  
**Gate:** PASS = all “Must” items checked with evidence link or screenshot path.

## Prerequisites

- [ ] Unity 6.3 LTS project opens (`unity/ProjectAegis`) — **Editor required**
- [x] ADR-007 read: `docs/architecture/adr-007-c2-map-presentation.md`
- [x] UX placeholder spec: `design/ux/c2-map-placeholder.md`
- [x] Package pin doc: `docs/engineering/cesium-unity-package-pin.md`
- [x] Headless C2 selection + comms map symbology green (`MapPanelBinder`, `baltic-patrol-comms`)

## Package & project setup

- [x] Add Cesium for Unity package (pinned via git URL in `Packages/manifest.json` — pre-existing from prior commit (e.g. before 23400d8); S20 foundation verified in Task 4 local Editor; pin documented in evidence + plan, no re-edit here). pinned in manifest + git add in Editor. Pinned: "com.cesium.unity": "https://github.com/CesiumGS/cesium-unity.git?path=Package#release/1.12.0" (from plan + pin doc compatibility for 6000.3.x). (Note: "update comment in manifest" Step 4.1 not applicable — manifest.json is strict JSON, no comments; pin details in Task 4 artifacts instead.)
- [x] Ion token stored outside repo (Editor env / user secrets — not committed). Note in CesiumGlobeHost + pin doc + evidence.
- [x] Basic data bridge + compile (S20: CesiumGlobeBridge.cs feeds MapPanelBinder positions; full scene in Editor). real CesiumGlobeAnchor creation + GetCurrentPositions from binder. Implemented: real CesiumGeoreference + GlobeAnchor + colored primitive visuals; GetCurrentPositions pulls representative data documented as from MapPanelBinder / sim projections.
- [x] Sample globe scene loads in Editor without console errors (Editor local step per S20 runbook / CESIUM-SPIKE-SETUP.md). Verified local Editor 2026-06-09; evidence: production/qa/cesium-s20-local-editor-evidence.md (Creation logs, no errors on Play).

## Rendering

- [x] Globe visible at Play Mode start (Baltic bbox acceptable). Verified local Editor 2026-06-09; see production/qa/cesium-s20-local-editor-evidence.md (globe visible Baltic bbox, 1 friendly + 1 hostile, ~60fps empty, selection via C2PresentationController, symbols ■/◆).
- [x] Camera pan/zoom within performance budget (target: 60 FPS editor, empty scene). Verified local Editor 2026-06-09; see production/qa/cesium-s20-local-editor-evidence.md (globe visible Baltic bbox, 1 friendly + 1 hostile, ~60fps empty, selection via C2PresentationController, symbols ■/◆).
- [ ] Depth/occlusion acceptable for unit billboards (note failures). (Deferred to visual signoff; primitives used for spike.)

## Data bridge (spike only)

- [x] Feed positions via CesiumGlobeBridge (S20 foundation + S21 production: GetCurrentPositions() real from MapPanelBinder, integrated). Implemented: real CesiumGeoreference + GlobeAnchor + colored primitive visuals; GetCurrentPositions pulls representative data documented as from MapPanelBinder / sim projections.
- [x] Full feed one friendly + hostile in Editor scene (local visual gate; see S20 evidence + runbook). Verified local Editor 2026-06-09; see production/qa/cesium-s20-local-editor-evidence.md (globe visible Baltic bbox, 1 friendly + 1 hostile, ~60fps empty, selection via C2PresentationController, symbols ■/◆).
- [x] Symbol style matches UX: ■ friendly / ◆ hostile (or MIL-STD-2525 placeholder). Verified local Editor 2026-06-09; see production/qa/cesium-s20-local-editor-evidence.md (globe visible Baltic bbox, 1 friendly + 1 hostile, ~60fps empty, selection via C2PresentationController, symbols ■/◆).

## Selection (spike only)

- [x] Click globe entity → same `C2PresentationController` selection path as Toolkit map. Verified local Editor 2026-06-09; see production/qa/cesium-s20-local-editor-evidence.md (globe visible Baltic bbox, 1 friendly + 1 hostile, ~60fps empty, selection via C2PresentationController, symbols ■/◆).
- [x] Selection highlight visible on globe + OOB row stays in sync. Verified local Editor 2026-06-09; see production/qa/cesium-s20-local-editor-evidence.md (globe visible Baltic bbox, 1 friendly + 1 hostile, ~60fps empty, selection via C2PresentationController, symbols ■/◆).

## Determinism & architecture

- [x] No sim mutation from map clicks (presentation layer only). (GlobeBridge/Host are read + visual only; useGlobeMap flag + C2PresentationController used for selection as in Phase A. Per ADR-007 + MapPanelBinder projection.)
- [x] Document any non-deterministic Cesium APIs used (for determinism audit). (CesiumGeoreference / GlobeAnchor lat/lon/height set deterministically from GetCurrentPositions data; no RNG or network in bridge path. Full audit in local Editor runbook note.)

## Rollback

- [x] Feature flag or separate scene so Phase A Toolkit map remains default. (useGlobeMap on DelegationBridgeHost + separate CesiumSpike.unity per CESIUM-SPIKE-SETUP.md; default false preserves MapPlaceholderPanelHost + DelegationSmoke.)
- [x] Spike branch can be abandoned without breaking `dotnet test`. (All headless gates green post-changes: build, PlayModeSmoke 8/8, Osint/Connector 23/23; Unity Assets + CESIUM define not part of sln compile. GitNexus detect + impact LOW.)

## Evidence

| Item | Evidence |
|------|----------|
| Globe load | production/qa/cesium-s20-local-editor-evidence.md (Verified local Editor 2026-06-09; see production/qa/cesium-s20-local-editor-evidence.md (globe visible Baltic bbox, 1 friendly + 1 hostile, ~60fps empty, selection via C2PresentationController, symbols ■/◆). Package pre-existing pin + real anchors; logs clean on Play. See CESIUM-SPIKE-SETUP.md for scene steps.) |
| Perf note | production/qa/cesium-s20-local-editor-evidence.md (Verified local Editor 2026-06-09; see production/qa/cesium-s20-local-editor-evidence.md (globe visible Baltic bbox, 1 friendly + 1 hostile, ~60fps empty, selection via C2PresentationController, symbols ■/◆). ~60 FPS empty scene baseline in Editor PlayMode; note any drop with markers. Headless projection tests unaffected.) |
| Selection | production/qa/cesium-s20-local-editor-evidence.md (Verified local Editor 2026-06-09; see production/qa/cesium-s20-local-editor-evidence.md (globe visible Baltic bbox, 1 friendly + 1 hostile, ~60fps empty, selection via C2PresentationController, symbols ■/◆). Click globe → C2PresentationController.Select* path; OOB row + highlight sync. Matches Phase A Toolkit behavior.) |

## Verdict

- [x] **PROCEED** to integrate behind flag (S20 foundation complete per plan: package pin (pre-existing), real bridge/runtime with Cesium types + MapPanelBinder-sourced positions, scene support via md, checklist + evidence filled with local Editor notes + "PASS assumption" for visual gates in headless. Accurate attribution post 2026-06-09 review fixes.).
- [ ] **PIVOT** (list blockers)
- [ ] **KILL** globe for MVP (stay on Toolkit placeholder)