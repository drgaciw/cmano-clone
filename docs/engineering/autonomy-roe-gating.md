# Autonomy & ROE gating runtime

Every order a delegation agent chooses passes one authorization checkpoint before
it can be issued: [`AutonomyGate.Evaluate`](../../src/ProjectAegis.Delegation/Orchestration/AutonomyGate.cs).
The gate answers a single question — *"may this order execute now, must it wait for
a human, or is it illegal?"* — by combining a **legality** check (Rules of
Engagement) with a **delegation** check (the unit's `AutonomyLevel`). ROE is
evaluated **first and is terminal**: a rejected order can never be un-rejected by
player approval or a permissive autonomy level. This page traces that runtime end
to end and the two-layer ROE split behind it.

> **Scope.** This is the *decision-time* authorization gate inside
> `DelegationOrchestrator.Tick`. It sits between the trait-weighted
> [decision pipeline](agent-decision-pipeline.md) (which *chooses* an order) and
> the order log. The downstream **kill-chain** gate chain that fires the weapon
> (readiness, EMCON, track, magazine, DLZ, …) lives in the sim engagement
> resolver — see [engagement-pipeline.md](engagement-pipeline.md). The stable
> machine-readable denial codes are catalogued in
> [abort-reason-catalog.md](abort-reason-catalog.md); the JSON that sets a unit's
> ROE level is in [scenario-policy-authoring.md](scenario-policy-authoring.md).
> Decision source: [ADR-002](../architecture/adr-002-policy-evaluator.md)
> (policy evaluator boundary) and [ADR-003](../architecture/adr-003-order-log-schema.md)
> (the `PolicyDenial` order-log row).

---

## The gate, end to end

```
AgentController.TryDecide
  DecisionPipeline.Choose ──► Order(kind, DefaultRiskClassifier.Classify(kind))
                                         │
                                         ▼
                       AutonomyGate.Evaluate(autonomy, order, playerApproved)
                                         │
                    ┌────────────────────┴─ (1) ROE FIRST ─────────────────────┐
                    ▼                                                            │
          IRoeFilter.Evaluate(order)                                            │
        (RoePolicyAdapter → sim IPolicyEvaluator)                              │
                    │                                                           │
        Reject? ──► GateResult(Rejected=true, reason)  ── terminal ────────────┘
                    │ Allow
                    ▼  (2) AUTONOMY SWITCH
        Manual / Assisted / SemiAutonomous / FullAutonomous
                    │
                    ▼
        GateResult(ExecuteNow?, QueueForApproval?)
                    │
     ┌──────────────┼───────────────────────────────┐
     ▼              ▼                                 ▼
 ExecuteNow    QueueForApproval                  Rejected + reason
 _issued.Add   (not issued this tick)     log.Append(PolicyDenialRecord)
```

A `DecisionRecord` is logged for **every** decision regardless of the verdict; a
`PolicyDenialRecord` is appended **only** on an ROE reject; and the order is added
to the agent's issued set **only** when `ExecuteNow` is true.

---

## `AutonomyGate.Evaluate` — legality then delegation

`AutonomyGate` is constructed with an `IRoeFilter` and returns a `GateResult`:

```csharp
public sealed record GateResult(
    bool ExecuteNow,
    bool QueueForApproval,
    bool Rejected,
    FireAbortReason PolicyDenialReason = FireAbortReason.None);
```

`Evaluate(AutonomyLevel autonomy, Order order, bool playerApproved)` runs two
stages:

1. **ROE (legality) first.** `_roe.Evaluate(order)`; if the verdict is `Reject`,
   return `GateResult(ExecuteNow: false, QueueForApproval: false, Rejected: true,
   reason)` **immediately**. A rejected order is terminal — it does not fall
   through to the autonomy switch and does **not** enter the approval queue.
2. **Autonomy (delegation) second.** Only reached when ROE allows:

| [`AutonomyLevel`](../../src/ProjectAegis.Delegation/Core/AutonomyLevel.cs) | Order risk | `ExecuteNow` | `QueueForApproval` |
|--------------------|------------|--------------|--------------------|
| `Manual` (1) | any | `playerApproved` | `!playerApproved` |
| `Assisted` (2) | `Low` | `true` | `false` |
| `Assisted` (2) | `High` | `playerApproved` | `!playerApproved` |
| `SemiAutonomous` (3) | any | `true` | `false` |
| `FullAutonomous` (4) | any | `true` | `false` |

So `Manual` never auto-executes; `Assisted` auto-executes only `Low`-risk work and
holds `High`-risk (i.e. `Engage`) for a human; `SemiAutonomous` and
`FullAutonomous` auto-execute anything ROE permits.

> **The load-bearing ordering invariant.** ROE is checked *before* the autonomy
> switch and *before* `playerApproved` is consulted, so **player approval can never
> override an ROE reject** — even on `Manual`. This is pinned by
> `AutonomyGateTests.player_approval_cannot_override_roe_reject_even_on_manual`
> (a `HoldFire` engage with `playerApproved: true` still returns `Rejected` with
> `PolicyDenialReason = RoeHoldFire`, and does **not** queue). A regression that
> reorders these two stages, or that treats "approved" as a superuser bypass,
> fails that test.

---

## `IRoeFilter` — the legality seam

```csharp
public enum RoeVerdict { Allow, Reject, Queue }

public interface IRoeFilter
{
    RoeEvaluation Evaluate(Order order);   // (RoeVerdict Verdict, FireAbortReason Reason)
}
```

Two implementations ship:

- **`PassthroughRoeFilter`** — always `Allow()`. Used in tests and any run with no
  policy wiring; makes the gate a pure autonomy switch.
- **`RoePolicyAdapter`** (the default the orchestrator installs) — bridges the
  legacy `IRoeFilter` seam to the sim `IPolicyEvaluator` per ADR-002.

The gate only special-cases `RoeVerdict.Reject`; `RoeEvaluation` itself only ever
produces `Allow()` or `Reject(reason)` (the `Queue` verdict is reserved in the enum
but unused by the shipped adapters).

---

## `RoePolicyAdapter` — bridge to the sim policy evaluator (ADR-002)

`RoePolicyAdapter.Evaluate(order)` does three things:

1. Build a `PolicyContext` via an injected `contextFactory`. The orchestrator wires
   [`PolicySnapshotRegistry.CreateContext`](../../src/ProjectAegis.Delegation/Orchestration/PolicySnapshotRegistry.cs)
   here, so the context carries the **policy snapshot pinned for that unit** (see
   below). Standalone use falls back to `DefaultContext` (`EffectivePolicy.DefaultFree`).
2. Map the order to an `ActionRequest` via `OrderActionMapper.ToActionRequest`.
3. Call `_evaluator.Evaluate(ctx, request)` and translate the `PolicyVerdict` back:
   `Allowed ? RoeEvaluation.Allow() : RoeEvaluation.Reject(verdict.Reason)`.

### `OrderActionMapper` — order kind → policy action

Only `Engage` maps to a fire action, so only engage orders are ROE-gated at this
layer:

| `OrderKind` | `ActionKind` | Fire-gated? |
|-------------|--------------|-------------|
| `Engage` | `FireGuided` | **yes** |
| `SetEwPosture` | `Jam` | no (EMCON is gated later, in the engagement pipeline) |
| `Move` / `Hold` / `ReturnToBase` / (default) | `Observe` | no |

`TargetIdToUlong` folds the string `TargetId` into a `ulong` with a deterministic
`hash = hash*31 + c` accumulation (stable, culture-independent) so the sim layer
can key on a numeric unit id.

### The two-layer ROE/WRA split

The delegation gate answers *"is this action legal for this unit?"*; the sim
[`PolicyEvaluator`](../../src/ProjectAegis.Sim/Policy/PolicyEvaluator.cs) is where
the actual ROE + weapons-release-authority (WRA) rules live:

- Resolve the effective policy: the pinned snapshot's `Effective` when
  `PolicySnapshotId != 0`, otherwise `_resolvePolicy(unitId)`.
- **Non-fire actions always `Allow()`** (`Observe` / `Jam` pass straight through).
- Fire actions (`FireBallistic` / `FireGuided`) run the ROE ladder, then the WRA
  salvo cap:

| Effective `RoeLevel` | Verdict | `FireAbortReason` |
|----------------------|---------|-------------------|
| `HoldFire` (0) | Deny | `RoeHoldFire` |
| `WeaponsTight` (1) | Deny | `WeaponsTight` |
| `WeaponsFree` (2) | Allow (subject to salvo) | — |
| salvo `> EffectivePolicy.MaxSalvo` (default `8`) | Deny | `WraSalvo` |

`EffectivePolicy` is `(RoeLevel Roe, int MaxSalvo = 8)`; `DefaultFree` is
`WeaponsFree`. These are the same `FireAbortReason` codes that appear in the
`POLICY_DENIAL` family of the [abort-reason catalog](abort-reason-catalog.md).

### Per-unit policy snapshots

`PolicySnapshotRegistry.Capture(targetId, effective, capturedAtSimTick)` stamps a
monotonically-increasing `PolicySnapshotId` and stores the `EffectivePolicy` for a
unit. `CreateContext(order)` then returns that snapshot's policy (and its id) if
one exists, or `(PolicySnapshotId: 0, EffectivePolicy.DefaultFree)` otherwise. This
is what lets an ROE change (e.g. a `mission.triggers` escalation to `WeaponsFree`)
be *pinned* at the moment of decision and be reflected verbatim in the logged
`PolicyDenialRecord.PolicySnapshotId` — see
[mission-timeline-runtime.md](mission-timeline-runtime.md).

---

## Risk classification

`Order.Risk` is stamped by
[`DefaultRiskClassifier.Classify`](../../src/ProjectAegis.Delegation/Roe/DefaultRiskClassifier.cs)
when the `AgentController` builds the order:

```csharp
OrderKind.Engage => RiskLevel.High,
_               => RiskLevel.Low,
```

`RiskLevel` is only `High` for `Engage`, which is exactly the case the `Assisted`
branch withholds for human approval.

---

## Where the verdict lands (the `AgentController` path)

In [`AgentController.TryDecide`](../../src/ProjectAegis.Delegation/Controllers/AgentController.cs)
the gate is always evaluated with **`playerApproved: false`** (agents never
self-approve):

1. Append the `DecisionRecord` for the chosen order — **always**, so the order log
   shows the reasoning even when nothing executes.
2. `gateResult = gate.Evaluate(Autonomy, order, playerApproved: false)`.
3. If `Rejected && PolicyDenialReason != None` → append a `PolicyDenialRecord`
   (`SequenceId`, `SimTime`, `SimTick`, `AgentId`, `TargetId`, `PolicySnapshotId`,
   `Reason`, `AttemptedKind`).
4. If `ExecuteNow` → add the order to the agent's issued set.

Note that a `QueueForApproval` result from the *agent* path is not routed to a
separate pending-approval store here — the order is simply **not issued this tick**.
Player-issued/approved orders arrive through the human ingress
(`HumanController` / the player order queue) rather than the agent decide loop.

---

## Determinism & replay safety

Unlike the authoring-path subsystems, this gate runs **on the simulation hotpath**
(every agent, every decision tick), so it is bound by the replay invariant. It is
safe because it is a **pure function**: no RNG draw, no `DateTime.UtcNow`, a plain
`switch` over `(RoeVerdict, AutonomyLevel, RiskLevel)`, and an injected
`PolicyContext`. `PolicyDenialRecord` rows are part of the append-only order log and
therefore contribute to `DecisionLog.ComputeFingerprint()` — so changing a denial
**reason**, its **ordering**, or *whether* a denial is emitted will change the
order-log hash and break the Baltic replay goldens (v2 hash
`17144800277401907079`). `AutonomyGate` is a distinct type from `DelegationBridge`;
it does not touch the zero-touch bridge hotpath.

---

## Extending safely

- **New autonomy behavior.** Edit the `switch` in `AutonomyGate.Evaluate` and add a
  case to `AutonomyGateTests`. Keep ROE **before** the switch — never let a new
  branch execute before the legality check.
- **New ROE/WRA rule.** Add it to the sim `PolicyEvaluator` (or a new
  `IPolicyEvaluator`), reusing an existing `FireAbortReason` or adding one via the
  [abort-reason-catalog](abort-reason-catalog.md) manifest → codegen workflow — do
  not invent an ad-hoc string reason.
- **New order kind that should be ROE-gated.** Map it to a fire `ActionKind` in
  `OrderActionMapper.MapKind` and classify it `High` in `DefaultRiskClassifier`;
  otherwise it stays an `Observe` action and passes ROE unconditionally.
- **Regenerate replay goldens intentionally** for any change that alters which
  denials are emitted or in what order.

## Verify against source

| Concern | Source of truth |
|---------|-----------------|
| Gate two-stage logic, `GateResult` | `src/ProjectAegis.Delegation/Orchestration/AutonomyGate.cs` |
| ROE seam / verdicts | `src/ProjectAegis.Delegation/Roe/IRoeFilter.cs`, `RoeEvaluation.cs`, `PassthroughRoeFilter.cs` |
| Bridge to sim policy (ADR-002) | `src/ProjectAegis.Delegation/Roe/RoePolicyAdapter.cs` |
| Order → action mapping / target hash | `src/ProjectAegis.Delegation/Roe/OrderActionMapper.cs` |
| Risk classification | `src/ProjectAegis.Delegation/Roe/DefaultRiskClassifier.cs` |
| ROE + WRA rules, effective policy | `src/ProjectAegis.Sim/Policy/PolicyEvaluator.cs`, `EffectivePolicy.cs`, `RoeLevel.cs`, `FireAbortReason.cs` |
| Per-unit policy snapshot / context | `src/ProjectAegis.Delegation/Orchestration/PolicySnapshotRegistry.cs`, `src/ProjectAegis.Sim/Policy/PolicyContext.cs` |
| Verdict → order log | `src/ProjectAegis.Delegation/Controllers/AgentController.cs`, `Decision/PolicyDenialRecord.cs` |
| Pinned behaviour | `ProjectAegis.Delegation.Tests/Roe/AutonomyGateTests.cs`, `RoePolicyAdapterTests.cs`, `ProjectAegis.Sim.Tests/Policy/PolicyEvaluatorTests.cs` |

## See also

| Doc | Why |
|-----|-----|
| [agent-decision-pipeline.md](agent-decision-pipeline.md) | Upstream: how the order the gate evaluates is chosen (traits, attention, softmax). |
| [engagement-pipeline.md](engagement-pipeline.md) | Downstream: the tick-8 kill-chain gate chain that fires the weapon after this gate authorizes it. |
| [abort-reason-catalog.md](abort-reason-catalog.md) | The `POLICY_DENIAL` / `ENGAGE_ABORT` machine-readable code catalog. |
| [scenario-policy-authoring.md](scenario-policy-authoring.md) | How `*.policy.json` sets the `RoeLevel` / `MaxSalvo` this gate enforces. |
| [mission-timeline-runtime.md](mission-timeline-runtime.md) | Contact-triggered ROE escalation that re-captures a unit's policy snapshot. |
| [determinism-and-replay.md](determinism-and-replay.md) | The ordering / hashing rules the `PolicyDenial` rows must respect. |
| [../architecture/adr-002-policy-evaluator.md](../architecture/adr-002-policy-evaluator.md) | The policy-evaluator boundary decision. |
