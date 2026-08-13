# DelegationBridge tick I/O — the sim-world ↔ delegation-core adapter seam

`DelegationBridge` is the **façade** that lets an engine (Unity ECS / DOTS, or any headless host) drive
the engine-agnostic delegation core one tick at a time. This guide covers the **control-path plumbing**
around its `Tick` method — how a per-tick world snapshot becomes an `ObservedState`, how chosen orders
are dispatched back into the sim, and the target/entity book-keeping that ties the two directions
together. It is the **write/control-side complement** to the read-model projection layer documented in
[c2-projection-layer.md](c2-projection-layer.md).

| Direction | Types | Location |
|-----------|-------|----------|
| **Ingress** (sim → core) | `ISimWorldSnapshot` → `ObservedStateBuilder` → `ObservedState` | [`Bridge/`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/) |
| **Egress** (core → sim) | `Orchestrator.ExecutedOrders` → `OrderDispatcher` → `IOrderSink` | [`Bridge/`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/) |
| **Binding registry** | `EntityKey` ↔ `TargetId` ↔ `ICommandableTarget` via `TargetRegistry` / `SimEntityBinding` | [`Bridge/`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/) + [`Delegation/Targets/`](../../src/ProjectAegis.Delegation/Targets/) |

> **Boundary / invariants:**
> - **`DelegationBridge.cs` is zero-touch through Release v1** — no hotpath changes (see [`AGENTS.md`](../../AGENTS.md)).
>   This page documents its collaborators and the tick contract; it does **not** propose edits to the
>   facade's hotpath.
> - **The engine implements the two interfaces, not the core.** `ISimWorldSnapshot` (read the world) and
>   `IOrderSink` (apply orders) are the only seams the host provides. Everything downstream — the
>   orchestrator, decision pipeline, ROE/autonomy gates — is pure and engine-agnostic (ADR-010 §2–3,
>   ADR-007, ADR-001).
> - **Orders only reach the sim through a registered binding.** `OrderDispatcher` silently skips an
>   order whose `Target` has no `TargetRegistry` binding — an unregistered target is a no-op, never a
>   throw on the hotpath.
> - **Determinism is preserved.** The builder copies snapshot fields verbatim (no RNG, no wall-clock);
>   dispatch iterates the orchestrator's already-ordered `ExecutedOrders`. The bridge tick does not
>   contribute to the Baltic v2 replay hash beyond what the orchestrator/session already log.

---

## Types

| Type | Kind | Role |
|------|------|------|
| [`ISimWorldSnapshot`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/ISimWorldSnapshot.cs) | `interface` | Per-tick read view the sim/ECS layer supplies (contacts, engagements, member liveness, primary-contact fire-control/EMCON, multi-domain preferred-hostile map). |
| [`IOrderSink`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/IOrderSink.cs) | `interface` | `ApplyOrder(EntityKey, in Order)` — the sim-side apply hook (movement / engage / EW). |
| [`EntityKey`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/EntityKey.cs) | `readonly record struct` | Opaque engine entity id (e.g. Unity/DOTS entity index). |
| [`SimEntityBinding`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/SimEntityBinding.cs) | `sealed record` | The `(EntityKey, TargetId, ICommandableTarget)` triple that pairs an engine entity with a core target. |
| [`TargetRegistry`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/TargetRegistry.cs) | `sealed class` | Registers units/groups, links group members, and resolves bindings by `EntityKey` or `TargetId`. |
| [`ObservedStateBuilder`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/ObservedStateBuilder.cs) | `static` | Snapshot + member-id list → `ObservedState`. |
| [`OrderDispatcher`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/OrderDispatcher.cs) | `static` | Orders + registry + sink → applied count. |
| [`DelegationTickResult`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationTickResult.cs) | `sealed record` | `(ExecutedOrders, DispatchedToSim, EngagementsResolved)` returned per tick. |
| [`ICommandableTarget`](../../src/ProjectAegis.Delegation/Targets/ICommandableTarget.cs) / [`UnitTarget`](../../src/ProjectAegis.Delegation/Targets/UnitTarget.cs) / [`GroupTarget`](../../src/ProjectAegis.Delegation/Targets/GroupTarget.cs) | `interface` / `sealed class` | The core target abstraction: an `Id`, a `ControllerSlot`, and detach/group semantics. |

---

## Ingress — `ISimWorldSnapshot` → `ObservedStateBuilder`

The host implements [`ISimWorldSnapshot`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/ISimWorldSnapshot.cs)
to expose the current world for one tick: `SimTime`, `ContactCount`, `ActiveEngagementCount`,
`IsMemberAlive(TargetId)`, the primary hostile contact (`PrimaryHostileContactId` +
`HasFireControlTrackOnPrimaryContact` + `PrimaryHostileDestroyed`), `ObserverRadarEmconActive`, and the
optional red-side / multi-domain extensions (`PrimaryBlueForceContactId`,
`PrimaryBlueForceContactDestroyed`, `PreferredHostileByShooter`). The later fields are **default-implemented**
(`false` / `null`) so existing snapshots keep working — additive-only, MVP-preserving.

[`ObservedStateBuilder.Build(snapshot, memberIds)`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/ObservedStateBuilder.cs)
copies those fields verbatim into a core `ObservedState`, materializing a per-member liveness map by
calling `IsMemberAlive` for each id from `TargetRegistry.CollectMemberIds()`. It performs **no
inference and no RNG** — the fog-of-war / perception logic lives downstream in the decision tick (see
[agent-decision-pipeline.md](agent-decision-pipeline.md)). This is the sole translation point from the
engine's mutable world into the immutable value the orchestrator consumes.

---

## Egress — `OrderDispatcher` → `IOrderSink`

After the core decides, [`OrderDispatcher.Dispatch(orders, registry, sink)`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/OrderDispatcher.cs)
walks the chosen `Order` list in order, resolves each order's `Target` to a `SimEntityBinding` via the
registry, and calls `sink.ApplyOrder(binding.Entity, order)` — returning the count actually applied. An
order whose target is **not registered is skipped** (no throw), so a stale or projection-only target can
never crash the tick. The host's `IOrderSink` implementation is where orders become movement, weapons,
or EW effects in the engine.

---

## Binding registry — `TargetRegistry`

[`TargetRegistry`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/TargetRegistry.cs) owns the
bidirectional `EntityKey` ↔ `TargetId` ↔ `ICommandableTarget` mapping and is the single place the host
declares what exists:

- **`RegisterUnit(entity, targetKey)`** / **`RegisterGroup(entity, targetKey)`** create a
  [`UnitTarget`](../../src/ProjectAegis.Delegation/Targets/UnitTarget.cs) /
  [`GroupTarget`](../../src/ProjectAegis.Delegation/Targets/GroupTarget.cs), store the binding under both
  keys, and register the target with the `DelegationOrchestrator`. A registered `UnitTarget` is appended
  to the member-id list that feeds `ObservedStateBuilder` and the read-side projections.
- **Dual-uniqueness guard.** Registration throws if the `EntityKey` **or** the `TargetId` is already
  registered. The `TargetId` check is load-bearing (fix `qa-r2-08-unity-adapter`): without it, two entities
  sharing one target key would silently overwrite the `_byTarget` mapping and push a **duplicate**
  member-id into `CollectMemberIds()`, which flows straight into `OobTreeProjection` /
  `MapPictureBridge` / `UnitDetailBridge` and renders the same unit twice in the C2 OOB tree/map picture.
- **`LinkGroupMember(groupId, memberId)`** adds an already-registered unit to a registered group (throws
  otherwise) and records it once in the member-id list.
- **`TryGetBinding(EntityKey…)` / `TryGetBinding(TargetId…)`** are the lookups `OrderDispatcher` and the
  read-side bridges use; `CollectMemberIds()` is the ordered id list handed to the builder each tick.

`SimEntityBinding` is the immutable triple that carries the target across the seam.
[`ICommandableTarget`](../../src/ProjectAegis.Delegation/Targets/ICommandableTarget.cs) exposes just
`Id`, a `ControllerSlot` (the human/agent arbitration slot — see
[direct-control-override-runtime.md](direct-control-override-runtime.md)), and `IsDetachedFromGroup`.
`UnitTarget` adds detach state (`SetDetached`); `GroupTarget` holds its member list and a
`PendingReplan` flag (set on roster edits like detach/rejoin).

---

## The tick contract — `DelegationBridge.Tick`

[`DelegationBridge.Tick(ISimWorldSnapshot, IOrderSink)`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs)
threads the pieces together (the bridge owns the `TargetRegistry` and the `DelegationOrchestrator`):

1. **Pre-decision emitters** run first — comms transitions, spoof timeline, and fuel transitions — so
   the observed world reflects contested-C2 / logistics state before decisions (see
   [comms-degradation-runtime.md](comms-degradation-runtime.md) and
   [logistics-fuel-runtime.md](logistics-fuel-runtime.md)).
2. **Build** the `ObservedState` from the snapshot + `Registry.CollectMemberIds()`.
3. **Two drive paths:**
   - **With a `SimulationSession`** (headless engagement resolution): `Session.Tick(observed)` runs the
     tick; if it returns `false` (e.g. paused), the orchestrator still ticks and the bridge returns an
     empty result. On success, the bridge dispatches only the **non-`Engage`** orders to the sink (the
     session resolves engagements internally — see [engagement-pipeline.md](engagement-pipeline.md)),
     reporting `Session.Sim.LastEngagementResults.Count` as `EngagementsResolved`.
   - **Without a session:** `Orchestrator.Tick(observed)` then `OrderDispatcher.Dispatch` of **all**
     `ExecutedOrders` to the sink.
4. **Return** a [`DelegationTickResult`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationTickResult.cs)
   = `(ExecutedOrders, DispatchedToSim, EngagementsResolved)`.

Human orders enter through the separate `TryEnqueueHumanOrder` ingress (comms-delay queued), orthogonal
to the agent decision path above. The session variant's per-tick engagement resolution is covered in
[engagement-pipeline.md](engagement-pipeline.md).

---

## Determinism & tests

- **Pure translation.** The builder and dispatcher add no RNG, no wall-clock, and no reordering; they
  mirror the snapshot fields and iterate the orchestrator's ordered output.
- **No-op over unregistered targets** keeps dispatch total but honest — a hostile-only or
  projection-only target simply isn't dispatched.
- **Registry uniqueness** protects the read-side projections from duplicate members.

| Suite | Location | Test methods |
|-------|----------|--------------|
| `TargetRegistryTests` | [`src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/TargetRegistryTests.cs`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/TargetRegistryTests.cs) | 4 |
| `DelegationBridgeTests` | [`src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/DelegationBridgeTests.cs`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/DelegationBridgeTests.cs) | 9 |
| `DelegationBridgeSimSessionTests` | [`src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/DelegationBridgeSimSessionTests.cs`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/DelegationBridgeSimSessionTests.cs) | 3 |
| `DelegationBridgeScenarioPolicyTests` | [`src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/DelegationBridgeScenarioPolicyTests.cs`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/DelegationBridgeScenarioPolicyTests.cs) | 2 |

> Counts are declared test-method counts (NUnit `[Test]`/`[TestCase]`); parameterized cases expand to
> more executed cases at run time. These fixtures also back the PlayMode smoke gate (`PlayModeSmokeHarnessTests`).

Related: [c2-projection-layer.md](c2-projection-layer.md) (the read-model projections this control seam
mirrors, fed from the same registry member-ids) · [agent-decision-pipeline.md](agent-decision-pipeline.md)
(what the `ObservedState` feeds) · [engagement-pipeline.md](engagement-pipeline.md) (the session drive
path's engagement resolve) · [direct-control-override-runtime.md](direct-control-override-runtime.md)
(the `ControllerSlot` on every target).
