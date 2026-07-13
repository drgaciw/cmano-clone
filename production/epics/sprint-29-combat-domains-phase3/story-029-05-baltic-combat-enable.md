---
id: S29-05
status: Complete
type: Integration
priority: should-have
graphite_branch: stack/sprint29/combat-baltic-enable
estimate_days: 1.5
dependencies:
  - S29-01 green baseline
owner: team-simulation
sprint: 29
req_trace: TR-combat-dom-001, TR-combat-dom-002; Req 18; ADR-009 Baltic enable
---

# Story 029-05 — Combat Domains Baltic Enablement

> **Epic:** sprint-29-combat-domains-phase3  
> **ADR:** ADR-009 (Accepted), ADR-003 (order log), ADR-001 (deterministic sim)

## Summary

Flip **`combatDomainsEnabled=true`** on isolated Baltic golden fixture; pin new replay hash; keep `combat-domains-smoke` on separate pin. **`combatDomainsEnabled=false` on Baltic until this story merges** — then document production Baltic fixture policy.

## Acceptance Criteria

- [x] `combatDomainsEnabled=true` on isolated Baltic golden fixture (not smoke pin)
- [x] New Baltic golden world-state hash pinned; `/replay-verify` PASS
- [x] `ReplayGoldenSuiteTests` — 6/6 PASS
- [x] `combat-domains-smoke.policy.json` remains on separate pin (unchanged hash)
- [x] Production Baltic fixture policy documented (when flag flips, hash evidence)
- [x] Domain validators (air/surface/subsurface) active on flag-on Baltic path
- [x] ZERO touch `DelegationBridge.cs`

## QA Test Cases

- **AC-1**: Baltic flag-on golden pins
  - Given: Baltic fixture with `combatDomainsEnabled=true` on isolated golden pin
  - When: `/replay-verify` runs twice + against stored golden
  - Then: identical world hash; new hash recorded in regression artifact
  - Edge cases: validator deny abort codes appear in order log; allow path unchanged hash on repeat

- **AC-2**: Smoke fixture isolation
  - Given: `combat-domains-smoke.policy.json` on separate pin
  - When: ReplayGolden 6/6 suite runs
  - Then: smoke hash unchanged; Baltic golden distinct from smoke pin
  - Edge cases: accidental conflation of smoke + Baltic pins

- **AC-3**: Pre-merge baseline guard
  - Given: trunk before S29-05 merge
  - When: default Baltic production fixtures inspected
  - Then: `combatDomainsEnabled=false` — zero abort delta vs S28 closeout
  - Edge cases: accidental early flag flip on production fixtures

## Verify Commands

```bash
export PATH="/home/username01/.dotnet:$PATH"
dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj \
  --filter "Combat|Domain|Damage" -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests" -v minimal
# Mandatory sim merge gate
/replay-verify
```

## GitNexus Symbols

| Symbol | Risk |
|--------|------|
| `DomainValidatorRegistry` | HIGH |
| `AirAspectDomainValidator` | HIGH |
| `SurfaceAspectDomainValidator` | HIGH |
| `SubsurfaceAspectDomainValidator` | HIGH |
| `DelegationBridge.cs` | ZERO touch |

## References

- ADR-009: `docs/architecture/adr-009-combat-domain-validators.md`
- S28-05 pattern: `production/epics/sprint-28-combat-domains-phase2/story-028-05-surface-validators.md`
- Smoke fixture: `data/scenarios/combat-domains-smoke.policy.json`
- Kickoff: `production/sprints/sprint-29-operationalize-data-fight-loop.md` (S29-05)
- Track plan: `production/agentic/sprint-29-plan-data-2026-06-18.md` *(create at kickoff)*
- QA plan: `production/qa/qa-plan-sprint-29-2026-10-02.md` *(create before implementation)*