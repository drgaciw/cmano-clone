# Watch attention & auto-pause runtime — "make the officer look" (S115/S116)

The `Watch/` folder in [`ProjectAegis.Delegation`](../../src/ProjectAegis.Delegation/Watch/)
is the small, pure, session-local runtime that decides **what a human watch officer must not
miss** and, optionally, **pauses the sim clock until they resolve it**. It is the code behind the
PRD P0-6 / P0-7 "significant events" and "auto-pause on hostile contact / own-side loss"
requirements.

This guide explains the event model, the ordered queue, the auto-pause gate, the pure
fact→event factories, how [`SimulationSession`](../../src/ProjectAegis.Delegation/Orchestration/SimulationSession.cs)
wires them into the tick loop, and the determinism/replay contract that keeps all of this off the
fingerprinted order log.

> **Not the same thing as `Attention/`.** `Delegation.Attention` (see
> [agent-traits-and-attention.md](agent-traits-and-attention.md)) models the *AI agent's*
> cognitive-load budget and graceful overload. `Delegation.Watch` models the *human player's*
> must-see queue and clock auto-pause. They share a word, not a subsystem — `WatchAttentionKind`
> is deliberately documented as "distinct from `Delegation.Attention`".

Related: [c2-projection-layer.md](c2-projection-layer.md) (the read-model layer, incl. the
`AlertSeverity` toast tiering that a `Critical` watch card feeds) ·
[determinism-and-replay.md](determinism-and-replay.md) ·
[detection-pipeline.md](detection-pipeline.md) (source of the `ContactTransition`s watch events
are minted from) · [engagement-pipeline.md](engagement-pipeline.md) /
[catalog-damage-readiness-runtime.md](catalog-damage-readiness-runtime.md) (source of the BDA
own-side losses).

---

## The core rule: watch state never enters the order log

Everything here is **presentation/session state**, not simulation state:

- The queue, the cards, ack/dismiss/restore, and the auto-pause reason live on the
  `SimulationSession` instance — **not** in the append-only `DecisionLog`
  ([`IOrderLog`](../../src/ProjectAegis.Delegation/Decision/IOrderLog.cs), ADR-003).
- Nothing in `Watch/` appends to the order log, mutates the sim, or draws RNG, and none of it owns
  the clock. It only *reads* facts (contact transitions, own-side losses) that the deterministic
  sim slices already produced, and it *asks* the session to pause/resume.
- Therefore watch attention **cannot change the order-log fingerprint** that the replay goldens
  assert. A run with the watch runtime firing produces the identical `ComputeFingerprint()` as one
  without — this is the load-bearing invariant (see
  [determinism-and-replay.md](determinism-and-replay.md)).

The one visible sim effect is the **clock pause flag** (`Sim.Clock.Pause()`), which stops wall-time
advancement in interactive play but is explicitly bypassed by the headless/batch path (below) so CI
and the Baltic replay harness never stall.

---

## Data model

Five small types under [`src/ProjectAegis.Delegation/Watch/`](../../src/ProjectAegis.Delegation/Watch/):

| Type | Kind | Role |
|------|------|------|
| [`WatchAttentionKind`](../../src/ProjectAegis.Delegation/Watch/WatchAttentionKind.cs) | enum | The two pause-class kinds today: `HostileOrUnknownContact` (first detection of a non-own-side track) and `OwnSideLossOrDamage` (own unit loss / battle-damage transition). |
| [`WatchAttentionPriority`](../../src/ProjectAegis.Delegation/Watch/WatchAttentionPriority.cs) | enum | Queue ordering, **lower ordinal = higher priority**: `Critical(0) → High(1) → Normal(2) → Low(3)`. |
| [`WatchAttentionEvent`](../../src/ProjectAegis.Delegation/Watch/WatchAttentionEvent.cs) | `sealed record` | The immutable fact: `(EventId, Kind, Priority, TriggerTick, SubjectId, GroupingKey?, ReasonDetail?)`. `IsPauseClass` is true for both current kinds. `EventId` must be **stable** per `(kind, subject)` so re-emission is idempotent. |
| [`WatchAttentionCard`](../../src/ProjectAegis.Delegation/Watch/WatchAttentionCard.cs) | `sealed record` | The presentation wrapper: the source `Event` plus `IsAcknowledged` / `IsDismissed`. `IsUnresolved = !Acknowledged && !Dismissed && IsPauseClass` — this is what gates resume. |
| [`WatchPauseReason`](../../src/ProjectAegis.Delegation/Watch/WatchPauseReason.cs) | enum | Headless reason code for *why* the clock auto-paused: `None / HostileOrUnknownContact / OwnSideLossOrDamage / ExplicitPlayer`. Sim stores the enum; presentation maps it to a label. |

`GroupingKey` and `ReasonDetail` are data-only carry-through fields (raid/formation grouping is a P1
UI concern; `ReasonDetail` is free text for projections). Neither is hashed.

---

## The queue — `WatchAttentionQueue`

[`WatchAttentionQueue`](../../src/ProjectAegis.Delegation/Watch/WatchAttentionQueue.cs) is a pure,
single-threaded, session-local ordered list of cards. It intentionally mirrors the existing
`PendingApprovalQueue` pattern (no Bridge, no statics, no clock).

- **Total ordering, re-sorted on every insert.** Sort key is
  `Priority (Critical first) → TriggerTick ascending → EventId ordinal`
  (`string.CompareOrdinal`). Never rely on insertion order.
- **`Enqueue` is idempotent on `EventId`.** A second event with an already-present `EventId` is a
  no-op — this is what makes "first detection" and "own-side loss" emit exactly once even if the
  fact is re-observed every tick. Enqueue throws only on a null event or empty `EventId`.
- **Ack / dismiss / restore are presentation-only and restorable.** `TryAcknowledge` and
  `TryDismiss` flip flags via `record with { … }`; `TryRestore` un-dismisses. All return `false`
  only when the `EventId` isn't found (and are no-op-true when already in the target state).
- **`UnresolvedPauseClassCount` / `HasUnresolvedPauseClass`** count cards that are pause-class and
  neither acknowledged nor dismissed — the number the resume gate and the UI badge key on.
- **`SnapshotVisible()`** returns non-dismissed cards in order (acked cards remain visible so the UI
  can style them); **`Clear()`** resets on scenario change / session reset.

---

## The auto-pause gate — `WatchAutoPauseGate`

[`WatchAutoPauseGate`](../../src/ProjectAegis.Delegation/Watch/WatchAutoPauseGate.cs) decides pause
and gates resume. It **does not own the clock** — it returns booleans and the session acts on them.

- **`ShouldAutoPause(evt)`** returns true only for pause-class events, and sets `LastPauseReason`
  from the kind (`HostileOrUnknownContact` / `OwnSideLossOrDamage`). Non-pause-class events return
  false and leave the reason untouched.
- **`CanResume(queue, explicitOverride)`** allows resume when there are **zero unresolved
  pause-class cards**, *or* when `explicitOverride` is true (the player force-resumes past an
  unresolved warning).
- **`ClearReason()`** resets the stored reason after a clean resume.

The gate holds only the last reason as state; the "are we still blocked?" truth lives in the queue.

---

## The emit factories — `WatchAttentionEmitFactory`

[`WatchAttentionEmitFactory`](../../src/ProjectAegis.Delegation/Watch/WatchAttentionEmitFactory.cs)
is a pure static class turning sim facts into `WatchAttentionEvent`s with **stable, prefixed
EventIds** so enqueue is idempotent. No Bridge, no RNG, no clock.

| Factory | Fires when | EventId | Priority |
|---------|-----------|---------|----------|
| `TryFromFirstHostileOrUnknownContact(in ContactTransition, out evt)` | `PreviousState == Unknown` → `Detected`/`Classified`/`Identified`, subject is **not** own-side | `watch:contact:{subject}` | `Critical` if hostile (`HostileContactFilter.IsEngageableHostileTarget`), else `High` (unknown/neutral track) |
| `TryFromOwnSideLoss(unitId, triggerTick, reasonDetail, out evt)` | `unitId` **is** own-side | `watch:loss:{unitId}` | `Critical` |
| `TryFromOwnSideLostTransition(in ContactTransition, out evt)` | `NewState == Lost` (delegates to `TryFromOwnSideLoss` with `reasonDetail = "lifecycle:Lost"`) | `watch:loss:{targetId}` | `Critical` |

Own-side classification is centralized in **`IsOwnSideUnit(unitId)`**: the legacy primary blue id
`"u1"`, or any unit `BalticV3SideRegistry.IsBlueForceUnit` reports as blue. This is what keeps a
*friendly* first-contact from raising a "hostile contact" card and a *hostile* kill from raising an
"own-side loss" card.

Because the EventId is derived only from the subject id, the same target detected on many
consecutive ticks yields **one** card — the "fire-once" edge is the `Unknown → …` state precondition
plus queue idempotency, not a separate latch.

---

## Session wiring — how it runs each tick

[`SimulationSession`](../../src/ProjectAegis.Delegation/Orchestration/SimulationSession.cs) owns one
`WatchQueue` and one `WatchPauseGate` and exposes the whole surface:

```text
Sim facts (already produced deterministically)
  ├─ tick-4 detection slice → IReadOnlyList<ContactTransition>
  │      → SimulationSession.ReportContactTransitions(transitions)
  │            → WatchAttentionEmitFactory.TryFromFirstHostileOrUnknownContact  (Critical/High)
  │            → WatchAttentionEmitFactory.TryFromOwnSideLostTransition         (Critical)
  │
  └─ post-engage BDA own-side loss (RunExecutingTick → MarkLost path)
         → SimulationSession.ReportOwnSideLoss(unitId, simTick, "bda:lost")     (Critical)

  both funnel into:
  ReportWatchAttention(evt)
      → WatchQueue.Enqueue(evt)                 (idempotent, re-sorted)
      → if WatchPauseGate.ShouldAutoPause(evt)  → PauseSim()  (Sim.Clock.Pause)

  resume:
  TryResumeSim(explicitOverride = false)
      → WatchPauseGate.CanResume(WatchQueue, override) ? ResumeSim() + ClearReason() : false
```

Key session members:

- **`WatchQueue`** / **`WatchPauseGate`** — the two live instances.
- **`LastWatchPauseReason`** — proxies `WatchPauseGate.LastPauseReason` for the UI/headless label.
- **`ReportWatchAttention(evt)`** — the single funnel: enqueue, then auto-pause if pause-class.
- **`ReportContactTransitions(list)`** / **`ReportOwnSideLoss(id, tick, reason?)`** — the two
  ingress helpers the sim/harness call *without* touching `DelegationBridge`. Both are safe no-ops
  for the non-matching side (e.g. `ReportOwnSideLoss` on a hostile id does nothing).
- **`PauseSim()` / `ResumeSim()`** — thin clock wrappers. `ResumeSim()` **bypasses the gate** for
  legacy callers; **`TryResumeSim(explicitOverride)`** is the gated path new code should use.
- **`TickHeadless(state)`** — the CI/batch path: advances the engagement pipeline **even while the
  clock is paused**, so an auto-pause never stalls the Baltic replay harness or the QA Gauntlet. The
  pause flag and reason are preserved so interactive resume still works after a batch run. (Mirrors
  `TimeCompressionMode.HeadlessBatch`.) The normal interactive `Tick(state)` respects the pause.

---

## UI contract — `WatchAttentionQueueProjection`

Hosts never re-derive priority or pause class; they bind the projection.
[`WatchAttentionQueueProjection`](../../src/ProjectAegis.Delegation/Projection/WatchAttentionQueueProjection.cs)
(part of the [C2 projection layer](c2-projection-layer.md)) exposes three pure reads:

- **`ProjectVisible(queue)`** → ordered non-dismissed cards (`SnapshotVisible`).
- **`ProjectUnresolvedCount(queue)`** → the badge / auto-pause-gating count.
- **`ProjectPauseReasonLabel(reason)`** → the human label:
  `HostileOrUnknownContact → "Hostile / unknown contact"`,
  `OwnSideLossOrDamage → "Own-side loss / damage"`,
  `ExplicitPlayer → "Player pause"`, `None → ""`.

A `Critical` card is the same tier that the C2 alert model
([`AlertSeverity`](c2-projection-layer.md#c2-rev-2-alert--lifecycle-contracts)) surfaces as a toast
"+ optional auto-pause" — this runtime is the auto-pause half of that contract.

---

## Determinism & fail-safe behavior

- **No fingerprint impact.** Watch state is off the order log; projecting/emitting watch events does
  not change `ComputeFingerprint()`. Assert this when extending (see the emit tests).
- **Stable, total ordering.** Queue sort is `Priority → TriggerTick → EventId (ordinal)` — never
  enumeration order.
- **Idempotent by EventId.** Re-observed facts don't duplicate cards; "first detection" is enforced
  by the `Unknown → …` precondition plus idempotent enqueue.
- **Fail-safe emit.** Factories return `false` (no event) on empty/whitespace ids, wrong side, or a
  non-matching transition — they never throw on ordinary sim data. `Enqueue` throws only on a
  null/empty-id programming error.
- **Headless never stalls.** `TickHeadless` advances despite the pause flag; CI and replay are
  unaffected by auto-pause.

---

## Known gaps / current scope (verified against trunk)

- **Two pause-class kinds only.** `HostileOrUnknownContact` and `OwnSideLossOrDamage`. Other
  "significant events" (weapon-range warnings, comms loss, bingo fuel, mission complete) are not yet
  watch-class here.
- **`GroupingKey` is data-only.** Raid/formation grouping of cards in the UI is a P1 concern; the
  field is carried but not yet used for grouping.
- **Own-side loss source is BDA "Lost".** Emitted from the post-engage `MarkLost` path with
  `reasonDetail = "bda:lost"`; graduated battle-damage (non-loss) transitions are not yet emitted.
- **`ExplicitPlayer` reason** exists in the enum/label but the player-initiated pause path that sets
  it is a UI concern outside `Watch/`.

---

## Tests

Verified against trunk — no new pipeline needed (part of `Delegation.Tests`):

| Fixture | Covers |
|---------|--------|
| [`Watch/WatchAttentionQueueTests`](../../src/ProjectAegis.Delegation.Tests/Watch/WatchAttentionQueueTests.cs) | Priority/tick/EventId ordering, EventId idempotency, ack/dismiss restorability, unresolved-count ignoring non-pause-class + acked cards |
| [`Watch/WatchAttentionEmitFactoryTests`](../../src/ProjectAegis.Delegation.Tests/Watch/WatchAttentionEmitFactoryTests.cs) | First `Unknown→Detected` hostile emit + stable id, re-detect is a no-op, own-side `u1` first-detect does **not** emit hostile, own-side loss + `Lost` transition emit, hostile loss does not emit own-side |
| [`Orchestration/SimulationSessionWatchAttentionTests`](../../src/ProjectAegis.Delegation.Tests/Orchestration/SimulationSessionWatchAttentionTests.cs) | `ReportWatchAttention` auto-pauses + sets reason (both kinds), distinct contacts stay pause-class, `TryResumeSim` blocks while unresolved unless override, resume after acknowledge, `ResumeSim` legacy bypass, `TickHeadless` advances despite auto-pause |
| [`Orchestration/SimulationSessionWatchEmitTests`](../../src/ProjectAegis.Delegation.Tests/Orchestration/SimulationSessionWatchEmitTests.cs) | `ReportContactTransitions` first-hostile enqueue + auto-pause, duplicate-target idempotency, `ReportOwnSideLoss` `u1` auto-pauses, hostile id is a no-op |

Run the slice:

```bash
dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj \
  --filter "FullyQualifiedName~Watch" -v minimal
```

---

## Adding a new watch-class event

1. **Add a fact source, not a store.** Mint the event from an already-deterministic sim fact
   (a `ContactTransition`, a BDA change, …) in a pure factory method — do not add a new event log.
2. **Give it a stable, prefixed `EventId`** (`watch:{class}:{subject}`) so re-observation is a
   no-op. Reuse `IsOwnSideUnit` for side classification.
3. **Pick a `Priority`** (Critical for must-not-miss). If it should auto-pause, make its `Kind`
   `IsPauseClass` and extend `WatchAutoPauseGate.ShouldAutoPause` + `WatchPauseReason`.
4. **Funnel through `SimulationSession.ReportWatchAttention`** from the sim/harness path — never from
   `DelegationBridge` (zero-touch invariant).
5. **Add a label** case to `WatchAttentionQueueProjection.ProjectPauseReasonLabel` if you added a
   reason.
6. **Test** the emit precondition, EventId idempotency, and that the order-log
   `ComputeFingerprint()` is unchanged.

---

## See also

| Topic | Doc |
|-------|-----|
| C2 read-model layer, alert tiering, the queue projection | [c2-projection-layer.md](c2-projection-layer.md) |
| AI agent cognitive-load model (the *other* "attention") | [agent-traits-and-attention.md](agent-traits-and-attention.md) |
| Where `ContactTransition`s come from | [detection-pipeline.md](detection-pipeline.md) |
| Where own-side BDA losses come from | [engagement-pipeline.md](engagement-pipeline.md) · [catalog-damage-readiness-runtime.md](catalog-damage-readiness-runtime.md) |
| Determinism rules, hashing, golden workflow | [determinism-and-replay.md](determinism-and-replay.md) |
| Delegation core & the order log | [`src/ProjectAegis.Delegation/README.md`](../../src/ProjectAegis.Delegation/README.md) |
