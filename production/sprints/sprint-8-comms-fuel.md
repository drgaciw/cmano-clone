# Sprint 8 — Comms degradation + fuel readout

**Dates:** 2026-06-02 → 2026-06-16  
**Goal:** P0 cyber-comms order log, C2 indicators, engage guard, logistics fuel MVP line.

## Done (headless + Unity bindings)

- [x] `OrderLogEntryKind.CommsStateChange` + `CommsStateChangeRecord`
- [x] `CommsTimelineSimulator` + `data/scenarios/baltic-patrol-comms.policy.json`
- [x] `CommsStateProjection` → top bar `COMMS:` label
- [x] `FireAbortReason.CommsDenied` engage guard in `SimulationSession`
- [x] Message log `COMMS` category
- [x] `FuelStateProjection` → unit detail `FUEL:` line (MVP thresholds)
- [x] Tests: comms timeline, projection, Baltic replay, fuel projection

## Remaining

- [ ] Unity QA with `scenarioPolicyId` = `baltic-patrol-comms`
- [ ] Map ghost symbology when Degraded (P1)
- [ ] Scenario-driven fuel from logistics GDD (replace time thresholds)

## Verification

```bash
dotnet test ProjectAegis.sln -v minimal
```