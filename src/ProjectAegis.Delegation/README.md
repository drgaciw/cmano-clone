# ProjectAegis.Delegation

Engine-agnostic C# library implementing the **Agent Delegation Framework** — the core that
turns an observed world state into gated, logged orders for every unit and group in a
scenario. The player commands theater-level forces and delegates tactical decisions to
autonomous agents; this assembly is where that delegation actually happens. It has no
`UnityEngine` reference (targets `net8.0`, depends only on `ProjectAegis.Sim` /
`ProjectAegis.Data`), so the whole pipeline runs headless under `dotnet test`.

> **Determinism is the load-bearing invariant.** Given the same `(scenario, seed)`, a run must
> reproduce the same order log bit-for-bit. Agent decisions draw from a **stateful** per-agent
> `SeededRng` (`Decision/SeededRng.cs`), so *draw order matters here* — never introduce
> ambient randomness, `DateTime.UtcNow`, or unordered-collection iteration in the tick path.
> See the [determinism & replay guide](../../docs/engineering/determinism-and-replay.md).

**Design spec:** [`docs/superpowers/specs/2026-05-28-agent-delegation-framework-design.md`](../../docs/superpowers/specs/2026-05-28-agent-delegation-framework-design.md)

---

## Subsystem map

| Folder | Purpose | Key types |
|--------|---------|-----------|
| `Orchestration/` | Main entry point + session wiring: run one tick, resolve policy snapshots, drive engagement | `DelegationOrchestrator`, `SimulationSession`, `AutonomyGate`, `SimulationModeConfigurator`, `OverrideService`, `PolicySnapshotRegistry`, `LoopPolicyGate` |
| `Controllers/` | Possession model — who is issuing orders for a target | `IController`, `AgentController`, `HumanController`, `ControllerSlot` |
| `Decision/` | Trait-weighted stochastic choice + the append-only order log | `DecisionPipeline`, `ScoredIntent`, `SeededRng` (stateful), `DecisionLog`/`IOrderLog`, `OrderLogEntry`, `FingerprintFloat` |
| `Core/` | Shared value types | `AutonomyLevel`, `Order`/`OrderKind`/`RiskLevel`, `SimulationMode`, `Identifiers`, `DeterministicHash` |
| `Traits/` | Agent personality inputs | `TraitVector`, `PersonalityCatalog`, `PersonalityPreset` |
| `Attention/` | Bandwidth model + graceful overload degradation | `AttentionCalculator`, `AttentionState`/`AttentionEvaluation` |
| `Roe/` | Rules-of-engagement gate bridging to the sim policy evaluator | `IRoeFilter`, `RoePolicyAdapter` (ADR-002), `PassthroughRoeFilter`, `OrderActionMapper`, `DefaultRiskClassifier` |
| `Policy/` | Candidate-generation plug-in seam | `IPolicy`, `PatrolCandidateEngagePolicy`, `StubPatrolPolicy`, `EngagePrimaryMode` |
| `Sim/` | Inbound world-state contract consumed by the tick | `ObservedState`, `PerceivedState`, `UnitReadinessMap` |
| `Targets/` | Commandable unit/group model | `ICommandableTarget`, `UnitTarget`, `GroupTarget` |
| `Groups/` | Detach-and-rejoin override semantics for group members | `DetachRejoinService` |
| `Mission/` | Deterministic mission timelines + contact-triggered ROE escalation | `MissionRuntime`, `MissionContactTriggerRuntime` (Baltic v3) |
| `Comms/` | Comms-state timeline + order-delay/staleness model (doc 19) | `CommsTimelineSimulator`, `CommsOrderDelay`, `CommsTrackStaleness`, `SpoofTrackTimelineSimulator` |
| `Logistics/` | Fuel burn + band-transition ledger | `FuelTimelineTracker` |
| `Replay/` | Scrub-to-tick checkpoints + order-log replay fingerprint | `ReplayCheckpoint`, `ReplayCheckpointStore`, `OrderLogReplayFingerprint` |
| `Projection/` | **Read-only** projections of the order log into C2 UI view-models | `MessageLogProjection`, `ContactPictureProjection`, `LossesScoringProjection`, `OobTreeProjection`, `SensorC2Projection`, `App6Sidc` (APP-6/2525C symbology, ADR-007) |
| `Trust/` | Emit-only post-scenario trust/XP signals for future campaign hooks | `TrustSignalEmitter`, `TrustSignal`, `AgentExperienceBlob` |
| `Hindsight/` | Optional session-memory sidecar (off in CI/replay) | `HindsightIntegration`, `HindsightOptions`, `IHindsightMemoryClient` |

---

## The delegation tick

[`DelegationOrchestrator.Tick(ObservedState)`](Orchestration/DelegationOrchestrator.cs) is the
main entry point. For every registered target whose active controller is an agent, it runs the
decision → gate → issue pipeline, then exposes the executed orders:

```text
Tick(observedState)                              // no-op while Phase == Planning
  └─ per target with an active AgentController:
       AgentController.TryDecide(...)
         ├─ AttentionCalculator.Evaluate(budget, memberCount, state)   → load / degradation
         ├─ PerceivedStateFactory.FromFull(state, SituationalAwareness) → fog-of-war view
         ├─ IPolicy.GenerateCandidates(perceived, traits)               → scored intents
         ├─ DecisionPipeline.Choose(candidates, traits, attention, rng) → one Order
         ├─ AutonomyGate.Evaluate(autonomy, order, playerApproved)      → execute / queue / reject
         └─ DecisionLog.Append(...)                                     → order-log rows
  → ExecutedOrders  (drained per controller)
```

[`SimulationSession`](Orchestration/SimulationSession.cs) wraps the orchestrator with the
`ProjectAegis.Sim` engagement pipeline (`SimTickPipeline` + `MvpEngagementResolver`): after the
delegation tick it deconflicts salvos, applies comms gating, resolves `Engage` orders, and
folds engagement/damage results back into the order log. This is the headless path used by the
Baltic replay harness and the CI replay goldens — build one with
`SimulationSession.BindMvpEngagementForScenario(orchestrator, scenarioPolicyId, catalog)`.

Under Unity, the [`UnityAdapter`](../ProjectAegis.Delegation.UnityAdapter/README.md) implements
`ISimWorldSnapshot` (sim → `ObservedState`) and `IOrderSink` (`ExecutedOrders` → sim), calling
this same `Tick`.

## Decision model

An agent never decides more often than its **reaction delay** allows (stretched under
attention overload). Each eligible tick:

- **Attention** — `AttentionCalculator` scores load as
  `contacts × 0.5 + activeEngagements × 1.0 + memberCount × 0.25` and compares it to the
  agent's budget. Crossing `1.0×`/`1.25×`/`1.5×` of budget triggers *slower reactions*,
  *narrowed focus* (only the top-2 candidates survive), and *simpler decisions* respectively.
- **Choice** — [`DecisionPipeline.Choose`](Decision/DecisionPipeline.cs) is a trait-weighted
  softmax: `weight = exp(score / temperature)` where
  `temperature = max(0.05, Decisiveness × (overloaded ? 0.5 : 1.0))`. A single draw from the
  stateful `SeededRng` picks the winner. Non-positive candidates are dropped only when a
  positive one remains, so a policy can de-prioritize (score ≤ 0) an intent without removing it
  from the logged candidate set.

Everything the agent saw and weighed is written to the `DecisionLog` for AAR / explainability.

## Autonomy & ROE gating

Every chosen order passes [`AutonomyGate.Evaluate`](Orchestration/AutonomyGate.cs). The ROE
check runs **first** (`IRoeFilter`; the default `RoePolicyAdapter` bridges to the sim
`IPolicyEvaluator` per ADR-002): a `Reject` verdict stops the order and logs a policy denial.
Otherwise the [`AutonomyLevel`](Core/AutonomyLevel.cs) decides:

| Autonomy level | Behavior |
|----------------|----------|
| `Manual` (1) | Queue for player approval; execute only if pre-approved |
| `Assisted` (2) | Auto-execute `Low`-risk orders; queue `High`-risk for approval |
| `SemiAutonomous` (3) | Auto-execute |
| `FullAutonomous` (4) | Auto-execute |

`SimulationModeConfigurator.Apply` assigns Human / Mixed / Agent-vs-Agent controllers per
`SimulationModeProfile` (doc 03), and `AttachReplayViewer` blocks all human ingress for AvA
observer runs.

## Possession & groups

A target's [`ControllerSlot`](Controllers/ControllerSlot.cs) holds exactly one active
`IController`. Taking direct control of a unit installs a `HumanController` (suspending the
agent); releasing restores it. `TryTakeDirectControl` on a **group member** detaches it via
[`DetachRejoinService`](Groups/DetachRejoinService.cs) (the group re-plans), and releasing
rejoins it — all transitions are logged as `ControllerChange` / `GroupMemberDetach` /
`GroupMemberRejoin` rows.

## Order log & projections

[`DecisionLog`](Decision/DecisionLog.cs) implements the append-only
[`IOrderLog`](Decision/IOrderLog.cs) (ADR-003) and is the single source of truth for a run.
`ComputeFingerprint()` produces the deterministic order-log hash the replay goldens assert.
The `Projection/` types are **pure read-models** rebuilt from the log — the message log,
contact/facility picture, OOB tree, sensor C2 panel, losses/scoring, and APP-6 map symbology —
so the UI never mutates simulation state.

## Personality presets

`PersonalityCatalog.All` ships six data-driven presets (default attention budget `20`):

| Preset | Notable trait bias | Attention |
|--------|--------------------|-----------|
| `Aggressive` | high Aggression / RiskTolerance | ×1.0 |
| `Defensive` | low Aggression, high SA | ×1.0 |
| `Cautious` | slow ReactionDelay, low RiskTolerance | ×1.0 |
| `Opportunistic` | balanced-aggressive | ×1.0 |
| `SwarmCoordinator` | high Decisiveness | ×1.25 |
| `EwSpecialist` | high SA, low ErrorRate | ×0.9 |

Create agents from a preset via `DelegationOrchestrator.CreateAgentFromPreset(id, preset, autonomy)`.

## Public seams (for neighboring systems)

| Seam | Type | Notes |
|------|------|-------|
| Orchestration | `DelegationOrchestrator.Tick(ObservedState)` | Main entry point |
| Sim → delegation | `ObservedState` / `PerceivedState` | Fed by the Unity bridge / `SimulationSession` |
| Policy | `IPolicy.GenerateCandidates(...)` | Pluggable candidate generator (`PatrolCandidateEngagePolicy`) |
| ROE | `IRoeFilter` → `IPolicyEvaluator` | `RoePolicyAdapter` bridges to the sim policy evaluator (ADR-002) |
| Order log | `IOrderLog` / `DecisionLog.ComputeFingerprint()` | Replay + projection surface (ADR-003) |
| Trust / XP | `TrustSignalEmitter.EmitFromSession(...)` | Emit-only hook for future campaign XP |
| Memory | `HindsightOptions` | Optional sidecar; null in CI/replay |

---

## Build, test & demo

```bash
dotnet build src/ProjectAegis.Delegation/ProjectAegis.Delegation.csproj
dotnet test  src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj -v minimal
dotnet run   --project src/ProjectAegis.Delegation.Demo        # Baltic replay harness
```

`ProjectAegis.Delegation.Tests` is part of the ≥1232-test solution baseline. For Unity, copy
the Release DLLs into `Plugins/` after a green build:

```bash
./tools/copy-delegation-assemblies.ps1
```

## See also

| Topic | Doc |
|-------|-----|
| Simulation core (engagement, sensors, sim-side RNG) | [`../ProjectAegis.Sim/README.md`](../ProjectAegis.Sim/README.md) |
| Unity bridge (`ISimWorldSnapshot` / `IOrderSink`) | [`../ProjectAegis.Delegation.UnityAdapter/README.md`](../ProjectAegis.Delegation.UnityAdapter/README.md) |
| Replay harness / golden regeneration | [`../ProjectAegis.Delegation.Demo/README.md`](../ProjectAegis.Delegation.Demo/README.md) |
| Data / catalog layer | [`../ProjectAegis.Data/README.md`](../ProjectAegis.Data/README.md) |
| Determinism rules, hashing, golden workflow | [`docs/engineering/determinism-and-replay.md`](../../docs/engineering/determinism-and-replay.md) |
| Abort-reason codes (`Engagement|`/`PolicyDenial` rows) | [`docs/engineering/abort-reason-catalog.md`](../../docs/engineering/abort-reason-catalog.md) |
| Policy evaluator boundary | [`adr-002-policy-evaluator.md`](../../docs/architecture/adr-002-policy-evaluator.md) |
| Order-log schema | [`adr-003-order-log-schema.md`](../../docs/architecture/adr-003-order-log-schema.md) |
| C2 map / APP-6 presentation | [`adr-007-c2-map-presentation.md`](../../docs/architecture/adr-007-c2-map-presentation.md) |
| Hard invariants + verification block | [`AGENTS.md`](../../AGENTS.md#hard-invariants--never-break-these) |
