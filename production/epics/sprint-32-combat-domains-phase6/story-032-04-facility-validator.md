---
id: S32-04
status: Complete
type: Integration
priority: must-have
graphite_branch: stack/sprint32/facility-validator
estimate_days: 1.5
dependencies:
  - S32-01 green baseline
  - S31-05 facility hot-tick complete
owner: team-simulation
sprint: 32
req_trace: TR-combat-dom-001, TR-combat-dom-002; Req 18; ADR-009 facility aspect plug-in
---

# Story 032-04 — Facility Aspect Domain Validator (Bounded)

> **Epic:** sprint-32-combat-domains-phase6  
> **ADR:** ADR-009 (Accepted), ADR-003 (order log), ADR-001 (deterministic sim)  
> **QA Classification:** Integration + Logic

## Summary

Register bounded **`FacilityAspectDomainValidator`** with **`FACILITY_ASPECT_BLOCK`** in `DomainValidatorRegistry` (ADR-009 Phase 6 facility plug-in) — allow/deny with stable abort codes, **flag-gated isolated fixture only**. Facility aspect only; no hot-tick world mutation in this story. `combatDomainsEnabled=false` on production Baltic fixtures.

## Acceptance Criteria

- [x] `FacilityAspectDomainValidator` registered in `DomainValidatorRegistry` with `FACILITY_ASPECT_BLOCK`
- [x] Allow in envelope; deny with documented `FireAbortReason` / order-log abort code
- [x] Validator deny appends order-log abort code in **isolated flag-on facility fixture** only
- [x] `combatDomainsEnabled=false` on production `baltic-patrol.policy.json` — zero abort delta vs S31 closeout
- [x] Sim tests PASS (`Combat|Domain|Facility` filters)
- [x] `ReplayGoldenSuiteTests` — 6/6 PASS on default path
- [x] `/replay-verify` PASS on sim merge
- [x] ZERO touch `DelegationBridge.cs`

## QA Test Cases

- **AC-1**: Facility validator allow/deny
  - Given: isolated flag-on fixture with facility-aspect engagement envelope
  - When: validator runs on in/out-of-envelope cases
  - Then: allow path proceeds; deny path emits stable abort code in order log
  - Edge cases: boundary envelope values; empty engagement set; unknown facility target

- **AC-2**: Registry stable iteration order
  - Given: `DomainValidatorRegistry` with air/surface/subsurface/land/mine/facility validators
  - When: registry enumerates validators
  - Then: stable ordinal order by domain id (deterministic across replays)
  - Edge cases: duplicate domain registration rejected; missing facility validator on flag-off path is no-op

## Verify Commands

```bash
export PATH="/home/username01/.dotnet:$PATH"
dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj \
  --filter "Combat|Domain|Facility" -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests" -v minimal
/replay-verify
git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
```

## GitNexus Symbols

| Symbol | Risk |
|--------|------|
| `DomainValidatorRegistry` | HIGH |
| `FacilityAspectDomainValidator` | HIGH |
| `MineAspectDomainValidator` | READ — pattern reference |
| `LandAspectDomainValidator` | READ — pattern reference |
| `DelegationBridge.cs` | ZERO touch |

## References

- ADR-009: `docs/architecture/adr-009-combat-domain-validators.md`
- GDD: `design/gdd/combat-domains-damage.md` (TR-combat-dom-001)
- S31-04 pattern: `production/epics/sprint-31-combat-domains-phase5/story-031-04-mine-validator.md`
- S31-05 pattern: `production/epics/sprint-31-combat-domains-phase5/story-031-05-facility-hot-tick.md`
- Kickoff: `production/sprints/sprint-32-release-train-combat-phase6-platform-phase-f.md` (S32-04)
- Track plan: `production/agentic/sprint-32-plan-sim-2026-06-18.md` *(create at kickoff)*
- QA plan: `production/qa/qa-plan-sprint-32-*.md` *(create before implementation)*