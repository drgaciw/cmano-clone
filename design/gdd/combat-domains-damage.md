# Combat Domains & Damage

> **Status:** Draft — Sprint 4 design pass  
> **Author:** design-system  
> **Last Updated:** 2026-06-02  
> **Implements Pillar:** Simulation fidelity  
> **Requirements:** [18-Combat-Domains.md](../../Game-Requirements/requirements/18-Combat-Domains.md)  
> **Depends on:** [engagement-fire-control.md](engagement-fire-control.md), [sensor-detection-ew.md](sensor-detection-ew.md), [logistics-magazines.md](logistics-magazines.md), [order-log-replay.md](order-log-replay.md)

## Overview

Extends the unified engagement pipeline with **domain validators** (air, surface, subsurface, land, mine, facility) and a **deterministic damage model** that feeds BDA, losses, and logistics. MVP Baltic slice uses platform-level outcomes (hit/miss/kill/intercept) already logged; this GDD formalizes domain rules for expansion beyond the engage stub.

## Player Fantasy

Every domain obeys the same staff logic: you always get a logged reason when a torpedo cannot fire or a runway strike degrades sorties—not a hidden mini-game.

## Detailed Design

### Domain validators (post-policy, pre-launch)

```
EngageIntent → IPolicyEvaluator → DomainValidator(domain).Validate() → geometry/magazine → launch
```

| Domain | MVP scope | Validator examples |
|--------|-----------|-------------------|
| Air | P0 Baltic | BVR/WVR envelope, aspect gate |
| Surface | P0 | Radar horizon, CIWS last-ditch |
| Subsurface | P0 partial | Classification ≥ Classified for ASW fire |
| Land | P1 | Fixed SAM / runway only |
| Mine | P1 | Transit hazard areas |
| Facility | P1 | Capacity degradation |

Validators emit `FireAbortReason` in domain namespaces (`ASW_NO_SOLUTION`, `AIR_ASPECT_BLOCK`).

### Damage application (MVP: platform level)

After `EngagementOutcomeRecord`:

| Outcome | Platform effect |
|---------|-----------------|
| Miss | No state change |
| Hit | Light damage flag (sensor degrade P1) |
| Kill | `KilledTargetRegistry` + remove from OOB |
| Intercept | Threat neutralized; target remains |

**Order:** Apply damage sorted by `engagementId`, then `sequenceId`.

### BDA

Contact picture merges BDA: `destroyed` targets show in message log (`KILL_CONFIRMED`) and drop from active contacts. Re-attack prompts in Assisted mode (C5).

### Logging

All domain aborts and damage steps append to order log (ADR-003). No parallel combat log.

## Formulas

### Damage level (P1 component model)

```
damageLevel = clamp(0, 3, floor(hitSeverity * platform.resilience))
```

| Variable | Range | Meaning |
|----------|-------|---------|
| hitSeverity | 0–1 | Weapon vs target table |
| resilience | 0.5–2 | Platform DB |

MVP uses binary alive/killed via `EngagementOutcomeCodes.Kill`.

### Deterministic ordering

```
applyOrder = engagements.OrderBy(e => e.EngagementId).ThenBy(e => e.SequenceId)
```

## Edge Cases

| Scenario | Behavior |
|----------|----------|
| Kill + contact Lost same tick | Kill wins; contact removed after damage apply |
| Friendly mis-ID | Policy + deconflict block before validator |
| Multi-domain salvo | One engagement id; domain = primary weapon domain |
| Facility at 0 HP | Runway sortie cap = 0 until repair event (P1) |

## Dependencies

| System | Needs from this GDD | This GDD needs |
|--------|---------------------|----------------|
| Engagement | Domain validator interface | Pipeline hooks |
| Sensors | BDA on contact | Contact lifecycle |
| Logistics | Magazine + repair | Kill outcomes |
| C2 UI | Damage badges | Unit detail projection |
| Scoring | Loss counts | Kill records |

## Tuning Knobs

| Knob | Safe range | Effect |
|------|------------|--------|
| `resilience` | 0.5–2.0 | Time-to-kill |
| `aspectPenalty` | 0–0.5 | Air PK modifier |
| `staleBdaTicks` | 10–600 | How long damaged-unknown persists |

## Acceptance Criteria

1. Same seed + intents → same domain abort set and kill set (replay golden).
2. Every domain abort maps to a `PolicyDenial` or engage abort code in order log.
3. Kill removes unit from `OobTreeProjection` on next tick.
4. No second engagement resolver per domain.

## UI Requirements

- Right unit panel: damage state label (ALIVE / DESTROYED / DAMAGED P1).
- Message log: domain-specific categories remain prefixed (`KILL_CONFIRMED`, `HIT`, `MISS`).
- Map symbology: struck unit icon state (deferred to C2 UX spec).

## TR IDs

| ID | Requirement |
|----|-------------|
| TR-combat-dom-001 | Domain validator plug-in |
| TR-combat-dom-002 | Deterministic damage order |
| TR-combat-dom-003 | BDA feeds contact picture |