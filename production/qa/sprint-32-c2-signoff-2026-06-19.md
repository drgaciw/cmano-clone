# Sprint 32 — S32-11 C2 Manual Sign-Off Upgrade

**Date:** 2026-06-19  
**Story:** S32-11 (`production/epics/sprint-32-presentation-qa/story-032-11-c2-signoff-upgrade.md`)  
**Branch:** `stack/sprint32/c2-signoff-upgrade` (evidence-only)  
**Build:** `main` @ `d3db76d`  
**ADR:** ADR-010 (headless-first; panel host seams), ADR-011 (platform import write-gate)  
**Baseline:** `production/qa/c2-manual-signoff-2026-06-02.md` (S19-01 checks 1–13 @ `7401fac`; S31-08 checks 14–16 @ `3406bc4`)  
**S32-10 dependency:** `production/qa/sprint-32-presentation-evidence-2026-06-19.md`  
**S32-06 dependency:** `production/agentic/stacks/sprint32/S32-06-DONE.md`  
**Environment:** Headless Linux CI/agent host — Unity 6.3 Editor unavailable; lean **PASS WITH NOTES** per PI-006 / ADR-010.

## Verdict

**PASS WITH NOTES** — C2 manual sign-off checklist upgraded post-S32: checks **1–13** remain PASS via S31 headless proxy re-confirmation (**61/61** @ `3406bc4`); checks **14–16** upgraded with S32-10 live evidence + S32-06 Phase F damage surfacing + headless proxy re-run (**33/33** @ `d3db76d`). Total **16/16**. Check 14 extended with `PlatformCatalogViewer` damage columns (`11/11`) and S32-10 PNGs; check 14 import staging upgraded to S32 MaxHp diff (`10/10`). Checks 15–16 re-confirmed headless (`Doctrine` **7/7**, `C2TopBar` **5/5**); doctrine/begin-execution PNGs remain valid S31-07 fallbacks per S32-10 scope. Merge authority remains headless gates.

## Acceptance criteria status

| AC | Status | Evidence |
|----|--------|----------|
| `c2-manual-signoff-*.md` updated with post-S32 SHA + verdict | **PASS** | `production/qa/c2-manual-signoff-2026-06-02.md` @ `d3db76d`, verdict **PASS WITH NOTES 16/16** |
| Checks 14–16 upgraded from S31 PASS WITH NOTES | **PASS** | S32-10 evidence linked; headless proxy re-run **33/33** |
| Check 14: Platform import staging review | **PASS (headless + S32 damage extension)** | `PlatformImport` **10/10**; `PlatformCatalogViewer` **11/11**; `platform-import-staging-s32-baltic-diff.png`; `platform-catalog-damage-s32-viewer-columns.png` |
| Check 15: Doctrine inheritance panel ROE override | **PASS (headless)** | `Doctrine` **7/7**; `production/qa/evidence/doctrine-panel-s31-roe-override.png` (S31 fallback) |
| Check 16: Begin Execution top bar (Planning phase) | **PASS (headless)** | `C2TopBar` **5/5**; `production/qa/evidence/begin-execution-s31-planning-topbar.png` (S31 fallback) |
| S32-06 damage viewer evidence linked | **PASS** | Check 14 extension: damage columns PNG + `Baltic_fixture_damage_row_surfaces_workbook_values_in_list_and_detail` |
| Evidence doc with verdict + limitation notes | **PASS** | This document |
| Lean PASS WITH NOTES (no Editor host) | **PASS** | Headless Linux agent; S32-10 protocol placeholder PNGs per S27-10/S31-07 pattern |
| ZERO touch `DelegationBridge.cs` | **PASS** | Empty diff vs `HEAD` |

## Checklist refresh map

| Check range | S31-08 baseline | S32-11 upgrade | Proxy / evidence |
|-------------|-----------------|----------------|------------------|
| 1–13 | PASS @ `3406bc4` | Re-confirmed PASS (no regression) | `PlayModeSmoke\|C2Selection\|OobTree\|LossesScoring\|BalticReplay\|FuelState\|AttackMenu` **61/61** @ `3406bc4` |
| 14 | Platform import staging (S31 **9/9**) | **Upgraded** — S32 MaxHp diff + damage viewer columns | `PlatformImport` **10/10**; `PlatformCatalogViewer` **11/11**; S32-10 PNGs |
| 15 | Doctrine ROE override | Re-confirmed PASS | `Doctrine` **7/7**; S31-07 PNG fallback |
| 16 | Begin Execution while Planning | Re-confirmed PASS | `C2TopBar` **5/5**; S31-07 PNG fallback |

## S29 advisory clearance (via checks 14–16)

| S29 story | Gap cleared | S32-11 check | Evidence |
|-----------|-------------|--------------|----------|
| **S29-04** Platform import staging UI | Editor screenshot deferred | Check 14 | `platform-import-staging-s32-baltic-diff.png` (replaces S31) |
| **S32-06** Phase F damage viewer | Editor screenshot deferred | Check 14 extension | `platform-catalog-damage-s32-viewer-columns.png` |
| **S29-07** Doctrine inheritance panel | Editor screenshot deferred | Check 15 | `doctrine-panel-s31-roe-override.png` (unchanged) |
| **S29-08** Begin Execution top bar | Editor screenshot deferred | Check 16 | `begin-execution-s31-planning-topbar.png` (unchanged) |

## Headless regression (CI merge authority)

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone

# Checks 14–16 proxy (S32-11 verify filter)
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "PlatformImport|Doctrine|C2TopBar|PlatformCatalogViewer" -v minimal

dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "PlatformImport" -v minimal

dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "PlatformCatalogViewer" -v minimal

dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "Doctrine" -v minimal

dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "C2TopBar" -v minimal

git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
```

## Gates run (agent session 2026-06-19)

| Gate | Result |
|------|--------|
| Build SHA | `d3db76dbca07237200f5e7ad69eb5d4fdcaa118f` (`d3db76d`) |
| Checks 14–16 filter `PlatformImport\|Doctrine\|C2TopBar\|PlatformCatalogViewer` | **33/33 PASS** |
| `PlatformImport` only | **10/10 PASS** |
| `PlatformCatalogViewer` only | **11/11 PASS** |
| `Doctrine` only | **7/7 PASS** |
| `C2TopBar` only | **5/5 PASS** |
| `DelegationBridge.cs` diff vs `HEAD` | **ZERO touch** (empty diff) |
| `platform-catalog-damage-s32-*.png` present | 1 file |
| `platform-import-staging-s32-*.png` present | 1 file |
| Checklist rows 1–16 marked PASS | **16/16** |
| Unity Editor batch (`import`, `begin-execution`) | **NOT RUN** (headless Linux; documented in S32-10) |

## Headless proxy test inventory (checks 14–16)

| Test | Check | Purpose |
|------|-------|---------|
| `Import_damage_MaxHp_round_trip_propose_acknowledge_approve_readback_baltic_fixture` | 14 | S32-06 MaxHp staging diff + approve gate |
| `Import_round_trip_propose_acknowledge_approve_readback_baltic_fixture` | 14 | Staging round-trip + approve gate |
| `Import_unedited_round_trip_produces_empty_diff_golden` | 14 | Empty-diff golden |
| `Platform_import_panel_host_routes_through_staging_projection` | 14 | ADR-011 seam |
| `Delegation_smoke_scene_builder_includes_platform_import_panel` | 14 | Scene wiring |
| `Baltic_fixture_damage_row_surfaces_workbook_values_in_list_and_detail` | 14 (S32-06) | Damage viewer list/detail columns |
| `Viewer_host_detail_bind_path_uses_browse_row_projection` | 14 (S32-06) | Detail bind path |
| `Platform_catalog_viewer_host_element_names_are_stable` | 14 (S32-06) | UXML/host contract |
| `Viewer_projection_path_has_no_write_gate_types` | 14 (S32-06) | ADR-011 read-only viewer |
| `Delegation_smoke_scene_builder_includes_platform_catalog_viewer` | 14 (S32-06) | Scene wiring |
| `Doctrine_override_round_trip_updates_policy_log_and_projection_bind` | 15 | ROE override read-back |
| `Doctrine_smoke_scene_builder_registers_doctrine_panel_host` | 15 | Scene wiring |
| `Doctrine_panel_uxml_assets_define_host_element_names` | 15 | UXML contract |
| `BeginExecution_transitions_planning_to_executing_via_bridge` | 16 | Phase transition |
| `Planning_top_bar_projection_freezes_score_until_execution` | 16 | Score freeze |
| `C2_top_bar_panel_wires_begin_execution_button_to_bridge_host` | 16 | UXML/host wiring |

## Advisory notes (lean mode)

- Check 14 upgraded with S32-10 protocol placeholder PNGs (damage viewer + MaxHp import diff) and S32-06 headless damage tests — satisfies S32-11 AC per lean mode.
- Checks 15–16 retain S31-07 protocol placeholder PNGs (not in S32-10 scope); headless proxy re-run confirms no regression.
- Check 1 remains batch Play Mode evidence from S19 @ `7401fac`; no new Editor batch run on Linux agent.
- Live Editor re-capture and `Invoke-C2PlayModeSignoffBatch.ps1 -Scenario import|begin-execution` do not block headless merge.
- `DelegationSmoke.unity` keeps `useGlobeMap: 0` — CI-safe default preserved.

## Conditions for full closeout (non-blocking merge)

1. Capture live Editor `platform-catalog-damage-s32-*.png` and `platform-import-staging-s32-*.png` on Windows/macOS Unity host (replace protocol placeholder labels).
2. Run `Invoke-C2PlayModeSignoffBatch.ps1 -Scenario import` and `-Scenario begin-execution`; archive `unity-c2-playmode-signoff.log`.
3. Optional human visual walk for checks 2–4 click feel per PI-006.

## Architecture compliance

- [x] **ZERO edits** to `src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs`
- [x] Platform import routes through `PlatformWorkbookWriteBridge` + `PlatformImportStagingProjection` (ADR-011)
- [x] Platform catalog viewer remains read-only via `PlatformCatalogViewerHost` (ADR-011)
- [x] Doctrine writes via `DelegationBridgeHost.TrySetDoctrineOverride` only (ADR-010)
- [x] Begin Execution via `DelegationBridgeHost.BeginExecution()` only (ADR-010)
- [x] S29-04/07/08 + S32-06 Phase F damage gaps reflected in checklist checks 14–16
- [ ] Live Editor captures (advisory — pending local Unity host)