# Mission timeline runtime — developer guide

Scenarios can drive two kinds of *scripted mission behaviour* at tick time, both authored under
the policy JSON `mission` block and both engine-agnostic:

1. **Scheduled timeline events** — a locked-order list of mission-phase transitions / event
   markers that fire at fixed sim ticks (`MissionRuntime`). This is the "mission clock".
2. **Contact-triggered ROE escalation** — a rule that, the moment a named observer *first
   detects* a matching contact, flips a set of units to a new ROE and stamps a mission-code
   transition (`MissionContactTriggerRuntime`). This is the Baltic v3 ASuW/AAA "weapons-free on
   recon" mechanism referenced throughout `AGENTS.md`.

Both are **inputs to the decision tick, not part of it**: they run in the
[Baltic replay harness](baltic-replay-harness.md) tick loop, append entries to the order log, and
(for triggers) mutate per-unit ROE *before* `DelegationOrchestrator.Tick` decides that tick. They
are pure and deterministic so they never perturb replay goldens.

- **Source:** [`src/ProjectAegis.Delegation/Mission/`](../../src/ProjectAegis.Delegation/Mission/)
  (runtimes + factories), with the scenario-side DTOs in
  [`src/ProjectAegis.Sim/Scenario/`](../../src/ProjectAegis.Sim/Scenario/).
- **Related:** how to *author* the `mission.events`, `mission.fireOrder`, and `mission.triggers`
  JSON — including enum values and fallbacks — is in
  [`scenario-policy-authoring.md`](scenario-policy-authoring.md). The contact transitions that
  drive triggers come from the [detection pipeline](detection-pipeline.md); the ROE a trigger sets
  is later enforced by the [engagement pipeline](engagement-pipeline.md); the decision tick that
  consumes the escalated ROE is the [agent decision pipeline](agent-decision-pipeline.md). This
  page documents what the mission runtimes actually **do** at tick time and how to extend them
  without breaking replay goldens.

---

## Where it lives

| File | Role |
|------|------|
| [`MissionRuntime.cs`](../../src/ProjectAegis.Delegation/Mission/MissionRuntime.cs) | `Tick(simTick, simTime, sequenceStart)` — fires due timeline events in locked order. |
| [`MissionEventDefinition.cs`](../../src/ProjectAegis.Delegation/Mission/MissionEventDefinition.cs) | One authored event: `(EventId, FireAtTick, Kind, Code)` + the `MissionEventKind` enum. |
| [`MissionRuntimeFactory.cs`](../../src/ProjectAegis.Delegation/Mission/MissionRuntimeFactory.cs) | `TryCreate(ScenarioMissionTimeline?)` → runtime or `null` when no events. |
| [`MissionContactTriggerRuntime.cs`](../../src/ProjectAegis.Delegation/Mission/MissionContactTriggerRuntime.cs) | `Evaluate(ContactTransition, simTime, simTick)` — fires matching triggers once, on `Unknown → Detected`. |
| [`MissionContactTriggerRuntimeFactory.cs`](../../src/ProjectAegis.Delegation/Mission/MissionContactTriggerRuntimeFactory.cs) | `TryCreate(ScenarioMissionTimeline?)` → runtime or `null` when no triggers. |
| [`ScenarioMissionTimeline.cs`](../../src/ProjectAegis.Sim/Scenario/ScenarioMissionTimeline.cs) | The parsed `mission` block: `FireOrder`, `Events`, `ContactTriggers`. |
| [`ScenarioMissionContactTrigger.cs`](../../src/ProjectAegis.Sim/Scenario/ScenarioMissionContactTrigger.cs) | One trigger rule + the `MissionContactPolicySide` enum. |
| [`MissionContactTargetClass.cs`](../../src/ProjectAegis.Sim/Scenario/MissionContactTargetClass.cs) | The `Surface`/`Air`/`Any` target class + `MissionContactTargetClassifier` (keys off the target id). |

The order-log payloads and the orchestrator entry point the runtimes drive:

| File | Role |
|------|------|
| [`MissionTransitionRecord.cs`](../../src/ProjectAegis.Delegation/Decision/MissionTransitionRecord.cs) | `MissionTransition` order-log payload `(SequenceId, SimTime, SimTick, EventId, PhaseCode)`. |
| [`EventFiredRecord.cs`](../../src/ProjectAegis.Delegation/Decision/EventFiredRecord.cs) | `EventFired` order-log payload `(SequenceId, SimTime, SimTick, EventId, EventCode)`. |
| [`DelegationOrchestrator.ApplyRoeToUnits`](../../src/ProjectAegis.Delegation/Orchestration/DelegationOrchestrator.cs) | Applies a trigger's ROE to units + logs a `PolicyUpdate` (only on an actual change). |

---

## 1. Scheduled timeline events (`MissionRuntime`)

### What it does

`MissionRuntime` is the deterministic mission clock. It is built once from the authored events and
the locked `fireOrder`, and each tick it releases every event whose `FireAtTick <= simTick`.

Construction sorts events into a **stable, deterministic** order (see
[`MissionRuntime.cs`](../../src/ProjectAegis.Delegation/Mission/MissionRuntime.cs)):

1. `FireAtTick` ascending, then
2. index within `fireOrder` (events not in `fireOrder` sort last), then
3. `EventId` ordinal.

`Tick` walks a monotonic cursor (`_nextIndex`) so each event fires **exactly once**, in that
sorted order, and assigns a per-tick contiguous `SequenceId` starting at the passed
`sequenceStart`:

```csharp
public IReadOnlyList<MissionTickEmission> Tick(ulong simTick, double simTime, ulong sequenceStart)
{
    var emissions = new List<MissionTickEmission>();
    while (_nextIndex < _events.Length && _events[_nextIndex].FireAtTick <= simTick)
    {
        var evt = _events[_nextIndex++];
        emissions.Add(new MissionTickEmission(evt, simTime, simTick, sequenceStart + (ulong)emissions.Count));
    }
    return emissions;
}
```

Because the cursor never rewinds, events authored with a `FireAtTick` in the past (relative to the
first tick they are evaluated) all flush on that first tick, still in sorted order.

### Event kinds → order log

Each emission carries a `MissionEventDefinition.Kind`, which the harness maps to one of two
order-log entry kinds:

| `MissionEventKind` | Order-log entry | Payload |
|--------------------|-----------------|---------|
| `MissionTransition` | `OrderLogEntryKind.MissionTransition` | `MissionTransitionRecord(SequenceId, SimTime, SimTick, EventId, PhaseCode = Code)` |
| `EventFired` | `OrderLogEntryKind.EventFired` | `EventFiredRecord(SequenceId, SimTime, SimTick, EventId, EventCode = Code)` |

`MissionTransition` marks a mission-phase change (its `Code` is the phase code, e.g. `PATROL` →
`STRIKE`); `EventFired` is a generic timeline marker. Both are folded into the decision-log
fingerprint (see [determinism](#determinism)) and are surfaced by the C2 mission projection
([c2-projection-layer.md](c2-projection-layer.md)).

### `fire_order` resolution (AC-2)

The authored `fireOrder` doubles as the canonical AC-2 `fire_order`. When a scenario has a mission
timeline, `BalticReplayHarness.ResolveFireOrder` returns it verbatim; only scenarios **without** a
timeline fall back to reconstructing the order chronologically from `EventFired` order-log entries:

```csharp
public static IReadOnlyList<string> ResolveFireOrder(
    ScenarioMissionTimeline? missionTimeline,
    DecisionLog decisionLog)
{
    if (missionTimeline != null)
        return missionTimeline.FireOrder;
    // else: chronological EventFired ids from the decision log
}
```

---

## 2. Contact-triggered ROE escalation (`MissionContactTriggerRuntime`)

### What it does

This is the Baltic v3 "weapons-free on first recon contact" mechanism. Each authored trigger says:
*when observer `X` first detects a `<targetClass>` contact, set ROE `<roe>` on `<unitIds>` for side
`<side>` and stamp mission code `<missionCode>`.*

The runtime fires a trigger on **one specific edge only** — a contact transition from
`Unknown → Detected` — and each trigger fires **at most once** for the whole run (tracked in the
`_fired` set):

```csharp
if (transition.PreviousState != ContactLifecycleState.Unknown ||
    transition.NewState != ContactLifecycleState.Detected)
{
    return Array.Empty<MissionContactTriggerEmission>();
}
```

For a qualifying transition, every not-yet-fired trigger is checked in **`TriggerId` ordinal order**
(sorted at construction, for determinism) against three conditions:

1. **Observer match** — `trigger.ObserverId == transition.ObserverId` (ordinal).
2. **Target class match** — `MissionContactTargetClassifier.Matches(trigger.TargetClass, transition.TargetId)`.
   The classifier keys off the **target id string**: ids starting with `ucav` classify as `Air`,
   everything else as `Surface`; `Any` matches all. (There is no separate domain field on the
   contact — the id prefix *is* the classification.)
3. **Not already fired** — the `TriggerId` is absent from `_fired`.

On a match it records the `TriggerId` in `_fired` and emits a `MissionContactTriggerEmission`
carrying the trigger + the transition's `simTime`/`simTick`.

### Trigger fields

`ScenarioMissionContactTrigger` (parsed from `mission.triggers[]`):

| Field | Type | Meaning |
|-------|------|---------|
| `TriggerId` | `string` | Stable id; also the deterministic evaluation-order key. Keep unique + ordinal-stable. |
| `ObserverId` | `string` | The unit whose detection arms the trigger. |
| `TargetClass` | `Any` / `Surface` / `Air` | Which contact class arms it (id-prefix classified). |
| `PolicySide` | `Friendly` / `Opposing` | Which side's policy resolution the new ROE is applied under. |
| `MissionCode` | `string` | Stamped as the `PhaseCode` of the emitted `MissionTransition`. |
| `Roe` | `RoeLevel` | The escalated ROE (`HoldFire` / `WeaponsTight` / `WeaponsFree`). |
| `UnitIds` | `string[]` | The units whose ROE is flipped. |

### Firing → order log + ROE mutation

For each emission the harness does two things (see
[`BalticReplayHarness`](../../src/ProjectAegis.Delegation.UnityAdapter/Baltic/BalticReplayHarness.cs)):

1. **Stamps a `MissionTransition`** in the order log with the trigger's `MissionCode` as the phase
   code (so the trigger shows up in the mission timeline / C2 picture just like a scheduled
   transition).
2. **Escalates ROE** via `DelegationOrchestrator.ApplyRoeToUnits(unitIds, new EffectivePolicy(roe, sideMaxSalvo), isFriendly, simTime, simTick)`,
   where `sideMaxSalvo` is the profile's `FriendlyDefault.MaxSalvo` (fallback
   `EffectivePolicy.DefaultFree.MaxSalvo`, applied to both sides' triggers) and
   `isFriendly = PolicySide == Friendly`.

`ApplyRoeToUnits` is **idempotent-per-value**: it resolves each unit's current effective policy,
and if the ROE + max-salvo are unchanged it skips the unit entirely. Only a real change captures a
new policy snapshot, appends a `PolicyUpdate` order-log entry (`field = "roe"`, old→new), and, for
agent-controlled units, rebinds the active `AgentController`'s policy snapshot. Units are processed
in **ordinal `unitId` order** for determinism, and unknown unit ids are silently skipped.

---

## Tick ordering (why triggers escalate the same tick)

Both runtimes are driven by the harness tick loop **before** the delegation decision tick for that
tick. The per-tick sequence is:

1. `harness.Advance(1.0)` → compute `simTick`.
2. **`MissionRuntime.Tick`** → append due `MissionTransition` / `EventFired` entries.
3. Compute this tick's detection **contact transitions** (+ optional datalink-shared transitions).
4. For each transition: **`MissionContactTriggerRuntime.Evaluate`** → append the trigger's
   `MissionTransition` and call `ApplyRoeToUnits`; then `AppendContactTransition`.
5. **`bridge.Tick(...)`** → `DelegationOrchestrator.Tick` runs the decision pipeline for this tick.

Because trigger evaluation (step 4) precedes the decision (step 5), a unit that first detects a
matching contact is already at the escalated ROE **on that same tick** — the autonomy/ROE gate in
the [engagement pipeline](engagement-pipeline.md) sees the new policy immediately, with no one-tick
lag.

---

## Determinism

Both runtimes are pure functions of their inputs and therefore replay-safe:

- **No wall-clock, no RNG.** Neither runtime reads `DateTime` nor draws from a `SeededRng`; timing
  comes only from the passed `simTick` / `simTime`. (Contrast with the detection pipeline, which
  *does* draw — see [determinism-and-replay.md](determinism-and-replay.md).)
- **Fixed evaluation order.** Timeline events are pre-sorted by `(FireAtTick, fireOrder index,
  EventId)`; triggers by `TriggerId`; `ApplyRoeToUnits` iterates units in ordinal order.
- **Fire-once semantics.** The timeline cursor is monotonic; each trigger id is one-shot via
  `_fired`.
- **Order-log fold.** Every `MissionTransition`, `EventFired`, and `PolicyUpdate` entry is folded
  into the decision-log **fingerprint** and thus into the replay golden. Changing when/whether a
  mission event or trigger fires *will* change the golden fingerprint — treat it like any other
  golden-affecting change (see the Baltic v3 isolation invariant in `AGENTS.md`; never touch v2
  goldens).

> **`ApplyRoeToUnits` only logs on change.** Because it no-ops when the ROE is already at the
> target value, re-authoring a trigger to a unit's current ROE produces no `PolicyUpdate` entry and
> no fingerprint delta. If you expect an escalation but the golden is unchanged, confirm the units
> weren't already at that ROE (e.g. via `missionPolicy.roe` or a `unitOverrides` entry).

---

## Extending it

- **Add a scheduled event:** author it under `mission.events` with a `fireAtTick`, `kind`
  (`MissionTransition` or `EventFired`), and `code`, and place its `id` in `mission.fireOrder` to
  pin its intra-tick order. No C# change is needed. Re-generate the affected Baltic v3 replay
  golden.
- **Add a contact trigger:** add an entry under `mission.triggers` (`id`, `observerId`,
  `targetClass`, `side`, `missionCode`, `roe`, `unitIds`). Choose an `id` whose ordinal sort gives
  the intra-detection ordering you want relative to sibling triggers. See the authoring reference
  and enum-fallback table in [scenario-policy-authoring.md](scenario-policy-authoring.md).
- **Add a new event kind:** extend `MissionEventKind`, map it to an order-log entry in the harness
  tick loop, and add a matching `OrderLogEntryKind` + payload record + `DecisionLog` fold. This is a
  golden-affecting, engine-agnostic change — cover it with a `BalticReplayHarness*` test.
- **Change the target classification:** `MissionContactTargetClassifier.Classify` is intentionally
  id-prefix based (`ucav*` → `Air`). If you introduce a richer classification (e.g. a real domain
  field on the contact), update `Classify`/`Matches` together and re-verify every `mission.triggers`
  target-class match plus the goldens.

---

## Related

| Doc | What |
|-----|------|
| [scenario-policy-authoring.md](scenario-policy-authoring.md) | Authoring `mission.events` / `mission.fireOrder` / `mission.triggers` JSON, enum values + fallbacks. |
| [detection-pipeline.md](detection-pipeline.md) | Where the `Unknown → Detected` contact transitions that arm triggers come from. |
| [agent-decision-pipeline.md](agent-decision-pipeline.md) | The decision tick that consumes the escalated ROE. |
| [engagement-pipeline.md](engagement-pipeline.md) | The ROE/autonomy gate that enforces the escalated policy. |
| [baltic-replay-harness.md](baltic-replay-harness.md) | The runner that wires both runtimes into the tick loop. |
| [determinism-and-replay.md](determinism-and-replay.md) | Order-log fingerprint / world-state hashing and the golden workflow. |
| [`docs/architecture/`](../architecture/) | ADR-008 (mission validation), ADR-002 (policy evaluator). |
