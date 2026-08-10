# Sprint 112 — Sim Clock Accel / Pause (DRG-14)

**Dates:** 2026-08-10  
**Program:** Release Product Progress S110–S114  
**Predecessor:** S111 COMPLETE  
**Stage:** **Release** · **Not Launch**  
**Authority:** `production/agentic/agentic-workflow-sprint-series-2026-08-09.md`  
**Linear:** [DRG-14](https://linear.app/drgamtd-workspace/issue/DRG-14)  
**QA:** `production/qa/qa-plan-sprint-112-sim-clock-2026-08-10.md`  
**Kickoff:** `production/agentic/sprint-112-parallel-kickoff-2026-08-10.md`

## Goal

Wire **pause** and **time acceleration** on the tick loop so sim time is player-controllable, deterministic, and session-exposed. Not full auto-pause-on-contact UI (PRD P0-7 residual).

## Tracks (surface-disjoint)

| Track | Story | Surface |
|-------|-------|---------|
| A Clock + runner | S112-01 | `src/ProjectAegis.Sim/Time/**`, `Core/SimTickRunner.cs`, `Core/SimTickPipeline.cs`, `Sim.Tests/**` |
| B Session API | S112-02 | `src/ProjectAegis.Delegation/Orchestration/SimulationSession.cs`, `Delegation.Tests/Orchestration/**` |
| C Optional residual | S112-03 | `DetectionTrialResolver` catalog Modality → trial (from S111 residual) — only if A/B not blocked |

## Must Have

| ID | AC |
|----|-----|
| S112-01 | `SimClock.IsPaused` + Pause/Resume; paused `TickOnce` does not advance SimTick/hash (except HeadlessBatch override documented); Accelerated mode advances `AccelerationFactor` full steps; N accelerated steps ≡ N RealTime steps for hash |
| S112-02 | Session Pause/Resume/SetAccelerationFactor (1..256); session tick path respects clock; tests |
| S112-03 | Suite + Replay 6/6; ZERO DelegationBridge hotpath; smoke closeout |

## Non-goals

Auto-pause UI · attention panel · weapons-release 1x drop · Launch · Phase N · Baltic hash change

## Hard gates

≥1638/0f · Replay 6/6 · stage Release · pause precedence over compression
