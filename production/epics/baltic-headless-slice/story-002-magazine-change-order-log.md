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

- [ ] `OrderLogEntryKind.MagazineChange` exists with payload `{shooterId, mountId, delta, reason}`.
- [ ] `MvpEngagementResolver` / `MagazineLedger.TryConsume` appends row on successful fire.
- [ ] Row appears in `DecisionLog.ChronologicalEntries()` with deterministic ordering (simTick, sequenceId).
- [ ] `ComputeFingerprint()` includes MagazineChange rows.
- [ ] Regression test: two fires → two MagazineChange entries with delta -1.

## Test Evidence

- `src/ProjectAegis.Delegation.Tests/Decision/` or `ProjectAegis.Sim.Tests/Engage/`

## Dependencies

- Story 001 optional (harness can assert fingerprint includes magazine rows later).