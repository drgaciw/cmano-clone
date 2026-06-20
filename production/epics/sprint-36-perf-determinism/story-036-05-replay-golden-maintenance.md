---
id: S36-05
status: Ready
type: Logic
priority: must-have
graphite_branch: stack/s36/perf-determinism-replay-maint
estimate_days: 1.5
dependencies:
  - S36-01 determinism audit baseline
  - S35 replay goldens stable
owner: team-simulation
sprint: 36
req_trace: TR-log-003; TR-simcore-005; TR-sensor-002
governing_adrs: ADR-003 (order log schema), ADR-004 (tick pipeline)
sprint_gate: true
---

# Story 036-05 — Replay Golden + Harness Maintenance

> **Epic:** sprint-36-perf-determinism

## Summary

Maintain the ReplayGolden 6/6 suite and `BalticReplayHarness` polish: ensure fixture pins, checkpoint logic, and double-run verification are robust post-S35 perf changes. Refresh any isolated regression goldens if needed (without touching production 6/6 hashes). Polish harness for clearer divergence reporting and immutable hash assertions. **ZERO** DelegationBridge. Enforce replay-verify.

GitNexus: Planning snapshot only on harness + DecisionLog integration paths.

## Acceptance Criteria

- [ ] ReplayGoldenSuiteTests — **6/6 PASS** (baltic-patrol, comms, classify, stale, spoof, readiness)
- [ ] `BalticReplayHarnessWorldHashTests` + related world-hash asserts PASS and pin current hashes
- [ ] Harness double-run (A vs B) + A vs golden documented and PASS for all catalog cases
- [ ] Divergence output improved (e.g. tick of first mismatch, sub-hash layer) — no behavior change
- [ ] Production golden files `tests/regression/replay-golden-*.txt` (6/6) **bit-identical** (hash immutable)
- [ ] `/replay-verify` (full suite) PASS on merge
- [ ] Any new regression golden (e.g. for harness polish test) isolated, not polluting 6/6 catalog
- [ ] ZERO touch `DelegationBridge.cs`
- [ ] Harness code changes limited to polish (logging, asserts, error paths) — no reordering of log appends or detection trials

## QA Test Cases

```
Test: ReplayGolden suite maintenance
  Given: S36 baseline after audit
  When: dotnet test --filter "ReplayGoldenSuiteTests" (Release + Debug)
  Then: 6/6 PASS; no hash drift vs committed goldens; elapsed recorded for re-profile

Test: Harness A/B + golden match
  Given: BalticReplayHarness run with seed 42 on baltic-patrol-comms
  When: Execute twice (fresh process) and compare to golden
  Then: WORLD_HASH + FINGERPRINT + contact order + log sequence identical

Test: Isolated regression golden
  Given: New harness polish test case (e.g. lag replay)
  When: Generate replay-golden-*-isolate-*.txt
  Then: Does not affect production 6/6 pins; documented in regression README

Manual check: Divergence reporting
  Setup: Artificially inject reorder in test harness copy
  Verify: Report shows first differing tick + hash component
  Pass condition: Clear actionable output without changing prod paths
```

## Test Evidence Path

- `src/ProjectAegis.Delegation.UnityAdapter.Tests/Baltic/ReplayGoldenSuiteTests.cs`
- `src/ProjectAegis.Delegation.UnityAdapter.Tests/Baltic/BalticReplayHarnessWorldHashTests.cs`
- `src/ProjectAegis.Delegation.UnityAdapter.Tests/Baltic/BalticReplayHarness*.cs` (polish)
- `tests/regression/replay-golden-baltic-*.txt` (6 files, immutable)
- `tests/regression/README.md`
- `production/determinism/replay-2026-06-19.md` (or append)
- GitNexus: harness + DecisionLog context captured in story evidence

## Out of Scope

- New scenarios in production 6/6 catalog (use isolated)
- Perf optimizations (S36-20)
- Datalink logic changes (S36-15 planning)
- Unity C2 / Editor harness
- Any DelegationBridge mutation

**Enforcement:** Replay-verify any implied; ZERO Delegation; hash immutable (explicit ACs).
