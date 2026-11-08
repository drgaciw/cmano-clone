# Sprint 113 Parallel Kickoff — 2026-08-11

**Orchestrator:** goal implementer (Grok)  
**Pattern:** `dispatching-parallel-agents` — surface-disjoint lanes  
**Stage:** Release  
**Status:** complete — A 007/008 · B 011/012 · C manifest — all closed.

## Surfaces (must not co-edit)

| Lane | Surface | Agent role | Deliverable |
|------|---------|------------|-------------|
| **asset-c2-a** | `production/assets/c2/C2LeftDrawerPanel.uss` only (+ own row in manifest via integrator) | Asset stub | ASSET-007 Done stub |
| **asset-c2-b** | `production/assets/c2/RightUnitDetailPanel.uss` only | Asset stub | ASSET-008 Done stub |
| **asset-011/012** | `DelegationBadgeOverlay.uss`, `PolicyEmconHud.uss` | Asset stub | ASSET-011/012 Done (#462) |
| **s36-ux** | `production/qa/s36-ux-pack-disposition-2026-08-11.md` + Linear DRG-22…28 states | UX pack close | All six Done/Canceled |
| **closeout** | S113 smoke + waterline (serial after green) | Integrator | Smoke + gates |

**Shared resource rule:** Only **closeout** edits `design/assets/asset-manifest.md` progress table after stubs land (or single integrator pass). Lanes A/B do not touch Linear. Lane C does not touch `production/assets/c2/`.

## CRITICAL hubs — do not touch

DelegationBridge · CatalogWriteGate · PatrolCandidateEngagePolicy · BalticReplayHarness · ScenarioDocumentEditor

## Dispatch order

1. Parallel: asset-c2-a ‖ asset-c2-b ‖ s36-ux  
2. Integrator: manifest honesty + S113 closeout  
3. Serial: waterline RUN+READ  

## Evidence

Closeout: `production/agentic/sprint-113-closeout-2026-08-11.md`  
Smoke: `production/qa/smoke-sprint-113-2026-08-11.md`
