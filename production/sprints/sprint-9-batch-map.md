# Sprint 9 — Batch CSV + degraded map symbology

**Goal:** Agentic batch runner + comms-degraded map presentation (cyber GDD P1).  
**Status:** **Complete** @ `4050abe` (2026-06-08 closeout)

## Done

- [x] `BalticBatchRunner` + Demo `--batch` / `--csv-out` / `--all-scenarios`
- [x] `tools/batch-replay/README.md`
- [x] Map `map-symbol--stale` / `--frozen` when comms Degraded / Denied
- [x] `ScenarioPolicyRepository.AllIds()` for discovery
- [x] Replay golden for `baltic-patrol-comms` (`tests/regression/replay-golden-baltic-comms-2026-06-02.txt`)
- [x] Commit + push Sprints 7–9 (`25fefa6`, `2a08518` gitignore)

## QA gate (closed 2026-06-08)

- [x] Unity QA comms + degraded map opacity — sign-off checks 9–10 (`c2-manual-signoff-2026-06-02.md`)
- [x] Cesium spike — S20/S21 foundation