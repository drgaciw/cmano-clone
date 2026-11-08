---
id: S31-04
status: Complete
type: Integration
priority: must-have
graphite_branch: stack/sprint31/mine-validator
estimate_days: 1.5
dependencies: []
owner: team-simulation
sprint: 31
req_trace: TR-combat-dom-001, TR-combat-dom-002; Req 18; ADR-009 mine aspect plug-in
---

# Story 031-04 — Mine Aspect Domain Validator (Bounded)

> **Epic:** sprint-31-combat-domains-phase5  
> **ADR:** ADR-009 (Accepted), ADR-003 (order log), ADR-001 (deterministic sim)  
> **QA Classification:** Integration + Logic

## Summary

Register bounded **`MineAspectDomainValidator`** with **`MINE_ASPECT_BLOCK`** in `DomainValidatorRegistry` (ADR-009 Phase 5 mine plug-in) — allow/deny with stable abort codes, **flag-gated isolated fixture only**. Mine aspect only; no hot-tick world mutation in this story. `combatDomainsEnabled=false` on production Baltic fixtures.

## Acceptance Criteria

- [x] `MineAspectDomainValidator` registered in `DomainValidatorRegistry` with `MINE_ASPECT_BLOCK`
- [x] Allow in envelope; deny with documented `FireAbortReason` / order-log abort code
- [x] Validator deny appends order-log abort code in **isolated flag-on mine fixture** only
- [x] `combatDomainsEnabled=false` on production `baltic-patrol.policy.json` — zero abort delta vs S30 closeout
- [x] Sim tests PASS (`Combat|Domain|Mine` filters)
- [x] `ReplayGoldenSuiteTests` — 6/6 PASS on default path
- [x] `/replay-verify` PASS on sim merge
- [x] ZERO touch `DelegationBridge.cs`

## QA Test Cases

- **AC-1**: Mine validator allow/deny
  - Given: isolated flag-on fixture with mine-aspect engagement envelope
  - When: validator runs on in/out-of-envelope cases
  - Then: allow path proceeds; deny path emits stable abort code in order log
  - Edge cases: boundary envelope values; empty engagement set; unknown mine target

- **AC-2**: Registry stable iteration order
  - Given: `DomainValidatorRegistry` with air/surface/subsurface/land/mine validators
  - When: registry enumerates validators
  - Then: stable ordinal order by domain id (deterministic across replays)
  - Edge cases: duplicate domain registration rejected; missing mine validator on flag-off path is no-op

## Verify Commands

```bash
export PATH="/home/username01/.dotnet:$PATH"
dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj \
  --filter "Combat|Domain|Mine" -v minimal
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
| `MineAspectDomainValidator` | HIGH |
| `LandAspectDomainValidator` | READ — pattern reference |
| `SurfaceAspectDomainValidator` | READ — pattern reference |
| `DelegationBridge.cs` | ZERO touch |

## References

- ADR-009: `docs/architecture/adr-009-combat-domain-validators.md`
- GDD: `design/gdd/combat-domains-damage.md` (TR-combat-dom-001)
- S30-05 pattern: `production/epics/sprint-30-combat-domains-phase4/story-030-05-land-validator.md`
- Kickoff: `production/sprints/sprint-31-corpus-combat-polish.md` (S31-04)
- Track plan: `production/agentic/sprint-31-plan-sim-2026-06-18.md` *(create at kickoff)*
- QA plan: `production/qa/qa-plan-sprint-31-2026-10-30.md`