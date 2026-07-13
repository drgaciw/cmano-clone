---
id: S31-06
status: Complete
Last Updated: 2026-06-18
type: Integration
priority: should-have
graphite_branch: stack/sprint31/bda-hot-path
estimate_days: 1.5
dependencies: []
owner: team-simulation
sprint: 31
req_trace: TR-combat-dom-003; Req 18 BDA; S27-06 order-log BDA extension
---

# Story 031-06 — BDA Hot Path (Bounded)

> **Epic:** sprint-31-combat-domains-phase5  
> **ADR:** ADR-009 (bounded BDA), ADR-001 (deterministic sim), ADR-003 (order log)  
> **GDD:** `design/gdd/combat-domains-damage.md` § BDA  
> **QA Classification:** Integration + Logic

## Summary

Extend S27-06 **order-log BDA slice** and S30-08 **hot-tick HP ledger** to map **Hit / `damageLevel`** → **contact status** in BDA projections. Bounded projection-only path — **not** full BDA component model. Isolated flag-on fixture; ReplayGolden 6/6 default unchanged.

## Acceptance Criteria

- [x] Hit outcomes with `damageLevel` 0–3 drive contact status transitions in BDA projections
- [x] `ContactPictureProjection` / BDA projection reflects damage-level contact status (not kill-only)
- [x] Projection-only — no sim-kernel contact mutation in default config
- [x] Flag-gated test fixture only (`combatDomainsEnabled=true`)
- [x] BDA projection tests PASS (`Combat|Domain|Bda` filters)
- [x] `ReplayGoldenSuiteTests` — 6/6 PASS on default path
- [x] No full BDA component model / mine-laying / component-level damage
- [x] ZERO touch `DelegationBridge.cs`

## Completion Notes
**Completed**: 2026-06-18
**Criteria**: 8/8 passing
**Deviations**: None
**Test Evidence**: `OrderLogBdaProjectionTests`, `BdaContactDamageStatesTests`, `ReplayGoldenSuiteTests` 6/6
**Code Review**: Skipped (lean mode)

## QA Test Cases

- **AC-1**: Damage-level contact status projection
  - Given: flag-on fixture with sorted Hit outcomes and `damageLevel` 1–3
  - When: BDA hot-path projection runs
  - Then: contact picture reflects degraded status per damage level; kill (`damageLevel` 3 or Kill outcome) drops contact
  - Edge cases: miss outcome unchanged; multiple hits stable order; boundary damageLevel values

- **AC-2**: Default path replay unchanged
  - Given: default Baltic fixture (`combatDomainsEnabled=false`)
  - When: ReplayGolden suite runs without BDA hot-path fixture loaded
  - Then: 6/6 PASS; no BDA hot-path side effects on flag-off path
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
| `OrderLogBdaProjection` | HIGH — extend pattern only |
| `ContactPictureProjection` | HIGH |
| `CatalogDamageHotTickApplier` | READ — S30-08 pattern |
| `PlatformDamageChangeRecord` | MEDIUM — order-log contract |
| `DelegationBridge.cs` | ZERO touch |

## References

- S27-06 pattern: `production/epics/sprint-27-adr009-bounded/story-027-06-order-log-bda.md`
- S30-08 pattern: `production/epics/sprint-30-combat-domains-phase4/story-030-08-hot-tick-hits.md`
- GDD: `design/gdd/combat-domains-damage.md` § BDA
- Kickoff: `production/sprints/sprint-31-corpus-combat-polish.md` (S31-06)
- Track plan: `production/agentic/sprint-31-plan-sim-2026-06-18.md` *(create at kickoff)*
- QA plan: `production/qa/qa-plan-sprint-31-2026-10-30.md`