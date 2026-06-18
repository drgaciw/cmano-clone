# ProjectAegis.Delegation — Agent Delegation Framework

Engine-agnostic C# library implementing the **Agent Delegation Framework** for
Project Aegis: the "brains" that turn an observed picture of the world into
`Order` objects, whether a human or an AI agent is in command of a unit.

**No `UnityEngine` dependency.** The assembly targets `net8.0;netstandard2.1`, so
it runs identically headless (CI, replay, the delegation demo) and inside the
Unity runtime. It consumes the per-side picture the simulation produces and emits
orders back; the deterministic tick/engagement core lives in
[`ProjectAegis.Sim`](../ProjectAegis.Sim/README.md) and the authoritative
catalog/scenario data in [`ProjectAegis.Data`](../ProjectAegis.Data/README.md).

**Design specs:**
[`docs/superpowers/specs/2026-05-28-agent-delegation-framework-design.md`](../../docs/superpowers/specs/2026-05-28-agent-delegation-framework-design.md)
and
[`2026-05-30-agent-delegation-decisions-design.md`](../../docs/superpowers/specs/2026-05-30-agent-delegation-decisions-design.md).

## What this library provides

- **Controller / possession model** — `HumanController` and `AgentController`
  share a `ControllerSlot` per unit and emit the same `Order` objects.
- **Unit + group delegation** with detach-and-rejoin override semantics.
- **Attention / bandwidth** with graceful degradation under overload.
- **Trait + stochastic** decision pipeline (seeded, deterministic).
- **Six personality presets** as data (`PersonalityCatalog`).
- **Autonomy levels** (Manual → Full Autonomous) and ROE gating.
- **Unified order log** stream (ADR-003) for AAR, replay, and explainability.
- **C2 UI projections** — read-only panel view-models derived from the log.

## Core invariant: determinism

**A delegation tick is a pure function of `(global seed, agent traits, observed
state, sim tick)`.** Re-running the same scenario with the same seed must produce
a byte-identical order log. Two rules enforce this:

1. **No wall-clock, no ambient randomness.** Each `AgentController` owns a
   per-agent `SeededRng` (xorshift) seeded from `globalSeed ^ agentSalt`, where
   `agentSalt = DeterministicHash.OrdinalHash(agentId)`. Nothing in `Tick(...)`
   reads `DateTime.UtcNow`, `System.Random`, or `Guid.NewGuid()`.
2. **Order-independent iteration.** Subsystems sort inputs by stable keys
   (`StringComparer.Ordinal`) before drawing RNG — e.g. mission events fire in a
   locked `fire_order`, trust signals seed agent ids `Distinct(Ordinal)`. Results
   never depend on dictionary/insertion order.

`DecisionLog.ComputeFingerprint()` serialises the whole timeline to a stable
string; replay tests compare it against a golden fingerprint. Do **not** call
Hindsight `recall`/`reflect` inside `Tick()` — session memory is a dev-time tool,
not a runtime input (see [`AGENTS.md`](../../AGENTS.md)).

## Orchestration entry point

`DelegationOrchestrator.Tick(ObservedState)` is the main per-frame call. The flow
for each registered target with an active `AgentController`:

```
ObservedState
  └─ AgentController.TryDecide(...)
       1. Gate on reaction delay        (skip until _nextDecisionSimTime)
       2. AttentionCalculator.Evaluate  → load vs budget, degradation flags
       3. PerceivedStateFactory.FromFull → fog-of-war by SituationalAwareness
       4. IPolicy.GenerateCandidates    → scored intents
       5. DecisionPipeline.Choose       → trait-weighted softmax + RNG draw
       6. append AgentDecision to DecisionLog
       7. AutonomyGate.Evaluate         → ExecuteNow / QueueForApproval / Reject
       8. issue Order (or log PolicyDenial)
  └─ DrainIssuedOrders → orchestrator.ExecutedOrders
```

```csharp
var orchestrator = new DelegationOrchestrator(globalSeed: 1337);

var agent = orchestrator.CreateAgentFromPreset(
    new AgentId("cap-01"),
    PersonalityCatalog.All[0],        // "Aggressive"
    AutonomyLevel.SemiAutonomous);

orchestrator.Register(unit);
orchestrator.AssignAgentToTarget(agent, unit, EffectivePolicy.DefaultFree);
orchestrator.BeginExecution();

orchestrator.Tick(observedState);
foreach (var order in orchestrator.ExecutedOrders) { /* feed the sim */ }

var trust = orchestrator.FinalizeScenario(missionSucceeded: true);
```

`SimulationModeConfigurator.Apply(...)` wires Human / Mixed / Agent-vs-Agent
controller assignments from a `SimulationModeProfile` (and optional scenario
policy id). In `Planning` phase `Tick` is a no-op; `BeginExecution()` flips to
`Executing` and logs a `ModeChange`.

## Decision model

| Trait (`TraitVector`) | Effect in the pipeline |
|-----------------------|------------------------|
| `Aggression`, `RiskTolerance` | scoring inputs for `IPolicy.GenerateCandidates` |
| `ReactionDelay` | decision cadence; ×5 when attention forces `SlowerReactions` |
| `ErrorRate` | policy-defined noise on candidate scoring |
| `SituationalAwareness` | fidelity of `PerceivedState` (fog of war) |
| `Decisiveness` | softmax temperature — high = sharper choice, low = exploratory |

`DecisionPipeline.Choose` builds a softmax over candidate scores with
`temperature = max(0.05, Decisiveness × (overloaded ? 0.5 : 1.0))`, then samples
with one `SeededRng.NextUnit()` draw. Under `NarrowedFocus` the candidate pool is
trimmed to the top 2 before sampling. The chosen intent, RNG draw, and rationale
are all recorded for explainability.

`AttentionCalculator.Evaluate` computes
`load = contacts×0.5 + activeEngagements×1.0 + members×0.25` and compares to the
agent's budget to set three degradation flags: `SlowerReactions` (`load > budget`),
`NarrowedFocus` (`> 1.25×`), and `SimplerDecisions` (`> 1.5×`).

## Autonomy & ROE gating

`AutonomyGate.Evaluate(autonomy, order, playerApproved)` first runs the ROE filter
(`IRoeFilter`); a `Reject` short-circuits to a `PolicyDenial` carrying a
`FireAbortReason`. Otherwise the autonomy level decides:

| Level | Behaviour |
|-------|-----------|
| `Manual` | execute only with `playerApproved`, else queue |
| `Assisted` | auto-execute `RiskLevel.Low` orders; queue higher risk |
| `SemiAutonomous` / `FullAutonomous` | auto-execute (ROE permitting) |

ROE is sourced from scenario policy via `RoePolicyAdapter` over the sim's
`IPolicyEvaluator` (ADR-002); `PassthroughRoeFilter` is the permissive default.

## Subsystem map

| Directory | Responsibility | Key entry points |
|-----------|----------------|------------------|
| `Core/` | Shared value types and ids. | `Order`, `OrderId`/`AgentId`/`TargetId` (`Identifiers`), `AutonomyLevel`, `SimulationMode`, `DeterministicHash` |
| `Orchestration/` | Per-tick driver, possession, gates, scenario wiring. | `DelegationOrchestrator`, `AutonomyGate`, `OverrideService`, `SimulationModeConfigurator`, `PolicySnapshotRegistry`, `SimulationPhase`, `SimulationSession`, `LoopPolicyGate`, `PlayerInfoFilter` |
| `Controllers/` | Possession of a unit's `ControllerSlot`. | `IController`, `AgentController` (`TryDecide`/`DrainIssuedOrders`), `HumanController`, `ControllerSlot` (suspend/resume) |
| `Decision/` | Decision pipeline, per-agent RNG, and the **unified order log**. | `DecisionPipeline`, `SeededRng`, `DecisionLog`/`IOrderLog`, `OrderLogEntry`/`OrderLogEntryKind`, `ScoredIntent`, `DecisionRecord`, `PlayerOrderExecutionQueue` |
| `Policy/` | Candidate generation strategies. | `IPolicy.GenerateCandidates`, `StubPatrolPolicy`, `PatrolCandidateEngagePolicy` |
| `Attention/` | Bandwidth model and overload degradation. | `AttentionCalculator`, `AttentionEvaluation`, `AttentionDegradation` |
| `Traits/` | Personality data. | `TraitVector` (6 axes), `PersonalityCatalog` (6 presets), `PersonalityPreset` |
| `Roe/` | Rules-of-engagement filtering and risk mapping. | `IRoeFilter`/`RoeVerdict`, `RoePolicyAdapter`, `PassthroughRoeFilter`, `DefaultRiskClassifier`, `OrderActionMapper` |
| `Groups/` | Group delegation override mechanics. | `DetachRejoinService` (detach a member, rejoin to parent group) |
| `Mission/` | Deterministic mission-event timeline. | `MissionRuntime` (locked `fire_order`), `MissionEventDefinition`, `MissionRuntimeFactory` |
| `Comms/` | Scenario-driven comms degradation and order delay. | `CommsTimelineSimulator`, `CommsState`, `CommsOrderDelay`, `CommsTrackStaleness`, `SpoofTrackTimelineSimulator` |
| `Logistics/` | Fuel burn timeline over the sim's `FuelLedger`. | `FuelTimelineTracker` (JOKER/BINGO band transitions) |
| `Trust/` | Emit-only campaign XP signals. | `TrustSignalEmitter.EmitFromSession`, `TrustSignal`, `AgentExperienceBlob` |
| `Projection/` | Read-only C2 UI view-models from the order log (ADR-007). | `C2TopBarProjection`, `MapPictureProjection`, `SensorC2Projection`, `MessageLogProjection`, `OobTreeProjection`, `*PanelState`, `*PanelBinder`, `LossesScoringCsvExporter` |
| `Replay/` | Determinism checkpoints and fingerprints. | `ReplayCheckpointStore`, `ReplayCheckpoint`, `OrderLogReplayFingerprint` |
| `Sim/` | Inbound sim-facing contracts. | `ObservedState`, `UnitReadinessMap` |
| `Targets/` | Commandable entities. | `ICommandableTarget`, `UnitTarget`, `GroupTarget` |
| `Hindsight/` | Optional session-memory sidecar (off in CI/replay). | see [`Hindsight/README.md`](Hindsight/README.md) |
| `Polyfills/` | `netstandard2.1` shim (`IsExternalInit` for records). | — |

## Order log schema (ADR-003)

`DecisionLog` implements `IOrderLog` as one append-only, monotonically sequenced
stream. Every event is an `OrderLogEntry` with an `OrderLogEntryKind`; typed
`Append*` helpers add `AgentDecision`, `PolicyDenial`, `Engagement`,
`EngagementOutcome`, `ControllerChange`, `GroupMemberDetach`/`Rejoin`,
`MagazineChange`, `ContactChange`, `MissionTransition`, `EventFired`,
`PlayerOrder`, `PolicyUpdate`, `ModeChange`, `CommsStateChange`,
`FuelStateChange`, and `FuelBurn` records.

- `ChronologicalEntries()` returns the merged timeline ordered by `SequenceId`.
- `ComputeFingerprint()` serialises every entry (`Kind|Seq|SimTime|payload`) for
  golden replay comparison — payloads use round-trip (`"R"`) float formatting so
  the string is bit-stable across platforms.
- `GetLiveOrderLogView()` (on the orchestrator) applies `PlayerInfoFilter` so the
  player only sees entries their scenario `PlayerInfoModel` permits.

## Build, test, and demo

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download). From the repo root:

```bash
dotnet test ProjectAegis.sln -v minimal
dotnet run --project src/ProjectAegis.Delegation.Demo
```

## Unity integration

Use **`ProjectAegis.Delegation.UnityAdapter`** — it implements `ISimWorldSnapshot`
(sim → delegation) and `IOrderSink` (orders → sim). See
[`../ProjectAegis.Delegation.UnityAdapter/README.md`](../ProjectAegis.Delegation.UnityAdapter/README.md)
and `unity/ProjectAegis/README.md`.

```bash
dotnet test ProjectAegis.sln -v minimal
./tools/copy-delegation-assemblies.ps1   # copies Release DLLs into Unity Plugins
```

## Cross-cutting rules

- **Determinism is non-negotiable.** No `DateTime.UtcNow`, `System.Random`,
  `Guid.NewGuid()`, or order-dependent iteration in the tick path. Use the
  per-agent `SeededRng`, `DeterministicHash`, and stable `Ordinal` ordering.
- **No Hindsight recall/reflect inside `Tick()`** — sidecar is emit-only at runtime.
- **Projections are read-only.** `Projection/` derives view-models from the log;
  it never mutates orchestrator or sim state.

## See also

- [ADR-002 — policy evaluator](../../docs/architecture/adr-002-policy-evaluator.md)
- [ADR-003 — order log schema](../../docs/architecture/adr-003-order-log-schema.md)
- [ADR-007 — C2 map presentation](../../docs/architecture/adr-007-c2-map-presentation.md)
- [ProjectAegis.Sim — deterministic simulation core](../ProjectAegis.Sim/README.md)
- [ProjectAegis.Data — database intelligence layer](../ProjectAegis.Data/README.md)
- [Delegation ↔ Sim wiring notes](../../docs/architecture/wiring-delegation-sim-2026-05-29.md)
- [Architecture overview](../../docs/architecture/architecture.md)
