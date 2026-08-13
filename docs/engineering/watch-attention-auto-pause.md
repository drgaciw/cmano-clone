# Watch attention & auto-pause — developer guide

A single-seat commander cannot watch every track. When something the watch officer *must* see
happens — a hostile or unknown contact appears, or an own-side unit is lost or damaged — the sim
raises a **pause-class attention event**, drops a card on the watch queue, and (interactively)
**auto-pauses the clock** until the player acknowledges it. This page documents that spine:
`ProjectAegis.Delegation/Watch/` plus the `SimulationSession` wiring and the `SimClock`
pause/acceleration controls it drives (PRD **P0-6 / P0-7**, **S112 / S115 / S116**, **DRG-14**).

The whole thing is **session-local and presentation-side**: watch events never enter the
fingerprinted decision/order log, and the headless/CI path advances the sim *through* an
auto-pause (see [Determinism](#determinism--replay-safety)), so replay goldens are untouched.

> **Not the AI attention model.** `Delegation.Watch` (this doc) is the *player's* alert queue —
> what a human watch officer must react to. The unrelated `Delegation.Attention` subsystem models
> an *AI agent's* cognitive load / overload degradation and is documented in
> [`agent-traits-and-attention.md`](agent-traits-and-attention.md). Same word, different actor.

- **Source:** the watch types in
  [`src/ProjectAegis.Delegation/Watch/`](../../src/ProjectAegis.Delegation/Watch/)
  (`WatchAttentionEvent`, `WatchAttentionKind`, `WatchAttentionPriority`, `WatchAttentionCard`,
  `WatchAttentionQueue`, `WatchAutoPauseGate`, `WatchPauseReason`, `WatchAttentionEmitFactory`);
  the session wiring in
  [`SimulationSession.cs`](../../src/ProjectAegis.Delegation/Orchestration/SimulationSession.cs);
  the read-model in
  [`WatchAttentionQueueProjection.cs`](../../src/ProjectAegis.Delegation/Projection/WatchAttentionQueueProjection.cs);
  the clock in [`SimClock.cs`](../../src/ProjectAegis.Sim/Time/SimClock.cs) +
  [`SimTickPipeline.cs`](../../src/ProjectAegis.Sim/Core/SimTickPipeline.cs) +
  [`TimeCompressionMode.cs`](../../src/ProjectAegis.Sim/Time/TimeCompressionMode.cs).
- **Related:** the alert-severity tiering in
  [`c2-projection-layer.md`](c2-projection-layer.md); the contact FSM that produces the
  transitions this consumes in [`detection-pipeline.md`](detection-pipeline.md); the BDA "Lost"
  own-side loss source in
  [`catalog-damage-readiness-runtime.md`](catalog-damage-readiness-runtime.md); determinism rules
  in [`determinism-and-replay.md`](determinism-and-replay.md).

---

## The data model

Everything downstream keys off one immutable event.

| Type | Kind | Notes |
|------|------|-------|
| [`WatchAttentionEvent`](../../src/ProjectAegis.Delegation/Watch/WatchAttentionEvent.cs) | `sealed record` | `(EventId, Kind, Priority, TriggerTick, SubjectId, GroupingKey?, ReasonDetail?)`. `EventId` must be **stable** for a given fact so re-emission is idempotent. `IsPauseClass` is true for both current kinds. |
| [`WatchAttentionKind`](../../src/ProjectAegis.Delegation/Watch/WatchAttentionKind.cs) | `enum : byte` | `HostileOrUnknownContact = 0`, `OwnSideLossOrDamage = 1`. |
| [`WatchAttentionPriority`](../../src/ProjectAegis.Delegation/Watch/WatchAttentionPriority.cs) | `enum : byte` | `Critical = 0` → `High = 1` → `Normal = 2` → `Low = 3` (**lower ordinal = higher priority**; drives queue sort). |
| [`WatchPauseReason`](../../src/ProjectAegis.Delegation/Watch/WatchPauseReason.cs) | `enum : byte` | Headless reason code for *why* the clock auto-paused: `None`, `HostileOrUnknownContact`, `OwnSideLossOrDamage`, `ExplicitPlayer`. Presentation maps it to a label; the sim only stores the enum. |
| [`WatchAttentionCard`](../../src/ProjectAegis.Delegation/Watch/WatchAttentionCard.cs) | `sealed record` | Presentation wrapper: the source event + `IsAcknowledged` / `IsDismissed`. `IsUnresolved = !Ack && !Dismissed && IsPauseClass` — this is what gates resume. |

`GroupingKey` (raid/formation) and `ReasonDetail` (free-text) are **data-only** and never hashed;
raid grouping in the UI is a deferred P1 concern.

---

## The queue

[`WatchAttentionQueue`](../../src/ProjectAegis.Delegation/Watch/WatchAttentionQueue.cs) is a pure,
single-threaded, session-local list — **no Bridge, no RNG, no clock** (same shape as
`PendingApprovalQueue`).

- **Ordering** (re-sorted on every insert): `Priority` (Critical first) → `TriggerTick` ascending →
  `EventId` ordinal. Deterministic and total.
- **Idempotent enqueue.** `Enqueue` is a no-op when a card with the same `EventId` already exists,
  so re-firing the same fact each tick never duplicates a card. Throws on a null event or an
  empty `EventId`.
- **Ack / dismiss / restore are presentation-only.** `TryAcknowledge` (keeps the card visible,
  marks it resolved), `TryDismiss` (soft-removes from the default view), `TryRestore` (un-dismiss).
  None of them mutate sim policy or the order log; all return `false` on an unknown id.
- **Resolve gate.** `UnresolvedPauseClassCount` / `HasUnresolvedPauseClass` count pause-class cards
  that are neither acknowledged nor dismissed — the auto-pause gate reads exactly this.
- `SnapshotVisible()` returns the non-dismissed cards in order (ack state preserved for styling);
  `Clear()` resets on scenario change.

---

## The auto-pause gate

[`WatchAutoPauseGate`](../../src/ProjectAegis.Delegation/Watch/WatchAutoPauseGate.cs) decides
*whether to pause* and *whether resume is allowed*. It **does not own the clock** — the session
calls `PauseSim` / `ResumeSim`.

- `ShouldAutoPause(evt)` → `true` for a pause-class event, and records the matching
  `LastPauseReason`. Non-pause-class kinds return `false` and never pause.
- `CanResume(queue, explicitOverride)` → `true` when there are **zero unresolved pause-class
  cards**, *or* when `explicitOverride` is set (player force-resume).
- `ClearReason()` resets `LastPauseReason` to `None` after a clean resume.

So the interactive loop is: pause-class event ⇒ card enqueued ⇒ clock paused ⇒ the player must
acknowledge/dismiss every unresolved card **or** force-resume before the clock runs again.

---

## Emitting events (the fact → event factories)

[`WatchAttentionEmitFactory`](../../src/ProjectAegis.Delegation/Watch/WatchAttentionEmitFactory.cs)
is a pure static mapper from a sim fact to a `WatchAttentionEvent` with a **stable `EventId`**
(so the idempotent queue de-dups automatically). No Bridge, no RNG, no clock.

| Factory | Fires on | EventId | Priority |
|---------|----------|---------|----------|
| `TryFromFirstHostileOrUnknownContact(in ContactTransition)` | `Unknown → Detected/Classified/Identified` for a **non-own-side** track | `watch:contact:{TargetId}` | `Critical` if the subject is a catalog-hostile/engageable id, else `High` (unknown/neutral) |
| `TryFromOwnSideLoss(unitId, tick, detail)` | An **own-side** unit loss / battle-damage | `watch:loss:{unitId}` | `Critical` |
| `TryFromOwnSideLostTransition(in ContactTransition)` | Own-side contact FSM → `Lost` (delegates to `TryFromOwnSideLoss` with `"lifecycle:Lost"`) | `watch:loss:{TargetId}` | `Critical` |

Side membership comes from `IsOwnSideUnit(unitId)` — the legacy primary blue id `"u1"` or any
`BalticV3SideRegistry.IsBlueForceUnit` id. Hostile classification uses
`HostileContactFilter.IsEngageableHostileTarget` (the same predicate the detection/engage picture
uses). `ContactTransition` is the tick-4 sensor-slice transition from
[`detection-pipeline.md`](detection-pipeline.md) — the factory only re-reads it, it never re-derives
the FSM.

---

## Session wiring

[`SimulationSession`](../../src/ProjectAegis.Delegation/Orchestration/SimulationSession.cs) owns the
queue + gate and exposes the interactive surface:

| Member | Does |
|--------|------|
| `WatchQueue` / `WatchPauseGate` | The live queue and gate (per session). |
| `ReportWatchAttention(evt)` | Enqueue, then `PauseSim()` if `ShouldAutoPause`. The single choke point. |
| `ReportContactTransitions(transitions)` | Run both contact factories over a tick's transitions (first-hostile/unknown + own-side Lost). Available seam for the sensor/harness path. |
| `ReportOwnSideLoss(unitId, tick, detail?)` | Own-side BDA/battle-damage loss; no-op for non-own-side ids. |
| `IsSimPaused` / `PauseSim()` / `ResumeSim()` | Direct clock control (`ResumeSim` is the **ungated** legacy path; it does not clear the reason). |
| `TryResumeSim(explicitOverride = false)` | **Gated** resume: fails (returns `false`, stays paused) while unresolved pause-class cards remain unless overridden; clears the reason on success. |
| `LastWatchPauseReason` | The gate's `LastPauseReason`, for the pause banner label. |

**Wired call-site (S116):** own-side BDA loss auto-emits. When the catalog-damage hot-tick marks an
own-side target `Lost`, the session calls `ReportOwnSideLoss(targetId, simTick, "bda:lost")`
(hostile losses stay silent here). The contact-transition emit (`ReportContactTransitions`) is a
**ready seam** the sensor/harness path can call — it is intentionally not wired through
`DelegationBridge`, keeping the Bridge zero-touch.

---

## The clock: pause + acceleration

[`SimClock`](../../src/ProjectAegis.Sim/Time/SimClock.cs) is a fixed-step clock with two knobs the
session drives:

- **Pause.** `Pause()` / `Resume()` toggle `IsPaused`. A paused clock is honored by the tick runner
  and by the interactive session tick — but **not** by the headless path (below).
- **Acceleration.** `SetAccelerationFactor(int)` clamps to `[1, 256]`
  (`MinAccelerationFactor`/`MaxAccelerationFactor`). Acceleration is applied by running **multiple
  full pipeline steps** per call, *not* by stretching `FixedDeltaSeconds` — every step still runs
  the full ADR-004 tick (including engagement + world-hash), so a fast-forwarded run is bit-for-bit
  identical to the same number of real-time ticks. The session exposes this as
  `TimeAccelerationFactor` / `SetTimeAccelerationFactor`.

[`TimeCompressionMode`](../../src/ProjectAegis.Sim/Time/TimeCompressionMode.cs) selects the tick
runner's behavior:

| Mode | Paused clock | Steps per `TickOnce` |
|------|--------------|----------------------|
| `RealTime` | no-op | 1 |
| `Accelerated` | no-op | `AccelerationFactor` |
| `HeadlessBatch` | **overrides pause** (advances) | 1 |

`SimTickPipeline.TickOnce(mode)` short-circuits when `Clock.IsPaused && mode != HeadlessBatch`.

**Interactive vs headless in the session.** `SimulationSession.Tick` (interactive) runs
`RunExecutingTick` with `headlessOverride: false`: if the clock is paused it surfaces ROE-denied
engagements and returns without advancing — the sim freezes for the watch officer. `TickHeadless`
runs with `headlessOverride: true` (mode `HeadlessBatch`), so **CI/batch/replay advance despite an
auto-pause**, and the pause flag + reason are preserved so an interactive resume still works after
the batch. Session-level acceleration replays `AccelerationFactor − 1` extra `TickOnce` calls after
the primary step.

---

## Determinism & replay safety

- **Watch state never enters the fingerprint.** Events go into the session-local `WatchQueue`
  only; they are never appended to the `DecisionLog` / `OrderLog`, so they cannot move the Baltic
  v2 replay hash (`17144800277401907079`). `EventId`, `GroupingKey`, and `ReasonDetail` are not
  hashed.
- **Auto-pause cannot stall a golden.** Replay/QA run the headless path, which advances through a
  pause. The pause is a live-play affordance, not a sim-authority state change.
- **Ordering is total and deterministic** (`Priority → TriggerTick → EventId`), so a projection
  snapshot is stable across runs.
- **Zero Bridge.** All wiring is at the session/factory level; `DelegationBridge` is untouched,
  per the Release-v1 zero-touch invariant.

---

## Presentation seam

[`WatchAttentionQueueProjection`](../../src/ProjectAegis.Delegation/Projection/WatchAttentionQueueProjection.cs)
projects the queue into a stable UI contract — Unity hosts **bind labels only** and never
re-derive priority or pause class:

- `ProjectVisible(queue)` — the ordered non-dismissed cards.
- `ProjectUnresolvedCount(queue)` — the badge / auto-pause-gating count.
- `ProjectPauseReasonLabel(reason)` — the human string for the pause banner
  (`"Hostile / unknown contact"`, `"Own-side loss / damage"`, `"Player pause"`, or empty).

Alert tiering (`Critical` → toast + optional auto-pause) lives in the C2 layer's `AlertSeverity`;
see [`c2-projection-layer.md`](c2-projection-layer.md).

---

## Runbook: add a watch attention kind

1. Add the case to `WatchAttentionKind` (and, if it should freeze the clock, to `WatchAttentionEvent.IsPauseClass`, `WatchPauseReason`, and the `WatchAutoPauseGate.ShouldAutoPause` switch).
2. Add a pure `TryFrom…` factory on `WatchAttentionEmitFactory` with a **stable, collision-free `EventId` prefix**; keep it Bridge-/RNG-/clock-free.
3. Wire the emit call-site at the session level (e.g. from a hot-tick applier or `ReportContactTransitions`), routing through `ReportWatchAttention`.
4. Extend `WatchAttentionQueueProjection.ProjectPauseReasonLabel` if you added a reason.
5. Add tests mirroring `WatchAttentionEmitFactoryTests` / `WatchAttentionQueueTests` / `SimulationSessionWatchAttentionTests`; confirm the emit is **idempotent** and does **not** touch the order log or the replay hash.
