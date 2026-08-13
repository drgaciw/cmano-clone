# Sim time control & watch attention — pause, acceleration, and auto-pause

> **Scope.** This page documents the interactive **time-control** spine and the **watch attention /
> auto-pause** layer that sit above the deterministic tick loop (S112 / S115 / S116, DRG-14):
> the [`SimClock`](../../src/ProjectAegis.Sim/Time/SimClock.cs) pause + acceleration state and its
> [`TimeCompressionMode`](../../src/ProjectAegis.Sim/Time/TimeCompressionMode.cs)-driven tick
> runners ([`SimTickRunner`](../../src/ProjectAegis.Sim/Core/SimTickRunner.cs) /
> [`SimTickPipeline`](../../src/ProjectAegis.Sim/Core/SimTickPipeline.cs)), the
> [`SimulationSession`](../../src/ProjectAegis.Delegation/Orchestration/SimulationSession.cs)
> pause/resume/acceleration controls, and the pure watch-officer layer in
> [`ProjectAegis.Delegation/Watch/`](../../src/ProjectAegis.Delegation/Watch/) (`WatchAttentionQueue`,
> `WatchAutoPauseGate`, `WatchAttentionEmitFactory`) plus its read-model projection. It is the
> "classic wargame auto-pause on first contact" feature, made deterministic and replay-safe.
>
> This is a **client / interactive-control** concern layered on top of the sim, not sim authority.
> Like the [deck-operations](deck-operations-runtime.md) FSMs, **none of it is part of the replay
> fingerprint** and the headless/CI path bypasses pause entirely (see
> [Determinism & replay](#determinism--replay)).

The delegation/sim pipeline is a **fixed-step, seed-deterministic** tick loop — every
`(scenario, seed)` produces the same order-log hash (see [Determinism & replay](determinism-and-replay.md)).
Time control adds two orthogonal, interactive knobs on top of that loop without touching the per-tick
math:

1. **Pause / resume** — freeze kinetic advance while the operator reads the picture, then continue.
2. **Time acceleration** — run *N* full deterministic steps per interactive frame (fast-forward).

The **watch attention** layer wires those knobs to game events: a first hostile/unknown contact or an
own-side loss both raise a **pause-class** card and can **auto-pause** the sim, and resume is **gated**
until the operator has acknowledged (or force-overrides) the outstanding cards.

---

## Layering

```
ProjectAegis.Sim/Time/           # pure clock state (no orchestrator, no RNG)
  SimClock                       # SimTick, IsPaused, AccelerationFactor
  TimeCompressionMode            # RealTime | Accelerated | HeadlessBatch

ProjectAegis.Sim/Core/           # tick runners that honor the mode
  ISimTickRunner.TickOnce(mode)
  SimTickRunner / SimTickPipeline

ProjectAegis.Delegation/Watch/   # pure watch-officer layer (session-local, no Bridge, no RNG)
  WatchAttentionKind / Priority / PauseReason   # enums
  WatchAttentionEvent / WatchAttentionCard      # immutable records
  WatchAttentionQueue            # ordered, idempotent, ack/dismiss/restore
  WatchAutoPauseGate             # should-auto-pause + gated-resume policy
  WatchAttentionEmitFactory      # pure fact → event factories (stable EventIds)

ProjectAegis.Delegation/Orchestration/
  SimulationSession              # owns the clock + queue + gate; drives everything

ProjectAegis.Delegation/Projection/
  WatchAttentionQueueProjection  # read-model contract for the C2 watch panel
```

Everything here is **pure and single-threaded** — no `Random`, no `DateTime.UtcNow`, no Unity types,
no `DelegationBridge` edits. The Unity adapter only *surfaces* the read-model and *invokes* the
session controls; it is never sim authority (ADR-010 §2–3, ADR-001).

---

## The clock — `SimClock`

[`SimClock`](../../src/ProjectAegis.Sim/Time/SimClock.cs) is the single owner of interactive time
state. It is a plain sealed class with no dependencies:

| Member | Meaning |
|--------|---------|
| `FixedDeltaSeconds` | Immutable step size (default `1.0 / 60.0`). Acceleration **never** stretches this. |
| `SimTick` (`ulong`) | Monotonic step counter; advanced only via `AdvanceOneTick()`. |
| `SimTime` | `SimTick * FixedDeltaSeconds` — derived, read-only. |
| `IsPaused` | `false` by default; toggled by `Pause()` / `Resume()`. |
| `AccelerationFactor` | Steps per `Accelerated` `TickOnce`; default `1`, clamped to `[1, 256]`. |

```csharp
public const int MinAccelerationFactor = 1;
public const int MaxAccelerationFactor = 256;

public void Pause()  => IsPaused = true;
public void Resume() => IsPaused = false;
public void SetAccelerationFactor(int factor) =>
    AccelerationFactor = Math.Clamp(factor, MinAccelerationFactor, MaxAccelerationFactor);
```

The load-bearing design decision: **acceleration is expressed as "how many whole deterministic steps
to run", not as a bigger `Δt`.** Because `FixedDeltaSeconds` is constant, four steps at `×4`
acceleration produce the *identical* world state to four steps at `×1` — fast-forward and real-time
are bit-for-bit equal. Out-of-range factors are silently clamped rather than throwing.

---

## Tick modes — `TimeCompressionMode`

```csharp
public enum TimeCompressionMode
{
    RealTime = 1,       // advance exactly one step per TickOnce
    Accelerated = 2,    // advance AccelerationFactor full steps per TickOnce
    HeadlessBatch = 3,  // advance one step per call AND ignore IsPaused
}
```

The mode is passed to `ISimTickRunner.TickOnce(mode)`. Both concrete runners
([`SimTickRunner`](../../src/ProjectAegis.Sim/Core/SimTickRunner.cs), the MVP hash-only runner, and
[`SimTickPipeline`](../../src/ProjectAegis.Sim/Core/SimTickPipeline.cs), the ADR-004 runner with the
engagement phase wired) implement the same rule:

```csharp
public void TickOnce(TimeCompressionMode mode)
{
    if (Clock.IsPaused && mode != TimeCompressionMode.HeadlessBatch)
        return;                       // interactive pause = no-op (SimTick + hash unchanged)

    var steps = mode == TimeCompressionMode.Accelerated ? Clock.AccelerationFactor : 1;
    for (var i = 0; i < steps; i++)
        RunOnePipelineStep();         // advance clock + detection + engagement + world hash
}
```

Two invariants fall out of this:

- **Interactive pause is a true no-op.** When `IsPaused` and the mode is `RealTime`/`Accelerated`,
  neither `SimTick` nor `LastWorldHash` change.
- **`HeadlessBatch` overrides pause.** CI, the [Baltic replay harness](baltic-replay-harness.md), and
  the [QA Gauntlet](qa-gauntlet.md) always advance deterministically without needing an explicit
  `Resume()` — a paused interactive session can hand its clock to a batch runner and still make
  progress. This is what keeps auto-pause an *interactive-only* feature that never perturbs goldens.

---

## Session controls — `SimulationSession`

[`SimulationSession`](../../src/ProjectAegis.Delegation/Orchestration/SimulationSession.cs) owns the
clock (via its `SimTickPipeline`) and exposes the operator-facing surface:

```csharp
public bool IsSimPaused            => Sim.Clock.IsPaused;
public void PauseSim()             => Sim.Clock.Pause();
public void ResumeSim()            => Sim.Clock.Resume();      // ungated (legacy callers)
public int  TimeAccelerationFactor => Sim.Clock.AccelerationFactor;
public void SetTimeAccelerationFactor(int factor) => Sim.Clock.SetAccelerationFactor(factor);
public bool TryResumeSim(bool explicitOverride = false);       // gated by the watch queue
```

### How a tick behaves under pause / acceleration

`Tick(state)` (interactive) and `TickHeadless(state)` both call the private `RunExecutingTick`:

- **Decisions always run.** `Orchestrator.Tick(state)` (the [decision pipeline](agent-decision-pipeline.md))
  executes *before* the pause check, so agents still perceive and reason while paused; ROE-denied
  engagements are still surfaced.
- **Kinetic resolution freezes under interactive pause.** If `IsPaused` and *not* a headless
  override, `RunExecutingTick` returns before enqueuing any engagements — so a paused session
  **does not strand pending shots**: nothing is queued, nothing is resolved, `SimTick` holds.
- **Acceleration is driven by the session loop, not the `Accelerated` mode.** The session passes
  `RealTime` (or `HeadlessBatch`), enqueues engagements once, runs the first `Sim.TickOnce(mode)`,
  logs the results, then advances the remaining `AccelerationFactor - 1` steps:

  ```csharp
  var mode = headlessOverride ? TimeCompressionMode.HeadlessBatch : TimeCompressionMode.RealTime;
  Sim.TickOnce(mode);                 // step 1: resolve enqueued engagements + log
  LogEngagementResults(...);
  ApplyCatalogDamageHotTick(...);
  var extraSteps = Math.Max(0, Sim.Clock.AccelerationFactor - 1);
  for (var i = 0; i < extraSteps; i++)
      Sim.TickOnce(mode);             // steps 2..N: advance clock/detection/world hash
  ```

  So at `×4`, one interactive `Tick` advances `SimTick` by 4 and still logs the engagement outcome
  on the first step. `TickHeadless` uses the same code with `headlessOverride: true`, which both
  bypasses the interactive pause guard and selects `HeadlessBatch`.

---

## Watch attention model

The watch layer answers "**what does the watch officer need to look at right now, and should the sim
stop so they can?**". It is deliberately **distinct from `Delegation.Attention`** (the AI
cognitive-load / bandwidth model in [agent-traits-and-attention.md](agent-traits-and-attention.md)):
that one models an *agent's* attention budget; this one models the *human operator's* attention queue.

### Value types

| Type | Purpose |
|------|---------|
| [`WatchAttentionKind`](../../src/ProjectAegis.Delegation/Watch/WatchAttentionKind.cs) | `HostileOrUnknownContact` (0), `OwnSideLossOrDamage` (1). Both are **pause-class** today. |
| [`WatchAttentionPriority`](../../src/ProjectAegis.Delegation/Watch/WatchAttentionPriority.cs) | Queue ordering — `Critical(0) < High(1) < Normal(2) < Low(3)` (lower ordinal = higher priority). |
| [`WatchPauseReason`](../../src/ProjectAegis.Delegation/Watch/WatchPauseReason.cs) | Headless reason code: `None`, `HostileOrUnknownContact`, `OwnSideLossOrDamage`, `ExplicitPlayer`. Presentation maps this to a label; the sim only stores the enum. |
| [`WatchAttentionEvent`](../../src/ProjectAegis.Delegation/Watch/WatchAttentionEvent.cs) | Immutable fact: `EventId`, `Kind`, `Priority`, `TriggerTick`, `SubjectId`, optional `GroupingKey` / `ReasonDetail`. `IsPauseClass` is derived from `Kind`. |
| [`WatchAttentionCard`](../../src/ProjectAegis.Delegation/Watch/WatchAttentionCard.cs) | Presentation wrapper over an event with `IsAcknowledged` / `IsDismissed`. `IsUnresolved = pause-class && !acked && !dismissed`. |

The **`EventId` is the identity key** and must be stable for a given `(kind, subject, trigger
context)` so that re-observing the same fact is idempotent (no duplicate cards). Ack / dismiss /
restore are **presentation-only** — they never mutate sim policy or the order log.

---

## The queue — `WatchAttentionQueue`

[`WatchAttentionQueue`](../../src/ProjectAegis.Delegation/Watch/WatchAttentionQueue.cs) is a
**session-local, pure, single-threaded** ordered list of cards. It mirrors the `PendingApprovalQueue`
pattern (no Bridge, no async). Key behaviors:

- **Stable sort order:** `Priority` (Critical first) → `TriggerTick` ascending → `EventId` ordinal.
  Re-sorted after every insert, so the order is deterministic regardless of enqueue order.
- **Idempotent `Enqueue`:** first write wins; re-enqueuing the same `EventId` is a no-op (a later
  `TriggerTick` does **not** overwrite the original). Empty / null `EventId` throws.
- **Ack / dismiss / restore:** `TryAcknowledge` marks a card resolved (still visible so the UI can
  style it); `TryDismiss` soft-hides it from the default view; `TryRestore` un-hides. All return
  `false` for an unknown `EventId`.
- **Gating counters:** `UnresolvedPauseClassCount` / `HasUnresolvedPauseClass` count pause-class
  cards that are neither acknowledged nor dismissed — this is what gates resume.
- **`SnapshotVisible()`** returns the non-dismissed cards in order (for the panel); **`Clear()`**
  wipes the queue on session reset / scenario change.

---

## Auto-pause & gated resume — `WatchAutoPauseGate`

[`WatchAutoPauseGate`](../../src/ProjectAegis.Delegation/Watch/WatchAutoPauseGate.cs) is the **policy**
that decides *when* to stop and *when* it is safe to continue. It does **not** own the clock — the
session calls `PauseSim()` / `ResumeSim()`:

- `ShouldAutoPause(evt)` → `true` for a pause-class event, and records `LastPauseReason` mapped from
  the kind. Non-pause-class events never auto-pause.
- `CanResume(queue, explicitOverride)` → `true` when there are **zero** unresolved pause-class cards,
  **or** when the operator force-resumes (`explicitOverride: true`).
- `ClearReason()` resets `LastPauseReason` to `None` after a clean resume.

### Session wiring

```csharp
public void ReportWatchAttention(WatchAttentionEvent evt)
{
    WatchQueue.Enqueue(evt);                       // idempotent add
    if (WatchPauseGate.ShouldAutoPause(evt))
        PauseSim();                                // auto-pause on pause-class
}

public bool TryResumeSim(bool explicitOverride = false)
{
    if (!WatchPauseGate.CanResume(WatchQueue, explicitOverride))
        return false;                              // still blocked by unresolved cards
    ResumeSim();
    WatchPauseGate.ClearReason();
    return true;
}
```

The operator loop is therefore: **contact/loss → card enqueued → sim auto-pauses → operator
acknowledges (or dismisses) the card(s) → `TryResumeSim()` succeeds.** `TryResumeSim(explicitOverride:
true)` is the "I know, resume anyway" escape hatch. The raw `ResumeSim()` is an **ungated** legacy
path that resumes without consulting the queue and intentionally leaves `LastPauseReason` set.

Note the deliberate asymmetry from the tick runner: even while auto-paused, `TickHeadless` still
advances (it selects `HeadlessBatch`), and the pause flag + reason are **preserved** so interactive
resume still behaves correctly after a batch step.

---

## Emitting watch events — `WatchAttentionEmitFactory`

[`WatchAttentionEmitFactory`](../../src/ProjectAegis.Delegation/Watch/WatchAttentionEmitFactory.cs)
(S116) turns raw sim facts into events with **stable, prefixed `EventId`s** so the queue stays
idempotent. It is pure — no Bridge, no RNG, no clock ownership.

| Factory | Fires when | EventId | Priority |
|---------|-----------|---------|----------|
| `TryFromFirstHostileOrUnknownContact(transition)` | A [`ContactTransition`](../../src/ProjectAegis.Sim/Sensors/ContactTransition.cs) goes `Unknown → Detected/Classified/Identified` for a **non-own-side** subject. | `watch:contact:{TargetId}` | `Critical` if hostile, else `High` (unknown/neutral) |
| `TryFromOwnSideLoss(unitId, tick, detail)` | An **own-side** unit takes a loss / battle-damage transition. | `watch:loss:{unitId}` | `Critical` |
| `TryFromOwnSideLostTransition(transition)` | An own-side contact transitions to `Lost` (delegates to `TryFromOwnSideLoss`). | `watch:loss:{TargetId}` | `Critical` |

"Own-side" is resolved by `IsOwnSideUnit` — the legacy primary blue id `"u1"` or any
`BalticV3SideRegistry.IsBlueForceUnit(...)`. Hostile classification reuses
`HostileContactFilter.IsEngageableHostileTarget` (see [detection pipeline](detection-pipeline.md)),
so the watch layer and the engage layer agree on what counts as hostile. The
`Unknown → …` edge condition matches the [mission-timeline](mission-timeline-runtime.md) "fire-once
on first recon" pattern: it fires on the *first* detection only, never re-fires as the track is
refined.

### Call-sites (session)

`SimulationSession` provides two thin, Bridge-free entry points that a sensor/harness path can call:

- `ReportContactTransitions(IReadOnlyList<ContactTransition>)` — runs both the first-contact and
  own-side-Lost factories over the tick's transitions and reports any that fire.
- `ReportOwnSideLoss(unitId, triggerTick, reasonDetail?)` — no-op for non-own-side ids.

Today the wired producer is the **BDA loss path**: when `ResolveSortedLostTargets` marks an own-side
target lost, `RunExecutingTick` calls `ReportOwnSideLoss(targetId, simTick, "bda:lost")` (hostile
losses stay silent). Additional producers (e.g. the contact FSM) can be wired by calling
`ReportContactTransitions` from wherever transitions are produced, without touching the hotpath.

---

## Read-model — `WatchAttentionQueueProjection`

[`WatchAttentionQueueProjection`](../../src/ProjectAegis.Delegation/Projection/WatchAttentionQueueProjection.cs)
is the stable UI contract (see the [C2 projection layer](c2-projection-layer.md)). Unity hosts **bind
labels only** — they never re-derive priority or pause class:

- `ProjectVisible(queue)` → ordered, non-dismissed cards for the watch panel.
- `ProjectUnresolvedCount(queue)` → badge / gating count.
- `ProjectPauseReasonLabel(reason)` → the human string for a `WatchPauseReason`
  (`"Hostile / unknown contact"`, `"Own-side loss / damage"`, `"Player pause"`, or empty).

---

## Determinism & replay

This whole subsystem is an **interactive-control client**; it is engineered to be invisible to the
determinism spine:

- **Not fingerprinted.** Watch cards, pause state, and acceleration factor are **not** appended to the
  `DecisionLog` / order log and do **not** contribute to `LastWorldHash`. Toggling pause or changing
  acceleration cannot move a replay golden or the Baltic v2 hash `17144800277401907079`.
- **Headless overrides pause.** `HeadlessBatch` (runner mode) and `TickHeadless` (session) both
  advance regardless of `IsPaused`, so CI / [replay](baltic-replay-harness.md) / [gauntlet](qa-gauntlet.md)
  runs never depend on operator input.
- **Acceleration is step-count only.** Because `FixedDeltaSeconds` is constant and acceleration runs
  whole steps, `×N` fast-forward is bit-for-bit identical to `×1` for the same tick count.
- **No RNG, no wall-clock.** The clock counts ticks; the queue and gate are pure ordered logic; the
  emit factories are pure functions of their inputs. Nothing here reads `Random.Shared` or
  `DateTime.UtcNow`.
- **Session-local, single-threaded.** The queue and gate live on the `SimulationSession`; there is no
  shared static state and no `DelegationBridge` involvement (the hotpath stays zero-touch).

---

## How to extend

**Add a new pause-class event kind:**

1. Add the value to [`WatchAttentionKind`](../../src/ProjectAegis.Delegation/Watch/WatchAttentionKind.cs)
   (append at the end — the enum is `byte`-backed) and, if it should stop the sim, include it in
   `WatchAttentionEvent.IsPauseClass` and add the matching case to `WatchAutoPauseGate.ShouldAutoPause`
   (plus a `WatchPauseReason` + a `ProjectPauseReasonLabel` string).
2. Add a factory to [`WatchAttentionEmitFactory`](../../src/ProjectAegis.Delegation/Watch/WatchAttentionEmitFactory.cs)
   that produces a **stable, prefixed `EventId`** for the new fact.
3. Call it from the producing path (a `SimulationSession.Report*` helper is the intended seam) — do
   **not** reach into `DelegationBridge` or the hotpath.

**Add a new time-control affordance:** extend `SimClock` with the new state and expose a
`SimulationSession` control that forwards to it. Keep acceleration expressed as **whole steps** and
never stretch `FixedDeltaSeconds`, or you will break the `×N == ×1` determinism guarantee.

Because the whole layer is off the fingerprint, none of these changes should require regenerating
replay goldens — but still run the standard verification (`dotnet build` + `dotnet test`, replay
golden `6/6`, PlayModeSmoke `≥20/20`, and the hash grep) before submitting.

---

## Related references

| Where | What |
|-------|------|
| [determinism-and-replay.md](determinism-and-replay.md) | The fixed-step, seed-deterministic tick loop this layer sits on top of. |
| [agent-decision-pipeline.md](agent-decision-pipeline.md) | The `DelegationOrchestrator.Tick` decision path that runs even while paused. |
| [agent-traits-and-attention.md](agent-traits-and-attention.md) | The **AI** attention/bandwidth model — deliberately distinct from watch attention. |
| [engagement-pipeline.md](engagement-pipeline.md) | The engage/kill-chain phase that freezes under interactive pause. |
| [detection-pipeline.md](detection-pipeline.md) | Where `ContactTransition`s and hostile classification come from. |
| [baltic-replay-harness.md](baltic-replay-harness.md) / [qa-gauntlet.md](qa-gauntlet.md) | The headless consumers that override pause via `HeadlessBatch`. |
| [c2-projection-layer.md](c2-projection-layer.md) | The read-model layer the watch projection belongs to. |
