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

- [x] Add Cesium for Unity package (pinned via git URL in `Packages/manifest.json` — S20 foundation; verify tag in local Editor)
- [ ] Ion token stored outside repo (Editor env / user secrets — not committed)
- [x] Basic data bridge + compile (S20: CesiumGlobeBridge.cs feeds MapPanelBinder positions; full scene in Editor)
- [ ] Sample globe scene loads in Editor without console errors (Editor local step per S20 runbook)

## Rendering

- [ ] Globe visible at Play Mode start (Baltic bbox acceptable)
- [ ] Camera pan/zoom within performance budget (target: 60 FPS editor, empty scene)
- [ ] Depth/occlusion acceptable for unit billboards (note failures)

## Data bridge (spike only)

- [x] Feed positions via CesiumGlobeBridge (S20 foundation + S21 production: GetCurrentPositions() real from MapPanelBinder, integrated)
- [ ] Full feed one friendly + hostile in Editor scene (local visual gate; see S20 evidence + runbook)
- [ ] Symbol style matches UX: ■ friendly / ◆ hostile (or MIL-STD-2525 placeholder)

## Selection (spike only)

- [ ] Click globe entity → same `C2PresentationController` selection path as Toolkit map
- [ ] Selection highlight visible on globe + OOB row stays in sync

## Determinism & architecture

- [ ] No sim mutation from map clicks (presentation layer only)
- [ ] Document any non-deterministic Cesium APIs used (for determinism audit)

## Rollback

- [ ] Feature flag or separate scene so Phase A Toolkit map remains default
- [ ] Spike branch can be abandoned without breaking `dotnet test`

## Evidence

| Item | Evidence |
|------|----------|
| Globe load | _screenshot or clip path_ |
| Perf note | _FPS / frame time_ |
| Selection | _Play Mode steps_ |

## Verdict

- [ ] **PROCEED** to integrate behind flag
- [ ] **PIVOT** (list blockers)
- [ ] **KILL** globe for MVP (stay on Toolkit placeholder)