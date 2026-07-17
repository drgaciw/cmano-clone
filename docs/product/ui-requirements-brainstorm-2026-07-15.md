# UI Requirements Brainstorm — Tactical Play UI & Scenario Editor

**Date:** 2026-07-15
**Method:** Structured product brainstorm (frame → diverge → provoke → converge → capture)
**Participants:** DRGAMTD (product owner), Claude (thinking partner)
**Status:** Captured — input to a forthcoming PRD (`/product-management:write-spec`)

---

## 1. Product identity

**"Command like a commander."** The human is architecturally just another agent: per ADR-010, the UI is a command-emitting client over the same validated command API that AI agents use. The UI's distinctive job is to make that human/AI symmetry legible and safe. This — not CMANO feature parity — is the differentiator.

The core primitive is **fluid control authority**: any unit, mission, side, or authoring task can be held by the human or an AI agent, and swapped live.

## 2. Player and posture

- **Player:** Human commander with AI subordinates (AI agents are the crews/COs executing within doctrine).
- **Default posture:** Direct control — classic CMANO-grade micromanagement is the home position.
- **Delegation:** The player may hand any unit — or the whole blue side — to an AI operator and take it back at any time.
- **Opposition:** AI-run red force with a tunable expertise setting (scenario-level, adjustable pre-run).

## 3. Decided requirements

### 3.1 UI spine: glass cockpit

CMANO-faithful fixed-panel layout, always open:

| Panel | Content (existing projection basis) |
|---|---|
| Units | Blue-force roster with ownership badges (`OobTreeEntry`) |
| Contacts | Hostile/unknown picture (`ContactPictureProjection`, `SensorC2Projection`) |
| Map | Tactical map, ADR-007 Phase A placeholder (`MapPictureProjection`) |
| Weapons | Engagement/magazine state (`ScenarioEngageDefaults`, `MagazineChanges`) |
| Missions | Mission board (`MissionListProjection`) |
| Log | Full message log (`MessageLogProjection`) |
| **Attention** | Exception events: pause events, weapons-release requests, AI guidance requests |

The **attention panel** is the addition the delegation model requires: auto-pause events and AI requests must have a permanent, first-class home distinct from the firehose log.

### 3.2 Authority model

- Every unit/mission/side has a visible owner: **Human** or **AI (profile)**. Ownership is rendered on map symbology and in the units roster.
- **Handoff semantics: freeze & hold.** Taking the stick stops the AI instantly; the unit continues its last orders until changed. On handback, the AI re-plans from current state. Predictable and explainable.
- Side-level toggle flips the entire blue force to AI-operator mode (and back).

### 3.3 Time model

Real-time with compression, plus auto-pause on:

- **New hostile/unknown contact** (first detection)
- **Own-side losses / battle damage**

**Queue-only (no pause):** weapons-release requests and AI guidance requests land in the attention panel.

### 3.4 Order line and comms log (in MVP)

A typed order line that emits the same canonical commands agents emit, beside direct manipulation. Every order from any author — human or AI — appears in one **attributed comms log**. This is a thin shell over the existing command API and the visible proof of human/AI symmetry.

### 3.5 Audit / explainability

Any AI order in the comms log or attention panel links one click into its `DecisionLog` intent trace — "why did the AI do that" is a first-class affordance, not a debug tool.

### 3.6 Scenario editor: review-first

Load → validate (ADR-008 engine) → map preview → tweak → approve. Supports both workflows:

- **Human authors, AI assists** (primary): hand-edited scenarios validated and previewed.
- **AI authors, human reviews** (on demand): agent-generated scenarios (qa-gauntlet pipeline) enter the same review surface.

Full direct-manipulation authoring is out of MVP scope.

### 3.7 Interaction and failure semantics

- An authority change, order submission, validation action, or approval must show a durable result: **accepted**, **rejected**, or **pending**. A rejection includes the validation or policy reason and leaves the prior authoritative state unchanged.
- The comms log records accepted commands only. Each record identifies author, timestamp/tick, target, command summary, and its outcome; a rejected human command is shown locally with its reason but must not be represented as an executed order.
- Typed and direct-manipulation order entry use the same command grammar, validation path, and result presentation. A command submitted through either path must produce the same authoritative effect.
- The UI must preserve the player’s selection and draft order text after a recoverable rejection so the command can be corrected without reconstructing intent.

### 3.8 Attention model

Attention is an actionable queue, not a second log. Each card has a stable event identifier, priority, subject, trigger tick, current time-compression state, requested player action, and a one-click link to the relevant unit/contact/mission and decision trace where available.

- Cards progress through **new**, **acknowledged**, and **resolved** states. Acknowledging a card changes only presentation state; resolving it must be tied to an accepted authoritative command or an observed terminal condition.
- Sorting is deterministic: priority first, then trigger tick, then stable event identifier. Equal-priority events therefore do not visibly shuffle between projection refreshes.
- Auto-pause events visibly state why time paused and provide an explicit resume/compression control. Queue-only events must never change time compression without the player choosing a response, except for the weapons-release behavior once open question 1 is decided.
- Filters and dismissal controls never discard the underlying comms or decision record. The player can restore filtered/resolved cards during the session.

### 3.9 Authority handoff safeguards

- A handoff card identifies the target scope, current owner, destination owner/profile, effective tick, and whether it changes a unit, mission, or the blue-side default.
- Handoff is atomic at its declared scope: the UI either reports the accepted ownership change or retains the previous owner and explains the rejection. It must not display a mixed owner state while a request is pending.
- Taking control never cancels the last accepted orders, active mission assignment, ROE, or doctrine. It only stops further AI planning for that scope; subsequent human commands are independently validated.
- Handing control to AI does not replay stale decisions. The AI receives the current snapshot and emits new attributed orders only after the handoff command is accepted.

### 3.10 Usability and accessibility baseline

- All core cockpit actions (selecting a unit/contact, inspecting attention, issuing/cancelling a draft order, changing ownership, and changing time compression) are keyboard reachable with a visible focus indicator.
- Ownership, priority, and validation severity use a text/icon label in addition to color. No required state is communicated by color alone.
- Panels expose accessible names and concise state summaries for their current selection, queue count, and validation result. Map symbols retain an equivalent roster/detail route for non-map interaction.
- Fixed cockpit density must remain legible at the supported minimum desktop resolution selected by the PRD; the PRD must name that resolution and any scaling behavior before UI implementation begins.

## 4. Explicitly set aside (revisit post-MVP)

- **Watch-officer spine** (map + exception queue as the spine, density on demand) — the runner-up; the attention panel preserves its core idea inside the cockpit.
- **Dual-seat** (separate commander/operator layouts) and **the conn** (authority panel drives map fidelity) — differentiated but higher scope/UX risk.
- **Full authoring editor** with map direct manipulation.
- **Cesium globe / APP-6 symbology** — already phased as ADR-007 Phases B/C.

## 5. Open questions

1. **Weapons-release pause semantics.** Current decision: no auto-pause. Risk: at high compression the AI holds fire and the launch window closes before approval. Proposed mitigation (not yet decided): weapons-release requests drop compression to 1x, AI holds fire, attention card shows a DLZ-expiry countdown.
2. **New-contact pause spam.** Dense scenarios will pause constantly. Needs grouping/dedupe rules (e.g., pause once per raid, not per bogey) and possibly per-scenario thresholds.
3. **MVP unit-detail depth.** Which of the 75+ projections earn space in the unit drill-down for the first playable milestone.
4. **Authority scope precedence.** A unit can participate in a mission while the side has an AI operator. The PRD must define whether the most-specific assignment wins, how the UI explains inherited ownership, and whether a side-level handoff overwrites or preserves explicit unit/mission assignments.
5. **Editor approval authority.** The PRD must name who may approve a validation-clean scenario, whether approval is a reversible command, and what provenance (human or agent author plus source revision) is retained.

## 6. Architectural constraints (inherited, non-negotiable)

- UI is never authoritative; all mutations via validated commands (ADR-010).
- Projections are read-only; presentation state lives in controllers (`C2PresentationController` pattern).
- UI must never affect sim determinism; golden-hash replay contracts hold (ADR-004).
- UI Toolkit, ADR-007 Phase A map for MVP.

## 7. MVP acceptance criteria

The forthcoming PRD must preserve these as testable requirements:

| ID | Requirement | Evidence |
|---|---|---|
| UI-01 | Every cockpit panel named in §3.1 is continuously reachable without replacing the fixed-panel layout. | UI interaction test or scripted walkthrough. |
| UI-02 | A selected unit, mission, and blue-side default each show an explicit effective owner and AI profile when AI-owned. | Projection/controller test plus UI binding test. |
| UI-03 | A human-to-AI or AI-to-human handoff preserves the last accepted orders and has exactly one accepted/rejected result. | Headless command-path test. |
| UI-04 | Human and AI accepted orders appear in one attributed comms log; an AI entry links to its decision intent trace. | Projection test with deterministic fixture. |
| UI-05 | Typed and direct-manipulation submission of an equivalent valid order reach the same canonical command path and outcome. | Command-adapter integration test. |
| UI-06 | Rejected commands expose their reason without altering authoritative state or destroying the player’s draft input. | UI/controller test. |
| UI-07 | Attention cards expose their required metadata, retain deterministic ordering, and do not alter simulation state when acknowledged, filtered, or dismissed. | Projection/controller and replay-regression tests. |
| UI-08 | First detection of a hostile/unknown contact and own-side loss/battle-damage events create visible auto-pause reasons. | Time-control integration test. |
| UI-09 | A scenario cannot be approved while ADR-008 validation reports blocking errors; a clean validation report, preview, and approved revision are visibly linked. | Headless validation and editor-flow integration test. |
| UI-10 | Core cockpit controls meet the keyboard, focus, non-color, and accessible-name requirements of §3.10. | UI accessibility walkthrough or automated UI test. |
| UI-11 | Replaying the same scenario and seed produces the existing golden hash regardless of UI selections, panel state, attention acknowledgements, or map view. | Replay golden regression test. |

## 8. Next steps

1. Turn this capture into a PRD with acceptance criteria (`/product-management:write-spec`).
2. Wireframe the **attention panel card** first — highest-risk component (pause spam vs. missed events).
3. Prototype the order line against the existing command API — cheapest end-to-end slice.
4. Resolve open questions 1, 2, 4, and 5 before implementing their affected flows; select the MVP unit-detail projection set before its UI is built.
