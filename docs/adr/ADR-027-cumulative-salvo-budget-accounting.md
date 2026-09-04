# ADR-027: Cumulative Salvo Budget Accounting across Engagement Windows

- **Status:** Accepted
- **Date:** 2026-09-04
- **Deciders:** Conductor Requirements Steward, Architecture Review Board, Simulation & Policy Stewards
- **Technical Story:** AEGIS-311 / DRG-241 (WRA Salvo Limits, Doc 13 ROE-04, Policy Evaluator)

## Context and Problem Statement

In naval and air combat doctrine, Weapons Release Authorization (WRA) rules define not merely the instantaneous launch quantity of a single salvo, but the maximum expenditure of ordnance authorized against a specific track or target before assessing battle damage (the doctrinal "shoot-look-shoot" cycle).

In the current simulation implementation (`src/ProjectAegis.Sim/Policy/PolicyEvaluator.cs`), the WRA salvo check is evaluated as follows:
```csharp
var salvo = Math.Max(1, ctx.SalvoSize);
if (salvo > policy.MaxSalvo)
{
    return PolicyVerdict.Deny(FireAbortReason.WraSalvo);
}
```

This implementation is **stateless**: it inspects only the instantaneous `SalvoSize` of the discrete incoming `ActionRequest` against `policy.MaxSalvo`.

Because there is no stateful tracking of cumulative rounds fired against a target within an active engagement window:
1. An agent or player can issue repeated single-round or sub-threshold fire commands on successive simulation ticks against the same target.
2. Each individual fire action passes the stateless `salvo <= policy.MaxSalvo` check.
3. The shooter can thus expend its entire magazine against a single target without ever triggering `FireAbortReason.WraSalvo`, circumventing the doctrinal intent of WRA salvo ceilings.

How should Project Aegis establish cumulative salvo budget accounting to enforce realistic WRA limits while maintaining pure-function policy evaluation and deterministic simulation execution?

## Decision Drivers

* **Doctrinal Fidelity**: Enforce realistic WRA salvo ceilings against individual targets to prevent unintended magazine exhaustion and unrealistic ordnance expenditure.
* **Deterministic Accounting**: Cumulative counters, budget allocations, and window timers must be 100% deterministic and replay-reproducible.
* **Separation of Concerns**: Avoid entangling the stateless `IPolicyEvaluator` contract with simulation state lifecycles and target tracking.
* **Memory and Performance Safety**: Engagement tracking entries must have bounded lifecycles that clean up automatically on target destruction, disengagement, or timeout.

## Considered Options

* **Option 1**: Retain purely stateless WRA salvo evaluation, accepting that `MaxSalvo` only limits instantaneous single-click salvo sizes, and rely on external controller scripting to prevent over-expenditure.
* **Option 2**: Introduce mutable engagement history tables directly into `PolicyEvaluator`, transforming it from a pure evaluation function into a stateful service.
* **Option 3**: Establish a stateful cumulative salvo tracking mechanism (`SalvoBudgetLedger` / engagement window tracker) at the simulation resolver layer (`ProjectAegis.Sim.Engage`), passing cumulative window state into `PolicyContext` or evaluating cumulative limits before weapon dispatch.

## Decision Outcome

Chosen option: **Option 3**, because it preserves the architectural purity of `IPolicyEvaluator`, enforces authentic cumulative WRA limits across engagement windows, and provides deterministic, leak-free lifecycle management in the simulation core.

### Decision Details

1. **Establish Engagement Window Cumulative Accounting**:
   - Track cumulative ordnance expended per `(ShooterUnitId, TargetId, WeaponTypeId)` key tuple.
   - Cumulative accounting applies across an active **Engagement Window**:
     - The window opens on the first authorized weapon release against a target.
     - Each dispatched round increments the cumulative expenditure counter.
     - Subsequent fire requests against the same target are checked: if `currentExpenditure + requestedSalvo > effectivePolicy.MaxSalvo`, the request is denied with `FireAbortReason.WraSalvo` / `EngagementAbortReason.WraSalvo`.

2. **Define Engagement Window Lifecycle**:
   - An engagement window automatically closes and its cumulative budget resets upon any of the following lifecycle events:
     - **Target Destruction**: Target entity is registered in `KilledTargetRegistry`.
     - **Explicit Retasking**: Shooter receives a command to disengage or retarget.
     - **Doctrinal Assessment Cooldown (BDA Window)**: An elapsed time window (defaulting to estimated time-of-flight plus BDA observation duration) expires without further engagements.

3. **Architectural Placement and Ledger Design**:
   - Retain `IPolicyEvaluator` as a pure, deterministic verdict calculator.
   - Maintain the cumulative salvo ledger within `ProjectAegis.Sim.Engage` alongside `MagazineLedger` and `KilledTargetRegistry`.
   - The resolver updates `PolicyContext.SalvoSize` with the cumulative projection or explicitly queries the salvo ledger during `MvpEngagementResolver.Resolve`.

4. **Multi-Shooter and Swarm Coordination**:
   - For coordinated multi-shooter or swarm attacks, target-allocated salvo budgets interact with `SwarmSalvoDeconfliction` (TR-engage-003a) to ensure collective fire does not exceed assigned target WRA caps.

### Positive Consequences

* Enforces authentic military doctrine: platforms cannot spam continuous micro-salvos against a single target beyond authorized WRA caps.
* Protects scarce ammunition stocks by preventing over-engagement before battle damage assessment.
* Keeps policy evaluation pure, testable, and deterministic.
* Automatic lifecycle pruning prevents memory leaks in long-running scenarios.

### Negative Consequences

* Introduces additional state tracking in `Sim.Engage` that must be checkpointed and restored during scenario serialization and replay runs.
* Scenario designers must account for BDA cooldown windows when scripting scenarios with intended rapid follow-up strikes.

## Pros and Cons of the Options

### Option 1: Retain Stateless Evaluation

* Good, because it requires zero architectural changes or state tracking.
* Bad, because it allows continuous micro-salvo spam that invalidates WRA governance.
* Bad, because it fails milsim realism benchmarks for naval missile defense and strike operations.

### Option 2: Stateful `PolicyEvaluator`

* Good, because all policy logic is centralized in one class.
* Bad, because it pollutes policy evaluation with simulation entity lifecycles, target tracking, and timer expirations.
* Bad, because it breaks pure unit testing of policy evaluation without setting up mocked world timers.

### Option 3: Cumulative Salvo Accounting via Engagement Window Ledger (Chosen)

* Good, because it accurately models authentic WRA salvo budgeting and shoot-look-shoot doctrine.
* Good, because it keeps `IPolicyEvaluator` pure while locating state in the simulation engine.
* Good, because window lifecycle is bound to explicit simulation events (kills, timeouts, retasking).
* Bad, because engagement ledger state must be included in save/load and replay checkpoint schemas.

## Implementation Notes

* **IMP-027.1**: The engagement ledger tracks entries keyed by `(ShooterUnitId, TargetId, WeaponTypeId)`.
* **IMP-027.2**: `EngagementAbortReason.WraSalvo` is returned whenever `cumulativeSpent + requestedSalvo > policy.MaxSalvo`.
* **IMP-027.3**: Integration with `DecisionLog` ensures that cumulative denials are logged with clear diagnostic metadata (`current_expended`, `requested`, `max_salvo`).

## References

* **REF-027.1**: `Game-Requirements/requirements/13-Rules-of-Engagement.md` (ROE-04 / WRA Salvo)
* **REF-027.2**: `Game-Requirements/requirements/04-Agent-Delegation.md` (Doctrinal Constraints)
* **REF-027.3**: `src/ProjectAegis.Sim/Policy/PolicyEvaluator.cs`
* **REF-027.4**: `src/ProjectAegis.Sim/Engage/MvpEngagementResolver.cs`
* **REF-027.5**: `docs/architecture/adr-002-policy-evaluator.md`
