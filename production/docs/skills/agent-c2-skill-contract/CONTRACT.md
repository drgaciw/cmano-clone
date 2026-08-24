# Agent-callable C2 skill contract (AGC-01 through AGC-04)

**Status:** Proposed (DRG-196)  
**Date:** 2026-08-22  
**Source of truth:** this file plus `catalog.json` and `envelopes/skill-envelope.schema.json`  
**Slice:** A (track assessment, data-link health, pairing proposals, explanation)

Authorized models may call named C2 skills. The simulation stays the authority for world state, sensor and data-link facts, policy gates, engagement authorization, and replay. A skill that both reads a track and fires a weapon in one call is a leak. Split the work: read projections, stage a bounded proposal, submit only after approval.

This ticket ships the contract, not C# types. Do not add projection records here (DRG-179). Do not edit `DelegationBridge.cs`, `CatalogWriteGate`, `SimulationSession`, `BalticReplayHarness`, t2 policy, `MissionContactTargetClass`, or gauntlet skills.

## Requirements mapped

| ID | Requirement | Where this contract binds it |
| --- | --- | --- |
| AGC-01 | Named, discoverable, capability-scoped skills | `catalog.json` plus the four SKILL.md files next to this directory |
| AGC-02 | Consume projections; submit bounded proposals or commands through approved interfaces | Three lanes below |
| AGC-03 | Authority basis, required approval, player override; a recommendation never implies engagement authorization | Envelope fields `authorityBasis`, `requiredApproval`, `playerOverride`; `engagementAuthorizationImplied` is always false on propose |
| AGC-04 | Record skill identity, inputs, evidence, assumptions, rationale, output, sim time | Envelope plus `replayProvenance` |

AGC-05 (degraded-mode disclosure), AGC-06 remainder (mission-package coordination), and AGC-07 (verification scenarios) stay out of this slice.

Related law already in the repo:

- ADR-010: UI and agents are clients. They render projections and submit commands.
- ADR-003 / ADR-019: order log is append-only; AAR and skills read `IReadOnlyOrderLog`.
- ADR-002: `IPolicyEvaluator` still gates fire. Skills do not bypass it.
- ADR-018: a datalink-shared track is situational awareness. It does not authorize weapons release.
- INF-7: operator copilot is supervised. Accepting a suggestion enqueues a normal `PlayerOrder`. Dismiss leaves no mutation.

## Discovery

Skills are discovered from `catalog.json`. A host loads that file, not a free-form prompt dump.

Each catalog row has:

- `skillId` (dotted, stable)
- `lanes` (subset of `read`, `propose`, `submit`)
- `path` to the SKILL.md
- `projections` it may read (existing types only)
- `commandIds` it may name on propose/submit (must resolve through `C2CommandIssuance`)

Unknown `skillId` is a hard miss. Do not invent a sibling skill at call time. Add a catalog row first.

Slice A catalog:

| skillId | Capability | Lanes |
| --- | --- | --- |
| `c2.track.assess` | Track assessment | `read`, `propose` |
| `c2.datalink.reason` | Data-link / network-health reasoning | `read`, `propose` |
| `c2.pairing.recommend` | Sensor-to-shooter pairing | `read`, `propose` |
| `c2.explain` | Human-readable explanation | `read` |

No Slice A skill lists `submit` as a native lane. Submit is a host verb over an approved proposal (`c2.skill.submit`). That keeps pairing from growing a silent fire path.

## Three lanes

```
projection (IReadOnlyOrderLog, ISimWorldSnapshot indicators)
    │
    ▼
READ  ── assessment / explanation ── no order-log append
    │
    ▼
PROPOSE ── SkillProposal (session-local staging) ── dismiss = no mutation
    │  player or autonomy gate approves
    ▼
SUBMIT ── C2CommandIssuance.TryResolve → HumanController enqueue → PlayerOrder
         IPolicyEvaluator still evaluates
```

This is the same shape as catalog staging (`IWriteGate` propose then `ApproveBatch`) and as `PendingApprovalQueue` (DRG-66). Do not reuse those types. Catalog writes and C2 orders are different authorities. Rhyme the lifecycle; do not share the class.

### Lane `read`

Purpose: fold already-recorded state into an assessment.

May:

- Call existing `*Projection` types (`ContactPictureProjection`, `SensorC2Projection`, `DatalinkPictureProjection`, `EngageExplainProjection`, `PendingApprovalProjection`, `MessageLogProjection`, and others listed on the skill).
- Read `IReadOnlyOrderLog` and per-tick `ISensorC2WorldIndicators` / `ISimWorldSnapshot` fields.
- Return envelope `output` plus evidence pointers.

Must not:

- Append to `DecisionLog` / `IOrderLog`.
- Call `C2PlayerCommandBridge.TryIssue`, `TryEnqueueHumanOrder`, or any `IOrderSink`.
- Touch `CatalogWriteGate` or scenario packages.
- Do anything that changes `ComputeFingerprint()`.

Replay and AAR hosts may call `read` freely. `C2PlayerCommandBridge.ReasonReplayAttached` is irrelevant here because nothing is issued.

### Lane `propose`

Purpose: stage one bounded recommendation the player can accept, reject, or ignore.

A proposal is session-local staging. It is not an `Order`. It is not a `PlayerOrderRecord`. Reject and TTL-expire drop the row. INF-7.4 applies: dismissed suggestions leave no order-log mutation. The audit of a rejected proposal, if any, is a presentation/AAR note, not an append.

Bounds (all of these, every proposal):

1. One `skillId`, one `proposalId`, at most one `commandId`.
2. `commandId` empty, or a value `C2CommandIssuance.TryResolve` already knows (`hold`, `rtb`, `move` / `plot_course`, `engage`, `set_emcon`, `set_sensors`, `launch` / `launch_aircraft`, `abort_launch`, `launch_boat`, `recover_boat`, `abort_boat_launch`). Unknown ids fail with `UNKNOWN_COMMAND`.
3. `ttlTicks` set. Default 30. Host drops the proposal when `simTick` exceeds `createdSimTick + ttlTicks`.
4. `requiredApproval` is `operator` or `weaponsRelease`. Never `none` when a `commandId` is present.
5. `authorityBasis.engagementAuthorizationImplied` is `false`. A pairing or track note that names `engage` is still a recommendation.
6. `playerOverride` names the command the player uses to countermand (`hold` is the usual stop).
7. Evidence cites at least one of `unitId`, `contactId`, `policySnapshotId`, `sequenceId` (INF-7.1).
8. If `authorityBasis.trackSource` is `datalinkShared` or `fusedWithoutOrganicFc`, `commandId` must not be `engage` and `requiredApproval` must not be `weaponsRelease` (ADR-018).

`PendingApprovalQueue` is the execution-side queue for orders the autonomy gate already promoted to `QueueForApproval`. A skill proposal is earlier than that. Promote to that queue only on submit, and only when the host maps the proposal onto a real `Order`.

### Lane `submit` (`c2.skill.submit`)

Purpose: turn an approved proposal into the same command the player could have issued from the toolbar.

Preconditions:

1. `proposalId` exists, is unexpired, and is in state `approved`.
2. `commandId` still resolves through `C2CommandIssuance`.
3. Target unit is under `HumanController` (player took the slot, or the unit was already human). Skill submit does not steal a delegated slot. That is a separate control-change, not this contract.
4. Replay viewer is not attached. Same refusal as `C2PlayerCommandBridge.ReasonReplayAttached`.
5. `IPolicyEvaluator.Evaluate` still runs. Copilot cannot bypass `FireAbortReason` (INF-7.3).
6. Organic fire-control is required when `commandId` is `engage`. `HasFireControlTrackOnPrimaryContact` (or the unit-scoped equivalent) must be true. Shared SA does not count.

On success the host enqueues through the existing player path (`C2PlayerCommandBridge.TryIssue` / `TryEnqueueHumanOrder`). The order log gains a `PlayerOrder` with the usual `sequenceId`. `replayProvenance.sequenceIdOnSubmit` records that id. `replayProvenance.orderLogFingerprintBefore` is the fingerprint taken immediately before the append.

On failure return a structured reason (`UNKNOWN_COMMAND`, `NO_SELECTION`, `UNKNOWN_UNIT`, `NOT_HUMAN_CONTROL`, `REPLAY_ATTACHED`, `ENQUEUE_FAILED`, `POLICY_DENIAL`, `NO_FIRE_CONTROL`, `PROPOSAL_EXPIRED`, `PROPOSAL_NOT_APPROVED`, `SHARED_TRACK_NO_RELEASE`). Do not retry by mutating sim internals.

## Envelope

JSON Schema: [`envelopes/skill-envelope.schema.json`](envelopes/skill-envelope.schema.json).

Field groups:

| Group | Fields | Lane |
| --- | --- | --- |
| Identity | `lane`, `skillId`, `invocationId` | all |
| Time | `simTick`, `simTime`, `scenarioId`, `seed` | all |
| Body | `inputs`, `evidence[]`, `assumptions[]`, `rationale`, `output` | all |
| Authority | `authorityBasis` | propose, submit; optional on read |
| Approval | `requiredApproval`, `proposalId`, `commandId` | propose, submit |
| Override | `playerOverride` | propose, submit |
| Provenance | `replayProvenance` | all |

`c2.explain` may be called on its own (`read`) or nested: every `propose` output includes an `explanation` object with the same fields `c2.explain` would return. Do not ship a proposal with an empty rationale.

## Authority basis

`authorityBasis` answers "why would this be legal if someone later fires?" It is not a clearance.

Required keys on propose/submit:

- `policySnapshotId` or an explicit `policyUnavailable: true` with assumption text
- `roe` (string label from the current doctrine projection)
- `emcon` (observer radar EMCON from `ISensorC2WorldIndicators.ObserverRadarEmconActive` or unit-scoped equivalent)
- `trackSource`: `organic` | `datalinkShared` | `fusedWithoutOrganicFc` | `unknown`
- `fireControlSatisfied`: boolean from `HasFireControlTrackOnPrimaryContact` when a primary contact exists
- `engagementAuthorizationImplied`: always `false` on propose

A host that sets `engagementAuthorizationImplied: true` is non-compliant even if the player later approves. Approval is a separate act.

## Player override

`playerOverride` is the countermand path, not the submit path.

Required keys on propose/submit:

- `path`: `C2PlayerCommandBridge.TryIssue` (or a documented alias that still ends there)
- `commandId`: usually `hold`; may be `abort_launch` / `abort_boat_launch` when the proposal named a launch
- `controllerRequirement`: `HumanController`
- `rejectLeavesNoMutation`: `true` for proposals that have not submitted

The player can also reject the proposal in the pending-approval UI (`PendingApprovalProjection` / `TryReject`). That is an override too. It must not append a `PlayerOrder` solely to record the no.

## Replay provenance

Every invocation records:

- `skillId`, `invocationId`
- `simTick`, `simTime`
- `orderLogFingerprintBefore` (read and propose may omit if they truly did not append; submit must set it)
- `sequenceIdOnSubmit` (null until submit succeeds)
- `submitted` boolean

AGC-04 wants the decision inspectable on replay. Provenance travels with the envelope the AAR reads. It does not become a second event store. After submit, the order log is the authority; provenance points at `sequenceId`.

Do not put skill envelopes into the pinned ReplayGolden hash in this slice. Follow ADR-018's caution: shared and agent-side artifacts stay out of the Baltic production hash until a later ADR says otherwise. Headless tests may still round-trip envelopes against fixtures.

## Lane transition

| From | To | Trigger |
| --- | --- | --- |
| `read` | (stop) | Assessment only |
| `read` | `propose` | New invocation; copy evidence; new `invocationId` |
| `propose` | (drop) | Reject, ignore, or TTL |
| `propose` | `submit` | Explicit approve (`operator` or `weaponsRelease`) then `c2.skill.submit` |
| `submit` | (stop) | Success or structured failure |

There is no `read` → `submit` shortcut. There is no `propose` that appends.

## Forbidden in this contract

- New Unity host code, `DelegationBridge.cs` hotpath, `CatalogWriteGate`, `SimulationSession`
- New `*Projection` record types (owned by DRG-179)
- Treating `DatalinkPictureProjection` / shared contacts as fire-control
- NL that "just issues engage" because the model is confident
- Silent model substitution (AGC-05, later)

## C# types (this ticket)

Headless contract in `ProjectAegis.Delegation.Skills` (no enqueue):

| Type | Role |
| --- | --- |
| `SkillIds` / `SkillCatalog` / `SkillDescriptor` | AGC-01 discovery |
| `SkillLane` | `Read` / `Propose` / `Submit` |
| `SkillEnvelope` | Shared invocation |
| `AuthorityBasis` / `PlayerOverride` / `ReplayProvenance` / `EvidencePointer` | AGC-03 / AGC-04 fields |
| `SkillEnvelopeValidator` | Pure gate. Resolves command ids via `C2CommandIssuance`. Does not call `C2PlayerCommandBridge.TryIssue`. |

Filter: `FullyQualifiedName~ProjectAegis.Delegation.Tests.Skills`

Enqueue / host submit remains a later story. Keep three APIs, not one god method:

1. `ISkillRead.Read(envelope) -> envelope` (pure, headless)
2. `ISkillProposalQueue.Stage / Reject / Approve` (session-local, no log append)
3. `ISkillSubmit.Submit(proposalId) -> PlayerOrder | reason` wrapping `C2CommandIssuance` + human enqueue

