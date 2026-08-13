# Sim clock, pause & time compression — developer guide

Project Aegis runs on a **fixed-step simulation clock**. The player (and CI) controls *how fast*
sim time advances relative to wall-clock through three time-compression modes and a pause flag —
the classic wargame "1×/accelerate/pause" control. This page documents the small deterministic
seam that owns that behavior: the `SimClock`, the `TimeCompressionMode` enum, the `ISimTickRunner`
tick loop, and the session-level pause/resume/acceleration controls.

The load-bearing property is that **compression is a scheduling decision, not a physics change**:
accelerating never stretches the per-step `Δt`, it just runs more full deterministic steps per call.
So *N* accelerated steps are bit-for-bit identical to *N* real-time steps, and the replay goldens are
untouched.

This runtime was built in **S112** (`S112-01` clock + runner, `S112-02` session API, DRG-14). It is
the mechanism the watch officer's **auto-pause** drives, but it knows nothing about the watch runtime —
the coupling is one-way. This page is verified against source and pinned by the tests listed at the
end.

- **Clock:** [`src/ProjectAegis.Sim/Time/`](../../src/ProjectAegis.Sim/Time/) — `SimClock` (the
  `IsPaused` flag, `AccelerationFactor`, tick counter) and the `TimeCompressionMode` enum.
- **Tick loop:** [`src/ProjectAegis.Sim/Core/`](../../src/ProjectAegis.Sim/Core/) — the
  `ISimTickRunner` seam and its two implementations: `SimTickRunner` (MVP clock+hash) and
  `SimTickPipeline` (the ADR-004 pipeline with detection sub-hash + engagement wired).
- **Session wiring:** [`SimulationSession`](../../src/ProjectAegis.Delegation/Orchestration/SimulationSession.cs)
  — `PauseSim` / `ResumeSim` / `TryResumeSim`, `SetTimeAccelerationFactor`, `IsSimPaused`,
  `TimeAccelerationFactor`, and the interactive-vs-headless tick paths.
- **Related:** the **watch officer** that *drives* pause on detection is a different subsystem —
  see [watch-attention-runtime.md](watch-attention-runtime.md). The per-step deterministic hashing
  contract is [determinism-and-replay.md](determinism-and-replay.md). What actually happens inside a
  pipeline step (detection tick 4, engagement tick 8) is
  [detection-pipeline.md](detection-pipeline.md) and [engagement-pipeline.md](engagement-pipeline.md).

---

## Design invariants — never break these

These are load-bearing and enforced by tests. Preserve them when extending the clock.

| Invariant | Rule |
|-----------|------|
| **Compression ≠ Δt change** | Acceleration advances *more full steps per `TickOnce`*, it does **not** scale `FixedDeltaSeconds`. `SimClock.SimTime` is always `SimTick × FixedDeltaSeconds`. This is what keeps acceleration deterministic. |
| **N accelerated ≡ N real-time** | Running `Accelerated` with `AccelerationFactor = k` once produces the same `SimTick` **and** the same `LastWorldHash` as running `RealTime` `k` times from the same seed. (`TC-CLK-3`, `Pipeline_Accelerated_matches_RealTime_hash`.) |
| **Pause has precedence over compression** | A paused clock is a no-op for *both* `RealTime` and `Accelerated` — pause wins even at `AccelerationFactor = 256`. (`TC-CLK-4`, `PauseSim_blocks_accelerated_Tick`.) |
| **HeadlessBatch overrides pause** | `TimeCompressionMode.HeadlessBatch` advances *regardless* of `IsPaused`, without an explicit `Resume`. This is the CI/batch escape hatch so replay goldens and the QA Gauntlet advance deterministically even when an interactive pause flag is set. (`TC-CLK-5`.) |
| **Pause never strands work** | Pausing does not drop or corrupt in-flight state: the pipeline keeps its pending engagements, and the session must not queue engagements it can't resolve. Resuming continues bit-for-bit. (`Pause_mid_run_freezes_resume_continues_deterministically`, `PauseSim_does_not_strand_pending_engagements`.) |
| **Factor is clamped, never trusted** | `SetAccelerationFactor` clamps to `[1, 256]` (`MinAccelerationFactor`/`MaxAccelerationFactor`). Callers can pass `0`, negatives, or `1000`; the clock silently clamps. Default is `1`. |
| **Clock is not replay state** | The pause flag and acceleration factor are *session/interactive* state. They change scheduling, not outcomes, so they never enter the fingerprinted `DecisionLog` or world hash. The Baltic v2 hash `17144800277401907079` is untouched by this subsystem. |

---

## The type model

### `SimClock` — the fixed-step clock

```csharp
public sealed class SimClock
{
    public const int MinAccelerationFactor = 1;
    public const int MaxAccelerationFactor = 256;

    public SimClock(double fixedDeltaSeconds = 1.0 / 60.0);

    public double FixedDeltaSeconds { get; }        // immutable step size
    public ulong  SimTick { get; }                  // steps taken
    public double SimTime => SimTick * FixedDeltaSeconds;
    public bool   IsPaused { get; }                 // default false
    public int    AccelerationFactor { get; }       // default 1, range [1, 256]

    public void Pause();                            // IsPaused = true
    public void Resume();                           // IsPaused = false
    public void SetAccelerationFactor(int factor);  // Math.Clamp(factor, 1, 256)
    public void AdvanceOneTick();                   // SimTick++
    public void Reset(ulong startTick = 0);
}
```

The clock is a pure state holder — it does not run the pipeline. It exposes the *intent*
(`IsPaused`, `AccelerationFactor`) that the tick runner reads.

### `TimeCompressionMode` — the scheduling decision

```csharp
public enum TimeCompressionMode
{
    RealTime      = 1,  // one step per TickOnce; honors pause
    Accelerated   = 2,  // AccelerationFactor steps per TickOnce; honors pause
    HeadlessBatch = 3,  // one step per TickOnce; IGNORES pause (CI/batch)
}
```

### `ISimTickRunner` — the tick loop seam

```csharp
public interface ISimTickRunner
{
    SimClock Clock { get; }
    SimSeed  Seed  { get; }
    ulong    LastWorldHash { get; }
    void TickOnce(TimeCompressionMode mode);
}
```

`TickOnce` is the single entry point. Its contract is identical across both implementations:

```csharp
public void TickOnce(TimeCompressionMode mode)
{
    if (Clock.IsPaused && mode != TimeCompressionMode.HeadlessBatch)
        return;                                                   // paused no-op

    var steps = mode == TimeCompressionMode.Accelerated
        ? Clock.AccelerationFactor
        : 1;                                                      // RealTime + HeadlessBatch = 1
    for (var i = 0; i < steps; i++)
        AdvanceOneStep();                                         // one full deterministic step
}
```

**Two implementations, one contract:**

| Runner | What one step does | Used by |
|--------|--------------------|---------|
| `SimTickRunner` | Advances the clock and folds a placeholder world hash (`MixWorldHash(seed, tick, previous)`, a splitmix-style mix). MVP runner for clock/hash tests. | `SimClock*` unit tests; composed inside the pipeline. |
| `SimTickPipeline` | Advances the same core step, then runs the **engagement phase** (drains pending `EngageRequest`s through `IEngagementResolver`, records `LastProcessed`/`LastEngagementResults`), and recomputes `LastWorldHash` by combining the core hash with the **detection sub-hash** (`MixDetectionTick`), the engagement mix, and the kill-registry mix via `SimWorldHash.Combine`. | `SimulationSession` and everything downstream (replay harness, gauntlet). |

Note the accelerated-step subtlety in the pipeline: pending engagements are drained on the **first**
sub-step, so subsequent accelerated sub-steps in the same `TickOnce` see an empty pending list
(`Pipeline_Accelerated_runs_engagement_per_step`).

---

## Session-level controls (`SimulationSession`, S112-02)

`SimulationSession` owns the pipeline (`Sim`) and re-exposes clock control as a small public API so
UI/host code never reaches into `Sim.Clock` directly:

```csharp
public bool IsSimPaused          => Sim.Clock.IsPaused;
public int  TimeAccelerationFactor => Sim.Clock.AccelerationFactor;

public void PauseSim()  => Sim.Clock.Pause();
public void ResumeSim() => Sim.Clock.Resume();
public void SetTimeAccelerationFactor(int factor) => Sim.Clock.SetAccelerationFactor(factor);

// Gated resume — the watch officer can refuse a resume until the queue is cleared,
// unless the player forces it. See watch-attention-runtime.md.
public bool TryResumeSim(bool explicitOverride = false);
```

### How the session applies pause & acceleration

The session's tick path (`RunExecutingTick`) does **not** simply forward `Accelerated` to the
pipeline — it interleaves order execution and engagement logging around the pipeline, so it applies
acceleration by **replaying real-time steps itself**:

1. If `Sim.Clock.IsPaused && !headlessOverride`, it surfaces ROE denials and returns early — no clock
   advance, no engagements queued (this is why pausing can't strand engagements).
2. Otherwise it maps to a mode: `TickHeadless` → `HeadlessBatch`, ordinary `Tick` → `RealTime`.
3. It runs one `Sim.TickOnce(mode)` with the tick's queued engagements, logs the results, applies the
   catalog-damage hot tick, then runs `AccelerationFactor − 1` **extra** `Sim.TickOnce(mode)` calls to
   consume the remaining compressed steps.

So `Tick` at `AccelerationFactor = 4` advances `SimTick` by 4 and still logs the engagement results
from the primed step (`Tick_with_acceleration_greater_than_one_advances_multiple_SimTicks`,
`Tick_with_acceleration_still_logs_engagement_results`).

### Interactive vs. headless entry points

| Method | Mode | Honors pause? |
|--------|------|---------------|
| `Tick(state)` | `RealTime` | Yes — paused sessions freeze `SimTick`. |
| `TickHeadless(state)` | `HeadlessBatch` | No — advances even when paused, preserving the pause flag and watch reason so interactive resume still works after the batch. |

This split is exactly why the watch auto-pause is **replay-neutral**: the goldens and gauntlet run
through the headless path, which ignores the interactive pause the watch officer sets.

---

## Determinism & the world hash

Determinism is the whole point of the compression design:

- **Same seed + same step count ⇒ same `LastWorldHash`**, and different seeds diverge
  (`Same_seed_and_ticks_produce_identical_world_hash`, `Different_seed_produces_different_world_hash`).
- Acceleration is defined so that the step *count* is all that matters, never the mode — the equality
  tests (`TC-CLK-3`, `Pipeline_Accelerated_matches_RealTime_hash_without_engagements`) are the guard.
- Pause/resume across a run produces the same hash as an uninterrupted run of the same number of steps
  (`Pause_mid_run_freezes_resume_continues_deterministically`).

Because pause and acceleration only change *when/how many* steps run — not what a step computes — none
of this touches the fingerprinted decision/world hashing described in
[determinism-and-replay.md](determinism-and-replay.md).

---

## Common pitfalls & constraints

- **Don't model acceleration by growing `Δt`.** `FixedDeltaSeconds` is immutable by construction; any
  "faster" behavior must come from more `AdvanceOneStep` calls, or determinism breaks.
- **Pause always wins.** If you add a new mode or a new caller, remember the pause gate runs *before*
  the step-count decision. Only `HeadlessBatch` is exempt, and that exemption is deliberate and tested.
- **Never resume by poking `Sim.Clock.Resume()` from UI.** Use `TryResumeSim()` so the watch-officer
  gate can refuse a resume with an unacknowledged pause-class card (unless the player force-overrides).
- **`AccelerationFactor` is clamped, not validated.** Passing `0`/negative silently becomes `1`; `>256`
  becomes `256`. Don't rely on out-of-range values round-tripping.
- **The session applies acceleration itself.** It calls `TickOnce(RealTime)` `AccelerationFactor`
  times rather than passing `Accelerated`, so it can log engagements per outer tick. If you add work to
  `RunExecutingTick`, keep it inside the loop only if it must run every compressed step.

---

## Extending without breaking replay

1. **Adding a new time mode?** Add it to `TimeCompressionMode`, then update *both* `SimTickRunner` and
   `SimTickPipeline` `TickOnce` (they must stay in lock-step). Decide explicitly whether it honors the
   pause gate; default to honoring it — only CI/batch semantics justify an override like
   `HeadlessBatch`.
2. **Adding per-step work to the pipeline?** Put it inside `RunOnePipelineStep` so it runs once per
   *step* (and thus `AccelerationFactor` times under `Accelerated`), and fold its contribution into
   `RecomputeWorldHash` deterministically (sorted, seed-driven) — see the detection/engagement mixes
   for the pattern.
3. **Changing default `fixedDeltaSeconds`?** This changes `SimTime` for every tick — treat it as a
   replay-affecting change and re-baseline goldens only under an ADR.
4. **Before landing:** run the clock suites below plus the full solution suite (`dotnet test`), confirm
   `ReplayGolden 6/6` and the Baltic v2 hash `17144800277401907079` are unchanged, and confirm ZERO
   `DelegationBridge` hotpath edits.

---

## Tests that pin this doc

All green as of writing (S112 / DRG-14):

| Test file | Count | Covers |
|-----------|-------|--------|
| [`SimClockTests.cs`](../../src/ProjectAegis.Sim.Tests/Time/SimClockTests.cs) | 4 | Defaults (unpaused, factor 1), `Pause`/`Resume` toggle, `SetAccelerationFactor` clamp `[1, 256]`, `AdvanceOneTick`/`Reset`/`SimTime`. |
| [`SimClockTickRunnerTests.cs`](../../src/ProjectAegis.Sim.Tests/Time/SimClockTickRunnerTests.cs) | 10 | `TC-CLK-1..5` (pause no-op, resume advances, `Accelerated×4 ≡ RealTime×4`, pause blocks accelerated, HeadlessBatch overrides pause), pause-mid-run determinism, and the pipeline pause/headless/accelerated variants. |
| [`SimTickRunnerTests.cs`](../../src/ProjectAegis.Sim.Tests/Core/SimTickRunnerTests.cs) | 2 | Same seed ⇒ identical world hash; different seed ⇒ different hash. |
| [`SimTickPipelineTests.cs`](../../src/ProjectAegis.Sim.Tests/Core/SimTickPipelineTests.cs) | — | Pipeline step wiring (detection sub-hash + engagement fold). |
| [`SimulationSessionClockControlsTests.cs`](../../src/ProjectAegis.Delegation.Tests/Orchestration/SimulationSessionClockControlsTests.cs) | 7 | Session `PauseSim`/`ResumeSim` freeze/advance `SimTick`, `SetTimeAccelerationFactor` round-trip, acceleration advances multiple ticks + still logs engagements, pause blocks accelerated tick, pause doesn't strand pending engagements. |

Run just this subsystem:

```bash
dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj \
  --filter "FullyQualifiedName~Time.SimClock|FullyQualifiedName~Core.SimTick"
dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj \
  --filter "FullyQualifiedName~SimulationSessionClockControls"
```
