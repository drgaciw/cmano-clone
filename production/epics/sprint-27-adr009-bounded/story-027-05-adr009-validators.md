---
id: S27-05
status: Ready
type: Integration
priority: should-have
graphite_branch: stack/sprint27/adr009-validators-bda
estimate_days: 2
dependencies:
  - S27-01 green baseline
owner: team-simulation
sprint: 27
req_trace: TR-combat-dom-001, TR-combat-dom-002; Req 18
---

# Story 027-05 — ADR-009 Bounded Validators

> **Epic:** sprint-27-adr009-bounded  
> **ADR:** ADR-009 (Accepted), ADR-003 (order log)

## Summary

Implement deterministic damage outcome batch sort (`OrderBy(EngagementId).ThenBy(SequenceId)`), `AirAspectDomainValidator` stub, and validator deny → order log when `combatDomainsEnabled=true` (isolated fixtures only).

## Acceptance Criteria

- [ ] `DeterministicDamageApplyBatch` (or equivalent) with ≥4 sort-order unit tests
- [ ] `AirAspectDomainValidator`: allow in envelope, deny with `AirAspectBlock`
- [ ] Validator deny appends order-log abort code in flag-on fixture
- [ ] `combatDomainsEnabled=false` on Baltic — zero abort delta
- [ ] `ReplayGoldenSuiteTests` — 6/6 PASS on default path
- [ ] ZERO touch `DelegationBridge.cs`

## QA Test Cases

- **AC-1**: Stable apply order
  - Given: shuffled engagement outcomes
  - When: batch sort runs
  - Then: output ordered by EngagementId then SequenceId
  - Edge cases: empty batch; single engagement; tie-break on SequenceId

- **AC-2**: Baltic flag-off regression
  - Given: default Baltic fixture
  - When: engage path runs
  - Then: identical replay golden vs pre-merge

## Verify Commands

```bash
dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj \
  --filter "Combat|Domain|Damage" -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests" -v minimal
```

## References

- ADR-009: `docs/architecture/adr-009-combat-domain-validators.md`
- S26-09: `production/agentic/stacks/sprint26/S26-09-DONE.md`
- QA plan: `production/qa/qa-plan-sprint-27-2026-06-18.md`