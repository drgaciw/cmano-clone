# Watch attention & auto-pause runtime — developer guide

The **watch attention** subsystem is the "the watch officer must look at this *now*" layer:
when the sim first sees a hostile/unknown contact or takes an own-side loss, it enqueues an
ordered **attention card** and — for pause-class events — **auto-pauses the simulation clock** so
the player can react. Resume is then gated until the outstanding cards are acknowledged or
dismissed (or the player force-resumes). It is the runtime behind PRD **P0-6** (auto-pause on
significant events) and **P0-7** (watch attention queue), landed in **S115** (queue + gate) and
wired to real sensor/BDA facts in **S116**.

This is deliberately **distinct** from the AI cognitive-load model in
[`Delegation/Attention/`](../../src/ProjectAegis.Delegation/Attention/) (agent bandwidth /
graceful overload — see [agent-traits-and-attention.md](agent-traits-and-attention.md)). That
model decides *how well the AI thinks*; this one decides *when to interrupt the human*.

- **Source:** [`src/ProjectAegis.Delegation/Watch/`](../../src/ProjectAegis.Delegation/Watch/)
  (the queue, gate, event/factory, and reason enum), the read-model
  [`WatchAttentionQueueProjection`](../../src/ProjectAegis.Delegation/Projection/WatchAttentionQueueProjection.cs)
  in the [projection layer](c2-projection-layer.md), and the sim-clock plumbing in
  [`src/ProjectAegis.Sim/Time/`](../../src/ProjectAegis.Sim/Time/).
- **Host:** [`SimulationSession`](../../src/ProjectAegis.Delegation/Orchestration/SimulationSession.cs)
  owns the queue, the gate, and the clock, and exposes the whole surface without any
  `DelegationBridge` hotpath edit (the Spirit-1 frozen-hub contract stays intact).
- **Related:** the contact transitions that feed it come from the
  [detection pipeline](detection-pipeline.md); the pause/acceleration clock is part of the tick
  loop below; determinism rules are in [determinism-and-replay.md](determinism-and-replay.md).

> **Pure, session-local, presentation-restorable.** Everything here is single-threaded, allocates
> no ambient state, uses no RNG or wall-clock, and never writes the order log or the sim. Acknowledge
> / dismiss / restore are presentation-only and reversible. So the whole subsystem is replay-safe:
> turning auto-pause on or off does not change any golden fingerprint.

---

## Where it lives

| File | Role |
|------|------|
| [`WatchAttentionKind.cs`](../../src/ProjectAegis.Delegation/Watch/WatchAttentionKind.cs) | The two pause-class kinds: `HostileOrUnknownContact(0)`, `OwnSideLossOrDamage(1)`. |
| [`WatchAttentionPriority.cs`](../../src/ProjectAegis.Delegation/Watch/WatchAttentionPriority.cs) | Queue ordering priority: `Critical(0) → High(1) → Normal(2) → Low(3)` (lower ordinal = higher priority). |
| [`WatchAttentionEvent.cs`](../../src/ProjectAegis.Delegation/Watch/WatchAttentionEvent.cs) | Immutable event: `(EventId, Kind, Priority, TriggerTick, SubjectId, GroupingKey?, ReasonDetail?)` + the `IsPauseClass` predicate. |
| [`WatchAttentionCard.cs`](../../src/ProjectAegis.Delegation/Watch/WatchAttentionCard.cs) | Presentation card wrapping an event with `IsAcknowledged` / `IsDismissed`; derives `IsUnresolved`. |
| [`WatchAttentionQueue.cs`](../../src/ProjectAegis.Delegation/Watch/WatchAttentionQueue.cs) | Session-local ordered queue: idempotent `Enqueue`, `TryAcknowledge` / `TryDismiss` / `TryRestore`, `SnapshotVisible`. |
| [`WatchAttentionEmitFactory.cs`](../../src/ProjectAegis.Delegation/Watch/WatchAttentionEmitFactory.cs) | Pure fact → event factories (first hostile/unknown contact, own-side loss / `Lost` transition) with stable `EventId`s. |
| [`WatchAutoPauseGate.cs`](../../src/ProjectAegis.Delegation/Watch/WatchAutoPauseGate.cs) | Decides whether an enqueue should auto-pause, and whether resume is allowed; holds the last `WatchPauseReason`. |
| [`WatchPauseReason.cs`](../../src/ProjectAegis.Delegation/Watch/WatchPauseReason.cs) | Headless reason code: `None / HostileOrUnknownContact / OwnSideLossOrDamage / ExplicitPlayer`. |
| [`WatchAttentionQueueProjection.cs`](../../src/ProjectAegis.Delegation/Projection/WatchAttentionQueueProjection.cs) | Read-model: visible cards, unresolved count, pause-reason label for the UI. |
| [`SimClock.cs`](../../src/ProjectAegis.Sim/Time/SimClock.cs) / [`TimeCompressionMode.cs`](../../src/ProjectAegis.Sim/Time/TimeCompressionMode.cs) | Fixed-step clock with `Pause` / `Resume` + `AccelerationFactor`, and the `RealTime / Accelerated / HeadlessBatch` tick modes. |

---

## The queue (`WatchAttentionQueue`)

A session-local ordered list of cards. The pattern mirrors `PendingApprovalQueue` — pure,
single-threaded, no Bridge. Two things matter: the **ordering** and the **idempotency**.

**Ordering (total, stable):** after every insert the queue re-sorts by

```
Priority (Critical first) → TriggerTick ascending → EventId ordinal
```

so the most urgent, earliest, deterministically-tie-broken card is always first — never
dictionary/hash enumeration order.

**Idempotency:** `Enqueue` is keyed on `EventId`. A second event with the same id is dropped, so a
fact that re-fires every tick (a contact that stays detected, a unit that stays lost) produces
exactly one card. `EventId` must therefore be **stable per fact** — the emit factory guarantees this
(see below). A `null` event throws `ArgumentNullException`; an empty `EventId` throws
`ArgumentException`.

**Acknowledge / dismiss / restore** are presentation-only mutations on the card, not the event:

- `TryAcknowledge(eventId)` — marks the card seen; it stays visible so the UI can style it. Idempotent.
- `TryDismiss(eventId)` — soft-removes it from the default view (`SnapshotVisible` filters dismissed).
- `TryRestore(eventId)` — clears the dismiss flag. All three return `false` only when the id is unknown.

`SnapshotVisible()` returns non-dismissed cards in sort order (acknowledged ones included, so the
UI can grey them out). `Clear()` empties the queue on a session reset / scenario change.

### Pause-class resolution

A card is **unresolved pause-class** when it `IsPauseClass` **and** is neither acknowledged nor
dismissed (`WatchAttentionCard.IsUnresolved`). The queue exposes:

- `UnresolvedPauseClassCount` — drives the watch badge and the resume gate;
- `HasUnresolvedPauseClass` — the boolean the auto-pause gate checks on resume.

Both current kinds are pause-class; the `IsPauseClass` predicate exists so future non-pausing
attention kinds (informational cards) can share the queue without auto-pausing.

---

## The emit factory (`WatchAttentionEmitFactory`)

Pure `fact → WatchAttentionEvent` conversions. No Bridge, no RNG, no clock ownership — just
stable-id construction so the queue's idempotency holds. Own-side membership is decided by
`IsOwnSideUnit` = catalog blue (`BalticV3SideRegistry.IsBlueForceUnit`) **or** the legacy primary
blue id `"u1"`.

| Factory | Fires when | EventId | Kind / Priority |
|---------|-----------|---------|-----------------|
| `TryFromFirstHostileOrUnknownContact(transition)` | A [`ContactTransition`](../../src/ProjectAegis.Sim/Sensors/ContactTransition.cs) goes `Unknown → Detected/Classified/Identified` for a **non-own-side** subject | `watch:contact:{targetId}` | `HostileOrUnknownContact`; **`Critical`** if the target is an engageable hostile (`HostileContactFilter.IsEngageableHostileTarget`), else **`High`** (unknown/neutral track) |
| `TryFromOwnSideLoss(unitId, tick, detail)` | An **own-side** unit takes a loss / battle-damage (BDA hook) | `watch:loss:{unitId}` | `OwnSideLossOrDamage`; **`Critical`** |
| `TryFromOwnSideLostTransition(transition)` | An **own-side** contact transitions to `Lost` (delegates to `TryFromOwnSideLoss` with `detail = "lifecycle:Lost"`) | `watch:loss:{targetId}` | `OwnSideLossOrDamage`; **`Critical`** |

Each returns `false` (and a `null` event) when the fact doesn't qualify — a promotion that didn't
start from `Unknown`, a blank subject, an own-side contact on the *hostile* path, or a non-own-side
unit on the *loss* path. This keeps the call sites (below) a simple "try, and report if it fired"
loop. The contact factory copies the transition's `ContactId` into `GroupingKey` (data only; raid /
formation UI grouping is a P1 concern), and records a short `ReasonDetail`
(`"hostile Unknown->Detected"`, etc.) for the projection — the detail string is **not** hashed.

---

## The auto-pause gate (`WatchAutoPauseGate`)

A tiny policy object that decides **pause** and **resume** but never touches the clock itself — the
session owns `Pause` / `Resume` (below). This separation keeps the gate trivially testable.

- **`ShouldAutoPause(evt)`** — after a successful enqueue, returns `true` only for a pause-class
  event, and sets `LastPauseReason` from the kind (`HostileOrUnknownContact` / `OwnSideLossOrDamage`).
  A non-pause-class event returns `false` and leaves the reason `None`.
- **`CanResume(queue, explicitOverride)`** — `true` when there are **zero** unresolved pause-class
  cards, **or** when the player force-resumes (`explicitOverride`). This is the whole "you must clear
  the board before time runs again — unless you insist" rule.
- **`ClearReason()`** — resets `LastPauseReason` to `None` after a clean resume.

> **Headless / CI never gets stuck.** The `HeadlessBatch` override that lets batch and replay runs
> advance through a paused clock lives in `SimTickPipeline` / the session's `TickHeadless` path — not
> in the gate — so the gate's pause/resume logic stays pure and the QA Gauntlet and replay goldens
> run unattended regardless of pause-class cards.

---

## The sim clock: pause & acceleration (`SimClock`)

[`SimClock`](../../src/ProjectAegis.Sim/Time/SimClock.cs) is a **fixed-step** clock (default
`1/60 s`), landed in **S112**. Acceleration is applied by running *multiple full steps per call*,
**not** by stretching `FixedDeltaSeconds` — so every tick is identical whether the sim runs at 1× or
256×, and acceleration never perturbs determinism.

| Member | Meaning |
|--------|---------|
| `IsPaused` | When `true`, interactive tick modes do not advance. Default `false`. |
| `Pause()` / `Resume()` | Toggle the pause flag. |
| `AccelerationFactor` | Steps per `Accelerated` tick. Default `1`; `SetAccelerationFactor` clamps to `[1, 256]`. |
| `AdvanceOneTick()` / `Reset(startTick)` | Advance / reset `SimTick` (`SimTime = SimTick × FixedDeltaSeconds`). |

[`TimeCompressionMode`](../../src/ProjectAegis.Sim/Time/TimeCompressionMode.cs) selects behaviour in
`SimTickPipeline.TickOnce(mode)`:

| Mode | Behaviour |
|------|-----------|
| `RealTime(1)` | One step; **no-op when paused**. |
| `Accelerated(2)` | `AccelerationFactor` full steps; each step runs the engagement slice; no-op when paused. |
| `HeadlessBatch(3)` | One step; **overrides pause** (CI / batch / replay). |

---

## `SimulationSession` integration

`SimulationSession` is the composition root. It owns one `WatchAttentionQueue`, one
`WatchAutoPauseGate`, and the `SimTickPipeline` (hence the `SimClock`), and exposes the full surface
— all without editing `DelegationBridge`.

**Clock / resume surface:**

```csharp
bool IsSimPaused                => Sim.Clock.IsPaused;
void PauseSim()                 => Sim.Clock.Pause();
void ResumeSim()                => Sim.Clock.Resume();
WatchPauseReason LastWatchPauseReason => WatchPauseGate.LastPauseReason;

bool TryResumeSim(bool explicitOverride = false); // gate-checked resume
int  TimeAccelerationFactor;    void SetTimeAccelerationFactor(int factor);
```

`TryResumeSim` asks the gate `CanResume(WatchQueue, explicitOverride)` first; only on success does it
`ResumeSim()` and `ClearReason()`. So a plain `TryResumeSim()` is refused (returns `false`) while any
pause-class card is unresolved, and `TryResumeSim(explicitOverride: true)` always resumes.

**Reporting facts (the write path into the queue):**

- `ReportWatchAttention(evt)` — enqueues, then `PauseSim()` **iff** `WatchPauseGate.ShouldAutoPause(evt)`.
  This is the single choke point where an attention event becomes an auto-pause.
- `ReportContactTransitions(transitions)` — the S116 sensor hook: for each `ContactTransition`, try
  the *first hostile/unknown* factory and the *own-side Lost* factory, reporting whatever fires.
  Callable from the harness/sensor path with no Bridge edit.
- `ReportOwnSideLoss(unitId, triggerTick, reasonDetail?)` — the BDA / battle-damage hook; a no-op for
  non-own-side ids.

**Tick loop (where pause & acceleration take effect):** both `Tick` (interactive) and `TickHeadless`
(CI/batch) call the private `RunExecutingTick(state, headlessOverride)`:

1. Always run `Orchestrator.Tick(state)` (the decision tick still produces gated, logged orders).
2. **If `Sim.Clock.IsPaused && !headlessOverride`**, surface any ROE policy-denied engagements and
   **return early** — the engage/kill-chain pipeline is skipped while paused. `TickHeadless`
   (`headlessOverride: true`) never takes this branch, so batch runs advance through a paused clock
   while the pause flag and watch reason are preserved for interactive resume afterwards.
3. Otherwise build the engage-order set, apply the comms new-engagement gate + swarm salvo
   deconfliction, `Sim.TickOnce(mode)` once (`mode = HeadlessBatch` under override else `RealTime`),
   log results, then run `AccelerationFactor − 1` **extra** `Sim.TickOnce(mode)` steps so a higher
   acceleration factor resolves proportionally more engagement sub-steps per session tick.

This is exactly the "pause skips the engage pipeline; acceleration preserves results" contract — the
decision tick and ROE-denial surfacing still happen every tick, but combat only advances when the
clock is running (or overridden).

---

## Read model (`WatchAttentionQueueProjection`)

Unity hosts bind **labels only**; they never re-derive priority or pause class. The projection is the
one-way contract:

- `ProjectVisible(queue)` → ordered non-dismissed cards (priority → tick → id already applied).
- `ProjectUnresolvedCount(queue)` → the unresolved pause-class count for the badge / resume-gate UI.
- `ProjectPauseReasonLabel(reason)` → a headless label string:
  `HostileOrUnknownContact → "Hostile / unknown contact"`, `OwnSideLossOrDamage → "Own-side loss /
  damage"`, `ExplicitPlayer → "Player pause"`, else empty.

See [c2-projection-layer.md](c2-projection-layer.md) for the wider read-only projection contract
these follow.

---

## Determinism & safety notes

- **No RNG, no wall-clock, no order-log writes** anywhere in `Watch/` — the subsystem is a pure
  read/interrupt layer, so enabling or disabling auto-pause cannot move a replay fingerprint.
- **Ack / dismiss / restore are presentation-only** and reversible; they change card view state, not
  the underlying event or the sim.
- **`EventId` is the determinism seam.** Idempotency relies on stable ids (`watch:contact:{id}`,
  `watch:loss:{id}`); a new emit factory must produce an id that is stable for its `(kind, subject,
  trigger)` or it will spam duplicate cards.
- **Acceleration is step-multiplication, not Δt-stretching**, so 1× and 256× produce identical
  per-step results — never widen `FixedDeltaSeconds` to "go faster".
- **Pause must never wedge CI.** Keep the `HeadlessBatch` / `TickHeadless` override intact so batch
  and replay runs always advance.

---

## Extending safely

- **New pause-class kind?** Add it to `WatchAttentionKind`, extend `WatchAttentionEvent.IsPauseClass`
  and the `ShouldAutoPause` / `WatchPauseReason` mapping, and add a `ProjectPauseReasonLabel` arm.
- **New non-pausing attention kind?** Leave it out of `IsPauseClass` — it will queue and display but
  not auto-pause, no other changes needed.
- **New fact source?** Add a pure `TryFrom…` factory with a stable `EventId` and call it from a
  `Report…` method on `SimulationSession` — do **not** reach into `DelegationBridge`.
- **Never** enqueue with an unstable id, mutate sim/order-log state from a factory or the projection,
  or remove the headless pause override.

---

## Tests

| Test file | Covers |
|-----------|--------|
| [`WatchAttentionQueueTests`](../../src/ProjectAegis.Delegation.Tests/Watch/WatchAttentionQueueTests.cs) | Ordering, idempotent enqueue, ack/dismiss/restore, unresolved-count / snapshot semantics. |
| [`WatchAttentionEmitFactoryTests`](../../src/ProjectAegis.Delegation.Tests/Watch/WatchAttentionEmitFactoryTests.cs) | Fact → event mapping, own-side vs hostile/unknown classification, stable ids, non-qualifying no-ops. |
| [`SimulationSessionWatchAttentionTests`](../../src/ProjectAegis.Delegation.Tests/Orchestration/SimulationSessionWatchAttentionTests.cs) | End-to-end report → auto-pause → gated resume, and the headless-override path. |
| [`SimClockTests`](../../src/ProjectAegis.Sim.Tests/Time/SimClockTests.cs) / [`SimClockTickRunnerTests`](../../src/ProjectAegis.Sim.Tests/Time/SimClockTickRunnerTests.cs) | Pause no-op, acceleration clamp + step multiplication, `HeadlessBatch` override. |

Run the delegation + sim suites after any change here:

```bash
dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj -v minimal
dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj -v minimal
```

---

## See also

| Topic | Doc |
|-------|-----|
| AI cognitive-load attention (distinct from this) | [agent-traits-and-attention.md](agent-traits-and-attention.md) |
| Contact transitions that feed the emit factory | [detection-pipeline.md](detection-pipeline.md) |
| The read-only projection contract the read model follows | [c2-projection-layer.md](c2-projection-layer.md) |
| The engage/kill-chain slice that pause skips | [engagement-pipeline.md](engagement-pipeline.md) |
| Seeded RNG domains, world-hash layers, golden workflow | [determinism-and-replay.md](determinism-and-replay.md) |
| Delegation core & the order log | [`src/ProjectAegis.Delegation/README.md`](../../src/ProjectAegis.Delegation/README.md) |
