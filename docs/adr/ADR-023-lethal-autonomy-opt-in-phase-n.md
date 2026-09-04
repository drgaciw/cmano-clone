# ADR-023: De-scoping Lethal Autonomy Phase Opt-In to Phase N and Interim Safety Gate (HOL-04B)

- **Status:** Accepted
- **Date:** 2026-09-04
- **Deciders:** Conductor Requirements Steward, Architecture Review Board, Delegation Stewards
- **Technical Story:** AEGIS-311 / DRG-241 (Audit Finding A-01, HOL-04 / HOL-04B)

## Context and Problem Statement

Audit finding **A-01** identified that `Game-Requirements/requirements/04-Agent-Delegation.md` historically graded the governance claim *"Full autonomous lethal engagement requires explicit player opt-in per mission phase"* as **Shipped**.

However, verification against the codebase confirms that `AutonomyGate.Evaluate` in `ProjectAegis.Delegation.Orchestration` returns `ExecuteNow` (via `GateResult(true, false, false)`) for units operating under `SemiAutonomous` or `FullAutonomous` tiers immediately following standard Rules of Engagement (ROE) evaluation. There is zero implementation of any `engage.lethalAutonomyOptIn` policy field, per-phase authorization token, or per-mission-phase gating flag anywhere in `src/`.

The capability was originally envisioned as human-on-the-loop governance (**HOL-04** in `Game-Requirements/requirements/24-Human-On-The-Loop-Authority.md`). Leaving an unbuilt software gate marked as "Shipped" compromises governance integrity and creates false assurances regarding autonomous weapon release authority. How should Project Aegis reconcile the unbuilt dynamic per-phase opt-in requirement with code reality while preserving operator safety?

## Decision Drivers

* **Truth in Governance**: Specification documents and compliance trackers must accurately reflect verifiable code behavior.
* **Operational Safety**: A clear, dependable mechanism must prevent unintended lethal engagements by autonomous units without relying on phantom phase gates.
* **Architectural Simplicity**: Avoid introducing complex dynamic phase-transition state machines into `AutonomyGate` during release stabilization.
* **Replay Determinism**: Any authority gating mechanism must be fully deterministic and replay-stable across simulation runs.

## Considered Options

* **Option 1**: Immediately design and implement dynamic per-mission-phase opt-in state machines, adding `engage.lethalAutonomyOptIn` to `ScenarioPolicyJsonDto`, updating `AutonomyGate`, and creating mission timeline phase-tracking events.
* **Option 2**: Completely remove the lethal autonomy opt-in requirement without defining an interim safety gate, relying entirely on unconstrained autonomous controller execution.
* **Option 3**: Formally **de-scope** dynamic per-mission-phase lethal autonomy opt-in to **Phase N / GAP** (under HOL-04), while formalizing the **interim HOL-04B safety gate** using shipped static doctrine / ROE thresholds, `PendingApprovalQueue`, and watch auto-pause.

## Decision Outcome

Chosen option: **Option 3**, because it honestly aligns requirements documentation with shipped code reality, avoids high-risk engine churn during release stabilization, and establishes an interim safety gate (HOL-04B) utilizing existing, battle-tested human-on-the-loop architecture.

### Decision Details

1. **De-scope Dynamic Phase Opt-in (HOL-04)**:
   - Formally de-scope the dynamic per-mission-phase lethal autonomy opt-in requirement from the v1.0 release stream.
   - Designate HOL-04 in `Game-Requirements/requirements/24-Human-On-The-Loop-Authority.md` as **Phase N / GAP** (Target Specification).
   - Revisit full dynamic phase authorization in post-v1 mission-command roadmaps.

2. **Specify Interim Safety Gate (HOL-04B)**:
   - Specify and ratify the interim safety architecture **HOL-04B** for scenarios requiring human oversight of autonomous units:
     - Operational authority is governed through static doctrine policies (`RoeLevel.HoldFire`, `RoeLevel.WeaponsTight`) or explicit autonomy tiers (`AutonomyLevel.Assisted` with `RiskLevel.High` / `AutonomyLevel.Manual`).
     - When higher authority approval is required (`RequiredApproval.Operator` or `WeaponsRelease`), fire orders are intercepted by `AutonomyGate` and routed to `PendingApprovalQueue` as `QueueForApproval = true`.
     - Per **AEGIS-301 (DRG-231)**, insertions into `PendingApprovalQueue` trigger `WatchAutoPauseGate`, pausing the simulation clock to guarantee operator countermand and supervised execution windows.
   - Maintain `AutonomyGate.Evaluate`'s current deterministic behavior where units under `SemiAutonomous` or `FullAutonomous` execute immediately only when the prevailing static ROE is `WeaponsFree`.

3. **Requirements Synchronization**:
   - Update requirement trackers (doc 04 mapping rows and doc 24 HOL-04/HOL-04B) to reflect that dynamic per-phase opt-in is a Phase N GAP, while interim safety is enforced via HOL-04B static doctrine gating and the pending approval queue.

### Positive Consequences

* Eliminates a critical audit defect (P0 finding A-01) by removing false claims of a shipped per-phase safety gate.
* Provides scenario authors and doctrine designers with a concrete, functional safety architecture (HOL-04B) using existing engine primitives.
* Retains deterministic simulation and replay golden stability without adding untested runtime phase dependencies.

### Negative Consequences

* Automated engine-level prevention of autonomous lethal engagement during specific mission phases is unavailable out-of-the-box; scenario authors must explicitly script or command ROE/policy transitions across mission phases.

## Pros and Cons of the Options

### Option 1: Implement Dynamic Phase Opt-in Immediately

* Good, because it would fulfill the original vision of HOL-04 in the current milestone.
* Bad, because it introduces significant architectural complexity (phase tracking, policy DTO expansion, UI controls) during release hardening.
* Bad, because it risks destabilizing existing autonomous mission tests and replay goldens.

### Option 2: Remove Requirement Completely Without Replacement

* Good, because it requires zero engineering or interim documentation.
* Bad, because it abandons human-on-the-loop governance principles for lethal systems.
* Bad, because it leaves operators without guidance on how to safely bound autonomous controllers.

### Option 3: De-scope to Phase N and Specify Interim HOL-04B Safety Gate (Chosen)

* Good, because it restores 100% truth in documentation and audit compliance.
* Good, because HOL-04B leverages existing, thoroughly verified infrastructure (`PendingApprovalQueue`, `WatchAutoPauseGate`, `EffectivePolicy`).
* Bad, because mission-phase transitions require explicit scenario scripting or operator intervention rather than automated policy binding.

## Implementation Notes

* **IMP-023.1**: `src/ProjectAegis.Delegation/Orchestration/AutonomyGate.cs` remains untouched in v1.0, preserving existing `SemiAutonomous` / `FullAutonomous` evaluations.
* **IMP-023.2**: In scenario scripting and mission editor configurations, mission phases requiring weapons hold must explicitly set `EffectivePolicy.Roe = RoeLevel.WeaponsTight` or `RoeLevel.HoldFire`.
* **IMP-023.3**: Verification is maintained by existing test fixtures in `AutonomyGateTests.cs` and `PendingApprovalQueueAutoPauseTests.cs`.

## References

* **REF-023.1**: `Game-Requirements/reviews/audit-findings-2026-09-02.md` (Finding A-01)
* **REF-023.2**: `Game-Requirements/requirements/04-Agent-Delegation.md` (Authority & Delegation)
* **REF-023.3**: `Game-Requirements/requirements/24-Human-On-The-Loop-Authority.md` (HOL-04 / HOL-04B)
* **REF-023.4**: `src/ProjectAegis.Delegation/Orchestration/AutonomyGate.cs`
* **REF-023.5**: `src/ProjectAegis.Delegation/Watch/WatchAutoPauseGate.cs` (AEGIS-301 / DRG-231)
