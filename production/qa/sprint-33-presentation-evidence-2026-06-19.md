# Sprint 33 — S33-10 Live Editor Presentation Evidence Closeout

**Date:** 2026-06-19  
**Story:** S33-10 (`production/epics/sprint-33-platform-editor-phase-g/story-033-10-presentation-evidence.md`)  
**Branch:** `stack/sprint33/presentation-evidence` (evidence-only)  
**ADR:** ADR-010 (headless-first; panel host seams), ADR-011 (platform import write-gate)  
**Replaces / extends:** S32-10 protocol placeholder PNGs (`*-s32-*.png`) with S33 Phase G comms + import staging evidence  
**Environment:** Headless Linux CI/agent host — Unity 6.3 Editor unavailable; evidence via S33 protocol placeholder PNGs + headless proxy tests (lean mode).

## Verdict

**APPROVED WITH CONDITIONS (lean PASS WITH NOTES)** — S33 Phase G presentation evidence package (protocol placeholder PNGs + headless proxy log). Headless regression **38/38 PASS** (`PlatformImport|PlatformCatalogViewer|PlatformComms|C2TopBar`); per-filter **PlatformImport 10/10**, **PlatformCatalogViewer 11/11**, **PlatformComms 12/12**, **C2TopBar 5/5**. Merge authority remains headless gates per ADR-010 lean mode. Live Editor re-capture optional polish before Production → Polish gate.

## Acceptance criteria status

| AC | Status | Evidence |
|----|--------|----------|
| `*-s33-*.png` — comms viewer + import staging captures | **PASS (protocol placeholder)** | `platform-catalog-comms-s33-viewer-columns.png`, `platform-import-staging-s33-comms-diff.png` |
| Evidence doc maps S32 placeholders → S33 | **PASS** | This document + `README-presentation-evidence-s33.md` |
| Headless filter ≥38/38 PASS | **PASS** | §Gates below — **38/38** |
| Lean PASS WITH NOTES (no Unity Editor host) | **PASS** | Headless Linux agent; protocol placeholder PNGs per S27-10/S32-10 pattern |
| ZERO touch `DelegationBridge.cs` | **PASS** | Empty diff vs `HEAD` |

## S32 → S33 replacement map

| S32-10 placeholder | S33-10 replacement | Clears / purpose |
|--------------------|--------------------|------------------|
| `platform-catalog-damage-s32-viewer-columns.png` | `platform-catalog-comms-s33-viewer-columns.png` | S33-06 Phase G comms list section (damage S32 artifact retained as historical) |
| `platform-import-staging-s32-baltic-diff.png` | `platform-import-staging-s33-comms-diff.png` | S33-06 Comms `COMMS row=…` staging diff |
| `doctrine-panel-s31-roe-override.png` | *(unchanged — valid S31 fallback)* | S29-07 advisory (not in S33-10 scope) |
| `begin-execution-s31-planning-topbar.png` | *(unchanged — valid S31 fallback)* | S29-08 advisory (not in S33-10 filter) |

## Protocol execution

### Platform catalog comms viewer (S33-06 Phase G)

**Protocol steps (S33-06 §Manual advisory):**

1. Open `unity/ProjectAegis`; load `DelegationSmoke` with `PlatformCatalogViewerHost`.
2. Ensure `PlatformCatalogPanel.uxml` bound (`platform-catalog-comms`, `platform-catalog-comms-list`).
3. Enter Play Mode with Baltic fixture; select platform `u1`.
4. **Expect:** comms list shows `NATO_TADIL_J` / `SATCOM_B` with `Role` and `SatcomCapable` columns.
5. Capture `Game` view 1920×1080.

**S33-10 evidence:** `production/qa/evidence/platform-catalog-comms-s33-viewer-columns.png` (labeled protocol placeholder, 1920×1080).  
**Headless proxy:** `PlatformComms_baltic_fixture_comms_surfaces_workbook_values_in_list_projection`; `PlatformComms_viewer_host_binds_comms_list_on_platform_selection`; `PlatformComms_delegation_smoke_scene_builder_includes_comms_viewer_wiring`.

### Platform import staging Comms diff (replaces S32 focus)

**Protocol steps (S33-06 §Manual advisory):**

1. Open `DelegationSmoke`; confirm `PlatformImportPanelHost` wired to `PlatformWorkbookWriteBridge`.
2. Enter Play Mode with Baltic fixture; propose edited workbook with Comms cell change or row add.
3. **Expect:** staging diff list shows `COMMS row=…` Role/SatcomCapable/LinkId delta; approve disabled until acknowledge.
4. Acknowledge → approve → read-back confirms comms binding in catalog projection.
5. Capture `Game` view 1920×1080.

**S33-10 evidence:** `production/qa/evidence/platform-import-staging-s33-comms-diff.png` (labeled protocol placeholder, 1920×1080).  
**Headless proxy:** `PlatformComms_import_round_trip_propose_acknowledge_approve_readback_baltic_fixture`; `PlatformComms_staging_diff_surfaces_added_comms_row`; `PlatformComms_import_panel_uxml_declares_entity_diff_for_comms_staging`.

## Headless regression (CI merge authority)

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone

dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "PlatformImport|PlatformCatalogViewer|PlatformComms|C2TopBar" -v minimal

dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "PlatformComms" -v minimal

git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
ls production/qa/evidence/*-s33-*.png
```

## Gates run (agent session 2026-06-19)

| Gate | Result |
|------|--------|
| `PlatformImport\|PlatformCatalogViewer\|PlatformComms\|C2TopBar` filter | **38/38 PASS** |
| `PlatformImport` only | **10/10 PASS** |
| `PlatformCatalogViewer` only | **11/11 PASS** |
| `PlatformComms` only | **12/12 PASS** |
| `C2TopBar` only | **5/5 PASS** |
| `DelegationBridge.cs` diff vs `HEAD` | **ZERO touch** (empty diff) |
| `platform-catalog-comms-s33-*.png` present | 1 file in `production/qa/evidence/` |
| `platform-import-staging-s33-*.png` present | 1 file in `production/qa/evidence/` |

## Headless proxy test inventory (S33-10 additions)

| Test | Clears / purpose |
|------|------------------|
| `PlatformComms_baltic_fixture_comms_surfaces_workbook_values_in_list_projection` | S33-06 comms list projection |
| `PlatformComms_delegation_smoke_scene_builder_includes_comms_viewer_wiring` | Scene builder + UXML comms section |
| `PlatformComms_import_panel_uxml_declares_entity_diff_for_comms_staging` | Import panel entity diff wiring |
| `PlatformComms_viewer_host_binds_comms_list_on_platform_selection` | Selection → BindComms |
| `PlatformComms_staging_diff_surfaces_added_comms_row` | RowAdded comms staging diff |

## Advisory notes (lean mode)

- PNGs are labeled protocol placeholders (headless host; Unity Editor unavailable) — satisfies S33-10 AC per lean mode, matching S26-07 / S27-10 / S32-10 pattern.
- Live Editor re-capture does not block headless merge; optional polish before Production → Polish gate.
- S32-10 damage viewer placeholders remain valid historical evidence; S33-10 artifacts focus on Phase G comms per story AC.
- `DelegationSmoke.unity` keeps `useGlobeMap: 0` — CI-safe default preserved.

## Conditions for full closeout (non-blocking merge)

1. Capture live Editor `platform-catalog-comms-s33-*.png` and `platform-import-staging-s33-*.png` on Windows/macOS Unity host (replace protocol placeholder labels).
2. Run `Invoke-C2PlayModeSignoffBatch.ps1 -Scenario import`; archive `unity-c2-playmode-signoff.log`.
3. Verify platform catalog comms list and import staging Comms diff UX with full Baltic catalog dataset in Editor.

## Architecture compliance

- [x] **ZERO edits** to `src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs`
- [x] Platform import routes through `PlatformWorkbookWriteBridge` + `PlatformImportStagingProjection` (ADR-011)
- [x] Platform catalog viewer remains read-only via `PlatformCatalogViewerHost` (ADR-011)
- [x] S32-10 placeholders extended with S33-06 comms viewer + import staging evidence
- [ ] Live Editor captures (advisory — pending local Unity host)