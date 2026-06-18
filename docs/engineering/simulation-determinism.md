# Simulation Determinism & Tick Pipeline

> **Scope:** `ProjectAegis.Sim` deterministic core — fixed-timestep tick pipeline,
> seeded RNG, and the layered world-state hash.
> **ADR:** [ADR-004 — Deterministic Tick Pipeline Ordering](../architecture/adr-004-tick-pipeline-order.md),
> [ADR-002 — Policy Evaluator](../architecture/adr-002-policy-evaluator.md),
> [ADR-005 — DOTS Sim Core](../architecture/adr-005-dots-sim-core.md)
> **Requirements:** req 03 (order log), req 04 (reproducibility)
> **Last updated:** 2026-06-18

The simulation is a **deterministic wargame**: a given `(scenario, seed)` must
produce the identical order log and end-state on every run and machine. This is
the core invariant the whole engine rests on — controllers are pure functions of
`(observed state, traits, seed)`. This runbook explains how that determinism is
engineered in `ProjectAegis.Sim` so you can extend the sim without breaking it.

> The danger of non-determinism is that it is **invisible until it isn't** — every
> unit test can pass while a replay diverges on a different machine or a different
> day. Read this before adding any randomness, collection iteration, or new tick
> phase.

---

## The deterministic boundary

Not all code must be deterministic — only the **sim path**. Code that builds view
models, renders, or reads wall-clock time is presentation and lives outside the
boundary.

**Must be deterministic** (no wall-clock, no unordered iteration, no unseeded RNG):

- `src/ProjectAegis.Sim/Core/` — clock, tick runner, RNG, world hash
- `src/ProjectAegis.Sim/Engage/`, `Policy/`, `Sensors/`, `Logistics/`, `Scenario/`
- delegation controllers/policy on the tick path (`ProjectAegis.Delegation`)

**Outside the boundary** — UI snapshots (tick step 11), telemetry observers
(see [Balance Drift Telemetry](balance-drift-telemetry.md)), and tooling.

---

## Fixed-timestep tick pipeline

One tick is a fixed, ordered sequence. The canonical order is the table in
[`architecture.md` § Fixed Timestep Tick Pipeline](../architecture/architecture.md);
the runtime contract is [`ISimTickRunner`](../../src/ProjectAegis.Sim/Core/ISimTickRunner.cs):

| Step | System | Output |
|------|--------|--------|
| 1 | Ingest player/MCP commands | Command queue |
| 2 | Apply mission timeline / events | Mission state |
| 3 | Movement & kinematics (coarse) | Positions |
| 4 | **Detection tick** (sorted emitter–target pairs) | Contact changes |
| 5 | Build `ObservedState` / `ISimWorldSnapshot` | Per-side picture |
| 6 | **Delegation tick** | `Order` list |
| 7 | **Policy evaluate** each order/intent | Allow / `FireAbortReason` |
| 8 | **Engagement resolve** legal fires | Launches, damage |
| 9 | Logistics (fuel, magazine) | Readiness |
| 10 | **Append order log** | Immutable entries |
| 11 | Optional UI snapshot | View models |

The ordering is the determinism guarantee: detection runs **before** the observed
state is built, policy runs **before** engagement, and logging is **last**. Never
reorder steps or interleave them across phases.

### What is wired today

Two runners implement `ISimTickRunner`:

- [`SimTickRunner`](../../src/ProjectAegis.Sim/Core/SimTickRunner.cs) — the MVP core.
  `TickOnce` advances [`SimClock`](../../src/ProjectAegis.Sim/Time/SimClock.cs) by
  one fixed step and folds `(seed, tick, previousHash)` into `LastWorldHash`.
- [`SimTickPipeline`](../../src/ProjectAegis.Sim/Core/SimTickPipeline.cs) — wraps the
  core and wires **step 8** (engagement). Engagements enqueued via
  `EnqueueEngagement` during a tick are resolved in enqueue order on the **same**
  tick, then `RecomputeWorldHash` folds the engagement and kill mixes into the
  composite hash.

```csharp
var pipeline = new SimTickPipeline(SimSeed.FromScenario(seed), engagementResolver);
pipeline.EnqueueEngagement(new EngageRequest(shooterId, targetId, weaponId, salvo));
pipeline.TickOnce(TimeCompressionMode.HeadlessBatch);
ulong hash = pipeline.LastWorldHash; // identical for identical (seed, inputs)
```

### Time compression does not change results

[`TimeCompressionMode`](../../src/ProjectAegis.Sim/Time/TimeCompressionMode.cs)
(`RealTime`, `Accelerated`, `HeadlessBatch`) only controls how fast wall-clock
maps to ticks — it is **not read** by `SimTickRunner.TickOnce` (the argument is
discarded). Interactive and headless runs share one code path, so a headless
batch replay reproduces an interactive session exactly. Sim time is always
`SimTick * FixedDeltaSeconds` (default `1/60 s`), never `DateTime.Now`.

---

## Seeded RNG: deterministic randomness

All randomness on the sim path derives from the scenario seed — never
`System.Random`, never `Guid.NewGuid()`, never time.

- [`SimSeed`](../../src/ProjectAegis.Sim/Core/SimSeed.cs) — the single global
  scenario seed (`SimSeed.FromScenario(ulong)`).
- [`RngDomain`](../../src/ProjectAegis.Sim/Core/RngDomain.cs) — separates draw
  streams so subsystems don't share/consume each other's sequence:
  `Detection`, `Engage`, `AgentDecision`, `Logistics`, `Combat`.
- [`SeededRng.UnitFloat`](../../src/ProjectAegis.Sim/Core/SeededRng.cs) — a pure
  function returning a stable float in `[0, 1)` from
  `(seed, domain, entityId, simTick, drawIndex)`.

```csharp
double roll = SeededRng.UnitFloat(seed, RngDomain.Detection, entityId, clock.SimTick, drawIndex);
```

Because the draw is a **stateless hash of its coordinates**, the same inputs
always yield the same value — there is no hidden RNG cursor to get out of sync.
The detection loop relies on exactly this: same seed and tick yields an identical
roll sequence (see
[`DeterministicDetectionLoopTests`](../../src/ProjectAegis.Sim.Tests/Sensors/DeterministicDetectionLoopTests.cs)).

**Rules for new RNG:**

- Pick or add an `RngDomain` value; do not reuse another subsystem's domain for
  unrelated draws.
- Feed a **stable** `entityId` (the catalog/scenario id, not an object hash or
  iteration index).
- Increment `drawIndex` per draw within the same `(entity, tick)` so repeated
  draws differ but stay reproducible.

---

## Layered world-state hash

The world hash is the fingerprint that `/replay-verify` compares.
[`SimWorldHash`](../../src/ProjectAegis.Sim/Core/SimWorldHash.cs) folds subsystem
state into one `ulong` in **layers**, tagged so each phase's contribution is
distinct and order-stable:

| Layer constant | Tag | Source |
|----------------|-----|--------|
| `LayerCore` | 1 | clock/seed core mix (`SimTickRunner`) |
| `LayerDetection` | 2 | detection sub-hash (tick step 4) |
| `LayerEngage` | 3 | engagement ids + outcome mix (step 8) |
| `LayerCombatOutcome` | 4 | killed-target registry mix |

`SimTickPipeline.RecomputeWorldHash` calls `SimWorldHash.Combine(core, detection,
engage, kill)` after each tick. `Combine` is **stable for identical inputs** and a
change in **any** layer changes the composite (see
[`SimWorldHashTests`](../../src/ProjectAegis.Sim.Tests/Core/SimWorldHashTests.cs)).

To add a subsystem to the hash: compute a stable mix from its sorted state, add a
new `Layer*` tag constant, and fold it in via `MixLayer` — do not XOR raw values
in without a layer tag (untagged folds collide across phases).

---

## Verifying determinism

Two skills form the **sim merge gate**; neither alone is sufficient:

| Skill | Proves | Method |
|-------|--------|--------|
| [`/determinism-audit`](../../.claude/skills/determinism-audit/SKILL.md) | no known non-deterministic *patterns* exist | static scan |
| [`/replay-verify`](../../.claude/skills/replay-verify/SKILL.md) | the sim actually *reproduces* | runs the sim, diffs order log + world hash |

`/replay-verify` runs a scenario twice in fresh processes and against a stored
golden baseline:

- **A vs B differ** → intra-run non-determinism — **release-blocking**.
- **A == B but A != golden** → behavior drift — a human decides intentional
  (re-record) vs. regression. Never silently re-record a baseline.

Fast local check (full suite includes the determinism tests above):

```bash
dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj -v minimal
```

> A PASS in the headless VM does **not** guarantee cross-platform reproduction —
> flag platform-sensitive float/transcendental math for a target-hardware check.

---

## Common pitfalls

| Pitfall | Why it breaks determinism | Do instead |
|---------|---------------------------|------------|
| `DateTime.Now` / `Stopwatch` on the sim path | wall-clock varies per run | use `SimClock.SimTick` / `SimTime` |
| `new Random()` or `Guid.NewGuid()` | unseeded, machine-dependent | `SeededRng.UnitFloat` with an `RngDomain` |
| Iterating a `Dictionary`/`HashSet` to drive logic | enumeration order is not guaranteed | sort by a stable key first (detection sorts observer→sensor→target) |
| Order-dependent float accumulation | re-association changes the sum | fix the iteration order; sort before reducing |
| `Parallel.For` / unawaited tasks mutating sim state | thread races | keep the sim path single-threaded or fully ordered |
| Reusing another subsystem's `RngDomain` | one stream's draws desync the other | give each draw stream its own domain |

When a replay diverges, run `/replay-verify <scenario> --bisect` to localise the
first divergent tick, then `/determinism-audit` on the implicated namespace.

---

## Checklist: adding a randomized or stateful sim subsystem

1. Place it inside the deterministic boundary (`src/ProjectAegis.Sim/...`).
2. Derive all randomness from `SeededRng.UnitFloat` with a dedicated `RngDomain`.
3. Sort any collection you iterate by a stable key before using it for logic.
4. Document its tick **step number** (ADR-004) and run it in pipeline order.
5. Fold its state into `SimWorldHash` under a new tagged layer.
6. Add a "same seed → identical sequence" test (mirror
   `DeterministicDetectionLoopTests`).
7. Run `/determinism-audit` then `/replay-verify` before merging.
