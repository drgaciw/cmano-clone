# Unity integration — Project Aegis Delegation

This folder is **scaffolding** until a full Unity project exists under `src/` or a separate game repo.

## 1. Build and copy plugin assemblies

From the repository root:

```powershell
./tools/copy-delegation-assemblies.ps1   # dotnet publish netstandard2.1 → Assets/Plugins/ProjectAegis/
./tools/Test-UnityPluginAssemblies.ps1   # verify all core + transitive DLLs present
```

This publishes `ProjectAegis.Delegation.UnityAdapter` for **netstandard2.1** and copies **all** publish-output DLLs (Delegation, Sim, Data, SQLite, System.Text.Json, etc.). Do **not** copy `net8.0` build outputs into Unity — Unity 6.3 loads netstandard2.1 plugins only.

Or scaffold everything (folders, manifest, DLLs, runtime scripts):

```powershell
./tools/init-unity-project.ps1
```

Headless .NET verification (no Editor):

```powershell
dotnet build ProjectAegis.sln -c Release
./tools/unity/Invoke-ManualQaHeadlessGate.ps1
```

Automated Unity compile + smoke scene (requires Unity 6000.3.14f1):

```powershell
./tools/unity/Invoke-DelegationSmokeSceneSetup.ps1
```

Play Mode checklist: [PLAYMODE-SMOKE.md](PLAYMODE-SMOKE.md).

## 2. Plugin layout

```text
Assets/Plugins/ProjectAegis/
  ProjectAegis.Data.dll
  ProjectAegis.Sim.dll
  ProjectAegis.Delegation.dll
  ProjectAegis.Delegation.UnityAdapter.dll
  (+ SQLite, System.Text.Json, and other transitive dependencies)
```

Plugin DLLs are **gitignored** — run the copy script after every clone or after .NET changes.

Use **Unity 6.3 LTS** (6000.3.x). Player Settings use .NET Standard 2.1 (`apiCompatibilityLevel: 6`).

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

See also: [Delegation ↔ Sim wiring](../../docs/architecture/wiring-delegation-sim-2026-05-29.md).

## 5. DOTS notes (future)

- **Read path:** A `SystemBase` (or `ISystem`) gathers contacts/engagements into a struct implementing `ISimWorldSnapshot`, then calls `DelegationBridge.Tick`.
- **Write path:** `IOrderSink` writes to dynamic buffers / command components on entities keyed by `EntityKey.Value` (map to `Entity` in your registry).
- Keep delegation on the **main thread** for v1; burst only the sim physics, not the orchestrator.
