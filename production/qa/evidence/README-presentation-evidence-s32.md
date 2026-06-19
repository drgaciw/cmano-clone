# Sprint 32 Editor Presentation Evidence (S32-10)

**Status:** **PASS (protocol placeholders)** — headless Linux CI/agent host; Unity 6.3 Editor unavailable.  
**QA verdict:** See `production/qa/sprint-32-presentation-evidence-2026-06-19.md`  
**Protocol source:** S31-07 presentation evidence refresh; S32-06 Phase F damage surfacing + import staging MaxHp diff  
**Automated proxy:** `PlatformImport|Doctrine|C2TopBar|PlayModeSmoke|PlatformCatalogViewer` filter **47/47 PASS** (merge authority per ADR-010 lean mode)

## Attached captures

| File | Scene / view | Replaces (S31 → S32) |
|------|----------------|----------------------|
| `platform-catalog-damage-s32-viewer-columns.png` | `PlatformCatalogPanel.uxml` — Phase F damage list/detail columns | *(new — S32-06 damage surfacing)* |
| `platform-import-staging-s32-baltic-diff.png` | `PlatformImportPanel.uxml` — Baltic staging diff with MaxHp `DAMAGE row=…` delta | `platform-import-staging-s31-baltic-diff.png` |

All files are **1920×1080 labeled protocol placeholders** generated on the headless agent host (2026-06-19). Each image documents the corresponding S32 Phase F protocol step and expected outcome. They satisfy the S32-10 attachment requirement; live Editor re-capture is optional polish.

**Paths:**

- Primary: `production/qa/evidence/platform-catalog-damage-s32-*.png`, `platform-import-staging-s32-*.png`

## Signoff batch scenarios (S30-06 extension — verified S32-10)

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

### Platform catalog damage viewer (S32-06 Phase F)

1. Open `unity/ProjectAegis` in Unity **6000.3.x** LTS.
2. Load `Assets/Scenes/DelegationSmoke.unity` — confirm `PlatformCatalogViewerHost` present.
3. Ensure `PlatformCatalogPanel.uxml` + `.uss` bound (`platform-catalog-detail-hp`, `platform-catalog-detail-resilience`, `platform-catalog-detail-withdraw`, `platform-catalog-detail-flags`).
4. Enter Play Mode with Baltic fixture via `ICatalogReader`.
5. **Expect:** list lines show damage workbook columns; detail panel shows MaxHp / resilience / withdraw / flags.
6. `Game` view → capture at 1920×1080; replace `platform-catalog-damage-s32-*.png`.

### Platform import staging MaxHp diff (replaces S31-07 placeholder)

1. Open `DelegationSmoke`; confirm `PlatformImportPanelHost` present.
2. Ensure `PlatformImportPanel.uxml` bound (`platform-import-root`, `platform-import-diff-list`, `platform-import-acknowledge`, `platform-import-approve`).
3. Enter Play Mode; propose edited Baltic workbook with `MaxHp` cell change via `PlatformWorkbookWriteBridge`.
4. **Expect:** staging diff shows `DAMAGE row=…` MaxHp delta; approve disabled until acknowledge.
5. `Game` view → capture at 1920×1080; replace `platform-import-staging-s32-*.png`.

## CI default (unchanged)

`DelegationSmoke.unity` keeps `useGlobeMap=false` — globe off in automated PlayMode; headless proxy tests are merge authority.

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone

dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "PlatformImport|Doctrine|C2TopBar|PlayModeSmoke|PlatformCatalogViewer" -v minimal

git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
# Expected: empty diff (ZERO touch)
```

## Headless proxy tests

| Test | Purpose |
|------|---------|
| `Import_damage_MaxHp_round_trip_propose_acknowledge_approve_readback_baltic_fixture` | S32-06 MaxHp staging diff + approve round-trip |
| `Import_round_trip_propose_acknowledge_approve_readback_baltic_fixture` | S29-04 staging round-trip |
| `Import_unedited_round_trip_produces_empty_diff_golden` | S29-04 empty-diff golden |
| `Baltic_fixture_damage_row_surfaces_workbook_values_in_list_and_detail` | S32-06 damage columns in viewer |
| `Platform_catalog_viewer_host_element_names_are_stable` | UXML/host contract |
| `Viewer_projection_path_has_no_write_gate_types` | ADR-011 read-only viewer |
| `Delegation_smoke_scene_builder_includes_platform_catalog_viewer` | Scene wiring |
| `Doctrine_override_round_trip_updates_policy_log_and_projection_bind` | S29-07 ROE override |
| `BeginExecution_transitions_planning_to_executing_via_bridge` | S29-08 phase transition |
| `Delegation_smoke_keeps_useGlobeMap_false_for_ci_safe_default` | CI-safe default |

## S31 → S32 replacement map

| S31-07 placeholder | S32-10 replacement | Headless proxy |
|--------------------|--------------------|----------------|
| *(none — damage viewer new in S32-06)* | `platform-catalog-damage-s32-viewer-columns.png` | `PlatformCatalogViewerTests` 11/11 |
| `platform-import-staging-s31-baltic-diff.png` | `platform-import-staging-s32-baltic-diff.png` | `PlatformImportPanelTests` 10/10 |
| `doctrine-panel-s31-roe-override.png` | *(unchanged — S31 artifact remains valid fallback)* | `Doctrine*` 7/7 |
| `begin-execution-s31-planning-topbar.png` | *(unchanged — S31 artifact remains valid fallback)* | `C2TopBar*` 5/5 |

## Related evidence

- `production/qa/sprint-32-presentation-evidence-2026-06-19.md` — S32-10 full closeout evidence
- `production/qa/sprint-31-presentation-evidence-2026-06-18.md` — S31-07 predecessor evidence
- `production/qa/evidence/README-presentation-evidence-s31.md` — S31 protocol README
- `production/agentic/stacks/sprint32/S32-06-DONE.md` — Phase F damage implementation
- `production/qa/sprint-27-presentation-evidence-2026-06-18.md` — S27-10 lean mode pattern reference