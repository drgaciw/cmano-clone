# Sprints 7–10 Closeout — Scoring CSV, Comms/Fuel, Batch Replay, Fuel Ledger QA

**Date:** 2026-06-08  
**Status:** COMPLETE  
**Kickoff:** Sprints 7–10 sprint plans under `production/sprints/`

## Delivered (headless + evidence)

### Sprint 7 — Scoring export, comms GDD, Cesium prep
- `LossesScoringCsvExporter` + `LossesScoringProjection` + unit tests
- `BalticReplayHarness.Result.ScoringCsvRow` deterministic CSV row
- GDD `design/gdd/cyber-comms-degradation.md` + `design/gdd/scoring-losses.md`
- Cesium package pin + Phase B checklist (Editor spike satisfied by S20/S21 `CesiumGlobeBridge`)

### Sprint 8 — Comms degradation + fuel readout
- `OrderLogEntryKind.CommsStateChange` + `CommsStateChangeRecord` in `DecisionLog`
- `CommsTimelineSimulator` + `baltic-patrol-comms.policy.json`
- `CommsStateProjection` → top bar `COMMS:` + message log `COMMS` category
- `FireAbortReason.CommsDenied` engage guard in `SimulationSession`
- `FuelStateProjection` → unit detail `FUEL:` line (scenario-driven thresholds)
- Map ghost symbology when Degraded (`MapPanelBinder` + USS)

### Sprint 9 — Batch CSV + degraded map symbology
- `BalticBatchRunner` + Demo `--batch` / `--csv-out` / `--all-scenarios`
- `tools/batch-replay/README.md`
- Map `map-symbol--stale` / `--frozen` when comms Degraded / Denied
- Replay golden `tests/regression/replay-golden-baltic-comms-2026-06-02.txt`

### Sprint 10 — Fuel ledger, replay hash, QA prep
- `FuelLedger` (`AdvanceTick` / `EnsureUnit` / `GetRemainingKg` / `ResolveBand`) + tests
- `FuelTimelineTracker` band transitions + optional `FuelBurn` order log (`logTickBurn`)
- `OrderLogReplayFingerprint.ComputeSha256Hex` + harness `FINGERPRINT_SHA256=` line
- `CatalogQuarantinePromoter`, `PlayerOrder` on human enqueue
- `ScenarioCommsStatusCommand` CLI (MCP-adjacent comms timeline export)

## Gates (final)

| Gate | Result |
|------|--------|
| `dotnet build ProjectAegis.sln` | PASS |
| `dotnet test ProjectAegis.sln` | **443/443 PASS** |
| PlayMode smoke | **8/8 PASS** |
| Sprints 7–10 scoped filters | **28/28 PASS** (Fuel 14, Comms 15, Scoring/Batch 6, golden comms 1 overlap) |
| C2 manual sign-off proxy | **13/13 PASS** @ `production/qa/c2-manual-signoff-2026-06-02.md` |

**Indexed commit:** `4050abe302401721bf41849198c9a589c09d3f9f`

## Test-criterion traceability (sprint tasks)

| Sprint task | Primary evidence |
|-------------|------------------|
| s7-csv-export | `LossesScoringCsvExporterTests`, `BalticBatchRunnerTests` |
| s7-c2-smoke | `C2SelectionFlowTests`, `PlayModeSmokeHarnessTests` |
| s7-gdd-cyber | `design/gdd/cyber-comms-degradation.md` |
| s7-cesium-spike | S20/S21 `CesiumGlobeBridge` + `docs/engineering/cesium-phase-b-spike-checklist.md` |
| s8-comms-log | `CommsTimelineSimulatorTests`, `BalticReplayHarnessCommsTests` |
| s8-comms-c2 | `CommsStateProjectionTests`, `C2TopBarProjectionTests` |
| s8-comms-engage | `BalticReplayHarnessCommsTests.Comms_denied_appends_policy_denial_*` |
| s8-fuel-line | `FuelStateProjectionTests`, `FuelTimelineTrackerTests` |
| s9-batch-cli | `BalticBatchRunnerTests`, `Program.cs --batch` |
| s9-map-stale | `MapPanelBinderTests`, `BalticReplayHarnessCommsTests` |
| s10-fuel-ledger | `FuelLedgerTests` (4), `FuelTimelineTrackerTests`, `BalticReplayHarnessFuelTests` |
| s10-replay-sha256 | `ReplayGoldenBalticCommsTests`, `ReplayGoldenAssertions` |
| s10-comms-cli | `ScenarioCommsStatusCommandTests` |

## GitNexus note

`gitnexus_detect_changes` may surface **later catalog impact** (S16–S21 `ProjectAegis.Data` / platform editor work) when re-indexing after this closeout. Sprints 7–10 symbols (`FuelLedger`, `CommsState`, `LossesScoring*`, `BalticBatchRunner`) are upstream of catalog reads only via harness defaults — no schema coupling in these slices.

## Out of scope (documented, not blocking)

- Full Editor visual walk for click-feel (non-blocking per PI-006 headless proxy)
- Req 19 AC-3 agent stale-track warning (no automated test yet)
- Req 19 AC-4 `DenyFireControl` cyber action (Wave 5 / separate track)

**Sprints 7–10 complete at 100% for declared MVP headless scope.**