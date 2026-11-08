# Delegation bridge — the engine ↔ core adapter boundary

> **Scope.** How one sim/ECS tick crosses the seam between the engine (Unity / DOTS, or a
> headless host) and the engine-agnostic `ProjectAegis.Delegation` core: the **ingress**
> (`ISimWorldSnapshot` → `ObservedStateBuilder` → `ObservedState`), the `DelegationBridge.Tick`
> **fold** (pre-decision timeline emitters → orchestrator/session drive → order collection), and
> the **egress** (`OrderDispatcher` → `IOrderSink.ApplyOrder`). This is the *write side* of the
> adapter — the read side (order-log → C2 view models) is covered by
> [`c2-projection-layer.md`](c2-projection-layer.md) and the thin `*Bridge` facades. The
> decision maths inside `DelegationOrchestrator.Tick` live in
> [`agent-decision-pipeline.md`](agent-decision-pipeline.md); the engage half of the fold lives in
> [`engagement-pipeline.md`](engagement-pipeline.md).
>
> Boundary rationale: [ADR-001 (sim assembly boundary)](../architecture/adr-001-sim-assembly-boundary.md),
> [ADR-010 (headless-first, command-driven UI)](../architecture/adr-010-headless-first-command-driven-ui.md).
> The bridge project has **no `UnityEngine` reference**, so everything here runs under plain
> `dotnet test`.

---

## Where it lives

All in `src/ProjectAegis.Delegation.UnityAdapter/Bridge/`:

| Type | Kind | Role |
|------|------|------|
| `DelegationBridge` | `sealed class` | The facade. Holds the `DelegationOrchestrator`, the `TargetRegistry`, and (optionally) a `SimulationSession`; owns `Tick`. **Zero-touch through Release v1** — no new hot-path logic. |
| `ISimWorldSnapshot` | `interface` | The per-tick **ingress** contract the sim/ECS layer implements. |
| `IOrderSink` | `interface` | The **egress** contract — `ApplyOrder(EntityKey, in Order)` writes an order back into movement / weapons / EW systems. |
| `ObservedStateBuilder` | `static` | Pure map `ISimWorldSnapshot` → `ObservedState` (the orchestrator's input record). |
| `OrderDispatcher` | `static` | Pure map executed `Order`s → `IOrderSink`, resolving `Order.Target` (a `TargetId`) back to the host's `EntityKey` via the registry. |
| `TargetRegistry` | `sealed class` | Dual `EntityKey ↔ TargetId` index; registers targets with the orchestrator and tracks member ids. |
| `SimEntityBinding` | `sealed record` | `(EntityKey Entity, TargetId TargetId, ICommandableTarget Target)` — one row of the registry. |
| `EntityKey` | `readonly record struct` | Opaque host entity id (`int`; e.g. a DOTS entity index). |
| `DelegationTickResult` | `sealed record` | `(ExecutedOrders, DispatchedToSim, EngagementsResolved)` returned by `Tick`. |

The registry's target model (`ICommandableTarget` / `UnitTarget` / `GroupTarget` / `ControllerSlot`)
is documented in [`direct-control-override-runtime.md`](direct-control-override-runtime.md); this
page treats it as a black box and focuses on the adapter plumbing.

---

## The two seams

### Ingress — `ISimWorldSnapshot`

The sim supplies one object per tick. Only three members are required; the rest are default
interface members that additive scenarios opt into.

| Member | Default | Meaning |
|--------|---------|---------|
| `SimTime` | — | Sim time in seconds. The bridge derives `simTick = (ulong)max(0, (long)SimTime)` for the timeline emitters and player-order delay. |
| `ContactCount` | — | Size of the tactical picture (also the HUD top-bar count). |
| `ActiveEngagementCount` | — | In-flight engagements. |
| `IsMemberAlive(TargetId)` | — | Alive state for a **registered** member; return `false` if unknown. |
| `PrimaryHostileContactId` | `null` | Engage/sensor MVP victim (`null` when `ContactCount == 0`). |
| `HasFireControlTrackOnPrimaryContact` | `false`¹ | Fire-control-quality track on the primary hostile. |
| `ObserverRadarEmconActive` | `false`¹ | Observer radar EMCON permits active illumination. |
| `PrimaryHostileDestroyed` | `false` | Lets blue patrol policies pre-filter `Engage` proposals (S57-03). |
| `PrimaryBlueForceContactId` / `PrimaryBlueForceContactDestroyed` | `null` / `false` | Red-side victim selection (Baltic v3). |
| `PreferredHostileByShooter` | `null` | Optional `shooterId → hostileId` map for multi-domain concurrent engage; `null` keeps single-primary MVP behaviour. |

¹ `bool` interface members without an explicit default expression are `false` when not overridden;
`HasFireControlTrackOnPrimaryContact` and `ObserverRadarEmconActive` are **not** default members —
every snapshot must implement them (test stubs return `true`).

### Egress — `IOrderSink`

```csharp
public interface IOrderSink
{
    void ApplyOrder(EntityKey entity, in Order order);
}
```

A single host object may implement **both** seams — the headless snapshot inside
`BalticReplayHarness` does exactly that (see [`baltic-replay-harness.md`](baltic-replay-harness.md)).

---

## The tick fold — `DelegationBridge.Tick(snapshot, sink)`

One `Tick` call is a fixed, deterministic sequence:

```text
Tick(snapshot, sink)
 1. EmitCommsTransitions(snapshot)   ─┐ pre-decision timeline emitters
 2. AdvanceSpoofTimeline(snapshot)    │ (append order-log rows / advance latches
 3. EmitFuelTransitions(snapshot)    ─┘  BEFORE the decision runs)
 4. observed = ObservedStateBuilder.Build(snapshot, Registry.CollectMemberIds())
 5a. if Session != null (MVP engage on):
        Session.Tick(observed)  → false while Planning ⇒ Orchestrator.Tick(observed);
                                                          return ( [], 0 )
        orders   = Orchestrator.ExecutedOrders
        nonEngage = orders where Kind != Engage        ← engage stays inside the session
        dispatched = OrderDispatcher.Dispatch(nonEngage, Registry, sink)
        return ( orders, dispatched, Session.Sim.LastEngagementResults.Count )
 5b. else (no engage session):
        Orchestrator.Tick(observed)
        dispatched = OrderDispatcher.Dispatch(Orchestrator.ExecutedOrders, Registry, sink)
        return ( ExecutedOrders, dispatched )
```

Key behaviours:

- **Planning is a no-op.** Before `BeginExecution()`, the orchestrator is in `SimulationPhase.Planning`; `Tick` executes no orders and dispatches nothing (`ExecutedOrders` empty, sink untouched).
- **Engage routing depends on `Session`.** With the MVP engage session enabled, `Engage` orders are resolved *inside* the session (kill-chain, magazine, DLZ — see the engagement pipeline) and are **not** pushed to `IOrderSink`; only non-engage orders (move, hold, EW, …) reach the sink. Without a session, **all** orders — including `Engage` — are dispatched to the sink, leaving the host to resolve the shot.
- **Order → entity resolution happens at dispatch**, not at decision time. Orders carry a `TargetId`; `OrderDispatcher` resolves it back to the host `EntityKey` through the registry.

### `ObservedStateBuilder.Build` — the field map

Pure, allocation-bounded (one `Dictionary` sized to the member count), no RNG:

| `ObservedState` field | Source |
|-----------------------|--------|
| `SimTime`, `ContactCount`, `ActiveEngagementCount` | direct passthrough |
| `MemberAlive[id]` | `snapshot.IsMemberAlive(id)` for each id in `Registry.CollectMemberIds()` |
| `HasFireControlTrack` | `HasFireControlTrackOnPrimaryContact` |
| `PrimaryHostileContactId`, `RadarEmconActive`, `PrimaryHostileDestroyed` | passthrough (`RadarEmconActive` ← `ObserverRadarEmconActive`) |
| `PrimaryBlueForceContactId`, `PrimaryBlueForceContactDestroyed` | passthrough |
| `PreferredHostileByShooter` | passthrough |

The orchestrator then narrows `ObservedState` to a per-agent `PerceivedState` using each agent's
`SituationalAwareness` trait — that fog-of-war step is in
[`agent-traits-and-attention.md`](agent-traits-and-attention.md).

### `OrderDispatcher.Dispatch` — registry-gated egress

```csharp
foreach (var order in orders)
{
    if (!registry.TryGetBinding(order.Target, out var binding)) continue; // silently skip unknown
    sink.ApplyOrder(binding.Entity, order);
    count++;                                                              // → DispatchedToSim
}
```

Orders whose `TargetId` is not in the registry are **silently skipped** (never applied), so the
returned count can be lower than `ExecutedOrders.Count`.

---

## `DelegationTickResult`

| Field | Meaning |
|-------|---------|
| `ExecutedOrders` | Every order the orchestrator executed this tick (engage + non-engage). |
| `DispatchedToSim` | How many of those actually reached `IOrderSink` — i.e. resolved to a known entity **and** (when a session is active) were non-engage. |
| `EngagementsResolved` | `Session.Sim.LastEngagementResults.Count` (0 when no engage session). |

So with the MVP session on, an all-`Engage` tick yields `DispatchedToSim == 0` while
`EngagementsResolved > 0`; with no session, the same tick dispatches the `Engage` order to the
sink and `EngagementsResolved` stays 0.

---

## `TargetRegistry` — the dual index

The registry keeps `EntityKey → binding` and `TargetId → binding` in lock-step and feeds
`CollectMemberIds()` (the ordered member list consumed by `ObservedStateBuilder` and the fuel
emitter):

| Method | Effect |
|--------|--------|
| `RegisterUnit(EntityKey, key)` | New `UnitTarget`; appends its `TargetId` to `_memberIds`. |
| `RegisterGroup(EntityKey, key)` | New `GroupTarget` (groups are **not** members). |
| `LinkGroupMember(groupId, memberId)` | Adds an already-registered unit to a group; throws if either is unregistered. |
| `TryGetBinding(EntityKey / TargetId)` | Dual lookup used by dispatch and the human-order / direct-control paths. |
| `CollectMemberIds()` | Ordered member ids for the observed-state alive map and fuel drain. |

> **Invariant (qa-r2-08).** `Register` rejects a duplicate **`EntityKey`** *and* a duplicate
> **`TargetId`**. Guarding only `EntityKey` let two entities share a target key, silently
> overwriting `_byTarget` and appending a duplicate id into `_memberIds` — which flows straight
> into the OOB tree / map picture (a unit rendered twice). Both checks throw
> `InvalidOperationException`.

---

## Pre-decision timeline emitters

Before the decision runs, `Tick` advances three optional scenario timelines so their state is in
effect for the *same* tick. Each is `null` unless the scenario policy enables it, and each only
appends to the order log / advances a latch — none influence order execution directly:

| Emitter | What it does | Deep-dive |
|---------|--------------|-----------|
| `EmitCommsTransitions` | Drains `CommsTimelineSimulator` → `CommsStateChange` log rows (`CurrentCommsState` then gates player-order delay, staleness, datalink share, `Denied` engage block). | [`comms-degradation-runtime.md`](comms-degradation-runtime.md) |
| `AdvanceSpoofTimeline` | Advances the latching `SpoofTrackTimelineSimulator` (`IsSpoofed` → kill-chain abort). | [`comms-degradation-runtime.md`](comms-degradation-runtime.md) |
| `EmitFuelTransitions` | Drains `FuelTimelineTracker` → `FuelBurn` / `FuelStateChange` rows over `CollectMemberIds()`. | [`logistics-fuel-runtime.md`](logistics-fuel-runtime.md) |

> **Fuel `deltaSeconds` (ADR-020 / DRG-50).** The fuel emitter derives elapsed time from the
> *actual* `SimTime` delta (`SimTime - previousSimTime`, or `SimTime` on the first tick), **never**
> a hardcoded `1.0`. Under a 1/60 s host cadence a hardcoded step over-drains ~60×. The baseline
> `_lastFuelSimTime` always advances — including empty-registry, pause, and rewind ticks — so a
> unit registered at `t = N` is never retro-charged for `[0, N)`. Non-positive deltas early-return.

---

## Determinism & invariants

- **`DelegationBridge.cs` is zero-touch through Release v1.** Add host wiring in the sim layer or in a new bridge type — not in `Tick`. ([AGENTS.md → Hard Invariants](../../AGENTS.md#hard-invariants--never-break-these).)
- **The builder and dispatcher are pure** — no RNG, no wall-clock, no static state. All non-determinism lives inside the orchestrator/session (seeded), so the adapter never perturbs the replay hash.
- **`ObservedState` is derived, never authoritative** — the snapshot is a read of sim state; the adapter owns no game state of its own.
- **Egress is idempotent per order** — one executed order → at most one `ApplyOrder`; unknown targets are skipped, not retried.

---

## Extend it

| You want to… | Do this |
|--------------|---------|
| Expose a new sim signal to policies | Add a **default** interface member to `ISimWorldSnapshot` (keeps existing hosts compiling), map it in `ObservedStateBuilder.Build` and `ObservedState`, then consume it in a policy. Confirm the Baltic v2 hash is unchanged. |
| Route a new order kind to the host | It already flows through `OrderDispatcher` — just handle the `OrderKind` in your `IOrderSink`. To keep it out of the sink under an engage session, add it to the engage-resolved set the way `Engage` is excluded. |
| Add a per-tick pre-decision effect | Prefer a new scenario timeline + emitter pattern (comms/spoof/fuel) rather than editing `Tick` — the bridge is zero-touch. |
| Register units/groups from a host | Use `TargetRegistry.RegisterUnit` / `RegisterGroup` / `LinkGroupMember`; respect the dual-uniqueness invariant. |

---

## See also

| Doc | For |
|-----|-----|
| [`src/…/UnityAdapter/README.md`](../../src/ProjectAegis.Delegation.UnityAdapter/README.md) | Project overview + integration quick-start. |
| [agent-decision-pipeline.md](agent-decision-pipeline.md) | What `DelegationOrchestrator.Tick(ObservedState)` does with the built state. |
| [engagement-pipeline.md](engagement-pipeline.md) | The engage half of the fold (`SimulationSession` / `MvpEngagementResolver`). |
| [direct-control-override-runtime.md](direct-control-override-runtime.md) | The `ICommandableTarget` model + `TargetRegistry` host registration. |
| [comms-degradation-runtime.md](comms-degradation-runtime.md) · [logistics-fuel-runtime.md](logistics-fuel-runtime.md) | The pre-decision timeline emitters. |
| [c2-projection-layer.md](c2-projection-layer.md) | The read side (order-log → view models). |
| [determinism-and-replay.md](determinism-and-replay.md) | Why the adapter must stay pure. |

## Tests

`src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/` (NUnit):

| Test | Pins |
|------|------|
| `DelegationBridgeTests.Tick_is_no_op_while_planning` | No orders/dispatch before `BeginExecution`. |
| `DelegationBridgeTests.Tick_builds_observed_state_and_dispatches_orders_to_sink` | `DispatchedToSim == ExecutedOrders.Count`; sink receives the resolved `EntityKey`. |
| `DelegationBridgeTests.ObservedStateBuilder_includes_registered_member_alive_flags` | Member alive map wired from `CollectMemberIds()`. |
| `DelegationBridgeTests.ObservedStateBuilder_maps_contact_and_track_from_snapshot` | Snapshot → `ObservedState` field map. |
| `DelegationBridgeTests.TryEnqueueHumanOrder_*` | Human-order enqueue + `AttachReplayViewer` block. |
| `DelegationBridgeSimSessionTests.EnableMvpEngagement_resolves_engages_via_sim_session` | Engage stays in the session (`sink` empty, `EngagementsResolved > 0`). |
| `DelegationBridgeSimSessionTests.Without_sim_session_dispatches_all_orders_including_engage` | No session ⇒ `Engage` reaches the sink. |
