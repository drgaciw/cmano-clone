---
id: S30-05
status: Complete
type: Integration
priority: should-have
graphite_branch: stack/sprint30/combat-land-validator
estimate_days: 1.5
dependencies:
  - S30-01 green baseline
owner: team-simulation
sprint: 30
req_trace: TR-combat-dom-001, TR-combat-dom-002; Req 18; ADR-009 land aspect plug-in
---

# Story 030-05 — Land Aspect Domain Validator (Bounded)

> **Epic:** sprint-30-combat-domains-phase4  
> **ADR:** ADR-009 (Accepted), ADR-003 (order log), ADR-001 (deterministic sim)  
> **QA Classification:** Integration + Logic

## Summary

Register bounded **`LandAspectDomainValidator`** (ADR-009 Phase 4 land plug-in) in `DomainValidatorRegistry` — allow/deny with stable abort codes, **flag-gated isolated fixture only**. Land aspect only; mine/facility stubs deferred. `combatDomainsEnabled=false` on production Baltic fixtures until S30-09. No hot-tick world mutation in this story.

## Acceptance Criteria

- [x] `LandAspectDomainValidator` (or `LandDomainValidator` equivalent) registered in `DomainValidatorRegistry`
- [x] Allow in envelope; deny with documented `FireAbortReason` / order-log abort code
- [x] Validator deny appends order-log abort code in **isolated flag-on land fixture** only
- [x] `combatDomainsEnabled=false` on production `baltic-patrol.policy.json` — zero abort delta vs S29 closeout
- [x] Sim tests PASS (`Combat|Domain` filters)
- [x] `ReplayGoldenSuiteTests` — 6/6 PASS on default path
- [x] `/replay-verify` PASS on sim merge
- [x] No mine/facility full runtime (land validator only; mine stub deferred)
- [x] ZERO touch `DelegationBridge.cs`

## QA Test Cases

- **AC-1**: Land validator allow/deny
  - Given: isolated flag-on fixture with land-aspect engagement envelope
  - When: validator runs on in/out-of-envelope cases
  - Then: allow path proceeds; deny path emits stable abort code in order log
  - Edge cases: boundary envelope values; empty engagement set; unknown land target

- **AC-2**: Registry stable iteration order
  - Given: `DomainValidatorRegistry` with air/surface/subsurface/land validators
  - When: registry enumerates validators
  - Then: stable ordinal order by domain id (deterministic across replays)
  - Edge cases: duplicate domain registration rejected; missing land validator on flag-off path is no-op

- **AC-3**: Baltic flag-off regression
  - Given: default Baltic production fixtures (`combatDomainsEnabled=false`)
  - When: engage path runs + ReplayGolden suite
  - Then: identical replay golden vs pre-merge; 6/6 PASS
  - Edge cases: accidental flag flip on production fixtures; isolated land pin does not pollute ReplayGolden catalog

## Verify Commands

```bash
export PATH="/home/username01/.dotnet:$PATH"
dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj \
  --filter "Combat|Domain" -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests" -v minimal
# Mandatory sim merge gate
/replay-verify
git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
```

## GitNexus Symbols

| Symbol | Risk |
|--------|------|
| `DomainValidatorRegistry` | HIGH |
| `LandAspectDomainValidator` | HIGH |
| `AirAspectDomainValidator` | READ — pattern reference |
| `SurfaceAspectDomainValidator` | READ — pattern reference |
| `SubsurfaceAspectDomainValidator` | READ — pattern reference |
| `DelegationBridge.cs` | ZERO touch |

## References

- ADR-009: `docs/architecture/adr-009-combat-domain-validators.md`
- GDD: `design/gdd/combat-domains-damage.md` (TR-combat-dom-001)
- S28-05 pattern: `production/epics/sprint-28-combat-domains-phase2/story-028-05-surface-validators.md`
- S29-05 Baltic isolate: `production/epics/sprint-29-combat-domains-phase3/story-029-05-baltic-combat-enable.md`
- Kickoff: `production/sprints/sprint-30-tl-bind-corpus-scale.md` (S30-05)
- Track plan: `production/agentic/sprint-30-plan-sim-2026-06-18.md` *(create at kickoff)*
- QA plan: `production/qa/qa-plan-sprint-30-2026-10-16.md`