# S34-06 — Platform Editor Phase H LinkCatalog Unity Evidence

**Date:** 2026-06-19  
**Story:** S34-06

## Changes

### Projection layer (headless-testable)
- `CatalogLinkListProjection` — global link catalog from `ICatalogReader.GetSortedLinks()` + display-name lookup
- `PlatformLinkListProjection` — formats `LinkId`, `DisplayName`, `LinkType`, `LatencyMsNominal`
- `PlatformImportStagingProjection.ExtractLinkCatalogDeltaRows` — LinkCatalog sheet diffs (`LINK row=…`)
- `PlatformCommsListProjection` — comms rows resolve link `DisplayName` when present in link catalog

### Unity viewer (schema-only, read-only)
- `PlatformCatalogPanel.uxml` — `platform-catalog-links`, `platform-catalog-links-list`
- `PlatformCatalogPanel.uss` — link catalog list styles
- `PlatformCatalogViewerHost` — global link list on refresh; comms list uses link display names

### Tests
- `PlatformLinkCatalogTests.cs` — 13 headless tests mirroring `PlatformCommsTests`
- `PlatformCommsTests.cs` — updated grep assertions for display-name overload

## Hard gates

| Gate | Status |
|------|--------|
| ZERO touch `DelegationBridge.cs` | PASS |
| Writes via `PlatformImportPanelHost` → `PlatformWorkbookWriteBridge` only | PASS |
| Extend-only patterns | PASS |

## Verification

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "PlatformLinkCatalog|PlatformComms" -v minimal
# Passed: 25/25 (PlatformLinkCatalog 13, PlatformComms 12)

git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
# empty — ZERO touch
```

## Files changed

| File | Change |
|------|--------|
| `src/ProjectAegis.Delegation/Projection/CatalogLinkListProjection.cs` | **NEW** |
| `src/ProjectAegis.Delegation/Projection/PlatformLinkListProjection.cs` | **NEW** |
| `src/ProjectAegis.Delegation/Projection/PlatformCommsListProjection.cs` | DisplayName resolution overload |
| `src/ProjectAegis.Delegation/Projection/PlatformImportStagingProjection.cs` | LinkCatalog delta extraction |
| `unity/.../PlatformCatalogPanel.uxml` | Link catalog list section |
| `unity/.../PlatformCatalogPanel.uss` | Comms + link list styles |
| `unity/.../PlatformCatalogViewerHost.cs` | Wire global link list + comms display names |
| `src/ProjectAegis.Delegation.UnityAdapter.Tests/Platform/PlatformLinkCatalogTests.cs` | **NEW** — 13 tests |
| `src/ProjectAegis.Delegation.UnityAdapter.Tests/Platform/PlatformCommsTests.cs` | Grep assertion updates |