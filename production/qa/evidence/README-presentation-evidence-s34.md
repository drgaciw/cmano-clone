# Sprint 34 Editor Presentation Evidence (S34-10)

**Status:** **PASS (protocol placeholders)** — headless Linux CI/agent host; Unity 6.3 Editor unavailable.  
**QA verdict:** See `production/qa/sprint-34-presentation-evidence-2026-06-19.md`  
**Protocol source:** S33-10 presentation evidence refresh; S34-06 Phase H LinkCatalog surfacing + import staging LINK diff  
**Automated proxy:** `PlatformImport|PlatformCatalogViewer|PlatformComms|PlatformLinkCatalog|C2TopBar` filter **≥48/48 PASS** (merge authority per ADR-010 lean mode)

## Attached captures

| File | Scene / view | Replaces (S33 → S34) |
|------|----------------|----------------------|
| `platform-catalog-link-s34-viewer-columns.png` | `PlatformCatalogPanel.uxml` — Phase H link catalog list section | `platform-catalog-comms-s33-viewer-columns.png` *(comms remains valid; link catalog is additive Phase H)* |
| `platform-import-staging-s34-link-diff.png` | `PlatformImportPanel.uxml` — Baltic staging diff with `LINK row=…` delta | `platform-import-staging-s33-comms-diff.png` |

All files are **1920×1080 labeled protocol placeholders** generated on the headless agent host (2026-06-19). Each image documents the corresponding S34 Phase H protocol step and expected outcome. They satisfy the S34-10 attachment requirement; live Editor re-capture is optional polish.

**Paths:**

- Primary: `production/qa/evidence/platform-catalog-link-s34-*.png`, `platform-import-staging-s34-*.png`

## Signoff batch scenarios (S30-06 extension — unchanged S34-10)

`Invoke-C2PlayModeSignoffBatch.ps1` accepts `-Scenario import` and `-Scenario doctrine` (in addition to comms/classify/begin-execution):

```powershell
pwsh tools/unity/Invoke-C2PlayModeSignoffBatch.ps1 -Scenario import -SkipBuild
pwsh tools/unity/Invoke-C2PlayModeSignoffBatch.ps1 -Scenario doctrine -SkipBuild
```

| Scenario | `-executeMethod` | Policy id |
|----------|------------------|-----------|
| `import` | `C2PlayModeSignoffBatchRunner.RunImportBatch` | `baltic-patrol-classify` |
| `doctrine` | `C2PlayModeSignoffBatchRunner.RunDoctrineBatch` | `baltic-patrol-mission-roe` |
| `begin-execution` | `C2PlayModeSignoffBatchRunner.RunBeginExecutionBatch` | `baltic-patrol-classify` |

Evidence when run locally: `unity-c2-playmode-signoff.log` with `C2PlayModeSignoffBatchRunner PASS` and zero `SIGNOFF_ERROR`.

## Capture steps (Unity 6.3 Editor — when unblocked)

### Platform catalog LinkCatalog viewer (S34-06 Phase H)

1. Open `unity/ProjectAegis` in Unity **6000.3.x** LTS.
2. Load `Assets/Scenes/DelegationSmoke.unity` — confirm `PlatformCatalogViewerHost` present.
3. Ensure `PlatformCatalogPanel.uxml` + `.uss` bound (`platform-catalog-links`, `platform-catalog-links-list`).
4. Enter Play Mode with Baltic fixture via `ICatalogReader.GetSortedLinks()`.
5. **Expect:** global link list shows `NATO_TADIL_J` / `SATCOM_B` with `DisplayName`, `LinkType`, `LatencyMsNominal` columns; comms list resolves link display names.
6. `Game` view → capture at 1920×1080; replace `platform-catalog-link-s34-*.png`.

### Platform import staging LinkCatalog diff (replaces S33-07 placeholder focus)

1. Open `DelegationSmoke`; confirm `PlatformImportPanelHost` present.
2. Ensure `PlatformImportPanel.uxml` bound (`platform-import-root`, `platform-import-diff-list`, `platform-import-acknowledge`, `platform-import-approve`).
3. Enter Play Mode; propose edited Baltic workbook with LinkCatalog cell change or row add via `PlatformWorkbookWriteBridge`.
4. **Expect:** staging diff shows `LINK row=…` DisplayName/LinkType/LatencyMsNominal delta; approve disabled until acknowledge.
5. `Game` view → capture at 1920×1080; replace `platform-import-staging-s34-*.png`.

## CI default (unchanged)

`DelegationSmoke.unity` keeps `useGlobeMap=false` — globe off in automated PlayMode; headless proxy tests are merge authority.

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone

dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "PlatformImport|PlatformCatalogViewer|PlatformComms|PlatformLinkCatalog|C2TopBar" -v minimal

git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
# Expected: empty diff (ZERO touch)
```

## Headless proxy tests

| Test | Purpose |
|------|---------|
| `PlatformLinkCatalog_baltic_fixture_links_surface_workbook_values_in_list_projection` | S34-06 link catalog columns in viewer list |
| `PlatformLinkCatalog_delegation_smoke_scene_builder_includes_link_viewer_wiring` | Scene builder + UXML link section |
| `PlatformLinkCatalog_import_panel_uxml_declares_entity_diff_for_link_staging` | Import panel entity diff host wiring |
| `PlatformLinkCatalog_viewer_host_binds_global_link_list_on_refresh` | Refresh → BindLinks path |
| `PlatformLinkCatalog_staging_diff_surfaces_added_link_row` | RowAdded link staging diff |
| `PlatformLinkCatalog_import_round_trip_propose_acknowledge_approve_readback_baltic_fixture` | S34-06 link staging round-trip |
| `PlatformLinkCatalog_comms_rows_resolve_link_display_name_when_present` | Comms display-name resolution |
| `PlatformComms_baltic_fixture_comms_surfaces_workbook_values_in_list_projection` | S33-06 comms columns (retained) |
| `Import_round_trip_propose_acknowledge_approve_readback_baltic_fixture` | S29-04 staging round-trip |
| `BeginExecution_transitions_planning_to_executing_via_bridge` | S29-08 phase transition |

## S33 → S34 replacement map

| S33-10 placeholder | S34-10 replacement | Headless proxy |
|--------------------|--------------------|----------------|
| `platform-catalog-comms-s33-viewer-columns.png` | `platform-catalog-link-s34-viewer-columns.png` | `PlatformLinkCatalogTests` 13/13 |
| `platform-import-staging-s33-comms-diff.png` | `platform-import-staging-s34-link-diff.png` | `PlatformImportPanelTests` 10/10 |
| `doctrine-panel-s31-roe-override.png` | *(unchanged — S31 artifact remains valid fallback)* | *(not in S34-10 filter)* |
| `begin-execution-s31-planning-topbar.png` | *(unchanged — S31 artifact remains valid fallback)* | `C2TopBar*` 5/5 |

## Related evidence

- `production/qa/sprint-34-presentation-evidence-2026-06-19.md` — S34-10 full closeout evidence
- `production/qa/sprint-33-presentation-evidence-2026-06-19.md` — S33-10 predecessor evidence
- `production/qa/evidence/README-presentation-evidence-s33.md` — S33 protocol README
- `production/agentic/sprint-34-platform-phase-h-link-catalog-2026-06-19.md` — Phase H LinkCatalog implementation
- `production/qa/sprint-27-presentation-evidence-2026-06-18.md` — S27-10 lean mode pattern reference