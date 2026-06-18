# S24-04 progress — phase-b-importer

**Branch:** `stack/sprint24/phase-b-importer`
**Story:** `production/sprints/sprint-24-phase-b-import-present-polish.md` §S24-04
**Status:** Core path started (~70% AC) — not story-done

## Implemented

- `PlatformWorkbookImporter`: Mobility/Signatures/Emcon in `SupportedSheets`; `Stage()` proposes `ProposeMobility/Signature/EmconBatch`
- FK guard: orphan `PlatformId` rows rejected before propose (notes on `PlatformImportResult`)
- `PlatformImportResult`: `MobilityBatchId` / `SignatureBatchId` / `EmconBatchId`
- Minimal S24-03 write-gate APIs on this branch: `IWriteGate` + `CatalogWriteGate` propose/approve/reject for Phase B staging tables
- `CatalogSortKeyComparer`: `SortMobility` / `SortSignatures` / `SortEmcon`
- Tests: `PlatformWorkbookPhaseBImportTests.cs` (empty-diff golden, edit stage, FK guard, approve smoke)
- CLI: `platform_import_xlsx` JSON exposes Phase B batch ids

## Test evidence

```bash
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj --filter "Platform|CatalogPhaseB" -v minimal
# Passed: 63
```

## Blockers / notes

- **S24-03 was scaffold-only** on stack tip — added minimal `ProposeMobility/Signature/EmconBatch` + approve paths on this branch so importer compiles/tests
- S24-02 reader/resolver work may land on sibling branch; importer uses injected `PlatformCatalogExportData` snapshot provider in tests
- Deferred to S24-05: Phase B validator rule pack, deletion diff category, ClosedXML Phase B UX golden

## Remaining for story-done

- Full E2E export → edit → import → approve → read-back across all three Phase B sheets
- Explicit deletion diff handling (PLE-2.4)
- `Data.Excel` project tests when S24-11 lands (no `ProjectAegis.Data.Excel.Tests` csproj on branch)