# Sprint 27 — S27-10 Unity Editor Presentation Evidence Closeout

**Date:** 2026-06-18  
**Story:** S27-10 (`production/epics/sprint-27-phase-c-presentation/story-027-10-editor-evidence.md`)  
**Branch:** `stack/sprint27/presentation-evidence` (evidence-only)  
**ADR:** ADR-007 Phase C (APP-6 atlas), ADR-010 (headless-first UI, read-only projections), ADR-011 (read-only Phase C platform viewer)  
**Closes:** S27-08, S27-07 **Editor screenshot advisory** (deferred per lean QA)  
**Environment:** Headless Linux CI/agent host — Unity 6.3 Editor unavailable; evidence via S27 protocol placeholder PNGs + headless proxy tests (lean mode).

## Verdict

**APPROVED WITH CONDITIONS** — S27 Phase C presentation advisory gaps cleared via S27-10 evidence package (protocol placeholder PNGs + headless proxy log). Headless regression **35/35 PASS** (`PlatformCatalogViewer|App6|MapPanelBinder`); per-filter **PlatformCatalogViewer 7/7**, **App6 28/28**, **MapPanelBinder 3/3**. Merge authority remains headless gates per ADR-010 lean mode. Live Editor re-capture optional polish before Production → Polish gate.

## Acceptance criteria status

| AC | Status | Evidence |
|----|--------|----------|
| `platform-viewer-s27-*.png` (list + filter) | **PASS (protocol placeholders)** | `production/qa/evidence/platform-viewer-s27-list-filter.png` |
| `app6-atlas-s27-*.png` (sprite frames vs unicode) | **PASS (protocol placeholders)** | `production/qa/evidence/app6-atlas-s27-sprite-frames.png` |
| Headless regression filters unchanged and documented | **PASS** | §Gates below — **35/35** combined filter |
| Protocol placeholders acceptable per S26-07 pattern | **PASS** | Labeled 1920×1080 PNGs + `README-platform-viewer-s27.md` |
| Clears S27-08 platform viewer screenshot advisory | **PASS** | List + filter placeholder + `PlatformCatalogViewerTests` 7/7 |
| Clears S27-07 APP-6 atlas screenshot advisory | **PASS** | Sprite vs unicode placeholder + `App6\|MapPanelBinder` 28/28 |
| `README-platform-viewer-s27.md` in evidence | **PASS** | `production/qa/evidence/README-platform-viewer-s27.md` |
| ZERO touch `DelegationBridge.cs` | **PASS** | Empty diff vs `main` |
| `useGlobeMap: 0` on DelegationSmoke preserved | **PASS** | `Delegation_smoke_keeps_useGlobeMap_false_for_ci_safe_default` |

## S27 advisory clearance map

| S27 story | S27 verdict | S27 gap | S27-10 clearance |
|-----------|-------------|---------|------------------|
| **S27-08** Platform viewer panel | Complete (lean) | Editor screenshot deferred to S27-10 | `platform-viewer-s27-list-filter.png` + `PlatformCatalogViewerTests` 7/7 |
| **S27-07** Addressables APP-6 atlas | Complete (lean) | Optional atlas sprite screenshot | `app6-atlas-s27-sprite-frames.png` + `App6\|MapPanelBinder` 28/28 |

## Protocol execution

### Platform viewer list + filter (clears S27-08)

**Protocol steps (S27-08 §Manual advisory):**

1. Open `unity/ProjectAegis`; load scene with `PlatformCatalogViewerHost`.
2. Ensure `PlatformCatalogPanel.uxml` + `.uss` bound (`platform-catalog-root`, `platform-catalog-search`, `platform-catalog-list`).
3. Enter Play Mode with Baltic fixture via `ICatalogReader`.
4. **Expect:** Sorted platform browse rows visible in list.
5. Type filter text matching one `PlatformId` (e.g. `hostile`) — list narrows; stable sort preserved.
6. Empty filter shows all rows; no-match filter shows empty list.
7. Capture `Game` view 1920×1080.

**S27-10 evidence:** `production/qa/evidence/platform-viewer-s27-list-filter.png` (labeled protocol placeholder, 1920×1080).  
**Headless proxy:** `Filter_narrows_baltic_fixture_preserving_stable_order`; `Viewer_projection_path_has_no_write_gate_types`; `Platform_catalog_panel_uxml_declares_stable_element_names`.

### APP-6 sprite frames vs Unicode (clears S27-07)

**Protocol steps (S27-07 §Editor protocol):**

1. Open map placeholder scene with `MapPlaceholderPanelHost`.
2. Ensure Addressables group `Map/App6FrameAtlas` resolves per `App6AtlasAddressablesManifest.json`.
3. Enter Play Mode with `useApp6AtlasFrames=true`.
4. **Expect:** USS sprite slices (`map-app6-frame--friendly`, `map-app6-frame--hostile`) — distinct from Unicode ▣/⬥ fallback.
5. With `App6AtlasCatalog.Unavailable` — symbols degrade to Unicode glyphs.
6. Capture `Game` view 1920×1080.

**S27-10 evidence:** `production/qa/evidence/app6-atlas-s27-sprite-frames.png` (labeled protocol placeholder).  
**Headless proxy:** `ResolveDisplay_with_loaded_atlas_uses_distinct_uss_frames_for_friendly_and_hostile`; `MapPanelBinder_with_unavailable_atlas_emits_unicode_glyphs`; `ResolveDisplay_when_atlas_unavailable_degrades_to_unicode_fallback`.

## Headless regression (CI merge authority)

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone

dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "PlatformCatalogViewer|App6|MapPanelBinder" -v minimal

dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "App6|MapPanelBinder" -v minimal

dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "PlatformCatalogViewer" -v minimal

rg "useGlobeMap" unity/ProjectAegis/Assets/Scenes/DelegationSmoke.unity
git diff main -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
ls production/qa/evidence/platform-viewer-s27-*.png production/qa/evidence/app6-atlas-s27-*.png
```

## Gates run (agent session 2026-06-18)

| Gate | Result |
|------|--------|
| `PlatformCatalogViewer\|App6\|MapPanelBinder` filter | **35/35 PASS** |
| `PlatformCatalogViewer` only | **7/7 PASS** |
| `App6` only | **28/28 PASS** |
| `MapPanelBinder` only | **3/3 PASS** |
| `App6\|MapPanelBinder` (S27-07 regression) | **28/28 PASS** |
| `DelegationBridge.cs` diff vs `main` | **ZERO touch** (empty diff) |
| `DelegationSmoke.unity` `useGlobeMap` | `useGlobeMap: 0` |
| `platform-viewer-s27-*.png` present | 1 file in `production/qa/evidence/` |
| `app6-atlas-s27-*.png` present | 1 file in `production/qa/evidence/` |

## Headless proxy test inventory

| Test | Clears / purpose |
|------|------------------|
| `Baltic_fixture_produces_sorted_browse_rows_without_write_gate` | S27-08 browse projection |
| `Filter_narrows_baltic_fixture_preserving_stable_order` | S27-08 filter narrows list |
| `Empty_filter_shows_all_baltic_platforms` | S27-08 empty filter |
| `No_match_filter_shows_empty_list` | S27-08 no-match filter |
| `Platform_catalog_panel_uxml_declares_stable_element_names` | UXML contract |
| `Viewer_projection_path_has_no_write_gate_types` | ADR-011 read-only |
| `MapPanelBinder_with_addressables_catalog_uses_atlas_frames` | S27-07 atlas bind |
| `MapPanelBinder_with_default_atlas_emits_frame_classes_on_projected_symbols` | Atlas frame classes |
| `MapPanelBinder_with_unavailable_atlas_emits_unicode_glyphs` | Unicode fallback |
| `ResolveDisplay_with_loaded_atlas_uses_distinct_uss_frames_for_friendly_and_hostile` | Sprite vs unicode |
| `ResolveDisplay_when_atlas_unavailable_degrades_to_unicode_fallback` | Degradation path |
| `ResolveDisplay_when_frame_missing_degrades_to_unicode_fallback` | Missing frame fallback |
| `Delegation_smoke_scene_builder_includes_platform_catalog_viewer` | S27-11 harness row |

## Advisory notes (lean mode)

- PNGs are labeled protocol placeholders (headless host; Unity Editor unavailable) — satisfies S27-10 AC per lean mode, matching S26-07 pattern.
- Live Editor re-capture does not block headless merge; optional polish before Production → Polish gate.
- `DelegationSmoke.unity` keeps `useGlobeMap: 0` — globe/APP-6 billboards validated in `CesiumSpike.unity` path (S26-07); S27-10 covers map placeholder + platform viewer paths.
- S27-08 completion notes explicitly deferred Editor screenshot to S27-10 — cleared by this evidence package.

## Conditions for full closeout (non-blocking merge)

1. Capture live Editor `platform-viewer-s27-*.png` and `app6-atlas-s27-*.png` on Windows/macOS Unity host (replace protocol placeholder labels).
2. Verify platform viewer filter UX with full Baltic catalog dataset in Editor.
3. Verify APP-6 sprite frames visually distinct at map zoom levels.

## Architecture compliance

- [x] **ZERO edits** to `src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs`
- [x] Platform viewer read-only — no `IWriteGate` / `Propose*` / `ApproveBatch` in viewer path (ADR-011)
- [x] Map/globe projection read-only (ADR-010)
- [x] S27-08/07 advisory conditions cleared via S27-10 evidence package
- [ ] Live Editor captures (advisory — pending local Unity host)