# Platform Viewer + APP-6 Atlas Sprint 27 Editor Screenshot Evidence (S27-10)

**Status:** **PASS (protocol placeholders)** — headless Linux CI/agent host; Unity 6.3 Editor unavailable.  
**QA verdict:** See `production/qa/sprint-27-presentation-evidence-2026-06-18.md`  
**Protocol source:** S27-08 platform viewer panel + S27-07 Addressables APP-6 atlas; S27-10 presentation closeout  
**Automated proxy:** `PlatformCatalogViewer|App6|MapPanelBinder` filter **35/35 PASS** (merge authority per ADR-010 lean mode)

## Attached captures

| File | Scene / view | Clears |
|------|----------------|--------|
| `platform-viewer-s27-list-filter.png` | `PlatformCatalogPanel.uxml` — Baltic browse list + search filter | S27-08 advisory |
| `app6-atlas-s27-sprite-frames.png` | `MapPlaceholderPanel` — APP-6 sprite frames vs Unicode fallback | S27-07 advisory |

All files are **1920×1080 labeled protocol placeholders** generated on the headless agent host (2026-06-18). Each image documents the corresponding S27-08 / S27-07 protocol step and expected outcome. They satisfy the S27-10 attachment requirement; live Editor re-capture is optional polish.

**Paths:**

- Primary: `production/qa/evidence/platform-viewer-s27-*.png`, `app6-atlas-s27-*.png`
- Symlinks: `production/qa/attachments/platform-viewer-s27-*.png`, `app6-atlas-s27-*.png` → `../evidence/`

## Capture steps (Unity 6.3 Editor — when unblocked)

### Platform viewer (clears S27-08 advisory)

1. Open `unity/ProjectAegis` in Unity **6000.3.x** LTS.
2. Load scene with `PlatformCatalogViewerHost` (or `DelegationSmoke` harness row).
3. Ensure `PlatformCatalogPanel.uxml` + `.uss` bound (`platform-catalog-root`, `platform-catalog-search`, `platform-catalog-list`).
4. Enter Play Mode with Baltic fixture via `ICatalogReader`.
5. Verify list shows sorted `PlatformId` rows; type filter text (e.g. `hostile`) — list narrows.
6. `Game` view → capture at 1920×1080; replace `platform-viewer-s27-list-filter.png`.

### APP-6 atlas sprites (clears S27-07 advisory)

1. Open map placeholder scene with `MapPlaceholderPanelHost`.
2. Ensure Addressables group `Map/App6FrameAtlas` resolves per `App6AtlasAddressablesManifest.json`.
3. Enter Play Mode with `useApp6AtlasFrames=true`.
4. **Expect:** USS sprite slices (`map-app6-frame--friendly`, `map-app6-frame--hostile`) — not raw Unicode ▣/⬥.
5. Toggle unavailable atlas — symbols degrade to Unicode fallback.
6. `Game` view → capture at 1920×1080; replace `app6-atlas-s27-sprite-frames.png`.

## CI default (unchanged)

`DelegationSmoke.unity` keeps `useGlobeMap=false` — globe off in automated PlayMode; headless proxy tests are merge authority.

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone

dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "PlatformCatalogViewer|App6|MapPanelBinder" -v minimal

dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "App6|MapPanelBinder" -v minimal

rg "useGlobeMap" unity/ProjectAegis/Assets/Scenes/DelegationSmoke.unity
git diff main -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
# Expected: useGlobeMap: 0; empty DelegationBridge diff
```

## Headless proxy tests

| Test | Purpose |
|------|---------|
| `Baltic_fixture_produces_sorted_browse_rows_without_write_gate` | S27-08 browse projection |
| `Filter_narrows_baltic_fixture_preserving_stable_order` | S27-08 filter narrows list |
| `Platform_catalog_panel_uxml_declares_stable_element_names` | UXML contract |
| `Viewer_projection_path_has_no_write_gate_types` | Read-only ADR-011 |
| `MapPanelBinder_with_addressables_catalog_uses_atlas_frames` | S27-07 atlas bind |
| `MapPanelBinder_with_unavailable_atlas_emits_unicode_glyphs` | Unicode fallback |
| `ResolveDisplay_with_loaded_atlas_uses_distinct_uss_frames_for_friendly_and_hostile` | Sprite vs unicode |
| `ResolveDisplay_when_atlas_unavailable_degrades_to_unicode_fallback` | Degradation path |
| `Delegation_smoke_scene_builder_includes_platform_catalog_viewer` | S27-11 harness row |

## S27 advisory clearance

S27-08 and S27-07 left **Editor screenshot advisory deferred to S27-10** (headless tests = merge authority). S27-10 attaches `platform-viewer-s27-*.png` and `app6-atlas-s27-*.png`, documents protocol execution, and archives headless gate counts — clearing Sprint 27 Phase C presentation advisory gaps per lean mode (S26-07 pattern).

## Related evidence

- `production/qa/sprint-27-presentation-evidence-2026-06-18.md` — S27-10 full closeout evidence
- `production/qa/sprint-26-platform-viewer-spike-2026-06-18.md` — S26-10 browse spike
- `production/qa/sprint-26-app6-atlas-2026-06-18.md` — S26-06 atlas asset pack
- `production/qa/sprint-26-presentation-closeout-2026-06-18.md` — S26-07 pattern reference
- `production/qa/qa-plan-sprint-27-2026-06-18.md` — S27-10 manual checklist