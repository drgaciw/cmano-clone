---
id: S30-08
status: Complete
type: Integration
priority: should-have
graphite_branch: stack/sprint30/hot-tick-hits
estimate_days: 1.5
dependencies:
  - S30-05 land aspect domain validator
owner: team-simulation
sprint: 30
req_trace: Req 18 Combat Domains; TR-combat-dom-002; S29-09 ambient drain extension
---

# Story 030-08 — Hot-Tick Hit → HP Ledger Extensions

> **Epic:** sprint-30-combat-domains-phase4  
> **ADR:** ADR-009 (bounded damage), ADR-001 (deterministic sim), ADR-003 (order log)  
> **GDD:** `design/gdd/combat-domains-damage.md` (`damageLevel` 0–3)  
> **QA Classification:** Integration + Logic

## Summary

Extend S29-09 ambient catalog drain to apply **engagement `Hit` outcomes** → **`PlatformHpLedger`** via **`DeterministicDamageApplyBatch`**, computing bounded **`damageLevel` 0–3** per GDD formula. Isolated flag-on fixture; `/replay-verify` mandatory. No hot-path SQLite; no full BDA component model.

## Acceptance Criteria

- [x] Engagement `Hit` outcomes apply HP delta through `DeterministicDamageApplyBatch.Sort` → `PlatformHpLedger`
- [x] `damageLevel = clamp(0, 3, floor(hitSeverity * platform.resilience))` wired for `Hit` path (bounded P1 slice)
- [x] Damage sourced from gate-approved catalog snapshot (no hot-path SQLite)
- [x] `PlatformDamageChange` order-log rows emitted on HP transitions
- [x] Sim tests PASS (`Combat|Domain|Damage` filters)
- [x] `/replay-verify` PASS on isolated hot-tick hit fixture
- [x] `ReplayGoldenSuiteTests` — 6/6 PASS on default path (production pins unchanged)
- [x] No full BDA component model / mine-land-facility full runtime beyond ledger
- [x] ZERO touch `DelegationBridge.cs`

## QA Test Cases

- **AC-1**: Hit outcome → HP ledger apply
  - Given: isolated flag-on fixture with `combatDomainsEnabled=true` and engagement `Hit` outcomes
  - When: `CatalogDamageHotTickApplier` (or successor) runs post-engage across N ticks
  - Then: `PlatformHpLedger` reflects sorted apply order; `damageLevel` bounded 0–3
  - Edge cases: multiple hits same tick; `Kill` vs `Hit` precedence; zero resilience; missing damage row

- **AC-2**: Deterministic apply order
  - Given: shuffled engagement damage outcomes same tick
  - When: `DeterministicDamageApplyBatch.Sort` runs twice
  - Then: identical apply sequence by `(engagementId, sequenceId)`; identical world hash
  - Edge cases: tie on engagementId; empty outcome set

- **AC-3**: Default path replay unchanged
  - Given: `combatDomainsEnabled=false` production fixtures (ReplayGolden 6/6 catalog)
  - When: ReplayGolden suite runs without hot-tick hit fixture loaded
  - Then: 6/6 PASS; no HP ledger mutation on flag-off path
  - Edge cases: accidental hit apply on flag-off fixtures; isolated pin not in golden catalog

## Verify Commands

```bash
export PATH="/home/username01/.dotnet:$PATH"
dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj \
  --filter "Combat|Domain|Damage" -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests" -v minimal
/replay-verify
git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
```

## GitNexus Symbols

| Symbol | Risk |
|--------|------|
| `CatalogDamageHotTickApplier` | HIGH |
| `DeterministicDamageApplyBatch` | HIGH |
| `PlatformHpLedger` | HIGH |
| `PlatformDamageChangeRecord` | MEDIUM — order-log contract |
| `CatalogDamageHotTickTracker` | MEDIUM |
| `DelegationBridge.cs` | ZERO touch |

## References

- GDD: `design/gdd/combat-domains-damage.md` (damage level formula §4)
- S29-09 pattern: `production/epics/sprint-29-combat-domains-phase3/story-029-09-hot-tick-damage.md`
- S29-09 evidence: `production/agentic/stacks/sprint29/S29-09-DONE.md`
- S28-08 pattern: `production/epics/sprint-28-combat-domains-phase2/story-028-08-damage-consumer-wire.md`
- Isolated fixture pattern: `data/scenarios/baltic-patrol-combat-domains-hot-tick-damage.policy.json`
- Kickoff: `production/sprints/sprint-30-tl-bind-corpus-scale.md` (S30-08)
- Track plan: `production/agentic/sprint-30-plan-sim-2026-06-18.md` *(create at kickoff)*
- QA plan: `production/qa/qa-plan-sprint-30-2026-10-16.md`