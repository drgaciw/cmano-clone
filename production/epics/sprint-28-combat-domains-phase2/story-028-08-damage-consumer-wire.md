---
id: S28-08
status: Not Started
type: Integration
priority: should-have
graphite_branch: stack/sprint28/combat-phase2
estimate_days: 1.5
dependencies:
  - S28-01 green baseline
owner: team-simulation
sprint: 28
req_trace: Req 18 Combat Domains; S25-13 stub extension
---

# Story 028-08 — Damage Sim Consumer Wire (Beyond Stub)

> **Epic:** sprint-28-combat-domains-phase2  
> **ADR:** ADR-009 (readiness wire), ADR-001 (deterministic sim)

## Summary

Connect Phase B damage catalog to readiness/withdraw evaluation beyond S25 stub. Sim tests PASS; stub path extended — **no hot-tick world-state damage apply**.

## Acceptance Criteria

- [ ] `PhaseBCatalogDamageReadinessStub` (or successor) extended with catalog damage columns
- [ ] Readiness/withdraw evaluation consumes catalog damage data
- [ ] Sim tests PASS (`Combat|Domain|Damage|Readiness` filters)
- [ ] `ReplayGoldenSuiteTests` — 6/6 PASS on default path
- [ ] `/replay-verify` PASS on sim merge
- [ ] No hot-tick world-state damage apply / full BDA component model
- [ ] `combatDomainsEnabled=false` on Baltic production fixtures
- [ ] ZERO touch `DelegationBridge.cs`

## QA Test Cases

- **AC-1**: Damage readiness from catalog
  - Given: Baltic fixture with Phase B damage columns seeded
  - When: readiness evaluator runs
  - Then: withdraw/readiness reflects catalog damage thresholds
  - Edge cases: missing damage row; zero damage; boundary threshold

- **AC-2**: Default path replay unchanged
  - Given: `combatDomainsEnabled=false` on Baltic
  - When: full replay golden suite runs
  - Then: 6/6 PASS; world hash unchanged
  - Edge cases: accidental flag-on on production fixtures

## Verify Commands

```bash
export PATH="/home/username01/.dotnet:$PATH"
dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj \
  --filter "Combat|Domain|Damage|Readiness" -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests" -v minimal
```

## GitNexus Symbols

| Symbol | Risk |
|--------|------|
| `WithdrawReadinessTrialResolver` | HIGH |
| `DelegationBridge.cs` | ZERO touch |

## References

- S25-13 pattern: `production/agentic/stacks/sprint25/S25-13-DONE.md`
- S26-05 pattern: `production/agentic/stacks/sprint26/S26-05-DONE.md`
- Kickoff: `production/sprints/sprint-28-corpus-write-combat-v2.md` (S28-08)
- QA plan: `production/qa/qa-plan-sprint-28-2026-09-18.md` *(create before implementation)*