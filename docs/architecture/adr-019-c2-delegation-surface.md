# ADR-019: C2 Delegation Overlay Surface (DelegationBridge add-only)

**Status:** Accepted  
**Date:** 2026-07-09  
**Related:** ADR-010, Phase 2b `TryCancelHumanOrder` pattern

## Context

Req 20 P0 delegation overlays need human/agent/mixed badges, autonomy control, agent pause/resume, intent-preview ghost, and OOB ownership filters. Standing **DelegationBridge zero-diff** blocked these until an explicit surface is approved.

User decision **D2** (2026-07-09): approve the following **add-only** surface. No `Tick` hotpath rewrite.

## Decision

### Read projections (engine-agnostic DTOs)

`DelegationStateProjection` per unit:

| Field | Meaning |
|-------|---------|
| `Owner` | Human \| Agent \| Mixed |
| `AutonomyLevel` | Existing `AutonomyLevel` enum |
| `PersonalityId` | Trait/personality preset id (string; empty if none) |
| `Paused` | Agent decision loop paused for this unit |

### Commands (logged intents via bridge)

| Command | Semantics |
|---------|-----------|
| `AgentPauseRequested` | Pause agent for entity |
| `AgentResumeRequested` | Resume agent for entity |
| `AutonomyLevelChangeRequested` | Set autonomy for entity |

### Bridge rules

1. **Add-only methods** on `DelegationBridge` (same discipline as `TryCancelHumanOrder`).
2. Existing methods remain byte-stable; no signature rewrites.
3. Agent pause **also** pushes/pops a reason on `PauseReasonStack` (T5 owns stack; T4 routes through it).
4. UI binds projections only; never mutates sim tables directly (ADR-010).

## Alternatives

| Option | Rejected because |
|--------|------------------|
| Projections only (no pause/autonomy commands) | Incomplete P0 badge UX |
| Keep zero-diff forever | Blocks req 20 P0 overlays |

## Consequences

- Track **T4** is the sole owner of new bridge methods for this surface.
- Integration gate: `detect_changes` / grep must show **only** D2-approved additions to `DelegationBridge.cs`.
- Baltic hash unchanged (no cancel/pause in golden path).

## GDD / TR

- Req 20 §Delegation Overlays  
- Doc 04 agent delegation framework  
