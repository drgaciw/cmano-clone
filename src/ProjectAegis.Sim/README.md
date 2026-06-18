# ProjectAegis.Sim — Deterministic Simulation Core

Pure C# simulation contracts and the fixed-timestep tick runner for Project Aegis.
**No `UnityEngine` dependency** — the assembly targets `net8.0;netstandard2.1` so it
runs identically headless (CI, replay, the delegation demo) and inside the Unity
runtime. It references `ProjectAegis.Data` for catalog/scenario data but never the
other way around (the sim consumes the snapshots the data layer produces).

This layer owns *how the world advances one tick*: detection, policy, engagement,
logistics, and the world-state hash that proves a run is reproducible. The agent
brains live in [`ProjectAegis.Delegation`](../ProjectAegis.Delegation/README.md);
the authoritative catalog/scenario data lives in
[`ProjectAegis.Data`](../ProjectAegis.Data/README.md).

## Core invariant: determinism

**The simulation is a pure function of `(scenario seed, observed state, tick)`.**
Re-running the same scenario with the same seed must produce a byte-identical
order log and world-state hash. Two rules enforce this:

1. **No wall-clock, no ambient randomness.** Time advances only via
   `SimClock.AdvanceOneTick()` (a `ulong SimTick` counter); randomness comes only
   from `SeededRng`. Nothing in the tick path reads `DateTime.UtcNow` or
   `System.Random`.
2. **Order-independent iteration.** Subsystems sort their inputs by stable keys
   (e.g. `DeterministicDetectionLoop` sorts trials by
   `observer → sensor → target` with `StringComparer.Ordinal`) before drawing
   RNG, so results never depend on dictionary/insertion order.

CI pins golden world-state hashes; if you intentionally change state shape or the
hash layering, re-run the matching golden test and update the pinned constant.
See [`/replay-verify`](../../.claude/skills/replay-verify/SKILL.md) and the
[determinism audit](../../.claude/skills/determinism-audit/SKILL.md) skills.

### Seeded RNG

`SeededRng.UnitFloat(seed, domain, entityId, simTick, drawIndex)` returns a
deterministic `double` in `[0, 1)` by hashing all five inputs. Because the draw is
addressed by `(domain, entityId, tick, drawIndex)` rather than a mutable stream,
subsystems can draw independently without a shared cursor and still stay
reproducible.

- `SimSeed` — the global `ulong` scenario seed (`SimSeed.FromScenario(...)`).
- `RngDomain` — separates draw streams so unrelated subsystems never collide:
  `Detection`, `Engage`, `AgentDecision`, `Logistics`, `Combat`.

### World-state hash

`SimWorldHash` combines per-phase sub-hashes into one `ulong` using tagged layers
(`LayerCore`, `LayerDetection`, `LayerEngage`, `LayerCombatOutcome`).
`SimTickPipeline.LastWorldHash` is recomputed each tick from the core hash, the
detection sub-hash, the engagement mix, and the kill-registry mix — this single
value is what golden replay tests compare.

## Tick pipeline

The canonical fixed-timestep order is defined in
[ADR-004](../../docs/architecture/adr-004-tick-pipeline-order.md) and
`architecture.md` § *Fixed Timestep Tick Pipeline*. The same code path runs in
interactive and headless modes:

| Step | System | Output | Where |
|------|--------|--------|-------|
| 1 | Ingest player/MCP commands | Command queue | host |
| 2 | Apply mission timeline / events | Mission state | `Scenario/` |
| 3 | Movement & kinematics (coarse) | Positions | host |
| 4 | **Detection tick** (sorted emitter–target pairs) | Contact changes | `Sensors/DeterministicDetectionLoop` |
| 5 | Build `ObservedState` / snapshot | Per-side picture | Delegation adapter |
| 6 | **Delegation tick** | `Order` list | `ProjectAegis.Delegation` |
| 7 | **Policy evaluate** each order/intent | Allow / `FireAbortReason` | `Policy/IPolicyEvaluator` |
| 8 | **Engagement resolve** legal fires | Launches, damage | `Engage/IEngagementResolver` |
| 9 | Logistics (fuel, magazine) | Readiness | `Logistics/`, `Engage/MagazineLedger` |
| 10 | Append order log | Immutable entries | host / Delegation |
| 11 | Optional UI snapshot | View models | host |

`SimTickPipeline` is the ADR-004 runner with the engagement phase (step 8) wired:

```csharp
var pipeline = new SimTickPipeline(
    SimSeed.FromScenario(scenarioSeed),
    engagement: new MvpEngagementResolver(world, magazines),
    fixedDeltaSeconds: 1.0 / 60.0);

// Step 4: feed detection results so they fold into the world hash.
pipeline.MixDetectionTick(DeterministicDetectionLoop.RollTick(
    pipeline.Seed, pipeline.Clock.SimTick, trials, unitRadarEmcon));

// Step 8: queue intents, then advance one tick.
pipeline.EnqueueEngagement(in engageRequest);
pipeline.TickOnce(TimeCompressionMode.HeadlessBatch);

ulong hash = pipeline.LastWorldHash;       // golden-replay comparison point
var results = pipeline.LastEngagementResults;
```

`SimTickRunner` is the minimal core (clock + placeholder world hash) that
`SimTickPipeline` wraps; both implement `ISimTickRunner`.

## Subsystem map

| Directory | Responsibility | Key entry points |
|-----------|----------------|------------------|
| `Core/` | Tick runners, seed/RNG, world hash. | `ISimTickRunner`, `SimTickPipeline`, `SimTickRunner`, `SeededRng`, `RngDomain`, `SimSeed`, `SimWorldHash` |
| `Time/` | Fixed-step clock and compression mode. | `SimClock` (`AdvanceOneTick`, `SimTick`, `SimTime`), `TimeCompressionMode` (`RealTime`/`Accelerated`/`HeadlessBatch`) |
| `Sensors/` | Tick-4 deterministic detection: Pd, EMCON gating, jamming, contact lifecycle. | `DeterministicDetectionLoop`, `DetectionProbability`, `DetectionRollResult`, `ScenarioJamResolver`, `ContactLifecycleState`, `DetectionWorldHash` |
| `Policy/` | Tick-7 ROE/WRA evaluation (ADR-002). | `IPolicyEvaluator`, `PolicyEvaluator`, `PolicyContext`, `PolicyVerdict`, `EffectivePolicy`, `RoeLevel`, `EmconState`, `FireAbortReason` |
| `Engage/` | Tick-8 engagement: fire-control, envelope/DLZ, magazine, combat domains, Pk outcome. | `IEngagementResolver`, `MvpEngagementResolver`, `EngageRequest`/`EngageResult`, `WeaponEnvelope`, `DlzEvaluator`, `MagazineLedger`, `CombatDomainValidator`, `CombatOutcomeResolver`, `KilledTargetRegistry` |
| `Logistics/` | Tick-9 deterministic fuel burn and JOKER/BINGO bands. | `FuelLedger` (`AdvanceTick`, `ResolveBand`) |
| `Scenario/` | Scenario-derived sim inputs: policy profiles, detection trials, EMCON, jammers, speculative settings. | `ScenarioPolicyRepository`, `ScenarioPolicyJsonLoader`, `ScenarioDetectionTrial`, `ScenarioEmconResolver`, `SpeculativeEngageGate`, `DetectionTrialResolver` |
| `Doctrine/` | Technology-maturity tagging for speculative systems (req 09–10). | `TechnologyMaturityTag` (`Simulated`/`Prototype`/`Production`) |
| `Glossary/` | req-12 stable abort/outcome log codes from a JSON manifest. | `AbortReasonManifest`, `AbortReasonCatalog.Generated` |
| `Polyfills/` | `netstandard2.1` shim (`IsExternalInit` for records). | — |

## Engagement abort/outcome semantics

`MvpEngagementResolver.Resolve` short-circuits to `EngageResult.Aborted(reason)`
at the first failed gate, in this order: already-killed target → no
fire-control track → speculative-systems gate → policy (ROE/WRA) → air ops not
ready → track spoofed → EMCON off → magazine empty → combat-domain validator
([ADR-009](../../docs/architecture/adr-009-combat-domain-validators.md)) →
hypersonic gate → weapon envelope → DLZ → salvo consumption. Only after every
gate passes does it `Launch` and apply the `CombatOutcomeResolver` Pk chain
(hit → intercept-on-hit → kill-on-hit). `EngagementAbortReason` /
`EngagementOutcomeCodes` map to stable log codes via the `Glossary/` manifest so
the order log stays diffable across runs.

## Build and test

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download). From the repo root:

```bash
dotnet build src/ProjectAegis.Sim/ProjectAegis.Sim.csproj
dotnet test  src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj -v minimal
```

`src/ProjectAegis.Sim.Tests/` mirrors this layout (`Core/`, `Engage/`, `Sensors/`,
`Policy/`, `Logistics/`, `Scenario/`, `Glossary/`) and includes the golden
world-hash and replay-determinism assertions.

## Cross-cutting rules

- **Determinism is non-negotiable.** Do not introduce `DateTime.UtcNow`,
  `System.Random`, `Guid.NewGuid()`, or order-dependent iteration into the tick
  path. Use `SimClock`, `SeededRng`, and stable key ordering.
- **No Hindsight recall/reflect inside `Tick()`** — session memory is a dev-time
  tool, not a runtime input (see `AGENTS.md`).
- **Advisory data stays advisory.** The sim reads committed catalog snapshots;
  it never mutates the catalog (that path is the `ProjectAegis.Data` write gate).

## See also

- [ADR-001 — sim assembly boundary](../../docs/architecture/adr-001-sim-assembly-boundary.md)
- [ADR-002 — policy evaluator](../../docs/architecture/adr-002-policy-evaluator.md)
- [ADR-003 — order log schema](../../docs/architecture/adr-003-order-log-schema.md)
- [ADR-004 — tick pipeline order](../../docs/architecture/adr-004-tick-pipeline-order.md)
- [ADR-005 — DOTS sim core](../../docs/architecture/adr-005-dots-sim-core.md)
- [ADR-009 — combat domain validators](../../docs/architecture/adr-009-combat-domain-validators.md)
- [ProjectAegis.Delegation — agent delegation framework](../ProjectAegis.Delegation/README.md)
- [ProjectAegis.Data — database intelligence layer](../ProjectAegis.Data/README.md)
- [Architecture overview](../../docs/architecture/architecture.md)
