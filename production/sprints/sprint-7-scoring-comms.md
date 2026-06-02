# Sprint 7 — Scoring export, comms GDD, Cesium prep

**Dates:** 2026-06-02 → 2026-06-16  
**Goal:** Close doc-17 CSV export, draft cyber/comms GDD, de-risk globe spike.  
**Code status:** complete on `main` @ `25fefa6` (2026-06-02)

## Done (headless + docs)

- [x] `LossesScoringCsvExporter` + unit tests
- [x] `BalticReplayHarness.Result.ScoringCsvRow`
- [x] C2 selection flow tests + classify map smoke
- [x] GDD `cyber-comms-degradation.md`
- [x] Cesium package pin doc (`docs/engineering/cesium-unity-package-pin.md`)
- [x] C2 manual sign-off template (`production/qa/c2-manual-signoff-2026-06-02.md`)
- [x] Comms order-log implementation (delivered in Sprint 8)

## Remaining (QA gate / Sprint 8+)

- [ ] Unity manual C2 sign-off — execute checklist (not substitutable by headless)
- [ ] Cesium spike scene in Editor — `docs/engineering/cesium-phase-b-spike-checklist.md`

## Verification

```bash
dotnet test ProjectAegis.sln -v minimal
```