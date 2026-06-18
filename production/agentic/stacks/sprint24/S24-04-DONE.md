# S24-04 story-done evidence — phase-b-importer

**Branch:** `stack/sprint24/phase-b-importer`  
**Story:** `production/sprints/sprint-24-phase-b-import-present-polish.md` §S24-04  
**Status:** Complete

## Deliverables

- `PlatformWorkbookImporter`: Mobility/Signatures/Emcon in `SupportedSheets`; `Stage()` proposes `ProposeMobility/Signature/EmconBatch`
- FK guard: orphan `PlatformId` rows rejected before propose (notes on `PlatformImportResult`)
- `PlatformImportResult`: `MobilityBatchId` / `SignatureBatchId` / `EmconBatchId`
- Empty-diff golden on unedited Phase B round-trip
- E2E: export → edit → Stage → ApproveBatch → read-back via `SqliteCatalogReader` (Mobility + all three Phase B sheets)
- Tests: `PlatformWorkbookPhaseBImportTests.cs` (10 tests; `Platform|CatalogPhaseB` filter)

## Deferred to S24-05

- Deletion diff category (PLE-2.4)
- Phase B validator rule pack (header parity, Emcon enum sanity, validation hash golden)

## Verify

```bash
export PATH="/home/username01/.dotnet:$PATH"
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj --filter "Platform|CatalogPhaseB" -v minimal
# Passed: 73

dotnet test ProjectAegis.sln -v minimal
# Passed: 569 (87 Sim + 196 Data + 171 Delegation + 94 UnityAdapter + 21 Cli)
```

## Acceptance criteria

| AC | Verdict |
|----|---------|
| Edited cells stage + approve | **PASS** — E2E tests commit via `CatalogWriteGate.ApproveBatch` |
| Unedited round-trip empty diff | **PASS** — `Plan_unedited_Phase_B_round_trip_has_no_supported_changes` |
| FK guard on PlatformId | **PASS** — `Stage_orphan_platform_mobility_is_rejected_not_proposed` |
| Importer tests PASS | **PASS** — 73 filtered / 569 solution-wide |