---
id: S34-04
status: Complete
type: Logic
priority: must-have
graphite_branch: stack/sprint34/catalog-datalink-latency
estimate_days: 1.5
dependencies:
  - S34-01 green baseline
  - S34-02 reader API (`TryGetLinkLatencyMs` or equivalent)
owner: team-simulation
sprint: 34
req_trace: Req 15 TR-sensor-004 catalog slice; Req 19
sprint_gate: true
---

# Story 034-04 — Catalog-Derived Datalink Share Lag

> **Epic:** sprint-34-datalink-latency

## Summary

`DatalinkShareLagResolver` maps `link_catalog.latency_ms_nominal` → effective `ShareLagTicks` at harness bind. Scenario `shareLagTicks` override wins. No per-tick catalog reads. **ZERO** `DelegationBridge.cs` edits.

## Acceptance Criteria

- [ ] Missing link → scenario default; catalog hit → computed ticks
- [ ] Explicit `shareLagTicks` overrides catalog
- [ ] Default production Baltic path unchanged
- [ ] `ReplayGoldenSuiteTests` — 6/6 PASS
- [ ] `/replay-verify` PASS on merge