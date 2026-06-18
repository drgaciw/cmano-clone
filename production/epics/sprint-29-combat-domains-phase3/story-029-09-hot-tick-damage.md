---
id: S29-09
status: Not Started
type: Integration
priority: nice-to-have
graphite_branch: stack/sprint29/hot-tick-damage
estimate_days: 1.5
dependencies:
  - S29-05 Baltic combat enablement
owner: team-simulation
sprint: 29
req_trace: Req 18 Combat Domains; S28-08 readiness wire extension
---

# Story 029-09 — Damage Hot-Tick Apply (Bounded)

> **Epic:** sprint-29-combat-domains-phase3  
> **ADR:** ADR-009 (bounded damage), ADR-001 (deterministic sim)

## Summary

Extend S28-08 readiness wire to **tick-level catalog damage apply** — bounded, no full BDA component model. Sim tests PASS; `/replay-verify` mandatory. No hot-path SQLite.

## Acceptance Criteria

- [ ] Tick-level catalog damage apply wired beyond S28-08 readiness/withdraw path
- [ ] Damage sourced from gate-approved catalog snapshot (no hot-path SQLite)
- [ ] Sim tests PASS (`Combat|Domain|Damage|Readiness` filters)
- [ ] `/replay-verify` PASS on sim merge
- [ ] `ReplayGoldenSuiteTests` — 6/6 PASS on default path
- [ ] No full BDA component model / mine-land-facility full runtime
- [ ] ZERO touch `DelegationBridge.cs`

## QA Test Cases

- **AC-1**: Tick-level damage from catalog
  - Given: Baltic flag-on fixture with Phase B damage columns (post S29-05)
  - When: bounded hot-tick damage apply runs across N ticks
  - Then: world-state reflects catalog damage thresholds; order log records changes
  - Edge cases: missing damage row; zero damage; boundary threshold; deterministic repeat

- **AC-2**: No hot-path SQLite
  - Given: sim tick pipeline under damage apply
  - When: grep/audit for SQLite reads in hot path
  - Then: catalog data pre-loaded or cached from approved snapshot only
  - Edge cases: per-tick DB query anti-pattern

- **AC-3**: Default path replay unchanged
  - Given: `combatDomainsEnabled=false` fixtures (pre-S29-05 policy) or flag-off smoke pin
  - When: ReplayGolden suite runs without hot-tick story changes on default pins
  - Then: 6/6 PASS unless S29-05 intentionally updated Baltic golden
  - Edge cases: accidental damage apply on flag-off fixtures

## Verify Commands

```bash
export PATH="/home/username01/.dotnet:$PATH"
dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj \
  --filter "Combat|Domain|Damage|Readiness" -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests" -v minimal
/replay-verify
```

## GitNexus Symbols

| Symbol | Risk |
|--------|------|
| `CatalogDamageWithdrawEngageGate` | HIGH |
| `WithdrawReadinessTrialResolver` | HIGH |
| `DelegationBridge.cs` | ZERO touch |

## References

- S28-08 pattern: `production/epics/sprint-28-combat-domains-phase2/story-028-08-damage-consumer-wire.md`
- S28-08 evidence: `production/agentic/stacks/sprint28/S28-08-DONE.md`
- Kickoff: `production/sprints/sprint-29-operationalize-data-fight-loop.md` (S29-09)
- Track plan: `production/agentic/sprint-29-plan-data-2026-06-18.md` *(create at kickoff)*
- QA plan: `production/qa/qa-plan-sprint-29-2026-10-02.md` *(create before implementation)*