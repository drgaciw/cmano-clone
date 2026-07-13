---
id: S28-09
status: Complete
Last Updated: 2026-06-18
type: Logic
priority: nice-to-have
graphite_branch: stack/sprint28/combat-phase2
estimate_days: 1
dependencies:
  - S28-05 complete
owner: team-simulation
sprint: 28
req_trace: Req 18 Combat Domains; S27-06 BDA pattern
---

# Story 028-09 — Facility Damage Projection Stub

> **Epic:** sprint-28-combat-domains-phase2  
> **ADR:** ADR-009 (projection-only slice)

## Summary

Order-log/projection-only facility damage stub mirroring S27-06 BDA pattern. Projection tests only; Baltic golden unchanged. No hot-tick world mutation; no full facility combat runtime.

## Acceptance Criteria

- [x] Facility damage projection type (order-log or projection-only)
- [x] Projection tests PASS in isolated fixtures
- [x] `ReplayGoldenSuiteTests` — 6/6 unchanged on default path
- [x] `combatDomainsEnabled=false` on Baltic production fixtures
- [x] No hot-tick world-state damage apply
- [x] ZERO touch `DelegationBridge.cs`

## QA Test Cases

- **AC-1**: Projection-only facility damage
  - Given: flag-on fixture with facility engagement
  - When: projection runs
  - Then: order-log or projection output matches expected stub behavior
  - Edge cases: zero facilities; multiple facility targets

- **AC-2**: Baltic regression
  - Given: default Baltic fixture
  - When: replay golden runs
  - Then: identical hash vs pre-merge
  - Edge cases: projection leak into default tick path

## Verify Commands

```bash
export PATH="/home/username01/.dotnet:$PATH"
dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj \
  --filter "Combat|Domain|Damage|Facility" -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests" -v minimal
```

## GitNexus Symbols

| Symbol | Risk |
|--------|------|
| `OrderLogBdaProjection` | HIGH — extend pattern only |
| `DelegationBridge.cs` | ZERO touch |

## References

- S27-06 pattern: `production/epics/sprint-27-adr009-bounded/story-027-06-order-log-bda.md`
- Kickoff: `production/sprints/sprint-28-corpus-write-combat-v2.md` (S28-09)
- QA plan: `production/qa/qa-plan-sprint-28-2026-09-18.md`

## Completion Notes
**Completed**: 2026-06-18
**Criteria**: 6/6 passing
**Deviations**: None
**Test Evidence**: `OrderLogFacilityDamageProjectionTests` + `FacilityPictureProjection.ProjectWithDamage`
**Code Review**: Skipped (lean mode)