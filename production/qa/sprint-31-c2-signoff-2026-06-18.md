# Sprint 31 — S31-08 C2 Manual Sign-Off Refresh

**Date:** 2026-06-18  
**Story:** S31-08 (`production/epics/sprint-31-presentation-polish/story-031-08-c2-signoff-refresh.md`)  
**Branch:** `stack/sprint31/c2-signoff-refresh` (evidence-only)  
**Build:** `main` @ `3406bc4`  
**ADR:** ADR-010 (headless-first; panel host seams), ADR-011 (platform import write-gate)  
**Baseline:** `production/qa/c2-manual-signoff-2026-06-02.md` (S19-01 checks 1–13 @ `7401fac`)  
**S31-07 dependency:** `production/qa/sprint-31-presentation-evidence-2026-06-18.md`  
**Environment:** Headless Linux CI/agent host — Unity 6.3 Editor unavailable; lean **PASS WITH NOTES** per PI-006 / ADR-010.

## Verdict

**PASS WITH NOTES** — C2 manual sign-off checklist refreshed post-S31: checks **1–13** remain PASS via headless proxy re-run (**61/61**); new checks **14–16** PASS via S31-07 evidence + headless proxy (**21/21**). Total **16/16** @ `3406bc4`. Merge authority remains headless gates. Live Editor batch scenarios (`import`, `begin-execution`) and screenshot re-capture optional polish.

## Acceptance criteria status

| AC | Status | Evidence |
|----|--------|----------|
| `c2-manual-signoff-*.md` updated with post-S31 SHA + verdict | **PASS** | `production/qa/c2-manual-signoff-2026-06-02.md` @ `3406bc4`, verdict **PASS WITH NOTES 16/16** |
| Checks 1–13 remain PASS | **PASS** | Headless proxy filter `PlayModeSmoke\|C2Selection\|OobTree\|LossesScoring\|BalticReplay\|FuelState\|AttackMenu` **61/61** |
| Check 14: Platform import staging | **PASS (headless)** | `PlatformImport` **9/9**; `production/qa/evidence/platform-import-staging-s31-baltic-diff.png` |
| Check 15: Doctrine inheritance panel ROE override | **PASS (headless)** | `Doctrine` **7/7**; `production/qa/evidence/doctrine-panel-s31-roe-override.png` |
| Check 16: Begin Execution top bar (Planning phase) | **PASS (headless)** | `C2TopBar` **5/5**; `production/qa/evidence/begin-execution-s31-planning-topbar.png` |
| S31-07 evidence linked for checks 14–16 | **PASS** | `production/qa/sprint-31-presentation-evidence-2026-06-18.md` + `production/qa/evidence/*-s31-*.png` |
| Evidence doc with verdict + limitation notes | **PASS** | This document |
| Lean PASS WITH NOTES (no Editor host) | **PASS** | Headless Linux agent; protocol placeholder PNGs per S27-10/S31-07 |
| ZERO touch `DelegationBridge.cs` | **PASS** | Empty diff vs `HEAD` |

## Checklist refresh map

| Check range | S19 baseline | S31-08 refresh | Proxy / evidence |
|-------------|--------------|----------------|------------------|
| 1–13 | PASS @ `7401fac` | Re-confirmed PASS @ `3406bc4` | `PlayModeSmoke\|C2Selection\|OobTree\|LossesScoring\|BalticReplay\|FuelState\|AttackMenu` **61/61** |
| 14 (new) | — | Platform import staging review | `PlatformImportPanelTests` **9/9**; S31-07 PNG |
| 15 (new) | — | Doctrine ROE override round-trip | `DoctrineOverrideCommandTests` + `Doctrine*` **7/7**; S31-07 PNG |
| 16 (new) | — | Begin Execution while Planning | `C2TopBarBeginExecutionTests` **5/5**; S31-07 PNG |

## S29 advisory clearance (via checks 14–16)

| S29 story | Gap cleared | S31-08 check | S31-07 evidence |
|-----------|-------------|--------------|-----------------|
| **S29-04** Platform import staging UI | Editor screenshot deferred | Check 14 | `platform-import-staging-s31-baltic-diff.png` |
| **S29-07** Doctrine inheritance panel | Editor screenshot deferred | Check 15 | `doctrine-panel-s31-roe-override.png` |
| **S29-08** Begin Execution top bar | Editor screenshot deferred | Check 16 | `begin-execution-s31-planning-topbar.png` |

## Headless regression (CI merge authority)

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone

# Baseline checks 1–13 proxy
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "PlayModeSmoke|C2Selection|OobTree|LossesScoring|BalticReplay|FuelState|AttackMenu" -v minimal

# New checks 14–16
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "PlatformImport|Doctrine|C2TopBar" -v minimal

git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
```

## Gates run (agent session 2026-06-18)

| Gate | Result |
|------|--------|
| Build SHA | `3406bc4902398538b16bd45d2eb52b1b3a8ad76c` (`3406bc4`) |
| Baseline filter `PlayModeSmoke\|C2Selection\|OobTree\|LossesScoring\|BalticReplay\|FuelState\|AttackMenu` | **61/61 PASS** |
| New checks filter `PlatformImport\|Doctrine\|C2TopBar` | **21/21 PASS** |
| `PlatformImport` only | **9/9 PASS** |
| `Doctrine` only | **7/7 PASS** |
| `C2TopBar` only | **5/5 PASS** |
| `DelegationBridge.cs` diff vs `HEAD` | **ZERO touch** (empty diff) |
| `platform-import-staging-s31-*.png` present | 1 file |
| `doctrine-panel-s31-*.png` present | 1 file |
| `begin-execution-s31-*.png` present | 1 file |
| Checklist rows 1–16 marked PASS | **16/16** |
| Unity Editor batch (`import`, `begin-execution`) | **NOT RUN** (headless Linux; documented in S31-07) |

## Headless proxy test inventory (checks 14–16)

| Test | Check | Purpose |
|------|-------|---------|
| `Import_round_trip_propose_acknowledge_approve_readback_baltic_fixture` | 14 | Staging round-trip + approve gate |
| `Import_unedited_round_trip_produces_empty_diff_golden` | 14 | Empty-diff golden |
| `Platform_import_panel_host_routes_through_staging_projection` | 14 | ADR-011 seam |
| `Delegation_smoke_scene_builder_includes_platform_import_panel` | 14 | Scene wiring |
| `Doctrine_override_round_trip_updates_policy_log_and_projection_bind` | 15 | ROE override read-back |
| `Doctrine_smoke_scene_builder_registers_doctrine_panel_host` | 15 | Scene wiring |
| `Doctrine_panel_uxml_assets_define_host_element_names` | 15 | UXML contract |
| `BeginExecution_transitions_planning_to_executing_via_bridge` | 16 | Phase transition |
| `Planning_top_bar_projection_freezes_score_until_execution` | 16 | Score freeze |
| `C2_top_bar_panel_wires_begin_execution_button_to_bridge_host` | 16 | UXML/host wiring |

## Advisory notes (lean mode)

- Checks 14–16 use S31-07 protocol placeholder PNGs (headless host; Unity Editor unavailable) — satisfies S31-08 AC per lean mode, matching S26-07 / S27-10 / S30-06 / S31-07 pattern.
- Check 1 remains batch Play Mode evidence from S19 @ `7401fac`; no new Editor batch run on Linux agent.
- Live Editor re-capture and `Invoke-C2PlayModeSignoffBatch.ps1 -Scenario import|begin-execution` do not block headless merge.
- `DelegationSmoke.unity` keeps `useGlobeMap: 0` — CI-safe default preserved.

## Conditions for full closeout (non-blocking merge)

1. Capture live Editor `*-s31-*.png` on Windows/macOS Unity host (replace protocol placeholder labels).
2. Run `Invoke-C2PlayModeSignoffBatch.ps1 -Scenario import` and `-Scenario begin-execution`; archive `unity-c2-playmode-signoff.log`.
3. Optional human visual walk for checks 2–4 click feel per PI-006.

## Architecture compliance

- [x] **ZERO edits** to `src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs`
- [x] Platform import routes through `PlatformWorkbookWriteBridge` + `PlatformImportStagingProjection` (ADR-011)
- [x] Doctrine writes via `DelegationBridgeHost.TrySetDoctrineOverride` only (ADR-010)
- [x] Begin Execution via `DelegationBridgeHost.BeginExecution()` only (ADR-010)
- [x] S29-04/07/08 advisory gaps reflected in checklist checks 14–16
- [ ] Live Editor captures (advisory — pending local Unity host)