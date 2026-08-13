# Sim ↔ delegation bridge — snapshot ingress, order egress & the tick loop

The delegation core ([`ProjectAegis.Delegation`](../../src/ProjectAegis.Delegation/README.md)) is
engine-agnostic: per **ADR-001** it consumes an `ISimWorldSnapshot` and emits `Order` objects, and
per **ADR-010** it never references `UnityEngine`. The **write-path adapter** that connects a sim/ECS
world to that core lives in
[`ProjectAegis.Delegation.UnityAdapter/Bridge/`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/):
`DelegationBridge.Tick` reads one world snapshot, drives the orchestrator, and pushes the resulting
orders back into the sim through an `IOrderSink`.

This guide covers that **ingress → decide → egress** loop and its two boundary contracts. It is the
complement to the **read-side** C2 presentation bridges (`OobTreeBridge` / `MapPictureBridge` /
`MessageLogBridge` / … under the same [`Bridge/`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/)
folder, which project the order log into UI feeds — see [c2-projection-layer.md](c2-projection-layer.md))
and to the engage internals that run inside the tick
([`SimulationSession`](../../src/ProjectAegis.Delegation/Orchestration/SimulationSession.cs);
[engagement-pipeline.md](engagement-pipeline.md)). It is the concrete realisation of the data-flow
sketch in [`AGENTS.md`](../../AGENTS.md).

> **`DelegationBridge.cs` is zero-touch through Release v1** (hard invariant — see `AGENTS.md`). This
> doc describes it; it does not authorise hot-path edits. Everything here is headless .NET 8 (the
> `netstandard`-friendly seam has no `UnityEngine` dependency — ADR-010 §2).

---

## Where it lives

| File | Role |
|------|------|
| [`DelegationBridge.cs`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs) | The facade: constructs the orchestrator + `TargetRegistry` (+ optional `SimulationSession`), and runs the per-tick loop `Tick(snapshot, sink)`. |
| [`ISimWorldSnapshot.cs`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/ISimWorldSnapshot.cs) | **Ingress contract** — the per-tick world picture the sim/ECS layer implements. |
| [`ObservedStateBuilder.cs`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/ObservedStateBuilder.cs) | Maps a snapshot + the registered member ids into the core `ObservedState`. |
| [`IOrderSink.cs`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/IOrderSink.cs) | **Egress contract** — `ApplyOrder(EntityKey, in Order)` back into the sim (movement/engage/EW). |
| [`OrderDispatcher.cs`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/OrderDispatcher.cs) | Resolves each order's `TargetId` → `EntityKey` binding and calls the sink; skips unbound targets. |
| [`DelegationTickResult.cs`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationTickResult.cs) | The tick result: `ExecutedOrders`, `DispatchedToSim`, `EngagementsResolved`. |
| [`EntityKey.cs`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/EntityKey.cs) / [`SimEntityBinding.cs`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/SimEntityBinding.cs) | The opaque sim entity id + the `(EntityKey, TargetId, ICommandableTarget)` binding record. |
| [`TargetRegistry.cs`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/TargetRegistry.cs) | The `EntityKey ↔ TargetId` index + `CollectMemberIds()`. Full host-registration model is documented in [direct-control-override-runtime.md](direct-control-override-runtime.md). |

**ADR anchors:** [ADR-001](../architecture/adr-001-sim-assembly-boundary.md) (sim/delegation boundary),
[ADR-010](../architecture/adr-010-headless-first-command-driven-ui.md) (headless-first, `.NET`-only
seams), [ADR-018](../architecture/adr-018-sensor-side-picture-datalink.md) (the fire-control contract
that `ObservedStateBuilder` preserves).

---

## The two boundary contracts

### Ingress — `ISimWorldSnapshot`

Implemented by the sim/ECS layer (headless test hosts use `SimWorldSnapshotStub`). Every field is a
read of world truth for the current tick — the bridge never writes back through this interface.

| Member | Meaning |
|--------|---------|
| `SimTime` | Current sim time (seconds). Drives comms/spoof/fuel tick math. |
| `ContactCount` / `ActiveEngagementCount` | Attention-load inputs for the decision tick. |
| `IsMemberAlive(TargetId)` | Per registered member liveness (unknown → false). |
| `PrimaryHostileContactId` + `HasFireControlTrackOnPrimaryContact` | The engage/sensor MVP target + its fire-control quality. |
| `ObserverRadarEmconActive` | Whether active radar illumination is allowed (EMCON gate). |
| `PrimaryHostileDestroyed` | Lets patrol policies pre-filter Engage proposals (S57-03; default `false`). |
| `PrimaryBlueForceContactId` + `PrimaryBlueForceContactDestroyed` | Red-side victim selection (Baltic v3; default `null`/`false`). |
| `PreferredHostileByShooter` | Optional shooter→hostile map for multi-domain concurrent engage (default `null` = single-primary MVP). |

New fields are added as **default-valued interface members** (see `PrimaryHostileDestroyed`,
`PreferredHostileByShooter`), so existing hosts keep compiling — the additive pattern that keeps the
boundary stable. `ObservedStateBuilder.Build` copies these fields plus a per-member `alive` dictionary
(over `Registry.CollectMemberIds()`) into the core `ObservedState` the orchestrator consumes.

### Egress — `IOrderSink`

```csharp
public interface IOrderSink { void ApplyOrder(EntityKey entity, in Order order); }
```

`OrderDispatcher.Dispatch(orders, registry, sink)` walks the executed orders in order, resolves each
`order.Target` (a `TargetId`) to a `SimEntityBinding` via the `TargetRegistry`, and calls
`sink.ApplyOrder(binding.Entity, order)` — **skipping orders whose target is not registered** — and
returns the dispatched count. `EntityKey` is the opaque sim/DOTS entity id; the sink translates an
`Order` into movement/weapons/EW effects on that entity.

---

## The tick loop

`DelegationBridge.Tick(ISimWorldSnapshot snapshot, IOrderSink sink)` runs, in order:

1. **Pre-decision timeline emitters** (each a no-op when its scenario block is absent):
   - `EmitCommsTransitions` — drains the [comms timeline](comms-degradation-runtime.md) into
     `CommsStateChange` order-log rows.
   - `AdvanceSpoofTimeline` — advances the latching spoof-track timeline.
   - `EmitFuelTransitions` — drains the [fuel](logistics-fuel-runtime.md) burn/band model. **Delta
     seconds are derived from elapsed `SimTime`, never assumed to be `1.0`** (ADR-020 / DRG-50 —
     hardcoding `1.0` over-drains ~60× under the 1/60 s Play Mode cadence); the baseline advances even
     for an empty registry so a unit registered at `t=N` is never retro-charged for `[0, N]`.
2. **Ingress** — `var observed = ObservedStateBuilder.Build(snapshot, Registry.CollectMemberIds());`
3. **Decide** — one of two paths:
   - **Engage session bound** (`Session != null`): `Session.Tick(observed)`. If it returns `false`
     (e.g. paused / not executing), the orchestrator still ticks and the bridge returns an **empty**
     result with nothing dispatched. Otherwise, only the **non-`Engage`** executed orders are
     dispatched to the sink (engagements are resolved inside the session; see
     [engagement-pipeline.md](engagement-pipeline.md)), and `EngagementsResolved` reflects
     `Session.Sim.LastEngagementResults.Count`.
   - **No session**: `Orchestrator.Tick(observed)` then dispatch **all** executed orders.
4. **Egress** — `OrderDispatcher.Dispatch(...)` (see above).
5. **Result** — `new DelegationTickResult(ExecutedOrders, DispatchedToSim, EngagementsResolved)`.

The decision tick itself (attention, SA fog-of-war, `IPolicy` candidates, the trait-weighted softmax,
the ROE-first autonomy gate, and the order log) is documented in
[agent-decision-pipeline.md](agent-decision-pipeline.md); ROE/WRA gating in
[autonomy-roe-gating.md](autonomy-roe-gating.md).

---

## Player & direct-control ingress (adjacent)

The bridge also exposes non-`Tick` entry points that enqueue human orders or move control between the
player and agents — `TryEnqueueHumanOrder`, `TryIssuePlayerCommand` (CMD-31),
`TryEnqueueAttackOption` (req 14), and `TryTakeDirectControl` / `TryReleaseDirectControl`. These are
thin wrappers that do **not** touch the `Tick` hot path and are all no-ops while
`AttachReplayViewer` is set. The command-resolution seam is documented in
[player-command-issuance.md](player-command-issuance.md) and the controller arbitration in
[direct-control-override-runtime.md](direct-control-override-runtime.md).

---

## Determinism & boundary invariants

- **Read-only ingress.** The bridge only *reads* `ISimWorldSnapshot`; all mutation flows out through
  `IOrderSink`. The snapshot is world truth; the delegation core never writes it back.
- **RNG ownership.** The single seeded RNG lives in the orchestrator/decision pipeline — the bridge,
  `ObservedStateBuilder`, and `OrderDispatcher` are pure glue with no RNG and no wall-clock reads.
- **Deterministic egress.** `OrderDispatcher` iterates the executed-order list in order and resolves
  bindings through the registry; unbound targets are skipped rather than throwing.
- **Additive snapshot growth.** New `ISimWorldSnapshot` members ship as default-valued interface
  members so no existing host breaks and no replay behaviour changes.
- **Zero-touch facade.** `DelegationBridge.cs` stays unchanged through Release v1; the Baltic v2
  replay hash `17144800277401907079` is unaffected by anything on this page.

---

## How to extend

1. **New per-tick world input** — add a **default-valued** member to `ISimWorldSnapshot`, copy it in
   `ObservedStateBuilder.Build`, extend `ObservedState`, and consume it in the orchestrator/policy.
   The default keeps existing hosts and goldens intact.
2. **New order effect** — implement it in your `IOrderSink`; if it is a new `OrderKind`, map it in
   `OrderActionMapper` (see [autonomy-roe-gating.md](autonomy-roe-gating.md)) so gating stays correct.
3. **New host** — implement `ISimWorldSnapshot` + `IOrderSink`, register entities via `TargetRegistry`
   (`RegisterUnit` / `RegisterGroup` — see [direct-control-override-runtime.md](direct-control-override-runtime.md)),
   and call `Tick` each frame. Prove it headlessly before Play Mode.

Verify with the bridge tests below plus the `AGENTS.md` verification block; the PlayMode smoke harness
(`--filter PlayModeSmokeHarnessTests`) exercises this loop end-to-end against a stub host.

---

## Tests

| Test | Covers |
|------|--------|
| [`DelegationBridgeTests`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/DelegationBridgeTests.cs) | Core `Tick` ingress→dispatch, registry binding, no-session path. |
| [`DelegationBridgeSimSessionTests`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/DelegationBridgeSimSessionTests.cs) | Engage-session path (non-engage dispatch, `EngagementsResolved`, pause→empty). |
| [`DelegationBridgeFuelDeltaTests`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/DelegationBridgeFuelDeltaTests.cs) | ADR-020/DRG-50 elapsed-time fuel delta (no `1.0` assumption). |
| [`DelegationBridgeScenarioPolicyTests`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/DelegationBridgeScenarioPolicyTests.cs) / [`DelegationBridgeHostDomainTagsTests`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/DelegationBridgeHostDomainTagsTests.cs) | Scenario-policy binding + host domain tags. |
| [`SimWorldSnapshotStub`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/SimWorldSnapshotStub.cs) | The headless snapshot double used across bridge tests. |

Run: `dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter DelegationBridge`.

---

## Related references

| Where | What |
|-------|------|
| [c2-projection-layer.md](c2-projection-layer.md) | The **read-side** order-log → UI-feed projections (`OobTreeBridge` / `MapPictureBridge` / … mirror this write path). |
| [`SimulationSession.cs`](../../src/ProjectAegis.Delegation/Orchestration/SimulationSession.cs) · [engagement-pipeline.md](engagement-pipeline.md) | The engage session that runs inside the tick when bound. |
| [agent-decision-pipeline.md](agent-decision-pipeline.md) | What `Orchestrator.Tick(ObservedState)` does with the ingress. |
| [player-command-issuance.md](player-command-issuance.md) · [direct-control-override-runtime.md](direct-control-override-runtime.md) | The player-order / direct-control ingress + `TargetRegistry` host registration. |
| [comms-degradation-runtime.md](comms-degradation-runtime.md) · [logistics-fuel-runtime.md](logistics-fuel-runtime.md) | The pre-decision emitters run at the top of `Tick`. |
| [ADR-001](../architecture/adr-001-sim-assembly-boundary.md) · [ADR-010](../architecture/adr-010-headless-first-command-driven-ui.md) | The boundary + headless-first decisions this bridge implements. |
