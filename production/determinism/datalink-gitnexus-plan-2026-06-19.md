# Datalink GitNexus Planning + Determinism Re-audit (S36-15)

**Date:** 2026-06-19
**Story:** S36-15
**Type:** Planning + GitNexus only. **NO CODE CHANGES.**
**Enforcement:** ZERO DelegationBridge; replay-verify gate; hash immutable; Polish Phase 1.

## GitNexus Context (from tool context() + cypher equivalents)

**Primary symbol:**
- Class: `src/ProjectAegis.Sim/Sensors/DatalinkSidePictureMerger.cs:DatalinkSidePictureMerger`

**Incoming CALLS (callers):**
- `src/ProjectAegis.Delegation.UnityAdapter/Baltic/BalticReplayHarness.cs:BalticReplayHarness.Run`
- 20+ tests in `src/ProjectAegis.Sim.Tests/Sensors/DatalinkSidePictureMergerTests.cs` (Peer_on_same_side..., Organic_only..., Observers_on_different..., Classify_promotion..., Duplicate_share..., Shared_transitions_sort_by_..., Share_lag_*, Degraded_*, Denied_*, etc.)

**Outgoing (methods):**
- Merge, ApplyOrganicTransitions, QueueShareableOrganic, FlushShareableOrganic, EmitSharedTransitions, ResolveBestOrganicState, CollectTargetsForSide, BuildObserversBySideSorted, Compare* (pending + shared), plus helpers for keying/rank.

**Related symbols:**
- `DatalinkShareLagResolver.cs`, `DatalinkCommsShareState.cs`
- Integration: DecisionLog (via harness appends of shared transitions), OrderLog fingerprints, SimWorldHash (indirect via detection subhash?).

**Impact notes (planning):**
- High callers: BalticReplayHarness (core replay gate), 15+ dedicated datalink tests, harness variants (DatalinkCatalogLatency, DatalinkComms, DatalinkLag).
- CRITICAL for golden stability: any change to sort keys (observer/sensor/target), pending queue ordering, or side grouping would break ReplayGolden datalink fixtures + world/fp hashes.
- Blast radius: tests + harness + (via DecisionLog) scoring/projections. Future edit requires full re-GitNexus + replay-verify + golden re-pin gate.

GitNexus detect_changes on worktree showed only doc/epic changes (low risk).

## Determinism Re-audit (Datalink paths)

From source scan (DatalinkSidePictureMerger.cs):
- Deterministic structures: Dicts keyed with StringComparer.Ordinal; pending lists sorted explicitly by ComparePendingShareable (tick, ids Ordinal, state rank).
- Emit order: sidesOrdered (built sorted), targets sorted Ordinal + dedup, shared.Sort(CompareSharedTransitions) by observer/sensor/target.
- Share lag: _pendingShareable append in arrival, Flush selects ready + Sort, remove in order. No dict enum for output.
- Side grouping: BuildObserversBySideSorted sorts unitIds, sides, observers.
- Matches GDD sensor-detection-ew TR-sensor-004 (side picture sharing) + TR-sensor-002 (sorted pairs) + ADR-004 (tick pipeline order).
- No wall time, unseeded rng, or order-dependent accum.
- Hash contribution: shared ContactTransitions appended to DecisionLog -> affects fingerprint + (via harness) world hash indirectly stable.

**All share transitions emit in documented deterministic order** — confirmed.

## Datalink Golden Coverage

- Fixtures: replay-golden-baltic-datalink-catalog-latency-2026-06-19.txt , baltic-datalink-comms-2026-06-19.txt , lag tests.
- Harness datalink tests + ReplayGolden (via catalog/comms cases) PASS per prior reports (A==B==golden).
- `/replay-verify` covers datalink scenarios: PASS (documented in replay-*.md).

## Verification Artifacts

- GitNexus context() output captured above.
- Code scan: deterministic (see earlier Datalink audit in S36-01 report).
- ZERO DelegationBridge: confirmed grep + GitNexus (no path).
- "No code edits this story — GitNexus planning + audit artifact only" — enforced.

## Future Change Plan

Any edit to Datalink* must:
1. GitNexus context/impact pre-edit (high risk).
2. Full replay-verify + 6/6 golden on datalink fixtures.
3. Hash immutable re-assert.
4. Re-index GitNexus post.

## References

- Story: story-036-15-datalink-gitnexus-plan.md
- Prior: determinism-audit-2026-06-19.md , replay-2026-06-*.md
- EPIC: sprint-36-perf-determinism/EPIC.md
- GDD/ADR/TR: as in story header
- GitNexus: context on DatalinkSidePictureMerger

**End of planning artifact. Ready for S36-20.**
