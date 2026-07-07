# 08 - Agentic Architecture Layer

**Last Updated:** 2026-06-04  
**Related:** [01-Project-Overview.md](01-Project-Overview.md) · [03-Simulation-Modes.md](03-Simulation-Modes.md) · [04-Agent-Delegation.md](04-Agent-Delegation.md) · [06-Database-Intelligence.md](06-Database-Intelligence.md) · [07-Agentic-Infrastructure.md](07-Agentic-Infrastructure.md) · [20-Command-And-Control-UI.md](20-Command-And-Control-UI.md)  
**Status:** Locked  
**Research basis:** [Agentic CMO Research](../../docs/research/agentic-cmano-research.md)  
**Architecture:** [Master Architecture](../../docs/architecture/architecture.md) · ADR-001–006 (see [Resolved Design Decisions](#resolved-design-decisions))

## Purpose

Define the core software architecture that enables true agentic development, high-performance military simulation, and seamless operation across human, mixed, and fully autonomous agent-versus-agent modes.

## Vision

A modern, modular, agent-aware architecture built on a **pure C# simulation kernel** (`ProjectAegis.Sim`) plus a pluggable **delegation and decision layer** (`ProjectAegis.Delegation`). Unity DOTS/ECS hosts scalable world state per [ADR-005](../../docs/architecture/adr-005-dots-sim-core.md); the catalog and scenario packages live in `ProjectAegis.Data`. The simulation core, scenario model, databases, UX, and automation evolve **semi-independently** — matching the CMO pattern of preserving the sim kernel while improving interface and tooling.

**P0 spine (shipped):** deterministic tick + engage + detection sub-hash, policy evaluator in Sim, delegation orchestration + unified order log, headless Baltic harness, catalog/scenario binding. **Post-P0:** full DOTS entity store, event priority queue, structured sim API for external RL agents, cloud batch farm.

## Five-Layer Clean-Room Architecture *(research-aligned)*

| Layer | Responsibilities | Project path(s) | ADR / req anchor |
|-------|------------------|-----------------|------------------|
| **Simulation kernel** | Time step, detection, kinematics, weapons, fuel, comms, doctrine | `src/ProjectAegis.Sim/` · tests `src/ProjectAegis.Sim.Tests/` | [ADR-001](../../docs/architecture/adr-001-sim-assembly-boundary.md), [ADR-002](../../docs/architecture/adr-002-policy-evaluator.md), [ADR-004](../../docs/architecture/adr-004-tick-pipeline-order.md), [ADR-005](../../docs/architecture/adr-005-dots-sim-core.md) |
| **Entity database** | Platforms, sensors, weapons, signatures, mounts, provenance | `src/ProjectAegis.Data/` · `data/catalog/` (SQLite, gitignored dev seed) | [ADR-006](../../docs/architecture/adr-006-data-layer-boundary.md), [06](06-Database-Intelligence.md) |
| **Scenario system** | Sides, geography, ORBAT, triggers, events, objectives | `src/ProjectAegis.Data/Scenario/` · `data/scenarios/*.policy.json` · `assets/data/scenarios/validation/` · `src/ProjectAegis.MissionEditor.Cli/` | [ADR-008](../../docs/architecture/adr-008-mission-editor-validation-engine.md), [11](11-Agentic-Mission-Editor.md) |
| **UX layer** | Map, timelines, planners, inspectors, agent supervision | `src/ProjectAegis.Delegation/Projection/` · `src/ProjectAegis.Delegation.UnityAdapter/Bridge/` · `unity/ProjectAegis/Assets/` (UI Toolkit + `C2PresentationController`) | [ADR-007](../../docs/architecture/adr-007-c2-map-presentation.md), [20](20-Command-And-Control-UI.md) |
| **Automation layer** | Scripting, batch analysis, orchestration, human-in-the-loop review | `production/agentic/` · `src/ProjectAegis.MissionEditor.Cli/` · MCP hosts (doc [07](07-Agentic-Infrastructure.md)) · `src/ProjectAegis.Delegation.UnityAdapter/Baltic/BalticBatchRunner.cs` | [07](07-Agentic-Infrastructure.md), [17](17-Replay-AAR-And-Order-Log.md) |

### Assembly dependency rule *(ADR-001, ADR-006)*

```
ProjectAegis.Data          (no UnityEngine; no Sim/Delegation)
    ↑
ProjectAegis.Sim           (no UnityEngine)
    ↑
ProjectAegis.Delegation    (orchestration, order log, projections)
    ↑
ProjectAegis.Delegation.UnityAdapter  (ISimWorldSnapshot, IOrderSink, harness)
    ↑
unity/ProjectAegis         (presentation only)
```

Highest-risk failure mode: uncontrolled complexity in equipment database and doctrine logic — architecture must prioritize auditable data and testable policy over UI polish.

### Interchange & integration requirements

- **Structured simulation API** — local-first, scriptable; headless and in-editor parity (`SimulationSession`, `BalticReplayHarness`)
- **Scenario import/export** — canonical `scenario.json` + policy sidecars (`ScenarioDocumentDto`, `ScenarioPolicyJsonLoader`)
- **External telemetry export** — order log, replay checkpoints, AAR artifacts ([17](17-Replay-AAR-And-Order-Log.md))
- **Separate release trains** — engine vs database versioning ([06](06-Database-Intelligence.md) `DbReleaseRecord`)
- **Deterministic stepping** — mandatory for replay, Monte Carlo, and agent-vs-agent ([ADR-003](../../docs/architecture/adr-003-order-log-schema.md), [ADR-004](../../docs/architecture/adr-004-tick-pipeline-order.md))

**Acceptance**

- [ ] **ARCH-0.1** Headless `BalticReplayHarness` and Unity `DelegationBridge` share `DelegationOrchestrator` + `SimTickPipeline` code path (no duplicate engage logic in UI).
- [ ] **ARCH-0.2** Scenario load binds `dbRef` / catalog snapshot before sim tick 0 (`ICatalogReader.TryResolveDbRef`).
- [ ] **ARCH-0.3** `OrderLogReplayFingerprint` stable across headless re-run for Baltic golden fixtures.
- [ ] **ARCH-0.4** `ProjectAegis.Sim` and `ProjectAegis.Data` compile with zero `UnityEngine` references.
- [ ] **ARCH-0.5** Post-P0: documented HTTP/gRPC sim API for external Python/RL co-simulation (P2).

## Functional Requirements

### 1. Simulation Core Agent

Deterministic world clock, fixed timestep, engagement phase, and headless batch execution.

- **Deterministic Simulation Engine**
  - Fixed timestep physics and logic updates (`SimTickRunner`, default Δt = 1/60 s)
  - Full support for time compression (`TimeCompressionMode`, `SimClock`)
  - Headless execution mode for agent-vs-agent batch simulations (`SimulationSession`, `BalticBatchRunner`)
  - Complete replay and save/load functionality with deterministic seeds (`SimSeed`, `ReplayCheckpointStore`)

- **Event Scheduling System**
  - Priority queue for future events (missile launches, sensor detections, etc.) — **post-P0**; MVP uses scenario timeline + mission runtime
  - Support for both real-time and accelerated execution

**P0 note:** `SimTickPipeline` wires core clock + engagement resolve (step 8); detection sub-hash mixed via `MixDetectionTick` before engage. Mission/event scheduling lives in `MissionRuntime` (Delegation) until unified sim event queue lands.

**Acceptance**

- [ ] **ARCH-1.1** `SimTickRunner.TickOnce` advances `SimClock.SimTick` by exactly 1 per pipeline pass; `SimTime = SimTick × FixedDeltaSeconds`.
- [ ] **ARCH-1.2** Same `SimSeed` + scenario policy JSON → identical `LastWorldHash` after N ticks in `SimTickPipelineTests` / Baltic golden harness.
- [ ] **ARCH-1.3** `TimeCompressionMode.HeadlessBatch` runs without Unity player loop (`BalticReplayHarness`, `BalticBatchRunner`).
- [ ] **ARCH-1.4** `ReplayCheckpointStore` records `(SimTick, WorldHash, LastSequenceId)` at configured interval (`ScenarioReplaySettings`).
- [ ] **ARCH-1.5** Post-P0: priority-queue event scheduler with deterministic tie-break (sorted event id).

### 2. Entity Component System (ECS) Layer

High-performance entity management and runtime archetypes.

- **High-Performance Entity Management**
  - Built on Unity DOTS (Entities, Components, Systems) per ADR-005 — **Unity layer**; sim rules stay pure C#
  - Support for 10,000–50,000+ entities with low memory footprint (target; MVP dictionary/registry model)
  - Burst-compiled systems for sensor, movement, and weapon logic (post-P0 hot paths)

- **Dynamic Entity Archetypes**
  - Aircraft, ships, submarines, drones, missiles, ground units, sensors, EW emitters
  - Runtime entity creation and destruction (critical for drone swarms)

**P0 note:** Battlespace entities are `TargetRegistry` + `UnitTarget` / `GroupTarget` (Delegation) with `SimEntityBinding` in UnityAdapter — not full DOTS archetypes yet. Catalog archetypes: `NearFutureArchetypeCatalog`, `CatalogArchetypeBinding`.

**Acceptance**

- [ ] **ARCH-2.1** No `UnityEngine` types in `ProjectAegis.Sim` entity or engage code paths.
- [ ] **ARCH-2.2** `TargetRegistry` supports runtime register/unregister without invalidating deterministic tick order.
- [ ] **ARCH-2.3** Swarm/deconflict path uses stable ordering (`SwarmSalvoDeconfliction`, sorted engage ids).
- [ ] **ARCH-2.4** Catalog near-future archetypes resolve via `NearFutureArchetypeRuntime` without Unity references.
- [ ] **ARCH-2.5** Post-P0: DOTS `Entity` store mirrors `ISimWorldSnapshot` with BlobAsset catalog bake (ADR-005).

### 3. Decision Engine Agent

Pluggable AI decision system and autonomy integration (see also [04](04-Agent-Delegation.md)).

- **Pluggable AI Decision System**
  - Core decision-making framework that different agent personalities plug into
  - Supports rule-based, utility-based, behavior-tree, and neural network agents
  - Agent personality profiles (`PersonalityCatalog`, `TraitVector`)

- **Autonomy Levels**
  - Full manual control → Semi-autonomous (agent suggests) → Full autonomous (agent executes)
  - `AutonomyLevel`, `AutonomyGate`, `SimulationModeProfile` (doc 03 / 04)

**P0 note:** `AgentController` + `DecisionPipeline` + `ScoredIntent` fingerprinting; policy gate via `RoePolicyAdapter` → `IPolicyEvaluator` in Sim.

**Acceptance**

- [ ] **ARCH-3.1** `DelegationOrchestrator.Tick` produces deterministic `ScoredIntent` ordering for same seed + snapshot (`ScoredIntentFingerprint`).
- [ ] **ARCH-3.2** `AgentController` and `HumanController` both implement `IController`; override via `OverrideService` yields immediately.
- [ ] **ARCH-3.3** `PolicySnapshotRegistry.Capture` binds `EffectivePolicy` at agent assign; denials log `PolicyDenialRecord` with `FireAbortReason`.
- [ ] **ARCH-3.4** `AutonomyGate` blocks high-risk orders per autonomy tier without bypassing `IPolicyEvaluator`.
- [ ] **ARCH-3.5** Post-P0: pluggable `IDecisionStrategy` module load (behavior tree / NN backends).

### 4. Physics & Sensing Agent

Sensor modeling, EW, and engagement resolution.

- **Advanced Sensor & Detection Modeling**
  - Multi-spectrum sensors (radar, IR, sonar, ESM, visual) — MVP PD loop + scenario trials
  - Line-of-sight, terrain masking, atmospheric effects — phased (doc 15)
  - Stealth signature and countermeasure modeling

- **Electronic Warfare & Cyber Layer**
  - Jamming, deception, and directed energy effects (`ScenarioJamResolver`, `ScenarioJammer`)
  - Cyber attack vectors on C4I systems (doc 19; comms delay + spoof timelines)

**P0 note:** `DeterministicDetectionLoop`, `PdDetectionContactSimulator`, `ScenarioContactSimulator`; engage via `MvpEngagementResolver`, `CombatDomainValidator`, `DlzEngageGate`.

**Acceptance**

- [ ] **ARCH-4.1** Detection rolls use `SeededRng` with `RngDomain` partition — no cross-domain draw reuse.
- [ ] **ARCH-4.2** `SimTickPipeline.MixDetectionTick` updates `DetectionSubhash` before engagement hash mix.
- [ ] **ARCH-4.3** `MvpEngagementResolver` is sole launch authority for headless engage (manual + agent orders funnel through `EngageRequest`).
- [ ] **ARCH-4.4** Policy denial precedes geometry: `PolicyEvaluator` / `RoePolicyAdapter` before `CombatDomainValidator` (ADR-002, ADR-004).
- [ ] **ARCH-4.5** Jam/spoof scenario fixtures (`baltic-patrol-jammed.policy.json`, `baltic-patrol-spoof.policy.json`) replay with stable contact/engage log fingerprints.

### 5. State Synchronization Agent

Serialization, replay, and reproducibility.

- **Game State Management**
  - Full serialization of simulation state for save/load and replay
  - Delta compression for future multiplayer synchronization
  - Versioned state for agent-vs-agent reproducibility

**P0 note:** MVP = append-only order log + periodic world hash checkpoints — not full world blob save. `DecisionLog` implements `IOrderLog`; migration to Sim-owned log tracked in ADR-003.

**Acceptance**

- [ ] **ARCH-5.1** `IOrderLog` append paths assign monotonic `sequenceId`; replay sorts unified timeline by sequence (ADR-003).
- [ ] **ARCH-5.2** `OrderLogReplayFingerprint` matches golden baselines in `ProjectAegis.Delegation.Tests` Baltic replay suite.
- [ ] **ARCH-5.3** `ReplayCheckpoint.WorldHash` equals `SimTickPipeline.LastWorldHash` at checkpoint tick when harness configured.
- [ ] **ARCH-5.4** `PlayerOrderRecord` preserves `ExecuteSimTick` for comms-delayed orders (req 19).
- [ ] **ARCH-5.5** Post-P0: full world-state snapshot blob + delta compression for netcode path.

## Non-Functional Requirements

- **Performance**
  - 60+ FPS in interactive mode with 5,000+ entities (Unity presentation budget)
  - 1000×+ simulation speed in headless agent-vs-agent mode (doc 03)
  - Efficient memory usage for large drone swarms

- **Determinism**
  - 100% reproducible results when using the same seed and inputs

- **Scalability**
  - Horizontal scaling support for future cloud-based batch simulations

- **Maintainability**
  - Clear separation of concerns so AI agents can modify individual systems without breaking others

**Acceptance**

- [ ] **ARCH-NFR-1** Headless Baltic harness completes 300-tick replay under CI time budget (existing golden tests).
- [ ] **ARCH-NFR-2** Ban `DateTime.Now` / `Time.realtimeSinceStartup` in `ProjectAegis.Sim` (analyzer or review gate).
- [ ] **ARCH-NFR-3** `gitnexus impact` run before moving symbols across Sim ↔ Delegation ↔ Data boundaries.
- [ ] **ARCH-NFR-4** Post-P0: 10k entity soak with LOD degradation path documented in performance agent (doc 07).

## Agentic Capabilities

- Every major system exposes **MCP tools** (via Unity-MCP and `ProjectAegis.MissionEditor.Cli`) so Claude/Cursor can:
  - Inspect and modify entity archetypes (catalog read/write gate — doc 06)
  - Tune sensor and weapon parameters in real time (staging batches only)
  - Create new decision logic or agent personalities (Delegation traits)
  - Run high-speed simulation batches and analyze results (`BalticBatchRunner`, Hindsight hooks)
  - Debug and balance systems interactively

- Custom C# attributes allow rapid creation of new MCP tools without boilerplate (doc 07)

**P0 guardrail:** MCP and headless CLI share the same validation and catalog APIs — no shadow write path.

## Technical Considerations

- **Primary Technology Stack**
  - Unity 6 + DOTS (Entities 1.3+, Burst, Jobs) for presentation and future world store
  - Unity-MCP for live AI control
  - Pure C# tick pipeline (`SimTickPipeline`, `DelegationOrchestrator`) for headless parity

- **Future-Proofing**
  - Custom deterministic lockstep first; Netcode for Entities when multiplayer is in scope
  - Modular design allows swapping physics or sensor backends

- **Integration with Other Layers**
  - Tightly coupled with Database Intelligence Layer ([06](06-Database-Intelligence.md)) for consistent unit data
  - Exposes hooks for Dynamic Speculative Systems ([05](05-Dynamic-Systems-Agent.md)) via catalog gates

## Cross-Domain Traceability

| Domain doc | Architecture touchpoint | Key types / paths |
|------------|-------------------------|-------------------|
| [03 Simulation modes](03-Simulation-Modes.md) | `SimulationModeConfigurator`, `SimulationModeKind`, headless AvA | `src/ProjectAegis.Delegation/Orchestration/` |
| [04 Agent delegation](04-Agent-Delegation.md) | Decision engine layer | `DelegationOrchestrator`, `AgentController`, `DecisionPipeline` |
| [06 Database intelligence](06-Database-Intelligence.md) | Entity database layer | `ICatalogReader`, `IWriteGate`, `data/catalog/` |
| [07 Agentic infrastructure](07-Agentic-Infrastructure.md) | Automation layer | `BalticBatchRunner`, `MissionEditor.Cli`, `production/agentic/` |
| [11 Mission editor](11-Agentic-Mission-Editor.md) | Scenario system | `ScenarioDocumentDto`, `ScenarioValidationEngine` |
| [13 Doctrine / ROE](13-Doctrine-ROE-EMCON-WRA.md) | Sim policy phase | `PolicyEvaluator`, `IPolicyEvaluator`, `ScenarioPolicyProfile` |
| [14 Engagement](14-Engagement-And-Fire-Control.md) | Sim engage phase | `IEngagementResolver`, `MvpEngagementResolver`, `DlzEngageGate` |
| [15 Sensors / EW](15-Sensor-Detection-And-EW.md) | Detection tick | `DeterministicDetectionLoop`, `PdDetectionContactSimulator` |
| [16 Logistics](16-Logistics-And-Magazines.md) | Magazine / fuel | `MagazineLedger`, `FuelLedger`, `FuelTimelineTracker` |
| [17 Replay / AAR](17-Replay-AAR-And-Order-Log.md) | Order log + checkpoints | `DecisionLog`, `IOrderLog`, `ReplayCheckpointStore` |
| [19 Cyber / comms](19-Cyber-And-Comms.md) | Order delay + spoof | `CommsOrderDelay`, `SpoofTrackTimelineSimulator` |
| [20 C2 UI](20-Command-And-Control-UI.md) | UX projections | `*Projection`, `*PanelBinder`, `unity/.../C2PresentationController.cs` |

**GDD:** [simulation-core-time.md](../../design/gdd/simulation-core-time.md) implements ARCH-1.x; [order-log-replay.md](../../design/gdd/order-log-replay.md) implements ARCH-5.x.

## Open Questions / Decisions Needed

All charter questions for agentic architecture are **locked** for Sprint 15 design review. See [Resolved Design Decisions](#resolved-design-decisions) and ADR-001–006. No reopen without user approval.

| Former open question | Resolution location |
|---------------------|---------------------|
| Netcode from day one? | [§1 Multiplayer sequencing](#1-multiplayer-sequencing) · ADR-001 |
| Max entity count v1.0? | [§2 Entity scale targets](#2-entity-scale-targets) |
| External agent co-simulation? | [§3 External agent API](#3-external-agent-api) |
| Burst vs managed C#? | [§4 Hot-path language split](#4-hot-path-language-split) · ADR-005 |
| ECS vs dictionary MVP? | [§5 World-state hosting](#5-world-state-hosting) · ADR-005 |
| Order log ownership? | [§6 Order log boundary](#6-order-log-boundary) · ADR-003 |

## Implementation Mapping (headless-first)

| Requirement area | Assembly / path | Key types |
|------------------|-----------------|-----------|
| **Sim kernel — time** | `ProjectAegis.Sim` · `Core/`, `Time/` | `ISimTickRunner`, `SimTickRunner`, `SimTickPipeline`, `SimClock`, `SimSeed`, `SeededRng`, `SimWorldHash`, `TimeCompressionMode` |
| **Sim kernel — policy** | `ProjectAegis.Sim` · `Policy/` | `IPolicyEvaluator`, `PolicyEvaluator`, `PassthroughPolicyEvaluator`, `PolicyContext`, `EffectivePolicy`, `ResolvedUnitPolicy`, `FireAbortReason`, `RoeLevel`, `EmconState` |
| **Sim kernel — engage** | `ProjectAegis.Sim` · `Engage/` | `IEngagementResolver`, `MvpEngagementResolver`, `EngageRequest`, `EngageContext`, `CombatDomainValidator`, `DlzEngageGate`, `MagazineLedger`, `KilledTargetRegistry`, `SwarmSalvoDeconfliction` |
| **Sim kernel — sensors** | `ProjectAegis.Sim` · `Sensors/` | `DeterministicDetectionLoop`, `PdDetectionContactSimulator`, `DetectionWorldHash`, `ContactLifecycleState`, `ScenarioContactSimulator` |
| **Sim kernel — logistics** | `ProjectAegis.Sim` · `Logistics/` | `FuelLedger` |
| **Sim kernel — scenario policy** | `ProjectAegis.Sim` · `Scenario/` | `ScenarioPolicyRepository`, `ScenarioPolicyJsonLoader`, `ScenarioPolicyProfile`, `ScenarioMissionTimeline`, `ScenarioEngageDefaults` |
| **Delegation — orchestration** | `ProjectAegis.Delegation` · `Orchestration/` | `DelegationOrchestrator`, `SimulationSession`, `AutonomyGate`, `OverrideService`, `PolicySnapshotRegistry`, `SimulationModeConfigurator` |
| **Delegation — controllers** | `ProjectAegis.Delegation` · `Controllers/`, `Decision/` | `IController`, `AgentController`, `HumanController`, `DecisionPipeline`, `DecisionLog`, `IOrderLog`, `PlayerOrderExecutionQueue` |
| **Delegation — targets** | `ProjectAegis.Delegation` · `Targets/`, `Groups/` | `ICommandableTarget`, `UnitTarget`, `GroupTarget`, `DetachRejoinService` |
| **Delegation — replay** | `ProjectAegis.Delegation` · `Replay/` | `ReplayCheckpointStore`, `ReplayCheckpoint`, `OrderLogReplayFingerprint` |
| **Delegation — projections (C2 DTOs)** | `ProjectAegis.Delegation` · `Projection/` | `MessageLogProjection`, `MapPictureProjection`, `EngagePreviewProjection`, `UnitDetailProjection`, `SensorC2Projection` |
| **Bridge / harness** | `ProjectAegis.Delegation.UnityAdapter` · `Bridge/`, `Baltic/` | `ISimWorldSnapshot`, `IOrderSink`, `DelegationBridge`, `BalticReplayHarness`, `ObservedStateBuilder`, `TargetRegistry` |
| **Data — catalog** | `ProjectAegis.Data` · `Catalog/`, `WriteGate/`, `Snapshots/` | `ICatalogReader`, `SqliteCatalogReader`, `CatalogReaderFactory`, `IWriteGate`, `CatalogWriteGate`, `DbSnapshotStore` |
| **Data — scenario** | `ProjectAegis.Data` · `Scenario/`, `Validation/` | `ScenarioDocumentDto`, `ScenarioValidationEngine`, `ValidationReport`, `ReachabilityCalculator` |
| **Automation CLI** | `ProjectAegis.MissionEditor.Cli` | `ScenarioValidationExportGate`, `CatalogIntelligenceRunCommand`, `BalticBatchRunner` (via adapter) |
| **Unity presentation** | `unity/ProjectAegis/Assets/Scripts/Runtime/` | `C2PresentationController`, `DelegationBridgeHost`, `RightUnitPanelHost` |

**Default scenario policy path:** `data/scenarios/baltic-patrol.policy.json` (and variants) via `ScenarioPolicyRepository`.

**Tests:** `src/ProjectAegis.Sim.Tests/`, `src/ProjectAegis.Delegation.Tests/`, `src/ProjectAegis.Delegation.UnityAdapter.Tests/` (replay goldens), `src/ProjectAegis.Data.Tests/`.

## Resolved Design Decisions

Decisions locked **2026-06-04** for Sprint 15 design review. Architecture ADRs **Accepted** unless noted Proposed.

### 1. Multiplayer sequencing

**Decision:** **Custom deterministic lockstep first**; Netcode for Entities when multiplayer is in scope.

| Concern | ADR | Implication |
|---------|-----|-------------|
| Sim vs Delegation split | [ADR-001](../../docs/architecture/adr-001-sim-assembly-boundary.md) | World truth in Sim; orders via Delegation |
| Tick ordering | [ADR-004](../../docs/architecture/adr-004-tick-pipeline-order.md) | Same 1–11 step order interactive and headless |
| Presentation | [ADR-007](../../docs/architecture/adr-007-c2-map-presentation.md) | UI read-only; commands only through orchestrator |

### 2. Entity scale targets

**Decision:** **10,000 entities** v1.0 target; **500/swarm** with LOD degradation path to **25k headless**.

| Mode | Target | MVP path |
|------|--------|----------|
| Interactive C2 | 5,000+ @ 60 FPS | Projection LOD; map symbol culling (doc 20) |
| Headless AvA | 25,000 @ 1000×+ | Batch ticks, no render, `TimeCompressionMode.HeadlessBatch` |
| Swarm engagements | 500 coordinated | `SwarmSalvoDeconfliction`, sorted engage ids |

### 3. External agent API

**Decision:** **Yes (P2)** — structured sim API + seed control for Python/RL agents.

| Phase | Surface |
|-------|---------|
| P0 | `BalticReplayHarness`, `SimulationSession`, order log export |
| P1 | CLI batch + CSV (`LossesScoringCsvExporter`) |
| P2 | Stable RPC/HTTP contract mirroring `ISimWorldSnapshot` + `IOrderSink` |

### 4. Hot-path language split

**Decision (revised 2026-07-07):** **Managed data-oriented C# for all hot paths.** The original
"Burst for hot paths" plan is **superseded** (ADR-005 reversed): the heaviest path (25k @ 1000×
headless, §2) runs under `dotnet`, where Burst/Jobs do not exist, so hot-path throughput must come
from managed parallelism (struct-of-arrays state, order-stable parallel reductions, optional
`System.Numerics` SIMD), measured by the INF-5.1 benchmark. Policy, agents, and I/O stay managed.
World state is **not** in DOTS/ECS. See
[unity integration review](../../docs/reports/unity-integration-review-2026-07-07.md) §3.

| Layer | Language | ADR |
|-------|----------|-----|
| Detection / engage math (future ECS systems) | Burst | [ADR-005](../../docs/architecture/adr-005-dots-sim-core.md) |
| Policy, delegation, order log | Managed pure C# | ADR-001, ADR-002 |
| Catalog, validation | Managed pure C# | [ADR-006](../../docs/architecture/adr-006-data-layer-boundary.md) |

### 5. World-state hosting

**Decision:** **Dual track** — pure C# sim rules and hashes today; DOTS entity store for scale per ADR-005.

| Now (P0) | Post-P0 |
|----------|---------|
| `TargetRegistry`, `ObservedState` / `PerceivedState` | Unity ECS components + BlobAssets from catalog |
| `SimWorldHash` / `DetectionWorldHash` | Same hash contracts fed from ECS systems |

### 6. Order log boundary

**Decision:** Unified append-only log in Delegation (`DecisionLog` : `IOrderLog`); schema locked in ADR-003.

| Entry families | Types |
|----------------|-------|
| Agent / player | `AgentDecisionPayload`, `PlayerOrderRecord` |
| Engage / policy | `EngagementRecord`, `PolicyDenialRecord`, `PolicyUpdateRecord` |
| Contacts / logistics | `ContactChangeRecord`, `MagazineChangeRecord`, `FuelBurnRecord` |

**Migration note:** Future `ProjectAegis.Sim.Log` assembly optional; single writer until migration ADR (see `architecture.md`).

### 7. P0 scope boundary (explicit deferrals)

| In P0 (evidence in repo) | Deferred |
|--------------------------|----------|
| `ProjectAegis.Sim` tick + engage + detection hash | Full ECS entity store (ADR-005 implementation) |
| `SimTickPipeline` + `SimulationSession` headless | Priority-queue global event scheduler |
| `IPolicyEvaluator` in Sim + `RoePolicyAdapter` | Full EMCON→sensor emission loop (partial) |
| `DecisionLog` + replay goldens | Full world blob save/load |
| Catalog/scenario binding via Data | External RL HTTP API (P2) |
| C2 projections + Unity smoke | 10k entity performance proof |
| Baltic harness + batch runner | Cloud simulation farm |

---

## Traceability

| Epic / FR | This document |
|-----------|---------------|
| FR-07 ([01](01-Project-Overview.md)) | In-simulation agent architecture — five layers + ARCH-* acceptance |
| Doc 03 | Modes, compression, headless AvA — ARCH-1.x, ARCH-NFR-* |
| Doc 04 | Decision engine — ARCH-3.x |
| Doc 06 | Database layer row in five-layer table |
| Doc 07 | Automation layer row |
| Doc 17 | Order log / replay — ARCH-5.x |
| Master architecture | [architecture.md](../../docs/architecture/architecture.md), [architecture-traceability-index.md](../../docs/architecture/architecture-traceability-index.md) |
| ADR-001–006 | [Resolved Design Decisions](#resolved-design-decisions) |

---

**Status:** Locked (Sprint 15)