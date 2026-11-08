# S25-08 story-done evidence — app6-atlas-phase-c

**Branch:** `stack/sprint25/app6-atlas-phase-c`  
**Story:** `production/sprints/sprint-25-phase-b-damage-assurance.md` §S25-08  
**Status:** Complete

## Deliverables

- `App6Sidc` expanded with USS frame ids and `ResolveMapGlyph` / `ResolveMapGlyphFromSidc`
- `App6GlyphAtlas`, `App6AtlasCatalog`, `IApp6AtlasAvailability` — atlas load + frame-missing degradation
- `MapSymbolEntry.App6UssFrameId`; `MapSymbolDisplayRow` atlas fields
- `MapPictureProjection` + `MapPanelBinder` wired to atlas resolver (read-only projection)
- `MapPlaceholderPanel.uss` APP-6 frame classes; `MapPlaceholderPanelHost` atlas rendering
- Tests: `App6SidcMapGlyphTests.cs`; extended `App6SidcTests`, `MapPictureProjectionTests`, `MapPanelBinderTests`
- Spike verdict: `production/qa/sprint-25-app6-atlas-2026-06-17.md` — **PROCEED**
- **ZERO touch** `DelegationBridge.cs`

## Verify

```bash
export PATH="/home/username01/.dotnet:$PATH"
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "PlayModeSmoke|Doctrine|MapPanelBinder|App6" -v minimal
dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj \
  --filter "App6|MapPicture|MapPanel" -v minimal
dotnet test ProjectAegis.sln -v minimal
rg "DelegationBridge" src/ProjectAegis.Delegation/Projection/ --glob "*App6*"
```

**Results:** 22 + 19 filtered PASS; full solution **639 PASS** (was 628).

## Acceptance criteria

| AC | Verdict |
|----|---------|
| Expand `App6Sidc` with USS frame ids for atlas-backed glyphs | **PASS** |
| ≥2 distinct icons via headless tests (friendly vs hostile) | **PASS** |
| Atlas load failure degrades to unicode fallback | **PASS** |
| `App6SidcMapGlyphTests.cs` added | **PASS** |
| Headless App6 + MapPanel tests extended | **PASS** |
| Spike doc PROCEED | **PASS** |
| ZERO `DelegationBridge` touch | **PASS** |
| Test floor ≥592 | **PASS** — 639 total |