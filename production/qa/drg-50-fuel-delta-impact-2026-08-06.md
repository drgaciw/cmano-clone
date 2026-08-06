# DRG-50 impact note — fuel Play Mode deltaSeconds (2026-08-06)

## Defect

`DelegationBridge.EmitFuelTransitions` passes hardcoded `1.0` to `FuelTimelineTracker.Drain`. `SimplePlayModeSimHost` advances `1/60` s per `Tick`, so fuel over-drains ~60× when a burn-model scenario runs in Play Mode.

## Authority

- ADR-020 Decision 1: `deltaSeconds` must be derived from real elapsed sim time.
- Linear **DRG-50** (High, live defect).
- CRITICAL hub: `DelegationBridge` — change is **localized to `EmitFuelTransitions` only** (no `Tick` body reorder, no order dispatch changes).
- Playbook: `production/agentic/critical-hub-merge-playbook-2026-07-14.md`
- Boundary: `production/release-continuity-scope-boundary-2026-07-14.md` (ZERO hotpath is hard; this is a **separate, impact-analyzed exception**)

## Proposed fix

See `production/qa/patches/drg-50-fuel-playmode-delta.patch` and implementation on **PR #407** (`copilot/fixdrg-50-fuel-playmode-delta`):

1. Track `_lastFuelSimTime`.
2. `deltaSeconds = snapshot.SimTime - previous` when a prior sample exists; otherwise measure from simulation epoch (`snapshot.SimTime`).
3. **Always** advance `_lastFuelSimTime` before the empty-registry guard (pause/rewind and empty ticks included) so a unit registered at `t = N` is never retro-charged for `[0, N]`.
4. Skip non-positive deltas; then `Drain(..., deltaSeconds, unitIds)`.

ReplayGolden / BalticReplayHarness path unchanged: units are registered before the tick loop, so the first interval is still 1.0 s and hash `17144800277401907079` is preserved.

## Required verification (before promote to `main`)

- [x] Source apply on PR #407 (`DelegationBridge.cs` + `DelegationBridgeFuelDeltaTests`)
- [ ] `dotnet test` full solution — hold suite floor **≥1638 / 0 failed** (AGENTS.md monotonic)
- [ ] ReplayGolden **6/6**
- [ ] PlayModeSmokeHarnessTests **≥20/20**
- [ ] `DelegationBridgeFuelDeltaTests` **4/4** (sub-second negative + positive cadence pin + 1.0 s JOKER + late-registration)
- [ ] Diff review: only `EmitFuelTransitions` + field + tests

## Status

Reference artifact on this branch (`fix/drg-50-fuel-playmode-delta`). Source + regressions land via **#407**. Full-suite floor reconciliation required before promotion to `main`.
