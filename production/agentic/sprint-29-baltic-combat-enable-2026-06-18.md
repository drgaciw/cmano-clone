# S29-05 — Combat Domains Baltic Enablement Evidence

**Story:** `production/epics/sprint-29-combat-domains-phase3/story-029-05-baltic-combat-enable.md`  
**Date:** 2026-06-18  
**Owner:** team-simulation

## Summary

Isolated Baltic golden fixture `baltic-patrol-combat-domains` enables ADR-009 `combatDomainsEnabled=true` without touching production `baltic-patrol` or ReplayGolden 6/6 catalog hashes. Smoke pin `combat-domains-smoke` unchanged.

## Artifacts

| Artifact | Path |
|----------|------|
| Flag-on Baltic policy | `data/scenarios/baltic-patrol-combat-domains.policy.json` |
| Regression golden | `tests/regression/replay-golden-baltic-combat-domains-2026-06-18.txt` |
| Dedicated pin tests | `src/ProjectAegis.Delegation.UnityAdapter.Tests/Baltic/BalticCombatDomainsPolicyTests.cs` |
| Production flip policy doc | `data/scenarios/scenario-policy-ids.md` § Production Baltic `combatDomainsEnabled` policy |
| JSON loader coverage | `src/ProjectAegis.Sim.Tests/Scenario/ScenarioPolicyJsonLoaderTests.cs` |

## Pinned hashes (`baltic-patrol-combat-domains` seed=42 ticks=4)

| Field | Value |
|-------|-------|
| `WORLD_HASH` | `17144800277401907079` |
| `DETECTION_WORLD_HASH` | `15600` |
| `FINGERPRINT_SHA256` | `080a4cbf18f620043a4e6401ac1f60749e9604760b91d2adfc770de816649917` |

Allow-path note: hash matches flag-off `baltic-patrol` engage golden — domain validators active but air aspect allows engagement (no `AIR_ASPECT_BLOCK` / `|Abort|` in fingerprint).

## Isolation matrix

| Fixture | `combatDomainsEnabled` | Pin location | Changed? |
|---------|------------------------|--------------|----------|
| `baltic-patrol` | `false` | ReplayGolden 6/6 | No |
| `baltic-patrol-combat-domains` | `true` | `BalticCombatDomainsPolicyTests` + regression txt | **New** |
| `combat-domains-smoke` | `true` | `CombatDomainsSmokePolicyTests` (`17144800277401907079`) | No |

## Verify (2026-06-18)

| Suite | Result |
|-------|--------|
| Sim `Combat\|Domain\|Damage` | **55/55 PASS** |
| `ReplayGoldenSuiteTests` | **6/6 PASS** |
| `CombatDomainsSmoke` + `BalticCombatDomains` | **10/10 PASS** |
| `DelegationBridge.cs` diff | **empty** |

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone
dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj --filter "Combat|Domain|Damage" -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter "ReplayGoldenSuiteTests" -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter "CombatDomainsSmoke|BalticCombatDomains" -v minimal
git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs  # empty
```

## Post-merge production flip (documented, not executed)

1. Set `engage.combatDomainsEnabled=true` in `data/scenarios/baltic-patrol.policy.json`.
2. Run `/replay-verify` and `ReplayGoldenSuiteTests`.
3. Update `replay-golden-baltic-engage-2026-06-02.txt` only if world hash diverges from current pin.

## Constraints

- **ZERO** touch `DelegationBridge.cs` — confirmed empty diff.
- ReplayGolden catalog remains **6 cases**; existing golden file hashes unchanged.