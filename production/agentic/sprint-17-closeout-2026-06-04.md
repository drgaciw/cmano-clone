# Sprint 17 — Close-out

**Date:** 2026-06-04  
**Trunk:** `main` @ `cde26fe`  
**Goal:** Database Intelligence P0 slices DATA-4 and DATA-5

## Delivered

1. **DATA-4** — `ValidationPipeline`, `ICatalogReader.TryGetWeaponEnvelope`, `CatalogEngageEnvelope` wiring (`production/qa/sprint-17-data-4-gitnexus-2026-06-04.md`).
2. **DATA-5** — `CmoMarkdownImporter`, `sensor-mini.md` fixture, write-gate import smoke (`production/qa/sprint-17-data-5-gitnexus-2026-06-04.md`).

**P0 Graphite stack (DATA-1..DATA-5):** complete on `main`.

## Verification evidence

- `production/qa/sprint-17-smoke-closeout-2026-06-04.md` — **PASS** (380 tests, 7 PlayMode, 7 replay)

## Carryover → Sprint 18

| ID | Theme | Priority |
|----|-------|----------|
| S18-01 | Unity C2 manual sign-off (13 checks incl. attack menu) | Must |
| S18-02 | OSINT / Dynamic Systems Agent spike (doc 05) | Should |
| S18-03 | CMO catalog Phase 2 — bulk markdown import CLI | Should |
| S18-04 | GitHub Actions billing / required checks | Should |
| S18-05 | `/map-systems` GDD wave (separate from locked reqs) | Nice |

## Next

- Kickoff: `production/sprints/sprint-18-kickoff.md`
- `current_sprint: 18` in `production/sprint-status.yaml` after producer ack