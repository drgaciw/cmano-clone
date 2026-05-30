# ADR-001: Simulation vs Delegation Assembly Boundary

**Status:** Accepted  
**Date:** 2026-05-29

## Context

`ProjectAegis.Delegation` exists with orchestrator, controllers, and `DecisionLog`. Requirements 13–18 define combat, sensors, and policy in the **world sim**, not in the controller layer.

## Decision

Create **`ProjectAegis.Sim`** for world truth (entities, sensors, engagements, policy evaluation, logistics). **Delegation** consumes `ISimWorldSnapshot` and emits `Order` only.

## Consequences

- **Positive:** Clear testability; headless sim without Unity; matches delegation framework spec §1.
- **Negative:** Bridge must assemble snapshots each tick; temporary duplication until Sim exists.
- **GitNexus:** Re-analyze after new assembly is indexed.
