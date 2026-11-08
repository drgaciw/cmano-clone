# S28-07 story-done — Platform Viewer Export/Diff Hook

**Story:** `production/epics/sprint-28-platform-editor-write/story-028-07-viewer-export-hook.md`  
**Status:** Complete  
**Completed:** 2026-06-18

## Verdict: COMPLETE

| AC | Test / Evidence | Status |
|----|-----------------|--------|
| Viewer panel exposes export/diff trigger (read-only) | `PlatformCatalogPanel.uxml` `platform-catalog-export` + `platform-catalog-diff`; `PlatformCatalogViewerHost` wires read-only bridge | **PASS** |
| Export invokes data API path (not raw SQLite) | `PlatformCatalogExportBridge` → `PlatformCatalogExportResolver` + `PlatformWorkbookExporter` | **PASS** |
| Headless export path test PASS | `PlatformCatalogExportBridgeTests` 3/3 + `PlatformCatalogViewerTests` export trigger | **PASS** |
| No `CatalogWriteGate` / `IWriteGate` bypass in viewer host | Source grep in `PlatformCatalogViewerTests` + bridge source assertions | **PASS** |
| Import UI deferred to CLI authority | Viewer logs CLI deferral; no import/propose UI chrome | **PASS** |
| ZERO touch `DelegationBridge.cs` | `git diff HEAD -- DelegationBridge.cs` empty | **PASS** |

## Architecture

- **Phase C (viewer):** `PlatformCatalogExportBridge` — export/diff only via approved data-layer APIs.
- **Phase D (write):** `PlatformWorkbookWriteBridge` — propose/approve unchanged; viewer does **not** reference it.
- **CLI authority:** `platform_export_xlsx` / `platform_import_xlsx` remain the Excel-primary path (ADR-011).

## Verify

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "PlatformCatalog|Excel|PlayModeSmoke" -v minimal

dotnet test src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj \
  --filter "Platform" -v minimal

git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
# empty — ZERO touch
```

## Files changed

| File | Change |
|------|--------|
| `src/ProjectAegis.Delegation.UnityAdapter/Bridge/PlatformCatalogExportBridge.cs` | **NEW** — read-only export/diff bridge |
| `src/ProjectAegis.Delegation.UnityAdapter.Tests/Platform/PlatformCatalogExportBridgeTests.cs` | **NEW** — bridge headless tests |
| `src/ProjectAegis.Delegation.UnityAdapter.Tests/Platform/PlatformCatalogViewerTests.cs` | Export UXML/host grep + export trigger test |
| `unity/ProjectAegis/Assets/Scripts/Runtime/PlatformCatalogViewerHost.cs` | Export/diff button hooks via read-only bridge |
| `unity/ProjectAegis/Assets/UI/PlatformCatalog/PlatformCatalogPanel.uxml` | Export + Diff buttons |
| `unity/ProjectAegis/Assets/UI/PlatformCatalog/PlatformCatalogPanel.uss` | Action button styles |
| `production/epics/sprint-28-platform-editor-write/story-028-07-viewer-export-hook.md` | Status Complete, AC checked |
| `production/agentic/stacks/sprint28/S28-07-DONE.md` | **NEW** — this evidence file |