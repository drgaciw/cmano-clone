# S29-04 story-done — Platform Editor Phase E Unity Import UI

**Story:** `production/epics/sprint-29-platform-editor-phase-e/story-029-04-unity-import-ui.md`  
**Status:** Complete  
**Date:** 2026-06-18

## Verdict: COMPLETE

| AC | Test / Evidence | Status |
|----|-----------------|--------|
| In-engine import invokes `PlatformWorkbookWriteBridge` propose path | `PlatformImportPanelHost` + `PlatformImportPanelTests` | **PASS** |
| Staging review UX wired (diff preview before approve) | `PlatformImportStagingProjection` + acknowledge gate | **PASS** |
| Import→propose→approve round-trip on Baltic fixture | `PlatformImportPanelTests::Import_round_trip_propose_acknowledge_approve_readback_baltic_fixture` | **PASS** |
| Headless write-gate + viewer tests PASS | filters below | **PASS** |
| `CatalogWriteGate` extend-only | no gate edits; bridge delegates to `PlatformWorkbookWriteService` | **PASS** |
| No write-gate bypass in import host | grep tests in `PlatformImportPanelTests` | **PASS** |
| ZERO touch `DelegationBridge.cs` | empty `git diff` | **PASS** |

## Architecture

- **Phase E (import UI):** `PlatformImportPanelHost` — workbook pick → `PlatformWorkbookWriteBridge.ProposeWorkbook` / `ProposeWorkbookFromFile` → entity-level diff via `PlatformImportStagingProjection` → acknowledge → approve/reject via bridge.
- **Phase C (viewer):** `PlatformCatalogViewerHost` unchanged — read-only export/diff only.
- **CLI authority preserved:** batch approve remains available via `platform_import_xlsx` + `catalog_write_approve`.

## Verify

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "WriteGate|Platform|Excel|Snapshot" -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "PlayModeSmoke|PlatformCatalog|PlatformWorkbook|PlatformImport" -v minimal
git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
# empty — ZERO touch
```

## Files changed

| File | Change |
|------|--------|
| `src/ProjectAegis.Delegation/Projection/PlatformImportStagingProjection.cs` | **NEW** — entity-level diff + acknowledge gate state |
| `src/ProjectAegis.Delegation.UnityAdapter/Bridge/PlatformWorkbookWriteBridge.cs` | `ProposeWorkbookFromFile` helper |
| `unity/ProjectAegis/Assets/Scripts/Runtime/PlatformImportPanelHost.cs` | **NEW** — import staging UI host |
| `unity/ProjectAegis/Assets/UI/PlatformImport/PlatformImportPanel.uxml` | **NEW** — import panel layout |
| `unity/ProjectAegis/Assets/UI/PlatformImport/PlatformImportPanel.uss` | **NEW** — import panel styles |
| `unity/ProjectAegis/Assets/Editor/DelegationSmokeSceneBuilder.cs` | Wire `PlatformImportPanelHost` into smoke scene |
| `src/ProjectAegis.Delegation.UnityAdapter.Tests/Platform/PlatformImportPanelTests.cs` | **NEW** — round-trip + bypass grep tests |
| `src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/PlayModeSmokeHarnessTests.cs` | Import staging + scene builder smoke rows |

## Merge risks (wave-2)

| Story | Risk | Mitigation |
|-------|------|------------|
| S29-07 Doctrine visual | Low — separate UI folder/host | No shared files with import panel |
| S29-08 C2 loop | Low — `DelegationSmokeSceneBuilder` overlap possible | Coordinate scene builder edits if both touch smoke scene |
| S29-02 TL export | None — no migration/manifest edits | Import UI read-only on export manifest |
| S29-03 Nightly approve | None — CLI path unchanged | Bridge-only writes preserved |