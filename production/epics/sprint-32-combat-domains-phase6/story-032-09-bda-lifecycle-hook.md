---
id: S32-09
status: Complete
Last Updated: 2026-06-19
type: Integration
priority: should-have
graphite_branch: stack/sprint32/bda-lifecycle-hook
estimate_days: 1.5
dependencies:
  - S31-06 BDA hot path complete
owner: team-simulation
sprint: 32
req_trace: TR-combat-dom-003; Req 18 BDA contact lifecycle; S31-06 projection extension
---

# Story 032-09 — BDA Contact Lifecycle Sim Hook

> **Epic:** sprint-32-combat-domains-phase6  
> **ADR:** ADR-009 (bounded BDA), ADR-001 (deterministic sim), ADR-003 (order log)  
> **GDD:** `design/gdd/combat-domains-damage.md` § BDA  
> **QA Classification:** Integration + Logic

## Summary

Promote contact FSM to **`Lost`** when **`damageLevel ≥ 3`** behind **`combatDomainsEnabled`** flag — sim-kernel hook extending S31-06 projection-only BDA path. Isolated fixture; projection + sim-kernel consistent. ReplayGolden 6/6 default unchanged.

## Acceptance Criteria

- [x] Contact FSM promotes to `Lost` when `damageLevel ≥ 3` on flag-on isolated fixture
- [x] Sim-kernel contact state consistent with BDA projection output
- [x] Flag-gated (`combatDomainsEnabled=true`) — no contact mutation on default path
- [x] BDA projection tests PASS (`Combat|Domain|Bda` filters)
- [x] `ReplayGoldenSuiteTests` — 6/6 PASS on default path
- [x] `/replay-verify` PASS on isolated fixture
- [x] No full BDA component model; no mine-laying / component-level damage
- [x] ZERO touch `DelegationBridge.cs`

## QA Test Cases

- **AC-1**: Contact Lost promotion at damageLevel ≥ 3
  - Given: flag-on fixture with sorted Hit outcomes reaching `damageLevel` 3
  - When: BDA lifecycle hook runs post-damage apply
  - Then: contact FSM state `Lost`; projection and sim-kernel agree
  - Edge cases: `damageLevel` 2 (degraded, not Lost); multiple hits same tick; boundary value 3

- **AC-2**: Default path replay unchanged
  - Given: default Baltic fixture (`combatDomainsEnabled=false`)
  - When: ReplayGolden suite runs without BDA lifecycle fixture loaded
  - Then: 6/6 PASS; no contact lifecycle side effects on flag-off path
  - Edge cases: projection leak into default tick path; accidental status mutation on production fixtures

## Verify Commands

```bash
export PATH="/home/username01/.dotnet:$PATH"
dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj \
  --filter "Combat|Domain|Bda|Damage" -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests" -v minimal
/replay-verify
git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
```

## GitNexus Symbols

| Symbol | Risk |
|--------|------|
| `OrderLogBdaProjection` | HIGH — extend S31-06 only |
| `ContactPictureProjection` | HIGH |
| `CatalogDamageHotTickApplier` | READ — S30-08 pattern |
| `PlatformDamageChangeRecord` | MEDIUM — order-log contract |
| `DelegationBridge.cs` | ZERO touch |

## References

- S31-06 prerequisite: `production/epics/sprint-31-combat-domains-phase5/story-031-06-bda-hot-path.md`
- S27-06 pattern: `production/epics/sprint-27-adr009-bounded/story-027-06-order-log-bda.md`
- GDD: `design/gdd/combat-domains-damage.md` § BDA
- Kickoff: `production/sprints/sprint-32-release-train-combat-phase6-platform-phase-f.md` (S32-09)
- Track plan: `production/agentic/sprint-32-plan-sim-2026-06-18.md` *(create at kickoff)*
- QA plan: `production/qa/qa-plan-sprint-32-*.md` *(create before implementation)*