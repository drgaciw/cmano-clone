# S27-16 story-done — ADR-009 Validation Checklist Closeout

**Story:** `production/epics/sprint-27-adr009-bounded/story-027-16-adr009-checklist.md`  
**Status:** Complete  
**Completed:** 2026-06-18

## Verdict: COMPLETE

| AC | Test / Evidence | Status |
|----|-----------------|--------|
| ADR-009 criteria 1–4 marked with file:line evidence | `docs/architecture/adr-009-combat-domain-validators.md` validation section | COVERED |
| Flag-on smoke fixture documented | `data/scenarios/combat-domains-smoke.policy.json`, `scenario-policy-ids.md` | COVERED |
| Separate hash pin (not Baltic golden) | `CombatDomainsSmokePolicyTests` world hash `17144800277401907079` | COVERED |
| Baltic `combatDomainsEnabled=false` unchanged | ReplayGolden 6/6 | COVERED |
| ZERO touch `DelegationBridge` | no bridge edits | COVERED |

## Pinned hash (combat-domains-smoke seed=42 ticks=4)

| Field | Value |
|-------|-------|
| `WORLD_HASH` | `17144800277401907079` |
| `DETECTION_WORLD_HASH` | `15600` |

## Verify

```bash
dotnet test src/ProjectAegis.Sim.Tests --filter "CombatDomains|DomainValidator|DeterministicDamage" -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests --filter "CombatDomainsSmoke" -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests --filter "ReplayGoldenSuiteTests" -v minimal
```