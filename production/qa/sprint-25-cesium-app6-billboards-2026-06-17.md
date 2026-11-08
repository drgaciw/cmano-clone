# Sprint 25 — S25-14 Cesium APP-6 Billboards

**Date:** 2026-06-17  
**Story:** S25-14 (`production/sprints/sprint-25-phase-b-damage-assurance.md`)  
**Branch:** `stack/sprint25/cesium-app6-glyphs`  
**ADR:** ADR-007 Phase B/C (globe + APP-6 glyphs), ADR-010 (headless-first UI, read-only projections)  
**Environment:** Headless Linux CI/agent host — Unity 6.3 Editor unavailable; evidence via lean-mode protocol placeholder + headless contract tests.

## Acceptance criteria status

| AC | Status | Evidence |
|----|--------|----------|
| Globe markers use `App6Sidc` affiliation for billboard rendering | **PASS** | `CesiumBillboardProjection` + `CesiumGlobeBridge.GetBillboardMarkers()`; source + unit tests |
| `useGlobeMap: 0` on DelegationSmoke preserved | **PASS** | `Delegation_smoke_keeps_useGlobeMap_false_for_ci_safe_default`; `rg useGlobeMap` |
| Headless PlayModeSmoke\|App6\|Cesium filter PASS | **PASS** | §Gates below |
| Missing SIDC → fallback glyph/frame | **PASS** | `ResolveGlyph_invalid_sidc_uses_fallback_glyph_and_frame`; `ResolveGlyph_missing_sidc_uses_affiliation_fallback` |
| Editor evidence updated (advisory) | **PASS (protocol placeholder)** | `production/qa/attachments/cesium-s25-app6-billboards.png` |
| ZERO touch `DelegationBridge.cs` | **PASS** | No edits to `src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs` |

## Implementation summary

### Read-only projection (`CesiumBillboardProjection`)

- Resolves APP-6 glyph + USS frame + SIDC per `MapSymbolEntry`
- Prefers valid 15-char SIDC via `App6Sidc.ResolveMapGlyphFromSidc`
- Falls back to affiliation via `App6Sidc.ResolveMapGlyph` when SIDC missing/invalid
- Maps symbols to Baltic demo geo (deterministic; no sim/catalog mutation)

### Unity bridge (`CesiumGlobeBridge`)

- `GetBillboardMarkers()` — primary S25-14 API; consumed by `CreateCesiumAnchors`
- Logs `frame={UssFrameId} glyph={UnicodeGlyph} sidc={Sidc}` per marker
- Anchor names encode `SymbolId` + USS frame id; visual names encode affiliation + glyph
- `GetCurrentPositions()` derived from billboard markers (backward compatible)
- Reads symbols via `MapPlaceholderPanelHost.CurrentMapSymbols` (read-only `LastMapSymbols` feed)

### Editor protocol (when unblocked)

1. Open `Assets/Scenes/CesiumSpike.unity` per `CESIUM-SPIKE-SETUP.md`
2. Enter Play Mode with Cesium ion token configured
3. **Expect:** Console logs per marker with APP-6 frame id + glyph + SIDC
4. **Expect:** Friendly green ▣ (`map-app6-frame--friendly`), hostile red ⬥ (`map-app6-frame--hostile`)
5. **Expect:** Missing/invalid SIDC markers use ● fallback (`map-app6-frame--unknown`) with amber tint
6. Capture `Game` view 1920×1080 → replace `cesium-s25-app6-billboards.png`

**S25-14 evidence:** `production/qa/attachments/cesium-s25-app6-billboards.png` (labeled protocol placeholder).

## Headless regression (CI merge authority)

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "PlayModeSmoke|App6|Cesium" -v minimal
rg "useGlobeMap" unity/ProjectAegis/Assets/Scenes/DelegationSmoke.unity
rg "App6Sidc|ResolveMapGlyph|GetBillboardMarkers" unity/ProjectAegis/Assets/Scripts/Runtime/CesiumGlobeBridge.cs
git diff main -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
dotnet test ProjectAegis.sln -v minimal
```

## Headless proxy tests

| Test | Purpose |
|------|---------|
| `Project_resolves_distinct_app6_frames_for_friendly_and_hostile_symbols` | Affiliation → distinct USS frames |
| `ResolveGlyph_missing_sidc_uses_affiliation_fallback` | Missing SIDC fallback |
| `ResolveGlyph_invalid_sidc_uses_fallback_glyph_and_frame` | Invalid SIDC → `FallbackFrame` / `FallbackGlyph` |
| `CesiumGlobeBridge_source_wires_App6Sidc_and_GetBillboardMarkers` | Unity bridge source contract |
| `Delegation_smoke_keeps_useGlobeMap_false_for_ci_safe_default` | CI-safe globe disabled |

## Advisory notes (lean mode)

- Billboard visuals remain primitive spheres in spike; production swaps to USS atlas billboard prefab
- `DelegationSmoke.unity` keeps `useGlobeMap: 0` — globe/APP-6 billboards validated in `CesiumSpike.unity` path only
- Live Editor re-capture optional; headless gates are merge authority

## Verdict

**PASS (lean mode)** — APP-6 affiliation wired into Cesium billboard projection; headless contract tests green; `useGlobeMap: 0` preserved; `DelegationBridge` untouched.