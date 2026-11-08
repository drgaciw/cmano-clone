# Watch attention & auto-pause runtime — developer guide

Project Aegis gives the human commander a **watch officer**: a session-local runtime that raises
pause-class attention cards ("hostile/unknown contact just detected", "own-side unit just lost") and,
by the classic wargame rule, **auto-pauses the sim clock** so the player can react before combat runs
away. This is the modern equivalent of Command's "pause on detection" — but expressed as a small,
pure, deterministic seam that lives entirely in the delegation layer and never touches the replay
goldens.

This runtime was built across **S115** (the pause spine — kinds, queue, gate, clock controls,
PRD P0-6·P0-7) and **S116** (the emit call-sites — first-detect contact + own-side loss). This page
documents what the runtime actually **does**, its public seams, and how to extend it without breaking
replay. It is verified against source and pinned by the tests listed at the end.

- **Core:** [`src/ProjectAegis.Delegation/Watch/`](../../src/ProjectAegis.Delegation/Watch/) — the
  event/card model, the ordered queue, the emit factory, and the auto-pause gate.
- **Session wiring:** [`SimulationSession`](../../src/ProjectAegis.Delegation/Orchestration/SimulationSession.cs)
  owns one `WatchQueue` + one `WatchPauseGate`, exposes `ReportContactTransitions` /
  `ReportOwnSideLoss`, and owns the pause/resume clock controls.
- **Clock:** [`SimClock`](../../src/ProjectAegis.Sim/Time/SimClock.cs) — the `IsPaused` flag the gate
  drives (the gate never touches the clock directly). The clock, pause precedence, and time
  compression are documented in [sim-clock-time-compression.md](sim-clock-time-compression.md).
- **Presentation:** [`WatchAttentionQueueProjection`](../../src/ProjectAegis.Delegation/Projection/WatchAttentionQueueProjection.cs)
  — the read-only UI contract over the queue.
- **Related:** a shorter S115/S116 spine index already lives in
  [watch-attention-autopause.md](watch-attention-autopause.md); this page is the runtime deep-dive
  (queue/card model, emit factory, gate, add-a-pause-class runbook). The AI cognitive-load model
  with the similar name is a **different** subsystem — see
  [agent-traits-and-attention.md](agent-traits-and-attention.md) (`Delegation.Attention`). Contact
  transitions are produced by the [detection-pipeline.md](detection-pipeline.md). The C2 read-model
  layer that hosts the watch panel is [c2-projection-layer.md](c2-projection-layer.md). Replay
  determinism rules are in [determinism-and-replay.md](determinism-and-replay.md).

---

## Design invariants — never break these

These are load-bearing and enforced by tests. Preserve them when extending the runtime.

| Invariant | Rule |
|-----------|------|
| **Session-local, not sim state** | The queue, cards, and gate are per-session presentation/UX state. Nothing here enters the fingerprinted `DecisionLog` or any world hash. `ReasonDetail` is explicitly *not hashed*. |
| **Replay-safe** | The watch runtime **never** changes replay outputs. Auto-pause only stops the *interactive* tick advance; the headless/CI path (`TickHeadless` / `HeadlessBatch`) advances regardless of pause, so goldens and the Baltic v2 hash `17144800277401907079` are untouched. |
| **Idempotent enqueue** | Events carry a **stable** `EventId` (per subject/kind). Re-emitting the same fact is a no-op — the queue never double-counts the same contact/loss. |
| **Deterministic ordering** | The queue is always sorted `Priority` (Critical first) → `TriggerTick` ascending → `EventId` ordinal. No wall-clock, no RNG, no `DateTime.UtcNow`. |
| **Gate does not own the clock** | `WatchAutoPauseGate` only *decides* pause/resume; the caller (`SimulationSession`) invokes `PauseSim` / `ResumeSim`. This keeps the clock owner single and testable. |
| **ROE cannot be overridden by ack** | Acknowledge / dismiss are **presentation-only** — they clear a card from the pause gate, they never mutate sim policy, ROE, or orders. |
| **Own-side classification is explicit** | "Own-side" = catalog blue-force (`BalticV3SideRegistry.IsBlueForceUnit`) or the legacy primary blue id `u1`. Loss events fire only for own-side ids; contact events fire only for non-own-side ids. |

---

## Where it lives

| File | Role |
|------|------|
| [`WatchAttentionKind.cs`](../../src/ProjectAegis.Delegation/Watch/WatchAttentionKind.cs) | The two pause-class kinds: `HostileOrUnknownContact(0)`, `OwnSideLossOrDamage(1)`. Distinct from the `Delegation.Attention` AI-load model. |
| [`WatchAttentionPriority.cs`](../../src/ProjectAegis.Delegation/Watch/WatchAttentionPriority.cs) | Queue ordering priority (lower ordinal = higher): `Critical(0)` / `High(1)` / `Normal(2)` / `Low(3)`. |
| [`WatchAttentionEvent.cs`](../../src/ProjectAegis.Delegation/Watch/WatchAttentionEvent.cs) | Immutable event `(EventId, Kind, Priority, TriggerTick, SubjectId, GroupingKey?, ReasonDetail?)`. `IsPauseClass` is true for both current kinds. |
| [`WatchAttentionCard.cs`](../../src/ProjectAegis.Delegation/Watch/WatchAttentionCard.cs) | Presentation wrapper over an event with `IsAcknowledged` / `IsDismissed`. `IsUnresolved = !ack && !dismissed && IsPauseClass` is what the resume gate counts. |
| [`WatchAttentionQueue.cs`](../../src/ProjectAegis.Delegation/Watch/WatchAttentionQueue.cs) | Session-local ordered queue (pattern mirrors `PendingApprovalQueue`): idempotent `Enqueue`, `TryAcknowledge` / `TryDismiss` / `TryRestore`, `SnapshotVisible` (non-dismissed), `UnresolvedPauseClassCount` / `HasUnresolvedPauseClass`, `Clear`. |
| [`WatchAttentionEmitFactory.cs`](../../src/ProjectAegis.Delegation/Watch/WatchAttentionEmitFactory.cs) | Pure fact → event factories (S116): `TryFromFirstHostileOrUnknownContact`, `TryFromOwnSideLoss`, `TryFromOwnSideLostTransition`, plus `IsOwnSideUnit`. Stable `EventId` prefixes `watch:contact:` / `watch:loss:`. No Bridge, no RNG. |
| [`WatchAutoPauseGate.cs`](../../src/ProjectAegis.Delegation/Watch/WatchAutoPauseGate.cs) | `ShouldAutoPause(evt)` (after enqueue) + `CanResume(queue, explicitOverride)` + `LastPauseReason` / `ClearReason`. |
| [`WatchPauseReason.cs`](../../src/ProjectAegis.Delegation/Watch/WatchPauseReason.cs) | Headless reason code: `None(0)` / `HostileOrUnknownContact(1)` / `OwnSideLossOrDamage(2)` / `ExplicitPlayer(3)`. Presentation maps to labels; sim stores only the enum. |
| [`WatchAttentionQueueProjection.cs`](../../src/ProjectAegis.Delegation/Projection/WatchAttentionQueueProjection.cs) | Read-only UI contract: `ProjectVisible`, `ProjectUnresolvedCount`, `ProjectPauseReasonLabel`. Hosts bind labels only — never re-derive priority or pause class. |

---

## Lifecycle: fact → event → queue → auto-pause → resume

```
detection tick 4 / BDA                          player action
   ContactTransition / own-side loss                (UI)
        │                                              │
        ▼   SimulationSession.ReportContactTransitions │
   WatchAttentionEmitFactory.Try*  (pure, stable EventId, own-side filter)
        │  emits WatchAttentionEvent only for the classic pause-class facts
        ▼   SimulationSession.ReportWatchAttention(evt)
   WatchAttentionQueue.Enqueue(evt)        ← idempotent on EventId, re-sorts
        │
        ▼   WatchAutoPauseGate.ShouldAutoPause(evt)
   pause-class? → set LastPauseReason → SimulationSession.PauseSim()  (SimClock.IsPaused = true)
        │
        │   interactive Tick() early-returns while paused (headless TickHeadless does not)
        ▼
   player reviews cards → TryAcknowledge / TryDismiss ────────────────┐
        │                                                             │
        ▼   SimulationSession.TryResumeSim(explicitOverride?)         │
   WatchAutoPauseGate.CanResume(queue, override)                      │
        │  allowed when UnresolvedPauseClassCount == 0  OR  override  │
        ▼                                                             │
   ResumeSim() + ClearReason()  ←───────────────────────────────────┘
```

Both emit call-sites are pure and idempotent, so the harness/sensor path can call them every tick
without a Bridge edit and without double-raising.

---

## Emit rules (what actually raises a card)

`WatchAttentionEmitFactory` is deliberately narrow — it raises the two *classic* pause facts and
nothing else:

- **First hostile/unknown contact** (`TryFromFirstHostileOrUnknownContact`): fires **only** on the
  `Unknown → {Detected | Classified | Identified}` edge (first acquisition), **only** for a
  non-own-side `TargetId`. Hostile targets (`HostileContactFilter.IsEngageableHostileTarget`) →
  `Critical`; other non-own tracks → `High` (treated as unknown/neutral). `EventId = watch:contact:<targetId>`,
  so the same contact re-transitioning never re-raises. `GroupingKey` carries the `ContactId` for
  future raid grouping (data only; UI grouping is a P1 concern).
- **Own-side loss / damage** (`TryFromOwnSideLoss` / `TryFromOwnSideLostTransition`): fires for an
  own-side unit id — either a direct BDA/battle-damage report, or a contact lifecycle transition to
  `Lost`. Always `Critical`. `EventId = watch:loss:<unitId>`.

Anything that is not one of these (own-side new contacts, hostile losses, non-terminal transitions)
is a **no-op** — the factory returns `false` and no event is created.

---

## Auto-pause & resume gating

- `ShouldAutoPause(evt)` runs **after** a successful enqueue and returns true for any pause-class
  event, recording `LastPauseReason`. `SimulationSession.ReportWatchAttention` then calls `PauseSim()`,
  which sets `SimClock.IsPaused`.
- Resume is **gated**: `TryResumeSim(explicitOverride)` calls `CanResume(queue, override)`, which
  allows resume only when `UnresolvedPauseClassCount == 0` **or** the player force-resumes
  (`explicitOverride = true`). On success it resumes the clock and clears the reason. Acknowledging or
  dismissing the outstanding cards is what drains `UnresolvedPauseClassCount` to zero.
- The bare `ResumeSim()` bypasses the gate and exists for legacy callers; interactive UX should prefer
  `TryResumeSim`.
- **Headless bypass:** `TickHeadless` (and `TimeCompressionMode.HeadlessBatch` in `SimTickPipeline`)
  advance the engagement pipeline even while `IsPaused` is set — the pause flag and reason are
  preserved so an interactive resume still works after a batch. This is why auto-pause never perturbs
  CI replay or the QA Gauntlet.

---

## Presentation

All watch surfaces are read-only projections over the session queue. `WatchAttentionQueueProjection`
exposes the ordered non-dismissed cards, the unresolved badge count, and a human label for the pause
reason. Unity hosts bind these directly and never re-derive priority or pause class — matching the
"presentation is a client" boundary used across the [c2-projection-layer.md](c2-projection-layer.md).

---

## Determinism & replay

- The queue sorts on `(Priority, TriggerTick, EventId)` only — all deterministic inputs — and enqueue
  is idempotent, so the visible card set is a pure function of the facts observed.
- No watch state is folded into the `DecisionLog`, world hash, or any golden. `ReasonDetail` is free
  text for projections and is not hashed.
- Auto-pause changes only *interactive* clock advance; headless/batch ticks ignore it. Consequently
  the watch runtime is **replay-neutral** — the Baltic v2 hash `17144800277401907079` and the
  ReplayGolden 6/6 are unaffected by anything in `Watch/`.

---

## How to extend without breaking replay

1. **Add a kind, not sim state.** New pause facts go in `WatchAttentionKind` + a
   `WatchAttentionEmitFactory.TryFrom*` factory returning a stable `EventId`. Keep the factory pure —
   no Bridge, no RNG, no clock.
2. **Keep EventIds stable & unique per subject.** Idempotent enqueue depends on it; a non-stable id
   re-raises every tick.
3. **Wire emit through the session, not the Bridge.** Call the new factory from
   `SimulationSession.ReportContactTransitions` / a sibling report method. `DelegationBridge.cs` stays
   zero-touch.
4. **Route pause/resume through the gate.** Extend `WatchPauseReason` if needed and map it in
   `ShouldAutoPause`; never toggle `SimClock` directly from the gate.
5. **Never let ack/dismiss mutate policy.** They are presentation-only; ROE/autonomy remain
   authoritative (see [autonomy-roe-gating.md](autonomy-roe-gating.md)).
6. **Confirm replay neutrality.** New watch behaviour must not enter any hash and must be bypassed by
   the headless path. Re-run the watch tests plus the replay goldens; the Baltic v2 hash must stay
   `17144800277401907079`.

---

## Tests (behaviour pins)

| Area | Test file |
|------|-----------|
| Queue ordering / idempotent enqueue / ack·dismiss·restore | [`Watch/WatchAttentionQueueTests.cs`](../../src/ProjectAegis.Delegation.Tests/Watch/WatchAttentionQueueTests.cs) |
| Emit factory (first-contact edge, own-side filter, priority) | [`Watch/WatchAttentionEmitFactoryTests.cs`](../../src/ProjectAegis.Delegation.Tests/Watch/WatchAttentionEmitFactoryTests.cs) |
| Session auto-pause + gated resume + headless bypass | [`Orchestration/SimulationSessionWatchAttentionTests.cs`](../../src/ProjectAegis.Delegation.Tests/Orchestration/SimulationSessionWatchAttentionTests.cs) |
| Session emit call-sites (contact transitions, own-side loss) | [`Orchestration/SimulationSessionWatchEmitTests.cs`](../../src/ProjectAegis.Delegation.Tests/Orchestration/SimulationSessionWatchEmitTests.cs) |
