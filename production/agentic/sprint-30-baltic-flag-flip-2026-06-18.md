# S30-09 — Production Baltic `combatDomainsEnabled` Flip (Producer Decision)

**Story:** `production/epics/sprint-30-combat-domains-phase4/story-030-09-baltic-flag-flip.md`  
**Date:** 2026-06-18  
**Owner:** team-simulation  
**Producer decision:** **APPROVED** 2026-06-18

## Decision

Flip `engage.combatDomainsEnabled=true` on production `data/scenarios/baltic-patrol.policy.json` (ADR-009 migration step 4).

**Rationale:**

- S29-05 isolated golden (`baltic-patrol-combat-domains`) proved allow-path determinism with validators active — world hash `17144800277401907079` matches pre-flip production engage golden.
- S30-05 landed `LandAspectDomainValidator`; all four aspect validators (air/surface/subsurface/land) are registered and flag-gated.
- ReplayGolden 6/6 catalog and `combat-domains-smoke` pin remain isolated; no hash drift expected on production flip.

**Constraints honored:**

- ZERO touch `DelegationBridge.cs`
- Isolated pins (`baltic-patrol-combat-domains`, `combat-domains-smoke`) unchanged
- `/replay-verify` mandatory before merge

## Artifacts

| Artifact | Path |
|----------|------|
| Production policy flip | `data/scenarios/baltic-patrol.policy.json` |
| Production engage golden | `tests/regression/replay-golden-baltic-engage-2026-06-02.txt` |
| Isolated combat golden (unchanged) | `tests/regression/replay-golden-baltic-combat-domains-2026-06-18.txt` |
| Policy tests | `src/ProjectAegis.Delegation.UnityAdapter.Tests/Baltic/BalticCombatDomainsPolicyTests.cs` |
| Policy doc | `data/scenarios/scenario-policy-ids.md` |

## Pinned hash (`baltic-patrol` seed=42 ticks=4, post-flip)

| Field | Value | Changed? |
|-------|-------|----------|
| `WORLD_HASH` | `17144800277401907079` | **No** — allow-path unchanged |
| `DETECTION_WORLD_HASH` | `15600` | **No** |
| `FINGERPRINT_SHA256` | `080a4cbf18f620043a4e6401ac1f60749e9604760b91d2adfc770de816649917` | **No** |

Allow-path note: domain validators active; no `AIR_ASPECT_BLOCK` / `SURFACE_ASPECT_BLOCK` / `SUBSURFACE_ASPECT_BLOCK` / `LAND_ASPECT_BLOCK` / `|Abort|` in fingerprint.

## Verify (2026-06-18)

| Suite | Result |
|-------|--------|
| Sim `Combat\|Domain\|Damage` | **73/73 PASS** |
| `ReplayGoldenSuiteTests` | **6/6 PASS** |
| `CombatDomainsSmoke\|BalticCombatDomains` | **11/11 PASS** |
| `DelegationBridge.cs` diff | **empty** |

## Isolation matrix (post-flip)

| Fixture | `combatDomainsEnabled` | Pin location | Changed? |
|---------|------------------------|--------------|----------|
| `baltic-patrol` | `true` | ReplayGolden 6/6 engage golden | **Flag only** — hash unchanged |
| `baltic-patrol-combat-domains` | `true` | `BalticCombatDomainsPolicyTests` | **No** |
| `combat-domains-smoke` | `true` | `CombatDomainsSmokePolicyTests` | **No** |