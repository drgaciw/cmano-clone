# 24 - Human-On-The-Loop Authority, Approvals & Agent Recommendations

**Last Updated:** 2026-09-02  
**Related:** [01-Project-Overview.md](../requirements/01-Project-Overview.md) · [04-Agent-Delegation.md](../requirements/04-Agent-Delegation.md) · [07-Agentic-Infrastructure.md](../requirements/07-Agentic-Infrastructure.md) · [08-Agentic-Architecture.md](../requirements/08-Agentic-Architecture.md) · [13-Doctrine-ROE-EMCON-WRA.md](../requirements/13-Doctrine-ROE-EMCON-WRA.md) · [14-Engagement-And-Fire-Control.md](../requirements/14-Engagement-And-Fire-Control.md) · [17-Replay-AAR-And-Order-Log.md](../requirements/17-Replay-AAR-And-Order-Log.md) · [18-Combat-Domains.md](../requirements/18-Combat-Domains.md) · [20-Command-And-Control-UI.md](../requirements/20-Command-And-Control-UI.md)  
**Status:** Draft (W3-HOL)  
**Requirement IDs:** `HOL-01` through `HOL-10`

---

## 1. Purpose & Vision

Define the **Human-On-The-Loop (HOTL)** and **Human-In-The-Loop (HITL)** authority architecture for Project Aegis. This document establishes how autonomous AI agents, supervisory co-pilots, and external model skills propose actions, evaluate doctrine constraints, surface advisory threat and resource assessments, manage pending approvals, and enforce escalation gates while guaranteeing that **the human player retains ultimate authority over lethal force, weapons release, and mission command**.

### Core Tenet: Propose ≠ Authorize
In Project Aegis, agent intelligence, skill invocations, threat projections, and targeting recommendations **propose** courses of action; they do **not** authorize weapons release or bypass doctrine gates.
- Staging a proposal is session-local and non-mutating to the simulation order log until explicit approval is granted.
- The simulation engine and its deterministic policy pipeline (`IPolicyEvaluator`, `AutonomyGate`, `RoePolicyAdapter`) remain the sole execution authorities.

---

## 2. Autonomy Tiers & Authority Dispositions

### 2.1 Autonomy Tiers
1. **Manual (`HUMAN_IN_LOOP`)**: Agent suggests actions; all non-trivial commands require player approval before enqueue.
2. **Assisted**: Agent executes routine/low-risk navigation or sensor actions automatically; high-risk decisions (such as engagement or posture shifts) require explicit player approval.
3. **Semi-Autonomous (`HUMAN_ON_LOOP`)**: Agent acts independently within bounded doctrine envelopes; the human player supervises via live telemetry and retains immediate override/countermand authority within a reaction window.
4. **Full Autonomous (`FULL_AUTONOMOUS`)**: Agent operates with minimal tactical oversight. Lethal engagement is governed by the ROE matrix and strict safety gates.

### 2.2 C2 Authority Dispositions & Escalation Gates
Every tactical action evaluated by C2 authority projectors (`C2AuthorityProjector`) and escalation gate ledgers (`EscalationGateProjection`, DRG-228) resolves to explicit, deterministic dispositions:
- **`Permitted` (0)**: Action is doctrine-compliant, fire-control is satisfied, and the current autonomy/approval state allows immediate issuance or execution.
- **`Withheld` (1)**: Action is blocked by doctrine (e.g., `ROE_HOLD_FIRE`, `ROE_WEAPONS_TIGHT`), lack of fire control, datalink shared-only track restrictions, or controller constraints.
- **`ApprovalRequired` (2)**: Action is tactically feasible but gated by operator approval (`Operator`) or lethal weapons release authorization (`WeaponsRelease`), generating advisory `HigherHq` escalation rows.
- **Corrective Next Action (`EngageNextActionProjection`, DRG-226)**: Withheld engagements project deterministic corrective action advisories (e.g., `ReloadRearm` for Winchester/NoAmmo or `Approval` for ROE gates).

### 2.3 Bounded Action Verbs
Authority projectors evaluate six core action kinds:
- **`Observe`**: Always `Permitted` to maintain situational awareness.
- **`Recommend`**: Permitted for advisory lanes; withheld for direct submission lanes.
- **`Approve`**: Permitted for valid pending gates; withheld if approval conditions or required credentials are not met.
- **`Engage`**: Permitted only when organic fire-control is held, ROE is `WeaponsFree`, and necessary approvals are cleared; otherwise `Withheld` or `ApprovalRequired`.
- **`Abort`**: Permitted for human-controlled units to immediately halt operations; withheld for non-human slots.
- **`Retask`**: Allows re-assigning courses, missions, or mounts subject to operator approval.

---

## 3. Skill Boundary & Contract Lifecycle

External model skills, AI agents, and C2 advisor extensions interact with the simulation across three strictly segregated lanes (`production/docs/skills/agent-c2-skill-contract/`):

```
┌────────────────────────────────────────────────────────────────────────┐
│ Simulation Authority (ISimWorldSnapshot, IReadOnlyOrderLog)            │
└───────────────────────────────────┬────────────────────────────────────┘
                                    │
                                    ▼
┌────────────────────────────────────────────────────────────────────────┐
│ LANE: READ (Assess, reason, explain — zero mutation / no order append) │
└───────────────────────────────────┬────────────────────────────────────┘
                                    │
                                    ▼
┌────────────────────────────────────────────────────────────────────────┐
│ LANE: PROPOSE (Session-local staging — engagementAuthorization=false)  │
└───────────────────────────────────┬────────────────────────────────────┘
                                    │
               ┌────────────────────┴────────────────────┐
               │ Player Approve / Autonomy Gate Promotes │
               ▼                                         ▼
┌────────────────────────────────┐       ┌───────────────────────────────┐
│ LANE: SUBMIT (c2.skill.submit) │       │ DISMISS / REJECT / EXPIRE     │
│ Enqueue via HumanController    │       │ Drops staging; zero mutation  │
│ IPolicyEvaluator still checks  │       └───────────────────────────────┘
└────────────────────────────────┘
```

1. **Read Lane**: Consumes read-only indicators and projections (`ContactPictureProjection`, `SensorC2Projection`, `DatalinkPictureProjection`, `EngageExplainProjection`). Prohibited from appending to `DecisionLog` or calling `IOrderSink`.
2. **Propose Lane**: Emits a bounded `SkillProposal` (containing `proposalId`, `skillId`, `commandId`, `ttlTicks`, `requiredApproval`, and `playerOverride`). `authorityBasis.engagementAuthorizationImplied` is always `false`. Dismissal or TTL expiration leaves zero footprint in the order log.
3. **Submit Lane (`c2.skill.submit`)**: Bridges an approved proposal into execution. Verifies that the proposal was approved, the target is under human control, replay is not attached, and organic fire-control track is satisfied. Enqueues through `C2CommandIssuance` / `HumanController`.

---

## 4. Requirements & Acceptance Criteria

### HOL-01: Propose ≠ Authorize Lifecycle
- **Description**: AI agent and supervisory recommendations, sensor-to-shooter pairings, and skill outputs must stage proposals without mutating the simulation order log or granting implied weapons release.
- **AC-HOL-01.1**: Propose-lane invocations MUST set `authorityBasis.engagementAuthorizationImplied = false`.
- **AC-HOL-01.2**: Dismissing, rejecting, or expiring a proposal MUST NOT append any record to `DecisionLog` or `IOrderLog`.
- **AC-HOL-01.3**: Proposals MUST specify a time-to-live (`ttlTicks`, default 30 ticks); expired proposals are dropped automatically without side effects.

### HOL-02: Pending Approval Queue & Execution Gate
- **Description**: The simulation MUST maintain a session-local `PendingApprovalQueue` for gating orders flagged with `GateResult.QueueForApproval`.
- **AC-HOL-02.1**: Orders enqueued to `PendingApprovalQueue` MUST NOT execute until explicitly approved via `TryApprove(orderId)`.
- **AC-HOL-02.2**: Rejection via `TryReject(orderId)` MUST remove the entry without execution.
- **AC-HOL-02.3**: Approved orders are drained via `DrainApproved()` and promoted to `ExecutedOrders` on the subsequent simulation tick.
- **AC-HOL-02.4**: Queue operations MUST be idempotent; duplicate `OrderId` submissions MUST be ignored.

### HOL-03: C2 Authority & Targeting Disposition Projection
- **Description**: Headless projection `C2AuthorityProjector` MUST evaluate ROE, track source, fire-control status, and required approval into explicit, non-implicit authority dispositions.
- **AC-HOL-03.1**: When `RoeLevel` is `HoldFire` or `WeaponsTight`, targeting disposition MUST be `Withheld` with reason codes `RoeHoldFire` or `WeaponsTight`.
- **AC-HOL-03.2**: When track source is `DatalinkShared` or `FusedWithoutOrganicFc`, targeting disposition MUST be `Withheld` with reason `SHARED_TRACK_NO_RELEASE` (ADR-018).
- **AC-HOL-03.3**: When `RequiredApproval` is `Operator` or `WeaponsRelease`, targeting disposition MUST be `ApprovalRequired` with corresponding reason codes.
- **AC-HOL-03.4**: Six core action verbs (`Observe`, `Recommend`, `Approve`, `Engage`, `Abort`, `Retask`) MUST each receive an explicit `C2AuthorityActionState`.

### HOL-04: Lethal Autonomy Phase Opt-In Gate (Phase N / GAP)
- **Description**: Policy-level gating requiring explicit player opt-in per mission phase before autonomous lethal engagements can be executed.
- **Status**: **Phase N / GAP** (De-scoped in governance decision A-01 / ADR-023).
- **Current Shipped Behavior**: `AutonomyGate.Evaluate` executes immediately for `SemiAutonomous` and `FullAutonomous` once ROE permits fire; policy field `engage.lethalAutonomyOptIn` is not implemented in v1.0.
- **AC-HOL-04.1 (Target Specification)**: In future phases, when `lethalAutonomyOptIn` is enabled in scenario policy, full autonomous controllers MUST transition lethal fire orders to `ApprovalRequired` unless the current phase has received human authorization.

### HOL-05: Agent C2 Skill Contract & Envelopes
- **Description**: Agent-callable C2 skills MUST conform to the structured envelope schema (`catalog.json`, `skill-envelope.schema.json`).
- **AC-HOL-05.1**: Every envelope MUST declare `lane` (`Read`, `Propose`, or `Submit`), `skillId`, `invocationId`, and time credentials (`simTick`, `simTime`).
- **AC-HOL-05.2**: Propose and submit envelopes MUST include `authorityBasis`, `requiredApproval`, `playerOverride`, and `evidence` pointers (citing `unitId`, `contactId`, `policySnapshotId`, or `sequenceId`).
- **AC-HOL-05.3**: `SkillEnvelopeValidator` MUST validate command IDs against known `C2CommandIssuance` verbs.

### HOL-06: Advisory Threat Assessment & Weapon Recommendation
- **Description**: Headless threat assessment projection (`ThreatAssessmentProjection`, DRG-212) MUST evaluate DLZ range, magazine state, mount status, and doctrine without issuing fire orders.
- **AC-HOL-06.1**: `WeaponRecommendation` MUST explicitly assert `IsWeaponsReleaseAuthorization = false`, `IsFireOrder = false`, and `IsAutomaticEngagement = false`.
- **AC-HOL-06.2**: Recommendations MUST classify outcomes into `Feasible`, `WithheldByPolicy`, or `WithheldByEngage`.
- **AC-HOL-06.3**: Recommendations MUST include human-readable assumptions, DLZ range assessment, policy constraints, confidence score, and status line.
- **AC-HOL-06.4**: `ComputeFingerprint` MUST produce deterministic, replay-stable fingerprints across runs given identical inputs.

### HOL-07: Resource Scarcity Ranking & Weapon Pairing
- **Description**: Headless resource ranking projection (`ResourceRankProjection`, DRG-217) MUST evaluate and rank candidate shooter/weapon pairings under scarcity constraints.
- **AC-HOL-07.1**: `ResourceRankSnapshot` MUST explicitly assert `IsWeaponsReleaseAuthorization = false`, `IsFireOrder = false`, and `IsAutomaticEngagement = false`.
- **AC-HOL-07.2**: Candidates MUST be scored across five deterministic weights: Expected Effect (0.35), Time-to-Effect (0.20), Round Availability (0.20), Commitment (0.15), and Magazine Conservation (0.10).
- **AC-HOL-07.3**: Candidates MUST be categorized into `Preferred` (rank 1), `Alternative` (rank 2+), or `Excluded` (with explicit reason codes such as `ExcludedByPolicy`, `ExcludedByEngage`, `ExcludedByAvailability`, `ExcludedByCommitment`).
- **AC-HOL-07.4**: `ComputeFingerprint` MUST produce a deterministic fingerprint string for the snapshot.

### HOL-08: Collateral Damage Estimation (CDE) & Advisory Withhold
- **Description**: Headless CDE projection (`CdeAssessProjection`, DRG-220) MUST evaluate collateral risks, range classes, and policy denials to generate advisory assessment rows.
- **AC-HOL-08.1**: `CdeAssessSnapshot` MUST be strictly advisory and MUST NOT emit weapons release or fire authorizations.
- **AC-HOL-08.2**: Risk MUST be categorized as `Low`, `Elevated` (e.g., engage preview blocked or out of envelope), or `Withheld` (when explicit collateral withhold is flagged or shooter-scoped policy denials apply).
- **AC-HOL-08.3**: Projections MUST record explicit assumptions regarding engage preview, policy denial records, and CDE withhold status.

### HOL-09: Presentation Gating & UI Host Binding
- **Description**: Presentation hosts in Unity/UI Toolkit (`PendingApprovalPanelHost`, `C2AuthorityProjector`, `WatchAttentionQueue`) MUST bind to headless projections without sim authority.
- **AC-HOL-09.1**: UI hosts MUST act as pure presentation clients (ADR-010), reading projections and dispatching user intent via command bridges.
- **AC-HOL-09.2**: The Pending Approval panel MUST display pending queue entries with order details, time enqueued, and Approve/Reject buttons.
- **AC-HOL-09.3**: In Play Mode and headless test harnesses, UI maturity hosts MUST satisfy smoke and integration assertions (`PlayModeSmokeHarnessTests`).

### HOL-10: Player Override & Non-Mutating Countermand
- **Description**: The human player MUST be able to countermand any proposed or executing action immediately.
- **AC-HOL-10.1**: Player override commands (`hold`, `abort_launch`, `abort_boat_launch`, `rtb`) issued to `HumanController` MUST take immediate precedence.
- **AC-HOL-10.2**: Countermanding an unsubmitted proposal via dismiss/reject MUST leave zero mutation in the order log (INF-7.4).

---

## 5. Traceability & Implementation Mapping

| Requirement ID | Shipped / Target Type | Source File | Test Suite / Evidence |
|---|---|---|---|
| **HOL-01** | `SkillEnvelopeValidator`, `SkillLane` | `src/ProjectAegis.Delegation/Skills/` | `SkillEnvelopeValidatorTests.cs`, `CONTRACT.md` |
| **HOL-02** | `PendingApprovalQueue`, `PendingApprovalEntry`, `EscalationGateProjection` | `src/ProjectAegis.Delegation/Orchestration/PendingApprovalQueue.cs`, `src/ProjectAegis.Delegation/EscalationGate/` | `PendingApprovalQueueTests.cs`, `EscalationGateProjectionTests.cs` |
| **HOL-03** | `C2AuthorityProjector`, `C2AuthorityProjection`, `EngageNextActionProjection` | `src/ProjectAegis.Delegation/Skills/C2AuthorityProjector.cs`, `src/ProjectAegis.Delegation/EngageNextAction/` | `C2AuthorityProjectorTests.cs`, `EngageNextActionProjectionTests.cs` |
| **HOL-04** | `AutonomyGate` (Phase N / GAP) | `src/ProjectAegis.Delegation/Orchestration/AutonomyGate.cs` | ADR-023 (Proposed), `AutonomyGateTests.cs` |
| **HOL-05** | `SkillCatalog`, `SkillEnvelope`, `SkillIds` | `src/ProjectAegis.Delegation/Skills/` | `SkillCatalogTests.cs`, `TEST-SPEC.md` |
| **HOL-06** | `ThreatAssessmentProjection`, `WeaponRecommendation` | `src/ProjectAegis.Delegation/ThreatAssessment/` | `ThreatAssessmentProjectionTests.cs` |
| **HOL-07** | `ResourceRankProjection`, `ResourceRankSnapshot` | `src/ProjectAegis.Delegation/ResourceRank/` | `ResourceRankProjectionTests.cs` |
| **HOL-08** | `CdeAssessProjection`, `CdeAssessSnapshot` | `src/ProjectAegis.Delegation/CdeAssess/` | `CdeAssessProjectionTests.cs` |
| **HOL-09** | `PendingApprovalPanelHost`, `PlayModeSmokeHarness` | `src/ProjectAegis.Delegation.UnityAdapter/` | `PlayModeSmokeHarnessTests.cs` |
| **HOL-10** | `PlayerOverride`, `C2PlayerCommandBridge` | `src/ProjectAegis.Delegation/Skills/C2AuthorityTypes.cs` | `SkillEnvelopeValidatorTests.cs` |
