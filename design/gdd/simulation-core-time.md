# Simulation Core & Time

> **Status:** In Review  
> **Author:** design-system  
> **Last Updated:** 2026-05-29  
> **Implements Pillar:** Simulation fidelity, Determinism  
> **Requirements:** [03-Simulation-Modes.md](../../Game-Requirements/requirements/03-Simulation-Modes.md), [08-Agentic-Architecture.md](../../Game-Requirements/requirements/08-Agentic-Architecture.md)  
> **Architecture:** [architecture.md](../../docs/architecture/architecture.md), [ADR-004](../../docs/architecture/adr-004-tick-pipeline-order.md), [ADR-005](../../docs/architecture/adr-005-dots-sim-core.md)  
> **Engine:** [VERSION.md](../../docs/engine-reference/unity/VERSION.md)

> **Quick reference** — Layer: **Foundation** · Priority: **MVP** · Key deps: None · Depended on by: All sim systems

## Summary

The simulation core owns **world time**, **fixed timestep advancement**, **global seed**, and **world-state identity**. It does not decide tactics—that is delegation—but every subsystem shares one clock and one deterministic ordering contract.

## Overview

`ProjectAegis.Sim` exposes a headless-capable **tick runner** that advances the battlespace in fixed steps. Interactive play, paused planning, and 1000× agent-vs-agent batch runs use the **same** pipeline (ADR-004). Unity DOTS hosts entity storage (ADR-005); clock and seed contracts live in plain C# so tests run without the editor.

## Player Fantasy

Pause the war for planning, then compress time until the salvo lands—without the simulation “cheating” or drifting from what a replay would show.

## Detailed Design

### Core Rules

1. **Fixed timestep** `Δt` default **1/60 s** sim-second; configurable per scenario, never variable within a run.
2. **Sim tick** `simTick` is `ulong`, increments once per full pipeline pass (steps 1–10 in architecture.md).
3. **Sim time** `simTime = simTick × Δt` (double); displayed to player with scenario start offset.
4. **Global seed** `scenarioSeed` set at scenario load; all subsystem RNG derives via `SeededRng(seed, domain, entityId)`.
5. **Time compression** scales how many ticks execute per wall-second in interactive mode; headless runs max compression with no render.
6. **Pause** freezes tick advancement; queued commands apply on resume in FIFO order.
7. **Mode switch** (Human / Mixed / Agent-vs-Agent per doc 03) does not reset seed or tick; emits `ModeChange` log entry.
8. **World state hash** computed at end of tick (or on checkpoint) for replay verify — excludes render-only data.
9. **Command queue** drains at step 1; duplicate commands same tick rejected with log warning.
10. **No wall-clock** in sim path: ban `DateTime.Now`, `Time.realtimeSinceStartup` in `ProjectAegis.Sim` and ECS sim systems.

### Time Compression

| Mode | Wall clock | Ticks per frame |
|------|------------|-----------------|
| 1× | Real-time aligned | 1 |
| 2×–64× | Accelerated | N (cap by frame budget) |
| 256×+ | Fast-forward | Batch until budget or target sim time |
| Headless AvA | Ignored | Run until end condition or tick limit |

**P0:** Headless must achieve doc 03 target (1000×+ effective) by skipping render and batching ticks.

### States — Simulation Run

| State | Entry | Exit |
|-------|-------|------|
| Loading | Scenario selected | Seed fixed, tick 0 |
| Running | Play / batch start | Pause or end |
| Paused | Player pause | Resume |
| Ended | Victory/timeout | Replay only |

### Interactions

| System | Interface |
|--------|-----------|
| Order Log | `simTick`, `simTime`, `scenarioSeed` on every entry |
| Delegation | Tick step 6; reads `ISimWorldSnapshot` after step 5 |
| Policy / Engage / Sensors | Invoked inside tick runner steps 4–8 |
| Unity | `FixedStepSimulationSystemGroup` mirrors `Δt` |

## Formulas

### Seeded draw

```
draw = HashToUnitFloat(scenarioSeed, domainId, entityId, simTick, drawIndex)
```

Domains are fixed enums (`Detection`, `Engage`, `AgentDecision`, …) — never reuse draws across domains.

### World hash (MVP)

```
worldHash_tick = SHA256( canonical bytes of all sim-relevant entity state sorted by entityId )
```

Checkpoints store `(simTick, worldHash_tick)`.

## Edge Cases

| Scenario | Behavior |
|----------|----------|
| Catch-up after long pause | Run multiple ticks per frame until caught up or cap |
| Compression during engagement | Still one ordered log per tick; no merged ticks |
| Mid-scenario seed change | Forbidden |
| Save/load | Restores `simTick`, `scenarioSeed`, RNG stream counters |
| Zero entities | Tick still advances for mission timers |

## Dependencies

| System | Direction |
|--------|-----------|
| Order Log | Downstream — timestamps |
| All sim domains | Upstream provider |
| Scenario & Mission Editor | Downstream — headless `scenario_simulate_sample` uses seeded `World.SetTime` stepping |

## Tuning Knobs

| Knob | Default |
|------|---------|
| `fixedDeltaSeconds` | 1/60 |
| `maxTicksPerFrame` | 8 (interactive), unlimited (headless) |
| `checkpointEveryTicks` | 300 |

## Acceptance Criteria

1. Same `scenarioSeed` + command script → identical `worldHash` at tick 3600 (headless).
2. Interactive pause does not advance `simTick`.
3. Mode switch without new orders → same outcomes as uninterrupted run (regression test).
4. `ProjectAegis.Sim` tests pass with no UnityEngine reference.
5. Compression 1000× headless meets doc 03 throughput on reference Baltic scenario (profile gate).

## Implementation Mapping

| Concept | Code (scaffolded) |
|---------|-------------------|
| Tick runner | `ProjectAegis.Sim.Core.SimTickRunner` |
| Seed | `ProjectAegis.Sim.Core.SimSeed` |
| Clock | `ProjectAegis.Sim.Time.SimClock` |
| ECS bridge | Future `ProjectAegis.Sim.DOTS` |

## TR IDs

| ID | Requirement |
|----|-------------|
| TR-simcore-001 | Fixed timestep |
| TR-simcore-002 | Global seed + domain RNG |
| TR-simcore-003 | Headless runner API |
| TR-simcore-004 | Time compression hooks |
| TR-simcore-005 | World hash per tick/checkpoint |

## Open Questions

1. Default `Δt`: 1/60 vs 1/10 for theater-scale (performance study).
2. Checkpoint cadence: tie to order-log GDD default (300 ticks).
