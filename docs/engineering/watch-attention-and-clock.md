# Watch attention & sim clock — developer guide

Project Aegis pauses itself when something the watch officer must see happens — a hostile or
unknown contact is detected for the first time, or an own-side unit is lost or damaged. This page
documents the two engine-agnostic subsystems that make that work and how they fit together:

1. **The sim clock controls** — pause / resume and time-acceleration on the fixed-step
   [`SimClock`](../../src/ProjectAegis.Sim/Time/SimClock.cs), surfaced at session level by
   `SimulationSession` (S112 / DRG-14).
2. **The watch-attention spine** — a session-local, ordered queue of pause-class events
   (`Watch/WatchAttentionQueue`), an auto-pause gate that freezes the clock and gates resume until
   the operator resolves them (`Watch/WatchAutoPauseGate`), and pure fact→event factories that map
   sensor/BDA facts into those events (`Watch/WatchAttentionEmitFactory`) — S115 / S116, PRD
   P0-6 · P0-7.

Both are **inputs to / controls around the decision tick, not part of the fingerprinted decision
log**. Acknowledge/dismiss and pause/resume are presentation-and-session state; they never mutate
sim policy or perturb the [replay goldens](determinism-and-replay.md). The headless
[Baltic replay harness](baltic-replay-harness.md) and QA batch runs bypass the pause entirely
(see [§4](#4-headless-batch-bypasses-the-pause)) so CI stays deterministic and non-interactive.

- **Source:** [`src/ProjectAegis.Delegation/Watch/`](../../src/ProjectAegis.Delegation/Watch/)
  (queue, gate, events, factory), the clock in
  [`src/ProjectAegis.Sim/Time/`](../../src/ProjectAegis.Sim/Time/), the session wiring in
  [`SimulationSession.cs`](../../src/ProjectAegis.Delegation/Orchestration/SimulationSession.cs),
  and the UI contract in
  [`WatchAttentionQueueProjection.cs`](../../src/ProjectAegis.Delegation/Projection/WatchAttentionQueueProjection.cs).
- **Related:** the contact transitions that drive the emit factory come from the
  [detection pipeline](detection-pipeline.md); the own-side-loss facts come from the catalog-damage
  BDA slice in [catalog-damage-readiness-runtime.md](catalog-damage-readiness-runtime.md). This
  subsystem is distinct from the AI *cognitive-load* attention model in
  [agent-traits-and-attention.md](agent-traits-and-attention.md) (`Delegation/Attention/`) and from
  the message-log `AlertSeverity` tiering in the [C2 projection layer](c2-projection-layer.md) —
  see [§6](#6-what-this-is-not).

---

## Where it lives

| File | Role |
|------|------|
| [`SimClock.cs`](../../src/ProjectAegis.Sim/Time/SimClock.cs) | Fixed-step clock: `SimTick`, `IsPaused`, `AccelerationFactor` (clamped `1..256`), `Pause()` / `Resume()` / `SetAccelerationFactor()` / `AdvanceOneTick()`. |
| [`TimeCompressionMode.cs`](../../src/ProjectAegis.Sim/Time/TimeCompressionMode.cs) | `RealTime` / `Accelerated` / `HeadlessBatch`. |
| [`SimTickPipeline.cs`](../../src/ProjectAegis.Sim/Core/SimTickPipeline.cs) | `TickOnce(mode)` — no-op when paused unless `HeadlessBatch`; advances `AccelerationFactor` full steps in `Accelerated`. |
| [`WatchAttentionEvent.cs`](../../src/ProjectAegis.Delegation/Watch/WatchAttentionEvent.cs) | Immutable event `(EventId, Kind, Priority, TriggerTick, SubjectId, GroupingKey?, ReasonDetail?)` + `IsPauseClass`. |
| [`WatchAttentionKind.cs`](../../src/ProjectAegis.Delegation/Watch/WatchAttentionKind.cs) | `HostileOrUnknownContact` / `OwnSideLossOrDamage`. |
| [`WatchAttentionPriority.cs`](../../src/ProjectAegis.Delegation/Watch/WatchAttentionPriority.cs) | `Critical` < `High` < `Normal` < `Low` (lower ordinal = higher priority). |
| [`WatchAttentionCard.cs`](../../src/ProjectAegis.Delegation/Watch/WatchAttentionCard.cs) | Presentation card wrapping an event + `IsAcknowledged` / `IsDismissed`; derives `IsUnresolved`. |
| [`WatchAttentionQueue.cs`](../../src/ProjectAegis.Delegation/Watch/WatchAttentionQueue.cs) | Session-local ordered queue: idempotent `Enqueue`, `TryAcknowledge` / `TryDismiss` / `TryRestore`, `SnapshotVisible`, `UnresolvedPauseClassCount`. |
| [`WatchAutoPauseGate.cs`](../../src/ProjectAegis.Delegation/Watch/WatchAutoPauseGate.cs) | `ShouldAutoPause(evt)` and `CanResume(queue, explicitOverride)` + `LastPauseReason`. |
| [`WatchPauseReason.cs`](../../src/ProjectAegis.Delegation/Watch/WatchPauseReason.cs) | `None` / `HostileOrUnknownContact` / `OwnSideLossOrDamage` / `ExplicitPlayer`. |
| [`WatchAttentionEmitFactory.cs`](../../src/ProjectAegis.Delegation/Watch/WatchAttentionEmitFactory.cs) | Pure fact → event factories with stable `EventId`s (`watch:contact:` / `watch:loss:` prefixes). |
| [`SimulationSession.cs`](../../src/ProjectAegis.Delegation/Orchestration/SimulationSession.cs) | Owns `WatchQueue` + `WatchPauseGate` + the clock; `ReportWatchAttention`, `ReportContactTransitions`, `ReportOwnSideLoss`, `PauseSim` / `ResumeSim` / `TryResumeSim`, `Tick` vs `TickHeadless`. |
| [`WatchAttentionQueueProjection.cs`](../../src/ProjectAegis.Delegation/Projection/WatchAttentionQueueProjection.cs) | Read-only UI contract: `ProjectVisible`, `ProjectUnresolvedCount`, `ProjectPauseReasonLabel`. |

---

## 1. The sim clock controls (`SimClock`)

[`SimClock`](../../src/ProjectAegis.Sim/Time/SimClock.cs) is a fixed-step counter. It does **not**
stretch `FixedDeltaSeconds`; instead:

- **Pause** sets a flag. `SimTickPipeline.TickOnce(mode)` early-returns when `IsPaused` is true and
  `mode != HeadlessBatch`, so `SimTick` does not advance.
- **Acceleration** is applied by running *multiple full pipeline steps* per call. `AccelerationFactor`
  is clamped to `[1, 256]` by `SetAccelerationFactor`.

`SimulationSession` exposes the interactive surface (no `DelegationBridge` involvement):

```csharp
session.PauseSim();                    // Sim.Clock.Pause()
session.ResumeSim();                   // Sim.Clock.Resume() — ungated (legacy callers)
bool ok = session.TryResumeSim();      // gated by the watch queue (see §3)
session.IsSimPaused;                   // Sim.Clock.IsPaused
session.SetTimeAccelerationFactor(4);  // Sim.Clock.SetAccelerationFactor(4)
session.TimeAccelerationFactor;        // Sim.Clock.AccelerationFactor
```

### How pause / accel behave inside a tick

`SimulationSession.Tick(state)` funnels through `RunExecutingTick(state, headlessOverride: false)`,
which:

1. runs `Orchestrator.Tick(state)` — **the delegation decision tick always runs**; it is not gated
   by the sim clock;
2. if the clock is paused (and not a headless override), surfaces any ROE-denied engagements to the
   order log and **returns before the engagement phase** — so no engagements are enqueued/resolved
   and `SimTick` does not advance (verified by `PauseSim_does_not_strand_pending_engagements` and
   `PauseSim_freezes_SimClock_SimTick_across_Tick`);
3. otherwise resolves engagements via `Sim.TickOnce(...)`, then runs
   `max(0, AccelerationFactor - 1)` **extra** `TickOnce` steps so a single interactive `Tick`
   advances `AccelerationFactor` sim ticks (verified by
   `Tick_with_acceleration_greater_than_one_advances_multiple_SimTicks`). Engagements are enqueued
   only on the first step; the extra steps advance the clock and re-fold the world hash.

> **Nuance:** pause gates the **sim engagement phase and clock advance**, not the delegation
> decision tick itself. If a host keeps pumping `session.Tick(...)` while paused, agents still
> *decide*; their engage orders are simply never resolved and the clock is frozen. Interactive
> hosts stop pumping (or rely on auto-pause) to freeze the picture.

---

## 2. The watch-attention queue (`WatchAttentionQueue`)

A session-local, single-threaded, pure list of `WatchAttentionCard`s. It mirrors the
`PendingApprovalQueue` pattern — no `DelegationBridge`, no RNG, no clock ownership.

**Ordering** is a stable sort re-applied on every insert:

1. `Priority` (Critical first), then
2. `TriggerTick` ascending, then
3. `EventId` ordinal.

**Idempotency:** `Enqueue` is a no-op if a card with the same `EventId` already exists. Re-emitting
the same fact (e.g. the same contact re-detected) never produces a duplicate card
(`Distinct_contacts_each_emit_stable_event_and_remain_pause_class`).

**Presentation-only state:** `TryAcknowledge`, `TryDismiss`, and `TryRestore` flip flags on the
card; they never remove sim state. `SnapshotVisible()` returns non-dismissed cards in sort order
(acknowledged cards stay visible, styled by the UI). `Clear()` empties the queue on session reset /
scenario change.

**Resolution accounting:** a card `IsUnresolved` when it is *pause-class* **and** neither
acknowledged nor dismissed. `UnresolvedPauseClassCount` / `HasUnresolvedPauseClass` drive the badge
and the resume gate.

---

## 3. The auto-pause gate (`WatchAutoPauseGate`)

The gate decides two things and **does not own the clock** — the session calls `PauseSim` /
`ResumeSim`:

- **`ShouldAutoPause(evt)`** returns `true` for a pause-class event and records `LastPauseReason`
  (`HostileOrUnknownContact` or `OwnSideLossOrDamage`). Non-pause-class events never auto-pause.
- **`CanResume(queue, explicitOverride)`** returns `true` when there are **zero** unresolved
  pause-class cards, **or** when `explicitOverride` is `true` (player force-resume).

The session ties them together:

```csharp
public void ReportWatchAttention(WatchAttentionEvent evt)
{
    WatchQueue.Enqueue(evt);
    if (WatchPauseGate.ShouldAutoPause(evt)) PauseSim();
}

public bool TryResumeSim(bool explicitOverride = false)
{
    if (!WatchPauseGate.CanResume(WatchQueue, explicitOverride)) return false;
    ResumeSim();
    WatchPauseGate.ClearReason();
    return true;
}
```

Behaviour (from `SimulationSessionWatchAttentionTests`):

- A hostile/unknown contact or own-side loss event **auto-pauses** and sets `LastWatchPauseReason`.
- `TryResumeSim(explicitOverride: false)` **fails** while an unresolved pause-class card remains;
  `explicitOverride: true` **succeeds** and clears the reason.
- Acknowledging (or dismissing) the card resolves it, after which `TryResumeSim()` succeeds.
- `ResumeSim()` is the **ungated legacy path**: it resumes without consulting the queue and
  intentionally does **not** clear `LastWatchPauseReason`. Prefer `TryResumeSim` in new code.

`ProjectPauseReasonLabel` maps the reason enum to a display string ("Hostile / unknown contact",
"Own-side loss / damage", "Player pause"); the sim only ever stores the enum.

---

## 4. Headless batch bypasses the pause

CI, the replay goldens, and the QA Gauntlet must run to completion regardless of auto-pause.
`SimulationSession.TickHeadless(state)` runs `RunExecutingTick(..., headlessOverride: true)`, which
uses `TimeCompressionMode.HeadlessBatch`. `SimTickPipeline.TickOnce` treats `HeadlessBatch` as the
one mode that advances **even when `IsPaused` is true**.

Critically, the headless path **preserves** the pause flag and `LastWatchPauseReason`, so an
interactive session that later resumes still sees the correct state
(`TickHeadless_advances_despite_auto_pause`). This mirrors the pipeline-level `HeadlessBatch`
override and never touches `DelegationBridge`.

---

## 5. Emitting events from facts (`WatchAttentionEmitFactory`)

S116 wires real sim facts into the queue with **pure, idempotent** factories (stable `EventId`s so
re-emission dedups). The session exposes three call-sites:

| Session method | Fires when | EventId |
|----------------|-----------|---------|
| `ReportContactTransitions(transitions)` | A contact goes `Unknown → Detected/Classified/Identified` for a **non-own-side** subject (hostile ⇒ `Critical`, other unknown/neutral ⇒ `High`), **or** an own-side contact transitions to `Lost`. | `watch:contact:<targetId>` / `watch:loss:<unitId>` |
| `ReportOwnSideLoss(unitId, tick, detail)` | `unitId` is an own-side unit (catalog blue, or legacy `u1`). No-op for hostiles. | `watch:loss:<unitId>` |

"Own-side" is decided by `WatchAttentionEmitFactory.IsOwnSideUnit` — legacy primary blue id `u1`
or `BalticV3SideRegistry.IsBlueForceUnit`. Hostile losses stay **silent** (they are not pause-class
for the watch officer). The catalog-damage BDA slice calls `ReportOwnSideLoss(targetId, tick,
"bda:lost")` when an own-side platform is marked lost (see
[`SimulationSession.ApplyBdaContactLifecycleHotTick`](../../src/ProjectAegis.Delegation/Orchestration/SimulationSession.cs)).

Both call-sites route through `ReportWatchAttention`, so emitting a pause-class fact both enqueues
the card **and** auto-pauses the clock in one step.

---

## 6. What this is *not*

- **Not the AI cognitive-load model.** `Delegation.Attention` (`AttentionCalculator`, budget
  multipliers, graceful overload) models how much an *agent* can process per tick and is documented
  in [agent-traits-and-attention.md](agent-traits-and-attention.md). `Watch` attention is the
  *human* watch-officer interrupt surface. They share a word, not code.
- **Not the message-log severity tiering.** The C2 `AlertSeverity` /`AlertSeverityMap`
  (`Critical` / `Notable` / `Routine`) in the [C2 projection layer](c2-projection-layer.md) tier
  *log lines* for toast/highlight routing. The watch queue is the narrower "must-pause" set. A
  future refinement could feed one from the other, but today they are independent.

---

## Determinism & boundaries

- **Replay-safe.** Nothing here is written into the fingerprinted `DecisionLog` / order-log hash.
  Pause/resume, ack/dismiss, acceleration, and the queue are session/presentation state. The Baltic
  v2 replay hash `17144800277401907079` is unaffected.
- **No RNG, no clock ownership in `Watch/`.** The queue, gate, and factory are pure; only
  `SimulationSession` calls `Pause()` / `Resume()`.
- **Idempotent by `EventId`.** Stable ids make re-emission safe, which is what lets the emit
  factory be called every tick from the sensor/BDA path without Bridge edits.
- **`netstandard2.1` note:** the `Watch/` types guard nulls with explicit
  `throw new ArgumentNullException(...)` rather than `ArgumentNullException.ThrowIfNull` (net5+
  only), because the Delegation assembly targets `netstandard2.1` for the Unity adapter.

## Pinned by tests

Behaviour above is covered by ~40 tests:

- [`WatchAttentionQueueTests`](../../src/ProjectAegis.Delegation.Tests/Watch/WatchAttentionQueueTests.cs) — ordering, idempotency, ack/dismiss/restore, unresolved count.
- [`WatchAttentionEmitFactoryTests`](../../src/ProjectAegis.Delegation.Tests/Watch/WatchAttentionEmitFactoryTests.cs) — contact/loss classification + stable ids.
- [`SimulationSessionWatchAttentionTests`](../../src/ProjectAegis.Delegation.Tests/Orchestration/SimulationSessionWatchAttentionTests.cs) — auto-pause + gated resume + headless override.
- [`SimulationSessionWatchEmitTests`](../../src/ProjectAegis.Delegation.Tests/Orchestration/SimulationSessionWatchEmitTests.cs) — the `ReportContactTransitions` / `ReportOwnSideLoss` call-sites.
- [`SimulationSessionClockControlsTests`](../../src/ProjectAegis.Delegation.Tests/Orchestration/SimulationSessionClockControlsTests.cs) — pause/resume/accel at session level.
- [`SimClockTests`](../../src/ProjectAegis.Sim.Tests/Time/SimClockTests.cs) / [`SimClockTickRunnerTests`](../../src/ProjectAegis.Sim.Tests/Time/SimClockTickRunnerTests.cs) — the clock + tick-runner primitives.

---

## Extending it

- **New pause-class kind:** add a `WatchAttentionKind`, mark it in
  `WatchAttentionEvent.IsPauseClass`, map it to a `WatchPauseReason` in
  `WatchAutoPauseGate.ShouldAutoPause`, add a label in `ProjectPauseReasonLabel`, and add a factory
  method + stable `EventId` prefix in `WatchAttentionEmitFactory`. Keep the factory pure and the id
  deterministic.
- **New emit source:** call `session.ReportWatchAttention(evt)` (or add a `TryFrom…` factory that a
  new `Report…` method drives). Never enqueue from inside `DelegationBridge` — keep the sim hotpath
  zero-touch.
- **New UI read:** extend `WatchAttentionQueueProjection` with another read-only projection over the
  queue. Hosts bind labels only; they must not re-derive priority, pause class, or resolution.
- **Do not** persist pause/ack/dismiss into the order log or otherwise let it influence engagement
  resolution — that would break replay determinism.
