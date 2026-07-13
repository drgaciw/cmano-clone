# S25-08 APP-6 Atlas Phase C Spike — Verdict

**Date:** 2026-06-17  
**Branch:** `stack/sprint25/app6-atlas-phase-c`  
**Story:** S25-08 APP-6 glyph atlas MVP  
**ADR:** ADR-007 Phase C; ADR-010 headless-first (read-only projection)

## Verdict: **PROCEED**

USS/atlas-backed APP-6 map glyphs are viable for Phase C MVP. `App6Sidc` now resolves unicode fallback glyphs **and** distinct USS frame ids; `App6GlyphAtlas` degrades to unicode when the atlas is unavailable or a frame is missing. Map projection remains read-only; **ZERO** `DelegationBridge` touch.

### Scope delivered

| Item | Status |
|------|--------|
| `App6Sidc` USS frame ids + `ResolveMapGlyph` | Done |
| `App6GlyphAtlas` + `App6AtlasCatalog` atlas resolver | Done |
| `MapSymbolEntry.App6UssFrameId` + display row atlas fields | Done |
| `MapPlaceholderPanel.uss` frame sprite classes (≥2 distinct) | Done |
| `MapPlaceholderPanelHost` atlas frame rendering | Done |
| Headless tests (`App6Sidc`, `App6SidcMapGlyph`, `MapPicture`, `MapPanelBinder`) | Done |
| `DelegationBridge.cs` touch | **ZERO** |

### Test evidence

```bash
export PATH="/home/username01/.dotnet:$PATH"

# PlayMode / doctrine / map binder / App6 (22 passed)
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "PlayModeSmoke|Doctrine|MapPanelBinder|App6" -v minimal

# Headless APP-6 + map projection (19 passed)
dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj \
  --filter "App6|MapPanel|MapPicture" -v minimal

# Full solution floor (639 passed)
dotnet test ProjectAegis.sln -v minimal
```

**Results:** 22 + 19 filtered PASS; full solution **639 PASS**, 0 failed (floor ≥592).

### Glyph / frame mapping (MVP table)

| Affiliation | Role | Unicode fallback | USS frame class | 15-char SIDC |
|-------------|------|------------------|-----------------|--------------|
| Friendly | Surface unit (alive) | `▣` | `map-app6-frame--friendly` | `SFGPU----------` |
| Friendly | Destroyed | `▢` | `map-app6-frame--friendly-destroyed` | `SFGPU----------` |
| Hostile | Contact | `⬥` | `map-app6-frame--hostile` | `SHGPU----------` |
| Unknown / invalid SIDC | Fallback | `●` | `map-app6-frame--unknown` | `SUZPU----------` |

### Degradation paths

| Condition | Display |
|-----------|---------|
| Atlas loaded + frame registered | USS frame `VisualElement` (empty glyph text) |
| Atlas not loaded (`App6AtlasCatalog.Unavailable`) | Unicode glyph |
| Atlas loaded but frame missing | Unicode glyph for that symbol |

### Risks / deferrals

- **Sprite atlas asset pack** (texture sheet / Addressables) deferred — MVP uses USS vector frame placeholders.
- **Cesium billboards** (S25-14) can consume `App6UssFrameId` / SIDC when globe path continues.
- **Editor visual sign-off** for atlas frames recommended before production polish; headless tests prove resolver contract.

### Recommendation

Proceed with Phase C incremental delivery: expand frame registry, add texture atlas asset pack, wire Cesium billboards (S25-14), keep projection read-only and `DelegationBridge`-free per ADR-010.