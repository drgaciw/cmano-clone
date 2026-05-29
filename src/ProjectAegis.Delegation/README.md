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

## Build and test

Requires [.NET 8 SDK](https://dotnet.microsoft.com/download).

```bash
dotnet test ProjectAegis.sln -v minimal
```

## Unity integration (future)

Reference this assembly from a Unity project (`Assets/Scripts` or UPM package). Keep simulation logic in DOTS/ECS; call `DelegationOrchestrator` from a thin adapter system that maps entity state to `ObservedState` and applies returned `Order` values.
