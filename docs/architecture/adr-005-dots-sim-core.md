# ADR-005: DOTS/ECS for World State

**Status:** ~~Accepted~~ **Superseded** (2026-07-07) — see *Superseded* below and
[unity integration review](../reports/unity-integration-review-2026-07-07.md) §3.  
**Date:** 2026-05-29 (superseded 2026-07-07)

## Superseded (2026-07-07)

**Reversal: world state is NOT in DOTS/ECS — it stays managed and headless-first.**

The Unity Integration Review found this ADR architecturally incompatible with the shipped design:

- The heaviest NFR (25,000 entities @ 1000× headless, doc 08 §2) runs under `dotnet`, **not** the
  Unity player loop — where Burst, Jobs, and Entities do not exist. "Burst for hot paths" cannot
  serve the path that needs throughput most.
- The pure, deterministic `ProjectAegis.Sim` (`netstandard2.1`) plus golden-replay tests are the
  project's strongest asset. Moving world state into ECS would either abandon that headless
  test/replay story or force two bit-identical sims under a permanent determinism-audit burden.
- The interactive 5,000 @ 60 FPS target is a **rendering** problem (map-symbol drawing), addressed
  by Cesium billboards / `BatchRendererGroup` — not an ECS world-state problem.

**Consequences of the reversal:** `com.unity.entities` and `com.unity.entities.graphics` were
removed from the Unity manifest; the managed tick (`SimTickPipeline`) remains the sim core; the
25k @ 1000× target is met with managed data-oriented parallelism (INF-5.1 benchmark pending);
`com.unity.burst` stays only because `com.unity.ai.inference` (Sentis) depends on it.

The original decision is retained below as a historical record.

---

## Context (original, 2026-05-29)

Doc 08 targets 5,000–10,000+ entities with Burst/Jobs.

## Decision

**World state** (units, sensors, weapons, contacts) lives in Unity DOTS/ECS. **Delegation** and **policy** remain plain C# assemblies testable without Unity player loop where possible.

## Consequences

- `ProjectAegis.Sim` splits: pure rules vs `Sim.DOTS` bridge (or systems in Unity project).
- Prototype policy evaluator in pure C# first; integrate ECS via snapshot builders.
