# S25-14 story-done evidence — cesium-app6-glyphs

**Branch:** `stack/sprint25/cesium-app6-glyphs`  
**SHA:** `cfde2fd`  
**PR:** #226  
**Story:** `production/sprints/sprint-25-phase-b-damage-assurance.md` §S25-14  
**Status:** Complete  
**Completed:** 2026-06-17  
**Review mode:** lean (headless contract tests + protocol placeholder evidence)

## Deliverables

- `CesiumBillboardProjection` + `CesiumBillboardMarker` — read-only APP-6 billboard resolution (ADR-010)
- `CesiumGlobeBridge.GetBillboardMarkers()` — wires `App6Sidc` affiliation/SIDC into globe markers
- `MapPlaceholderPanelHost.CurrentMapSymbols` — read-only feed for Cesium bridge
- `CesiumApp6BillboardContractTests.cs` — headless affiliation, fallback, and source contract tests
- `production/qa/sprint-25-cesium-app6-billboards-2026-06-17.md` — evidence doc
- `production/qa/attachments/cesium-s25-app6-billboards.png` — labeled protocol placeholder (1920×1080)
- **ZERO touch** `DelegationBridge.cs`

## Acceptance criteria traceability

| AC | Evidence | Status |
|----|----------|--------|
| Globe markers use `App6Sidc` affiliation for billboard rendering | `CesiumBillboardProjection`; `CesiumGlobeBridge` logs frame/glyph/sidc | **PASS** |
| `useGlobeMap: 0` on DelegationSmoke preserved | `Delegation_smoke_keeps_useGlobeMap_false_for_ci_safe_default`; `useGlobeMap: 0` | **PASS** |
| Headless PlayModeSmoke\|App6\|Cesium filter PASS | §Verify below | **PASS** |
| Missing SIDC → fallback glyph/frame | `ResolveGlyph_*` tests; `App6Sidc.FallbackFrame` | **PASS** |
| Editor evidence updated (advisory) | `cesium-s25-app6-billboards.png` + evidence doc | **PASS** |
| Test floor ≥592 | Full sln — see §Verify | **PASS** |
| ZERO touch `DelegationBridge.cs` | Empty diff vs `main` | **PASS** |

## Verify

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone
dotnet build ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "PlayModeSmoke|App6|Cesium" -v minimal
dotnet test ProjectAegis.sln -v minimal
rg "useGlobeMap" unity/ProjectAegis/Assets/Scenes/DelegationSmoke.unity
rg "App6Sidc|GetBillboardMarkers" unity/ProjectAegis/Assets/Scripts/Runtime/CesiumGlobeBridge.cs
git diff main -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
ls production/qa/attachments/cesium-s25-app6-billboards.png
```

## Verdict

**COMPLETE** — S25-14 APP-6 Cesium billboard wiring landed; headless gates are merge authority; `DelegationSmoke` CI-safe default unchanged.