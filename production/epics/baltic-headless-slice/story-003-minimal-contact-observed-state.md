# Story 003: Minimal contact feed for ObservedState

> **Epic**: Baltic Headless Vertical Slice
> **Status**: Ready
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

- [ ] `ISimWorldSnapshot` exposes at least one hostile contact id for engage tests (or documented stub adapter).
- [ ] `ObservedStateBuilder` maps contact count / alive flags from snapshot without hardcoded empty dictionaries in harness.
- [ ] Engage abort `NoFireControlTrack` only when snapshot reports no track (not always-on stub).
- [ ] Play-mode + headless harness use same snapshot contract.

## Test Evidence

- `PlayModeSmokeHarnessTests` + `EngagementOrderLogContractTests` updated for track-present vs absent cases.

## Dependencies

- Stories 001–002 merged; SIM stack on main.