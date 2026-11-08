# H6 save/resume contract

Same-build, same-catalog, same-scenario save and resume for a single-player session.
This page is the DRG-348 contract. It follows accepted
[ADR-029](../architecture/adr-029-h6-save-resume.md) (owner, 2026-10-15, Option B, waiver W0).
It does not implement save, edit `DelegationBridge` or `SimulationSession`, or define a
multiplayer protocol.

**Checked against** `main` `ff3c06a4a243586b8ebb87c1e2312b05e6c38d72`. Symbol names below were
read from that tree. Implementation still requires GitNexus impact on every symbol it edits
(ADR-029). This page edits none.

The save is a **versioned side file**. It is not an input to
`DecisionLog.ComputeFingerprint` or `SimWorldHash` (ADR-003). Restore **replays from tick 0**
through public calls. It does not hydrate private fields. `ReplayCheckpointStore` remains a
verify boundary. Its rows are not a world blob and are not a save payload.

---

## Decisions this contract inherits

ADR-029 and the DRG-328 owner decision (2026-10-07, Option B) close the product choices the
original DRG-348 text left open:

| Topic | Rule |
| --- | --- |
| Model | Replay-to-tick from an external input journal. No full snapshot and no skip-to-checkpoint in H6. |
| Compatibility | Load requires the same build, the same catalog, and the same scenario. A patch invalidates existing saves. There is no upgrade path. |
| Slots | Three manual slots plus one rolling autosave. Mid-engagement saves are allowed. |
| Restored authority | Orders, pending approvals, and the direct-control override come back by replay. |
| After a successful load | The sim is paused. Watch acknowledgements and dismissals are cleared. |
| Load cost | Re-sim from tick 0 is acceptable until a measured load exceeds the owner's ceiling (about 30 s). That measurement is DRG-349. |
| Waiver | W0. No new members and no new lines on `DelegationBridge` or `SimulationSession`. |
| Multiplayer | Out of scope. No network fields in the envelope. |

Two items stay unspecified on purpose. **Autosave cadence** (which tick, or which other
condition, writes the rolling slot) is not chosen here. **Canonical byte layout, field
widths, and parser bounds** are not chosen here. DRG-349 must not invent either by silent
default. A failed or partial write must still obey the reject rules below.

---

## Capture classes

Every state owner is one of these. The class is the contract. A later serializer that
hydrates a **Regenerated** owner, or that treats a **Checksum** as an input, does not
implement this page.

| Class | Meaning |
| --- | --- |
| **Identity** | Compared before any candidate is built. Mismatch rejects the load. The active session is not touched. |
| **Journal** | An external public call, recorded at the command façade, then forwarded unchanged. Replay applies the row. The decision log is not a substitute. |
| **Regenerated** | Rebuilt by scenario setup plus journal replay from tick 0. It must not appear as a hydrate blob. |
| **Checksum** | Stored so the candidate can be accepted or discarded after replay. It is not fed back in as an input. |
| **Discarded** | Replayed only as far as the journal needs for clock fidelity, then cleared on the candidate after the checksums match and before the swap. |
| **Excluded** | Not part of this save. |

**Capture boundary.** The envelope holds identity, the two clocks, the journal, and
checksums. Everything the sim can recompute from `(scenario, catalog, seed, journal)` stays
out of the payload. A mutation of authority-bearing state that is not a journal row fails
the save: the envelope is not written, and no slot changes.

**What is not a payload.** `ReplayCheckpoint` is `(SimTick, WorldHash, LogFingerprint,
LastSequenceId)`. `ReplayCheckpointStore.Record` / `FindAtOrBefore` do not hydrate.
`CatalogSnapshotHasher.ComputeSha256Hex` covers sorted sensor rows only
(`CatalogSnapshotBinder.BindAfterApprove` stores that digest on
`catalog_snapshot.content_hash_sha256`). That digest is not the whole-catalog identity
this contract requires. `DecisionLog` is a checksum of outputs. Re-applying its rows as
inputs double-applies sim-generated records and still misses the state-only calls below
(ADR-029, alternative 3).

---

## Inventory of state owners

### Identity, seeds, and scenario setup

| Owner | State | Class | Why |
| --- | --- | --- | --- |
| Build identity | Exact build id of the writing process | Identity | A different build is rejected before construction. Token format is unspecified; equality is exact. |
| Scenario package | Scenario id and a content hash of the loaded scenario | Identity | Setup (`ConfigureSimulationMode`, mission timeline, detection trials, OOB mesh) is reproduced from this package. |
| `DbSnapshotStore` | `snapshot_id` of the bound catalog | Identity | ADR-006. A save never writes through `CatalogWriteGate`. |
| Whole-catalog hash | Hash covering sensors, weapons, mounts, magazines, mobility, damage, and comms | Identity | Sensor-only `CatalogSnapshotHasher` and an empty `content_hash_sha256` are not sufficient. The function is unspecified; the coverage is not. |
| Bound policies | Each policy id and a hash of its loaded document (`*.policy.json` and the profile the session bound) | Identity | Includes the scenario policy id passed into `DelegationBridge`. |
| `DelegationOrchestrator.GlobalSeed` | Global seed | Identity | Salts every delegation RNG and the sim seed. |
| `SimTickPipeline.Seed` / `SimSeed` | Scenario sim seed derived from the global seed | Regenerated | `SimTickPipeline` is constructed with `SimSeed` during setup. |
| `DelegationBridge.ConfigureSimulationMode` | Friendly/opposing mesh, default traits, autonomy, `TargetRegistry` friendly marks | Regenerated | Scenario setup, once, before the journal. Not a mid-session row unless a player path is later found; that path would become a journal row. |
| `DelegationOrchestrator.BeginExecution` | `SimulationPhase` Planning → Executing, one `ModeChangeRecord` | Regenerated | Part of setup. The phase is not stored separately. |
| `ScenarioAuthoringSession` | Editor document | Excluded | Authoring save/load is a different product (ADR-029). |

### Clocks

Two clocks move differently. The envelope stores both. Replay reproduces the split by
replaying acceleration. It does not assign `SimClock.SimTick`.

| Owner | State | Class | Why |
| --- | --- | --- | --- |
| Harness / world snapshot `SimTime` | Observed time. `BalticReplayHarness.HeadlessSnapshot` advances by `1.0` per observed step. Orders and engagements in the session use `(ulong)state.SimTime`. | Identity (the saved value) and Regenerated (the clock itself) | The saved observed tick is the resume position. The live clock is rebuilt. |
| `SimClock` | `SimTick`, `SimTime = SimTick * FixedDeltaSeconds` (default `1/60`), `IsPaused`, `AccelerationFactor` (1..256) | Regenerated, except the saved `SimTick` and factor are Identity for the position check | `Pause`, `Resume`, and `SetAccelerationFactor` have no order-log row. |
| `SimulationSession.PauseSim` | Forwards to `SimClock.Pause` | Journal | Player or watch auto-pause. Auto-pause also happens inside replay when the same watch event is reported, so the player pause and the explicit resume still need rows. |
| `SimulationSession.TryResumeSim(bool explicitOverride)` | Resume gated by `WatchAutoPauseGate.CanResume` | Journal | `explicitOverride` is an argument. `ResumeSim()` alone is the inner call and is not a separate row. |
| `SimulationSession.SetTimeAccelerationFactor` | Clamped factor | Journal | Each accepted change is a row. The factor multiplies pipeline steps (up to 256) per observed tick. |
| `C2ClockCommand.TryPause` / `TryResume` / `TrySetAcceleration` | Façade over the three session methods | Journal (capture site) | Record here, then forward. Do not add a second row for the session method the façade calls. |

`SimTickPipeline.TickOnce` does not step when `IsPaused` unless the mode is `HeadlessBatch`.
Interactive restore uses the interactive rule. Headless batch override is not a saved mode.

### RNG

| Owner | State | Class | Why |
| --- | --- | --- | --- |
| `ProjectAegis.Sim.Core.SeededRng` | Stateless `UnitFloat(seed, domain, entityId, simTick, drawIndex)` | Regenerated | No stream counter. Domains are `RngDomain`: Detection, Engage, AgentDecision, Logistics, Combat, MineHazard. Draw index and domain must stay stable or the value changes. |
| `ProjectAegis.Delegation.Decision.SeededRng` | Private `_state`, xorshift, constructed from `(globalSeed, agentSalt)` in `CreateAgent` | Regenerated | Salt is `DeterministicHash.OrdinalHash` of the agent id. There is no public getter. Replay from tick 0 with the same draw count and order restores `_state`. A checkpoint cannot jump the stream. No reflection and no private export (W0). |
| `MvpEngagementResolver._nextEngagementId` | Monotonic engagement id | Regenerated | Advances inside `Resolve` during replayed ticks. |

### Orders, approvals, and the player queue

| Owner | State | Class | Why |
| --- | --- | --- | --- |
| `HumanController` / `PlayerOrderExecutionQueue` | Orders waiting on `ExecuteSimTick` (comms delay) | Regenerated | Replayed `TryEnqueueHumanOrder` / `TryIssuePlayerCommand` re-enqueue them. |
| `DelegationBridge.TryEnqueueHumanOrder` | `EntityKey`, `OrderKind`, `simTime`, optional `RiskLevel` | Journal | `PlayerOrderRecord` stores kind, time, target, and `ExecuteSimTick`. It does not store `RiskLevel`. The journal must. |
| `DelegationBridge.TryIssuePlayerCommand` | `EntityKey`, `commandId`, `simTime` | Journal | CMD-31 wrapper. Keep `commandId`. The wrapper is not edited. |
| `DelegationBridge.TryEnqueueAttackOption` | `EntityKey`, `optionId`, the resolved salvo | Journal | On success it sets `SimulationSession.NextEngageSalvoOverride` then calls `TryEnqueueHumanOrder`. The salvo is not on `PlayerOrderRecord`. Record `optionId` and the salvo the resolver returned. |
| `SimulationSession.NextEngageSalvoOverride` | One-shot `int?`, consumed on the next engage template and then cleared | Regenerated | The journal row is the input. The field is the output. |
| `PendingApprovalQueue` | `_pending` and `_approved` | Regenerated | Agent ticks enqueue. Player approve/reject is the external input. |
| `DelegationOrchestrator.TryApprovePendingOrder` / `TryRejectPendingOrder` | `OrderId` | Journal | Neither call appends to `DecisionLog`. Dropping the row can still leave the log fingerprint and world hash unchanged. The journal digest must fail the restore. |
| `DelegationOrchestrator.ExecutedOrders` | Last tick's issued orders | Regenerated | Replaced every `Tick`. |
| `DelegationOrchestrator._orderIdSequence` | Next order id | Regenerated | |
| `DecisionLog` | Chronological rows and per-kind lists, `_sequenceId` | Checksum | SHA-256 via `OrderLogReplayFingerprint.ComputeSha256Hex`. Not an input stream. Save data must not become a new fingerprint input. |

`DecisionLog` already records outputs of replayed work: agent decisions, policy denials,
engagements and outcomes, controller changes, group detach/rejoin, magazines, contacts,
mission transitions, events, player orders, policy updates, mode changes, comms, fuel,
damage, and ordnance. Those lists come back because the calls and ticks run again. They
are not copied out of the save file into the log.

### Control override, groups, traits, and attention budget

| Owner | State | Class | Why |
| --- | --- | --- | --- |
| `DelegationBridge.TryTakeDirectControl` / `TryReleaseDirectControl` | `EntityKey`, `simTime` | Journal | Forwarded to `DelegationOrchestrator.TryTakeDirectControl` / `TryReleaseDirectControl`. |
| `OverrideService` + `ControllerSlot` | Active controller, `SuspendedAgent` | Regenerated | Take/release installs or restores `HumanController` / the suspended `AgentController`. |
| `DetachRejoinService` | Called from take/release when the unit has a `GroupTarget` parent | Regenerated | Not a separate row. |
| `GroupTarget` | `_members`, `PendingReplan` | Regenerated | Detach removes the member and marks replan. The log records `GroupMemberDetachRecord` / `GroupMemberRejoinRecord` as outputs. |
| `UnitTarget` | `IsDetachedFromGroup`, `DetachedFromGroupId` | Regenerated | |
| `AgentController` | `_traits`, `Autonomy`, `EffectivePolicy`, `PolicySnapshotId`, `PersonalitySlug`, `AttentionBudget`, `_nextDecisionSimTime`, `_issued` | Regenerated | Trait and budget changes that the player causes need a journal row. `_nextDecisionSimTime` has no setter for save. |
| `DelegationOrchestrator.TryRebindAgentTraits` | `AgentController`, `TraitVector` | Journal | No log append. Same digest rule as approve/reject. |
| `DelegationOrchestrator.TryRebriefAgent` | Preset name, `simTime` | Journal | Appends `PolicyUpdateRecord` (`personality.rebrief`) and also rebinds traits, slug, and `AttentionBudget`. The log row does not carry the budget. Replay the call. Hindsight re-register is a side effect, not authority. |
| `DelegationOrchestrator.TryAssignAgentController` | Target, the agent description needed to rebuild that agent, `isFriendly`, `simTime` | Journal | Appends `ControllerChangeRecord` and installs the agent. The log does not carry traits, seed salt, or autonomy. |
| `PolicySnapshotRegistry` | `_nextSnapshotId`, `_byTarget` | Regenerated | `Capture` runs again from doctrine override, rebrief, and ROE application. |
| `DoctrineOverrideCommand.TryApply` | `TargetId`, ROE label, `simTime` | Journal | Appends `PolicyUpdateRecord` and advances the snapshot id. Replaying the log row instead of the call does not recapture the snapshot. |
| `LoopPolicyGate` denials | No mutation | Excluded | A rejected call is not a journal row. The façade records only calls that run. |

Direct-control and group membership are restored because take/release is journaled, which
matches the owner decision that the override is restored. Group membership is not a
separate blob.

### Mission triggers and mission timeline

| Owner | State | Class | Why |
| --- | --- | --- | --- |
| `MissionRuntime` | `_nextIndex` over the ordered timeline | Regenerated | Owned by `BalticReplayHarness.RunCore`, not by `SimulationSession`. `Tick` appends mission and event rows. |
| `MissionContactTriggerRuntime` | `_fired` trigger ids | Regenerated | Same harness. `Evaluate` fires once per trigger on Unknown → Detected for a matching observer and target class, then `ApplyRoeToUnits` logs the ROE change. |
| `MissionContactTriggerRuntimeFactory` / `MissionRuntimeFactory` | Built from `ScenarioMissionTimeline` | Regenerated | The scenario content hash pins the trigger list and fire order. |

### Sensors and contacts

| Owner | State | Class | Why |
| --- | --- | --- | --- |
| `PdDetectionContactSimulator` | Detected set, destroyed and BDA-lost sets, `ContactTrack`s, classify/identify counters, primary-target flags, `LastDetectionHash`, LOS blocks, comms stale divisor | Regenerated | Harness-owned. Contact FSM states are `ContactLifecycleState`: Unknown, Detected, Classified, Identified, Lost. |
| `ScenarioContactSimulator` | Schedule-seeded contacts | Regenerated | Used when the policy has contact seeds. |
| `DatalinkSidePictureMerger` | Organic and shared pictures by observer/target | Regenerated | Present when datalink sharing is enabled on the policy. |
| `DetectionEnvironment`, EMCON, jammer lists | Scenario inputs to the detector | Identity via the scenario hash | Not a second copy in the envelope. |
| `ISimWorldSnapshot` contact counts and primary ids | Read model over the detector and killed set | Regenerated | `HeadlessSnapshot` is today's harness stand-in. The driver builds a fresh snapshot over the candidate world. |
| `CommsGridRegistry` | Membership, off-grid intervals, last on-grid report, `_nextIndex` | Regenerated | Built from the scenario policy and advanced during `Tick`. Off-grid blocks `TryTakeDirectControl` and new direct orders. The registry is not a blob. `LastCommsGridChanges` is the last step's output. |

Sensor state and comms-grid membership are authoritative, and they are still regenerated.
The save does not store track dictionaries or off-grid intervals.

### Attention and the watch

`AttentionCalculator.Evaluate` is a pure function of budget, member count, and observed
state. `AttentionDegradation` is not stored on the session. The budget lives on
`AgentController` and is regenerated from preset creation plus journaled rebind/rebrief.

| Owner | State | Class | Why |
| --- | --- | --- | --- |
| `WatchAttentionQueue` | Cards, ack and dismiss flags | Regenerated, then Discarded flags | Cards are rebuilt when replay reports the same contact and loss transitions. |
| `WatchAttentionQueue.TryAcknowledge` / `TryDismiss` | `eventId` | Journal, then Discarded | Presentation-only (`WatchAttentionCard` says they do not mutate sim policy). They are journaled anyway so `TryResumeSim(false)` sees the same `WatchAutoPauseGate` during replay. "Restorable" on the queue means the UI can undo a dismiss. It does not mean a load keeps the flag. |
| `WatchAutoPauseGate` | `LastPauseReason` | Regenerated | `ShouldAutoPause` runs again from replayed `ReportWatchAttention`. The gate does not own the clock. |
| `SimulationSession.ReportWatchAttention` / `ReportContactTransitions` / `ReportOwnSideLoss` | Enqueue and maybe pause | Regenerated | Sim and harness paths, not player rows. |

After checksums match, and before the swap, the driver clears `IsAcknowledged` and
`IsDismissed` on every card and pauses the candidate. That forced pause and the cleared
flags are host policy. They are not written back into the checksum.

### Engagement

| Owner | State | Class | Why |
| --- | --- | --- | --- |
| `SimTickPipeline` | `_pending`, `_lastProcessed`, `_lastResults`, `LastWorldHash`, `DetectionSubhash` | Regenerated (`LastWorldHash` is also a Checksum) | Pending engagements at the save tick come back because the ticks that queued them run again. |
| `MvpEngagementResolver` | Policy callback, domain validators, swarm sink, `_nextEngagementId` | Regenerated | Constructed with the candidate world. |
| `MagazineLedger` | Rounds and capacity by shooter and mount | Regenerated | `GetRounds` is a read. There is no hydrate API on the session. |
| `KilledTargetRegistry` | Killed ids and undrained labels | Regenerated | |
| `DictionaryEngageWorldQuery` / `EngageContext` | World query and default engage context on the session | Regenerated | Setup. |
| `IOrderSink` | Side effects of dispatched orders on that world | Regenerated | The candidate sink is swapped with the world. A bridge-only rebuild that leaves the host sink behind is not a successful restore. |

Mid-engagement save is allowed because this state is regenerated, not snapshotted.

### Logistics, damage, BDA, and other session outputs

These are optional objects `SimulationSession` holds when the scenario turned them on.
They are outputs of `Tick`. They are not envelope fields.

| Owner | State | Class |
| --- | --- | --- |
| `FuelTimelineTracker` | Ledger and last band per unit | Regenerated |
| `AirOpsStateMap`, `BoatOpsStateMap` | Per-unit ops FSM | Regenerated |
| `UnitReadinessMap` | Readiness seeded from the policy | Regenerated |
| `CatalogDamageHotTickTracker` | Hot-tick damage | Regenerated |
| `BdaContactLifecycleRegistry` | Promoted and pending Lost ids | Regenerated |
| `BalanceDriftAdvisoryConsumer` | Advisory consumer | Regenerated |
| `SimulationSession._lastOrdnanceBand` | Last band per unit | Regenerated |
| `SimulationSession._observedFireControlByTarget` and the two fire-control flags | Last observed fire-control track | Regenerated |
| `CombatWeaponFamilyId` | Family id string on the session | Regenerated from setup |

### Bridge-private timelines

`DelegationBridge` holds `_commsTimeline`, `_spoofTimeline`, `_fuelTimeline`, and
`_lastFuelSimTime`. `Tick` emits transitions from them before the orchestrator runs.
They are **Regenerated** by replaying ticks on a fresh bridge. They are not exported.
W0 forbids a private-field read on the bridge. `CurrentCommsState` is the public read
of the comms timeline and is used when `TryEnqueueHumanOrder` computes `ExecuteSimTick`.
Replay reproduces that delay by rebuilding the timeline, then applying the journaled
order at the same observed time.

`_nonEngageOrdersCache` is per-tick scratch. It is not state.

### Checksums and stores that must not hydrate

| Owner | What is stored | Class |
| --- | --- | --- |
| `OrderLogReplayFingerprint` | SHA-256 hex of `DecisionLog.ComputeFingerprint()` | Checksum |
| `SimTickPipeline.LastWorldHash` | Pipeline world hash at the save tick | Checksum |
| `ReplayCheckpointStore` | Rows with `SimTick <=` save tick. The harness records `SimWorldHash.Combine(LastWorldHash, detectionHash, 0)`, the **raw** `ComputeFingerprint()` text (not the SHA-256), and the last sequence id, every `ScenarioReplaySettings.CheckpointIntervalTicks` (default 300) | Checksum |
| Journal digest | Digest of every journal row in order | Checksum, and part of the envelope hash |

`DecisionLog` and `LastWorldHash` do not move for trait rebind, approve/reject, salvo
override, pause, resume, or acceleration. Those rows live in the journal digest. A
candidate that matches the log fingerprint and the world hash, and mismatches the digest,
is rejected.

### Excluded optional state

| Owner | Why excluded |
| --- | --- |
| `HindsightIntegration` | Optional HTTP sidecar. `DecisionLog` documents that the hook does not affect fingerprints. Memory is not authority and is not replayed. A rebrief may re-register an agent as a side effect. The save does not read the sidecar. |
| `AgentExperienceBlob.Metrics` | Owned by `AgentController`. Empty unless a writer fills it. Not an envelope field. If a writer mutates it outside the journal, that writer fails the save. |
| `TrustSignal` list | Produced by `FinalizeScenario`, not by a mid-session save. |
| `AttachReplayViewer` | The restored bridge is constructed with `AttachReplayViewer = false`. Observer attachment is not resumed. |
| Presentation projections, toast binder, map hosts | Rebuilt after a successful swap through existing projection inputs (ADR-010 §2–3, ADR-007, ADR-001). `MapPlaceholderPanelHost` and `GlobeMapProductHost` are not edited and are not serialized. |
| Multiplayer, hot-seat, lockstep peer state | A later ADR. This envelope has no peer, slot-side, or fog-of-war-per-side fields. |

---

## Save envelope

One side file per occupied slot. Logical fields only. Ordering inside the file, encodings,
and length limits are unspecified. A parser that accepts a truncated stream, an unknown
version, or a hash mismatch does not implement this contract.

The envelope hash covers the identity block, the position block, the journal, and the
checksum block. The journal digest is inside that hash.

### 1. Identity

| Field | Rule |
| --- | --- |
| Schema version | Integer. Unknown version is rejected before construction. |
| Build id | Exact match to the loading process. |
| Scenario id | Exact match. |
| Scenario content hash | Exact match to the bytes the loader would set up. |
| Catalog snapshot id | Exact match to `DbSnapshotStore` for the bound snapshot. |
| Whole-catalog hash | Exact match. Must cover sensors, weapons, mounts, magazines, mobility, damage, and comms. Must not be `CatalogSnapshotHasher` alone. Empty `content_hash_sha256` is a mismatch, not a wildcard. |
| Policy ids and hashes | Exact match for every bound policy, including the scenario policy id. |
| Global seed | Exact match. |

### 2. Position

| Field | Rule |
| --- | --- |
| Observed tick | `state.SimTime` at save. |
| Pipeline tick | `SimClock.SimTick` at save. |
| Last sequence id | Last `DecisionLog` sequence id (0 if the log is empty). |

Both ticks are required. A file with only one of them is truncated.

### 3. Input journal

An ordered list. Each row is the façade call, the observed tick (and pipeline tick when
the call has no sim-time argument), and the arguments in the inventory. Order is the
order the calls ran. The digest is over that sequence, not over a set.

Rows:

1. `TryTakeDirectControl`
2. `TryReleaseDirectControl`
3. `TryEnqueueHumanOrder` (includes `RiskLevel`)
4. `TryIssuePlayerCommand` (includes `commandId`)
5. `TryEnqueueAttackOption` (includes `optionId` and resolved salvo)
6. `TryApprovePendingOrder`
7. `TryRejectPendingOrder`
8. `TryRebindAgentTraits`
9. `TryRebriefAgent`
10. `TryAssignAgentController`
11. `DoctrineOverrideCommand.TryApply`
12. `PauseSim` (including a player pause; auto-pause from a replayed watch event is not a second row)
13. `TryResumeSim` (includes `explicitOverride`)
14. `SetTimeAccelerationFactor`
15. `WatchAttentionQueue.TryAcknowledge`
16. `WatchAttentionQueue.TryDismiss`

A new public command that changes authority-bearing state joins this list before it
ships. A command that bypasses the journal fails the save. Denied calls (gate rejected,
replay viewer attached, unknown unit) are not rows.

The journal is recorded, then the existing method runs unchanged. `DelegationBridge` and
`SimulationSession` gain no members.

### 4. Checksums

| Field | Rule |
| --- | --- |
| Journal digest | Matches the ordered rows. Included in the envelope hash. |
| Order-log fingerprint | `OrderLogReplayFingerprint.ComputeSha256Hex` at the save tick. |
| `LastWorldHash` | `SimTickPipeline.LastWorldHash` at the save tick. |
| Checkpoint rows | Copy of `ReplayCheckpointStore` rows with `SimTick` at or before the save tick, in tick order. Raw fingerprint text stays raw. These rows are compared. They are not loaded into the session as state. |

### Slots

| Slot | Write rule |
| --- | --- |
| `manual-1`, `manual-2`, `manual-3` | A successful manual save replaces that slot only. |
| `autosave` | A successful autosave replaces this slot only. |

A failed save, including "mutation with no journal row", writes no file and does not
replace a previous good slot. The rolling autosave does not keep older autosaves.
Manual slots are not deleted by autosave. Overwrite confirmation is presentation and is
not specified here. **When** autosave runs is not specified here.

Loading a slot does not delete it.

---

## Restore order

The live host world, its snapshot, its order sink, its bridge, its RNG, its queues, and
its log stay in place until step 8. Any failure before step 8 discards the candidate, if
one was built, and leaves that live set unchanged (RPL-31).

1. **Parse.** If the file is truncated, undecodable, or a different schema version, reject.
   Do not construct a world, a bridge, or a session.
2. **Integrity.** Recompute the envelope hash and the journal digest. Mismatch rejects.
   Do not construct a candidate.
3. **Compatibility.** Compare build id, scenario id, scenario content hash, catalog
   snapshot id, whole-catalog hash, every policy id and hash, and the global seed.
   Any mismatch rejects. Do not construct a candidate. Do not call `CatalogWriteGate`.
4. **Shape.** Both clocks, the last sequence id, and checkpoint rows limited to the save
   tick must be present. A network or multiplayer field rejects. Do not construct a
   candidate.
5. **Build the candidate, off to the side.** One replayable sim world, the
   `ISimWorldSnapshot` that world exposes, the `IOrderSink` that applies that world's
   order side effects, and a fresh `DelegationBridge` bound to that snapshot and sink
   with `AttachReplayViewer = false`. Scenario mesh setup and `BeginExecution` run here,
   through the shared stepper DRG-349 extracts from `BalticReplayHarness.RunCore`. This
   contract does not authorize that extraction.
6. **Replay the journal from tick 0** through the shared stepper up to the saved observed
   tick and the saved pipeline tick. Each row calls the same public method with the
   recorded arguments. The stepper advances the candidate world (positions, alive,
   contacts, magazines, sink side effects), not only hashes. Acceleration rows change how
   many pipeline steps run per observed tick. Watch ack rows run so a later
   `TryResumeSim(false)` is gated the way it was in the original session.
7. **Compare.** Journal digest, order-log SHA-256, `LastWorldHash`, and checkpoint rows
   must match the envelope. A digest miss fails even when the log fingerprint and the
   world hash match. A replay exception fails. On failure, discard the whole candidate.
8. **Pause, clear watch flags, swap.** Only after step 7 matches: clear acknowledgement
   and dismiss flags, pause the candidate (`paused-on-load`), then swap the candidate
   world, snapshot, order sink, and bridge into the host as one unit. Do not swap a
   bridge while leaving the previous world or sink installed.
9. **Publish.** The host re-publishes snapshots and projections through the existing
   inputs. Map hosts are not edited. The save/load UI remains a command-façade client.

Step 8's pause and flag clear are not re-checked against the saved fingerprint. The
fingerprint was taken at step 7, before those host-policy mutations.

There is no step that assigns `SeededRng._state`, copies `ReplayCheckpoint` into
magazines or contacts, or replays `DecisionLog` rows as commands.

---

## Failure rules

| Condition | When | Active session |
| --- | --- | --- |
| Truncated, undecodable, unknown schema | Before construction | Unchanged. No slot write on the save path. |
| Envelope hash or journal digest mismatch | Before construction | Unchanged |
| Build, catalog, scenario, policy, or seed mismatch | Before construction | Unchanged |
| Missing clock, missing sequence id, extra network field | Before construction | Unchanged |
| Replay exception, digest miss, fingerprint miss, world-hash miss, checkpoint miss | After the candidate exists, before the swap | Unchanged. Candidate discarded. |
| Unjournaled authority mutation discovered at save time | Before the envelope is written | Unchanged. Previous slot file kept. |

"Unchanged" means the active world, snapshot, sink, bridge, RNG, queues, and log compare
equal before and after the failed attempt.

---

## Out of scope for this contract

- Code, tests, and the shared-stepper extraction (DRG-349).
- Edits to `DelegationBridge` hotpath methods, `SimulationSession`, `CatalogWriteGate`
  write paths, `MapPlaceholderPanelHost`, `GlobeMapProductHost`, or CMD-31/32/34 seams.
- A product waiver (W1/W2), private-field export, or skip-to-checkpoint.
- Re-bless of Baltic v2 hash `17144800277401907079` or any `baltic-v3-*` golden.
- GDD wording changes for `simulation-core-time.md` and `order-log-replay.md`. ADR-029
  notes those amendments. They are not this file.
- Autosave interval, on-disk schema, whole-catalog hash function, build-id token, and
  scenario-hash function. Coverage and reject behavior above are binding. The algorithms
  are not selected here.
- Multiplayer and hot-seat.

DRG-349 proves this contract: uninterrupted N+M ticks equals save at N, restore, then M
ticks, including pending engagements, delayed orders, triggers, acceleration greater than
1, and a trait rebind or approve/reject immediately before save. Corrupt, truncated,
wrong-build, and wrong-catalog fixtures must leave the active session unchanged.
