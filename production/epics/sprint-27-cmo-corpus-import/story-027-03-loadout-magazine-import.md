---
id: S27-03
status: Complete
type: Integration
priority: must-have
graphite_branch: stack/sprint27/cmo-loadout-magazine
estimate_days: 2
dependencies:
  - S27-01 green baseline
owner: team-data
sprint: 27
req_trace: Req 06; PLE-2.3 FK quarantine
---

# Story 027-03 — CMO Mount→Loadout→Magazine Import

> **Epic:** sprint-27-cmo-corpus-import  
> **ADR:** ADR-011 (write-gate only)

## Summary

Extend `CmoMarkdownImporter` / `CmoMarkdownImportProposer` beyond S26-03 mounts-only to stage loadout + magazine rows via `ProposeLoadoutBatch` + `ProposeMagazineBatch` → `ApproveBatch`.

## Acceptance Criteria

- [x] Platform import path derives loadout + magazine rows from CMO platform tables
- [x] `ProposeLoadoutBatch` + `ProposeMagazineBatch` wired (extend-only)
- [x] Baltic fixture E2E: mount + loadout + magazine readable via `ICatalogReader`
- [x] Orphan `WeaponId`/`MountId` → quarantine (never silent drop)
- [x] Chunk 500/batch preserved
- [x] `gitnexus impact CatalogWriteGate` archived before merge

## QA Test Cases

- **AC-1**: Baltic round-trip
  - Given: `baltic-platform-mini.md` fixture
  - When: import → approve → read-back
  - Then: mount + loadout + magazine rows in stable sort
  - Edge cases: orphan FK quarantine; empty loadout table

## Verify Commands

```bash
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "CmoMarkdown|WriteGate|Platform" -v minimal
npx gitnexus impact CatalogWriteGate --direction upstream
```

## References

- S26-03 completion: `production/agentic/stacks/sprint26/S26-03-DONE.md`
- QA plan: `production/qa/qa-plan-sprint-27-2026-06-18.md`

## Completion Notes
**Completed**: 2026-06-18
**Criteria**: 6/6 passing
**Deviations**: None — `CatalogWriteGate` extend-only (no gate source edits)
**Test Evidence**: `src/ProjectAegis.Data.Tests/Import/CmoMarkdownLoadoutMagazineTests.cs`
**Code Review**: Skipped (lean mode)