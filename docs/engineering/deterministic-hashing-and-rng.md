# Deterministic hashing & RNG core — developer guide

The whole simulation is a **pure function of `(scenario, seed)`**, and that guarantee rests on a
small set of primitives in `ProjectAegis.Sim/Core/` and `ProjectAegis.Delegation/Core/`: the two
seeded RNGs, the global seed value type, the layered world-state hash, and the cross-process
string hash. Almost every runtime guide references these (`RngDomain.Detection`, `RngDomain.Combat`,
`SimWorldHash.Combine`, per-agent `SeededRng`), but this page is the one that documents them **as a
subsystem** — what each primitive is, which one to reach for, the on-disk-contract fields that feed
the replay goldens, and how to extend any of them without moving the Baltic v2 hash.

This is the *building-block* reference. The **rules, golden workflow, and pitfalls** live in
[determinism-and-replay.md](determinism-and-replay.md); the *order-log fingerprint* (the other half
of the reproducibility story) is [order-log-runtime.md](order-log-runtime.md); the tick order that
folds these hashes is [ADR-004](../architecture/adr-004-tick-pipeline-order.md). This guide is
verified against source and pinned by the tests listed at the end.

- **Global seed:** [`SimSeed`](../../src/ProjectAegis.Sim/Core/SimSeed.cs) — `readonly record struct`
  wrapping one `ulong` (`SimSeed.FromScenario(...)`).
- **Stateless sim RNG:** [`SeededRng.UnitFloat`](../../src/ProjectAegis.Sim/Core/SeededRng.cs) +
  [`RngDomain`](../../src/ProjectAegis.Sim/Core/RngDomain.cs).
- **Stateful per-agent RNG:** [`ProjectAegis.Delegation.Decision.SeededRng`](../../src/ProjectAegis.Delegation/Decision/SeededRng.cs).
- **World-state hash:** [`SimWorldHash`](../../src/ProjectAegis.Sim/Core/SimWorldHash.cs) (layered
  fold) — the sim-side companion to the order-log fingerprint.
- **Cross-process string hash:** [`DeterministicHash.OrdinalHash`](../../src/ProjectAegis.Delegation/Core/DeterministicHash.cs).

---

## Design invariants — never break these

Load-bearing and enforced by tests / the golden gate. Preserve them when touching any primitive here.

| Invariant | Rule |
|-----------|------|
| **Same `(seed, inputs)` → same value, everywhere** | Every primitive is a pure integer function. The same inputs produce the same `ulong`/`double` on every process, machine, and OS. No wall-clock, no `Random.Shared`, no `Guid.NewGuid()` in the sim/delegation path. |
| **Enum numbers & layer tags are on-disk contract** | `RngDomain` values (`0..5`) and `SimWorldHash` layer tags (`1..4`) are mixed into the hashes that the replay goldens pin. **Never renumber, reorder, or reuse a value** — doing so silently changes every golden. Append new values at the end. |
| **`SeededRng.UnitFloat` is stateless / order-independent** | The *value* of a draw depends only on `(SimSeed, RngDomain, entityId, simTick, drawIndex)` — never on call order. Give each independent draw within one `(domain, entity, tick)` a distinct `drawIndex`, or two draws will alias to the same value. |
| **Delegation `SeededRng` is stateful / order-significant** | The per-agent xorshift stream advances on every `NextUnit()`. Reordering, adding, or removing a draw per tick changes every subsequent value. Keep the number and order of draws per agent-tick stable. |
| **Quantize floats before mixing into a hash** | Floating-point inputs to the world hash are converted to integers first (e.g. `Pd`/draw `× 10_000`) so `double` round-trip noise and negative zero can't diverge the hash. Floats reaching the *order-log fingerprint* go through `FingerprintFloat` instead (see [determinism-and-replay.md](determinism-and-replay.md#float-formatting-is-where-determinism-bugs-hide)). |
| **Hash strings with `DeterministicHash`, never `string.GetHashCode()`** | `string.GetHashCode()` is randomized per process in .NET — stable within a run, different next launch. Any string that seeds RNG or feeds a hash must go through `DeterministicHash.OrdinalHash` (FNV-1a). |

---

## The two RNGs — pick the right one

There are **two** seeded RNGs with deliberately different contracts. Reach for the one whose
addressing model matches your call site; never introduce a third randomness source.

### 1. Stateless, coordinate-addressed — `Sim.Core.SeededRng`

```csharp
double draw = SeededRng.UnitFloat(seed, RngDomain.Detection, entityId, simTick, drawIndex: 0);
bool detected = draw < pd;   // unit float in [0, 1)
```

`UnitFloat` avalanche-mixes `(seed, domain, entityId, simTick, drawIndex)` into a `ulong` (a
SplitMix64-style finalizer with the two well-known constants), then maps its low 32 bits into
`[0, 1)` via `/ uint.MaxValue`. Because it is **stateless**, there is no stream to advance and draw
*order does not matter* — but `domain`, `entityId`, and `drawIndex` fully determine the value, so
**two independent draws that share all five coordinates return the same number**. That is why the
combat resolver uses `drawIndex 0/1/2` for its Hit / Intercept / Kill draws on the same engagement
and tick.

`SimSeed` is the single global scenario seed (`SimSeed.FromScenario(scenarioSeed)`); subsystems
never carry their own seed — they derive from `SimSeed` via `(domain, entityId)`.

**`RngDomain` — wired vs. reserved.** The domain is a namespace so unrelated subsystems can never
alias one another's stream. Six values are declared; today only three are actually drawn from in the
production sim (the rest are reserved — but their **numbers are still contract** because a future
draw must not shift the existing ones):

| # | `RngDomain` | Status | Drawn by |
|---|-------------|--------|----------|
| 0 | `Detection` | **wired** | [`DeterministicDetectionLoop.RollTick`](../../src/ProjectAegis.Sim/Sensors/DeterministicDetectionLoop.cs) — one draw per detection trial vs. `Pd` ([detection-pipeline.md](detection-pipeline.md)). |
| 1 | `Engage` | reserved | — (engagement launch gating is deterministic, not stochastic; see [engagement-pipeline.md](engagement-pipeline.md)). |
| 2 | `AgentDecision` | reserved | — (agent decisions use the *stateful* delegation RNG below, not this one). |
| 3 | `Logistics` | reserved | — (fuel burn is deterministic arithmetic; see [logistics-fuel-runtime.md](logistics-fuel-runtime.md)). |
| 4 | `Combat` | **wired** | [`CombatOutcomeResolver`](../../src/ProjectAegis.Sim/Engage/CombatOutcomeResolver.cs) — three draws (`drawIndex` 0/1/2) folding Hit → Intercept → Kill ([engagement-pipeline.md](engagement-pipeline.md)). |
| 5 | `MineHazard` | **wired** | [`MineTransitHazardHotTickApplier`](../../src/ProjectAegis.Sim/Catalog/MineTransitHazardHotTickApplier.cs) — transit-mine roll ([catalog-damage-readiness-runtime.md](catalog-damage-readiness-runtime.md)). |

### 2. Stateful, per-agent stream — `Delegation.Decision.SeededRng`

```csharp
var salt = DeterministicHash.OrdinalHash(agentId.Value);   // process-stable per-agent salt
var rng  = new SeededRng(GlobalSeed, salt);                // one stream per agent
double u = rng.NextUnit();                                 // advances state; low 24 bits / 2^24
```

This is a small **xorshift** stream (`13 / 7 / 17` shifts) seeded from `(globalSeed ^ agentSalt ×
0x9E3779B9)`, with a `state == 0 → 1` guard so a degenerate seed can't lock the stream at zero. It
backs the trait-weighted softmax draw in the decision pipeline
([agent-decision-pipeline.md](agent-decision-pipeline.md)). Here **draw order is significant**: each
`NextUnit()` mutates `_state`, so every added/removed/reordered draw shifts all later values for that
agent. `DelegationOrchestrator.CreateAgent` builds exactly one stream per `AgentId`, salted by the
agent id so two agents with the same global seed still get independent — but reproducible — streams.

---

## The world-state hash — `SimWorldHash`

`SimWorldHash` folds a tick's end-state into one `ulong`. It is the sim-side reproducibility
artifact (the order-log fingerprint is the decision-side one; the goldens pin **both**). The fold is
a fixed integer mix (`Fold` is the SplitMix64 finalizer), so the value depends only on the layered
integer inputs — never on float round-trip noise or enumeration order.

Layers are combined in a **fixed order**, each stamped with a one-byte tag so the same number mixed
at a different layer produces a different composite:

| Layer | Tag | Contributes |
|-------|-----|-------------|
| `LayerCore` | 1 | The MVP clock/seed core hash from `SimTickRunner` (`seed ^ tick ^ previous`). |
| `LayerDetection` | 2 | The detection sub-hash (`DetectionWorldHash`, quantized `Pd`/draw). |
| `LayerEngage` | 3 | Launched engagement ids + outcome mix. |
| `LayerCombatOutcome` | 4 | Kill registry / HP-ledger / swarm-drone-count deltas. |

```csharp
// 3-arg: core → detection → engage (harness world hash without combat outcome)
ulong h  = SimWorldHash.Combine(coreHash, detectionHash, engageMix);
// 4-arg: adds the combat-outcome layer (full pipeline)
ulong h4 = SimWorldHash.Combine(coreHash, detectionHash, engageMix, killMix);
```

`SimTickPipeline.RecomputeWorldHash` produces the canonical `LastWorldHash` this way each tick; the
`BalticReplayHarness` mirrors it for the golden values (`WORLD_HASH`, `DETECTION_WORLD_HASH`).
Subsystems that own their own bounded state fold it with `MixLayer` under an appropriate tag before
it reaches `Combine` — e.g. [`KilledTargetRegistry`](../../src/ProjectAegis.Sim/Engage/KilledTargetRegistry.cs),
[`PlatformHpLedger`](../../src/ProjectAegis.Sim/Catalog/PlatformHpLedger.cs), and the swarm order log
([swarm-runtime.md](swarm-runtime.md)) all use `LayerCombatOutcome`.

---

## The cross-process string hash — `DeterministicHash`

`DeterministicHash.OrdinalHash(string)` is a 32-bit **FNV-1a** over the little-endian UTF-16 bytes of
the string, reinterpreted as `int`. It exists because .NET randomizes `string.GetHashCode()` per
process: a string-seeded RNG built on it would look stable within one run and silently differ on the
next launch — the exact failure the determinism contract forbids. It is **not** a security hash (no
adversarial collision resistance); its only job is process-stable identity → integer.

Current call sites:

- [`DelegationOrchestrator.CreateAgent`](../../src/ProjectAegis.Delegation/Orchestration/DelegationOrchestrator.cs)
  — the per-agent salt for the stateful `SeededRng` above.
- [`MapPictureProjection`](../../src/ProjectAegis.Delegation/Projection/MapPictureProjection.cs) —
  deterministic per-key jitter (`OrdinalHash($"{seed}:{key}")`) in the read-only map projection.

---

## Producers & consumers

```
SimSeed.FromScenario(seed)
   │
   ├── SeededRng.UnitFloat(seed, domain, entity, tick, draw)   (stateless, per-subsystem)
   │      ├─ Detection  → DeterministicDetectionLoop  ─┐
   │      ├─ Combat     → CombatOutcomeResolver        ├─→ world-state hash (SimWorldHash)
   │      └─ MineHazard → MineTransitHazardHotTickApplier ┘
   │
   ├── DeterministicHash.OrdinalHash(agentId)  → agent salt
   │      └── new SeededRng(GlobalSeed, salt)  (stateful) → DecisionPipeline softmax draw
   │                                                        → order-log fingerprint (decisions)
   │
   └── SimTickRunner core hash ─→ SimWorldHash.Combine(core, detection, engage, kill)
                                     → SimTickPipeline.LastWorldHash / BalticReplayHarness
                                       → replay goldens (WORLD_HASH, DETECTION_WORLD_HASH)
```

The two artifacts are independent and both pinned: `SimWorldHash` captures the **end-state**, the
order-log fingerprint captures the **decisions**. A change can move one without the other, which is
exactly why the golden files carry both.

---

## Runbooks

### Add a new RNG domain

1. **Append the value at the end of `RngDomain`** (next unused number). Never renumber — the number
   is mixed into every draw and therefore into the goldens.
2. Draw with `SeededRng.UnitFloat(seed, RngDomain.<New>, entityId, simTick, drawIndex)`. Assign each
   independent draw within one `(domain, entity, tick)` a distinct `drawIndex`.
3. If the new draw fires in the Baltic v2 scenarios it **will** move `WORLD_HASH`; prefer gating it
   behind a scenario feature so v2 stays untouched, or re-record only the affected isolated goldens
   per the [golden workflow](determinism-and-replay.md#the-replay-golden-workflow).

### Add a hashed world-state field or layer

1. **Quantize floats to integers first** (mirror `DetectionWorldHash`'s `× 10_000`); never mix a raw
   `double`.
2. Fold it with `SimWorldHash.MixLayer(composite, value, tag)` under an existing tag, in a **fixed,
   deterministic order** (sort by a stable key first — never rely on `Dictionary`/`HashSet` order).
3. Mixing anything new into a layer that already fires in v2 moves the hash — treat it as an
   intentional golden re-record.

### Hash a string id deterministically

Use `DeterministicHash.OrdinalHash(value)`. Do **not** call `string.GetHashCode()` (per-process
randomized) or `value.GetHashCode(StringComparison.Ordinal)` in any sim/delegation path.

---

## Tests that pin this doc

All green as of writing (xUnit, co-located under `src/*.Tests/`):

| Test file | Covers |
|-----------|--------|
| [`SimWorldHashTests.cs`](../../src/ProjectAegis.Sim.Tests/Core/SimWorldHashTests.cs) | `Combine` stability for identical inputs; a changed detection layer changes the composite. |
| [`SimTickRunnerTests.cs`](../../src/ProjectAegis.Sim.Tests/Core/SimTickRunnerTests.cs) | Same seed + tick count → identical world hash; different seed → different hash. |
| [`SimTickPipelineTests.cs`](../../src/ProjectAegis.Sim.Tests/Core/SimTickPipelineTests.cs) | The layered `RecomputeWorldHash` fold (core → detection → engage → combat). |
| [`CombatOutcomeResolverTests.cs`](../../src/ProjectAegis.Sim.Tests/Engage/CombatOutcomeResolverTests.cs) | The three `RngDomain.Combat` draws (`drawIndex` 0/1/2) fold Hit → Intercept → Kill deterministically. |
| [`DeterministicDetectionLoopTests.cs`](../../src/ProjectAegis.Sim.Tests/Sensors/DeterministicDetectionLoopTests.cs) | `RngDomain.Detection` per-trial draw vs. `Pd`. |
| [`MineTransitHazardHotTickApplierTests.cs`](../../src/ProjectAegis.Sim.Tests/Catalog/MineTransitHazardHotTickApplierTests.cs) | `RngDomain.MineHazard` transit-mine roll. |
| [`DeterminismTests.cs`](../../src/ProjectAegis.Delegation.Tests/Decision/DeterminismTests.cs) · [`DecisionPipelineTests.cs`](../../src/ProjectAegis.Delegation.Tests/Decision/DecisionPipelineTests.cs) | The stateful per-agent `SeededRng` stream + salted-per-agent reproducibility. |

Run the sim-core subset:

```bash
dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj \
  --filter "FullyQualifiedName~Core.SimWorldHash|FullyQualifiedName~Core.SimTickRunner|FullyQualifiedName~Core.SimTickPipeline"
```

---

*Verified against source at the paths above. If you add an `RngDomain` value, a `SimWorldHash` layer,
or change how a float is quantized into a hash, update this doc together with
[determinism-and-replay.md](determinism-and-replay.md) and confirm the replay golden posture per
[AGENTS.md](../../AGENTS.md#hard-invariants--never-break-these) (Baltic v2 hash `17144800277401907079`
unchanged, `ReplayGolden 6/6`).*
