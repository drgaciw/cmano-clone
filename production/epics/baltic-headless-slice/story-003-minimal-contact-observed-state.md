# Story 003: Minimal contact feed for ObservedState

> **Epic**: Baltic Headless Vertical Slice
> **Status**: Complete
> **Layer**: Feature
> **Type**: Integration
> **Estimate**: 6h
> **Last Updated**: 2026-06-01

## Context

**GDD**: `design/gdd/sensor-detection-ew.md` (stub), `engagement-fire-control.md`
**Requirement**: TR-sensor-001 (contact FSM stub)

**Governing ADRs**: ADR-001

**Engine**: .NET 8 + Unity bridge | **Risk**: MEDIUM

## Acceptance Criteria

- [x] `ISimWorldSnapshot` exposes `PrimaryHostileContactId` + `HasFireControlTrackOnPrimaryContact`.
- [x] `ObservedStateBuilder` maps contact + track into `ObservedState`.
- [x] Engage abort `NoFireControlTrack` when snapshot/state reports no track (`PrimeEngageWorld` uses `state.HasFireControlTrack`).
- [x] Play-mode + headless + Unity `SimplePlayModeSimHost` share the contract.

## Test Evidence

- `PlayModeSmokeHarnessTests` + `EngagementOrderLogContractTests` updated for track-present vs absent cases.

## Dependencies

- Stories 001–002 merged; SIM stack on main.