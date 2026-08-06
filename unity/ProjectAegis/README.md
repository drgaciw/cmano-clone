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

## 5. Simulation core: managed, not DOTS

The sim is a **managed, headless-first** deterministic tick (`ProjectAegis.Sim`, `netstandard2.1`)
consumed through the `ISimWorldSnapshot` / `IOrderSink` seam above. **Unity DOTS/ECS is not used
for world state** — see [`docs/reports/unity-integration-review-2026-07-07.md`](../../docs/reports/unity-integration-review-2026-07-07.md) §3
for the rationale (the heaviest NFR — 25k @ 1000× headless — runs under `dotnet` where Burst/Jobs
do not exist, and ECS world state would fork the deterministic replay/test story). Accordingly
`com.unity.entities` and `com.unity.entities.graphics` were removed from the manifest. The
interactive 5k-symbol @ 60 FPS target is a **rendering** problem (Cesium billboards /
`BatchRendererGroup`), tracked separately. `com.unity.burst` remains only because
`com.unity.ai.inference` (Sentis) depends on it.

## 6. Assemblies, build & dev tooling

- **Assembly definitions:** runtime scripts compile into `ProjectAegis.Unity.Runtime`
  (`Assets/Scripts/Runtime`), editor tooling into `ProjectAegis.Unity.Editor`, tests into the
  PlayMode `ProjectAegis.Unity.Tests` assembly, and the optional Cesium integration into
  `ProjectAegis.Unity.Runtime.Cesium` (compiled only when `com.cesium.unity` is present).
- **CI build:** `BuildPlayer.PerformBuild` reads `-buildTarget` / `-outputPath` /
  `-scriptingBackend` (default **IL2CPP** for release) / `-buildVersion`, and exits non-zero on
  failure. Example:
  ```bash
  Unity -batchmode -quit -projectPath . -executeMethod BuildPlayer.PerformBuild \
    -buildTarget StandaloneLinux64 -outputPath Builds/Linux64/ProjectAegis -scriptingBackend IL2CPP
  ```
- **CI tests:** `Unity -batchmode -runTests -testPlatform PlayMode -testResults results.xml` runs
  the C2 smoke + `MapSymbolPool` tests headlessly.
- **`com.ivanmurzak.unity.mcp` is a dev-only Editor tool** (the AI/MCP bridge). It logs a benign
  SignalR exception in headless Play Mode when no MCP server is attached; the smoke test ignores it
  via `LogAssert.ignoreFailingMessages`. It is not required for player builds.
