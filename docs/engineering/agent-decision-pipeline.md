# Agent decision pipeline — developer guide

The **delegation decision tick** is where an observed world state becomes gated, logged orders:
for every registered target whose active controller is an agent, the framework scores intents,
picks one with a **trait-weighted stochastic choice**, passes it through the ROE + autonomy
gate, and appends the whole reasoning trail to the order log. It is the seam that feeds the
sim engagement layer — the `Engage` orders the [engagement pipeline](engagement-pipeline.md)
resolves at tick-8 are produced here, from the tactical picture the
[detection pipeline](detection-pipeline.md) builds at tick-4.

- **Source:** [`src/ProjectAegis.Delegation/`](../../src/ProjectAegis.Delegation/) — engine-agnostic
  pure C# (`net8.0`, no `UnityEngine`), so the whole decision path runs headless under
  `dotnet test`. The per-project overview is the
  [Delegation README](../../src/ProjectAegis.Delegation/README.md); this page is the runtime
  deep-dive.
- **Related:** the *concepts* (seeded RNG, order-log hashing, golden workflow) are in
  [`determinism-and-replay.md`](determinism-and-replay.md); the ROE codes a gate rejection emits
  are catalogued in [`abort-reason-catalog.md`](abort-reason-catalog.md); the read-models built
  from the log this tick writes are in [`c2-projection-layer.md`](c2-projection-layer.md); the
  end-to-end runner that drives this tick each step is
  [`baltic-replay-harness.md`](baltic-replay-harness.md). This page documents what the decision
  code actually **does** at runtime and how to extend it without breaking replay goldens.

> **Determinism is the load-bearing invariant.** Given the same `(scenario, seed)`, a run must
> reproduce the same order log bit-for-bit. Every agent draws from a **stateful** per-agent
> `SeededRng`, so *draw order matters* — never introduce ambient randomness, `DateTime.UtcNow`,
> or unordered-collection iteration in this path. Agents are ticked in registration order and
> each agent draws exactly once per eligible tick (see [Determinism](#determinism--replay-safety)).

---

## Where it lives

| File | Role |
|------|------|
| [`Orchestration/DelegationOrchestrator.cs`](../../src/ProjectAegis.Delegation/Orchestration/DelegationOrchestrator.cs) | `Tick(ObservedState)` — the main entry point; iterates targets and drives each agent. |
| [`Controllers/AgentController.cs`](../../src/ProjectAegis.Delegation/Controllers/AgentController.cs) | `TryDecide(...)` — the per-agent decide → gate → issue path + reaction-delay throttle. |
| [`Attention/AttentionCalculator.cs`](../../src/ProjectAegis.Delegation/Attention/AttentionCalculator.cs) | `Evaluate(budget, memberCount, state)` — load score and the three degradation flags. |
| [`Attention/AttentionState.cs`](../../src/ProjectAegis.Delegation/Attention/AttentionState.cs) | `AttentionEvaluation` / `AttentionDegradation` records (`IsOverloaded`). |
| [`Sim/ObservedState.cs`](../../src/ProjectAegis.Delegation/Sim/ObservedState.cs) | Inbound world state + `PerceivedState` + `PerceivedStateFactory.FromFull` (SA fog-of-war). |
| [`Policy/IPolicy.cs`](../../src/ProjectAegis.Delegation/Policy/IPolicy.cs) / [`Policy/PatrolCandidateEngagePolicy.cs`](../../src/ProjectAegis.Delegation/Policy/PatrolCandidateEngagePolicy.cs) | The candidate-generation seam and the Baltic patrol policy. |
| [`Decision/ScoredIntent.cs`](../../src/ProjectAegis.Delegation/Decision/ScoredIntent.cs) | `(OrderKind Kind, double Score, RiskLevel Risk)` — one weighed intent. |
| [`Decision/DecisionPipeline.cs`](../../src/ProjectAegis.Delegation/Decision/DecisionPipeline.cs) | `Choose(...)` — the trait-weighted softmax + single RNG draw. |
| [`Decision/SeededRng.cs`](../../src/ProjectAegis.Delegation/Decision/SeededRng.cs) | The stateful per-agent xorshift RNG (`NextUnit`). |
| [`Orchestration/AutonomyGate.cs`](../../src/ProjectAegis.Delegation/Orchestration/AutonomyGate.cs) | `Evaluate(autonomy, order, playerApproved)` — ROE-first, then autonomy `GateResult`. |
| [`Roe/RoePolicyAdapter.cs`](../../src/ProjectAegis.Delegation/Roe/RoePolicyAdapter.cs) | ADR-002 bridge from `IRoeFilter` to the sim `IPolicyEvaluator`. |
| [`Roe/DefaultRiskClassifier.cs`](../../src/ProjectAegis.Delegation/Roe/DefaultRiskClassifier.cs) | `OrderKind → RiskLevel` (only `Engage` is `High`). |
| [`Decision/DecisionRecord.cs`](../../src/ProjectAegis.Delegation/Decision/DecisionRecord.cs) | The full per-decision row appended to the order log. |
| [`Traits/TraitVector.cs`](../../src/ProjectAegis.Delegation/Traits/TraitVector.cs) / [`Traits/PersonalityCatalog.cs`](../../src/ProjectAegis.Delegation/Traits/PersonalityCatalog.cs) | The six personality inputs and the preset catalog. |

---

## The tick, end to end

`DelegationOrchestrator.Tick(ObservedState)` is a **no-op while `Phase == Planning`** (it clears
`ExecutedOrders` and returns). Once `BeginExecution()` flips the phase to `Executing`, each tick
walks every registered target in **registration order** and drives whichever controller is
active in its `ControllerSlot`:

```text
Tick(observedState)                                   // no-op while Phase == Planning
  simTick = max(0, (long)state.SimTime)
  └─ per registered target (registration order):
       group with PendingReplan → ClearReplanPending()
       memberCount = group ? Members.Count : 1
       switch slot.Active:
         AgentController → agent.TryDecide(targetId, state, memberCount, ref orderIdSeq, gate, log)
                           executed += agent.DrainIssuedOrders(simTick)
         HumanController → executed += human.DrainIssuedOrders(simTick)   // player-queued orders
  → ExecutedOrders = executed
```

`ExecutedOrders` is the tick's output: the orders neighbouring systems apply. Under Unity the
[`UnityAdapter`](../../src/ProjectAegis.Delegation.UnityAdapter/README.md) turns the sim snapshot
into `ObservedState` (`ISimWorldSnapshot`) and pushes `ExecutedOrders` back to the sim
(`IOrderSink`); headless, [`SimulationSession`](../../src/ProjectAegis.Delegation/Orchestration/SimulationSession.cs)
wraps the same `Tick` and hands the drained orders to the engagement resolver.

> **`simTick` comes from `SimTime`.** The orchestrator derives the integer tick as
> `(ulong)Math.Max(0, (long)state.SimTime)`. `ObservedState.SimTime` is the single clock the
> decision path reads — there is no wall-clock anywhere in this loop.

---

## Per-agent decide (`AgentController.TryDecide`)

Each eligible agent runs the same five-stage path. The order is fixed and load-bearing for
replay: **throttle → attention → perceive → generate → choose → log → gate → issue**.

```text
TryDecide(targetId, state, memberCount, ref orderIdSeq, gate, log):
  if state.SimTime < _nextDecisionSimTime: return           // 1. reaction-delay throttle
  attention = AttentionCalculator.Evaluate(budget, memberCount, state)   // 2. attention
  delay     = attention.Degradation.SlowerReactions ? max(1.0, ReactionDelay*5) : ReactionDelay
  _nextDecisionSimTime = state.SimTime + delay
  perceived = PerceivedStateFactory.FromFull(state, Traits.SituationalAwareness)   // 3. fog
  candidates = Policy.GenerateCandidates(perceived, Traits)              // 4. candidates
  choice    = DecisionPipeline.Choose(candidates, Traits, attention, Rng)  // 5. RNG draw
  order     = new Order(orderIdSeq++, targetId, SimTime, choice.Kind, DefaultRiskClassifier.Classify(kind))
  log.Append(DecisionRecord{...candidates, rationale, load, budget, rngDraw...})   // 6. log first
  gateResult = gate.Evaluate(Autonomy, order, playerApproved: false)     // 7. ROE + autonomy
  if gateResult.Rejected && PolicyDenialReason != None: log PolicyDenialRecord
  if gateResult.ExecuteNow: _issued.Add(order)                           // 8. issue (drained by Tick)
```

Two subtleties that matter for correctness:

- **The decision row is logged before the gate runs.** The full `DecisionRecord` (every scored
  candidate, the rationale, attention load/budget, the RNG draw) is appended *whether or not* the
  order survives the gate — so AAR always shows what the agent considered, even for a
  policy-denied `Engage`. A denied order additionally appends a `PolicyDenialRecord`.
- **`playerApproved` is always `false` on the agent path.** Agents never self-approve; the
  approval bit only matters for `Manual`/`Assisted` orders a human later confirms out-of-band. So
  a `Manual` agent's order is *queued*, never auto-executed, from this call.

### 1. Reaction-delay throttle

An agent does not decide every tick. `_nextDecisionSimTime` gates re-entry: if
`state.SimTime < _nextDecisionSimTime` the call returns immediately (no draw, no log row). After
a decision the next slot is `SimTime + delay`, where `delay = ReactionDelay` normally, stretched
to `max(1.0, ReactionDelay × 5)` when attention overload set `SlowerReactions`. A slower,
overloaded agent therefore reacts less often — the throttle *and* the choice both degrade.

### 2. Attention (`AttentionCalculator.Evaluate`)

Load is a pure linear score of the observed world scaled by how many units the agent shepherds:

```text
load = ContactCount × 0.5 + ActiveEngagementCount × 1.0 + memberCount × 0.25
```

compared against the agent's `AttentionBudget` (default `20`, scaled per personality). Crossing
multiples of the budget flips three cumulative degradation flags (all-or-nothing per threshold):

| Threshold | Flag | Effect |
|-----------|------|--------|
| `load > budget` (`IsOverloaded`) | `SlowerReactions` | reaction delay ×5 (see throttle) |
| `load > budget × 1.25` | `NarrowedFocus` | only the top-2 scored candidates survive into the softmax |
| `load > budget × 1.5` | `SimplerDecisions` | reserved signal (surfaced for AAR/tuning; no extra pruning today) |

`IsOverloaded` also halves the softmax temperature (below), making an overloaded agent both
narrower *and* more deterministic about its highest-scoring option.

### 3. Perceived state (fog-of-war)

`PerceivedStateFactory.FromFull(state, SituationalAwareness)` scales what the agent *sees* by its
SA trait (clamped `[0,1]`): `contacts = round(ContactCount × SA)`,
`engagements = round(ActiveEngagementCount × SA)`. Primary-target destroyed flags pass through
unscaled. A low-SA agent generates candidates from a thinned picture — this is the only place the
agent's perception diverges from ground truth.

### 4. Candidate generation (`IPolicy.GenerateCandidates`)

The policy is the pluggable "what could I do here" seam — it returns a list of `ScoredIntent`
`(Kind, Score, Risk)`. Higher score = more preferred; the score is a *relative weight*, not a
probability. The Baltic default,
[`PatrolCandidateEngagePolicy`](../../src/ProjectAegis.Delegation/Policy/PatrolCandidateEngagePolicy.cs),
ships:

```text
Hold   score 1.0  (Low)
Move   score 0.8  (Low)
Engage score 99.0 (High)     ← heavily biased toward Engage while a live primary hostile exists
```

Once the primary target is confirmed destroyed (`PrimaryHostileDestroyed`, or
`PrimaryBlueForceContactDestroyed` in blue-force mode) it **re-scores `Engage` to `0.0`** rather
than dropping it — so the intent still appears in the logged candidate set (AAR completeness),
but the pipeline's non-positive filter (below) keeps it from being chosen. This is the
"no re-engagement proposals after a confirmed kill" remediation; the resolver's late
`TargetDestroyed` abort remains a safety net.

### 5. The choice (`DecisionPipeline.Choose`)

`Choose` turns scores into a softmax distribution and takes exactly **one** draw:

```text
pool = candidates
if pool has a Score > 0 and a Score <= 0:  pool = pool.Where(Score > 0)   // drop de-prioritized
if attention.NarrowedFocus:                pool = pool.OrderByDescending(Score).Take(2)
temperature = max(0.05, Decisiveness × (IsOverloaded ? 0.5 : 1.0))
weights = pool.Select(c => exp(c.Score / temperature))
draw    = rng.NextUnit() × weights.Sum()
chosen  = first index where running-sum(weights) >= draw
rationale = IsOverloaded ? "overload: narrowed focus applied" : "nominal: trait-weighted stochastic choice"
```

Key behaviours to preserve:

- **Non-positive filter.** A candidate scored `≤ 0` is dropped **only when a positive-scored
  candidate remains** (the pool is never emptied). This is essential because `exp(0) == 1`, not
  `0` — without the filter a "de-prioritized" zero-scored `Engage` would still carry weight
  comparable to `Hold`/`Move` and could be drawn, silently defeating a policy's pre-filter. Guard
  it with a test if you touch this (see `DecisionPipelineTests`).
- **Temperature = `Decisiveness`.** Lower `Decisiveness` → lower temperature → sharper preference
  for the top score; higher → flatter, more exploratory. Overload halves it. The `max(0.05, …)`
  floor prevents divide-by-near-zero blowups.
- **One draw per decision.** Exactly one `rng.NextUnit()` call per `Choose`. Adding or removing a
  draw here shifts every subsequent agent's RNG stream and breaks replay goldens.

---

## Autonomy & ROE gating (`AutonomyGate.Evaluate`)

The chosen order is not automatically executed. `AutonomyGate.Evaluate` runs the **ROE check
first**, then the autonomy policy:

```text
roe = _roe.Evaluate(order)                       // RoePolicyAdapter → IPolicyEvaluator (ADR-002)
if roe.Verdict == Reject: return GateResult(Execute=false, Queue=false, Rejected=true, roe.Reason)
switch autonomy:
  Manual                 → GateResult(Execute=playerApproved, Queue=!playerApproved)
  Assisted & Risk==Low   → GateResult(Execute=true)
  Assisted (High risk)   → GateResult(Execute=playerApproved, Queue=!playerApproved)
  SemiAutonomous | FullAutonomous → GateResult(Execute=true)
```

| Autonomy | Behaviour on the agent path (`playerApproved=false`) |
|----------|------------------------------------------------------|
| `Manual` (1) | Queue for player approval — never auto-executes |
| `Assisted` (2) | Auto-execute `Low`-risk (`Move`/`Hold`/…); queue `High`-risk (`Engage`) |
| `SemiAutonomous` (3) | Auto-execute |
| `FullAutonomous` (4) | Auto-execute |

A `Reject` verdict is terminal — **player approval cannot override an ROE reject, even at
`Manual`** (the ROE check happens before autonomy is consulted). The rejection carries a
`FireAbortReason`; when it is not `None`, `TryDecide` appends a `PolicyDenialRecord` so the "why
can't I fire?" explain surface has a machine-readable code (see
[`abort-reason-catalog.md`](abort-reason-catalog.md)). Risk is assigned by
`DefaultRiskClassifier`: only `Engage` is `High`; everything else is `Low`.

The default ROE bridge is [`RoePolicyAdapter`](../../src/ProjectAegis.Delegation/Roe/RoePolicyAdapter.cs)
(ADR-002): it maps the `Order` to a policy `ActionRequest` and a `PolicyContext`, asks the sim
`IPolicyEvaluator`, and returns `Allow`/`Reject(reason)`. `HoldFire` rejects `Engage`;
`WeaponsFree` allows it — so scenario ROE (and contact-triggered escalation) flows straight into
the agent gate without the decision code knowing the policy details.

---

## What lands in the order log

`TryDecide` writes to the append-only `DecisionLog` (the `IOrderLog`, ADR-003) — the single
source of truth a run replays from and every C2 projection is built over. Per eligible decision:

| Row | When | Carries |
|-----|------|---------|
| `DecisionRecord` | every decision | `SimTime`, `AgentId`, `TargetId`, `AutonomyLevel`, `ChosenKind`, **all** `Alternatives` (scored candidates), `Rationale`, `AttentionLoad`, `AttentionBudget`, `RngDraw`, `SimTick` |
| `PolicyDenialRecord` | ROE-rejected order | `AgentId`, `TargetId`, `PolicySnapshotId`, `FireAbortReason`, `OrderKind` |
| `PolicyUpdateRecord` | ROE rebind (elsewhere) | old→new ROE + snapshot id (from `BindPolicySnapshot` / scenario triggers) |

Because the record includes the RNG draw and the full candidate set, the log is the substrate for
both determinism (`DecisionLog.ComputeFingerprint()` is what replay goldens assert) and
explainability (AAR / Hindsight). Do not drop fields from `DecisionRecord` — the fingerprint and
the projections both depend on the exact column set (`AgentDecisionPayloadTests` pins the mapping).

---

## Determinism & replay safety

This path is a pure function of `(ObservedState, TraitVector, AutonomyLevel, SeededRng state)`.
The invariants that keep it reproducible:

- **Registration-order iteration.** `Tick` walks `_targets` in the order they were `Register`ed —
  never re-sort or iterate an unordered collection here.
- **One draw per eligible agent per tick.** The single `rng.NextUnit()` in `Choose` is the only
  randomness. Each agent owns its own `SeededRng`, seeded from
  `SeededRng(GlobalSeed, DeterministicHash.OrdinalHash(agentId))`, so agents are independent yet
  reproducible. Reordering agents, adding a draw, or sharing an RNG all shift the stream.
- **No wall-clock, no ambient RNG.** Everything reads `state.SimTime`; there is no
  `DateTime.UtcNow` or `Random.Shared`.
- **`FingerprintFloat` for doubles.** Scores, load, and the RNG draw are hashed through the
  canonical float normaliser (handles `-0.0`) — see
  [`determinism-and-replay.md`](determinism-and-replay.md).

If you change scoring, temperature, attention thresholds, or the candidate set, the order-log
fingerprint **will** change and replay goldens will fail — that is the guard working. Regenerate
goldens deliberately (never to "make the test pass") per the replay-harness guide, and confirm
the Baltic v2 hash invariant `17144800277401907079` only moves when an ADR says so.

---

## Extending it

| You want to… | Do this | Watch out for |
|--------------|---------|---------------|
| Add a new order kind | Add to `OrderKind`, classify it in `DefaultRiskClassifier`, and score it from an `IPolicy` | New `High`-risk kinds queue under `Assisted`; the ROE `OrderActionMapper` must map it |
| Add a new candidate-generation strategy | Implement `IPolicy.GenerateCandidates` and pass it to `CreateAgent(...)` | Express "don't pick this" as `Score ≤ 0` (kept in log, dropped by the filter) — don't omit it |
| Tune an agent's behaviour | Adjust its `TraitVector` / pick a `PersonalityPreset` (Decisiveness=temperature, SA=fog, ReactionDelay=cadence) | Trait changes move the fingerprint; treat as a golden-affecting change |
| Change attention sensitivity | Edit the coefficients / thresholds in `AttentionCalculator` | Golden-affecting; keep the flags cumulative and monotonic |
| Add a new ROE outcome | Extend the policy evaluator + `FireAbortReason` via the abort-reason manifest → codegen | Never alter existing codes/values (replay stability) — see the abort-reason catalog |

**Do not** call `Tick` while `Phase == Planning` expecting orders, add per-tick allocations to the
hotpath, or reach for wall-clock/ambient randomness. The `DelegationBridge` hotpath stays
zero-touch through Release v1 (see [`AGENTS.md`](../../AGENTS.md#hard-invariants--never-break-these)).

---

## Test map

| Behaviour | Test |
|-----------|------|
| Same seed + inputs → identical choice & draw | `Decision/DecisionPipelineTests.Pipeline_is_deterministic_for_same_seed_and_inputs` |
| Zero-scored candidate never chosen when a positive one exists | `Decision/DecisionPipelineTests.Choose_never_selects_a_zero_scored_candidate_when_a_positive_scored_alternative_exists` |
| Overload flips all three degradation flags in threshold order | `Attention/AttentionTests.Overload_enables_all_degradation_flags_in_order` |
| `Manual` never executes without approval | `Roe/AutonomyGateTests.Manual_never_executes_without_approval` |
| `Assisted` auto-executes low-risk only | `Roe/AutonomyGateTests.Assisted_auto_executes_low_risk_only` |
| Player approval cannot override an ROE reject | `Roe/AutonomyGateTests.player_approval_cannot_override_roe_reject_even_on_manual` |
| `HoldFire` rejects / `WeaponsFree` allows `Engage` via the adapter | `Roe/RoePolicyAdapterTests` |
| Two ticks, same seed → identical `ExecutedOrders` | `Orchestration/OrchestratorTests.Two_ticks_same_seed_produce_identical_executed_orders` |
| `DecisionRecord` ↔ order-log payload round-trips (incl. `SimTick`) | `Decision/AgentDecisionPayloadTests` |
| Policy-denied order emits a `PolicyDenialRecord` | `Decision/PolicyDenialLogTests` |
| Seeded RNG sequence stable; six presets exposed | `Decision/DeterminismTests` |

Run the assembly:

```bash
dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj -v minimal
```

---

## See also

| Topic | Doc |
|-------|-----|
| Delegation assembly overview (subsystem map, seams) | [`ProjectAegis.Delegation/README.md`](../../src/ProjectAegis.Delegation/README.md) |
| Detection / contact picture (tick-4, produces the fire-control track) | [`detection-pipeline.md`](detection-pipeline.md) |
| Engagement / kill-chain (tick-8, resolves the `Engage` orders) | [`engagement-pipeline.md`](engagement-pipeline.md) |
| Determinism rules, hashing, golden workflow | [`determinism-and-replay.md`](determinism-and-replay.md) |
| Abort-reason codes emitted by a gate reject | [`abort-reason-catalog.md`](abort-reason-catalog.md) |
| Read-models built from the order log this tick writes | [`c2-projection-layer.md`](c2-projection-layer.md) |
| Scenario ROE / policy authoring (what drives the gate) | [`scenario-policy-authoring.md`](scenario-policy-authoring.md) |
| Policy-evaluator boundary | [`adr-002-policy-evaluator.md`](../architecture/adr-002-policy-evaluator.md) |
| Order-log schema | [`adr-003-order-log-schema.md`](../architecture/adr-003-order-log-schema.md) |
