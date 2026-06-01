# Unity integration — Project Aegis Delegation

This folder is **scaffolding** until a full Unity project exists under `src/` or a separate game repo.

## 1. Build assemblies

From the repository root:

```bash
dotnet build ProjectAegis.sln -c Release
```

Outputs:

- `src/ProjectAegis.Sim/bin/Release/net8.0/ProjectAegis.Sim.dll`
- `src/ProjectAegis.Delegation/bin/Release/net8.0/ProjectAegis.Delegation.dll`
- `src/ProjectAegis.Delegation.UnityAdapter/bin/Release/net8.0/ProjectAegis.Delegation.UnityAdapter.dll`

Or scaffold everything (folders, manifest, DLLs, runtime scripts):

```powershell
./tools/init-unity-project.ps1
```

Play Mode checklist: [PLAYMODE-SMOKE.md](PLAYMODE-SMOKE.md).

## 2. Copy into Unity

In your Unity project:

```text
Assets/Plugins/ProjectAegis/
  ProjectAegis.Sim.dll
  ProjectAegis.Delegation.dll
  ProjectAegis.Delegation.UnityAdapter.dll
```

Use **Unity 6** or a version that supports referencing modern .NET assemblies in Plugins. If load fails, enable compatible API compatibility level in Player Settings.

## 3. Implement sim seams

| Unity / DOTS responsibility | Adapter type |
|-----------------------------|--------------|
| Per-tick world snapshot | `ISimWorldSnapshot` |
| Apply orders to entities | `IOrderSink` |
| Entity id mapping | `EntityKey` + `TargetRegistry.RegisterUnit` |

## 4. Optional host component

`Assets/Scripts/Runtime/DelegationBridgeHost.cs` holds a `DelegationBridge` for play-mode tests.

`Assets/Scripts/Runtime/SimplePlayModeSimHost.cs` is a **zero-ECS smoke harness**: add to the same GameObject as `DelegationBridgeHost`; it implements `ISimWorldSnapshot` + `IOrderSink` and calls `RunTick` every frame.

For production, wire `ISimWorldSnapshot` / `IOrderSink` from ECS in `SystemBase` instead.

**Policy (ADR-002):** Use `orchestrator.AssignAgentToTarget(agent, target, effectivePolicy)` to freeze snapshots. `DecisionLog.PolicyDenials` records blocked engages.

**Engage (ADR-002/003):** `DelegationBridgeHost` enables MVP engage by default (`EnableMvpEngagement`). Engage orders resolve in `SimulationSession`; other orders still go to `IOrderSink`.

**Unity packages (6.3 LTS):** Copy [`Packages/manifest.template.json`](Packages/manifest.template.json) into your Unity project's `Packages/manifest.json` and resolve via Package Manager.

## 5. DOTS notes (future)

- **Read path:** A `SystemBase` (or `ISystem`) gathers contacts/engagements into a struct implementing `ISimWorldSnapshot`, then calls `DelegationBridge.Tick`.
- **Write path:** `IOrderSink` writes to dynamic buffers / command components on entities keyed by `EntityKey.Value` (map to `Entity` in your registry).
- Keep delegation on the **main thread** for v1; burst only the sim physics, not the orchestrator.
