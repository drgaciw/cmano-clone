# 24 - Human-on-the-Loop Authority, Approvals & Agent Recommendations

**Last Updated:** 2026-09-02  
**Status:** **DRAFT — not landed.** Proposed by the [2026-09-02 corpus review](../reviews/requirements-corpus-review-gitnexus-2026-09-02.md) §6; lives in `Game-Requirements/drafts/` pending owner triage. Landing checklist at the end.  
**FR reverse-ref:** proposed **FR-21** — *No lethal action without a named authority basis* (hub row to add in [01](../requirements/01-Project-Overview.md) Related Index when landing; `RequirementsHubContractTests` requires it)  
**CMO basis:** CMO ROE / WRA weapons-release authority and doctrine postures (Hold fire, Weapons tight, Weapons free); Aegis extension: LLM-agent-proposed orders, human approval loop, explainable withholds  
**Related:** [04](../requirements/04-Agent-Delegation.md) (autonomy levels — this doc owns what those levels *gate*), [13](../requirements/13-Doctrine-ROE-EMCON-WRA.md) (ROE/WRA inputs), [14](../requirements/14-Engagement-And-Fire-Control.md) (engage pipeline consumes the disposition), [17](../requirements/17-Replay-AAR-And-Order-Log.md) (audit rows), [20](../requirements/20-Command-And-Control-UI.md) (approval/escalation surface, proposed CMD-40/41), [22](../requirements/22-Drone-Swarm-Platforms.md) (SWARM-31 composite tracks), proposed 23 Kill-Chain Explainability (targetability chain feeds HOL-01), [07](../requirements/07-Agentic-Infrastructure.md) INF-7 "no silent lethal path", [08](../requirements/08-Agentic-Architecture.md) (skill envelope as the agent boundary)  
**Linear:** DRG-66/67 (pending-approval queue + panel), DRG-196 (agent-callable C2 skill contract, AGC-01…04), DRG-209 (authority & ROE projection), DRG-212 (threat assessment / weapon recommendation), DRG-217 (resource ranking), DRG-220 (collateral / CDE), DRG-226 (withheld-order next action), DRG-228 (escalation / approval-required gate ledger)  
**Evidence basis:** trunk `81831e76` (2026-08-29); every type and test named below was verified to exist on 2026-09-02; `[x]` boxes cite tests that are part of the trunk suite (AGENTS.md baseline ≥1638 / 0 failures) — re-run before landing.

## Purpose

Specify, in one place, **what gates a weapon release, what an agent may only propose, and what the human approval loop guarantees**. Today doc 04 defines autonomy levels and a one-line `HUMAN_IN_LOOP` promise, doc 13 defines ROE/WRA, and the code ships a full authority stack (`AutonomyGate`, `C2AuthorityProjector`, `EscalationGateProjection`, `PendingApprovalQueue`, `SkillEnvelopeValidator`, threat/resource/CDE projections) that no requirement text owns. This document owns the **authority semantics**; doc 04 keeps the autonomy-level vocabulary, doc 13 keeps policy inputs, doc 20 keeps the UI surface, proposed doc 23 keeps the targetability chain.

## Vision

The player is always able to answer three questions for any engagement: **who authorized it, under what basis, and what would have to change for a withheld shot to be permitted**. Agents propose, rank and explain; they never authorize. Every withhold carries a named gate. Nothing lethal happens in the sim without a human decision or an explicit, scenario-authored delegation of that decision — and both are visible in the order log and the replay.

## Owner triage (proposed — needs decision)

| Decision | Proposed outcome |
|----------|------------------|
| Charter | **In scope now.** HOL-01/02/03/05/06/07/08 are shipped headless (DRG-2xx, DRG-66/67, DRG-196); this doc back-fills their requirement text. |
| HOL-04 lethal full-autonomy opt-in | **Implement** (recommended): doc 04 already promises it. Alternative: de-scope with an ADR amendment and delete the doc 04 sentence. Either way the current "Shipped" mapping row in doc 04 must change. |
| Ownership split | 04 = autonomy levels + attention; **24 = gate semantics, approval loop, agent recommendations**; 13 = ROE/EMCON/WRA inputs; 20 = approval/escalation UI (CMD-40/41); 23 = targetability chain. |
| Headless batch runs | Batch/AvA runs have no human: approval-gated orders must resolve by **scenario policy** (auto-approve, auto-reject, or FullAutonomous with HOL-04 opt-in authored in the scenario) — never by a silent default. |
| Glossary | Add "approval gate", "escalation gate (authority)" vs "escalation tier (political, Phase N)", "skill lane", "track source", "required approval" to [12](../requirements/12-Terms-Glossary.md). |

## Requirement index

| ID | Requirement | Priority | Status @ 81831e76 |
|----|-------------|----------|-------------------|
| HOL-01 | Authority disposition per C2 action | P0 | Shipped (headless) |
| HOL-02 | Escalation / approval-required gate ledger | P0 | Shipped (headless) |
| HOL-03 | Pending-approval queue semantics | P0 | Shipped core; **Partial** semantics (expiry, mode change, batch policy) |
| HOL-04 | Lethal full-autonomy opt-in | P0 | **GAP** — no code |
| HOL-05 | Agent-callable C2 skill contract (propose ≠ authorize) | P0 | Shipped (headless) |
| HOL-06 | Threat assessment & weapon recommendation | P1 | Shipped (headless) |
| HOL-07 | Resource ranking under scarcity | P1 | Shipped (headless) |
| HOL-08 | Collateral / CDE advisory | P1 | Shipped (headless) |
| HOL-09 | Audit rows for every authority decision | P0 | **Partial** — approve/reject/gate rows to verify in order log |
| HOL-10 | Approval & escalation surface (UI) | P1 | Partial — `PendingApprovalPanelHost` exists; escalation ledger surface open (→ doc 20 CMD-40/41) |

---

# Phase A — Authority core (shipped headless; text back-fill)

## HOL-01 — Authority disposition per C2 action **[P0]**

**Requirement.** For each C2 action kind (assess, datalink-reason, pairing-recommend, explain, propose-engage, submit) the system SHALL derive a disposition **Allowed / Withheld / RequiresApproval** from the skill lane, the required-approval class, the ROE snapshot and the track source, without issuing any order.

**Rules.**
- Track source ∈ {`Organic`, `DatalinkShared`, `FusedWithoutOrganicFc`, `Unknown`} (`TrackSource`). Targeting on a **shared track without organic fire control** is withheld even under Weapons Free (reason `SharedTrackNoRelease`).
- Withhold reasons are drawn from a fixed set: `WeaponsTight`, `RoeHoldFire`, `NO_FIRE_CONTROL`, `SharedTrackNoRelease`, `WeaponsReleaseRequired`, `ApprovalRequired`, `NOT_HUMAN_CONTROL`, `LANE_SUBMIT_NO_RECOMMEND` (constants on `C2AuthorityProjector`). New reasons extend the set only with a doc update.
- The projection is pure: identical context ⇒ identical projection.

**Acceptance criteria.**
- [x] AC-1 A `DatalinkShared` track under Weapons Free yields targeting *withheld* with `SharedTrackNoRelease` — `C2AuthorityProjectorTests.Shared_track_withholds_targeting_even_under_weapons_free`.
- [x] AC-2 A propose-engage envelope requires `RequiredApproval.WeaponsRelease` — `SkillEnvelopeValidatorTests.Propose_engage_requires_weapons_release_approval`.
- [ ] AC-3 Golden replay: the authority projection of every Baltic v3 engagement is byte-identical across two runs (add to `ReplayGoldenSuiteTests` or a dedicated fingerprint test).

**Implementation mapping.** `src/ProjectAegis.Delegation/Skills/C2AuthorityProjector.cs` (`Project(in C2AuthorityProjectionContext)`, `ParseRoeLabel`), `SkillIds.cs` (`TrackSource`, `RequiredApproval`, `SkillLane`).

## HOL-02 — Escalation / approval-required gate ledger **[P0]**

**Requirement.** The system SHALL publish, per contact or order, a deterministic ledger of the gates currently blocking release, each with a stable code and an ROE/authority reason. Weapons-free organic targeting SHALL emit **no** gate rows; a gated row SHALL never be silent.

**Rules.**
- Gate codes: `HOLD_FIRE`, `WEAPONS_TIGHT`, `HIGHER_HQ` (`EscalationGateCode`). `HIGHER_HQ` denotes a required approval above the unit (`RequiredApproval.WeaponsRelease` or `Operator`).
- Vocabulary: an **escalation gate** is an *authority* concept (this doc). It is not the political **escalation tier / ladder** of doc 10, which remains Phase N; `SpeculativeHonestyPinsTests` forbids only the literal type name `EscalationTier`.
- Ledger rows are derived from policy and order-log facts only; the projector never mutates state.

**Acceptance criteria.**
- [x] AC-1 Weapons-free organic targeting emits no gate rows — `EscalationGateProjectionTests.Weapons_free_organic_targeting_emits_no_gate_rows`.
- [x] AC-2 A propose-engage under `HIGHER_HQ` produces a weapons-release gate row — `EscalationGateProjectionTests.Higher_hq_propose_engage_requires_weapons_release_gate`.
- [ ] AC-3 Every row in `EscalationGateSnapshot` has a non-empty code and reason (add a property-style test over the Baltic v3 fixtures).

**Implementation mapping.** `src/ProjectAegis.Delegation/EscalationGate/EscalationGateProjection.cs` (`Project(EscalationGateInput)`, `Project(IReadOnlyList<EscalationGateInput>)` → `EscalationGateSnapshot`), `EscalationGateCode.cs`.

## HOL-03 — Pending-approval queue semantics **[P0]**

**Requirement.** An order whose gate result is `QueueForApproval` SHALL NOT execute until a human approves it; rejection SHALL be recorded; approved orders SHALL drain into the next tick in queue order; the queue is session-local and single-threaded.

**Rules (shipped).**
- `AutonomyGate.Evaluate(autonomy, order, playerApproved)` returns `GateResult(ExecuteNow, QueueForApproval, Rejected, PolicyDenialReason)`. ROE rejection wins over autonomy. Current matrix: **Manual** → execute only if `playerApproved`, else queue; **Assisted** → `RiskLevel.Low` executes, `High` queues unless approved; **SemiAutonomous / FullAutonomous** → execute (see HOL-04).
- `PendingApprovalQueue`: `Enqueue(Order)`, `TryApprove(OrderId)`, `TryReject(OrderId)`, `DrainApproved()`, `Pending`.

**Rules (to specify — Partial).**
- *Staleness:* a queued order whose target contact is Lost or whose ROE snapshot changed SHALL be re-evaluated on drain, not executed blindly; the re-evaluation outcome is logged.
- *Expiry:* an optional scenario knob `approvalExpiryTicks`; expired entries are rejected with a named reason.
- *Mode / phase changes:* Begin Execution, pause and autonomy-level changes SHALL NOT silently execute or drop queued orders; the queue survives pause and is re-gated when the autonomy level rises.
- *Headless batch:* runs without a human SHALL resolve queued orders by scenario policy (`autoApprove | autoReject`) declared in the policy JSON; absence of the policy is a validation error for batch profiles, never a silent default.
- *Replay:* approve/reject timing is a `PlayerOrder`-kind order-log row so replays reproduce the decision at the same tick.

**Acceptance criteria.**
- [x] AC-1 Queue invariants (no execution before approval, order preserved, reject removes) — `PendingApprovalQueueTests` (12 facts).
- [x] AC-2 Queue projection rows and badge — `PendingApprovalProjectionTests`.
- [ ] AC-3 Stale-target re-evaluation on drain (new test).
- [ ] AC-4 Batch policy `autoApprove`/`autoReject` honoured; missing policy fails validation for batch profiles (new tests in `ScenarioValidationEngine` rules + `BalticBatchRunner`).
- [ ] AC-5 Approve/reject rows present in the replay fingerprint (extend `EngagementOrderLogContractTests`).

**Implementation mapping.** `src/ProjectAegis.Delegation/Orchestration/AutonomyGate.cs`, `PendingApprovalQueue.cs`; `src/ProjectAegis.Delegation/Projection/PendingApprovalProjection.cs`; `unity/ProjectAegis/Assets/Scripts/Runtime/PendingApprovalPanelHost.cs`.

## HOL-04 — Lethal full-autonomy opt-in **[P0] — GAP**

**Requirement.** A `FullAutonomous` (and, by owner decision, `SemiAutonomous`) engagement order with lethal effect SHALL execute without human approval **only if** the scenario policy or the active mission phase carries an explicit opt-in; otherwise the gate result SHALL be `QueueForApproval`. The opt-in SHALL be authored data (scenario/policy JSON), visible in the C2 top bar, and recorded in the order log when it takes effect.

**Why this is a gap.** Doc 04 states "full autonomous lethal engagement requires explicit player opt-in per mission phase" and grades the row Shipped; `AutonomyGate.Evaluate` returns `ExecuteNow` for `SemiAutonomous or FullAutonomous` immediately after the ROE check, with no opt-in input anywhere in `src/`.

**Rules.**
- New policy field `engage.lethalAutonomyOptIn` ∈ {`none`, `perMissionPhase`, `scenario`} with an optional phase list; default `none` (fail closed).
- Non-lethal orders (move, sensor posture, EMCON) are unaffected.
- Headless batch profiles MUST set the field explicitly (see HOL-03 batch rule).

**Acceptance criteria.**
- [ ] AC-1 `FullAutonomous` lethal order with opt-in `none` ⇒ `QueueForApproval` (new case in `AutonomyGateTests`).
- [ ] AC-2 With opt-in `scenario` ⇒ `ExecuteNow`, and an order-log row records the opt-in basis.
- [ ] AC-3 Baltic v2 replay hash `17144800277401907079` is preserved (v2 policies must be authored with the opt-in that reproduces today's behaviour, or the change is gated behind a policy version).

**Implementation mapping.** None today. Touches `AutonomyGate`, `ScenarioPolicyProfile`/policy JSON schema, `C2TopBarProjection`, order-log kinds. Blast radius note: `DelegationBridge` is zero-touch by invariant; route the opt-in through `SimulationSession` policy resolution.

## HOL-05 — Agent-callable C2 skill contract **[P0]**

**Requirement.** LLM agents SHALL interact with C2 only through catalogued skills — `c2.track.assess`, `c2.datalink.reason`, `c2.pairing.recommend`, `c2.explain` — whose envelopes carry evidence pointers (`EvidenceKind` ∈ Contact, Unit, Policy, OrderLog, Projection, Snapshot) and an authority basis. Read-lane envelopes SHALL carry no command id; propose-lane envelopes SHALL never imply engagement authorization; `c2.skill.submit` is a host verb resolved through `C2CommandIssuance`, never by the agent.

**Rules.**
- Lanes: `SkillLane` ∈ {`Read`, `Propose`, `Submit`}. Only `Submit` may reach the orchestrator, and only after HOL-01/02 dispositions are Allowed or approved.
- The contract of record is `production/docs/skills/agent-c2-skill-contract/CONTRACT.md` (AGC-01…04, `catalog.json`, `verify-contract.ps1`); landing this doc promotes it into the corpus by reference.

**Acceptance criteria.**
- [x] AC-1 A propose envelope does not imply authorization — `SkillEnvelopeValidatorTests.Propose_does_not_imply_engagement_authorization`.
- [x] AC-2 A read-lane envelope carrying a command id is rejected — `SkillEnvelopeValidatorTests.Read_with_commandId_fails`.
- [x] AC-3 Catalog integrity (every skill id has a descriptor and lane) — `SkillCatalogTests`.
- [ ] AC-4 `verify-contract.ps1` runs in CI (→ proposed doc 26 VER-03).

**Implementation mapping.** `src/ProjectAegis.Delegation/Skills/{SkillCatalog,SkillEnvelope,SkillEnvelopeValidator,SkillIds,C2AuthorityProjector}.cs`; `src/ProjectAegis.Delegation/Input/C2CommandIssuance.cs`.

---

# Phase B — Agent recommendations (shipped headless; text back-fill)

## HOL-06 — Threat assessment & weapon recommendation **[P1]**

**Requirement.** In Assisted mode (and as an explanation in higher modes) the system SHALL produce, per hostile contact, a ranked weapon recommendation with a confidence range and the policy constraints applied, and SHALL withhold the recommendation — naming the gate — under Weapons Tight, Hold Fire, DLZ-out, Winchester, or a magazine below the required salvo. A recommendation is advisory: it never issues fire.

**Acceptance criteria.**
- [x] AC-1 Feasible recommendation includes confidence range and constraints — `ThreatAssessmentProjectionTests.Feasible_recommendation_includes_confidence_range_and_policy_constraints`.
- [x] AC-2 Winchester (empty magazine) withholds even when all other gates pass — `ThreatAssessmentProjectionTests.Winchester_empty_mag_withholds_even_when_other_gates_pass`.
- [ ] AC-3 Fingerprint stability across runs for identical inputs (`ThreatAssessmentProjection.ComputeFingerprint`; add golden).

**Implementation mapping.** `src/ProjectAegis.Delegation/ThreatAssessment/{ThreatAssessmentProjection,WeaponRecommendation}.cs`. Inputs: doc 14 DLZ state, doc 16 ordnance bands (LOG-12 proposed), doc 13 policy.

## HOL-07 — Resource ranking under scarcity **[P1]**

**Requirement.** When several shooters or weapons are eligible for a target, the system SHALL rank them deterministically with scores and a disposition, and every excluded candidate SHALL carry a named reason (e.g., already committed).

**Acceptance criteria.**
- [x] AC-1 Two candidates rank in a stable preferred order — `ResourceRankProjectionTests.Two_candidates_rank_with_stable_preferred_order`.
- [x] AC-2 A candidate excluded by commitment carries a named reason — `ResourceRankProjectionTests.Candidate_excluded_by_commitment_has_named_reason`.

**Implementation mapping.** `src/ProjectAegis.Delegation/ResourceRank/ResourceRankProjection.cs` (`Project(candidates)` → `ResourceRankSnapshot`, `ComputeFingerprint`).

## HOL-08 — Collateral / CDE advisory **[P1]**

**Requirement.** The system SHALL emit a per-engagement collateral advisory (risk kind, assumptions, geometry, applicable policy fields) that never authorizes release. An explicit CDE withhold SHALL take precedence over a clear preview and over policy; victim-scoped policy denials SHALL not by themselves withhold.

**Acceptance criteria.**
- [x] AC-1 CDE withhold precedence — `CdeAssessProjectionTests.Cde_withhold_takes_precedence_over_clear_preview_and_policy`.
- [x] AC-2 Low-risk output is advisory and never authorizes — `CdeAssessProjectionTests.Low_risk_output_is_advisory_never_authorizes`.
- [ ] AC-3 A CDE withhold appears as a gate row (HOL-02) and an order-log denial (HOL-09).

**Implementation mapping.** `src/ProjectAegis.Delegation/CdeAssess/CdeAssessProjection.cs` (`Project(CdeAssessInput, DecisionLog?)` → `CdeAssessSnapshot`). Related: doc 13 WRA target category, doc 18 DOM-13 (proposed), doc 10 `ACCOUNTABILITY_EVENT` (design only).

---

# Phase C — Auditability and surface

## HOL-09 — Audit rows for every authority decision **[P0] — Partial**

**Requirement.** Every gate outcome (execute, queue, approve, reject, withhold with gate code, opt-in taking effect) SHALL be an order-log row with the authority basis, so replay and AAR can answer "who authorized what, under what basis". Rows are append-only (doc 17 RPL-01) and included in the replay fingerprint.

**Acceptance criteria.**
- [ ] AC-1 Approve and reject produce rows with the actor kind and tick (verify against `EngagementOrderLogContractTests`; add if missing).
- [ ] AC-2 Every `EscalationGateSnapshot` row is reconcilable to an order-log denial or policy row (fingerprint cross-check).
- [ ] AC-3 AAR ledger (DRG-218, proposed RPL-30) can filter by authority basis.

**Implementation mapping.** `DecisionLog`, `OrderLogEntryKind`, `PendingApprovalQueue`; evidence to collect — status Partial until AC-1 is proven.

## HOL-10 — Approval & escalation surface **[P1] — Partial**

**Requirement.** Pending orders at Manual/Assisted appear in a queue with APPROVE / REJECT, each carrying its gate code and the withheld-order next action (DRG-226, proposed KCX-07); critical approvals may raise an attention card that auto-pauses per the watch policy. Defined in doc 20 as CMD-40 (approval queue surface) and CMD-41 (alert tiers); this doc only fixes the semantics the surface must show: gate code, basis, age, and what unblocks it.

**Acceptance criteria.** Owned by doc 20 (CMD-40/41); headless evidence today: `PendingApprovalProjectionTests`, `EngageNextActionProjectionTests`, `AttentionToastHostContractTests`.

**Implementation mapping.** `unity/ProjectAegis/Assets/Scripts/Runtime/PendingApprovalPanelHost.cs`; `src/ProjectAegis.Delegation/EngageNextAction/EngageNextActionProjection.cs`; `src/ProjectAegis.Delegation/Watch/WatchAutoPauseGate.cs`.

---

## Formulas & tuning knobs

| Knob / rule | Value today | Source | Proposed change |
|---|---|---|---|
| Gate matrix by autonomy | Manual: approve-or-queue; Assisted: Low executes, High queues; Semi/Full: execute | `AutonomyGate.Evaluate` | HOL-04 adds the opt-in test before Semi/Full execute |
| Risk classification | `RiskLevel` ∈ {Low, High} on `Order` | `Order.cs` | doc 04 should define which order kinds are High (lethal, EMCON change, detach) |
| Required approval class | `None`, `Operator`, `WeaponsRelease` | `SkillIds.cs` | unchanged |
| Gate codes | `HOLD_FIRE`, `WEAPONS_TIGHT`, `HIGHER_HQ` | `EscalationGateCode.cs` | add `CDE_WITHHOLD`, `APPROVAL_EXPIRED` when HOL-03/08 ACs land |
| Approval expiry | none | — | `approvalExpiryTicks` (policy JSON), default 0 = never |
| Batch resolution | none (would stall) | — | `batchApproval` ∈ {autoApprove, autoReject}; required for batch profiles |
| Lethal opt-in | none | — | `engage.lethalAutonomyOptIn` ∈ {none, perMissionPhase, scenario}; default none |

## Edge cases

- Queued order whose target is Lost before approval → re-evaluate on drain; log the outcome; never fire on a stale contact (ties to proposed KCX-04).
- ROE tightened while orders are queued → previously approved orders are re-gated on drain; loosened ROE does not auto-approve.
- Autonomy raised to FullAutonomous with a non-empty queue → the queue drains only if HOL-04 opt-in is present; otherwise it stays pending and the top bar shows the count.
- Pause / Begin Execution → queue persists; approvals during pause take effect on the next tick after resume (deterministic tick).
- Shared-track engage under Weapons Free → withheld (`SharedTrackNoRelease`) until organic fire-control quality exists (doc 22 SWARM-31 CEC composite track counts as organic-quality only when the CEC mesh reports FC quality).
- Headless batch with no batch policy → validation error at scenario load, not a stalled run.
- Agent submits directly (lane `Submit`) → rejected `LANE_SUBMIT_NO_RECOMMEND`; only the host issues.

## Dependencies

Doc 04 (autonomy levels, attention/auto-pause), 13 (ROE/WRA snapshot, `PolicyUpdateRecord`), 14 (engage pipeline gate order — the 22-step chain must place HOL gates before launch), 16 (ordnance bands for HOL-06), 17 (order-log kinds, fingerprint), 20 (CMD-40/41), 22 (SWARM-31), proposed 23 (targetability), proposed 26 (CI for `verify-contract.ps1`).

## Open questions

1. Does the opt-in (HOL-04) apply to `SemiAutonomous` as well as `FullAutonomous`? (Recommendation: yes for lethal orders.)
2. Should `Operator` approval be satisfiable by an agent acting in a human-delegated role (doc 04 tiered rebrief), or strictly by the player? (Recommendation: strictly the player; agents can only pre-stage.)
3. Approval expiry default: never (0) or a doctrine default (e.g., 120 ticks)?
4. Should CDE withhold be a hard gate (blocks launch) or an advisory that requires explicit override? (Today: advisory precedence only.)

## Glossary additions (for doc 12)

**Approval gate** — a gate outcome that queues an order for human approval (`QueueForApproval`). **Escalation gate** — an authority gate row (`HOLD_FIRE`, `WEAPONS_TIGHT`, `HIGHER_HQ`); distinct from the Phase N political **escalation tier**. **Skill lane** — Read / Propose / Submit boundary for agent envelopes. **Track source** — Organic, DatalinkShared, FusedWithoutOrganicFc, Unknown. **Required approval** — None / Operator / WeaponsRelease. **Lethal opt-in** — scenario-authored permission for autonomous lethal release (HOL-04, proposed).

## Landing checklist

- [ ] Owner triage decisions recorded (table above), including the HOL-04 implement/de-scope call.
- [ ] Move to `Game-Requirements/requirements/24-Human-On-The-Loop-Authority.md`; add hub row **FR-21** in doc 01 Related Index (pin: `RequirementsHubContractTests`).
- [ ] Doc 04: change the "Autonomy + ROE gating — Shipped" row to reference HOL-03/04; delete or re-point the opt-in sentence.
- [ ] Doc 12 glossary rows; doc 20 CMD-40/41 stubs; doc 17 RPL cross-ref for HOL-09.
- [ ] Tracker row 24 (post-2026-07-09 delta section) and `/design-review` verdict recorded.
- [ ] Re-run `dotnet test ProjectAegis.sln` (floor per AGENTS.md) — no code changes are required to land the text; HOL-04 implementation is a separate story with `impact` analysis on `AutonomyGate` first.
