# Watch attention & auto-pause spine

The **watch attention** subsystem (S115 / S116, PRD P0-6·P0-7) is the "the watch officer
must not miss this" spine: when a pause-class fact is first observed — a **hostile or
unknown contact**, or an **own-side loss / battle-damage** — the interactive sim
auto-pauses and surfaces an ordered card so the player can react before time resumes.

It lives entirely in the engine-agnostic core under
[`src/ProjectAegis.Delegation/Watch/`](../../src/ProjectAegis.Delegation/Watch/) (plus one
projection) and is wired into the session by
[`SimulationSession`](../../src/ProjectAegis.Delegation/Orchestration/SimulationSession.cs).
Every piece is pure, single-threaded, RNG-free, and **does not touch `DelegationBridge` or
the fingerprinted `DecisionLog`** — so it never moves a replay golden hash.

> **Distinct from `Delegation.Attention`.** The `Attention/` model is the *AI cognitive-load*
> budget that degrades an agent's own decisions ([agent-traits-and-attention.md](agent-traits-and-attention.md)).
> `Watch/` is the *human* watch-officer alert queue. They share a word, nothing else.

| Concern | Type |
|---------|------|
| Event value | [`WatchAttentionEvent`](../../src/ProjectAegis.Delegation/Watch/WatchAttentionEvent.cs) (`sealed record`) |
| Kinds | [`WatchAttentionKind`](../../src/ProjectAegis.Delegation/Watch/WatchAttentionKind.cs) · [`WatchAttentionPriority`](../../src/ProjectAegis.Delegation/Watch/WatchAttentionPriority.cs) · [`WatchPauseReason`](../../src/ProjectAegis.Delegation/Watch/WatchPauseReason.cs) |
| UI card | [`WatchAttentionCard`](../../src/ProjectAegis.Delegation/Watch/WatchAttentionCard.cs) |
| Ordered queue | [`WatchAttentionQueue`](../../src/ProjectAegis.Delegation/Watch/WatchAttentionQueue.cs) |
| Pause decision | [`WatchAutoPauseGate`](../../src/ProjectAegis.Delegation/Watch/WatchAutoPauseGate.cs) |
| Fact → event factory | [`WatchAttentionEmitFactory`](../../src/ProjectAegis.Delegation/Watch/WatchAttentionEmitFactory.cs) |
| UI read-model | [`WatchAttentionQueueProjection`](../../src/ProjectAegis.Delegation/Projection/WatchAttentionQueueProjection.cs) |
| Session wiring | `SimulationSession` (`ReportWatchAttention` / `ReportContactTransitions` / `ReportOwnSideLoss` / `TryResumeSim`) |

Pinned by 21 tests: `WatchAttentionQueueTests` (4), `WatchAttentionEmitFactoryTests` (6),
`SimulationSessionWatchAttentionTests` (7), `SimulationSessionWatchEmitTests` (4).

---

## Mental model

```
 sim fact (contact transition / own-side loss)
        │
        ▼
WatchAttentionEmitFactory.Try…            # pure fact → WatchAttentionEvent? (stable EventId)
        │  (event or nothing)
        ▼
SimulationSession.ReportWatchAttention(evt)
        ├── WatchQueue.Enqueue(evt)             # idempotent on EventId; re-sorts
        └── WatchAutoPauseGate.ShouldAutoPause(evt)
                 │  true (pause-class)
                 ▼
            SimulationSession.PauseSim()  → Sim.Clock.Pause()
                                                     │
player acknowledges / dismisses card ────────────────┘
        │
        ▼
SimulationSession.TryResumeSim(explicitOverride?)
        └── WatchAutoPauseGate.CanResume(queue, override)   # blocked while unresolved
                 │  allowed
                 ▼
            ResumeSim() + gate.ClearReason()

UI binds:  WatchAttentionQueueProjection.ProjectVisible / ProjectUnresolvedCount / ProjectPauseReasonLabel
```

The four moving parts are deliberately split: a **factory** (fact → event), a **queue**
(ordering + ack/dismiss state), a **gate** (pause/resume policy), and a **projection**
(read-only UI contract). The session owns the clock; nothing under `Watch/` calls
`Pause`/`Resume` itself.

---

## Pause classes

Two kinds are defined today, and **both are pause-class**
([`WatchAttentionEvent.IsPauseClass`](../../src/ProjectAegis.Delegation/Watch/WatchAttentionEvent.cs)):

| `WatchAttentionKind` | Fires on | `WatchPauseReason` |
|----------------------|----------|--------------------|
| `HostileOrUnknownContact` | first detection of a non-own-side contact | `HostileOrUnknownContact` |
| `OwnSideLossOrDamage` | own-side unit loss / battle-damage transition | `OwnSideLossOrDamage` |

`WatchPauseReason` also carries `ExplicitPlayer` (manual pause, label-only) and `None`
(cleared). Priority is a separate axis (`Critical < High < Normal < Low`, lower ordinal =
higher priority) used purely for queue ordering — it does **not** decide pause.

---

## The event and the card

`WatchAttentionEvent` is immutable and identified by a caller-supplied **stable `EventId`**.
Stability is the whole idempotency contract: re-emitting the same `(kind, subject, trigger)`
must produce the same id so the queue drops the duplicate.

```csharp
public sealed record WatchAttentionEvent(
    string EventId,          // stable & deterministic (e.g. "watch:contact:hostile-1")
    WatchAttentionKind Kind,
    WatchAttentionPriority Priority,
    ulong TriggerTick,       // sim tick the fact was first observed
    string SubjectId,        // contact/unit id
    string? GroupingKey = null,   // optional raid/formation grouping (data only; UI grouping is P1)
    string? ReasonDetail = null); // free-text for projections; NOT part of identity/ordering
```

`WatchAttentionCard` wraps an event with two **presentation-only** flags — `IsAcknowledged`
and `IsDismissed` — and exposes the derived `IsUnresolved = !acked && !dismissed &&
IsPauseClass`, which is exactly what the resume gate counts. Ack/dismiss never mutate sim
state or policy.

---

## The queue

[`WatchAttentionQueue`](../../src/ProjectAegis.Delegation/Watch/WatchAttentionQueue.cs) is a
session-local ordered list (the pattern mirrors `PendingApprovalQueue`):

- **Ordering** (stable, re-applied on every insert): `Priority` (Critical first) →
  `TriggerTick` ascending → `EventId` ordinal.
- **`Enqueue`** is idempotent on `EventId` — a second event with the same id is dropped, and
  the **first** event's fields win (the later `TriggerTick` is *not* applied). Throws on a
  null event or empty `EventId`. This prevents a duplicate **card**, not a duplicate pause:
  `ReportWatchAttention` does not gate `ShouldAutoPause` on a successful insert.
- **`TryAcknowledge` / `TryDismiss` / `TryRestore`** return `false` only when the id is
  unknown; they are no-op-safe if the card is already in the target state. Dismiss is a soft
  remove (`SnapshotVisible()` hides it) and is reversible via `TryRestore`.
- **`UnresolvedPauseClassCount` / `HasUnresolvedPauseClass`** drive both the resume gate and
  the UI badge.
- **`SnapshotVisible()`** returns non-dismissed cards in order (acknowledged cards stay
  visible so the UI can style them); **`Clear()`** resets on scenario change.

---

## The auto-pause gate

[`WatchAutoPauseGate`](../../src/ProjectAegis.Delegation/Watch/WatchAutoPauseGate.cs) holds
the pause *policy* and the last reason; it never owns the clock.

- **`ShouldAutoPause(evt)`** → `true` for pause-class events, and records
  `LastPauseReason`. Non-pause-class events return `false` and do not change the reason.
- **`CanResume(queue, explicitOverride)`** → `true` when there are **zero unresolved
  pause-class cards**, *or* `explicitOverride` is set (player force-resume). This is the
  load-bearing rule: you cannot silently resume past an unacknowledged threat.
- **`ClearReason()`** resets to `None` (called by the gated resume path after a clean
  resume).

---

## Session wiring

`SimulationSession` exposes the whole surface and is the only component that touches the
clock:

| Member | Behavior |
|--------|----------|
| `WatchQueue`, `WatchPauseGate` | the live queue + gate for this session |
| `LastWatchPauseReason` | passthrough to `WatchPauseGate.LastPauseReason` |
| `IsSimPaused` | `Sim.Clock.IsPaused` |
| `ReportWatchAttention(evt)` | void `Enqueue` then `ShouldAutoPause` — **EventId dedupes cards, not re-pause**. After ack/dismiss + `TryResumeSim`, reporting the same id pauses the clock again even though no card was inserted |
| `ReportContactTransitions(transitions)` | S116 seam: map each transition via the factory, report any events |
| `ReportOwnSideLoss(unitId, tick, detail?)` | report an own-side loss (no-op for non-own-side ids) |
| `TryResumeSim(explicitOverride = false)` | gate-checked resume; on success calls `ResumeSim()` + `ClearReason()`, returns `false` if blocked |
| `ResumeSim()` | **ungated** clock resume for legacy callers — does *not* clear the reason |

**`ResumeSim()` vs `TryResumeSim()`:** the raw `ResumeSim()` bypasses the gate (kept for
legacy callers) and intentionally leaves `LastWatchPauseReason` set; interactive UI should
always call `TryResumeSim` so the unresolved-threat gate and reason-clear both apply.

### Emit call-sites today

The pure seams exist and are unit-pinned; production wiring is deliberately minimal
(S115/S116 = "minimal spine"):

- **Own-side BDA loss is auto-wired.** `ApplyBdaContactLifecycleHotTick` calls
  `ReportOwnSideLoss(targetId, simTick, "bda:lost")` when a blue unit's damage lifecycle
  reaches Lost. Hostile losses stay silent by design.
- **Contact-transition emit is a ready seam.** `ReportContactTransitions` is meant to be
  called from the harness/sensor path *without* Bridge edits; it is fully tested but not yet
  auto-driven by a production detection loop.

---

## Headless / CI: `TickHeadless` override (not the Baltic harness)

Auto-pause is an **interactive** affordance. `SimulationSession` exposes a separate batch
entry point that must keep advancing even while paused:

- **`Tick(state)`** (interactive) short-circuits the engagement phase when
  `Sim.Clock.IsPaused` (still surfaces ROE-denied engagements, then returns).
- **`TickHeadless(state)`** passes `headlessOverride: true`, so `RunExecutingTick` advances
  the engagement pipeline regardless of the pause flag — mirroring
  `TimeCompressionMode.HeadlessBatch`. The **pause flag and watch reason are preserved**, so
  an interactive resume still works after a batch.

This override applies **only to direct `TickHeadless` callers**. The Baltic replay / QA
harness does **not** use it today: `BalticReplayHarness` calls `bridge.Tick`, and
`DelegationBridge.Tick` routes an attached session through interactive `Session.Tick`. After
an own-side BDA loss auto-pauses the session, subsequent harness ticks therefore take the
paused short-circuit rather than the documented override. `TickHeadless` currently has no
non-test production caller; do not claim that replay/QA runs cannot freeze until the batch
host is wired to `TickHeadless` (or an equivalent `headlessOverride` path).

The clock ([`SimClock`](../../src/ProjectAegis.Sim/Time/SimClock.cs)) also carries an
`AccelerationFactor` (clamped `1..256`); `RunExecutingTick` runs `factor − 1` extra
`Sim.TickOnce` steps per tick. Pause and acceleration are independent knobs on the same
clock.

---

## Presentation contract

Unity / UI hosts bind labels only and **never re-derive** priority, pause class, or the
pause reason — they read
[`WatchAttentionQueueProjection`](../../src/ProjectAegis.Delegation/Projection/WatchAttentionQueueProjection.cs):

| Projection | Returns |
|------------|---------|
| `ProjectVisible(queue)` | ordered non-dismissed cards (`queue.SnapshotVisible()`) |
| `ProjectUnresolvedCount(queue)` | unresolved pause-class count for the badge / gating UI |
| `ProjectPauseReasonLabel(reason)` | headless label string (`"Hostile / unknown contact"`, `"Own-side loss / damage"`, `"Player pause"`, or empty for `None`) |

This keeps the read-model boundary identical to the rest of the C2 layer — see
[c2-projection-layer.md](c2-projection-layer.md).

---

## Determinism & invariants

- **No RNG, no clock ownership, no wall-clock** anywhere under `Watch/`. The factory is a
  pure function of the fact.
- **Stable `EventId`s** make **card enqueue** idempotent: re-observing a fact does not spawn
  a second card. That is **not** a no-double-pause guarantee. `ReportWatchAttention` always
  `Enqueue`s then unconditionally evaluates `ShouldAutoPause(evt)`. If a card is acknowledged
  and the sim resumes, re-observing the same `EventId` pauses the clock again even though the
  queue drops the duplicate and has no unresolved card. Narrow this to card de-duplication
  until the session gates auto-pause on a *newly inserted* event.
- **Ack / dismiss are presentation-only** — they never mutate sim policy, the order log, or
  any hashed state.
- **Nothing is appended to the fingerprinted `DecisionLog`**, and `DelegationBridge` is
  untouched → the Baltic v2 replay hash `17144800277401907079` is unaffected.
- **`TickHeadless` preserves pause state** so a *direct* batch caller never freezes and
  interactive resume still works afterward. The Baltic harness is not that caller today.

---

## `EventId` conventions

The factory uses these stable prefixes ([`WatchAttentionEmitFactory`](../../src/ProjectAegis.Delegation/Watch/WatchAttentionEmitFactory.cs)):

| Fact | `EventId` | Priority |
|------|-----------|----------|
| first hostile contact (`HostileContactFilter.IsEngageableHostileTarget`) | `watch:contact:<targetId>` | `Critical` |
| first unknown/neutral non-own contact | `watch:contact:<targetId>` | `High` |
| own-side loss / damage | `watch:loss:<unitId>` | `Critical` |

Contact events fire **only** on `Unknown → Detected|Classified|Identified` (the first-detect
edge); own-side ids never produce a hostile-contact card. Own-side membership is
`unitId == "u1"` (legacy primary blue) **or** `BalticV3SideRegistry.IsBlueForceUnit(unitId)`.
`GroupingKey` is populated from the transition's `ContactId` (data only; UI grouping is P1).

---

## Adding a new pause class

1. Add the kind to `WatchAttentionKind` and extend `WatchAttentionEvent.IsPauseClass` if it
   should auto-pause (or leave it non-pause-class for a badge-only alert).
2. Map it in `WatchAutoPauseGate.ShouldAutoPause` and add a matching `WatchPauseReason` +
   `WatchAttentionQueueProjection.ProjectPauseReasonLabel` case.
3. Add a `WatchAttentionEmitFactory.TryFrom…` factory that produces a **stable `EventId`**
   from the sim fact, then a `SimulationSession.Report…` seam that calls
   `ReportWatchAttention`.
4. Pin it with queue-ordering + idempotency tests and a session auto-pause/resume test
   (mirror `SimulationSessionWatchAttentionTests`). Keep it RNG-free and off the Bridge.

---

## See also

| Topic | Where |
|-------|-------|
| C2 read-model layer, alert/lifecycle contracts, `AlertSeverity` | [c2-projection-layer.md](c2-projection-layer.md) |
| AI cognitive-load attention (the *other* "attention") | [agent-traits-and-attention.md](agent-traits-and-attention.md) |
| Contact lifecycle FSM that feeds `ContactTransition` | [detection-pipeline.md](detection-pipeline.md) |
| Catalog damage runtime behind the own-side BDA-loss emit | [catalog-damage-readiness-runtime.md](catalog-damage-readiness-runtime.md) |
| Session engage tick that hosts the emit call-sites | [engagement-pipeline.md](engagement-pipeline.md) |
