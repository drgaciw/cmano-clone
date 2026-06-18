# ADR-009: Combat Domain Validators & Deterministic Damage Order

## Status

**Accepted** (S26-09 stub validators + `combatDomainsEnabled` default false; no runtime BDA)

## Date

2026-06-03

## Last Verified

2026-06-18 (S27-16: validation criteria 1–4 closed with file:line evidence; `combat-domains-smoke` test-only fixture)

## Decision Makers

Architecture review 2026-06-03; `combat-domains-damage.md` GDD

## Summary

Extend the unified engagement pipeline (ADR-001 / engagement GDD) with **pluggable domain validators** (air, surface, subsurface, land, mine, facility) that run **after policy** and **before launch**, plus a **deterministic damage application pass** ordered by `engagementId` then `sequenceId`. MVP Baltic continues platform-level kill/miss outcomes; validators start as no-op or single-domain (air/surface) stubs with stable `FireAbortReason` codes logged to the order log (ADR-003).

## Engine Compatibility

| Field | Value |
|-------|-------|
| Engine | Unity 6.3 LTS + .NET 8 headless |
| Unity APIs | None in validator hot path (plain C# in `ProjectAegis.Sim`) |
| Burst/Jobs | P2 — validators run pre-tick batch, not per-frame |
| Risk | LOW — extends existing `ProjectAegis.Sim.Engage` types |

## ADR Dependencies

| Relationship | ADR / artifact |
|--------------|----------------|
| **Depends on** | ADR-001 (deterministic sim), ADR-003 (order log), ADR-004 (engagement pipeline), ADR-006 (platform DB reads) |
| **Enables** | TR-combat-dom-001..003; scoring/BDA projections |
| **Blocks** | None for current Baltic MVP (engage stub remains valid) |
| **Conflicts with** | None |

## GDD Requirements Addressed

| TR-ID | GDD | Requirement |
|-------|-----|-------------|
| TR-combat-dom-001 | combat-domains-damage.md | Domain validator plug-in |
| TR-combat-dom-002 | combat-domains-damage.md | Deterministic damage order |
| TR-combat-dom-003 | combat-domains-damage.md | BDA feeds contact picture |

## Decision

### Validator pipeline

```
EngageIntent → IPolicyEvaluator → IDomainValidator.Validate(domain) → geometry/magazine → launch
```

- Interface: `IDomainValidator` in `ProjectAegis.Sim.Engage` (or `ProjectAegis.Sim.CombatDomains`)
- Registry: `DomainValidatorRegistry` keyed by `CombatDomain` enum; **stable iteration order** by domain id (ordinal)
- MVP: `AirDomainValidator`, `SurfaceDomainValidator` return `ValidateResult.Allow` unless data-driven abort configured
- Abort codes: namespaced strings (`AIR_ASPECT_BLOCK`, `ASW_NO_SOLUTION`) → order log via ADR-003

### Damage application

After `EngagementOutcomeRecord` collection for a tick:

```
applyOrder = outcomes.OrderBy(e => e.EngagementId).ThenBy(e => e.SequenceId)
```

- MVP: map `Kill` → `KilledTargetRegistry`; `Miss`/`Hit`/`Intercept` unchanged from current engage slice
- P1: component damage levels from GDD §4 formula

### BDA / contact picture

- `Kill` removes target from active contacts on next tick (existing contact lifecycle)
- Message log categories unchanged (`KILL_CONFIRMED`, etc.)

## Alternatives Considered

| Alternative | Rejected because |
|-------------|------------------|
| Per-domain engagement resolvers | Violates GDD AC-4 (single resolver) |
| Validators in Unity / MonoBehaviour | Breaks headless replay and ADR-006 |
| Non-deterministic validator RNG | Breaks replay-verify gate |

## Consequences

### Positive

- Closes TR-combat-dom gap in traceability
- Clear extension point for ASW, mines, facilities without forking engage core

### Negative

- Additional types in Sim assembly; must keep zero allocations in tick hot path (validators run only on engage intents, not every tick)

## Validation Criteria

- [x] `IDomainValidator` + registry with stable domain ordering — `src/ProjectAegis.Sim/Engage/IDomainValidator.cs:4-8`, `DomainValidatorRegistry.cs:6-14`, `DomainValidatorRegistryTests.cs:28-32`
- [x] Golden replay: same seed → same abort set (empty for MVP allow-all) — `CombatDomainsSmokePolicyTests.cs:33-44`, `DomainValidatorRegistryTests.cs:61-77`
- [x] Damage apply order test: shuffled input → sorted apply order — `DeterministicDamageApplyBatch.cs:6-9`, `DeterministicDamageApplyBatchTests.cs:24-49`
- [x] Order log contains abort code when validator denies — `DomainValidatorRegistryTests.cs:112-128`, `MvpEngagementResolver.cs:83-89`, `AirAspectDomainValidator.cs:17`

## Migration Plan

1. Add interfaces + no-op validators; wire into existing `MvpEngagement` path behind feature flag `combatDomainsEnabled`.
2. Implement air aspect stub + test.
3. Enable BDA contact removal hook (TR-combat-dom-003 partial).
4. Flip flag on for Baltic catalog scenario when golden tests pass.