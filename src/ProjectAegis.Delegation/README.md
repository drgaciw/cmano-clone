# ProjectAegis.Delegation

Engine-agnostic C# library implementing the **Agent Delegation Framework** for Project Aegis.

**Design spec:** `docs/superpowers/specs/2026-05-28-agent-delegation-framework-design.md`

## What this library provides

- **Controller / possession model** — `HumanController` and `AgentController` emit shared `Order` objects
- **Unit + group delegation** with detach-and-rejoin override semantics
- **Attention / bandwidth** with graceful degradation under overload
- **Trait + stochastic** decision pipeline (seeded, deterministic)
- **Six personality presets** as data (`PersonalityCatalog`)
- **Autonomy levels** (Manual → Full Autonomous) and ROE gating
- **Decision log** stream for AAR / explainability

## Public seams (for neighboring systems)

| Seam | Type |
|------|------|
| Sim | `ObservedState`, `PerceivedState` — future ECS/sim feeds these |
| Policy | `IPolicy.GenerateCandidates(...)` — future decision engine plugs in |
| ROE | `IRoeFilter` — scenario rules of engagement |
| Trust | `AgentExperienceBlob` — emit-only hook for future campaign XP |
| Orchestration | `DelegationOrchestrator.Tick(...)` — main entry point |

## Build, test, and demo

Requires [.NET 8 SDK](https://dotnet.microsoft.com/download). From the repo root:

```bash
dotnet test ProjectAegis.sln -v minimal
dotnet run --project src/ProjectAegis.Delegation.Demo
```

`SimulationModeConfigurator` applies Human / Mixed / Agent-vs-Agent controller assignments per `SimulationModeProfile` (doc 03).

## Unity integration

Use **`ProjectAegis.Delegation.UnityAdapter`** — implements `ISimWorldSnapshot` (sim → delegation) and `IOrderSink` (orders → sim). See `src/ProjectAegis.Delegation.UnityAdapter/README.md` and `unity/ProjectAegis/README.md`.

```bash
dotnet test ProjectAegis.sln -v minimal
./tools/copy-delegation-assemblies.ps1   # copies Release DLLs for Unity Plugins
```
