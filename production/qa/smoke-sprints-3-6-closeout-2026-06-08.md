# Smoke — Sprints 3–6 C2 Closeout

**Date:** 2026-06-08  
**Commit:** `41dbd00` (baseline) → post-closeout  
**Plan:** [docs/superpowers/plans/2026-06-08-sprints-3-6-c2-closeout.md](../../docs/superpowers/plans/2026-06-08-sprints-3-6-c2-closeout.md)

## Gate results

| Gate | Result |
|------|--------|
| `dotnet build ProjectAegis.sln` | PASS |
| `dotnet test ProjectAegis.sln -v minimal` | PASS (441 tests) |
| `PlayModeSmokeHarnessTests` | PASS |
| Replay golden | PASS (included in adapter suite) |

## New / expanded coverage

| Area | Tests |
|------|-------|
| C2 top bar | `C2TopBarProjectionTests` (4) |
| Selection controller | `C2PresentationControllerTests` (3) |
| Selection flow | `C2SelectionFlowTests` (+2), `PlayModeSmokeHarnessTests.Baltic_classify_selection_flow_syncs_map_oob_and_contact_summary` |
| SensorC2 overlap | `C2ContactsOverlapTests` |
| Adapter seam | `IC2PresentationFeed` on `DelegationBridgeHost`; `MapPlaceholderPanelHost`, `SensorC2PanelHost` |

## ADR-007 Phase A checklist

- [x] Placeholder map deterministic layout (`MapPictureProjectionTests`)
- [x] MapPanelBinder selection + comms ghost (`MapPanelBinderTests`, comms harness)
- [x] Selection sync map ↔ OOB ↔ contacts (classify harness row)
- [ ] Cesium globe (Phase B — Sprint 20, out of S3–6 scope)

## Binder / selection flow

- Default friendly unit → map + OOB highlights agree
- OOB row click path → `MapPanelBinder` + `OobTreePanelBinder` selected row
- Hostile symbol / contact row → `ContactSummaryProjection` line
- `C2PresentationController.ApplyDefaultSelection` skips when selection set

## Unity manual

Deferred to existing `production/qa/c2-manual-signoff-2026-06-02.md` (S19-01 PASS @ 2026-06-08). Headless proxy covers binder/selection paths above.