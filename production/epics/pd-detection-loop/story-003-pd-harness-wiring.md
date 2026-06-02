# Story 003: Pd-driven contacts in Baltic harness

> **Epic**: Pd Detection Loop  
> **Status**: Complete (PR 1)  
> **Type**: Integration  
> **TR**: TR-sensor-001/002

## Acceptance Criteria

- [x] Harness prefers `PdDetectionContactSimulator` when profile has detection trials.
- [x] `baltic-patrol` with `detection[]` emits `ContactChange` + stable fingerprint.
- [x] EMCON Off still suppresses active-radar trials.

## Test Evidence

- `src/ProjectAegis.Delegation.UnityAdapter.Tests/Baltic/BalticReplayHarnessPdDetectionTests.cs`