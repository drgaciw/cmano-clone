# Sprint 32 — S32-10 Live Editor Presentation Evidence Closeout

**Date:** 2026-06-19  
**Story:** S32-10 (`production/epics/sprint-32-platform-editor-phase-f/story-032-10-presentation-evidence.md`)  
**Branch:** `stack/sprint32/presentation-evidence` (evidence-only)  
**ADR:** ADR-010 (headless-first; panel host seams), ADR-011 (platform import write-gate)  
**Replaces / extends:** S31-07 protocol placeholder PNGs (`*-s31-*.png`) with S32 Phase F damage + import staging evidence  
**Environment:** Headless Linux CI/agent host — Unity 6.3 Editor unavailable; evidence via S32 protocol placeholder PNGs + headless proxy tests (lean mode).

## Verdict

**APPROVED WITH CONDITIONS (lean PASS WITH NOTES)** — S32 Phase F presentation evidence package (protocol placeholder PNGs + signoff script scenarios verified + headless proxy log). Headless regression **47/47 PASS** (`PlatformImport|Doctrine|C2TopBar|PlayModeSmoke|PlatformCatalogViewer`); per-filter **PlatformImport 10/10**, **Doctrine 7/7**, **C2TopBar 5/5**, **PlayModeSmoke 17/17**, **PlatformCatalogViewer 11/11**. Merge authority remains headless gates per ADR-010 lean mode. Live Editor re-capture optional polish before Production → Polish gate.

## Acceptance criteria status

| AC | Status | Evidence |
|----|--------|----------|
| `*-s32-*.png` — damage viewer + import staging captures | **PASS (protocol placeholder)** | `platform-catalog-damage-s32-viewer-columns.png`, `platform-import-staging-s32-baltic-diff.png` |
| Evidence doc maps S31 placeholders → S32 | **PASS** | This document + `README-presentation-evidence-s32.md` |
| Headless regression unchanged PASS (≥35/35) | **PASS** | §Gates below — **47/47** combined filter |
| Signoff script scenarios (`import`, `doctrine`) | **PASS** | `-Scenario import` + `-Scenario doctrine` in `Invoke-C2PlayModeSignoffBatch.ps1`; `RunImportBatch` / `RunDoctrineBatch` in runner |
| Lean PASS WITH NOTES (no Unity Editor host) | **PASS** | Headless Linux agent; protocol placeholder PNGs per S27-10/S31-07 pattern |
| ZERO touch `DelegationBridge.cs` | **PASS** | Empty diff vs `HEAD` |

## S31 → S32 replacement map

| S31-07 placeholder | S32-10 replacement | Clears / purpose |
|--------------------|--------------------|------------------|
| *(none — damage viewer new)* | `platform-catalog-damage-s32-viewer-columns.png` | S32-06 Phase F damage list/detail columns |
| `platform-import-staging-s31-baltic-diff.png` | `platform-import-staging-s32-baltic-diff.png` | S32-06 MaxHp `DAMAGE row=…` staging diff |
| `doctrine-panel-s31-roe-override.png` | *(unchanged — valid S31 fallback)* | S29-07 advisory (not in S32-10 scope) |
| `begin-execution-s31-planning-topbar.png` | *(unchanged — valid S31 fallback)* | S29-08 advisory (not in S32-10 scope) |

## Protocol execution

### Platform catalog damage viewer (S32-06 Phase F)

**Protocol steps (S32-06 §Manual advisory):**

1. Open `unity/ProjectAegis`; load `DelegationSmoke` with `PlatformCatalogViewerHost`.
2. Ensure `PlatformCatalogPanel.uxml` bound (`platform-catalog-detail-hp`, `platform-catalog-detail-resilience`, `platform-catalog-detail-withdraw`, `platform-catalog-detail-flags`).
3. Enter Play Mode with Baltic fixture; browse platform list.
4. **Expect:** list projection shows damage workbook columns; detail panel shows MaxHp / resilience / withdraw threshold / critical flags.
5. Capture `Game` view 1920×1080.

**S32-10 evidence:** `production/qa/evidence/platform-catalog-damage-s32-viewer-columns.png` (labeled protocol placeholder, 1920×1080).  
**Headless proxy:** `Baltic_fixture_damage_row_surfaces_workbook_values_in_list_and_detail`; `Platform_catalog_viewer_host_element_names_are_stable`; `Viewer_projection_path_has_no_write_gate_types`.

### Platform import staging MaxHp diff (replaces S31-07)

**Protocol steps (S32-06 §Manual advisory):**

1. Open `DelegationSmoke`; confirm `PlatformImportPanelHost` wired to `PlatformWorkbookWriteBridge`.
2. Enter Play Mode with Baltic fixture; propose edited workbook with `MaxHp` cell change (e.g. `u1` → `120`).
3. **Expect:** staging diff list shows `DAMAGE row=…` MaxHp delta; approve disabled until acknowledge.
4. Acknowledge → approve → read-back confirms `MaxHp` in catalog browse projection.
5. Capture `Game` view 1920×1080.

**S32-10 evidence:** `production/qa/evidence/platform-import-staging-s32-baltic-diff.png` (labeled protocol placeholder, 1920×1080).  
**Headless proxy:** `Import_damage_MaxHp_round_trip_propose_acknowledge_approve_readback_baltic_fixture`; `Import_round_trip_propose_acknowledge_approve_readback_baltic_fixture`; `Delegation_smoke_scene_builder_includes_platform_import_panel`.

## Signoff batch extension (S30-06 — verified S32-10)

| Scenario | PS1 flag | Execute method | Policy id |
|----------|----------|----------------|-----------|
| Import | `-Scenario import` | `RunImportBatch` | `baltic-patrol-classify` |
| Doctrine | `-Scenario doctrine` | `RunDoctrineBatch` | `baltic-patrol-mission-roe` |
| Begin Execution | `-Scenario begin-execution` | `RunBeginExecutionBatch` | `baltic-patrol-classify` |

Documented in `production/qa/evidence/README-presentation-evidence-s32.md` and `unity/ProjectAegis/PLAYMODE-SMOKE.md`.

`Invoke-C2PlayModeSignoffBatch.ps1` import scenario mapping (verified, no script changes required):

```powershell
# ValidateSet includes "import"
# switch maps: "import" -> RunImportBatch
pwsh tools/unity/Invoke-C2PlayModeSignoffBatch.ps1 -Scenario import -SkipBuild
```

## Headless regression (CI merge authority)

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone

dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "PlatformImport|Doctrine|C2TopBar|PlayModeSmoke|PlatformCatalogViewer" -v minimal

dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "PlatformImport" -v minimal

dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "PlatformCatalogViewer" -v minimal

git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
ls production/qa/evidence/*-s32-*.png
```

## Gates run (agent session 2026-06-19)

| Gate | Result |
|------|--------|
| `PlatformImport\|Doctrine\|C2TopBar\|PlayModeSmoke\|PlatformCatalogViewer` filter | **47/47 PASS** |
| `PlatformImport` only | **10/10 PASS** |
| `Doctrine` only | **7/7 PASS** |
| `C2TopBar` only | **5/5 PASS** |
| `PlayModeSmoke` only | **17/17 PASS** |
| `PlatformCatalogViewer` only | **11/11 PASS** |
| `DelegationBridge.cs` diff vs `HEAD` | **ZERO touch** (empty diff) |
| `platform-catalog-damage-s32-*.png` present | 1 file in `production/qa/evidence/` |
| `platform-import-staging-s32-*.png` present | 1 file in `production/qa/evidence/` |
| Signoff script `-Scenario import` | **PASS** (ValidateSet + switch mapping) |
| Signoff script `-Scenario doctrine` | **PASS** (ValidateSet + switch mapping) |

## Headless proxy test inventory

| Test | Clears / purpose |
|------|------------------|
| `Import_damage_MaxHp_round_trip_propose_acknowledge_approve_readback_baltic_fixture` | S32-06 MaxHp staging diff + approve |
| `Import_round_trip_propose_acknowledge_approve_readback_baltic_fixture` | S29-04 staging round-trip |
| `Import_unedited_round_trip_produces_empty_diff_golden` | S29-04 empty-diff golden |
| `Baltic_fixture_damage_row_surfaces_workbook_values_in_list_and_detail` | S32-06 damage viewer columns |
| `Viewer_host_detail_bind_path_uses_browse_row_projection` | S32-06 detail bind path |
| `Platform_catalog_viewer_host_element_names_are_stable` | UXML/host contract |
| `Viewer_projection_path_has_no_write_gate_types` | ADR-011 read-only viewer |
| `Delegation_smoke_scene_builder_includes_platform_catalog_viewer` | Scene wiring |
| `Doctrine_override_round_trip_updates_policy_log_and_projection_bind` | S29-07 ROE override |
| `BeginExecution_transitions_planning_to_executing_via_bridge` | S29-08 phase transition |
| `Delegation_smoke_keeps_useGlobeMap_false_for_ci_safe_default` | CI-safe default |

## Advisory notes (lean mode)

- PNGs are labeled protocol placeholders (headless host; Unity Editor unavailable) — satisfies S32-10 AC per lean mode, matching S26-07 / S27-10 / S31-07 pattern.
- Live Editor re-capture does not block headless merge; optional polish before Production → Polish gate.
- Signoff batch scenarios (`import`, `doctrine`) documented for local Unity host; not executed on Linux agent.
- `DelegationSmoke.unity` keeps `useGlobeMap: 0` — CI-safe default preserved.
- S31-07 placeholders for doctrine/begin-execution remain valid historical fallback; S32-10 artifacts focus on Phase F damage + import staging per story AC.

## Conditions for full closeout (non-blocking merge)

1. Capture live Editor `platform-catalog-damage-s32-*.png` and `platform-import-staging-s32-*.png` on Windows/macOS Unity host (replace protocol placeholder labels).
2. Run `Invoke-C2PlayModeSignoffBatch.ps1 -Scenario import`; archive `unity-c2-playmode-signoff.log`.
3. Verify platform catalog damage columns and import staging MaxHp diff UX with full Baltic catalog dataset in Editor.

## Architecture compliance

- [x] **ZERO edits** to `src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs`
- [x] Platform import routes through `PlatformWorkbookWriteBridge` + `PlatformImportStagingProjection` (ADR-011)
- [x] Platform catalog viewer remains read-only via `PlatformCatalogViewerHost` (ADR-011)
- [x] S31-07 placeholders superseded for import staging; S32-06 damage viewer evidence added
- [ ] Live Editor captures (advisory — pending local Unity host)