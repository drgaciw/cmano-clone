# Sim clock & time-compression runtime

The fixed-step **simulation clock** and the pause / acceleration controls layered on top of it
(`ProjectAegis.Sim/Time/`, `ProjectAegis.Sim/Core/`, and the session-level façade in
`ProjectAegis.Delegation/Orchestration/SimulationSession.cs`). This is the *engineering*
reference for how one sim tick is advanced, how the player pauses/compresses time, and why
none of it can move a replay golden. It complements:

- the design intent in [`design/gdd/simulation-core-time.md`](../../design/gdd/simulation-core-time.md) (requirements 03 / 08, ADR-004 / ADR-005),
- the tick-hash model in [`determinism-and-replay.md`](determinism-and-replay.md), and
- the short pipeline overview in [`../../src/ProjectAegis.Sim/README.md`](../../src/ProjectAegis.Sim/README.md).

Added by **S112 / DRG-14** (clock pause + acceleration on the tick loop and session controls);
extended by **S115 / S116** (watch auto-pause). Verified against source and pinned by
`SimClockTests`, `SimClockTickRunnerTests`, and `SimulationSessionClockControlsTests`.

---

## Where it lives

| Type | File | Role |
|------|------|------|
| `SimClock` | [`Sim/Time/SimClock.cs`](../../src/ProjectAegis.Sim/Time/SimClock.cs) | Fixed-Δt tick counter + pause flag + acceleration factor. Owns no world state. |
| `TimeCompressionMode` | [`Sim/Time/TimeCompressionMode.cs`](../../src/ProjectAegis.Sim/Time/TimeCompressionMode.cs) | `RealTime` / `Accelerated` / `HeadlessBatch`. |
| `ISimTickRunner` | [`Sim/Core/ISimTickRunner.cs`](../../src/ProjectAegis.Sim/Core/ISimTickRunner.cs) | Tick seam: `Clock`, `Seed`, `LastWorldHash`, `TickOnce(mode)`. |
| `SimTickRunner` | [`Sim/Core/SimTickRunner.cs`](../../src/ProjectAegis.Sim/Core/SimTickRunner.cs) | MVP runner — advances clock + folds a core world hash. |
| `SimTickPipeline` | [`Sim/Core/SimTickPipeline.cs`](../../src/ProjectAegis.Sim/Core/SimTickPipeline.cs) | ADR-004 runner — wraps `SimTickRunner`, resolves engagements, mixes the detection sub-hash. |
| `SimulationSession` (clock façade) | [`Delegation/Orchestration/SimulationSession.cs`](../../src/ProjectAegis.Delegation/Orchestration/SimulationSession.cs) | `PauseSim` / `ResumeSim` / `TryResumeSim`, `SetTimeAccelerationFactor`, `Tick` / `TickHeadless`. |
| `WatchAutoPauseGate` | [`Delegation/Watch/WatchAutoPauseGate.cs`](../../src/ProjectAegis.Delegation/Watch/WatchAutoPauseGate.cs) | Decides *when* to auto-pause and *whether* resume is allowed (S115/S116). |

---

## `SimClock` — the counter

`SimClock` is a pure counter; it holds **no** world state and computes **no** hashes. It owns
exactly three things: the tick count, the pause flag, and the acceleration factor.

| Member | Meaning |
|--------|---------|
| `FixedDeltaSeconds` | Immutable sim Δt (default `1.0 / 60.0`), set at construction. Never varies within a run. |
| `SimTick` (`ulong`) | Increments once per full pipeline pass via `AdvanceOneTick()`. |
| `SimTime` (`double`) | Derived: `SimTick × FixedDeltaSeconds`. |
| `IsPaused` | `Pause()` / `Resume()`; default `false`. |
| `AccelerationFactor` (`int`) | `SetAccelerationFactor(int)`, **clamped to `[1, 256]`** (`MinAccelerationFactor` / `MaxAccelerationFactor`). Default `1`. |
| `Reset(startTick = 0)` | Rewinds the tick counter. |

Acceleration is **not** implemented by stretching `FixedDeltaSeconds` — Δt is constant. Instead
the runner executes *multiple full steps per call* (see below), so each accelerated step is
bit-identical to a real-time step at the same tick.

---

## Tick runners & `TickOnce(mode)`

Both `ISimTickRunner` implementations share the same mode contract:

| `mode` | Behaviour when running | When paused |
|--------|------------------------|-------------|
| `RealTime` | Advances **1** step. | **No-op** (tick + hash unchanged). |
| `Accelerated` | Advances **`AccelerationFactor`** steps in one call. | **No-op** — pause blocks accelerated too. |
| `HeadlessBatch` | Advances **1** step. | **Overrides pause** and advances anyway. |

`HeadlessBatch` is the CI/batch escape hatch: it lets replay and gauntlet runners advance
deterministically without an explicit `Resume()`, so an interactive pause left in state can
never wedge a headless run.

`SimTickRunner.AdvanceOneStep()` is `internal` — it advances exactly one step with **no**
pause/acceleration checks, for pipelines that own the outer loop (`SimTickPipeline` calls it
per sub-step). External callers must go through `TickOnce(mode)`.

```csharp
var runner = new SimTickRunner(SimSeed.FromScenario(42));
runner.Clock.SetAccelerationFactor(4);
runner.TickOnce(TimeCompressionMode.Accelerated);   // SimTick 0 → 4 in one call
```

### `SimTickPipeline` — engagement + detection per step

`SimTickPipeline` (ADR-004) is the production runner. Each step:

```text
RunOnePipelineStep()
  → SimTickRunner.AdvanceOneStep()            (clock advance + core hash)
  → drain pending EngageRequests → IEngagementResolver.Resolve(...)
  → RecomputeWorldHash: SimWorldHash.Combine(core, detection, engage, kill)
```

Under `Accelerated`, engagement resolution runs **once per step**. Because the pending queue is
drained on the first sub-step, later accelerated steps of the same call see an empty queue —
so enqueue-then-`TickOnce(Accelerated)` resolves the batch exactly once, then advances the
remaining ticks with no engagements (see `Pipeline_Accelerated_runs_engagement_per_step`).
Detection is mixed separately via `MixDetectionTick(...)` (tick step 4) so the harness can pin
it independently of engagement.

---

## Session-level controls (`SimulationSession`)

The interactive host (C2) and headless harnesses drive the clock through the session façade,
never by poking `SimClock` directly. The façade forwards to `Sim.Clock`:

| Member | Effect |
|--------|--------|
| `IsSimPaused` | `Sim.Clock.IsPaused`. |
| `PauseSim()` / `ResumeSim()` | Set / clear the pause flag. |
| `TryResumeSim(bool explicitOverride = false)` | Watch-gated resume (see below); returns `false` if refused. |
| `TimeAccelerationFactor` / `SetTimeAccelerationFactor(int)` | Read / set the clamped factor. |
| `Tick(ObservedState)` | Interactive pass (`RealTime`). |
| `TickHeadless(ObservedState)` | CI/batch pass (`HeadlessBatch`) — advances even when paused. |

### How the session realises pause & acceleration

`SimulationSession.RunExecutingTick` does **not** hand `Accelerated` to the pipeline. It:

1. Runs the delegation tick (`Orchestrator.Tick`) — this always happens, even when paused, so
   agent decisions and ROE denials keep flowing to the order log.
2. If `IsPaused` and this is **not** a headless pass, returns early **before** any engagement is
   enqueued — so pausing strands nothing (`PauseSim_does_not_strand_pending_engagements`). It
   still surfaces ROE-denied engagements for the picture.
3. Otherwise enqueues the tick's engage orders, calls `Sim.TickOnce(mode)` **once** (with
   `RealTime` or `HeadlessBatch`, i.e. a single step), then logs engagement results and applies
   catalog damage for that one authored step.
4. Advances the remaining `AccelerationFactor − 1` steps with extra single-step `TickOnce(mode)`
   calls that only move the clock and world hash.

The result: the session's `SimTick` advances by the full acceleration factor, but engagement
resolution, order-log entries, and catalog-damage hot-tick apply **once per authored tick**
(`Tick_with_acceleration_greater_than_one_advances_multiple_SimTicks` /
`Tick_with_acceleration_still_logs_engagement_results`). This keeps per-tick side effects
aligned to a single resolve while still compressing wall-clock time.

```csharp
var session = new SimulationSession(seed, new MvpEngagementResolver(/* … */));
session.BeginExecution();

session.SetTimeAccelerationFactor(4);
session.Tick(observed);               // SimTick += 4; engagements resolved/logged once
session.PauseSim();
session.Tick(observed);               // no-op advance; SimTick unchanged; ROE denials still surfaced
session.TickHeadless(observed);       // advances despite pause (CI/batch), pause flag preserved
```

---

## Watch auto-pause integration (S115 / S116)

`WatchAutoPauseGate` decides *policy*; it never touches the clock itself.

- `ReportWatchAttention(evt)` enqueues the event and, if `ShouldAutoPause(evt)` is true (a
  **pause-class** event — first hostile/unknown contact, or an own-side loss/damage), calls
  `PauseSim()`. The reason is recorded in `LastWatchPauseReason`.
- `TryResumeSim(explicitOverride)` consults `WatchAutoPauseGate.CanResume(queue, explicitOverride)`:
  resume is refused while the watch queue still has **unresolved pause-class cards**, unless the
  player passes `explicitOverride: true` (force-resume). On success it resumes and clears the
  reason.

`ReportContactTransitions(...)` / `ReportOwnSideLoss(...)` are the pure factories that turn
sensor/BDA transitions into watch events, so the harness and sensor path can drive auto-pause
without editing `DelegationBridge`.

---

## Determinism invariants — never break these

1. **Accelerated N once ≡ RealTime N times.** `TickOnce(Accelerated)` at factor *N* yields the
   same `SimTick` **and** `LastWorldHash` as *N* separate `RealTime` calls
   (`TC_CLK_3`, `Pipeline_Accelerated_matches_RealTime_hash_without_engagements`).
2. **Pause is a clean freeze.** A paused `RealTime` / `Accelerated` `TickOnce` leaves both
   `SimTick` and `LastWorldHash` untouched; resuming continues bit-identically to an un-paused
   run (`TC_CLK_1`, `TC_CLK_4`, `Pause_mid_run_freezes_resume_continues_deterministically`).
3. **`HeadlessBatch` overrides pause only** — it changes *whether* a step runs, never the *value*
   of a step (`TC_CLK_5`).
4. **No wall-clock, constant Δt.** `FixedDeltaSeconds` is immutable; acceleration adds steps, it
   does not stretch time. No `DateTime.UtcNow` / `Random.Shared` in this path.
5. **Replay-safe.** The Baltic replay goldens run through `DelegationBridge.Tick` and never pause
   or set an acceleration factor, so the clock controls default to inert (`RealTime`, factor `1`)
   and leave the Baltic v2 hash `17144800277401907079` untouched.

---

## Consumers

| Consumer | Path |
|----------|------|
| Interactive C2 host | `SimulationSession.PauseSim` / `SetTimeAccelerationFactor` / `Tick` (RealTime). |
| Replay / gauntlet harness | `DelegationBridge.Tick` (goldens); factor `1`, never paused. |
| CI / batch throughput | `TickHeadless` / `TimeCompressionMode.HeadlessBatch`. |
| Micro-benchmark | [`ProjectAegis.Sim.Benchmark/SimBenchmark.cs`](../../src/ProjectAegis.Sim.Benchmark/SimBenchmark.cs) — `SimTickPipeline.TickOnce(HeadlessBatch)`. |

---

## Extending it — pitfalls

- **Adding a per-tick side effect?** Hang it off the *authored* step in
  `SimulationSession.RunExecutingTick` (alongside `LogEngagementResults` /
  `ApplyCatalogDamageHotTick`), **not** inside the accelerated extra-steps loop — those steps
  intentionally advance clock + hash only.
- **Never** pass `TimeCompressionMode.Accelerated` from the session to `Sim.TickOnce`; the
  session realises acceleration through its own extra-steps loop so engagement resolution stays
  once-per-tick.
- **Never** call `SimClock.AdvanceOneTick()` / `SimTickRunner.AdvanceOneStep()` from outside a
  runner — you would advance the clock without folding the world hash and desync replay.
- **Respect the `[1, 256]` clamp.** Don't reimplement acceleration bounds elsewhere; call
  `SetAccelerationFactor` and let the clock clamp.
- Auto-pause is **policy only** — new pause triggers belong in `WatchAutoPauseGate` /
  `WatchAttentionEmitFactory`, not in the clock or the runner.
