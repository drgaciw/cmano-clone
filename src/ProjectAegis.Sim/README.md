# ProjectAegis.Sim

Pure C# **simulation core** — the deterministic tick pipeline, sensors/detection, engagement
resolution, and policy evaluation that sit behind the delegation framework. No `UnityEngine`
reference (targets `net8.0;netstandard2.1`, depends only on `ProjectAegis.Data`), so every
subsystem is exercised headless with `dotnet test`.

> **Determinism is the load-bearing invariant.** Everything here must be a pure function of
> `(scenario, seed)` — no `DateTime.UtcNow`, `Random.Shared`, `Guid.NewGuid()`, or
> unordered-collection iteration in the tick path. For the *why*, the hashing model, and the
> golden-regeneration workflow, read the
> [determinism & replay developer guide](../../docs/engineering/determinism-and-replay.md).

---

## Subsystem map

| Folder | Purpose | Key types |
|--------|---------|-----------|
| `Core/` | Tick pipeline, seeded RNG, world-state hashing, clock seed | `SimTickPipeline`, `SimTickRunner`, `ISimTickRunner`, `SimSeed`, `SeededRng`, `RngDomain`, `SimWorldHash` |
| `Sensors/` | Tick-4 deterministic detection loop + detection sub-hash + hostile classification | `DeterministicDetectionLoop`, `DetectionWorldHash`, `PdDetectionContactSimulator`, `ContactLifecycleState`, `DatalinkSidePictureMerger`, `HostileContactFilter` |
| `Engage/` | Tick-8 engagement pipeline: policy → track → envelope/DLZ → magazine → combat outcome | `IEngagementResolver`, `MvpEngagementResolver`, `EngageRequest`/`EngageResult`, `DlzEvaluator`, `MagazineLedger`, `CombatOutcomeResolver`, `DomainValidatorRegistry` |
| `Policy/` | Per-unit policy evaluation at the orchestrator boundary (ADR-002) | `IPolicyEvaluator`, `PolicyEvaluator`, `EffectivePolicy`, `PolicyContext`, `RoeLevel`, `EmconState` |
| `Scenario/` | Scenario/policy profiles + JSON repository, comms/EMCON/mission triggers, blue/red side registry | `ScenarioPolicyProfile`, `ScenarioPolicyRepository`, `ScenarioMissionContactTrigger`, `DetectionTrialResolver`, `BalticV3SideRegistry` |
| `Catalog/` | Hot-tick appliers bridging `ProjectAegis.Data` catalog values into sim state | `CatalogDamageHotTickApplier`, `CatalogMagazineResolver`, `CatalogRadarEmconResolver`, `PlatformHpLedger` |
| `Logistics/` | Fuel accounting | `FuelLedger` |
| `Time/` | Fixed-timestep clock + time-compression | `SimClock`, `TimeCompressionMode` |
| `Glossary/` | Generated abort-reason catalog/manifest ([guide](../../docs/engineering/abort-reason-catalog.md)) | `AbortReasonManifest`, `AbortReasonCatalog` |

---

## The deterministic tick

`SimTickPipeline` (ADR-004) advances the clock, resolves queued engagements, folds each
phase into a single world-state hash, and exposes a detection sub-hash:

```text
TickOnce(mode)
  → SimTickRunner.TickOnce   (clock advance, core hash)
  → resolve pending EngageRequests via IEngagementResolver
  → RecomputeWorldHash: SimWorldHash.Combine(core, detection, engageMix, killMix)
```

Detection is mixed separately (tick step 4) so the harness can pin it independently:

```csharp
pipeline.MixDetectionTick(detectionRolls);   // updates DetectionSubhash + LastWorldHash
pipeline.EnqueueEngagement(request);
pipeline.TickOnce(TimeCompressionMode.Normal);
ulong worldHash = pipeline.LastWorldHash;     // pinned by replay goldens
```

### World-state hash layers

[`SimWorldHash`](Core/SimWorldHash.cs) folds the tick into one `ulong` in a fixed layer order
(`core → detection → engage → combat-outcome`, tags `1..4`). The fold is a fixed
integer-mix (SplitMix64-style constants), so the value depends only on the layered inputs —
never on floating-point round-trip noise or enumeration order. `DetectionWorldHash.MixTick`
quantizes `Pd`/draw to integers (`× 10_000`) before mixing for the same reason.

### Seeded RNG — pick the right domain

There is no ambient randomness. [`SeededRng.UnitFloat`](Core/SeededRng.cs) returns a unit
float in `[0,1)` as a **stateless** pure function of `(SimSeed, RngDomain, entityId, simTick,
drawIndex)`:

```csharp
double pd = SeededRng.UnitFloat(seed, RngDomain.Detection, observerId, simTick, drawIndex: 0);
```

Because it is stateless, the *value* of a draw does not depend on call order — but `domain`
and `drawIndex` do. Always select the correct [`RngDomain`](Core/RngDomain.cs)
(`Detection`, `Engage`, `AgentDecision`, `Logistics`, `Combat`, `MineHazard`) so unrelated
subsystems can never alias one another's draw stream. `SimSeed` is the single global scenario
seed (`SimSeed.FromScenario(...)`); subsystems derive from it via domain + entity id.

> Draw-order stability only matters for the *stateful* delegation-side RNG
> (`ProjectAegis.Delegation.Decision.SeededRng`), not for this coordinate-addressed one — see
> the [determinism guide](../../docs/engineering/determinism-and-replay.md#seeded-rng).

---

## Engagement pipeline

`IEngagementResolver.Resolve(in EngageRequest)` is the tick-8 seam.
[`MvpEngagementResolver`](Engage/MvpEngagementResolver.cs) is the production path: it applies
the resolved `EffectivePolicy`, checks the weapon envelope / dynamic launch zone
(`DlzEvaluator`), consumes magazines (`MagazineLedger`), validates the combat domain
(`DomainValidatorRegistry`, ADR-009), and records kills in `KilledTargetRegistry` (which
contributes the `killMix` layer of the world hash). `EngageResult` reports `Launched`, the
monotonic `EngagementId`, an `OutcomeCode`, or an `EngagementAbortReason` — the abort reason
is turned into a stable order-log/message-log code via the manifest-driven
[abort-reason catalog](../../docs/engineering/abort-reason-catalog.md). Test/fixture
resolvers (`StubEngagementResolver`, `RecordingEngagementResolver`) implement the same
interface for isolation. The **exact ordered gate chain**, the `EngageContext` input surface,
the DLZ personality table, the three-draw combat-outcome fold, and how to add a gate/validator
without breaking combat goldens are documented in the
[engagement / kill-chain pipeline guide](../../docs/engineering/engagement-pipeline.md).

## Policy evaluation

[`IPolicyEvaluator`](Policy/IPolicyEvaluator.cs) (ADR-002) replaces the old ROE filter at the
orchestrator boundary. It resolves an `EffectivePolicy` `(RoeLevel, MaxSalvo)` per unit from
`PolicyContext` + `ActionRequest` and returns a `PolicyVerdict`. Scenario-driven values load
from `data/scenarios/*.policy.json` via
[`ScenarioPolicyRepository`](Scenario/ScenarioPolicyRepository.cs) — gameplay numbers live in
data, not in C# constants.

## Blue/red side resolution

Headless Baltic runs decide "friend or foe" through
[`BalticV3SideRegistry`](Scenario/BalticV3SideRegistry.cs) — a thread-safe static that layers
**scenario-scoped registrations** (added per run for catalog/joint ORBAT units) over a
**legacy default map** (`u1`/`ucav-blue` → blue, `hostile-1`/`ucav-red` → red; unknown ids →
no side). [`HostileContactFilter`](Sensors/HostileContactFilter.cs) and the engage-target
fallback both read it instead of guessing from id prefixes. The full lifecycle (how the batch
harness registers `gauntlet.units[]` and clears state after every run) is documented in the
[QA Gauntlet runbook → *Side resolution (joint ORBAT)*](../../docs/engineering/qa-gauntlet.md#side-resolution--joint-orbat).

---

## Build & test

```bash
dotnet build src/ProjectAegis.Sim/ProjectAegis.Sim.csproj
dotnet test  src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj -v minimal
```

`ProjectAegis.Sim.Tests` mirrors this folder layout (`Core/`, `Sensors/`, `Engage/`,
`Policy/`, `Scenario/`, …) and is part of the ≥1638-test solution baseline. The replay
goldens that assert reproducibility of this core live in
[`tests/regression/`](../../tests/regression/) and are driven by the
[`ProjectAegis.Delegation.Demo`](../ProjectAegis.Delegation.Demo/README.md) harness.

## See also

| Topic | Doc |
|-------|-----|
| Determinism rules, hashing, golden workflow | [`docs/engineering/determinism-and-replay.md`](../../docs/engineering/determinism-and-replay.md) |
| Engage/kill-chain gate chain + combat outcome | [`docs/engineering/engagement-pipeline.md`](../../docs/engineering/engagement-pipeline.md) |
| Abort-reason codes (manifest → codegen → order log) | [`docs/engineering/abort-reason-catalog.md`](../../docs/engineering/abort-reason-catalog.md) |
| Tick pipeline order + world-hash layers | [`adr-004-tick-pipeline-order.md`](../../docs/architecture/adr-004-tick-pipeline-order.md) |
| Policy evaluator boundary | [`adr-002-policy-evaluator.md`](../../docs/architecture/adr-002-policy-evaluator.md) |
| Sim assembly boundary (no Unity) | [`adr-001-sim-assembly-boundary.md`](../../docs/architecture/adr-001-sim-assembly-boundary.md) |
| Combat domain validators | [`adr-009-combat-domain-validators.md`](../../docs/architecture/adr-009-combat-domain-validators.md) |
| Data / catalog layer (this core's only dependency) | [`../ProjectAegis.Data/README.md`](../ProjectAegis.Data/README.md) |
| Delegation framework (consumer of this core) | [`../ProjectAegis.Delegation/README.md`](../ProjectAegis.Delegation/README.md) |
| Replay harness / golden regeneration | [`../ProjectAegis.Delegation.Demo/README.md`](../ProjectAegis.Delegation.Demo/README.md) |
| Hard invariants + verification block | [`AGENTS.md`](../../AGENTS.md#hard-invariants--never-break-these) |
