# Unity DOTS/ECS — Project Aegis Notes (6.3 LTS)

**Last verified:** 2026-05-29 · **Editor:** 6000.3.x

## Assembly split

| Assembly | Unity dependency | Contents |
|----------|------------------|----------|
| `ProjectAegis.Sim` | **None** | Policy interfaces, tick contracts, deterministic helpers |
| `ProjectAegis.Sim.DOTS` | Entities + Burst | ECS systems, worlds (future) |
| `ProjectAegis.Unity` | Full editor | Presentation, bootstrap |

## Fixed timestep

- Systems: `[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]`
- Default step: 1/60 s; override via `FixedStepSimulationSystemGroup.Timestep`
- Batch/replay: `World.SetTime` / `PushTime` — do not rely on wall clock
- ECB: `BeginFixedStepSimulationEntityCommandBufferSystem` at step boundaries

## Headless / agent-vs-agent

- Build: **Dedicated Server** (`UNITY_SERVER`) strips render/input loop
- Bootstrap: `ICustomBootstrap` — separate server vs client worlds
- Filters: `[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]` on sim systems
- **No Entities Graphics** on server world

## Burst determinism

```csharp
[BurstCompile(FloatMode = FloatMode.Deterministic)]
```

Managed code and float order outside Burst can still diverge — keep policy/engage rules in tested pure C# or deterministic Burst paths.

## Migration from 2022 LTS Entities 1.0–1.3

1. `Entities.ForEach` → `IJobEntity` or `SystemAPI.Query`
2. `IAspect` → explicit component queries
3. `GetRefRWOptional` → `TryGetRefRW`
4. ECB `AtRecord` → prefer `AtPlayback` (audit semantics)
5. Async `ToEntityArrayAsync` → `ToEntityListAsync`

## Gotcha

Dedicated Server optimizations **do not** apply to Entities subscenes / Addressables content — plan server memory for subscene-heavy scenarios.
