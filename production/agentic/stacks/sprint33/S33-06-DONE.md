# S33-06 story-done — Platform Editor Phase G Comms Unity Surfacing

**Story:** `production/epics/sprint-33-platform-editor-phase-g/story-033-06-platform-phase-g-comms.md`  
**Status:** Complete  
**Completed:** 2026-06-19

## Verdict: COMPLETE

| AC | Test / Evidence | Status |
|----|-----------------|--------|
| Viewer shows comms fittings per platform (`LinkId`, `Role`, `SatcomCapable`) | `PlatformCatalogPanel.uxml` comms list; `PlatformCommsListProjection`; `CatalogPlatformCommsProjection`; `PlatformCatalogViewerHost` | **PASS** |
| Import staging diff surfaces comms field deltas | `PlatformImportStagingProjection.ExtractCommsDeltaRows` + `BuildDiffRows` | **PASS** |
| Headless propose→approve round-trip tests PASS | `PlatformCommsTests::PlatformComms_import_round_trip_propose_acknowledge_approve_readback_baltic_fixture` | **PASS** |
| Writes route `PlatformImportPanelHost` → `PlatformWorkbookWriteBridge` only | Existing S29-04 bridge path unchanged | **PASS** |
| No new SQLite migrations; no write-gate bypass | No migration files touched; projection grep tests PASS | **PASS** |
| ZERO touch `DelegationBridge.cs` | `git diff HEAD -- DelegationBridge.cs` empty | **PASS** |

## Architecture

- **Phase G (comms surfacing):** `ICatalogReader.GetSortedComms()` on SQLite + in-memory readers; `CatalogPlatformCommsProjection` filters fittings per selected platform; `PlatformCommsListProjection` formats `LinkId` / `Role` / `SatcomCapable` list lines.
- **Staging diff:** `PlatformImportStagingProjection` promotes Comms-sheet `LinkId` / `Role` / `SatcomCapable` cell edits and row adds to explicit `COMMS row=…` diff lines before entity-level grouping.
- **Read-only viewer:** `PlatformCatalogViewerHost` comms `ListView` remains export/diff read-only; writes stay on import panel → `PlatformWorkbookWriteBridge`.

## Verify

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "PlatformImport|PlatformCatalogViewer|PlatformComms" -v minimal
# Passed: 28/28

dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj \
  --filter "PlatformComms|CatalogPlatformComms" -v minimal
# Passed: 3/3

dotnet test ProjectAegis.sln -v minimal
# Passed: 1128/1128

git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
# empty — ZERO touch
```

## Files changed

| File | Change |
|------|--------|
| `src/ProjectAegis.Data/Catalog/ICatalogReader.cs` | `GetSortedComms()` default + core |
| `src/ProjectAegis.Data/Catalog/InMemoryCatalogReader.cs` | Comms fixture parameter + sorted comms |
| `src/ProjectAegis.Data/Catalog/SqliteCatalogReader.cs` | Public `GetSortedComms()` with cache |
| `src/ProjectAegis.Delegation/Projection/CatalogPlatformCommsProjection.cs` | **NEW** — per-platform comms filter |
| `src/ProjectAegis.Delegation/Projection/PlatformCommsListProjection.cs` | **NEW** — comms list line formatting |
| `src/ProjectAegis.Delegation/Projection/PlatformImportStagingProjection.cs` | Comms delta extraction + combined diff rows |
| `unity/.../PlatformCatalogViewerHost.cs` | Wire comms list on platform selection |
| `unity/.../PlatformCatalogPanel.uxml` | Comms fittings list section |
| `src/ProjectAegis.Delegation.UnityAdapter.Tests/Platform/PlatformCommsTests.cs` | **NEW** — 7 headless Phase G tests |
| `src/ProjectAegis.Delegation.Tests/Projection/PlatformCommsListProjectionTests.cs` | **NEW** |
| `src/ProjectAegis.Delegation.Tests/Projection/CatalogPlatformCommsProjectionTests.cs` | **NEW** |