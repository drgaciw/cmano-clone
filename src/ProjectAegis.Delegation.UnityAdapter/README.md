# ProjectAegis.Delegation.UnityAdapter

Thin bridge between **Unity / DOTS sim** and `ProjectAegis.Delegation`. No `UnityEngine` reference — safe to unit test with `dotnet test`.

## Data flow

```text
ECS / sim systems
    │  implement ISimWorldSnapshot (contacts, engagements, member alive)
    ▼
DelegationBridge.Tick(snapshot, orderSink)
    │  ObservedStateBuilder → DelegationOrchestrator.Tick
    ▼
IOrderSink.ApplyOrder(entityKey, order)  →  your movement / weapons / EW systems
```

## Quick start (C# / tests)

```csharp
var bridge = new DelegationBridge(globalSeed: 42);
var friendly = bridge.Registry.RegisterUnit(new EntityKey(1), "friendly-1");
var opposing = bridge.Registry.RegisterUnit(new EntityKey(2), "opposing-1");

bridge.ConfigureSimulationMode(
    new SimulationModeProfile(SimulationModeKind.Mixed, PlayerControlsFriendlySide: true),
    friendly: [friendly.Target],
    opposing: [opposing.Target],
    PersonalityCatalog.All[0].Traits);

bridge.Tick(mySnapshot, myOrderSink);
```

Player orders while in human control:

```csharp
bridge.TryEnqueueHumanOrder(new EntityKey(1), OrderKind.Hold, simTime: sim.Clock);
```

## Unity Editor wiring

See `unity/ProjectAegis/README.md` — build DLLs, copy to `Plugins`, optional `DelegationBridgeHost` MonoBehaviour.

## Deep reference

See [`docs/engineering/delegation-unity-bridge.md`](../../docs/engineering/delegation-unity-bridge.md) for the full contract and tick-pipeline reference: the `ISimWorldSnapshot` / `IOrderSink` contracts, MVP-engage vs delegation-only dispatch, player-order injection, the read-only C2 presentation seam, determinism rules, and common pitfalls.
