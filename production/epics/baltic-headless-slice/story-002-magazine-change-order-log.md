# Story 002: MagazineChange order-log rows

> **Epic**: Baltic Headless Vertical Slice
> **Status**: Complete
> **Layer**: Core
> **Type**: Logic
> **Estimate**: 4h
> **Last Updated**: 2026-06-01

## Context

**GDD**: `design/gdd/logistics-magazines.md`
**Requirement**: TR-log-002 — schema variants; logistics AC-2

**Governing ADRs**: ADR-003

**Engine**: .NET 8 | **Risk**: LOW

## Acceptance Criteria

- [x] `OrderLogEntryKind.MagazineChange` exists with payload `{shooterId, mountId, delta, reason}`.
- [x] `SimulationSession` appends row on successful launch (`MagazineLedger.TryConsume`).
- [x] Row appears in `DecisionLog.ChronologicalEntries()` with deterministic ordering (simTick, sequenceId).
- [x] `ComputeFingerprint()` includes MagazineChange rows.
- [x] Regression test: two fires → two MagazineChange entries with delta -1.

## Test Evidence

- `src/ProjectAegis.Delegation.Tests/Decision/` or `ProjectAegis.Sim.Tests/Engage/`

## Dependencies

- Story 001 optional (harness can assert fingerprint includes magazine rows later).