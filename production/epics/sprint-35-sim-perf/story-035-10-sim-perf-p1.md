---
id: S35-10
status: Complete
type: Logic
priority: should-have
graphite_branch: stack/sprint35/sim-perf-p1
estimate_days: 2.5
dependencies:
  - S35-05 merged
owner: team-simulation
sprint: 35
req_trace: TR-log-001; perf-profile P1 hotspots
governing_adrs: ADR-003
sprint_gate: true
---

# Story 035-10 — Sim Perf P1 — DecisionLog and Datalink LINQ

> **Epic:** sprint-35-sim-perf

## Summary

Reduce P1 allocations in `DecisionLog.ChronologicalEntries` / fingerprint path and `DatalinkSidePictureMerger` nested LINQ. Preserve ADR-003 sequence ordering and merge output hashes.

## Acceptance Criteria

- [x] Chronological entry order unchanged on golden fixtures vs S35-05 baseline
- [x] Datalink merge output hash-identical on `baltic-patrol-datalink-comms` + catalog-latency fixtures
- [x] `ReplayGoldenSuiteTests` — **6/6** PASS
- [x] `/replay-verify` PASS on merge
- [x] `dotnet test --filter "DecisionLog|DatalinkSidePicture"` — all PASS
- [x] ZERO touch `DelegationBridge.cs`

## QA Test Cases

```
Test: Order log sequence preserved
  Given: Fixed Baltic replay seed
  When: Export chronological entries before/after refactor
  Then: Byte-identical or documented equivalent per ADR-003

Test: Datalink merger output stable
  Given: datalink-comms isolated fixture
  When: Run merger across N ticks
  Then: Transition hashes match golden
```

## Test Evidence Path

- `src/ProjectAegis.Delegation.Tests/` (DecisionLog tests)
- `src/ProjectAegis.Sim.Tests/Sensors/DatalinkSidePictureMergerTests.cs`

## Out of Scope

- Full incremental fingerprint hasher if risk exceeds sprint (document deferral)
- BalticReplayHarness Concat fix (optional quick win — fold in if trivial)