# S24-07 APP-6 Phase C Spike — Verdict

**Date:** 2026-06-17  
**Branch:** `stack/sprint24/c2-app6-spike`  
**Story:** S24-07 C2 regression + APP-6 Phase C spike  
**ADR:** ADR-007 Phase C start; ADR-010 headless-first (read-only projection)

## Verdict: **PROCEED**

Headless APP-6 glyph resolution is viable for Phase C. A data-driven `App6Sidc` resolver in `ProjectAegis.Delegation` produces **two visually distinct** Toolkit placeholder glyphs (friendly surface unit `▣` vs hostile contact `⬥`) with optional 15-char SIDC strings on `MapSymbolEntry` for a future USS/icon atlas. No `DelegationBridge` changes required.

### Scope delivered

| Item | Status |
|------|--------|
| `App6Sidc.cs` glyph + SIDC resolver | Done |
| `MapPictureProjection` wired to resolver | Done |
| `MapSymbolEntry.App6Sidc` optional field | Done |
| Headless tests (`App6Sidc`, `MapPicture`, `MapPanelBinder`) | Done |
| PlayMode harness regression (tri-batch proxy) | PASS |
| `DelegationBridge.cs` touch | **ZERO** |

### Test evidence

```bash
export PATH="/home/username01/.dotnet:$PATH"

# PlayMode / doctrine / map binder proxy (17 passed)
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "PlayModeSmoke|Doctrine|MapPanelBinder" -v minimal

# Headless APP-6 + map projection (14 passed)
dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj \
  --filter "App6|MapPanel|MapPicture" -v minimal
```

**Results:** 17 + 14 = **31 tests PASS**, 0 failed.

### Glyph mapping (spike table)

| Affiliation | Role | Glyph | 15-char SIDC |
|-------------|------|-------|----------------|
| Friendly | Surface unit (alive) | `▣` | `SFGPU----------` |
| Friendly | Destroyed | `▢` | `SFGPU----------` |
| Hostile | Contact | `⬥` | `SHGPU----------` |
| Unknown / invalid SIDC | Fallback | `●` | `SUZPU----------` |

### Risks / deferrals

- **Editor tri-batch** (`Invoke-C2PlayModeSignoffBatch.ps1` comms/classify/doctrine) not run in this headless session — headless proxies cover the same code paths; Editor sign-off remains recommended before merge to main.
- **Full APP-6 atlas** (USS frames, modifiers, LOD clustering per doc 20) deferred to a follow-on story; spike proves DTO + resolver contract only.
- **Cesium icon wiring** (`CesiumGlobeBridge`) can consume `MapSymbolEntry.App6Sidc` when Phase C continues — comment already anticipates this field.

### Recommendation

Proceed with Phase C incremental delivery: expand `App6Sidc` lookup table, add Unity USS/icon atlas asset pack, keep projection read-only and `DelegationBridge`-free per ADR-010.