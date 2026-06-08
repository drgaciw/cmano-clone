# Simulation Core & Time

> **Status:** In Review (Sprint 19 refresh — S19-06)  
> **Author:** design-system  
> **Last Updated:** 2026-06-08  
> **Implements Pillar:** Simulation fidelity, Determinism  
> **Requirements:** [03-Simulation-Modes.md](../../Game-Requirements/requirements/03-Simulation-Modes.md), [08-Agentic-Architecture.md](../../Game-Requirements/requirements/08-Agentic-Architecture.md)  
> **Architecture:** [architecture.md](../../docs/architecture/architecture.md), [ADR-004](../../docs/architecture/adr-004-tick-pipeline-order.md), [ADR-005](../../docs/architecture/adr-005-dots-sim-core.md)  
> **Engine:** [VERSION.md](../../docs/engine-reference/unity/VERSION.md)  
> **Implementation baseline:** `main` @ `afd2e1a`

> **Quick reference** — Layer: **Foundation** · Priority: **MVP** · Key deps: None · Depended on by: All sim systems

## Summary

The simulation core owns **world time**, **fixed timestep advancement**, **global seed**, and **world-state identity**. It does not decide tactics—that is delegation—but every subsystem shares one clock and one deterministic ordering contract (ADR-004).

## Overview

| Concern | Owner | Notes |
|---------|-------|-------|
| Canonical tick | `SimTickPipeline` | ADR-004 engage-integrated pipeline; wraps core `SimTickRunner` |
| Session entry | `SimulationSession.Tick` | Delegation tick → sim engagement phase; used by Unity bridge and headless harness |
| Headless batch | `BalticReplayHarness`, `BalticBatchRunner` | No render loop; replay-verify CI gate |
| Seed / RNG | `SimSeed`, `SeededRng`, `DeterministicDetectionLoop` | Domain-scoped draws; no wall-clock in sim path |
| World identity | `SimWorldHash`, `DetectionWorldHash` | Unified `WORLD_HASH` + `DETECTION_WORLD_HASH` in replay goldens |
| Checkpoints | `ReplayCheckpointStore`, `ScenarioReplaySettings` | `(simTick, worldHash, fingerprint, lastSequenceId)` |
| ECS host | Unity DOTS (ADR-005) | **P1 deferred** — clock/seed contracts live in plain C# today |

Interactive play, paused planning, and agent-vs-agent batch runs share the **same** pipeline. `ProjectAegis.Sim` has **no** `UnityEngine` references; headless tests run without the editor.

## Player Fantasy

Pause the war for planning, then compress time until the salvo lands—without the simulation “cheating” or drifting from what a replay would show.

## Detailed Rules

### Core Rules

1. **Fixed timestep** `Δt` default **1/60 s** sim-second; configurable per scenario via `SimClock.FixedDeltaSeconds`, never variable within a run.
2. **Sim tick** `simTick` is `ulong`, increments once per full pipeline pass (`SimTickRunner.TickOnce` / `SimTickPipeline.TickOnce`).
3. **Sim time** `simTime = simTick × Δt` (double); displayed to player with scenario start offset.
4. **Global seed** `scenarioSeed` set at scenario load via `SimSeed.FromScenario`; subsystem RNG derives via `SeededRng(seed, domain, entityId, simTick, drawIndex)` or `DeterministicDetectionLoop.RollTick`.
5. **Time compression** scales how many ticks execute per wall-second in interactive mode; headless runs use `TimeCompressionMode.HeadlessBatch` with no render.
6. **Pause / planning** freezes tick advancement: `SimulationSession.Tick` is a no-op while `SimulationPhase.Planning`; queued commands apply on `BeginExecution` in FIFO order.
7. **Mode switch** (Human / Mixed / Agent-vs-Agent per doc 03) does not reset seed or tick; emits `ModeChange` log entry via `DecisionLog.AppendModeChange`.
8. **World state hash** computed at end of tick (and on checkpoint) for replay verify — excludes render-only data. Layers: core → detection → engage → combat outcome (`SimWorldHash.Combine`).
9. **Command queue** drains at delegation step; duplicate commands same tick rejected with log warning (orchestrator contract).
10. **No wall-clock** in sim path: ban `DateTime.Now`, `Time.realtimeSinceStartup` in `ProjectAegis.Sim` and ECS sim systems.

### Time Compression

| Mode | Enum | Ticks per frame |
|------|------|-----------------|
| 1× | `RealTime` | 1 |
| 2×–64× | `Accelerated` | N (cap by frame budget) |
| 256×+ | `Accelerated` (batched) | Batch until budget or target sim time |
| Headless AvA | `HeadlessBatch` | Run until end condition or tick limit |

**P0:** Headless API ships (`BalticReplayHarness`, `BalticBatchRunner`). **P1 deferred:** doc 03 **1000×+** effective throughput profile gate on reference Baltic scenario.

### States — Simulation Run

| State | Entry | Exit |
|-------|-------|------|
| Loading | Scenario selected | Seed fixed, tick 0 |
| Planning | Human mode default / player pause | `BeginExecution` |
| Running | `SimulationPhase.Executing` | End or return to Planning |
| Ended | Victory/timeout | Replay only |

### Interactions

| System | Interface |
|--------|-----------|
| Order Log | `simTick`, `simTime`, `scenarioSeed` on every entry |
| Delegation | `SimulationSession.Tick` → `SimTickPipeline.TickOnce` after engage enqueue |
| Policy / Engage / Sensors | Invoked inside tick pipeline steps 4–8 |
| Unity | `DelegationBridge` mirrors session; `FixedStepSimulationSystemGroup` targets ADR-005 Δt |

## Formulas

### Symbol table

| Symbol | Type | Meaning |
|--------|------|---------|
| `Δt` | `double` | `SimClock.FixedDeltaSeconds` (default 1/60) |
| `simTick` | `ulong` | Monotonic tick counter |
| `simTime` | `double` | `simTick × Δt` |
| `scenarioSeed` | `ulong` | `SimSeed.Value` at load |
| `draw` | `double` | Unit float in [0, 1) from seeded hash |
| `worldHash` | `ulong` | Layer-mixed composite at tick end |

### Seeded draw

```
draw = SeededRng.UnitFloat(scenarioSeed, domainId, entityId, simTick, drawIndex)
```

Domains are fixed enums (`Detection`, `Engage`, `AgentDecision`, …) — never reuse draws across domains.

### World hash (MVP)

```
detectionSubhash = DetectionWorldHash.MixTick(previous, rolls)
engageMix        = xor(engagementId, outcomeCode) per launched shot
worldHash_tick   = SimWorldHash.Combine(coreHash, detectionSubhash, engageMix, killMix)
```

Checkpoints store `(simTick, worldHash_tick, logFingerprint, lastSequenceId)` via `ReplayCheckpointStore.Record`.

### Worked example

Seed `42`, `Δt = 1/60`, after 4 headless ticks on `baltic-patrol`:

- `simTick = 4`, `simTime = 4/60 ≈ 0.067 s`
- `ReplayGoldenSuiteTests` pins `WORLD_HASH` and `DETECTION_WORLD_HASH` lines in `tests/regression/replay-golden-baltic-engage-2026-06-02.txt`
- Repeat run with same seed → identical fingerprint and `WorldHash` (`BalticReplayHarnessWorldHashTests`)

## Edge Cases

| Scenario | Behavior |
|----------|----------|
| Catch-up after long pause | Run multiple ticks per frame until caught up or `maxTicksPerFrame` cap |
| Compression during engagement | Still one ordered log per tick; no merged ticks |
| Mid-scenario seed change | Forbidden |
| Save/load | Restores `simTick`, `scenarioSeed`, RNG stream counters (checkpoint + log tail) |
| Zero entities | Tick still advances for mission timers |
| Planning phase tick call | `SimulationSession.Tick` returns false; `LastWorldHash` unchanged |
| Checkpoint on tick 0 | Skipped — interval uses `simTick % interval == 0` after advancement |

## Dependencies

| System | Direction |
|--------|-----------|
| Order Log | Downstream — timestamps, fingerprint, checkpoints |
| All sim domains | Upstream provider (sensors, engage, policy) |
| Scenario & Mission Editor | Downstream — headless `scenario_simulate_sample` uses seeded stepping |
| Agentic Infrastructure | Downstream — `BalticBatchRunner` CSV export |

## Tuning Knobs

| Knob | Default | Config path |
|------|---------|-------------|
| `fixedDeltaSeconds` | 1/60 | `SimClock` ctor / scenario JSON (future) |
| `maxTicksPerFrame` | 8 (interactive), unlimited (headless) | Unity bridge (future) |
| `checkpointEveryTicks` | 300 | `ScenarioReplaySettings.CheckpointIntervalTicks` |

## Acceptance Criteria

1. **AC-1 (TR-simcore-005):** Same `scenarioSeed` + scenario policy → identical unified `WorldHash` and replay fingerprint on repeat headless runs. **Evidence:** `ReplayGoldenSuiteTests` (6 catalog cases), `BalticReplayHarnessWorldHashTests`, `ReplayGoldenAssertions` (`WORLD_HASH=`, `DETECTION_WORLD_HASH=`); CI replay gate `ReplayGolden|ReplayOrderLog` **7/7 PASS**.
2. **AC-2 (TR-simcore-001):** Each `SimTickPipeline.TickOnce` / `SimTickRunner.TickOnce` advances `SimClock.SimTick` by exactly 1; `SimTime = SimTick × FixedDeltaSeconds`. **Evidence:** `SimTickRunnerTests`, `SimTickPipelineTests`.
3. **AC-3 (TR-simcore-002):** Same `SimSeed` + sorted trial list + `simTick` → identical detection rolls and draws. **Evidence:** `DeterministicDetectionLoopTests`, `SimTickRunnerTests` (seed divergence).
4. **AC-4 (TR-simcore-003):** Headless runner API executes without Unity player loop. **Evidence:** `BalticReplayHarnessTests`, `BalticBatchRunnerTests`, `SimulationSessionPhaseTests`, `ReplayGoldenSuiteTests`.
5. **AC-5:** Planning phase (pause) does not advance sim tick or world hash. **Evidence:** `SimulationSessionPhaseTests.Tick_is_no_op_while_planning`.
6. **AC-6 (TR-simcore-004):** `TimeCompressionMode.HeadlessBatch` used in headless paths; mode parameter does not change hash sequence for same tick count. **Evidence:** `SimTickRunnerTests` (`HeadlessBatch`, 60 ticks), `BalticReplayHarness` loop.
7. **AC-7 (TR-simcore-005):** Checkpoints record `(SimTick, WorldHash, logFingerprint, LastSequenceId)` at `ScenarioReplaySettings.CheckpointIntervalTicks`. **Evidence:** `ReplayGoldenBalticCheckpointTests`, `ReplayCheckpointStore`, `ScenarioReplaySettings`.
8. **AC-8:** Same global seed → identical delegation order stream across runs (mode/config held constant). **Evidence:** `OrchestratorTests.Two_ticks_same_seed_produce_identical_executed_orders`, `SimulationModeConfiguratorTests`.
9. **AC-9:** `ProjectAegis.Sim` builds and tests pass with no `UnityEngine` assembly reference. **Evidence:** `dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj`; PlayMode regression **7/7** via `PlayModeSmokeHarnessTests` (bridge uses `SimulationSession`).

## Implementation Mapping

| Concept | Code path | Test path |
|---------|-----------|-----------|
| Canonical tick pipeline | `src/ProjectAegis.Sim/Core/SimTickPipeline.cs` | `src/ProjectAegis.Sim.Tests/Core/SimTickPipelineTests.cs` |
| Core tick scaffold | `src/ProjectAegis.Sim/Core/SimTickRunner.cs` | `src/ProjectAegis.Sim.Tests/Core/SimTickRunnerTests.cs` |
| Session orchestration | `src/ProjectAegis.Delegation/Orchestration/SimulationSession.cs` | `src/ProjectAegis.Delegation.Tests/Orchestration/SimulationSessionPhaseTests.cs`, `SimulationSessionMvpTests.cs` |
| Headless harness | `src/ProjectAegis.Delegation.UnityAdapter/Baltic/BalticReplayHarness.cs` | `src/ProjectAegis.Delegation.UnityAdapter.Tests/Baltic/BalticReplayHarnessTests.cs`, `BalticReplayHarnessWorldHashTests.cs` |
| Batch runner | `src/ProjectAegis.Delegation.UnityAdapter/Baltic/BalticBatchRunner.cs` | `src/ProjectAegis.Delegation.UnityAdapter.Tests/Baltic/BalticBatchRunnerTests.cs` |
| Replay golden gate | `src/ProjectAegis.Delegation.UnityAdapter.Tests/Baltic/ReplayGoldenSuiteTests.cs` | `tests/regression/replay-golden-baltic-*.txt` |
| Global seed | `src/ProjectAegis.Sim/Core/SimSeed.cs` | `SimTickRunnerTests`, `DeterministicDetectionLoopTests` |
| Domain RNG | `src/ProjectAegis.Sim/Core/SeededRng.cs` | `src/ProjectAegis.Sim.Tests/Sensors/DeterministicDetectionLoopTests.cs` |
| Detection loop | `src/ProjectAegis.Sim/Sensors/DeterministicDetectionLoop.cs` | `DeterministicDetectionLoopTests` |
| Sim clock | `src/ProjectAegis.Sim/Time/SimClock.cs` | `SimTickRunnerTests` |
| Time compression | `src/ProjectAegis.Sim/Time/TimeCompressionMode.cs` | `SimTickRunnerTests` (`HeadlessBatch`) |
| World hash layers | `src/ProjectAegis.Sim/Core/SimWorldHash.cs`, `src/ProjectAegis.Sim/Sensors/DetectionWorldHash.cs` | `src/ProjectAegis.Sim.Tests/Core/SimWorldHashTests.cs`, `ReplayGoldenAssertions.cs` |
| Checkpoints | `src/ProjectAegis.Delegation/Replay/ReplayCheckpointStore.cs`, `src/ProjectAegis.Sim/Scenario/ScenarioReplaySettings.cs` | `ReplayGoldenBalticCheckpointTests.cs`, `ReplayGoldenBalticKillCheckpointTests.cs` |
| Unity bridge | `src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs` | `src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/PlayModeSmokeHarnessTests.cs` (7/7) |

## TR IDs

| ID | Requirement | Implementation evidence | Status |
|----|-------------|-------------------------|--------|
| TR-simcore-001 | Fixed timestep | `SimClock`, `SimTickRunner`, `SimTickPipeline` | **Covered** — `SimTickRunnerTests`, `SimTickPipelineTests` |
| TR-simcore-002 | Global seed + domain RNG | `SimSeed`, `SeededRng`, `DeterministicDetectionLoop` | **Covered** — `DeterministicDetectionLoopTests` |
| TR-simcore-003 | Headless runner API | `BalticReplayHarness`, `BalticBatchRunner`, `SimulationSession` | **Covered** — `ReplayGoldenSuiteTests`, `BalticBatchRunnerTests` |
| TR-simcore-004 | Time compression hooks | `TimeCompressionMode`, headless harness loop | **Partial** — `HeadlessBatch` wired; interactive multi-tick catch-up **P1** |
| TR-simcore-005 | World hash per tick/checkpoint | `SimWorldHash`, `DetectionWorldHash`, `ReplayCheckpointStore` | **Covered** — `ReplayGoldenSuiteTests`, checkpoint golden files |

## GitNexus (2026-06-08)

| Symbol | Risk | Action before edit |
|--------|------|-------------------|
| `SimulationSession` | **HIGH** | `npx gitnexus impact --repo cmano-clone -d upstream SimulationSession` |
| `SimTickPipeline` | **HIGH** | Verify ADR-004 ordering + replay goldens before edits |
| `SimTickRunner` | **LOW** | Core scaffold; wrapped by `SimTickPipeline` |
| `DelegationBridge` | **HIGH** | Indirect — session host; run PlayMode 7/7 after changes |

## Open Questions

1. **Default `Δt`:** 1/60 vs 1/10 for theater-scale — **deferred P1** (performance study; current default 1/60 in `SimClock`).
2. **Checkpoint cadence:** Tied to order-log GDD default (300 ticks) — **implemented** via `ScenarioReplaySettings`; engagement-triggered checkpoints **deferred P1**.
3. **1000×+ headless throughput:** Doc 03 profile gate on Baltic reference — **deferred P1** (no CI throughput gate on `main` @ `afd2e1a`).
4. **Full Unity DOTS ECS bridge:** ADR-005 entity storage migration — **deferred P1** (`ProjectAegis.Sim.DOTS` not started; plain C# kernel is canonical today).
5. **Long-horizon hash regression (tick 3600+):** Extend pinned goldens beyond 3–6 tick slices — **deferred P1** (determinism proven at pinned counts; scale test not CI-blocked).