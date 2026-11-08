# S32-06 story-done — Platform Editor Phase F Damage Unity Surfacing

**Story:** `production/epics/sprint-32-platform-editor-phase-f/story-032-06-platform-phase-f-damage.md`  
**Status:** Complete  
**Completed:** 2026-06-19

## Verdict: COMPLETE

| AC | Test / Evidence | Status |
|----|-----------------|--------|
| `PlatformCatalogViewerHost` displays damage fields (`MaxHp`, resilience, damage workbook columns) | `PlatformCatalogPanel.uxml` detail + list columns; `PlatformCatalogListProjection`; `PlatformCatalogDetailProjection` | **PASS** |
| Import staging diff surfaces `MaxHp` edit deltas before approve | `PlatformImportStagingProjection.ExtractDamageDeltaRows` + `BuildDiffRows` | **PASS** |
| Headless propose→approve round-trip tests PASS | `PlatformImportPanelTests::Import_damage_MaxHp_round_trip_propose_acknowledge_approve_readback_baltic_fixture` | **PASS** |
| Writes route `PlatformImportPanelHost` → `PlatformWorkbookWriteBridge` only | Existing S29-04 bridge path unchanged; grep tests preserved | **PASS** |
| No new SQLite migrations; no write-gate bypass | No migration files touched; viewer/import grep tests PASS | **PASS** |
| ZERO touch `DelegationBridge.cs` | `git diff HEAD -- DelegationBridge.cs` empty | **PASS** |

## Architecture

- **Phase F (damage surfacing):** `CatalogPlatformBrowseRow` extended with `Resilience`, `WithdrawThresholdPct`, `CriticalFlags` from `CatalogPlatformDamage`; viewer list/detail projections surface workbook-aligned damage columns.
- **Staging diff:** `PlatformImportStagingProjection` promotes Platforms-sheet `MaxHp` / `WithdrawThresholdPct` / `CriticalFlags` cell edits to explicit `DAMAGE row=…` diff lines before entity-level grouping.
- **Read-only viewer:** `PlatformCatalogViewerHost` remains export/diff read-only; writes stay on import panel → `PlatformWorkbookWriteBridge`.

## Verify

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "PlatformImport|PlatformCatalogViewer" -v minimal
# Passed: 21/21

dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "WriteGate|Platform" -v minimal
# Passed: 152/152

dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj \
  --filter "PlatformCatalog|CatalogPlatformBrowse" -v minimal
# Passed: 14/14

git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
# empty — ZERO touch
```

## Files changed

| File | Change |
|------|--------|
| `src/ProjectAegis.Delegation/Projection/CatalogPlatformBrowseProjection.cs` | Extended browse row with damage workbook fields |
| `src/ProjectAegis.Delegation/Projection/PlatformCatalogDetailProjection.cs` | HP / resilience / withdraw / flags detail labels |
| `src/ProjectAegis.Delegation/Projection/PlatformCatalogListProjection.cs` | **NEW** — list line formatting with damage columns |
| `src/ProjectAegis.Delegation/Projection/PlatformImportStagingProjection.cs` | Damage delta extraction + combined diff rows |
| `unity/ProjectAegis/Assets/Scripts/Runtime/PlatformCatalogViewerHost.cs` | Wire damage detail labels + list projection |
| `unity/ProjectAegis/Assets/UI/PlatformCatalog/PlatformCatalogPanel.uxml` | Resilience / withdraw / flags detail lines |
| `src/ProjectAegis.Delegation.UnityAdapter.Tests/Platform/PlatformCatalogViewerTests.cs` | Damage surfacing + UXML/host assertions |
| `src/ProjectAegis.Delegation.UnityAdapter.Tests/Platform/PlatformImportPanelTests.cs` | MaxHp propose→approve round-trip + staging diff |
| `src/ProjectAegis.Delegation.Tests/Projection/PlatformCatalogDetailProjectionTests.cs` | Damage detail label coverage |
| `src/ProjectAegis.Delegation.Tests/Projection/CatalogPlatformBrowseProjectionTests.cs` | Damage field join from export data |
| `src/ProjectAegis.Delegation.Tests/Projection/PlatformCatalogListProjectionTests.cs` | **NEW** — list line damage column tests |
| `production/epics/sprint-32-platform-editor-phase-f/story-032-06-platform-phase-f-damage.md` | Status Complete, AC checked |
| `production/agentic/stacks/sprint32/S32-06-DONE.md` | **NEW** — this evidence file |