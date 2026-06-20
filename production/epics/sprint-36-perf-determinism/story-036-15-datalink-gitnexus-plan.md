---
id: S36-15
status: Ready
type: Integration
priority: should-have
graphite_branch: stack/s36/perf-determinism-datalink-plan
estimate_days: 1
dependencies:
  - S35-10 (Datalink P1)
  - S36-01 + S36-05
owner: team-simulation
sprint: 36
req_trace: TR-sensor-004; TR-sensor-002; TR-log-001
governing_adrs: ADR-001 (Sim/Delegation boundary), ADR-004
sprint_gate: true
---

# Story 036-15 — Datalink Merger GitNexus Planning + Re-audit (Planning Only)

> **Epic:** sprint-36-perf-determinism

## Summary

**CRITICAL: Planning + GitNexus only. NO code changes.**

Re-audit `DatalinkSidePictureMerger` (and related `DatalinkShareLagResolver`, `DatalinkCommsShareState`) for determinism after S35 P1. Capture full GitNexus context/impact reports for Datalink symbols + consumers (BalticReplayHarness, projections, tests). Document sort order contracts, share lag application, and hash contribution to world/fingerprint. Note blast radius; plan any future polish. Enforce that any implied verify uses replay-verify. ZERO Delegation touch. Hash immutable.

## Acceptance Criteria

- [ ] GitNexus reports generated for: DatalinkSidePictureMerger, DatalinkShareLagResolver, related symbols + callers (DecisionLog integration, harness)
- [ ] Determinism re-audit section for datalink paths (sort by observer/sensor/target, pending share ordering, side grouping)
- [ ] All share transitions emit in documented deterministic order (matches sensor-detection-ew GDD + ADR-004)
- [ ] ReplayGolden datalink fixtures (baltic-patrol-comms, catalog-latency, datalink-lag)  pass 6/6 + hash match
- [ ] `/replay-verify` PASS (covers datalink scenarios)
- [ ] Documented: "No code edits this story — GitNexus planning + audit artifact only"
- [ ] ZERO touch `DelegationBridge.cs` (confirmed by grep + GitNexus)
- [ ] Impact notes: high callers in tests/harness; future changes require full replay gate + re-GitNexus

## QA Test Cases

```
Test: Datalink GitNexus + determinism re-audit
  Given: S35 P1 datalink baseline
  When: GitNexus context + detect (clean); manual code scan of merger paths
  Then: Report written; sort contracts + lag application verified deterministic

Test: Datalink golden coverage
  Given: baltic-patrol-datalink-* fixtures
  When: Harness replay
  Then: Shared contacts, lag, classify propagation match golden hashes exactly

Manual check: Blast radius doc
  Setup: Review GitNexus incoming for DatalinkSidePictureMerger
  Verify: BalticReplayHarness + 15+ tests + projections listed
  Pass condition: Future change plan cites full gate
```

## Test Evidence Path

- `src/ProjectAegis.Sim/Sensors/DatalinkSidePictureMerger.cs`
- `src/ProjectAegis.Sim/Scenario/DatalinkShareLagResolver.cs`
- `src/ProjectAegis.Sim/Scenario/DatalinkCommsShareState.cs`
- `src/ProjectAegis.Sim.Tests/Sensors/DatalinkSidePictureMergerTests.cs`
- `src/ProjectAegis.Delegation.UnityAdapter.Tests/Baltic/BalticReplayHarnessDatalink*.Tests.cs`
- GitNexus output artifacts (context + any impact) saved to story or determinism/
- `production/determinism/` append

## Out of Scope

- Any code edits to datalink paths (planning only)
- New lag or catalog features (S34 follow)
- Perf hot path optimization (S36-20)
- Delegation changes

**Critical:** GitNexus on Datalink — planning only. Replay-verify required. Hash immutable. ZERO Delegation.
