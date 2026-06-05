# Story 003: Harness feeds ObservedState from contacts

> **Epic**: Sensor Headless Slice
> **Status**: Complete
> **Layer**: Integration
> **Type**: Integration
> **Estimate**: 3h

## Context

**GDD**: sensor AC-5 — engage abort when no track; agents consume ObservedState only

## Acceptance Criteria

- [x] `BalticReplayHarness` runs contact sim before `DelegationBridge.Tick`.
- [x] `HeadlessSnapshot` reads `ScenarioContactSimulator` for count/primary/track.
- [x] Replay fingerprint includes `ContactChange|` for `baltic-patrol`.
- [x] Identical seed/scenario/ticks → identical fingerprint.

## Test Evidence

- `src/ProjectAegis.Delegation.UnityAdapter.Tests/Baltic/BalticReplayHarnessContactTests.cs`