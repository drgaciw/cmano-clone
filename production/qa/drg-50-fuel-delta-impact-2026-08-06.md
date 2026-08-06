# DRG-50 impact note — fuel Play Mode deltaSeconds (2026-08-06)

## Defect

`DelegationBridge.EmitFuelTransitions` passes hardcoded `1.0` to `FuelTimelineTracker.Drain`. `SimplePlayModeSimHost` advances `1/60` s per `Tick`, so fuel over-drains ~60× when a burn-model scenario runs in Play Mode.

## Authority

- ADR-020 Decision 1: `deltaSeconds` must be derived from real elapsed sim time.
- Linear **DRG-50** (High, live defect).
- CRITICAL hub: `DelegationBridge` — change is **localized to `EmitFuelTransitions` only** (no `Tick` body reorder, no order dispatch changes).

## Proposed fix

See `production/qa/patches/drg-50-fuel-playmode-delta.patch`:

1. Track `_lastFuelSimTime`.
2. `deltaSeconds = snapshot.SimTime - previous` (skip non-positive).
3. First tick: use `SimTime` when `< 1.0` (Play Mode), else `1.0` (harness).

## Required verification (before merge)

- [ ] Apply patch to `DelegationBridge.cs` on branch `fix/drg-50-fuel-playmode-delta`
- [ ] `dotnet test` full solution — hold suite floor / 0f
- [ ] ReplayGolden **6/6** (harness still 1 s/call)
- [ ] New regression: drive bridge at 1/60 cadence with fuel-burn scenario; assert consumed fuel tracks elapsed sim time, not call count
- [ ] Diff review: only `EmitFuelTransitions` + field; ZERO other Tick path edits

## Status

Patch authored 2026-08-06; source apply + suite run pending agent/host with .NET SDK.
