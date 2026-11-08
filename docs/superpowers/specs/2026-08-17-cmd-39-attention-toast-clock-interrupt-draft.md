# CMD-39 — Attention Toast + Clock Interrupt (DRAFT)

**Status:** Implemented partial (Track A UI host, 2026-08-17) — **pending owner approval to land in REQ-20**  
**Do not append** to `Game-Requirements/requirements/20-Command-And-Control-UI.md` until explicitly approved.  
**Date:** 2026-08-17  
**Parent audit:** `docs/superpowers/reviews/2026-08-17-playmode-visual-audit.md`  
**Review mode:** lean (director gates skipped)

---

## Elevator intent

Bring **CMANO-style attention** into Unity Play Mode: when something important happens (hostile/unknown contact, own-side loss/damage, attention-tier crossings), the player gets a **toast / pop-up card** and, for pause-class events, an optional **clock interrupt** (auto-pause or drop time compression) — without arcade flash spam.

Headless already has `AttentionTierAlertProjection` (tier-change alerts) and `WatchAutoPauseGate` (pause-class enqueue → ShouldAutoPause). Play Mode has **no toast host** and does not bind those gates to the top-bar clock. CMD-39 is the product AC that wires presentation to those seams.

---

## Relationship to existing requirements

| ID | Relationship |
|----|----------------|
| **CMD-04** | Time / compression / pause chrome — CMD-39 **drives** pause or compression drop via existing top-bar state; does not replace CMD-04’s compression model question (multiplier vs time-per-step). |
| **CMD-05** | Message log remains the durable feed; toasts are **ephemeral** and should deep-link or mirror a log line / `sequenceId` when one exists. |
| **ADR-010** | Presentation client only: toast queue and pause UX are presentation/session chrome; sim pause is invoked through existing session/clock APIs (same pattern as `WatchAutoPauseGate` callers), not by inventing a second authority. |
| **AGD / S109 / S115** | Headless alert + watch-pause work already shipped; CMD-39 is the **Unity product surface**. |

---

## Acceptance criteria (draft)

1. **Toast host.** Unity C2 chrome includes an attention toast / card host (UI Toolkit) that can show at least one active card with title, short body, severity, and dismiss / acknowledge.
2. **Attention-tier binding.** When `AttentionTierAlertProjection.Diff` emits alerts (non-Nominal crossings per existing rules), Play Mode shows a toast (or queues behind an active pause-class card) with accessible text from the projection.
3. **Pause-class interrupt.** When `WatchAutoPauseGate.ShouldAutoPause` is true for a newly enqueued pause-class event (hostile/unknown contact, own-side loss/damage), the session **pauses** (or drops compression to 1x then pauses — product pick in MVP notes) and a toast remains until acknowledged.
4. **Resume gating.** Resume follows `WatchAutoPauseGate.CanResume` (unresolved pause-class cards block resume unless explicit override).
5. **No spam.** Unchanged attention tiers produce no toast (projection already suppresses). Multiple events coalesce or queue with a visible count — not full-screen strobe.
6. **Message log coherence.** Pause-class and tier alerts that have order-log counterparts still appear (or are projectable) in the message log; toast is not a second truth source.
7. **Headless unchanged.** Gate and projection behavior remain covered by existing unit tests; Unity host is an additional consumer.

---

## Non-goals

- **Arcade flash spam** — no screen-shake, particle bursts, random flicker, or non-deterministic decorative pulses (art-bible §7 / evidence-grade clarity).
- **Replacing the message log** with toasts as the only telemetry.
- **Inventing new sim engagement rules** — alerts consume existing watch/attention projections.
- **DelegationBridge hotpath edits.**
- **Interactive time-compression redesign** beyond what CMD-04 already requires (treat interactive compression as CMD-04 residual; CMD-39 only interrupts the clock).

---

## Dependencies

| Dependency | Notes |
|------------|--------|
| `AttentionTierAlertProjection` | Already emits attributable alerts + `ToMessageLogLine` |
| `WatchAutoPauseGate` + watch attention queue | Already decides auto-pause / resume |
| Session PauseSim / ResumeSim (or Play Mode equivalent) | Callers must exist in Unity host stack |
| Top bar pause / compression labels (`C2TopBarPanelHost`) | Reflect interrupt state |
| Optional: richer Play Mode snapshot / log | So pause-class events actually fire in Editor (Track A synergy) |

---

## MVP vs later

| Slice | Scope |
|-------|--------|
| **MVP** | Toast host for pause-class + attention-tier Critical/Notable; auto-pause on pause-class; dismiss/ack; resume gate; bind to seeded/demo events in Play Mode or recorded log replay |
| **Later** | Player-configurable “pop-up on contact / loss / weapon release”; unread highlight in message log; multi-monitor toast bookmark; compression drop-without-full-pause modes |

### MVP product pick (to resolve at REQ land)

- **Interrupt mode:** (A) hard pause only, or (B) drop to 1x then pause if was compressed. Recommend **(A)** for parity with CMO “break” feel and simpler ADR-010 session semantics.

---

## Implementation tracks (informational)

Often paired with **Track A** (make telemetry visible) so toasts have real events in Play Mode. Full kinematic picture (**Track B / CMD-38**) is independent. Track C VFX remains rejected.

---

## Approval gate

- [ ] Owner approves this draft intent and ACs  
- [ ] Owner authorizes append of a numbered **CMD-39** block to REQ-20  
- [ ] Implementation plan only after REQ-20 land (or explicit “implement from draft” exception)

*Pending owner approval to land in REQ-20.*
