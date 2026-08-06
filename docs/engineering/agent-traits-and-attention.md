# Agent traits, personality presets & the attention model — developer guide

Every delegated agent decision is shaped by two small, deterministic inputs that this page
documents end-to-end:

1. **A `TraitVector`** — the six per-agent personality scalars (`Aggression`, `RiskTolerance`,
   `ReactionDelay`, `ErrorRate`, `SituationalAwareness`, `Decisiveness`) that ride with an
   `AgentController` for its whole life.
2. **An attention/bandwidth model** — a per-tick load-vs-budget calculation
   (`AttentionCalculator`) that produces three **graceful-overload degradation flags**
   (`SlowerReactions`, `NarrowedFocus`, `SimplerDecisions`) plus an `IsOverloaded` bit.

Together with the ready-made **`PersonalityCatalog`** presets, these are the "who is this agent and
how stressed is it right now" half of the [agent decision pipeline](agent-decision-pipeline.md).
That page documents the decision *tick* as a whole; this page zooms in on the trait/attention
inputs it consumes — the exact formulas, which fields are actually wired today, and how to extend
them without perturbing replay goldens.

- **Source:**
  [`src/ProjectAegis.Delegation/Traits/`](../../src/ProjectAegis.Delegation/Traits/)
  (`TraitVector`, `PersonalityCatalog`, `PersonalityPreset`) and
  [`src/ProjectAegis.Delegation/Attention/`](../../src/ProjectAegis.Delegation/Attention/)
  (`AttentionCalculator`, `AttentionEvaluation`, `AttentionDegradation`).
- **Consumers:**
  [`AgentController.TryDecide`](../../src/ProjectAegis.Delegation/Controllers/AgentController.cs)
  (reaction throttle + fog-of-war view), the
  [`DecisionPipeline.Choose`](../../src/ProjectAegis.Delegation/Decision/DecisionPipeline.cs)
  softmax, and
  [`DelegationOrchestrator.CreateAgent*`](../../src/ProjectAegis.Delegation/Orchestration/DelegationOrchestrator.cs).
- **Related:** the trait-weighted softmax and the ROE/autonomy gate that runs *after* it are in
  [agent-decision-pipeline.md](agent-decision-pipeline.md) and
  [autonomy-roe-gating.md](autonomy-roe-gating.md); the determinism rules these inputs must respect
  are in [determinism-and-replay.md](determinism-and-replay.md).

---

## Where it lives

| File | Role |
|------|------|
| [`TraitVector.cs`](../../src/ProjectAegis.Delegation/Traits/TraitVector.cs) | `sealed record` of six `double` trait scalars. Immutable value; no clamping or defaults. |
| [`PersonalityCatalog.cs`](../../src/ProjectAegis.Delegation/Traits/PersonalityCatalog.cs) | The `PersonalityPreset` record (`Name`, `Traits`, `AttentionBudgetMultiplier`), the six built-in presets (`All`), `DefaultAttentionBudget = 20.0`, and `ResolveAttentionBudget(preset)`. |
| [`AttentionCalculator.cs`](../../src/ProjectAegis.Delegation/Attention/AttentionCalculator.cs) | Pure static `Evaluate(budget, memberCount, ObservedState)` → `AttentionEvaluation`. |
| [`AttentionState.cs`](../../src/ProjectAegis.Delegation/Attention/AttentionState.cs) | `AttentionEvaluation(Budget, Load, Degradation)` with `IsOverloaded`, and `AttentionDegradation(SlowerReactions, NarrowedFocus, SimplerDecisions)`. |

Both folders are engine-agnostic and have **no** dependency on Unity, the catalog, or wall-clock
time. `AttentionCalculator.Evaluate` and every trait read are pure functions of their arguments.

---

## 1. `TraitVector` — the six scalars

```csharp
public sealed record TraitVector(
    double Aggression,
    double RiskTolerance,
    double ReactionDelay,
    double ErrorRate,
    double SituationalAwareness,
    double Decisiveness);
```

The positional order matters — presets are constructed positionally (see below), so a field
reorder silently remaps every preset. By convention the scalars are in `[0, 1]` (except
`ReactionDelay`, which is a sim-seconds delay), but **`TraitVector` itself does not validate or
clamp**; the only clamp in the whole subsystem is on `SituationalAwareness` at read time (§3).

### Which traits are actually wired today

This is the load-bearing "don't fabricate behavior" fact for this subsystem. Only **three** of the
six scalars are read by shipped code:

| Trait | Read by | Effect |
|-------|---------|--------|
| `ReactionDelay` | [`AgentController.TryDecide`](../../src/ProjectAegis.Delegation/Controllers/AgentController.cs) | Sets the next-decision throttle (§2). Under `SlowerReactions` it is stretched. |
| `SituationalAwareness` | [`PerceivedStateFactory.FromFull`](../../src/ProjectAegis.Delegation/Sim/ObservedState.cs) | Scales down how many contacts/engagements the agent perceives — fog of war (§3). |
| `Decisiveness` | [`DecisionPipeline.Choose`](../../src/ProjectAegis.Delegation/Decision/DecisionPipeline.cs) | Softmax temperature — low = sharper/greedier, high = flatter/more exploratory (§4). |

`Aggression`, `RiskTolerance`, and `ErrorRate` are **declared inputs only**: `AgentController`
passes the full `TraitVector` into `IPolicy.GenerateCandidates(perceived, traits)`, but the shipped
policies ([`PatrolCandidateEngagePolicy`](../../src/ProjectAegis.Delegation/Policy/PatrolCandidateEngagePolicy.cs),
[`StubPatrolPolicy`](../../src/ProjectAegis.Delegation/Policy/StubPatrolPolicy.cs)) currently ignore
them (`_ = traits;` / fixed candidate scores). They are the extension surface for a
trait-sensitive `IPolicy`, not behavior you can assume today. Do not describe them as affecting
engagement decisions unless a custom policy reads them.

---

## 2. The attention/bandwidth model (`AttentionCalculator`)

Called once per deciding agent per tick, *before* the policy generates candidates:

```csharp
var attention = AttentionCalculator.Evaluate(AttentionBudget, memberCount, state);
```

### Load formula

`Load` is a weighted sum of the three things that cost command bandwidth:

```
Load = ContactCount        * 0.5
     + ActiveEngagementCount * 1.0
     + memberCount           * 0.25
```

- `ContactCount` / `ActiveEngagementCount` come from the tick's raw
  [`ObservedState`](../../src/ProjectAegis.Delegation/Sim/ObservedState.cs) — **not** the
  fog-of-war `PerceivedState`. Attention load is computed from the *true* world; the agent's SA
  only narrows what its policy then sees.
- `memberCount` is the size of the group/unit the agent commands (1 for a single unit).

### Degradation ladder

Three flags latch on at successively higher load, all relative to the agent's `Budget`:

| Flag | Trips when | Consumed by |
|------|-----------|-------------|
| `SlowerReactions` | `Load > Budget` | Reaction throttle — `ReactionDelay` is multiplied ×5 (min 1.0s). |
| `NarrowedFocus` | `Load > Budget * 1.25` | `DecisionPipeline` — pool trimmed to the top-2 highest-scored candidates. |
| `SimplerDecisions` | `Load > Budget * 1.5` | **Not consumed by shipped code.** Reserved flag; surfaced in the evaluation for future/custom use. |

`IsOverloaded` is simply `Load > Budget` (the same threshold as `SlowerReactions`) and is what the
softmax uses to sharpen the temperature (§4).

> **Accuracy note:** `SimplerDecisions` is *computed* but no current codepath reads it — it is pinned
> by [`AttentionTests`](../../src/ProjectAegis.Delegation.Tests/Attention/AttentionTests.cs) only as
> a value, not as a behavior. Treat it as a documented no-op until something consumes it.

`AttentionEvaluation` carries `Budget` and `Load` forward into the order log via the
`DecisionRecord` (`attention.Load`, `attention.Budget`), so overload state is replayable and
surfaced to the C2 read-model — see [c2-projection-layer.md](c2-projection-layer.md).

---

## 3. Situational awareness → fog of war

`SituationalAwareness` is applied in
[`PerceivedStateFactory.FromFull`](../../src/ProjectAegis.Delegation/Sim/ObservedState.cs) to derive
the `PerceivedState` the policy actually sees:

```csharp
var factor = Math.Clamp(situationalAwareness, 0, 1);
var contacts    = (int)Math.Round(full.ContactCount        * factor);
var engagements = (int)Math.Round(full.ActiveEngagementCount * factor);
```

- Lower SA ⇒ the agent *perceives fewer* contacts/engagements than truly exist (rounded to the
  nearest integer). `SA = 1.0` sees everything; `SA = 0.0` perceives zero contacts/engagements.
- Boolean world facts (`PrimaryHostileDestroyed`, `PrimaryBlueForceContactDestroyed`) pass through
  untouched — SA scales *counts*, not the destroyed-target pre-filter.
- This is the **only** place a trait scalar is clamped. Feed SA in `[0, 1]`; out-of-range values are
  clamped rather than rejected.

---

## 4. Decisiveness → softmax temperature

The trait-weighted stochastic choice lives in `DecisionPipeline.Choose`. The full pipeline (the
non-positive pre-filter, the single seeded RNG draw, the logged candidate set) is documented in
[agent-decision-pipeline.md](agent-decision-pipeline.md); here is only the trait/attention math:

```csharp
// NarrowedFocus (Load > Budget*1.25) trims the pool to the two best-scored candidates first.
if (attention.Degradation.NarrowedFocus)
    pool = pool.OrderByDescending(c => c.Score).Take(2).ToList();

var temperature = Math.Max(0.05, traits.Decisiveness * (attention.IsOverloaded ? 0.5 : 1.0));
var weights     = pool.Select(c => Math.Exp(c.Score / temperature)).ToArray();
```

- **Temperature = `Decisiveness`**, halved when overloaded, floored at `0.05` (so a
  `Decisiveness = 0` agent is still numerically stable, not a divide-by-zero).
- **Lower temperature ⇒ sharper choice** (the top-scored candidate dominates the `Math.Exp` weights);
  **higher temperature ⇒ flatter distribution** (more likely to pick a lower-scored option). So a
  *low-Decisiveness* agent behaves more greedily/deterministically, and overload makes every agent
  sharper (temperature halved).
- The `rationale` string logged on the `DecisionRecord` is `"overload: narrowed focus applied"`
  when `IsOverloaded`, else `"nominal: trait-weighted stochastic choice"`.

---

## 5. Reaction throttle — where `ReactionDelay` meets `SlowerReactions`

`AgentController` self-throttles how often it decides, and overload stretches that interval:

```csharp
if (state.SimTime < _nextDecisionSimTime) return;   // not time yet

var attention = AttentionCalculator.Evaluate(AttentionBudget, memberCount, state);
var delay = attention.Degradation.SlowerReactions
    ? Math.Max(1.0, Traits.ReactionDelay * 5.0)
    : Traits.ReactionDelay;
_nextDecisionSimTime = state.SimTime + delay;
```

- Nominal cadence is `ReactionDelay` sim-seconds between decisions.
- Under overload (`SlowerReactions`) the cadence stretches to `ReactionDelay * 5` (minimum 1.0s), so
  a stressed agent both decides *less often* and — via the softmax halving — more *sharply* when it
  does. This is the "graceful degradation" behavior end-to-end.

---

## 6. `PersonalityCatalog` — the six presets

`PersonalityPreset` bundles a name, a `TraitVector`, and an `AttentionBudgetMultiplier` (default
`1.0`). The catalog ships six presets over the default budget of `20`:

| Preset | Trait vector `(Agg, Risk, RxDelay, Err, SA, Decis)` | Budget × | Resolved budget |
|--------|-----------------------------------------------------|:--------:|:---------------:|
| `Aggressive` | `(0.9, 0.8, 0.2, 0.15, 0.6, 0.8)` | 1.0 | 20.0 |
| `Defensive` | `(0.2, 0.3, 0.3, 0.08, 0.7, 0.5)` | 1.0 | 20.0 |
| `Cautious` | `(0.3, 0.2, 0.5, 0.05, 0.8, 0.3)` | 1.0 | 20.0 |
| `Opportunistic` | `(0.6, 0.6, 0.25, 0.12, 0.65, 0.7)` | 1.0 | 20.0 |
| `SwarmCoordinator` | `(0.5, 0.5, 0.15, 0.1, 0.75, 0.85)` | 1.25 | 25.0 |
| `EwSpecialist` | `(0.4, 0.4, 0.2, 0.07, 0.85, 0.6)` | 0.9 | 18.0 |

`ResolveAttentionBudget(preset) = DefaultAttentionBudget * preset.AttentionBudgetMultiplier`. The
budget multiplier is the one place a preset tunes the *attention* model rather than the trait
scalars: `SwarmCoordinator` tolerates 25% more load before degrading; `EwSpecialist` degrades 10%
sooner. Both are pinned by
[`PersonalityAttentionBudgetTests`](../../src/ProjectAegis.Delegation.Tests/Traits/PersonalityAttentionBudgetTests.cs)
(`SwarmCoordinator → 25.0`, `EwSpecialist → 18.0`).

> The presets are **C# constants**, not data-driven from `data/scenarios/*.policy.json`. Scenario
> policy JSON tunes ROE / salvo / mission behavior (see
> [scenario-policy-authoring.md](scenario-policy-authoring.md)); it does not currently select
> personalities or edit trait vectors. Adding presets or a JSON binding is a source change.

### Wiring an agent

Two orchestrator factories build an `AgentController` with a trait vector and an attention budget:

| Factory | Traits | Budget |
|---------|--------|--------|
| `CreateAgent(id, traits, autonomy, attentionBudget = 20, policy = null)` | caller-supplied | caller-supplied (default 20) |
| `CreateAgentFromPreset(id, preset, autonomy, policy = null)` | `preset.Traits` | `ResolveAttentionBudget(preset)` |

`CreateAgentFromPreset` also stamps `AgentController.PersonalitySlug = preset.Name`, which flows to
the Hindsight order-log hook (`RegisterAgent(agent.Id, agent.PersonalitySlug)`) for AAR labeling —
see [hindsight-session-memory-sidecar.md](hindsight-session-memory-sidecar.md). Both factories
derive the agent's `SeededRng` from `(GlobalSeed, OrdinalHash(id))`, so trait/attention behavior is
reproducible per `(scenario, seed, agentId)`.

`AgentController.RebindTraits(traits)` can swap the trait vector mid-session (e.g. a doctrine
change); the attention budget is fixed at construction.

> **What the replay goldens actually run:** the
> [Baltic replay harness](baltic-replay-harness.md) builds its agents with
> `CreateAgent(..., PersonalityCatalog.All[0].Traits, ...)` — i.e. the **`Aggressive`** trait vector
> at the default budget `20` — not `CreateAgentFromPreset`. So the six-preset catalog is exercised by
> unit tests and available to callers, but the shipped goldens pin a single preset. Changing
> `All[0]`, the `Aggressive` values, or the load/temperature formulas will move golden hashes.

---

## Determinism & invariants

The trait/attention subsystem sits directly on the replay hot path, so it obeys the
[determinism rules](determinism-and-replay.md):

- **Pure & seed-driven.** `AttentionCalculator.Evaluate` and every trait read are pure functions;
  the only randomness is the single `rng.NextUnit()` draw in the softmax, seeded per agent. No
  wall-clock, no `Random.Shared`, no unordered iteration.
- **Constants are golden-load-bearing.** The load weights (`0.5 / 1.0 / 0.25`), the degradation
  multipliers (`1.0 / 1.25 / 1.5`), the ×5 reaction stretch, the `0.05` temperature floor, the
  `0.5` overload sharpening, `DefaultAttentionBudget = 20.0`, and every preset value feed decisions
  that land in the order log. Treat them as tuning that moves the Baltic v2 hash
  (`17144800277401907079`) unless changed behind an isolated scenario/golden.
- **Field order is a contract.** `TraitVector` is constructed positionally throughout the catalog;
  reordering fields silently remaps every preset.
- **Advisory flags stay advisory.** `SimplerDecisions` and the three unused trait scalars are inert
  today; wiring them in is a behavior change requiring new/regenerated goldens.

---

## Extending it without breaking replay

1. **New preset (safe, additive).** Append to `PersonalityCatalog.All`. Existing agents/goldens are
   untouched because the harness references `All[0]`. Add a `PersonalityAttentionBudgetTests` case
   for its resolved budget.
2. **Trait-sensitive policy (safe if opt-in).** Implement `IPolicy.GenerateCandidates` reading
   `traits.Aggression` / `RiskTolerance` / `ErrorRate`, and pass it via `CreateAgent(..., policy)`.
   Only scenarios that select the new policy change; the default patrol policies are unaffected.
3. **Retuning the model (golden-moving).** Editing a load weight, degradation multiplier, the
   temperature math, or an existing preset's values changes decisions on the hot path — pair it with
   an ADR and regenerate the affected replay goldens; never edit Baltic v2 goldens for a v3-scoped
   change (see [`AGENTS.md`](../../AGENTS.md) invariants).
4. **Wiring `SimplerDecisions` or the unused scalars.** New behavior ⇒ new tests + regenerated
   goldens. Update this page's "which traits are wired" table so it stays honest.

Always run the full verification block from [`AGENTS.md`](../../AGENTS.md) and grep the Baltic v2
hash before submitting any change that touches these files.

---

## Tests to read first

| Test | Pins |
|------|------|
| [`AttentionTests`](../../src/ProjectAegis.Delegation.Tests/Attention/AttentionTests.cs) | All three degradation flags trip together under heavy overload. |
| [`PersonalityAttentionBudgetTests`](../../src/ProjectAegis.Delegation.Tests/Traits/PersonalityAttentionBudgetTests.cs) | `ResolveAttentionBudget` math + `CreateAgentFromPreset` budget wiring. |
| [`DecisionPipelineTests`](../../src/ProjectAegis.Delegation.Tests/Decision/DecisionPipelineTests.cs) | Same seed + traits + attention ⇒ identical choice; the non-positive pre-filter. |
