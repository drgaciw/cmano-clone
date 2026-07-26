# Mission Runtime

> **Status:** Documented as-built (2026-07-24) — **headless / CI-scoped, not player-facing**
> **Last Updated:** 2026-07-24
> **Implements Pillar:** Determinism, Scenario authoring fidelity
> **Requirements:** [02-Core-Gameplay-Loop.md](../../Game-Requirements/requirements/02-Core-Gameplay-Loop.md), [07-Agentic-Infrastructure.md](../../Game-Requirements/requirements/07-Agentic-Infrastructure.md), [11-Agentic-Mission-Editor.md](../../Game-Requirements/requirements/11-Agentic-Mission-Editor.md)
> **Architecture:** [ADR-021](../../docs/architecture/adr-021-mission-timeline-runtime.md), [ADR-003](../../docs/architecture/adr-003-order-log-schema.md), [ADR-004](../../docs/architecture/adr-004-tick-pipeline-order.md)
> **Engineering reference:** [mission-timeline-runtime.md](../../docs/engineering/mission-timeline-runtime.md)

> **Quick reference** — Layer: **Gameplay** · Priority: **MVP (shipped)** · Systems index: **#9** · Key deps: Sensor & Contact Model, Policy/ROE, Order Log · Depended on by: nothing player-facing today

## Summary

The **mission runtime** executes scenario-authored mission structure during a run: a **scripted timeline** that fires events at tick boundaries in a locked order, and **contact triggers** that escalate ROE when a designated observer first detects a matching contact. Both emit rows into the order log and therefore into the replay fingerprint.

This GDD documents behaviour that **already ships**. It was written retroactively from the code, because the system had no GDD and no ADR — the traceability index listed system #9 as "Needs GDD + ADR".

## Player Fantasy

**None today — and that is the deliberate scope.**

The mission runtime does **not execute during interactive play**. `SimTickPipeline` has no mission step, and the Unity interactive path (`DelegationBridgeHost.RunTick`) never calls `MissionRuntime.Tick` or `MissionContactTriggerRuntime.Evaluate` — it only projects a read-only mission list for display. Mission execution happens exclusively inside `BalticReplayHarness.RunCore`, the headless CLI / replay-verify / gauntlet runner.

So the system currently serves **scenario validation and CI**, not players. Its "fantasy" is an authoring-side one: a designer writes a mission timeline and can trust it will fire in exactly the authored order, reproducibly, every run.

> ⚠️ `architecture.md`'s tick-pipeline table lists "Apply mission timeline / events" as step 2 of a pipeline it describes as running on the same code path for interactive and headless modes. **That is not accurate for this system.** See ADR-021 Decision 1.

**If mission timeline is ever wired into live play**, this section must be rewritten and the requirement re-scoped — the player-facing fantasy (mission phases advancing, ROE tightening as contact is gained) would then be real rather than aspirational.

## Detailed Rules

### Scripted timeline

1. Events are sorted **once at construction** by `(FireAtTick asc, position in authored fireOrder asc, EventId ordinal)`. Events absent from `fireOrder` sort last via `int.MaxValue`.
2. A **monotonic cursor** (`_nextIndex`) guarantees each event fires exactly once, in that order.
3. On each tick, **every** event with `FireAtTick <= simTick` flushes — events whose tick has already passed are not skipped.
4. Event kinds map to order-log entries: `MissionTransition` → `MissionTransitionRecord`, `EventFired` → `EventFiredRecord`.
5. `MissionRuntimeFactory.TryCreate` returns `null` when a scenario has no events, so the runtime is absent rather than idle.

### Contact triggers

6. Triggers are sorted by `TriggerId` ordinal at construction.
7. A trigger fires on exactly one edge: `ContactLifecycleState.Unknown → Detected`. No other transition fires it.
8. Each `TriggerId` fires **at most once per run**, tracked in a `HashSet<string>`.
9. A trigger matches when its `ObserverId` equals the transition's observer **and** its `TargetClass` matches.
10. **Target class is derived from the target id prefix**, not a domain field: `targetId.StartsWith("ucav")` → `Air`, everything else → `Surface`; `Any` matches all. (`MissionContactTargetClassifier`, `src/ProjectAegis.Sim/Scenario/MissionContactTargetClass.cs:12-15`)
11. A fired trigger calls `DelegationOrchestrator.ApplyRoeToUnits`, which iterates units in ordinal order and applies the new ROE **only where it differs**.

### Ordering within the tick

12. Inside the harness loop the order is: `MissionRuntime.Tick` → detection transitions → `MissionContactTriggerRuntime.Evaluate` → `DelegationOrchestrator.Tick`.
13. That ordering is why a unit which gains a trigger-matching contact is already at escalated ROE **on the same tick** the agent decides — there is no one-tick lag.

### Fire order

14. When a scenario has a mission timeline, `fire_order` is returned verbatim as authored. Only scenarios *without* one reconstruct it chronologically from `EventFired` rows.

## Formulas

None. The mission runtime contains no numeric model — no probability, damage, or rate maths. Its only "formula" is the total-order sort key in rule 1:

```
sortKey(event) = (FireAtTick, orderIndex(EventId) ?? int.MaxValue, EventId ordinal)
```

This is a **total** order — ties are impossible — which is what makes emission sequence reproducible.

## Edge Cases

| Case | Behaviour |
|---|---|
| Events scheduled in the past relative to first evaluation | All flush together on the first tick evaluated; none skipped |
| Two or more events on the same `FireAtTick` | Fire in `fireOrder` position, then `EventId` ordinal. **See the caveat below** |
| Scenario has no mission events | `TryCreate` returns `null`; no runtime, no rows |
| Duplicate `TriggerId` | Second and later occurrences are suppressed by the fire-once `HashSet` |
| Trigger's unit id does not resolve | **Silently skipped** — a typo shrinks the ROE escalation with no error or warning |
| Trigger re-applies a unit's current ROE | No `PolicyUpdate` row, no fingerprint delta (idempotent-per-value) |
| Contact never reaches `Detected` | Trigger never fires; no rows |
| Air unit named without the `ucav` prefix | **Misclassified as `Surface`**; trigger silently does not match |

> **Same-tick caveat.** `MissionRuntime.Tick` computes local sequence ids `sequenceStart + emissionIndex` which, for the 2nd and later same-tick events, would collide with early-run order-log ids. They are inert today because the value is discarded twice before reaching `DecisionLog`. **Do not "fix" this by wiring the value through** — that activates the collision against two golden-backed fixtures. Tracked as **DRG-49**; the parameter should be deleted, not plumbed.

## Dependencies

| Direction | System |
|---|---|
| **Upstream** | Sensor & Contact Model (`ContactTransition`), Policy/ROE (`ApplyRoeToUnits`, `EffectivePolicy`), Order Log (#2) |
| **Downstream** | Message-log projection only. **No player-facing consumer** |
| **Host** | `BalticReplayHarness.RunCore` — the sole production caller |

## Tuning Knobs

None global. All mission behaviour is **per-scenario authored content**, not tunable configuration:

| Field | Location |
|---|---|
| `events[].fireAtTick`, `eventId`, `kind`, `code` | scenario `mission` block |
| `fireOrder` | scenario `mission` block |
| `triggers[].triggerId`, `observerId`, `targetClass`, `missionCode`, `roe`, `unitIds` | scenario `mission` block |

Unlike Order Log & Replay (which has `checkpointIntervalTicks`), this system has no dial a designer turns globally.

## Acceptance Criteria

Sourced from REQ-07 (INF-3.2, INF-2.5) and REQ-11 (AME-6.6/AC-2, AME-5.5/AC-7):

- [x] Authored `fire_order` is honoured verbatim and reproducibly (`ResolveFireOrder`)
- [x] Emission order is deterministic under a total sort key; no wall-clock read, no unseeded RNG, no unordered iteration for output
- [x] Each event and each `TriggerId` fires exactly once per run
- [x] Mission rows participate in `ComputeFingerprint()` and therefore in replay verification
- [x] ROE escalation applies on the same tick the qualifying contact is gained
- [ ] **Not applicable while headless-scoped:** any player-facing acceptance criterion. Re-open if the system is wired into interactive play

## Open Questions

1. **Should target class come from a real domain field** rather than an id prefix? The current rule is a documented MVP shortcut that breaks silently on rename.
2. **Should unresolved unit ids in triggers warn** instead of being silently skipped? Today a scenario typo degrades the escalation invisibly.
3. **Is headless-only permanent?** Confirmed as current scope 2026-07-24. If it changes, Player Fantasy and the acceptance criteria both need rewriting.

## TR IDs

| TR-ID | Status |
|---|---|
| Systems index **#9** (Mission Runtime) | GDD + ADR now present — was "Needs GDD + ADR" |
