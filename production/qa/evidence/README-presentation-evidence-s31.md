# Sprint 31 Editor Presentation Evidence (S31-07)

**Status:** **PASS (protocol placeholders)** — headless Linux CI/agent host; Unity 6.3 Editor unavailable.  
**QA verdict:** See `production/qa/sprint-31-presentation-evidence-2026-06-18.md`  
**Protocol source:** S30-06 presentation evidence refresh; S29-04 platform import staging, S29-07 doctrine panel, S29-08 Begin Execution  
**Automated proxy:** `PlatformImport|Doctrine|C2TopBar|PlayModeSmoke` filter **35/35 PASS** (merge authority per ADR-010 lean mode)

## Attached captures

| File | Scene / view | Replaces (S30 → S31) |
|------|----------------|----------------------|
| `platform-import-staging-s31-baltic-diff.png` | `PlatformImportPanel.uxml` — Baltic staging diff + acknowledge/approve | `platform-import-staging-s30-baltic-diff.png` |
| `doctrine-panel-s31-roe-override.png` | `DoctrineInheritancePanel.uxml` — ROE override read-back | `doctrine-panel-s30-roe-override.png` |
| `begin-execution-s31-planning-topbar.png` | `C2TopBarPanel.uxml` — Begin Execution while Planning | `begin-execution-s30-planning-topbar.png` |

All files are **1920×1080 labeled protocol placeholders** generated on the headless agent host (2026-06-18). Each image documents the corresponding S30 protocol step and expected outcome. They satisfy the S31-07 attachment requirement; live Editor re-capture is optional polish.

**Paths:**

- Primary: `production/qa/evidence/platform-import-staging-s31-*.png`, `doctrine-panel-s31-*.png`, `begin-execution-s31-*.png`

## Signoff batch scenarios (S30-06 extension — unchanged S31-07)

`Invoke-C2PlayModeSignoffBatch.ps1` accepts `-Scenario import` and `-Scenario begin-execution` (in addition to comms/classify/doctrine):

```powershell
pwsh tools/unity/Invoke-C2PlayModeSignoffBatch.ps1 -Scenario import -SkipBuild
pwsh tools/unity/Invoke-C2PlayModeSignoffBatch.ps1 -Scenario begin-execution -SkipBuild
pwsh tools/unity/Invoke-C2PlayModeSignoffBatch.ps1 -Scenario doctrine -SkipBuild
```

| Scenario | `-executeMethod` | Policy id |
|----------|------------------|-----------|
| `import` | `C2PlayModeSignoffBatchRunner.RunImportBatch` | `baltic-patrol-classify` |
| `begin-execution` | `C2PlayModeSignoffBatchRunner.RunBeginExecutionBatch` | `baltic-patrol-classify` |
| `doctrine` | `C2PlayModeSignoffBatchRunner.RunDoctrineBatch` | `baltic-patrol-mission-roe` |

Evidence when run locally: `unity-c2-playmode-signoff.log` with `C2PlayModeSignoffBatchRunner PASS` and zero `SIGNOFF_ERROR`.

## Capture steps (Unity 6.3 Editor — when unblocked)

### Platform import staging (replaces S30-06 placeholder)

1. Open `unity/ProjectAegis` in Unity **6000.3.x** LTS.
2. Load `Assets/Scenes/DelegationSmoke.unity` — confirm `PlatformImport` host present.
3. Ensure `PlatformImportPanel.uxml` + `.uss` bound (`platform-import-root`, `platform-import-diff-list`, `platform-import-acknowledge`, `platform-import-approve`).
4. Enter Play Mode; propose edited Baltic workbook via `PlatformWorkbookWriteBridge`.
5. Verify diff preview / staging list visible; approve disabled until acknowledge.
6. `Game` view → capture at 1920×1080; replace `platform-import-staging-s31-*.png`.

### Doctrine panel (replaces S30-06 placeholder)

1. Open `DelegationSmoke`; confirm `DoctrineInheritance` host wired to `DelegationBridgeHost`.
2. Enter Play Mode on `baltic-patrol-mission-roe`; select friendly unit `u1`.
3. Confirm UNIT/ROE/SALVO/EMCON/SOURCE labels and override controls visible.
4. Change ROE dropdown → **Apply** → labels refresh; console clean.
5. `Game` view → capture at 1920×1080; replace `doctrine-panel-s31-*.png`.

### Begin Execution (replaces S30-06 placeholder)

1. Open `DelegationSmoke`; load scenario in `SimulationPhase.Planning`.
2. Ensure `C2TopBarPanel.uxml` exposes `begin-execution-button`.
3. Verify score line frozen (`SCORE: {base}  KILLS: 0  MSLS: 0`) until execution.
4. Click **Begin Execution** → phase transitions; button hidden/disabled after.
5. `Game` view → capture at 1920×1080; replace `begin-execution-s31-*.png`.

## CI default (unchanged)

`DelegationSmoke.unity` keeps `useGlobeMap=false` — globe off in automated PlayMode; headless proxy tests are merge authority.

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone

dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "PlatformImport|Doctrine|C2TopBar|PlayModeSmoke" -v minimal

git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
# Expected: empty diff (ZERO touch)
```

## Headless proxy tests

| Test | Purpose |
|------|---------|
| `Import_round_trip_propose_acknowledge_approve_readback_baltic_fixture` | S29-04 staging round-trip |
| `Import_unedited_round_trip_produces_empty_diff_golden` | S29-04 empty-diff golden |
| `Delegation_smoke_scene_builder_includes_platform_import_panel` | Scene wiring |
| `Doctrine_override_round_trip_updates_policy_log_and_projection_bind` | S29-07 ROE override |
| `Doctrine_smoke_scene_builder_registers_doctrine_panel_host` | Scene wiring |
| `BeginExecution_transitions_planning_to_executing_via_bridge` | S29-08 phase transition |
| `Planning_top_bar_projection_freezes_score_until_execution` | S29-08 score freeze |
| `C2_top_bar_panel_wires_begin_execution_button_to_bridge_host` | UXML/host wiring |
| `Delegation_smoke_keeps_useGlobeMap_false_for_ci_safe_default` | CI-safe default |

## S30 → S31 replacement map

| S30-06 placeholder | S31-07 replacement | Headless proxy |
|--------------------|--------------------|----------------|
| `platform-import-staging-s30-baltic-diff.png` | `platform-import-staging-s31-baltic-diff.png` | `PlatformImportPanelTests` 9/9 |
| `doctrine-panel-s30-roe-override.png` | `doctrine-panel-s31-roe-override.png` | `Doctrine*` 7/7 |
| `begin-execution-s30-planning-topbar.png` | `begin-execution-s31-planning-topbar.png` | `C2TopBarBeginExecutionTests` 5/5 |

## Related evidence

- `production/qa/sprint-31-presentation-evidence-2026-06-18.md` — S31-07 full closeout evidence
- `production/qa/sprint-30-presentation-evidence-2026-06-18.md` — S30-06 predecessor evidence
- `production/qa/evidence/README-presentation-evidence-s30.md` — S30 protocol README
- `production/qa/sprint-27-presentation-evidence-2026-06-18.md` — S27-10 pattern reference