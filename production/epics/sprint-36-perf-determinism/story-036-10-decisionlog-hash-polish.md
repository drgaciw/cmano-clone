---
id: S36-10
status: Ready
type: Logic
priority: should-have
graphite_branch: stack/s36/perf-determinism-decisionlog
estimate_days: 2
dependencies:
  - S35-10 (DecisionLog P1) merged
  - S36-01 audit
owner: team-simulation
sprint: 36
req_trace: TR-log-001; TR-log-003; perf-profile P1 hotspots; TR-simcore-005
governing_adrs: ADR-003 (order log schema + fingerprint), ADR-004
sprint_gate: true
---

# Story 036-10 — DecisionLog Immutable Hash + P1 Polish

> **Epic:** sprint-36-perf-determinism

## Summary

Polish DecisionLog post S35-10: ensure ChronologicalEntries, fingerprint SHA, and append paths remain stable and allocation-light. Add/strengthen asserts for hash immutability under replay. Follow GitNexus planning (CRITICAL symbol) — no structural changes. Preserve exact ADR-003 ordering. Enforce replay-verify and ZERO Delegation.

## Acceptance Criteria

- [ ] Chronological entry order + fingerprint byte-identical vs S35-10 baseline on golden fixtures
- [ ] `OrderLogReplayFingerprintSha256Tests` + `ReplayOrderLogFingerprintTests` PASS; SHA stable for same log
- [ ] DecisionLog appends (policy, engage, contact, magazine, etc.) produce identical fingerprint across runs
- [ ] No per-tick allocations or LINQ OrderBy in hot fingerprint/chrono path (P1 budget)
- [ ] `ReplayGoldenSuiteTests` 6/6 + `/replay-verify` PASS
- [ ] `dotnet test --filter "DecisionLog|OrderLog|ReplayOrder"` — all PASS
- [ ] World hash / detection subhash unchanged (hash immutable)
- [ ] ZERO touch `DelegationBridge.cs`
- [ ] GitNexus context captured for DecisionLog (planning); any symbols touched noted for post-merge re-index

## QA Test Cases

```
Test: DecisionLog chrono + fingerprint stability
  Given: baltic-patrol replay golden fixture
  When: Run harness A vs B vs S35 baseline log export
  Then: ChronologicalEntries sequence identical; SHA256 hex identical

Test: Append paths hash immutable
  Given: Multiple record types appended in fixed order (player, policy, engage, contact)
  When: Compute fingerprint twice
  Then: Identical output; no variance from dict/enum iteration

Test: P1 allocation profile
  Given: DecisionLog hot path under 300-tick replay
  When: Measure allocations in Release
  Then: <= S35-10 baseline (document delta)
```

## Test Evidence Path

- `src/ProjectAegis.Delegation/Decision/DecisionLog.cs`
- `src/ProjectAegis.Delegation.Tests/Decision/DecisionLogTests.cs`
- `src/ProjectAegis.Delegation.Tests/Decision/OrderLogReplayFingerprintSha256Tests.cs`
- `src/ProjectAegis.Delegation.Tests/Decision/OrderLogFingerprintTests.cs`
- `src/ProjectAegis.Delegation.Tests/Decision/ReplayOrderLogFingerprintTests.cs`
- `tests/regression/replay-golden-baltic-*.txt`
- GitNexus report for DecisionLog

## Out of Scope

- New log entry types
- Full incremental hasher (if deferred)
- Changes to Datalink or sensors (other stories)
- Delegation side effects

**Replay-verify implied:** Required. Hash immutable enforced.
