# ADR-023: De-scoping Lethal Autonomy Mission Phase Opt-in Requirement (HOL-04)

## Status

**Proposed** (2026-09-02)

## Date

2026-09-02

## Context

Audit finding **A-01** identified that `Game-Requirements/requirements/04-Agent-Delegation.md` graded the claim *"Full autonomous lethal engagement requires explicit player opt-in per mission phase"* as **Shipped**.

However, verification against the codebase confirms that `AutonomyGate.Evaluate` in `ProjectAegis.Delegation` returns `ExecuteNow` for units operating under `SemiAutonomous` or `FullAutonomous` modes immediately following standard ROE evaluation. There is zero implementation of any `engage.lethalAutonomyOptIn` policy field or per-mission-phase gating flag in `src/`.

The capability was originally envisioned as human-on-the-loop governance (HOL-04).

## Decision

1. **De-scope** the per-mission-phase lethal autonomy opt-in requirement for the current release stream.
2. Maintain the current behavior where `AutonomyGate` respects `AutonomyLevel` and ROE thresholds directly without requiring a separate per-phase opt-in.
3. Retain the concept of mission-phase opt-in for future consideration in post-v1 authority architecture (HOL-04 in Phase N / follow-up governance tracks).
4. Update requirement documentation (doc 04 mapping rows per W1-HUB tracker pins) to reflect status as **Partial / GAP** rather than claiming it as shipped.

## Consequences

### Positive
- Requirements accurately match the shipped implementation in `ProjectAegis.Delegation`.
- Eliminates discrepancy where documentation claimed a security/governance safety gate that did not exist in code.

### Negative
- Scenarios requiring hard software prevention of autonomous engagement during specific mission phases must rely on operational doctrine (e.g. setting ROE / AutonomyLevel explicitly per phase via mission scripting or player command) rather than an automated engine-level phase gate.
