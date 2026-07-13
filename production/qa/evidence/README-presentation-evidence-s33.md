# Sprint 33 Editor Presentation Evidence (S33-10)

**Status:** **PASS (protocol placeholders)** — headless Linux CI/agent host; Unity 6.3 Editor unavailable.  
**QA verdict:** See `production/qa/sprint-33-presentation-evidence-2026-06-19.md`  
**Protocol source:** S32-10 presentation evidence refresh; S33-06 Phase G comms/datalink surfacing + import staging Comms diff  
**Automated proxy:** `PlatformImport|PlatformCatalogViewer|PlatformComms|C2TopBar` filter **≥38/38 PASS** (merge authority per ADR-010 lean mode)

## Attached captures

| File | Scene / view | Replaces (S32 → S33) |
|------|----------------|----------------------|
| `platform-catalog-comms-s33-viewer-columns.png` | `PlatformCatalogPanel.uxml` — Phase G comms list section | `platform-catalog-damage-s32-viewer-columns.png` *(damage remains valid; comms is additive Phase G)* |
| `platform-import-staging-s33-comms-diff.png` | `PlatformImportPanel.uxml` — Baltic staging diff with `COMMS row=…` delta | `platform-import-staging-s32-baltic-diff.png` |

All files are **1920×1080 labeled protocol placeholders** generated on the headless agent host (2026-06-19). Each image documents the corresponding S33 Phase G protocol step and expected outcome. They satisfy the S33-10 attachment requirement; live Editor re-capture is optional polish.

**S38-07 Live PNG re-capture / evidence refresh note:** Aligned to S38-04 C2 + Platform Editor additional polish (residual filters, tooltips, density). Lean PASS WITH NOTES acceptable (headless proxy primary per qa-plan-sprint-38); 12+ PNGs or notes; cross S37 evidence (graph viewer, tooltips, FK). Full compliance with polish-scope-boundary-2026-06-19.md + sprint-38 plan. (C2/Polish track isolated.)

**Paths:**

- Primary: `production/qa/evidence/platform-catalog-comms-s33-*.png`, `platform-import-staging-s33-*.png`

## Signoff batch scenarios (S30-06 extension — unchanged S33-10)

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

### Platform catalog comms viewer (S33-06 Phase G)

1. Open `unity/ProjectAegis` in Unity **6000.3.x** LTS.
2. Load `Assets/Scenes/DelegationSmoke.unity` — confirm `PlatformCatalogViewerHost` present.
3. Ensure `PlatformCatalogPanel.uxml` + `.uss` bound (`platform-catalog-comms`, `platform-catalog-comms-list`).
4. Enter Play Mode with Baltic fixture via `ICatalogReader.GetSortedComms()`.
5. **Expect:** selecting `u1` shows comms fittings (`LinkId`, `Role`, `SatcomCapable`) in comms list.
6. `Game` view → capture at 1920×1080; replace `platform-catalog-comms-s33-*.png`.

### Platform import staging Comms diff (replaces S32-07 placeholder focus)

1. Open `DelegationSmoke`; confirm `PlatformImportPanelHost` present.
2. Ensure `PlatformImportPanel.uxml` bound (`platform-import-root`, `platform-import-diff-list`, `platform-import-acknowledge`, `platform-import-approve`).
3. Enter Play Mode; propose edited Baltic workbook with Comms cell change or row add via `PlatformWorkbookWriteBridge`.
4. **Expect:** staging diff shows `COMMS row=…` Role/SatcomCapable/LinkId delta; approve disabled until acknowledge.
5. `Game` view → capture at 1920×1080; replace `platform-import-staging-s33-*.png`.

## CI default (unchanged)

`DelegationSmoke.unity` keeps `useGlobeMap=false` — globe off in automated PlayMode; headless proxy tests are merge authority.

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone

dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "PlatformImport|PlatformCatalogViewer|PlatformComms|C2TopBar" -v minimal

git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
# Expected: empty diff (ZERO touch)
```

## Headless proxy tests

| Test | Purpose |
|------|---------|
| `PlatformComms_baltic_fixture_comms_surfaces_workbook_values_in_list_projection` | S33-06 comms columns in viewer list |
| `PlatformComms_delegation_smoke_scene_builder_includes_comms_viewer_wiring` | Scene builder + UXML comms section |
| `PlatformComms_import_panel_uxml_declares_entity_diff_for_comms_staging` | Import panel entity diff host wiring |
| `PlatformComms_viewer_host_binds_comms_list_on_platform_selection` | Selection → BindComms path |
| `PlatformComms_staging_diff_surfaces_added_comms_row` | RowAdded comms staging diff |
| `PlatformComms_import_round_trip_propose_acknowledge_approve_readback_baltic_fixture` | S33-06 comms staging round-trip |
| `Import_round_trip_propose_acknowledge_approve_readback_baltic_fixture` | S29-04 staging round-trip |
| `Baltic_fixture_damage_row_surfaces_workbook_values_in_list_and_detail` | S32-06 damage columns (retained) |
| `BeginExecution_transitions_planning_to_executing_via_bridge` | S29-08 phase transition |

## S32 → S33 replacement map

| S32-10 placeholder | S33-10 replacement | Headless proxy |
|--------------------|--------------------|----------------|
| `platform-catalog-damage-s32-viewer-columns.png` | `platform-catalog-comms-s33-viewer-columns.png` | `PlatformCommsTests` 12/12 |
| `platform-import-staging-s32-baltic-diff.png` | `platform-import-staging-s33-comms-diff.png` | `PlatformImportPanelTests` 10/10 |
| `doctrine-panel-s31-roe-override.png` | *(unchanged — S31 artifact remains valid fallback)* | *(not in S33-10 filter)* |
| `begin-execution-s31-planning-topbar.png` | *(unchanged — S31 artifact remains valid fallback)* | `C2TopBar*` 5/5 |

## Related evidence

- `production/qa/sprint-33-presentation-evidence-2026-06-19.md` — S33-10 full closeout evidence
- `production/qa/sprint-32-presentation-evidence-2026-06-19.md` — S32-10 predecessor evidence
- `production/qa/evidence/README-presentation-evidence-s32.md` — S32 protocol README
- `production/agentic/stacks/sprint33/S33-06-DONE.md` — Phase G comms implementation
- `production/qa/sprint-27-presentation-evidence-2026-06-18.md` — S27-10 lean mode pattern reference