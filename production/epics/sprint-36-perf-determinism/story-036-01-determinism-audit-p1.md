---
id: S36-01
status: Ready
type: Logic
priority: must-have
Last Updated: 2026-06-20
graphite_branch: stack/s36/perf-determinism-audit-p1
estimate_days: 2
dependencies:
  - S35-05 + S35-10 merged (perf baseline)
  - determinism-audit-2026-05-29.md findings
owner: team-simulation
sprint: 36
req_trace: TR-simcore-005; TR-sensor-002; ARCH-NFR-1 (determinism); perf-profile P1
governing_adrs: ADR-001 (boundary), ADR-004 (tick pipeline), ADR-005 (sim core)
sprint_gate: true
---

# Story 036-01 — Determinism Audit P1 Follow-up

> **Epic:** sprint-36-perf-determinism

## Summary

Extend S35 determinism baseline and prior audit (DET-001 fixed). Scan hot paths post-S35 perf changes for new non-determinism: unordered iteration in DecisionLog chronological / fingerprint, Datalink merger side lists, world-hash accumulation, RNG domain reuse. Produce updated `production/determinism/determinism-audit-2026-06-19.md`. **No code changes** except doc + test assertions if gaps found. Enforce hash immutable, replay-verify. **ZERO** touch `DelegationBridge.cs`.

GitNexus context (planning only): DecisionLog + DatalinkSidePictureMerger + BalticReplayHarness are CRITICAL (high caller count in tests + harness; see GitNexus reports). No edits.

## Acceptance Criteria

- [ ] Updated determinism audit report covers post-S35 P0/P1 paths (DecisionLog.ChronologicalEntries, DatalinkSidePictureMerger merge ordering, SimWorldHash combine)
- [ ] Any new findings classified (CRITICAL/HIGH/MEDIUM/LOW); remediation plan or explicit "no change needed, hash immutable"
- [ ] All production golden hashes (WORLD_HASH, DETECTION_WORLD_HASH, order log fingerprints) documented as immutable under fixed seed
- [ ] `ReplayGoldenSuiteTests` — **6/6** PASS (no drift introduced by audit)
- [ ] `/replay-verify` (or equivalent harness double-run) PASS; A vs B match; A vs golden match
- [ ] `dotnet test --filter "DecisionLog|Datalink|ReplayGolden|WorldHash"` — all PASS
- [ ] ZERO touch `DelegationBridge.cs` or any Delegation assembly hot path
- [ ] GitNexus planning notes appended (no code impact analysis triggers)
- [ ] Hash immutable contract cited: same seed + scenario → byte-identical world hash + fingerprint across runs/machines

## QA Test Cases

```
Test: Determinism audit P1 re-scan
  Given: S35-05/S35-10 merged baseline + current worktree
  When: Run determinism-audit scan + replay double-run on baltic-patrol* fixtures
  Then: No new CRITICAL; hashes match golden; report written with GitNexus citations

Test: World hash + fingerprint immutability
  Given: Fixed seed 42 on baltic-patrol
  When: Run harness twice in fresh processes
  Then: WORLD_HASH, DETECTION_WORLD_HASH, FINGERPRINT_SHA256 identical across runs and vs golden

Test: No Delegation path touched
  Given: Audit + any follow-on test asserts
  When: Grep for edits touching DelegationBridge
  Then: Zero matches
```

## Test Evidence Path

- `production/determinism/determinism-audit-2026-06-19.md` (new or appended P1 section)
- `src/ProjectAegis.Delegation.Tests/Decision/DecisionLogTests.cs` + OrderLog*Tests (re-assert stable chrono)
- `src/ProjectAegis.Sim.Tests/Sensors/DatalinkSidePictureMergerTests.cs`
- `src/ProjectAegis.Delegation.UnityAdapter.Tests/Baltic/BalticReplayHarnessWorldHashTests.cs`
- `tests/regression/replay-golden-*.txt` (unchanged)
- GitNexus outputs (context/impact on DecisionLog, DatalinkSidePictureMerger, BalticReplayHarness) logged in audit

## Out of Scope

- Remediation code changes (next sprint if CRITICAL found)
- DOTS/ECS
- Unity frame profiling (see S35-04)
- Changes to catalog or policy data
- Any edit to `DelegationBridge.cs` or `ProjectAegis.Delegation` orchestration hot paths (enforced)

## GitNexus Planning Notes (CRITICAL — planning only)

- DecisionLog: 30+ callers in tests + projections + orchestrator; fingerprint stability is merge gate.
- DatalinkSidePictureMerger: Used by BalticReplayHarness.Run; 20+ test assertions on sort order + share transitions.
- BalticReplayHarness: Central to 20+ ReplayGolden* tests; any reorder here breaks golden suite.
- Plan: Only doc + verify; re-index GitNexus post-S36 if symbols change (future).

**Replay-verify implied:** All ACs gate on `/replay-verify` PASS. Hash immutable.
