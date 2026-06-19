---
id: S34-07
status: Complete
Last Updated: 2026-06-19
type: Integration
priority: should-have
graphite_branch: stack/sprint34/datalink-catalog-latency-fixture
estimate_days: 1
dependencies:
  - S34-04 merged
owner: team-simulation
sprint: 34
req_trace: TR-sensor-004; Req 15
---

# Story 034-07 — Datalink Catalog-Latency Isolated Fixture

> **Epic:** sprint-34-datalink-latency

## Summary

Add `baltic-patrol-datalink-catalog-latency` fixture proving catalog `LatencyMsNominal` → deferred peer share. Pinned isolated golden; **not** in ReplayGolden 6/6 catalog.

## Acceptance Criteria

- [x] `/replay-verify` PASS on isolated fixture
- [x] Production Baltic hash `17144800277401907079` unchanged
- [x] ReplayGolden 6/6 on default path