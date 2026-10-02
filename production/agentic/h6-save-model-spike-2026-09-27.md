# DRG-330 — H6 read-only spike: replay-to-tick resume + mutable session state

| Field | Value |
| --- | --- |
| Linear | [DRG-330](https://linear.app/drgamtd-workspace/issue/DRG-330/h6-read-only-spike-replay-to-tick-resume-feasibility-mutable-session) |
| Parent | DRG-325 (H6 Save-Load / Multiplayer). Feeds DRG-329 (ADR story — this file does **not** write or number an ADR). Product questions go to DRG-328. |
| Main SHA | `1902dc1299678ea17f75014406fd9c616e5576bd` (2026-09-27, fetched `origin/main` before branch) |
| Date | 2026-09-27 |
| Kind | **Read-only spike, no code.** Analysis only. No `src/`, `unity/`, `tests/`, ADR, baseline, or golden edits. |

Every code claim below was read at that SHA. **Verified** means the statement is in the cited lines. **Unverified / inferred** means a conclusion that follows from those lines but was not executed (no `dotnet test`, no replay run).

Presentation boundary, when a later save UI is discussed: **ADR-010 §2–3**, **ADR-007**, **ADR-001**. Git **ADR-018** is sensor side-picture / datalink and is not the presentation wall.

Frozen hub: `docs/architecture/adr-simulation-session-frozen-hub-spirit1-2026-06-20.md` treats `SimulationSession` as frozen through Release v1.0 unless a scoped story authorizes hub edits. This spike does not authorize that.

---

## 1. Question

Can a game-state save/load be built with **zero edits** to `DelegationBridge.Tick` / hotpath and to `SimulationSession`, by persisting `(scenario id, seed, catalog snapshot hash, human/player order log)` and restoring via deterministic replay-to-tick through **existing public APIs**?

**Verdict for option (a): Feasible-with-conditions.** Detail in §4. Options (b) and (c) are in §3.

---

## 2. Mutable session-state inventory

"Reproducible from (scenario, seed, order log)?" means: a fresh process, same scenario policy id, same global seed, same catalog bytes, plus re-application of human/player rows already stored on `DecisionLog`, reaches the same field **without** new writers inside `SimulationSession` or `DelegationBridge.Tick`.

The live order log **is** `DecisionLog`. `DelegationOrchestrator.OrderLog` returns it (`src/ProjectAegis.Delegation/Orchestration/DelegationOrchestrator.cs:78`). Checkpoints store `DecisionLog.ComputeFingerprint()` text, not a separate human-only log (`src/ProjectAegis.Delegation.UnityAdapter/Baltic/BalticReplayHarness.cs:478-483`).

| Field / member | Owning type + path:line | Set by | Reproducible from (scenario, seed, order log)? | Notes |
| --- | --- | --- | --- | --- |
| Sim clock pause (`IsPaused`) | `SimClock` `src/ProjectAegis.Sim/Time/SimClock.cs:25-30`; surfaced as `SimulationSession.IsSimPaused` / `PauseSim` / `ResumeSim` `src/ProjectAegis.Delegation/Orchestration/SimulationSession.cs:136-140` | UI (interactive session methods). Sim does not set it. Headless harness does not call it. | **No** | Not an order-log row. `Session.Tick` skips `Sim.TickOnce` while paused unless headless override (`SimulationSession.cs:247-251`). `TickHeadless` passes `headlessOverride: true` and still runs the pipeline (`SimulationSession.cs:225-239`). |
| Time acceleration (`AccelerationFactor`, 1..256) | `SimClock` `src/ProjectAegis.Sim/Time/SimClock.cs:10-11,28-36`; `SimulationSession.SetTimeAccelerationFactor` `SimulationSession.cs:208-210` | UI via session. Default 1. | **No** | `RunExecutingTick` runs `extraSteps = AccelerationFactor - 1` additional `Sim.TickOnce` calls after the first (`SimulationSession.cs:317-327`). `SimTickPipeline.TickOnce(RealTime)` itself steps once; acceleration inside the pipeline applies only for `TimeCompressionMode.Accelerated` (`src/ProjectAegis.Sim/Core/SimTickPipeline.cs:41-52`). Interactive session applies acceleration **outside** that mode flag. |
| Two clocks (snapshot sim time vs pipeline clock) | `BalticReplayHarness.HeadlessSnapshot.SimTime` advanced by `Advance(1.0)` (`BalticReplayHarness.cs:340-341,650`); `SimClock.SimTick` / `SimTime = SimTick * FixedDeltaSeconds` (`SimClock.cs:20-22`, default Δt `1/60` at `SimTickPipeline.cs:16`) | Harness sets observed `SimTime`. Pipeline clock advances inside `TickOnce`. | **Partial** | Order/engagement sim ticks in the session are `(ulong)state.SimTime` (`SimulationSession.cs:245`), **not** `SimClock.SimTick`. A save of snapshot tick does not capture pipeline tick count when acceleration extra-steps. **Verified** split. Whether golden files' `WORLD_HASH` tracks pipeline hash vs harness combine: harness combines `Session.Sim.LastWorldHash` with detection hash (`BalticReplayHarness.cs:486-488`). |
| `WatchAttentionQueue` cards, ack, dismiss | `src/ProjectAegis.Delegation/Watch/WatchAttentionQueue.cs:9-14,65-119`; held by `SimulationSession.WatchQueue` `SimulationSession.cs:130` | Enqueue: sim/session (`ReportWatchAttention`, `ReportContactTransitions`, `ReportOwnSideLoss` `SimulationSession.cs:154-206`). Ack/dismiss: UI (`TryAcknowledge`, `TryDismiss`). | **Partial** | Comment says ack/dismiss are presentation-only and restorable (`WatchAttentionQueue.cs:6-7`). The only restore method is in-memory `TryRestore` (`WatchAttentionQueue.cs:104-119`). No serializer. Baltic harness appends contact transitions to the order log (`BalticReplayHarness.cs:445`) and does **not** call `ReportContactTransitions`. Re-calling that public session method could rebuild cards from transitions; ack/dismiss are not in the log. |
| `WatchAutoPauseGate` (`LastPauseReason`, resume gate) | `src/ProjectAegis.Delegation/Watch/WatchAutoPauseGate.cs:9-67`; `SimulationSession.WatchPauseGate` `SimulationSession.cs:132-134` | Sim: `ShouldAutoPause` on pause-class enqueue, then `PauseSim` (`SimulationSession.cs:162-165`). UI: `TryResumeSim(explicitOverride)` (`SimulationSession.cs:142-151`). | **Partial** | Reason is derivable if pause-class events are re-enqueued in order. Explicit force-resume is not logged. `ClearReason` runs only after a successful resume (`SimulationSession.cs:150`). Gate does not own the clock (`WatchAutoPauseGate.cs:6-7`). |
| Direct-control slot (Human vs suspended agent, detach/rejoin) | `DelegationOrchestrator.TryTakeDirectControl` / `TryReleaseDirectControl` `DelegationOrchestrator.cs:202-270`; bridge wrappers `src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs:282-311` | UI. Both refuse when `AttachReplayViewer` (`DelegationBridge.cs:284-286`, `Orchestrator` `202-206`). | **Partial** | Success appends `ControllerChangeRecord` (`DelegationOrchestrator.cs:239-241,268-269`) plus detach/rejoin rows (`226`, `260`). A driver can re-call the public Try* methods at the logged `simTime` on a **fresh** session. There is no "apply this ControllerChangeRecord" API. Re-take while already human is a no-op that **keeps** the existing `HumanController` queue (`DelegationOrchestrator.cs:209-217`). |
| Queued human orders (`PlayerOrderExecutionQueue`) | `HumanController` `src/ProjectAegis.Delegation/Controllers/HumanController.cs:6-17`; drained in `DelegationOrchestrator.Tick` `DelegationOrchestrator.cs:385-386` | UI via `DelegationBridge.TryEnqueueHumanOrder` `DelegationBridge.cs:167-204` | **Partial** | Log row is `PlayerOrderRecord` (unit, kind, sim time, `ExecuteSimTick`) `src/ProjectAegis.Delegation/Decision/PlayerOrderRecord.cs:6-15`. Enqueue recomputes execute tick from current comms (`DelegationBridge.cs:186-195`). On a fresh session, re-calling `TryEnqueueHumanOrder` before the same tick rebuilds the queue **and** appends a new log row (correct for rebuild; duplicate if applied onto a copied log). Fails unless the slot's active controller is already `HumanController` (`DelegationBridge.cs:178-181`). `RiskLevel` is classified at enqueue and is **not** a field on `PlayerOrderRecord`. |
| Player commands (CMD-31) | `DelegationBridge.TryIssuePlayerCommand` `DelegationBridge.cs:206-215` → `C2PlayerCommandBridge.TryIssue` `src/ProjectAegis.Delegation.UnityAdapter/Bridge/C2PlayerCommandBridge.cs:21-66` | UI. Analysis only. This spike does not modify CMD-31. | **Partial** | Resolves `commandId` → `OrderKind`, then `TryEnqueueHumanOrder`. Does not change the control slot (`C2PlayerCommandBridge.cs:18-19`). Failure reasons include `REPLAY_ATTACHED`, `NOT_HUMAN_CONTROL` (`C2PlayerCommandBridge.cs:12-15,42-57`). The log stores `OrderKind`, not `commandId` (`PlayerOrderRecord.cs:6-13`). **Unverified:** whether two command ids share one kind (would make the id unrecoverable from the log). |
| Attack-menu salvo override | `SimulationSession.NextEngageSalvoOverride` `SimulationSession.cs:663`; set from `TryEnqueueAttackOption` `DelegationBridge.cs:274-277`; consumed once in `PrimeEngageWorld` `SimulationSession.cs:699-700` | UI (attack menu). | **No** | One-shot session field. `PlayerOrderRecord` has no salvo size. A replay that only re-enqueues `OrderKind` drops the override. |
| Orchestrator / agent state (phase, agents, policy snapshots, executed orders, pending approvals, trust) | `DelegationOrchestrator` `DelegationOrchestrator.cs:62-104,108-161,187-200,300-338,349-391`; `PendingApprovalQueue` `src/ProjectAegis.Delegation/Orchestration/PendingApprovalQueue.cs:11-71`; `AgentController` `src/ProjectAegis.Delegation/Controllers/AgentController.cs:15-54` | Agents and initial policy: scenario + harness/UI setup. Per-tick decisions: sim. ROE rebind: scenario triggers via `ApplyRoeToUnits` (logged `PolicyUpdateRecord`, `DelegationOrchestrator.cs:324-331`). Approve/reject: UI (`TryApprovePendingOrder` `DelegationOrchestrator.cs:94-101`). Trait rebind: `TryRebindAgentTraits` `DelegationOrchestrator.cs:394-406`. | **Partial** | Agent-vs-agent with no human input is what `BalticReplayHarness.Run` already re-sims from `(seed, scenarioPolicyId, ticks)` (`BalticReplayHarness.cs:88-97,172-176,301`). Pending approve/reject mutates queues and is **not** appended to `DecisionLog` (`PendingApprovalQueue.cs:41-71` has no log call). Trait rebind writes traits on the agent (`agent.RebindTraits`) with no order-log append in the method body (`DelegationOrchestrator.cs:394-405`). `ExecutedOrders` is per-tick output, not save state. |
| Decision RNG stream (delegation) | `ProjectAegis.Delegation.Decision.SeededRng` `src/ProjectAegis.Delegation/Decision/SeededRng.cs:3-22`; constructed in `CreateAgent` `DelegationOrchestrator.cs:115-116`; held as `AgentController.Rng` `AgentController.cs:43` | Sim (each `NextUnit` advances private `_state`). | **Partial** | Stream is a pure function of `(globalSeed, agentSalt)` **plus draw count/order**. Salt is `DeterministicHash.OrdinalHash(id.Value)` (`DelegationOrchestrator.cs:115`). No public getter for `_state`. Replay from tick 0 with the same draw sequence restores it. A checkpoint cannot jump the stream. GDD asks to restore "RNG stream counters" (`design/gdd/simulation-core-time.md:126`); that counter is not on `ReplayCheckpoint`. |
| Sim RNG (stateless) | `ProjectAegis.Sim.Core.SeededRng.UnitFloat` `src/ProjectAegis.Sim/Core/SeededRng.cs:4-15` | Sim. Pure function of `(seed, domain, entityId, simTick, drawIndex)`. | **Yes** (given seed + tick + draw index) | No stream counter to persist. Documented as stateless in `docs/engineering/determinism-and-replay.md:63-70`. Draw index must stay stable or the value changes. |
| Checkpoint store | `ReplayCheckpointStore` `src/ProjectAegis.Delegation/Replay/ReplayCheckpointStore.cs:5-36`; record `ReplayCheckpoint` `src/ProjectAegis.Delegation/Replay/ReplayCheckpoint.cs:4-8`; interval `ScenarioReplaySettings` `src/ProjectAegis.Sim/Scenario/ScenarioReplaySettings.cs:4-6` (default 300) | Sim/harness: `Record` when `simTick % interval == 0` (`BalticReplayHarness.cs:169-170,473-483`). Not a field of `SimulationSession`. | **Partial** | Store is append-only hashes: `(SimTick, WorldHash, LogFingerprint, LastSequenceId)`. `FindAtOrBefore` returns that record. **No hydrate, no world blob, no RNG.** GDD text says "world-state snapshot" (`design/gdd/order-log-replay.md:80`). Implementation is a verify boundary (`ReplayCheckpoint.cs:3`). Fingerprint stored at the checkpoint is raw `ComputeFingerprint()` text, not the SHA-256 (`BalticReplayHarness.cs:481` vs SHA at `504` via `OrderLogReplayFingerprint.cs:10-14`). |
| Catalog bindings (reader, weapon family, readiness, magazines, engage context) | `SimulationSession.CatalogReader` / `CombatWeaponFamilyId` / `UnitReadiness` / `Magazines` / `DefaultEngageContext` `SimulationSession.cs:624-649`; bound from bridge ctor `DelegationBridge.cs:48-62` and harness `BalticReplayHarness.cs:135-136,182-194` | Scenario + catalog at construction. Magazines then mutate on fire (`SimulationSession.cs:550-559`). | **Partial** | Same scenario id + same catalog **bytes** + re-sim reproduces bindings. `CatalogSnapshotHasher.ComputeSha256Hex` hashes **sorted sensor rows only** (`src/ProjectAegis.Data/Snapshots/CatalogSnapshotHasher.cs:8-38`). **Verified:** it does not cover weapons, mounts, damage, or mobility. A sensor-only hash match does not prove the catalog the session will read. |
| Withdraw trials | `SimulationSession.CatalogWithdrawTrials` `SimulationSession.cs:651-661`; refreshed from hot-tick damage `SimulationSession.cs:472-478`; initial bind in harness `BalticReplayHarness.cs:191-193` | Sim (catalog damage hot tick) and scenario/catalog at start. | **Partial** | Re-sim from start reproduces them. Damage changes are appended to the decision log (`AppendPlatformDamageChange`, `SimulationSession.cs:475`). There is no public "set trials from log" API; `BindCatalogWithdrawTrials` replaces the list from a caller-supplied collection (`SimulationSession.cs:660-661`). |
| Other mutable sim state a four-field save would miss | `MagazineLedger` `src/ProjectAegis.Sim/Engage/MagazineLedger.cs:4,33`; `KilledTargets`, `FuelTimeline`, `AirOps`, `BoatOps`, `BdaContactLifecycleRegistry`, `BalanceDriftConsumer`, `_lastOrdnanceBand` `SimulationSession.cs:626-658,630`; bridge-private `_commsTimeline`, `_spoofTimeline`, `_fuelTimeline`, `_lastFuelSimTime` `DelegationBridge.cs:27-31` | Sim during `Tick` / session engage phase. Fuel delta uses prior `SimTime` (`DelegationBridge.cs:30-31`). | **Partial** | These are outputs of re-sim, not inputs. They are not readable as a complete snapshot through one public DTO. Private bridge timelines are not on `SimulationSession`. **Unverified:** full field list of `FuelTimelineTracker` / detection simulators (`PdDetectionContactSimulator` is local to the harness, `BalticReplayHarness.cs:140-156`). |

UI that later displays this state stays a client: read via snapshot / projection / `*Bridge`, write via command façade (ADR-010 §2–3, ADR-007, ADR-001). Watch ack/dismiss is already documented as presentation (`WatchAttentionQueue.cs:6-7`) and must not become sim authority.

---

## 3. Save-model options

### (a) Replay-to-tick

Re-run from tick 0 through tick T. Persist scenario id, seed, catalog identity, and the human inputs required to diverge from pure agent-vs-agent. Compare `DecisionLog` fingerprint and world hash to the save.

| | |
| --- | --- |
| Symbols a driver would **call** (not edit) | `DelegationBridge` ctor `DelegationBridge.cs:33-63`; `Registry.RegisterUnit`; `Orchestrator.CreateAgent` `DelegationOrchestrator.cs:108-124`; `AssignAgentToTarget` `142-161`; `BeginExecution` `187-200` or `DelegationBridge.BeginExecution` `98`; `ApplyRoeToUnits` `300-338`; `TryTakeDirectControl` / `TryReleaseDirectControl` `DelegationBridge.cs:282-311`; `TryEnqueueHumanOrder` `167-204`; `TryIssuePlayerCommand` `210-215` (CMD-31 wrapper — call only, do not modify); `TryApprovePendingOrder` / `TryRejectPendingOrder` `DelegationOrchestrator.cs:94-101`; `Session.PauseSim` / `ResumeSim` / `TryResumeSim` / `SetTimeAccelerationFactor` / `ReportContactTransitions` `SimulationSession.cs:136-210`; **`DelegationBridge.Tick`** `130-165` **as a call**; `DecisionLog.ComputeFingerprint`; `OrderLogReplayFingerprint.ComputeSha256Hex` `OrderLogReplayFingerprint.cs:10-14`; `SimWorldHash.Combine` `src/ProjectAegis.Sim/Core/SimWorldHash.cs:14-18`; `ReplayCheckpointStore.FindAtOrBefore` `ReplayCheckpointStore.cs:20-36`. Existing full re-sim without human orders: `BalticReplayHarness.Run` `BalticReplayHarness.cs:88-97`. |
| Hard-no touch? | **No, if** the driver is new code that only calls those methods. **Yes, if** someone adds an order-log parameter by editing `DelegationBridge.Tick` or `SimulationSession.Tick` / `RunExecutingTick`. `BalticReplayHarness.Run` has no order-log argument (`88-97`); teaching it to inject orders is a harness change, not a SimulationSession or Tick edit. Harness change is outside this spike's hard-no list and outside this docs-only surface. |
| Determinism risk | **Medium.** Agent-vs-agent path is the existing golden contract (`docs/engineering/determinism-and-replay.md:20-35,148-153`). Human path is deterministic only if every input that changed the run is re-applied in order **before** `Tick`, on a fresh session, with `AttachReplayViewer == false` (enqueue and direct-control return false when it is set, `DelegationBridge.cs:173-176,284-286`). Missing pause, acceleration, salvo override, approval, or trait rebind desyncs fingerprint and/or `LastWorldHash`. Catalog hash false match (sensor-only hasher) desyncs engage/damage. Duplicating harness setup in a second driver can drift from `RunCore` (`BalticReplayHarness.cs:123-484`) even when both call public APIs. |

### (b) Full state snapshot

Serialize live objects and restore them in place.

| | |
| --- | --- |
| Symbols it would read/write | Would need private `SeededRng._state` `src/ProjectAegis.Delegation/Decision/SeededRng.cs:5`; `SimClock` pause/tick/acceleration `src/ProjectAegis.Sim/Time/SimClock.cs:20-28`; magazine rounds; killed-target registry; fuel/comms/spoof cursors; air/boat FSMs; BDA registry; withdraw trials; watch cards; human queue; pending-approval lists; agent traits and `_nextDecisionSimTime` `src/ProjectAegis.Delegation/Controllers/AgentController.cs:18`. Public reads exist for some (`Magazines.GetRounds` `MagazineLedger.cs:33`, `WatchQueue.Cards`, `PendingApprovals`, `LastWorldHash` `SimTickPipeline.cs:26`) and not for others (delegation RNG state, bridge-private timelines). |
| Hard-no touch? | **Yes if** restore setters or a load method are added on `SimulationSession` or inside `DelegationBridge.Tick`. **No** public load API exists today. An external snapshot that only copies public getters is incomplete, so a faithful (b) does not stay zero-touch on the frozen hub. |
| Determinism risk | **High** until every field in §2 is in the blob. Missed field looks like a successful load and then diverges. Snapshot format must stay **out** of `DecisionLog.ComputeFingerprint` and `SimWorldHash` or Baltic v2 goldens move. ADR-004 pipeline order (`docs/architecture/adr-004-tick-pipeline-order.md`) is the hash input; a side file is not. |

### (c) Hybrid checkpoint + log tail

GDD: re-sim from the nearest world snapshot plus the log tail (`design/gdd/order-log-replay.md:80`; `design/gdd/simulation-core-time.md:126`).

| | |
| --- | --- |
| Symbols it would read/write | Today: `ReplayCheckpointStore.Record` / `FindAtOrBefore` and `ReplayCheckpoint` fields only (`ReplayCheckpoint.cs:4-8`). That is a hash boundary. A real hybrid needs (b)'s world blob at interval ticks **plus** (a)'s tail of human inputs after `LastSequenceId`. `ScenarioReplaySettings.CheckpointIntervalTicks` default 300 (`ScenarioReplaySettings.cs:4`). Engagement-triggered checkpoints are deferred (`design/gdd/simulation-core-time.md:201`). |
| Hard-no touch? | **Verification-only hybrid (hash check after full re-sim) is already implemented** inside `BalticReplayHarness.RunCore` and does not restore state. **Skip-to-checkpoint hybrid** needs a world restore. That is option (b) at checkpoint boundaries. Same hard-no: no hydrate on `SimulationSession`. |
| Determinism risk | Using the checkpoint hash as a skip license without a world blob is **unsafe**: `FindAtOrBefore` cannot rebuild magazines, RNG, or contacts. Re-sim from 0 and **compare** to the checkpoint (current harness) is safe and does not shorten the run. Log append during replay is forbidden by the GDD (`design/gdd/order-log-replay.md:97`); `AttachReplayViewer` blocks human enqueue but does not by itself make the session read-only (`docs/engineering/replay-checkpoint-observer-runtime.md:153` — `DoctrineOverrideCommand` can still append a `PolicyUpdateRecord`). **Verified** for the enqueue guard in code; the observer-doc sentence was read, not re-tested. |

---

## 4. Zero-touch verdict for (a)

**Feasible-with-conditions.**

A new replay driver (new type, not an edit to `SimulationSession` or `DelegationBridge.Tick`) can restore a tick-T session by constructing a fresh bridge and calling existing public APIs through tick T. Calling `Tick` is not an edit of `Tick`. `SimulationSession` stays frozen (`docs/architecture/adr-simulation-session-frozen-hub-spirit1-2026-06-20.md`).

### Public APIs the driver would call

1. **Construct:** `new DelegationBridge(globalSeed, policyEvaluator: null, mvpEngagement: true, scenarioPolicyId, catalog)` (`DelegationBridge.cs:33-63`). Catalog argument must be the **bytes** behind the save, not the hash alone.
2. **Scenario mesh (same calls the harness makes):** `Registry.RegisterUnit`, `Orchestrator.CreateAgent`, `AssignAgentToTarget`, `BeginExecution`, and, when the scenario timeline says so, `ApplyRoeToUnits` plus `Orchestrator.OrderLog.Append*` for mission/contact rows. Those appends are public on `DecisionLog` via `OrderLog` (`DelegationOrchestrator.cs:78`). They live today inside private `RunCore` (`BalticReplayHarness.cs:338-447`), which a save driver cannot pass human orders into.
3. **Before each `Tick`, re-apply human inputs** whose log time is this tick: `TryTakeDirectControl` / `TryReleaseDirectControl`, then `TryEnqueueHumanOrder` or `TryIssuePlayerCommand`, then `TryApprovePendingOrder` / `TryRejectPendingOrder` if product persists them. Clock and watch: `Session.SetTimeAccelerationFactor`, `PauseSim` / `TryResumeSim`, `ReportContactTransitions`, `WatchQueue.TryAcknowledge` / `TryDismiss`.
4. **Advance:** `bridge.Tick(snapshot, sink)` (`DelegationBridge.cs:130`). Headless batch that must ignore pause can call `Session.TickHeadless` (`SimulationSession.cs:231`) **instead of** going through bridge pause semantics. That is still a call, not an edit. Do not change either method.
5. **Verify:** `DecisionLog.ComputeFingerprint()` and `OrderLogReplayFingerprint.ComputeSha256Hex` (`OrderLogReplayFingerprint.cs:10-14`); `Session.Sim.LastWorldHash` (`SimTickPipeline.cs:26`); optional `ReplayCheckpointStore.FindAtOrBefore`.

`BalticReplayHarness.Run(seed, scenarioPolicyId, ticks, ...)` (`BalticReplayHarness.cs:88-97`) is the existing public re-sim for **agent-vs-agent only**. It does not accept an order log, a stop-at-human-command list, or a resume tick other than "run N ticks from zero."

### Proof these calls are not a Tick/hotpath or SimulationSession edit

- `DelegationBridge.Tick` (`DelegationBridge.cs:130-165`) is the hotpath. The driver invokes it. This spike adds no line inside it. `TryEnqueueHumanOrder` is explicitly "does not touch the Tick hot path" (`DelegationBridge.cs:206-208`). CMD-31 is a wrapper over that method (`C2PlayerCommandBridge.cs:7-8`).
- `SimulationSession.Tick` / `TickHeadless` / `RunExecutingTick` (`SimulationSession.cs:214-331`) are unchanged. Pause, acceleration, watch, and withdraw trials stay as they are.
- No new member is proposed on `CatalogWriteGate`, `MapPlaceholderPanelHost`, `GlobeMapProductHost`, CMD-31/32/34, or ReplayGolden.

### Conditions (all required)

1. **The four-tuple is not sufficient for interactive sessions.** `(scenario id, seed, catalog snapshot hash, human/player order log)` restores agent-vs-agent plus logged `PlayerOrderRecord` / `ControllerChangeRecord` rows only if the driver also re-creates harness world setup. It does **not** restore pause, acceleration, watch ack/dismiss, explicit resume override, pending approve/reject, trait rebind, or `NextEngageSalvoOverride` (§2, all **No** or gaps inside **Partial**).
2. **Catalog hash ≠ catalog snapshot.** `CatalogSnapshotHasher` is sensor-row SHA-256 (`CatalogSnapshotHasher.cs:8-11`). Restore needs a resolvable catalog snapshot id or the bytes. Hash mismatch must abort load.
3. **Replay starts at 0.** Checkpoints do not skip work. Cost is O(T) full ticks (§5).
4. **Fresh session only.** Re-enqueue appends new log rows (`DelegationBridge.cs:196-202`). Apply inputs to an empty `DecisionLog`, then compare fingerprints. Do not append onto a deserialized log (GDD: no append during replay, `design/gdd/order-log-replay.md:97`).
5. **`AttachReplayViewer` stays false while injecting orders.** Otherwise `TryEnqueueHumanOrder` and direct-control return false (`DelegationBridge.cs:173-176,284-286`).
6. **Direct control before enqueue.** CMD-31 and `TryEnqueueHumanOrder` require `HumanController` (`C2PlayerCommandBridge.cs:54-57`, `DelegationBridge.cs:178-181`). Controller-change rows must be applied first.
7. **One driver, not a forked copy of `RunCore`.** **Unverified / inferred:** two loops that both call public APIs will diverge the moment mission, detection, datalink, or kill-transition append order differs (`BalticReplayHarness.cs:338-471`). A later implementation should extend the harness **outside** `SimulationSession` and `DelegationBridge.Tick`, or extract a shared stepper. That extraction is future work, not this spike.
8. **Delegation RNG has no export.** Mid-tick resume without replaying draws is not available (`SeededRng.cs:5-22`). Condition (3) covers this.
9. **Presentation stays off the authority path.** Save/load of world truth is sim/driver work. A future C2 control must issue commands through the façade (ADR-010), not write `DecisionLog` from the map (ADR-007) or read sim internals from a `MonoBehaviour` (ADR-001, ADR-010 §2–3).

Agent-vs-agent save/load with only scenario id + seed + catalog bytes is the stronger, already-shipped subset: `BalticReplayHarness.Run` plus fingerprint compare. Adding human orders is the conditional part.

---

## 5. Replay-to-tick cost estimate

No Baltic session ms/tick was measured for this spike (docs-only; tests not run).

### Verified numbers

| Source | Number | What it measures |
| --- | --- | --- |
| `production/qa/swarm-a6-replay-performance-caps-2026-08-09.md:26-30,48-57` | 16 concurrent swarms, 60 ticks, ≤960 integrity ops (16×60), logical drones 640, counterfactual O(drones×ticks) 38400 **not** used, wall clock **< 2000 ms** soft gate | `SwarmReplayHarness` / `RunDesignMaxStress` integrity. Phase A swarm caps. The same note says Baltic `ReplayGoldenRegressionCatalog` was not mutated (`:18`). |
| `tools/batch-replay/README.md:10,16` | Example CLI: `--ticks 6` for two scenarios × two seeds; `--ticks 4` for all scenario JSON × seed 42 | Invocation shape only. **No wall-clock figure** in that file. |
| `src/ProjectAegis.Sim/Scenario/ScenarioReplaySettings.cs:4` | Checkpoint interval default **300** ticks | How often a hash row is stored. `Record` does not snapshot world state (`ReplayCheckpointStore.cs:10-17`), so the interval does **not** reduce re-sim work. |
| `design/gdd/simulation-core-time.md:146` | `checkpointEveryTicks` 300, documented as implemented | Same knob. |

### Unverified / inferred

- A restore to tick T costs one full `BalticReplayHarness`-equivalent loop of T ticks, plus human-input calls that are O(logged human rows) and cheap next to the tick. Checkpoints do not amortize that.
- Dividing the swarm soft cap (2000 ms / 960 ops ≈ 2.1 ms/op) and calling that a Baltic tick cost is **unverified** and the wrong workload (swarm integrity ops vs delegation + detection + engage).
- Interactive acceleration multiplies `Sim.TickOnce` by `AccelerationFactor` per session tick (`SimulationSession.cs:323-327`, factor up to 256, `SimClock.cs:11`). A save taken under acceleration can cost up to 256 pipeline steps per observed tick. No measurement of that multiplier is in the two perf sources.
- GDD `maxTicksPerFrame` 8 interactive / unlimited headless is marked future (`design/gdd/simulation-core-time.md:145`). Not a measured cap.

---

## 6. ReplayGolden / hash impact

**None of (a), (b), or (c) requires changing Baltic v2 world hash `17144800277401907079` if the implementation is additive:** new driver, new save file, no edit to tick order, log canonical text, `SimWorldHash` mix, v2 scenario JSON, or `tests/regression/replay-golden-baltic-v2-*.txt`.

**Verified present** at this SHA (read-only grep, files not modified):

- `tests/regression/replay-golden-baltic-v2-patrol-2026-06-22.txt`
- `tests/regression/replay-golden-baltic-v2-patrol-band-b-2026-06-22.txt`
- `tests/regression/replay-golden-baltic-v2-patrol-band-c-2026-06-22.txt`
- `tests/regression/replay-golden-baltic-v2-patrol-mission-v2-2026-06-22.txt`
- `tests/regression/replay-golden-baltic-v2-mission-event-2026-06-22.txt`

Each has `WORLD_HASH=17144800277401907079`. Contract: `docs/engineering/determinism-and-replay.md:148-153`. This spike did not re-bless goldens and did not run the suite.

**What would change the hash** (do not do these under H6 save/load):

- Any edit to `DelegationBridge.Tick`, `SimulationSession.RunExecutingTick`, or `BalticReplayHarness.RunCore` append order that changes v2 fingerprints or `Sim.LastWorldHash`.
- Putting save-only fields into `DecisionLog.ComputeFingerprint` input (ADR-003: replay hash covers the full log, `docs/architecture/adr-003-order-log-schema.md:12-14`).
- Changing `SimWorldHash.Fold` / `Combine` (`SimWorldHash.cs:11-28`) or checkpoint interval **inside** v2 policy JSON that the golden runner loads.
- Re-recording `tests/regression/` because a save test was pointed at the v2 golden scenario and its log text moved.

A side-file save format and a new test scenario id leave the v2 pin untouched. Baltic v3 `baltic-v3-*` goldens are a separate corpus; this spike does not propose editing them either.

---

## 7. Open questions

### DRG-328 (product decision)

1. Is the first save **agent-vs-agent only** (scenario id + seed + catalog bytes + tick), or must it round-trip direct control and player orders?
2. Which unlogged UI actions are in-scope for the save: pause, acceleration, watch ack/dismiss, force-resume, pending approve/reject, trait rebind, attack-menu salvo? Each is a product cut; the log does not contain them today (§2).
3. Is a full re-sim to tick T acceptable, given checkpoints do not skip work (§5)? What T must feel instant?
4. On catalog mismatch (sensor hash or broader snapshot id), is load a hard fail?
5. Does "human/player order log" mean the full `DecisionLog` (engagements, contacts, mission rows included) or only `PlayerOrder` + `ControllerChange` rows? Replay-to-tick wants the full log as a **checksum**, and only human rows as **inputs**. Using the full log as inputs would double-apply sim-generated rows.
6. Is `commandId` (CMD-31) required on load, or is `OrderKind` enough? The record has kind only (`PlayerOrderRecord.cs:6-13`).
7. Are watch ack/dismiss presentation (drop on load) or part of the saved game? The type comment says presentation (`WatchAttentionQueue.cs:6-7`). ADR-007: map/C2 must not become authority for that choice.

### DRG-329 (ADR story — questions only, no ADR text, no number)

1. Confirm zero-touch: save driver calls `DelegationBridge.Tick` and does not modify it; `SimulationSession` gains no load method (frozen hub addendum).
2. Where the driver lives (new type next to `BalticReplayHarness` vs a later shared stepper) so interactive replay cannot drift from `RunCore`.
3. Save envelope vs ADR-003 log schema: human inputs that are not log rows need a **side channel**, not new fingerprint fields, or v2 goldens move (§6).
4. Catalog identity: sensor `CatalogSnapshotHasher` vs a wider snapshot id. ADR-006 applies if the save binds SQLite; presentation must not open the DB (ADR-010 / ADR-006).
5. GDD `simulation-core-time.md:126` ("restore simTick, scenarioSeed, RNG stream counters") does not match the code: sim RNG is stateless (`SeededRng.cs` in Sim), delegation RNG state is private, `ReplayCheckpoint` has no RNG field. The ADR story should say whether the GDD line is amended to "replay from 0" or whether a future **non-frozen** type exports stream counters. Do not put that export on `SimulationSession` without a new authorization.
6. Checkpoint GDD wording ("world-state snapshot", `order-log-replay.md:80`) vs hash-only `ReplayCheckpoint`. Skip-to-checkpoint is a different decision from verify-after-resim.
7. `AttachReplayViewer` is the wrong flag for save restore (it blocks order injection). Viewer scrub and save-load need different modes if both exist.
8. Presentation wall for any later UI: ADR-010 §2–3, ADR-007, ADR-001. Do not cite Git ADR-018 as the UI boundary.

---

## 8. What this spike did not do

- No code, test, golden, ADR, or baseline edits.
- No `dotnet test`. No claim that ReplayGolden or PlayModeSmoke passed or failed on this branch.
- No Unity Editor / Unity MCP session. Headless read of C# and docs only.
- Did not modify DelegationBridge, SimulationSession, CatalogWriteGate, map hosts, CMD-31/32/34, or `tests/regression/`.
