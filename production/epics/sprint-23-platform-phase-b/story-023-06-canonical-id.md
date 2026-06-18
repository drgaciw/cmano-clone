---
id: S23-06
status: Complete
type: Logic
priority: nice-to-have
graphite_branch: stack/sprint23/canonical-determinism
estimate_days: 1
dependencies:
  - S23-04 (or parallel if staging types stable)
owner: c-sharp-engineer / team-data
sprint: 23
req_trace: DBI-1.1, DBI-1.2, DBI-7.3, PLE-1.3
last_updated: 2026-06-17
---

# Story 023-06 — CanonicalId Determinism on Catalog* Types

> **Epic:** sprint-23-platform-phase-b  
> **Sprint:** 23 — Platform Phase B I/O + Doctrine Polish  
> **Gap:** Sprint 22 QA noted determinism gap on new `Catalog*` types

## Summary

Stable `OrderBy` composite keys per DBI-7.3 on mount/loadout/magazine/comms/platform/weapon `Catalog*` types. Determinism test PASS; golden hash pinned for batch proposal + sheet export ordering.

## Acceptance Criteria

- [x] Determinism test PASS
- [x] Golden hash pinned for stable ordering across runs
- [x] Stable `OrderBy` composite keys on all 7 entity types (mount/loadout/magazine/comms/platform/weapon)
- [x] Propose* batch insert order stable (DBI-1.2)
- [x] Export sort keys match reader sort keys (PLE-1.3)
- [x] No `DateTime.Now` in commit or export paths (`ICatalogClock` only)
- [x] Aliases do not rewrite canonical keys (DBI-7.3)

## Verify Commands

```powershell
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj --filter "WriteGate|Platform|Canonical" -v minimal
```

## GitNexus Symbols to Impact-Check

| Symbol | Risk | Rule |
|--------|------|------|
| `CatalogPlatformBinding` | MEDIUM | Sort-key / CanonicalId paths |
| `CatalogWeaponRecord` | MEDIUM | Sort-key / CanonicalId paths |
| `CatalogMount` | MEDIUM | Sort-key / CanonicalId paths |
| `CatalogLoadout` | MEDIUM | Sort-key / CanonicalId paths |
| `CatalogMagazineEntry` | MEDIUM | Sort-key / CanonicalId paths |
| `CatalogCommsBinding` | MEDIUM | Sort-key / CanonicalId paths |
| `CatalogWriteGate` | HIGH | Propose* `OrderBy` paths |

After edits: `npx gitnexus detect_changes --repo cmano-clone` before commit.

## Files to Create / Modify

| Action | Path |
|--------|------|
| Create | `src/ProjectAegis.Data.Tests/Catalog/CatalogSortKeyDeterminismTests.cs` (or `CanonicalIdDeterminismTests.cs`) |
| Extend | `src/ProjectAegis.Data.Tests/Platform/PlatformWorkbookRoundTripTests.cs` |
| Extend | `src/ProjectAegis.Data.Tests/Import/CmoMarkdownImporterTests.cs` |
| Modify | `src/ProjectAegis.Data/WriteGate/CatalogWriteGate.cs` (OrderBy helpers if needed) |
| Modify | Catalog record types under `src/ProjectAegis.Data/Platform/` (sort-key comparers) |

## Completion Notes

- `CatalogSortKeyComparer` + `CatalogSortKeyGoldenHashes` centralize DBI-7.3 composite keys; `CatalogWriteGate` delegates sort before staging inserts.
- `CatalogSortKeyDeterminismTests`: 13 cases (7 entity ProposeBatch order + fixture hash + alias + export key parity + workbook golden).
- Verification @ `47419e4`: full solution **538/538 PASS**; scoped `WriteGate|Platform|CatalogSortKey` **68/68 PASS**.
- GitNexus `detect_changes` deferred to S23-07 closeout slice.

## References

- Kickoff: `production/sprints/sprint-23-platform-phase-b-doctrine-polish.md` (S23-06)
- Implementation plan: `docs/superpowers/plans/sprint-23-implementation.md`
- Data plan: `production/agentic/sprint-23-plan-data-2026-06-17.md` (S23-D03)
- Req 06: `Game-Requirements/requirements/06-Database-Intelligence.md` (DBI-7.3)