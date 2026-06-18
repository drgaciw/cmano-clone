# S26-09 story-done evidence — ADR-009 acceptance + validator stubs

**Story:** `production/sprints/sprint-26-cmo-phase2-presentation-closeout.md` §S26-09  
**Status:** Complete  
**Completed:** 2026-06-18

## Deliverables

- ADR-009 status → **Accepted** (stub validators; `combatDomainsEnabled` default false)
- `IDomainValidator`, `DomainValidateResult`, `NoOpDomainValidators`, `DomainValidatorRegistry`
- `MvpEngagementResolver` — optional registry hook after policy when `combatDomainsEnabled=true`
- `ScenarioEngageDefaults.MvpFallback.CombatDomainsEnabled` default **false**
- `ScenarioPolicyJsonDto` + loader — `combatDomainsEnabled` JSON field
- `SimulationSession` — passes engage-default flag into resolver wiring
- Tests: `DomainValidatorRegistryTests` (5 tests)
- **ZERO touch** `DelegationBridge.cs`

## Verify

```bash
export PATH="/home/username01/.dotnet:$PATH"
dotnet test src/ProjectAegis.Sim.Tests --filter "DomainValidator" -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests --filter "ReplayGoldenSuiteTests" -v minimal
# DomainValidator: 5/5 PASS; ReplayGolden: 6/6 PASS
```

## Acceptance criteria

| AC | Evidence | Verdict |
|----|----------|---------|
| ADR-009 Accepted | `docs/architecture/adr-009-combat-domain-validators.md` | **PASS** |
| No-op validator stubs | `NoOpDomainValidators` air/surface allow-all | **PASS** |
| `combatDomainsEnabled=false` default | `ScenarioEngageDefaults.MvpFallback` + policy JSON | **PASS** |
| Golden replay abort set empty when enabled | ReplayGolden 6/6 unchanged (flag off in Baltic fixtures) | **PASS** |
| No runtime BDA | Registry runs pre-launch only; no damage apply path | **PASS** |
| Sim tests PASS | `DomainValidator` filter **5/5** | **PASS** |
| ReplayGolden 6/6 | Baltic hashes unchanged | **PASS** |
| ZERO touch `DelegationBridge.cs` | Empty diff vs `HEAD` | **PASS** |

## Verdict

**COMPLETE** — ADR-009 accepted with stub-only combat domain validators; replay determinism preserved.