# Sprint 35 — S35-09 Live Editor Presentation Evidence Closeout

**Date:** 2026-06-19  
**Story:** S35-09 (`production/epics/sprint-35-c2-platform-polish/story-035-09-live-editor-evidence.md`)  
**Branch:** `stack/sprint35/live-editor-evidence` (evidence-only)  
**ADR:** ADR-010 (headless-first; panel host seams), ADR-011 (platform import write-gate)  
**Scope:** Consolidated inventory of all `production/qa/evidence/*-s30..s34-*.png` protocol placeholders (12 targets)  
**Environment:** Headless Linux CI/agent host — Unity 6.3 Editor unavailable; evidence via retained protocol placeholder PNGs + headless proxy tests (lean mode per S34-10).

## Verdict

**APPROVED WITH CONDITIONS (lean PASS WITH NOTES)** — S35-09 presentation evidence audit maps all 12 s30–s34 PNG targets to README protocols with lean deferral (placeholders retained; live 1920×1080 Editor capture deferred to Windows/macOS Unity host). Headless regression **58/58 PASS** (`PlatformImport|Doctrine|C2TopBar|PlatformCatalogViewer|PlatformComms|PlatformLinkCatalog`); per-filter **PlatformImport 10/10**, **Doctrine 7/7**, **C2TopBar 5/5**, **PlatformCatalogViewer 11/11**, **PlatformComms 12/12**, **PlatformLinkCatalog 13/13**. Exceeds S35-09 minimum gate **≥58/58**. Merge authority remains headless gates per ADR-010 lean mode. Live Editor re-capture optional polish before Production → Polish gate.

## Acceptance criteria status

| AC | Status | Evidence |
|----|--------|----------|
| 12 PNG targets per presentation README protocols (s30–s34 phases) | **PASS (protocol placeholder retained)** | §12-target inventory below — all 12 files present on disk |
| `Invoke-C2PlayModeSignoffBatch.ps1` scenarios clean log (zero `SIGNOFF_ERROR`) | **DEFERRED (lean)** | Unity Editor unavailable on Linux CI host; signoff batch advisory per S30-06 README |
| Evidence doc `sprint-35-presentation-evidence-2026-06-19.md` | **PASS** | This document |
| Headless filter ≥58/58 PASS | **PASS** | §Gates below — **58/58** |
| Editor blocked → lean PASS WITH NOTES documented | **PASS** | Headless Linux agent; placeholders retained per S34-10 pattern — no fabricated PNG binaries |

## 12-target PNG inventory (s30–s34)

| # | File | Sprint / story | Protocol README | Capture status | Headless proxy |
|---|------|----------------|-----------------|----------------|----------------|
| 1 | `platform-import-staging-s30-baltic-diff.png` | S30-06 / S29-04 | `README-presentation-evidence-s30.md` | **Protocol placeholder retained** (2026-06-18) | `Import_round_trip_propose_acknowledge_approve_readback_baltic_fixture` |
| 2 | `doctrine-panel-s30-roe-override.png` | S30-06 / S29-07 | `README-presentation-evidence-s30.md` | **Protocol placeholder retained** (2026-06-18) | `Doctrine_override_round_trip_updates_policy_log_and_projection_bind` |
| 3 | `begin-execution-s30-planning-topbar.png` | S30-06 / S29-08 | `README-presentation-evidence-s30.md` | **Protocol placeholder retained** (2026-06-18) | `BeginExecution_transitions_planning_to_executing_via_bridge` |
| 4 | `platform-import-staging-s31-baltic-diff.png` | S31-07 | `README-presentation-evidence-s31.md` | **Protocol placeholder retained** (2026-06-18) | `Import_round_trip_propose_acknowledge_approve_readback_baltic_fixture` |
| 5 | `doctrine-panel-s31-roe-override.png` | S31-07 | `README-presentation-evidence-s31.md` | **Protocol placeholder retained** (2026-06-18) | `Doctrine_override_round_trip_updates_policy_log_and_projection_bind` |
| 6 | `begin-execution-s31-planning-topbar.png` | S31-07 | `README-presentation-evidence-s31.md` | **Protocol placeholder retained** (2026-06-18) | `Planning_top_bar_projection_freezes_score_until_execution` |
| 7 | `platform-catalog-damage-s32-viewer-columns.png` | S32-10 / S32-06 Phase F | `README-presentation-evidence-s32.md` | **Protocol placeholder retained** (2026-06-19) | `Baltic_fixture_damage_row_surfaces_workbook_values_in_list_and_detail` |
| 8 | `platform-import-staging-s32-baltic-diff.png` | S32-10 / S32-06 Phase F | `README-presentation-evidence-s32.md` | **Protocol placeholder retained** (2026-06-19) | `Import_damage_MaxHp_round_trip_propose_acknowledge_approve_readback_baltic_fixture` |
| 9 | `platform-catalog-comms-s33-viewer-columns.png` | S33-10 / S33-06 Phase G | `README-presentation-evidence-s33.md` | **Protocol placeholder retained** (2026-06-19) | `PlatformComms_baltic_fixture_comms_surfaces_workbook_values_in_list_projection` |
| 10 | `platform-import-staging-s33-comms-diff.png` | S33-10 / S33-06 Phase G | `README-presentation-evidence-s33.md` | **Protocol placeholder retained** (2026-06-19) | `PlatformComms_staging_diff_surfaces_added_comms_row` |
| 11 | `platform-catalog-link-s34-viewer-columns.png` | S34-10 / S34-06 Phase H | `README-presentation-evidence-s34.md` | **Protocol placeholder retained** (2026-06-19) | `PlatformLinkCatalog_baltic_fixture_links_surface_workbook_values_in_list_projection` |
| 12 | `platform-import-staging-s34-link-diff.png` | S34-10 / S34-06 Phase H | `README-presentation-evidence-s34.md` | **Protocol placeholder retained** (2026-06-19) | `PlatformLinkCatalog_staging_diff_surfaces_added_link_row` |

**Lean deferral note:** No new PNG binaries were generated in this session. Existing labeled protocol placeholders (91–119 KB each, 1920×1080 metadata) remain authoritative per S27-10 / S34-10 lean mode. Live Editor re-capture replaces placeholder labels only — does not block headless merge.

## Phase progression map (s30 → s34)

| Phase | Viewer capture | Import staging capture | Doctrine / top bar (historical) |
|-------|----------------|------------------------|----------------------------------|
| **S30** | — | `platform-import-staging-s30-baltic-diff.png` | `doctrine-panel-s30-roe-override.png`, `begin-execution-s30-planning-topbar.png` |
| **S31** | — | `platform-import-staging-s31-baltic-diff.png` (replaces s30) | `doctrine-panel-s31-roe-override.png`, `begin-execution-s31-planning-topbar.png` |
| **S32** | `platform-catalog-damage-s32-viewer-columns.png` | `platform-import-staging-s32-baltic-diff.png` (MaxHp diff) | s31 doctrine/topbar retained as fallback |
| **S33** | `platform-catalog-comms-s33-viewer-columns.png` | `platform-import-staging-s33-comms-diff.png` (COMMS row) | s31 doctrine/topbar retained as fallback |
| **S34** | `platform-catalog-link-s34-viewer-columns.png` | `platform-import-staging-s34-link-diff.png` (LINK row) | s31 doctrine/topbar retained as fallback |

## Protocol execution (by sprint README)

### S30 — import staging, doctrine ROE, Begin Execution

**Protocol source:** `production/qa/evidence/README-presentation-evidence-s30.md`  
**Targets:** rows 1–3 in §12-target inventory.  
**Capture steps:** DelegationSmoke Play Mode → `PlatformImportPanel` staging diff; `DoctrineInheritancePanel` ROE override; `C2TopBarPanel` Begin Execution while Planning. Game view 1920×1080.

### S31 — S30 refresh

**Protocol source:** `production/qa/evidence/README-presentation-evidence-s31.md`  
**Targets:** rows 4–6 — replaces s30 placeholders with s31-labeled artifacts.  
**Headless proxy:** `PlatformImport` 10/10, `Doctrine` 7/7, `C2TopBar` 5/5.

### S32 — Phase F damage surfacing

**Protocol source:** `production/qa/evidence/README-presentation-evidence-s32.md`  
**Targets:** rows 7–8 — damage viewer columns + MaxHp `DAMAGE row=…` staging diff.  
**Headless proxy:** `PlatformCatalogViewer` 11/11, `PlatformImport` (damage round-trip).

### S33 — Phase G comms surfacing

**Protocol source:** `production/qa/evidence/README-presentation-evidence-s33.md`  
**Targets:** rows 9–10 — comms list section + `COMMS row=…` staging diff.  
**Headless proxy:** `PlatformComms` 12/12.

### S34 — Phase H LinkCatalog surfacing

**Protocol source:** `production/qa/evidence/README-presentation-evidence-s34.md`  
**Targets:** rows 11–12 — link catalog list + `LINK row=…` staging diff.  
**Headless proxy:** `PlatformLinkCatalog` 13/13.

## Signoff batch scenarios (advisory — Editor host required)

`Invoke-C2PlayModeSignoffBatch.ps1` scenarios per S30-06 extension:

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

**S35-09 lean deferral:** Signoff batch not executed on Linux CI host (Unity Editor unavailable). Expected evidence when run locally: `unity-c2-playmode-signoff.log` with `C2PlayModeSignoffBatchRunner PASS` and zero `SIGNOFF_ERROR`.

## Headless regression (CI merge authority)

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone

dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "PlatformImport|Doctrine|C2TopBar|PlatformCatalogViewer|PlatformComms|PlatformLinkCatalog" -v minimal

git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
ls production/qa/evidence/*-s3*.png
```

## Gates run (agent session 2026-06-19)

| Gate | Result |
|------|--------|
| `PlatformImport\|Doctrine\|C2TopBar\|PlatformCatalogViewer\|PlatformComms\|PlatformLinkCatalog` filter | **58/58 PASS** |
| `PlatformImport` only | **10/10 PASS** |
| `Doctrine` only | **7/7 PASS** |
| `C2TopBar` only | **5/5 PASS** |
| `PlatformCatalogViewer` only | **11/11 PASS** |
| `PlatformComms` only | **12/12 PASS** |
| `PlatformLinkCatalog` only | **13/13 PASS** |
| `DelegationBridge.cs` diff vs `HEAD` | **ZERO touch** (empty diff) |
| `*-s30..s34-*.png` present | **12/12** files in `production/qa/evidence/` |

## Advisory notes (lean mode)

- PNGs are labeled protocol placeholders (headless host; Unity Editor unavailable) — satisfies S35-09 AC per lean mode, matching S26-07 / S27-10 / S34-10 pattern.
- S35-09 does **not** replace s30–s34 placeholders with new binaries on headless host; audit + mapping is the deliverable.
- Live Editor re-capture of all 12 targets does not block headless merge; optional polish before Production → Polish gate.
- Historical s30/s31 doctrine and top-bar placeholders remain valid alongside s32–s34 catalog/import phase artifacts.
- `DelegationSmoke.unity` keeps `useGlobeMap: 0` — CI-safe default preserved.

## Conditions for full closeout (non-blocking merge)

1. On Windows/macOS Unity 6.3 host: re-capture all 12 `*-s30..s34-*.png` at 1920×1080 Game view (replace protocol placeholder labels).
2. Run `Invoke-C2PlayModeSignoffBatch.ps1 -Scenario import|begin-execution|doctrine`; archive `unity-c2-playmode-signoff.log` with zero `SIGNOFF_ERROR`.
3. Verify platform catalog viewer (damage → comms → link) and import staging diff UX (Baltic → MaxHp → COMMS → LINK) with full dataset in Editor.

## Architecture compliance

- [x] **ZERO edits** to `src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs`
- [x] Platform import routes through `PlatformImportPanelHost` → `PlatformWorkbookWriteBridge` (ADR-011)
- [x] Platform catalog viewer remains read-only via `PlatformCatalogViewerHost` (ADR-011)
- [x] All 12 s30–s34 PNG targets traced to README protocol + headless proxy
- [ ] Live Editor captures (advisory — pending local Unity host)