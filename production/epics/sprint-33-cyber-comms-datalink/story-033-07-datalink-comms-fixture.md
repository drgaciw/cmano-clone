---
id: S33-07
status: Complete
last_updated: 2026-06-19
completed: 2026-06-19
type: Integration
priority: should-have
graphite_branch: stack/sprint33/datalink-comms-fixture
estimate_days: 1
dependencies:
  - S33-04
owner: team-simulation
sprint: 33
req_trace: Req 15, Req 19
---

# Story 033-07 — Isolated `baltic-patrol-datalink-comms` Fixture

> **Epic:** sprint-33-cyber-comms-datalink

## Summary

Merge datalink doctrine + comms transitions in isolated fixture; wire harness comms state into merger; pin isolated golden. Not in ReplayGolden 6/6 catalog.

## Acceptance Criteria

- [x] `/replay-verify` PASS on isolated pin (deterministic golden pinned)
- [x] `CommsStateChange` + deferred datalink contacts at expected ticks
- [x] Production Baltic hash unchanged
- [x] Fixture excluded from `ReplayGoldenRegressionCatalog`

## Verify Commands

```bash
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "DatalinkComms|ReplayGoldenSuiteTests" -v minimal
/replay-verify
```