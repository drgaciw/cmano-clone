# Sprint 7 — Scoring export, comms GDD, Cesium prep

**Dates:** 2026-06-02 → 2026-06-16  
**Goal:** Close doc-17 CSV export, draft cyber/comms GDD, de-risk globe spike.  
**Status:** **Complete** @ `4050abe` (2026-06-08 closeout)

## Done (headless + docs)

- [x] `LossesScoringCsvExporter` + unit tests
- [x] `BalticReplayHarness.Result.ScoringCsvRow`
- [x] C2 selection flow tests + classify map smoke
- [x] GDD `cyber-comms-degradation.md`
- [x] Cesium package pin doc (`docs/engineering/cesium-unity-package-pin.md`)
- [x] C2 manual sign-off template (`production/qa/c2-manual-signoff-2026-06-02.md`)
- [x] Comms order-log implementation (delivered in Sprint 8)

## QA gate (closed 2026-06-08)

- [x] Unity manual C2 sign-off — 13/13 PASS (`production/qa/c2-manual-signoff-2026-06-02.md`)
- [x] Cesium spike — S20/S21 `CesiumGlobeBridge` + checklist (`docs/engineering/cesium-phase-b-spike-checklist.md`)

**Evidence:** `production/qa/smoke-sprints-7-10-closeout-2026-06-08.md`

## Verification

```bash
dotnet test ProjectAegis.sln -v minimal
```