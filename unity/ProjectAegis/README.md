# Unity integration — Project Aegis Delegation

This folder is **scaffolding** until a full Unity project exists under `src/` or a separate game repo.

## 1. Build assemblies

From the repository root:

```bash
dotnet build ProjectAegis.sln -c Release
```

Outputs:

- `src/ProjectAegis.Delegation/bin/Release/net8.0/ProjectAegis.Delegation.dll`
- `src/ProjectAegis.Delegation.UnityAdapter/bin/Release/net8.0/ProjectAegis.Delegation.UnityAdapter.dll`

Or run:

```powershell
./tools/copy-delegation-assemblies.ps1
```

## 2. Copy into Unity

In your Unity project:

```text
Assets/Plugins/ProjectAegis/
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

`Runtime/DelegationBridgeHost.cs` is a minimal `MonoBehaviour` that holds a `DelegationBridge` instance for play-mode smoke tests. Wire `ISimWorldSnapshot` / `IOrderSink` from your ECS systems in `Update` or a `SystemBase` tick.

## 5. DOTS notes (future)

- **Read path:** A `SystemBase` (or `ISystem`) gathers contacts/engagements into a struct implementing `ISimWorldSnapshot`, then calls `DelegationBridge.Tick`.
- **Write path:** `IOrderSink` writes to dynamic buffers / command components on entities keyed by `EntityKey.Value` (map to `Entity` in your registry).
- Keep delegation on the **main thread** for v1; burst only the sim physics, not the orchestrator.
