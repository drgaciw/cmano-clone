# Sprint 8 — Comms degradation + fuel readout

**Dates:** 2026-06-02 → 2026-06-16  
**Goal:** P0 cyber-comms order log, C2 indicators, engage guard, logistics fuel MVP line.  
**Status:** complete (headless) on `main` @ `25fefa6` (2026-06-02)

## Done (headless + Unity bindings)

- [x] `OrderLogEntryKind.CommsStateChange` + `CommsStateChangeRecord`
- [x] `CommsTimelineSimulator` + `data/scenarios/baltic-patrol-comms.policy.json`
- [x] `CommsStateProjection` → top bar `COMMS:` label
- [x] `FireAbortReason.CommsDenied` engage guard in `SimulationSession`
- [x] Message log `COMMS` category
- [x] `FuelStateProjection` → unit detail `FUEL:` line (MVP thresholds)
- [x] Tests: comms timeline, projection, Baltic replay, fuel projection

## Remaining (QA gate / P1)

- [ ] Unity QA with `scenarioPolicyId` = `baltic-patrol-comms` (shared manual sign-off)
- [x] Map ghost symbology when Degraded (P1) — `map-symbol--ghost` + scenario `commsDisplay`
- [x] Scenario-driven fuel thresholds — `logistics` block on policy JSON
- [ ] Full fuel burn sim per logistics GDD AC-3 (P2)

## Verification

```bash
dotnet test ProjectAegis.sln -v minimal
```