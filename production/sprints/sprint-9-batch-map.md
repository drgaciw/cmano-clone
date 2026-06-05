# Sprint 9 — Batch CSV + degraded map symbology

**Goal:** Agentic batch runner + comms-degraded map presentation (cyber GDD P1).  
**Status:** complete on `main` @ `25fefa6` (2026-06-02)

## Done

- [x] `BalticBatchRunner` + Demo `--batch` / `--csv-out` / `--all-scenarios`
- [x] `tools/batch-replay/README.md`
- [x] Map `map-symbol--stale` / `--frozen` when comms Degraded / Denied
- [x] `ScenarioPolicyRepository.AllIds()` for discovery
- [x] Replay golden for `baltic-patrol-comms` (`tests/regression/replay-golden-baltic-comms-2026-06-02.txt`)
- [x] Commit + push Sprints 7–9 (`25fefa6`, `2a08518` gitignore)

## Next (QA gate)

- [ ] Unity QA comms + degraded map opacity (manual sign-off items 9–10)
- [ ] Cesium spike (Editor)