---
id: S33-04
status: Complete
last_updated: 2026-06-19
completed: 2026-06-19
type: Logic
priority: must-have
graphite_branch: stack/sprint33/datalink-share-gate
estimate_days: 1.5
dependencies:
  - S33-01 green baseline
owner: team-simulation
sprint: 33
req_trace: Req 15, Req 19; TR-sensor-004, TR-cyber-001
sprint_gate: true
---

# Story 033-04 — Datalink Comms Share Gate

> **Epic:** sprint-33-cyber-comms-datalink

## Summary

Extend `DatalinkSidePictureMerger.Merge` to accept `CommsState`: Nominal unchanged; Degraded suppresses new peer shares; Denied suppresses all share emit. Wire via `BalticReplayHarness` reading `bridge.CurrentCommsState` — no `DelegationBridge.cs` edits.

## Acceptance Criteria

- [x] `DatalinkSidePictureMergerTests` cover Nominal/Degraded/Denied
- [x] Default `CommsState.Nominal` preserves S30-10 behavior; ReplayGolden 6/6 unchanged
- [x] Production Baltic hash `17144800277401907079` unchanged
- [x] ZERO touch `DelegationBridge.cs`

## Verify Commands

```bash
dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj \
  --filter "Datalink|Comms" -v minimal
/replay-verify
npx gitnexus impact DatalinkSidePictureMerger
```