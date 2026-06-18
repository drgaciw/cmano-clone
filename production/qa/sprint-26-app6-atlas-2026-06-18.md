# S26-06 APP-6 Texture Atlas Asset Pack — Verdict

**Date:** 2026-06-18  
**Story:** S26-06 APP-6 texture atlas asset pack  
**ADR:** ADR-007 Phase C; ADR-010 headless-first (read-only projection)

## Verdict: **PROCEED**

Texture-backed APP-6 atlas frames are viable beyond the S25-08 USS vector MVP. `App6Sidc` registry now covers **7** distinct affiliation/frame combinations (Friendly, Hostile, Friendly-destroyed, Unknown, Neutral, Suspect, Pending). `App6AtlasSpriteSheet` + `App6AtlasCatalog` expose sprite-sheet slice metadata for headless hosts; Unity USS references `App6FrameAtlas.png` with per-frame `background-position` slices. Map projection remains read-only; **ZERO** `DelegationBridge` touch.

### Scope delivered

| Item | Status |
|------|--------|
| `App6Sidc` expanded registry (Neutral, Suspect, Pending + SIDC identity fix for `S`) | Done |
| `App6AtlasSpriteSheet` slice metadata + Addressables manifest | Done |
| `App6FrameAtlas.png` 7-frame sprite sheet (112×16) | Done |
| `MapPlaceholderPanel.uss` texture atlas frame classes | Done |
| `App6AtlasAddressablesManifest.json` (`Map/App6FrameAtlas`) | Done |
| Headless tests (`App6AtlasAssetTests`, extended `App6Sidc`, `App6SidcMapGlyph`) | Done |
| `DelegationBridge.cs` touch | **ZERO** |

### Test evidence

```bash
export PATH="/home/username01/.dotnet:$PATH"

# Unity adapter App6 + MapPanel (filter)
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "App6|MapPanelBinder" -v minimal

# Headless APP-6 + map projection
dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj \
  --filter "App6|MapPanel|MapPicture" -v minimal
```

### Glyph / frame mapping (texture atlas table)

| Affiliation | Role | Unicode fallback | USS frame class | Sprite slice X | 15-char SIDC |
|-------------|------|------------------|-----------------|----------------|--------------|
| Friendly | Surface unit (alive) | `▣` | `map-app6-frame--friendly` | 0 | `SFGPU----------` |
| Friendly | Destroyed | `▢` | `map-app6-frame--friendly-destroyed` | 32 | `SFGPU----------` |
| Hostile | Contact | `⬥` | `map-app6-frame--hostile` | 16 | `SHGPU----------` |
| Neutral | Surface unit | `◻` | `map-app6-frame--neutral` | 64 | `SNGPU----------` |
| Suspect | Contact | `◇` | `map-app6-frame--suspect` | 80 | `SSGPU----------` |
| Pending | Surface unit | `◎` | `map-app6-frame--pending` | 96 | `SPGPU----------` |
| Unknown / invalid SIDC | Fallback | `●` | `map-app6-frame--unknown` | 48 | `SUZPU----------` |

### Degradation paths

| Condition | Display |
|-----------|---------|
| Atlas loaded + frame registered | USS sprite slice (`background-image` + `background-position`) |
| Atlas not loaded (`App6AtlasCatalog.Unavailable`) | Unicode glyph |
| Atlas loaded but frame missing | Unicode glyph for that symbol |

### Risks / deferrals

- **Addressables package** not yet in `manifest.json` — manifest JSON stub documents `Map/App6FrameAtlas`; wire full Addressables group when package is added (S27+).
- **Editor visual sign-off** screenshot deferred to S26-07; headless tests prove atlas contract.
- **Cesium billboard prefab** still uses primitive spheres; consumes same `UssFrameId` contract (S25-14 regression green).

### Recommendation

Proceed with Phase C polish: add `com.unity.addressables` package, promote manifest stub to live group, capture Editor sprite screenshots in S26-07. Keep projection read-only and `DelegationBridge`-free per ADR-010.