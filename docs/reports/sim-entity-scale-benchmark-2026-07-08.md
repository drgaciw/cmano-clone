# Sim Entity-Scale Benchmark & Gap Report (INF-5.1)

**Date:** 2026-07-08
**Author:** benchmark harness (`src/ProjectAegis.Sim.Benchmark`) + analysis
**Branch:** `unity-integration-review`
**Requirement:** INF-5.1 — *"Headless benchmark records entity count, ticks executed, and wall-clock duration per run (CSV or JSON artifact)."*
**NFR under test:** doc 01 / doc 03 / doc 08 §2 — **25,000 entities @ 1000×+ headless** (and 5,000 @ 60 FPS interactive).

---

## 1. TL;DR

- A headless, managed benchmark now exists (`ProjectAegis.Sim.Benchmark`), emits CSV/JSON, and is
  unit-tested. **INF-5.1 measurement infrastructure: delivered.**
- **The 25k @ 1000× NFR cannot be honestly *measured* yet, because the sim performs no per-entity
  work per tick.** `SimTickRunner.TickOnce` is O(1) (clock advance + world-hash mix); `SimTickPipeline`
  adds work only per *pending engagement*; `BalticReplayHarness` runs a fixed ~handful-unit ORBAT.
  There is no code path that processes N entities per tick, so throughput is independent of entity count.
- What the benchmark *can* measure — the **time-advance ceiling** — is ~**457,000× realtime**. That is
  the entire budget the (unbuilt) per-entity workload must fit inside. Deriving from it shows the target
  is **brutally tight**: **~0.67 ns/entity/tick single-threaded at 25k @ 1000×** (~2 CPU cycles).
- Conclusion: 25k @ 1000× is reachable **only** with heavy managed parallelism + SIMD, and only once a
  data-oriented per-entity tick is built. This report quantifies that budget so that work can be scoped
  against a hard number instead of a hope.

---

## 2. Method

`dotnet run -c Release --project src/ProjectAegis.Sim.Benchmark -- --ticks 10000000 --reps 3 --warmup 500000 --budget`

- **Mode `core-tick`:** loops `SimTickPipeline.TickOnce(HeadlessBatch)` with a `StubEngagementResolver`
  and no pending engagements — i.e. the sim's real *fixed* per-tick cost.
- Warm-up ticks excluded; wall-clock via `Stopwatch`; fixed dt = 1/60 s (`SimClock`).
- **Entity count is recorded as `0`** for `core-tick` — an honest statement that no per-entity work runs,
  not an aspirational figure.

**Environment (baseline run):** 13th Gen Intel Core i7-13850HX (28 logical cores), .NET SDK 8.0.422,
Release, single-threaded. Absolute numbers are machine-dependent; the *ratios* and the derived budget
are what matter.

---

## 3. Baseline result

| mode | entity_count | total_ticks | wall_ms | ticks/s | sim_seconds | effective_realtime_multiple |
|------|-------------:|------------:|--------:|--------:|------------:|----------------------------:|
| core-tick | 0 | 30,000,000 | 1,093.6 | **27,432,770** | 500,000 | **457,213×** |

Fixed per-tick cost ≈ **36.5 ns/tick**. Artifact: `sim-benchmark-baseline.csv` / `.json` (this run,
regenerate locally; not committed as a golden — timings are machine-dependent).

**Reading it:** the sim can *advance time* at ~457,000× realtime. The NFR asks for 1000×. So the raw
tick loop leaves a ~457× headroom factor — all of which must pay for per-entity work across up to 25k
entities.

---

## 4. The derived per-entity budget (the actionable part)

At target multiple *M*, one sim second (60 ticks) must finish in `1/M` wall-seconds, so each tick has a
wall budget of `1e9 / (M × 60)` ns. Subtract the measured ~36.5 ns fixed cost and divide by entity count:

| entities | 256× target (ns/entity/tick, 1 core) | 1000× target (ns/entity/tick, 1 core) |
|---------:|-------------------------------------:|--------------------------------------:|
| 1,000 | 65.1 | 16.6 |
| 5,000 | 13.0 | 3.33 |
| 10,000 | 6.51 | 1.66 |
| **25,000** | **2.60** | **0.67** |

**0.67 ns/entity/tick** at the headline 25k @ 1000× target is ≈ 2 cycles on a 3 GHz core — impossible for
any real per-entity computation single-threaded. Feasibility therefore depends entirely on parallelism:

- 28 cores × 8-wide SIMD ≈ 224× throughput → effective budget ≈ **150 ns/entity/tick** at 25k @ 1000×.
  Enough for simple movement integration; tight for a detection broadphase (which is super-linear unless
  spatially partitioned).
- The lower **256× headless floor** (doc 03) is far more forgiving (~2.6 ns → ~580 ns/entity across
  224×) and is the pragmatic first milestone.

---

## 5. Gap: what must exist before 25k is *measurable*

The benchmark measures what exists. To measure (and then hit) the NFR, the following must be built —
this is the deferred "synthetic/real entity-scale workload" (review §3, Option B):

1. **A data-oriented per-entity tick.** Struct-of-arrays hot state (position, velocity, sensor, magazine),
   iterated per tick. Today entities are `TargetRegistry` bindings with no per-tick numeric workload.
2. **A detection/broadphase pass** that is spatially partitioned (grid/BVH), not O(N²).
3. **Order-stable parallelism** (deterministic reductions) so the managed parallel path keeps the
   golden-replay invariant. Determinism is the hard constraint, not raw speed.
4. **A `--mode entity-scale --entities N` benchmark mode** that drives the above, at which point the
   `entity_count` column becomes non-zero and the budget table above becomes a pass/fail gate.

Until (1)–(3) exist, any "25k @ 1000× achieved" claim would be measuring an empty loop. This report
deliberately avoids that.

---

## 6. Status vs INF-5.x

| AC | Status |
|----|--------|
| **INF-5.1** headless benchmark records entity count / ticks / wall-clock, CSV or JSON | **Done** — `ProjectAegis.Sim.Benchmark`, CSV+JSON, unit-tested (7 tests) |
| INF-5.2 swarm LOD policy flag | Not in scope here |
| INF-5.3 perf suggestions advisory, no silent sim mutation | Honored — benchmark is read-only, mutates nothing |
| INF-5.4 manual QA gate baseline thresholds | Follow-up — wire this artifact into `Invoke-ManualQaHeadlessGate.ps1` once entity-scale mode lands |

**Recommendation:** treat §4 as the acceptance target for the future entity-scale sim work; adopt the
**256× floor at 25k** as the first measurable milestone, then push toward 1000×.
