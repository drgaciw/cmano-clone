# S24-07 story-done evidence — c2-app6-spike

**Branch:** `stack/sprint24/c2-app6-spike`  
**Story:** `production/sprints/sprint-24-phase-b-import-present-polish.md` §S24-07  
**Status:** Complete

## Deliverables

- `src/ProjectAegis.Delegation/Projection/App6Sidc.cs` — data-driven glyph + 15-char SIDC resolver
- `MapPictureProjection` wired to `App6Sidc` for `ShapeGlyph` + `App6Sidc` field on `MapSymbolEntry`
- Tests: `App6SidcTests.cs`; extended `MapPictureProjectionTests`, `MapPanelBinderTests`
- Spike verdict: `production/qa/sprint-24-app6-spike-2026-06-17.md` — **PROCEED**
- **ZERO touch** `DelegationBridge.cs`

## Verify

```bash
export PATH="/home/username01/.dotnet:$PATH"
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter "PlayModeSmoke|Doctrine|MapPanelBinder" -v minimal
dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj --filter "App6|MapPanel|MapPicture" -v minimal
```

**Results:** 17 + 14 = 31 PASS