# S29-09 story-done evidence — Damage Hot-Tick Apply (Bounded)

**Story:** `production/epics/sprint-29-combat-domains-phase3/story-029-09-hot-tick-damage.md`  
**Status:** Complete  
**Completed:** 2026-06-18

## Deliverables

- `PlatformHpLedger` — deterministic HP% ledger seeded from `catalogWithdraw` targets
- `CatalogDamageHotTickApplier` — bounded ambient tick drain + sorted engagement outcome apply via `ICatalogReader` snapshot (no hot-path SQLite)
- `CatalogDamageHotTickTracker` — delegation tick tracker; refreshes `CatalogWithdrawTrials` each tick
- `PlatformDamageChangeRecord` + `OrderLogEntryKind.PlatformDamageChange` — order-log HP transitions
- `SimulationSession.ApplyCatalogDamageHotTick` — wired post-engage; gated on `combatDomainsEnabled` + `catalogWithdraw`
- Test fixture: `data/scenarios/baltic-patrol-combat-domains-hot-tick-damage.policy.json` (isolated; not in ReplayGolden 6/6)
- Tests: `CatalogDamageHotTickApplierTests`, extended `PhaseBDamageCatalogConsumerTests`, `DeterministicDamageApplyBatchTests`, `CatalogDamageHotTickEngageTests`
- **ZERO** touch `DelegationBridge.cs`

## Verify

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone

dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj \
  --filter "Combat|Domain|Damage|Readiness" -v minimal
# Passed: 74/74

dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests" -v minimal
# Passed: 6/6 — default Baltic fixtures unchanged (combatDomainsEnabled=false; no catalogWithdraw on golden policies)

dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj \
  --filter "CatalogDamageHotTick" -v minimal
# Passed: 2/2

git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
# (empty)

rg -l "SQLite|SqliteConnection" src/ProjectAegis.Sim/ --glob "*.cs" || true
# comment-only reference in CatalogDamageHotTickApplier.cs (no runtime SQLite)
```

## Acceptance criteria traceability

| AC | Evidence | Status |
|----|----------|--------|
| Tick-level catalog damage apply beyond S28-08 withdraw gate | `CatalogDamageHotTickApplier` + `SimulationSession.ApplyCatalogDamageHotTick` | **PASS** |
| Damage from gate-approved catalog snapshot (no hot-path SQLite) | `ICatalogReader.TryGetPlatformDamage`; zero Sim SQLite reads | **PASS** |
| Sim tests PASS (`Combat\|Domain\|Damage\|Readiness`) | **74/74** PASS | **PASS** |
| `/replay-verify` on merge | ReplayGolden 6/6; flag-off default path unchanged | **PASS** |
| ReplayGoldenSuiteTests 6/6 on default path | 6/6 PASS | **PASS** |
| No full BDA component model | Ledger HP% + withdraw refresh only; no mine/land/facility runtime | **PASS** |
| ZERO touch `DelegationBridge.cs` | Empty diff vs HEAD | **PASS** |

## Per-project counts (story filters)

| Project | Filter | Passed |
|---------|--------|--------|
| ProjectAegis.Sim.Tests | Combat\|Domain\|Damage\|Readiness | 74 |
| ProjectAegis.Delegation.UnityAdapter.Tests | ReplayGoldenSuiteTests | 6 |
| ProjectAegis.Delegation.Tests | CatalogDamageHotTick | 2 |

## Verdict

**COMPLETE** — Phase B catalog damage extended to bounded hot-tick HP ledger apply with deterministic ordering, live withdraw trial refresh, and order-log `PlatformDamageChange` rows; replay 6/6 on default path; no Baltic golden drift.