# S29-05 story-done — Combat Domains Baltic Enablement

**Story:** `production/epics/sprint-29-combat-domains-phase3/story-029-05-baltic-combat-enable.md`  
**Status:** Complete  
**Completed:** 2026-06-18

## Verdict: COMPLETE

| AC | Test / Evidence | Status |
|----|-----------------|--------|
| `combatDomainsEnabled=true` on isolated Baltic golden | `baltic-patrol-combat-domains.policy.json`, `BalticCombatDomainsPolicyTests` | COVERED |
| New Baltic golden world-state hash pinned; replay-verify PASS | `replay-golden-baltic-combat-domains-2026-06-18.txt` | COVERED |
| `ReplayGoldenSuiteTests` — 6/6 PASS | catalog unchanged | COVERED |
| `combat-domains-smoke` separate pin unchanged | `CombatDomainsSmokePolicyTests` `17144800277401907079` | COVERED |
| Production Baltic fixture policy documented | `scenario-policy-ids.md` | COVERED |
| Domain validators active on flag-on Baltic path | allow-path fingerprint; no abort codes; Sim `DomainValidatorRegistryTests` | COVERED |
| ZERO touch `DelegationBridge` | empty diff | COVERED |

## Pinned hash (`baltic-patrol-combat-domains` seed=42 ticks=4)

| Field | Value |
|-------|-------|
| `WORLD_HASH` | `17144800277401907079` |
| `DETECTION_WORLD_HASH` | `15600` |
| `FINGERPRINT_SHA256` | `080a4cbf18f620043a4e6401ac1f60749e9604760b91d2adfc770de816649917` |

## Verify

```bash
export PATH="/home/username01/.dotnet:$PATH"
dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj --filter "Combat|Domain|Damage" -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter "ReplayGoldenSuiteTests" -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter "CombatDomainsSmoke|BalticCombatDomains" -v minimal
```

Evidence: `production/agentic/sprint-29-baltic-combat-enable-2026-06-18.md`