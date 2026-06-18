---
id: S28-05
status: Complete
type: Integration
priority: should-have
graphite_branch: stack/sprint28/combat-phase2
estimate_days: 1.5
dependencies:
  - S28-01 green baseline
owner: team-simulation
sprint: 28
req_trace: TR-combat-dom-001, TR-combat-dom-002; Req 18
---

# Story 028-05 — Surface/Subsurface Domain Validators (Bounded)

> **Epic:** sprint-28-combat-domains-phase2  
> **ADR:** ADR-009 (Accepted), ADR-003 (order log)

## Summary

Extend ADR-009 beyond air aspect with **surface** and **subsurface** domain validators — flag-gated fixtures only. `combatDomainsEnabled=false` on Baltic production fixtures. No hot-tick world mutation.

## Acceptance Criteria

- [x] `SurfaceAspectDomainValidator` (or equivalent): allow in envelope, deny with documented abort code
- [x] `SubsurfaceAspectDomainValidator` (or equivalent): allow/deny with documented abort code
- [x] Validator deny appends order-log abort code in **isolated flag-on fixtures** only
- [x] `combatDomainsEnabled=false` on Baltic — zero abort delta
- [x] `ReplayGoldenSuiteTests` — 6/6 PASS on default path
- [x] `/replay-verify` PASS on sim merge
- [x] ZERO touch `DelegationBridge.cs`
- [x] No mine/land/facility combat at full runtime (validators only)

## QA Test Cases

- **AC-1**: Surface validator allow/deny
  - Given: flag-on fixture with surface engagement envelope
  - When: validator runs on in/out-of-envelope cases
  - Then: allow path proceeds; deny path emits abort code
  - Edge cases: boundary envelope values; empty engagement set

- **AC-2**: Baltic flag-off regression
  - Given: default Baltic fixture (`combatDomainsEnabled=false`)
  - When: engage path runs
  - Then: identical replay golden vs pre-merge
  - Edge cases: accidental flag flip on production fixtures

## Verify Commands

```bash
export PATH="/home/username01/.dotnet:$PATH"
dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj \
  --filter "Combat|Domain|Damage" -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests" -v minimal
```

## GitNexus Symbols

| Symbol | Risk |
|--------|------|
| `DomainValidatorRegistry` | HIGH |
| `DelegationBridge.cs` | ZERO touch |

## References

- ADR-009: `docs/architecture/adr-009-combat-domain-validators.md`
- S27-05 pattern: `production/epics/sprint-27-adr009-bounded/story-027-05-adr009-validators.md`
- Kickoff: `production/sprints/sprint-28-corpus-write-combat-v2.md` (S28-05)
- QA plan: `production/qa/qa-plan-sprint-28-2026-09-18.md` *(create before implementation)*