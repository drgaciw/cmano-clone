# S30-09 story-done — Production Baltic `combatDomainsEnabled` Flip

**Story:** `production/epics/sprint-30-combat-domains-phase4/story-030-09-baltic-flag-flip.md`  
**Status:** Complete  
**Completed:** 2026-06-18  
**Branch:** `stack/sprint30/baltic-flag-flip`

## Verdict: COMPLETE

| AC | Test / Evidence | Status |
|----|-----------------|--------|
| Producer approval documented before merge | `production/agentic/sprint-30-baltic-flag-flip-2026-06-18.md` APPROVED | COVERED |
| `combatDomainsEnabled=true` on `baltic-patrol.policy.json` | `data/scenarios/baltic-patrol.policy.json` | COVERED |
| New production Baltic golden world-state hash pinned; `/replay-verify` PASS | `replay-golden-baltic-engage-2026-06-02.txt` — hash unchanged `17144800277401907079` | COVERED |
| `ReplayGoldenSuiteTests` — 6/6 PASS | production pin comment updated; hash unchanged | COVERED |
| `combat-domains-smoke` separate pin unchanged | `CombatDomainsSmokePolicyTests` | COVERED |
| `baltic-patrol-combat-domains` isolated pin unchanged | `BalticCombatDomainsPolicyTests` + regression txt | COVERED |
| Domain validators (air/surface/subsurface/land) active on flag-on production path | `Production_baltic_patrol_flag_on_matches_isolated_combat_domains_hash` | COVERED |
| ZERO touch `DelegationBridge` | empty diff vs HEAD | COVERED |

## QA test-case traceability

| Criterion | Test / Evidence | Status |
|-----------|-----------------|--------|
| AC-1 Producer gate enforced | `sprint-30-baltic-flag-flip-2026-06-18.md` APPROVED | COVERED |
| AC-2 Production golden pins after flip | `ReplayGoldenBalticEngageTests`; hash `17144800277401907079` | COVERED |
| AC-3 Isolated pin isolation | smoke + isolated combat hashes unchanged | COVERED |

## Files changed

- `data/scenarios/baltic-patrol.policy.json` — `engage.combatDomainsEnabled=true`
- `data/scenarios/scenario-policy-ids.md` — production flip documented
- `tests/regression/replay-golden-baltic-engage-2026-06-02.txt` — comment (hash unchanged)
- `tests/regression/replay-golden-baltic-combat-domains-2026-06-18.txt` — comment update
- `src/ProjectAegis.Delegation.UnityAdapter.Tests/Baltic/BalticCombatDomainsPolicyTests.cs` — production flag-on tests
- `src/ProjectAegis.Sim.Tests/Scenario/ScenarioPolicyJsonLoaderTests.cs` — loader assertion
- `production/agentic/sprint-30-baltic-flag-flip-2026-06-18.md` — producer decision record

## Pinned world hash (unchanged)

`WORLD_HASH=17144800277401907079` — production flip is allow-path; no abort delta on seed=42 ticks=4.

## Verify (2026-06-18)

| Suite | Result |
|-------|--------|
| Sim `Combat\|Domain\|Damage` | **73/73 PASS** |
| `ReplayGoldenSuiteTests` | **6/6 PASS** |
| `CombatDomainsSmoke\|BalticCombatDomains` | **11/11 PASS** |
| `DelegationBridge.cs` diff | **empty** |

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone
dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj \
  --filter "Combat|Domain|Damage" -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests" -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "CombatDomainsSmoke|BalticCombatDomains" -v minimal
git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs   # empty
```

## Not touched (by design)

- `DelegationBridge.cs`
- `baltic-patrol-combat-domains.policy.json` (isolated pin)
- `combat-domains-smoke.policy.json` (smoke pin)