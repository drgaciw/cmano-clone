# Order Log & Replay

> **Status:** In Progress (C1 typed payload landed 2026-06-02)  
> **Last Updated:** 2026-06-02
> **Implements Pillar:** Determinism, Research reproducibility  
> **Requirements:** [17-Replay-AAR-And-Order-Log.md](../../Game-Requirements/requirements/17-Replay-AAR-And-Order-Log.md)  
> **Architecture:** [ADR-003](../../docs/architecture/adr-003-order-log-schema.md), [architecture.md](../../docs/architecture/architecture.md)

> **Quick reference** — Layer: **Foundation** · Priority: **MVP** · Key deps: Simulation Core (#1) · Depended on by: Policy denials, UI message log, AAR agents

## Summary

The **order log** is the single source of truth for what happened in a scenario. Replay, message log, losses, scoring, and AAR agents are **projections** of this log—not parallel histories.

## Player Fantasy

After the fight, scrub time like a DVR: every shot, denial, and agent decision is clickable evidence—not a cinematic guess.

## Overview

| Concern | Owner | Notes |
|---------|-------|-------|
| Append API | `IOrderLog` (evolve `DecisionLog`) | Delegation + Sim append |
| Agent decisions | `AgentDecision` variant | Maps from `DecisionRecord` |
| World events | Sim subsystems | Engagement, contact, policy |
| Playback | Unity + headless | Same schema |
| Golden gate | `ReplayGoldenTests` + `/replay-verify` | Hash full log |

## Migration: DecisionRecord → OrderLogEntry

**Design-review blocker C1 resolution:**

| Today (`DecisionRecord`) | Future (`AgentDecision` payload) |
|--------------------------|----------------------------------|
| `SimTime` | `simTick` + `simTime` |
| `AgentId` | `agentId` |
| `TargetId` | `targetId` |
| `AutonomyLevel` | `autonomyLevel` |
| `ChosenKind` | `chosenOrderKind` |
| `Alternatives` | `scoredIntents[]` |
| `Rationale` | `rationale` |
| `AttentionLoad/Budget` | `attentionLoad`, `attentionBudget` |
| `RngDraw` | `rngDraw` (replay fingerprint) |

**Rule:** `DecisionLog.Append(DecisionRecord)` becomes `IOrderLog.Append(OrderLogEntry)` with factory `OrderLogEntry.FromDecisionRecord(record, simTick, sequenceId)`.

## Entry Types (discriminated union)

```csharp
enum OrderLogEntryType {
  PlayerOrder, AgentDecision, PolicyDenial, Engagement,
  ContactChange, MissionTransition, EventFired, PolicyUpdate, ModeChange
}
```

### Common header (all entries)

| Field | Type | Rule |
|-------|------|------|
| `simTick` | `ulong` | Monotonic per run |
| `sequenceId` | `ulong` | Global total order |
| `simTime` | `double` | Scenario clock |
| `scenarioSeed` | `ulong` | From sim core |
| `type` | enum | Discriminator |
| `payload` | typed struct | Per variant |

### PolicyDenial (links policy GDD)

| Field | Source |
|-------|--------|
| `policySnapshotId` | TR-policy-003 |
| `fireAbortReason` | TR-policy-005 |
| `unitId`, `weaponId` | Engage attempt |

## Detailed Rules

1. **Ordering:** Compare `(simTick, sequenceId)`; never reorder on load.
2. **Single writer per tick:** Sim tick end flushes batch; delegation appends during step 6–7 of pipeline.
3. **Message log:** UI subscribes to filtered stream; filters by `type`, side, unit—no duplicate storage.
4. **Checkpoints:** Optional world-state snapshot every N ticks or on `Engagement`; replay re-sim from nearest checkpoint + tail log.
5. **Headless:** Identical JSON/binary schema; no UI-only fields.

## Formulas

**Replay fingerprint (MVP):**

```
fingerprint = SHA256( concat( entry.CanonicalBytes() for entry in log ordered by sequenceId ) )
```

Extend `DeterministicHash` usage in `ReplayGoldenTests` when new types land.

## Edge Cases

| Case | Behavior |
|------|----------|
| Log append during replay | Forbidden — read-only playback mode |
| Duplicate `sequenceId` | Assert fail in debug; reject in release |
| Empty log at scenario end | WARN; scoring still runs with zero events |
| Agent with no decisions | No `AgentDecision` rows; valid |
| Policy denial without engage | `PolicyDenial` only; no `Engagement` |
| Checkpoint missing mid-replay | Fall back to full re-sim from seed + full log (slower) |

## Dependencies

| System | Direction |
|--------|-----------|
| Simulation Core | Upstream — tick, seed |
| Policy | Produces `PolicyDenial`, `PolicyUpdate` |
| Engagement | Produces `Engagement` |
| Sensors | Produces `ContactChange` |
| C2 UI / Message log | Downstream projection |
| Scenario & Mission Editor | Downstream reader — quick-run/AAR summaries + event debugger |

## Tuning Knobs

| Knob | Default | Range |
|------|---------|-------|
| `checkpointIntervalTicks` | 300 | 60–3600 |
| `logCompression` | on | off for debug |
| `maxLogSizeMb` | 500 | scenario setting |

## Acceptance Criteria

Maps to req 17 § Acceptance Criteria:

1. Headless and interactive runs yield **identical** `fingerprint`.
2. Scrub to `Engagement` shows correct map state (checkpoint + events).
3. `PolicyDenial` `sequenceId` matches message log row.
4. AAR agent cites only existing `sequenceId`s (validated in tests).
5. Batch CSV from headless includes score/losses columns.
6. `/replay-verify` PASS on golden scenario.

## GitNexus (2026-05-29)

| Symbol | Risk | Action before edit |
|--------|------|-------------------|
| `DecisionLog` | **LOW** | `npx gitnexus impact --repo cmano-clone -d upstream DecisionLog` |
| `ReplayGoldenTests` | indirect via orchestrator | Update golden files when schema changes |

## TR IDs

| ID | Requirement |
|----|-------------|
| TR-log-001 | Append-only ordered log |
| TR-log-002 | Entry type union |
| TR-log-003 | Replay fingerprint + golden CI |

## Open Questions

1. Checkpoint interval: time vs engagement-triggered (req 17).
2. Binary vs JSON on disk for research exports.
3. Hash chain (P1) vs MVP fingerprint only.
