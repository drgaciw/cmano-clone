# Commandable target object model — developer guide

Every unit or formation the delegation framework can command is an **`ICommandableTarget`**. This is
the small polymorphic object model that lets the orchestrator, override/detach services, and the
Unity host all treat a single ship and a whole surface-action group **the same way** — each carries
its own controller slot, its own identity, and its own group/detach state. This page documents the
*objects and how they are registered/keyed*; the **runtime behaviour** that mutates them (taking
control, detaching, rejoining) is in the [direct-control override runtime](direct-control-override-runtime.md).

- **Source:** [`src/ProjectAegis.Delegation/Targets/`](../../src/ProjectAegis.Delegation/Targets/)
  (`ICommandableTarget`, `UnitTarget`, `GroupTarget`) with the per-target controller in
  [`Controllers/ControllerSlot`](../../src/ProjectAegis.Delegation/Controllers/ControllerSlot.cs).
  The host-side registry that keys them lives in
  [`Delegation.UnityAdapter/Bridge/`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/)
  (`TargetRegistry`, `SimEntityBinding`).
- **Related:** the take/release-control and detach/rejoin **behaviour** over these objects is
  [direct-control-override-runtime.md](direct-control-override-runtime.md); the decision tick that
  runs per target is [agent-decision-pipeline.md](agent-decision-pipeline.md); the player commands
  that target them are validated in the C2 command-issuance layer.

> **One abstraction, two shapes.** A `UnitTarget` (single unit) and a `GroupTarget` (formation) both
> implement `ICommandableTarget`, so the orchestrator holds and commands them through the one
> interface. Anything that must branch on shape does so explicitly (`target is GroupTarget group`) —
> there is no shared mutable base, and both are plain engine-agnostic C# (no Unity, no sim state).

---

## Where it lives

| Type | Role |
|------|------|
| [`ICommandableTarget`](../../src/ProjectAegis.Delegation/Targets/ICommandableTarget.cs) | The contract: `Id` (`TargetId`), `Slot` (`ControllerSlot`), `IsDetachedFromGroup`. |
| [`UnitTarget`](../../src/ProjectAegis.Delegation/Targets/UnitTarget.cs) | A single commandable unit; carries detach state (`IsDetachedFromGroup`, `DetachedFromGroupId`, `SetDetached`). |
| [`GroupTarget`](../../src/ProjectAegis.Delegation/Targets/GroupTarget.cs) | A formation; carries `Members` + `PendingReplan` (`IsDetachedFromGroup` is always `false`). |
| [`ControllerSlot`](../../src/ProjectAegis.Delegation/Controllers/ControllerSlot.cs) | Each target's `Active` controller + optional `SuspendedAgent` (the override/park mechanism). |
| [`SimEntityBinding`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/SimEntityBinding.cs) | Host record binding `(EntityKey, TargetId, ICommandableTarget)`. |
| [`TargetRegistry`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/TargetRegistry.cs) | Host-side dual index `EntityKey ↔ TargetId ↔ binding`; registers targets with the orchestrator. |

---

## The abstraction (`ICommandableTarget`)

The interface is deliberately tiny — three members that every commandable thing exposes:

| Member | Meaning |
|--------|---------|
| `TargetId Id` | Stable domain identity (the id the order log, projections, and orders key on). |
| `ControllerSlot Slot` | The target's controller seat — who is currently deciding for it. |
| `bool IsDetachedFromGroup` | Whether this target has been pulled out of its parent group (unit-only concept). |

Because the orchestrator only depends on this interface, adding a new kind of commandable thing is a
matter of implementing these three members — the decision tick, override, and logging paths don't
change.

### `ControllerSlot` — the seat every target carries

Each target owns one [`ControllerSlot`](../../src/ProjectAegis.Delegation/Controllers/ControllerSlot.cs):

- `Active` — the current `IController` (a `HumanController` or an `AgentController`).
- `SuspendedAgent` — an `AgentController` parked by a player override.
- `SuspendAgent(agent)` parks the agent and clears `Active`; `ResumeSuspendedAgent()` restores **the
  same instance** (throwing if none is parked); `SetActive` / `ClearActive` set the seat directly.

This is what makes "take control, then hand it back to the same agent state" work — the object model
holds the parked agent on the target itself. The *when/why* of suspend/resume is the
[override runtime](direct-control-override-runtime.md); the *where it is stored* is here.

---

## The two implementations

### `UnitTarget` — a single unit

`UnitTarget(TargetId)` gets a fresh `ControllerSlot`. Beyond the interface it tracks **detach**:

- `IsDetachedFromGroup` / `DetachedFromGroupId` — set together via
  `SetDetached(detached, fromGroup?)`. When detached it remembers which group it left; clearing
  detach nulls the origin id.

Detach is how a single ship is pulled out of a formation to be commanded on its own (the
[detach/rejoin service](direct-control-override-runtime.md) drives this).

### `GroupTarget` — a formation

`GroupTarget(TargetId)` also gets its own `ControllerSlot`, and adds formation state:

- `Members` (`IReadOnlyList<TargetId>`) with `AddMember` / `RemoveMember` — the member ids the group
  commands.
- `PendingReplan` with `MarkReplanPending()` / `ClearReplanPending()` — a **one-shot** flag set when
  the roster changes (e.g. a member detaches) and consumed by the orchestrator's next tick so the
  group re-plans exactly once.
- `IsDetachedFromGroup` is always `false` — a group is a parent, not a detachable member.

The group commands its members but does not own their controller slots — each member is its own
`ICommandableTarget` with its own slot.

---

## Host registry (`TargetRegistry` + `SimEntityBinding`)

The Unity adapter maps the engine's opaque `EntityKey` to a domain `TargetId` and target object. A
[`SimEntityBinding`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/SimEntityBinding.cs) is
just the immutable triple `(EntityKey, TargetId, ICommandableTarget)`.
[`TargetRegistry`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/TargetRegistry.cs) keeps a
**dual index** so lookups work from either side:

- `RegisterUnit(entity, targetKey)` / `RegisterGroup(entity, targetKey)` — build a `UnitTarget` /
  `GroupTarget`, index it by both `EntityKey` and `TargetId`, and **register it with the
  `DelegationOrchestrator`** so the sim owns the decision loop for it.
- `LinkGroupMember(groupId, memberId)` — add an already-registered target as a member of a registered
  group (throws if either is missing).
- `TryGetBinding(EntityKey…)` / `TryGetBinding(TargetId…)` — the two lookup directions the bridge and
  command layer use.
- `CollectMemberIds()` — the flat member-id list that feeds `OobTreeProjection` / map / unit-detail
  read-models. Registering a `UnitTarget` appends its id here.

> **Dual-uniqueness invariant (bug-fixed).** `Register` rejects both a duplicate `EntityKey` **and** a
> duplicate `TargetId` (fix `qa-r2-08-unity-adapter`). Before the `TargetId` check existed, two
> different entities registered under the same string target key would silently overwrite the
> `TargetId → binding` map and push a duplicate id into `CollectMemberIds()` — rendering the same
> unit twice in the OOB tree / map picture. Keep **both** guards when touching registration.

---

## How it is commanded

Once registered, the orchestrator drains and commands each target uniformly through
`ICommandableTarget.Slot.Active`:

```text
TargetRegistry.RegisterUnit/RegisterGroup  →  orchestrator.Register(target)
        │
        ▼
DelegationOrchestrator.Tick  →  per target: Slot.Active decides (AgentController / HumanController)
        │                                    GroupTarget: consume PendingReplan (one-shot)
        ▼
OverrideService / DetachRejoinService  →  mutate Slot (suspend/resume) + UnitTarget.SetDetached +
                                          GroupTarget.Add/RemoveMember + MarkReplanPending
```

The override/detach services are the only things that mutate control and membership; the decision
tick only *reads* `Slot.Active` and *consumes* `PendingReplan`. That separation is what keeps
control changes observable (each is paired with an order-log record) and replay-safe.

---

## Determinism & safety notes

- **Plain engine-agnostic objects** — no RNG, no wall-clock, no sim/order-log writes in the `Targets/`
  types themselves; they are pure state holders.
- **`PendingReplan` is one-shot** — set on a roster change, consumed by the next tick. Don't re-read
  it without clearing, or a group re-plans forever.
- **Dual-uniqueness on registration** — keep both the `EntityKey` and `TargetId` guards in
  `TargetRegistry.Register` (see the bug note above).
- **Branch on shape explicitly** — use `target is GroupTarget` / `is UnitTarget`; don't smuggle
  shared mutable state into the interface.
- **Slots hold the parked agent** — override/park lives on the target's `ControllerSlot`, so restoring
  control restores the exact suspended `AgentController` instance.

---

## Common pitfalls

- **Registering two entities under one target key.** Rejected by design — pick unique `TargetId`s;
  the duplicate would corrupt the OOB/map member list.
- **Expecting a group to own member control.** A `GroupTarget` lists member ids but each member is its
  own `ICommandableTarget` with its own `ControllerSlot`; command members through their own targets.
- **Forgetting to clear `PendingReplan`.** Treat it as a one-shot the orchestrator consumes.
- **Mutating detach/membership outside the services.** Route roster/detach changes through
  `DetachRejoinService` / `OverrideService` so the order-log record is written in lockstep (see the
  [override runtime](direct-control-override-runtime.md)).

---

## Tests

| Test file | Covers |
|-----------|--------|
| [`TargetRegistryTests`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/TargetRegistryTests.cs) | Register unit/group, dual `EntityKey`/`TargetId` uniqueness, `LinkGroupMember`, `CollectMemberIds`. |
| [`OrchestratorOverrideTests`](../../src/ProjectAegis.Delegation.Tests/Orchestration/OrchestratorOverrideTests.cs) / detach-rejoin tests | Slot suspend/resume + detach/rejoin behaviour over these targets (see the override runtime doc). |

Run the delegation + adapter suites after any change here:

```bash
dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj -v minimal
```

---

## See also

| Topic | Doc |
|-------|-----|
| Take/release control + detach/rejoin **behaviour** over these targets | [direct-control-override-runtime.md](direct-control-override-runtime.md) |
| The per-target decision tick (`Slot.Active` deciding) | [agent-decision-pipeline.md](agent-decision-pipeline.md) |
| Read-models that consume `CollectMemberIds()` (OOB tree / map) | [c2-projection-layer.md](c2-projection-layer.md) |
| Delegation core, the bridge, and the order log | [`src/ProjectAegis.Delegation/README.md`](../../src/ProjectAegis.Delegation/README.md) |
