# Direct-control override & group detach/rejoin — developer guide

The player commands theater-level forces and *delegates* tactical decisions to AI agents, but must
be able to reach in and take the wheel of a single unit at any time — and hand it back. This page
documents the **controller-arbitration runtime** behind that seam: who is driving each unit each
tick (a `HumanController` or an `AgentController`), how "Take Direct Control" / "Release" swap them,
and how detaching one member of an AI-led group works without corrupting the group's plan.

It is the human-in-the-loop counterpart to the [agent decision pipeline](agent-decision-pipeline.md):
the decision pipeline decides for *agent*-controlled targets; this runtime decides *which* controller
a target uses in the first place, and drains the player's queued orders for *human*-controlled ones.

- **Source:** controllers in
  [`src/ProjectAegis.Delegation/Controllers/`](../../src/ProjectAegis.Delegation/Controllers/),
  the override + orchestrator boundary in
  [`src/ProjectAegis.Delegation/Orchestration/`](../../src/ProjectAegis.Delegation/Orchestration/),
  detach/rejoin in [`src/ProjectAegis.Delegation/Groups/`](../../src/ProjectAegis.Delegation/Groups/),
  and the commandable targets in
  [`src/ProjectAegis.Delegation/Targets/`](../../src/ProjectAegis.Delegation/Targets/).
- **Related:** the Unity/DOTS ingress that calls these methods is the
  [`DelegationBridge`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs);
  the comms-delay that holds player orders is covered by the
  [comms degradation runtime](comms-degradation-runtime.md); the order log this runtime writes to is
  surfaced to the C2 UI by the [C2 projection layer](c2-projection-layer.md). This page documents
  what the override/detach machinery actually **does** and how to extend it without breaking replay
  goldens.

---

## Mental model

Every commandable target — a single `UnitTarget` or a `GroupTarget` — owns exactly one
**`ControllerSlot`**. The slot is a tiny state machine with two fields:

| Field | Meaning |
|-------|---------|
| `Active` | The `IController?` actually driving the target this tick (`AgentController`, `HumanController`, or `null`/none). |
| `SuspendedAgent` | An `AgentController` parked while a human is temporarily in control, so it can be resumed later. |

There are **two distinct ways** a player takes control, and picking the right one is the whole job
of the orchestrator boundary:

1. **Suspend override** (`OverrideService`) — for a target whose *own* slot holds an agent (a
   standalone unit, or a group). The agent is *parked* in `SuspendedAgent`, a `HumanController` takes
   over, and releasing **resumes the same agent instance** (traits, RNG, experience intact).
2. **Detach / rejoin** (`DetachRejoinService`) — for a unit that is a *member of an AI-led group*.
   The unit is removed from the group roster, the group is flagged to replan without it, and the unit
   gets its **own fresh** `HumanController`. Rejoin reverses both: the unit's controller is cleared
   and it is re-added to the group.

The difference matters: a group member is normally commanded *collectively* through the group's
controller and has no controller of its own, so there is no per-unit agent to suspend — detaching is
a roster edit, not a controller swap. A standalone unit *does* hold its own agent, so its agent is
suspended in place.

```
          Take Direct Control
                  │
   ┌──────────────┴───────────────┐
   │ is the unit a group member?  │
   └──────────────┬───────────────┘
        yes ┌─────┴─────┐ no
            ▼           ▼
   DetachRejoinService  slot.Active is AgentController?
   .Detach(group,unit)      yes ┌──┴──┐ no (empty slot)
   • remove from roster         ▼      ▼
   • group.MarkReplanPending    OverrideService   slot.SetActive(
   • unit gets fresh Human      .TakeDirectControl   new HumanController())
                                • park agent →
                                  SuspendedAgent
                                • install Human
```

---

## Where it lives

### Controllers & slot

| File | Role |
|------|------|
| [`IController.cs`](../../src/ProjectAegis.Delegation/Controllers/IController.cs) | The controller contract: `bool IsHuman` and `DrainIssuedOrders(ulong currentSimTick)`. |
| [`AgentController.cs`](../../src/ProjectAegis.Delegation/Controllers/AgentController.cs) | AI controller: `TryDecide(...)` runs the decision tick and buffers orders; `DrainIssuedOrders` hands them to the orchestrator. Holds traits, autonomy, `SeededRng`, policy, attention budget, experience. |
| [`HumanController.cs`](../../src/ProjectAegis.Delegation/Controllers/HumanController.cs) | Player controller: wraps a `PlayerOrderExecutionQueue`; `Enqueue` accepts an order + execute tick, `DrainIssuedOrders` releases only orders whose delay has elapsed. |
| [`ControllerSlot.cs`](../../src/ProjectAegis.Delegation/Controllers/ControllerSlot.cs) | `Active` + `SuspendedAgent` with `SetActive` / `ClearActive` / `SuspendAgent` / `ResumeSuspendedAgent` (the last throws if nothing is suspended). |

### Targets

| File | Role |
|------|------|
| [`ICommandableTarget.cs`](../../src/ProjectAegis.Delegation/Targets/ICommandableTarget.cs) | `TargetId Id`, `ControllerSlot Slot`, `bool IsDetachedFromGroup`. |
| [`UnitTarget.cs`](../../src/ProjectAegis.Delegation/Targets/UnitTarget.cs) | A single unit; tracks `IsDetachedFromGroup` + `DetachedFromGroupId` via `SetDetached(detached, fromGroup)`. |
| [`GroupTarget.cs`](../../src/ProjectAegis.Delegation/Targets/GroupTarget.cs) | A group: a `Members` roster (`AddMember` / `RemoveMember`) + a `PendingReplan` flag (`MarkReplanPending` / `ClearReplanPending`). `IsDetachedFromGroup` is always `false`. |

### Override, detach & orchestrator boundary

| File | Role |
|------|------|
| [`OverrideService.cs`](../../src/ProjectAegis.Delegation/Orchestration/OverrideService.cs) | `TakeDirectControl(target, human)` parks any active agent then installs the human; `ReleaseDirectControl(target)` resumes a suspended agent, or clears the slot when there is none. |
| [`DetachRejoinService.cs`](../../src/ProjectAegis.Delegation/Groups/DetachRejoinService.cs) | `Detach(group, unit)` flags the unit detached, removes it from the roster, marks the group for replan, and gives the unit a fresh human; `Rejoin` reverses all four. |
| [`DelegationOrchestrator.cs`](../../src/ProjectAegis.Delegation/Orchestration/DelegationOrchestrator.cs) | Public boundary: `TryTakeDirectControl` / `TryReleaseDirectControl` (pick the right path + log), `FindParentGroup`, and the per-tick controller drain in `Tick`. |
| [`SimulationModeConfigurator.cs`](../../src/ProjectAegis.Delegation/Orchestration/SimulationModeConfigurator.cs) | Initial controller assignment per `SimulationModeKind` (`Human` puts the friendly side on `HumanController`; `AgentVsAgent` assigns agents to both sides and calls `BeginExecution`; `Mixed` splits by side). |
| [`PlayerOrderExecutionQueue.cs`](../../src/ProjectAegis.Delegation/Decision/PlayerOrderExecutionQueue.cs) | The comms-delay buffer inside every `HumanController` (req 19). |

### Order-log payloads (what a control change writes)

| File | Order-log entry | Payload |
|------|-----------------|---------|
| [`ControllerChangeRecord.cs`](../../src/ProjectAegis.Delegation/Decision/ControllerChangeRecord.cs) | `OrderLogEntryKind.ControllerChange` (=3) | `(SequenceId, SimTime, TargetId, PreviousKind, NewKind, AgentId?)` |
| [`GroupMemberDetachRecord.cs`](../../src/ProjectAegis.Delegation/Decision/GroupMemberDetachRecord.cs) | `OrderLogEntryKind.GroupMemberDetach` (=4) | `(SequenceId, SimTime, GroupId, UnitId)` |
| [`GroupMemberRejoinRecord.cs`](../../src/ProjectAegis.Delegation/Decision/GroupMemberRejoinRecord.cs) | `OrderLogEntryKind.GroupMemberRejoin` (=5) | `(SequenceId, SimTime, GroupId, UnitId)` |

`DelegationOrchestrator` writes these through `DecisionLog.AppendControllerChange` /
`AppendGroupMemberDetach` / `AppendGroupMemberRejoin`. Each is folded into the unified order log and
is readable both chronologically (`DecisionLog.ChronologicalEntries()`) and via the typed accessors
`DecisionLog.ControllerChanges` / `GroupMemberDetaches` / `GroupMemberRejoins`.

---

## 1. Suspend override (`OverrideService`)

For a target that holds its *own* agent, taking control parks the agent and swaps in a human:

```csharp
public void TakeDirectControl(ICommandableTarget target, HumanController human)
{
    if (target.Slot.Active is AgentController agent)
        target.Slot.SuspendAgent(agent);   // parked in SuspendedAgent, Active → null
    target.Slot.SetActive(human);          // human now drives
}
```

Releasing prefers to **resume the parked agent**; only when nothing is suspended does it clear the
slot:

```csharp
public void ReleaseDirectControl(ICommandableTarget target)
{
    if (target.Slot.SuspendedAgent is not null)
    {
        target.Slot.ResumeSuspendedAgent();  // Active → the same agent instance
        return;
    }
    target.Slot.ClearActive();               // no agent to restore → None
}
```

Because the *same* `AgentController` instance is resumed, its `TraitVector`, `SeededRng` cursor,
bound `EffectivePolicy` snapshot, and `AgentExperienceBlob` all survive the human interlude — the AI
picks up exactly where it left off. Pinned by
[`OverrideTests`](../../src/ProjectAegis.Delegation.Tests/Controllers/OverrideTests.cs)
(`Override_swaps_agent_for_human_and_preserves_suspended_agent`,
`Release_restores_suspended_agent`).

---

## 2. Detach / rejoin (`DetachRejoinService`)

A group member has no controller of its own — it is commanded through the group's controller.
Detaching is therefore a **roster edit plus a fresh human**, not an agent suspend:

```csharp
public void Detach(GroupTarget group, UnitTarget unit)
{
    unit.SetDetached(true, group.Id);   // remembers which group to rejoin
    group.RemoveMember(unit.Id);        // drops out of the roster
    group.MarkReplanPending();          // group must replan without it
    _overrideService.TakeDirectControl(unit, new HumanController());  // fresh human on the unit
}

public void Rejoin(GroupTarget group, UnitTarget unit)
{
    _overrideService.ReleaseDirectControl(unit);  // unit's slot had no suspended agent → cleared
    unit.SetDetached(false);
    group.AddMember(unit.Id);
    group.MarkReplanPending();
}
```

Detaching a member shrinks `group.Members.Count`, which is exactly the `memberCount` the group's
agent feeds into the [attention model](agent-decision-pipeline.md) each tick — so a detached member
also *reduces the group agent's cognitive load*. `PendingReplan` is a one-shot flag consumed by the
next `Tick` (see §4). Pinned by
[`DetachRejoinTests`](../../src/ProjectAegis.Delegation.Tests/Groups/DetachRejoinTests.cs).

---

## 3. The orchestrator boundary (`TryTakeDirectControl` / `TryReleaseDirectControl`)

`OverrideService` and `DetachRejoinService` are low-level; game/UI code never calls them directly.
The public seam is on `DelegationOrchestrator`, which **chooses** the right path, **logs** the
change, and **guards** the human-ingress rules.

### `TryTakeDirectControl(UnitTarget unit, double simTime)`

Ordered decision:

1. **`AttachReplayViewer` block.** When the orchestrator is in observer / replay-scrub mode (req 03
   AvA), all human ingress is refused — returns `false`, nothing is logged.
2. **Already human → no-op success.** If the unit is *already* under a `HumanController`, return
   `true` without touching the slot. This is deliberate: once a unit is detached it is no longer in
   any group roster, so `FindParentGroup` would miss and the code would fall through to installing a
   *brand-new* `HumanController` — silently discarding any orders already queued in the existing one
   (the req 19 comms-delay queue). The guard preserves the queue on a double-click / re-issued
   "take control".
3. **Group member → detach path.** If `FindParentGroup(unit.Id)` finds a group, run
   `DetachRejoinService.Detach` and append a `GroupMemberDetach` record.
4. **Standalone agent → suspend path.** Otherwise, if the unit's own slot holds an `AgentController`,
   run `OverrideService.TakeDirectControl` (park + install human).
5. **Empty slot → fresh human.** Otherwise just `SetActive(new HumanController())`.

In every non-blocked case it appends a `ControllerChange` record (`previous → "Human"`), carrying
the affected `AgentId` (the suspended agent's id, or the active agent's) for provenance.

### `TryReleaseDirectControl(UnitTarget unit, double simTime)`

1. **`AttachReplayViewer` block** → `false`.
2. **Not human → `false`.** Releasing only makes sense when a human is currently driving.
3. **Detached member → rejoin.** If the unit is flagged detached and its `DetachedFromGroupId`
   resolves to a live `GroupTarget`, run `DetachRejoinService.Rejoin` and append a
   `GroupMemberRejoin` record.
4. **Otherwise → release.** Run `OverrideService.ReleaseDirectControl` (resume the suspended agent,
   or clear the slot).

Then append a `ControllerChange` (`"Human" → …`) with the resumed `AgentId` when an agent came back.
The take-detach-log, double-click-preservation, and rejoin behaviors are pinned by
[`OrchestratorOverrideTests`](../../src/ProjectAegis.Delegation.Tests/Orchestration/OrchestratorOverrideTests.cs).

### Unity/DOTS ingress

The [`DelegationBridge`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs)
exposes `TryTakeDirectControl(EntityKey, simTime)` / `TryReleaseDirectControl(EntityKey, simTime)`,
which re-check `AttachReplayViewer`, resolve the `EntityKey` to a `UnitTarget` via the
`TargetRegistry`, and forward to the orchestrator. There is no other write path — `DelegationBridge`
stays zero-touch on the hotpath (see `AGENTS.md`).

---

## 4. What the tick does with each controller

`DelegationOrchestrator.Tick(ObservedState)` iterates every registered target and dispatches on
`Slot.Active`:

```csharp
foreach (var target in _targets)
{
    if (target is GroupTarget group && group.PendingReplan)
        group.ClearReplanPending();               // consume the one-shot replan flag

    var memberCount = target is GroupTarget g ? g.Members.Count : 1;

    switch (target.Slot.Active)
    {
        case AgentController agent:
            agent.TryDecide(target.Id, state, memberCount, ref _orderIdSequence, _autonomyGate, DecisionLog);
            executed.AddRange(agent.DrainIssuedOrders(simTick));
            break;
        case HumanController human:
            executed.AddRange(human.DrainIssuedOrders(simTick));
            break;
    }
}
```

Key consequences:

- **A target with an empty slot (`null` active, e.g. a released standalone unit, or a group whose
  agent is suspended) issues nothing** — it is simply skipped. A *detached* member is never idle,
  though: detach installs a human, so it drains from its own queue.
- **`PlanningPhase` short-circuits.** In `SimulationPhase.Planning`, `Tick` clears `ExecutedOrders`
  and returns before touching any controller; controllers only run once `BeginExecution` flips the
  phase to `Executing`.
- **Player orders are gated by the comms-delay queue.** `HumanController.DrainIssuedOrders` returns
  only orders whose `ExecuteSimTick <= currentSimTick`; the rest stay pending
  (`PlayerOrderExecutionQueue.DrainReady` preserves insertion order and leaves not-yet-due orders in
  place). The execute tick itself is computed at enqueue time by
  `DelegationBridge.TryEnqueueHumanOrder` from `CurrentCommsState` — see
  [comms-degradation-runtime.md](comms-degradation-runtime.md).

---

## Determinism

This runtime is **replay-safe** and folds cleanly into the order-log fingerprint:

- **No wall-clock, no RNG in control arbitration.** `OverrideService`, `DetachRejoinService`, and the
  `Try*DirectControl` boundary read no `DateTime` and draw no `SeededRng`; all timing comes from the
  passed `simTime` / `simTick`. (The resumed agent's own decisions remain deterministic because its
  `SeededRng` cursor is carried across suspend/resume — nothing is re-seeded.)
- **Stable iteration.** `Tick` iterates `_targets` in registration order; `FindParentGroup` /
  `FindTarget` scan the same list in order and return the first match.
- **Order-log fold.** `ControllerChange`, `GroupMemberDetach`, and `GroupMemberRejoin` entries are
  appended to the unified order log (and exposed via the `DecisionLog` typed accessors). Note the
  live player-driven `Try*DirectControl` calls are **not** part of the headless Baltic replay
  goldens: those run agent-vs-agent with no human ingress, so no control-change entries are produced.
  Human control is exercised by the C2 Play Mode smoke path, not the Baltic replay suite.
- **Queue ordering is deterministic.** `PlayerOrderExecutionQueue.DrainReady` releases due orders in
  their original enqueue order and is a pure function of `(pending, currentSimTick)`.

> **The "already human" guard is load-bearing.** Removing the early-return in
> `TryTakeDirectControl` reintroduces the queue-loss bug: a redundant take-control on an
> already-detached unit replaces the `HumanController`, dropping queued (comms-delayed) orders. It is
> pinned by `TryTakeDirectControl_calledAgainOnAlreadyDetachedUnit_preservesQueuedPlayerOrders` —
> keep that test green.

---

## Extending it

- **Add a new controller kind:** implement `IController` (set `IsHuman` and `DrainIssuedOrders`), add
  a `case` in the `Tick` switch, and decide its `SimulationModeConfigurator` assignment. If a control
  change to/from it must be observable, extend `DescribeActiveController` and reuse the existing
  `ControllerChange` record rather than adding a new order-log kind.
- **Change take/release policy:** edit the ordered decision in `TryTakeDirectControl` /
  `TryReleaseDirectControl` — keep the `AttachReplayViewer` guard first and the "already human"
  no-op, and keep every state change paired with its order-log append so the C2 message log and any
  fingerprint stay consistent.
- **Richer group semantics** (e.g. sub-groups, multi-unit detach): extend `GroupTarget` /
  `DetachRejoinService`, keep `PendingReplan` a one-shot consumed by `Tick`, and keep `Members`
  edits and the detach/rejoin order-log records in lockstep. Add coverage under
  `Groups/DetachRejoinTests` and `Orchestration/OrchestratorOverrideTests`.
- **Do not** add human-ingress paths that bypass the orchestrator boundary or the
  `AttachReplayViewer` guard — that is the single choke point that keeps replay/observer modes safe.

---

## Related

| Doc | What |
|-----|------|
| [agent-decision-pipeline.md](agent-decision-pipeline.md) | The per-agent decision tick that runs for agent-controlled targets (and the attention model `memberCount` feeds). |
| [comms-degradation-runtime.md](comms-degradation-runtime.md) | How `CurrentCommsState` computes the player-order execute tick that the `HumanController` queue gates on. |
| [c2-projection-layer.md](c2-projection-layer.md) | The read-model layer over the order log (message log, OOB tree, selection state) that surfaces control state to the C2 UI. |
| [baltic-replay-harness.md](baltic-replay-harness.md) | The headless runner (agent-vs-agent) behind the replay goldens; no human ingress. |
| [determinism-and-replay.md](determinism-and-replay.md) | Order-log fingerprint / world-state hashing and the golden workflow. |
| [`docs/architecture/`](../architecture/) | ADR-003 (order-log schema), ADR-002 (policy evaluator). |
