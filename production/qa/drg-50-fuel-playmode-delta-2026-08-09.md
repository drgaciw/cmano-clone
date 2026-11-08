# DRG-50 — Fuel Play Mode deltaSeconds (land 2026-08-09)

## Defect

`DelegationBridge.EmitFuelTransitions` passed hardcoded `1.0` to `FuelTimelineTracker.Drain`.
`SimplePlayModeSimHost` advances `1/60` s per `Tick`, so fuel over-drains ~60× when a burn-model
scenario runs in Play Mode.

## Fix (this PR)

| Item | Detail |
|------|--------|
| File | `src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs` |
| Method | `EmitFuelTransitions` only |
| Field | `private double? _lastFuelSimTime` |
| Tests | `DelegationBridgeFuelDeltaTests` (4) |
| Tick() reorder | **None** |

1. Track `_lastFuelSimTime`.
2. `deltaSeconds = snapshot.SimTime - previous` when prior sample exists; else measure from epoch (`snapshot.SimTime`).
3. **Always** advance `_lastFuelSimTime` before empty-registry / non-positive guards so late registration is not retro-charged.
4. Skip non-positive deltas; then `Drain(..., deltaSeconds, unitIds)`.

## Authority

- ADR-020 Decision 1: `deltaSeconds` must be derived from real elapsed sim time.
- Linear **DRG-50** (High, live defect).
- CRITICAL hub: `DelegationBridge` — localized to fuel emission helper only.
- Playbook: `production/agentic/critical-hub-merge-playbook-2026-07-14.md`

## Verification

- [x] `DelegationBridgeFuelDeltaTests` 4/4 (sub-second negative + positive cadence pin + 1.0 s JOKER + late-registration)
- [x] ReplayGolden 6/6
- [x] PlayModeSmokeHarnessTests: engine-side harness OK; Unity asset path tests N/A in sparse checkout (CI has full tree)
- [ ] Full suite floor — CI Build and test gate (local sparse checkout lacks unity assets)

## Supersedes

Stale open PRs documenting the same fix without a clean rebase:

- #406 (docs/patch only, BEHIND)
- #407 (copilot source, CONFLICTING)
