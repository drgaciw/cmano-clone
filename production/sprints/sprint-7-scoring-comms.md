# Sprint 7 — Scoring export, comms GDD, Cesium prep

**Dates:** 2026-06-02 → 2026-06-16  
**Goal:** Close doc-17 CSV export, draft cyber/comms GDD, de-risk globe spike.

## In progress / done (headless)

- [x] `LossesScoringCsvExporter` + unit tests
- [x] `BalticReplayHarness.Result.ScoringCsvRow`
- [x] C2 selection flow tests + classify map smoke
- [x] GDD `cyber-comms-degradation.md`
- [x] Cesium package pin doc

## Remaining

- [ ] Unity manual C2 sign-off (`production/qa/c2-manual-signoff-2026-06-02.md`)
- [ ] Cesium spike scene in Editor (checklist)
- [ ] Comms order-log kinds (implementation — Sprint 8)

## Verification

```bash
dotnet test ProjectAegis.sln -v minimal
```