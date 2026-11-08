# Sprint 35 — S35-07 C2 Manual Sign-Off Refresh

**Date:** 2026-06-19  
**Story:** S35-07 (`production/epics/sprint-35-polish-foundation/story-035-07-c2-signoff-refresh.md`)  
**Branch:** `stack/sprint35/c2-signoff-refresh` (evidence-only)  
**Build:** `main` @ `8de98b1` (local working tree includes uncommitted S35 W0–W2 polish; headless gates run on current tree)  
**ADR:** ADR-010 (headless-first; panel host seams), ADR-011 (platform import write-gate)  
**Baseline:** `production/qa/c2-manual-signoff-2026-06-02.md` (S34-11 @ `d3db76d`; checks 1–13 proxy **61/61**; checks 14–18 proxy **58/58**)  
**S35-06 dependency:** `production/epics/sprint-35-c2-platform-polish/story-035-06-c2-tooltips-onboarding.md` (comms legend, datalink lag helper, NPE onboarding copy)  
**Environment:** Headless Linux CI/agent host — Unity 6.3 Editor unavailable; lean **PASS WITH NOTES** per PI-006 / ADR-010.

## Verdict

**PASS WITH NOTES** — C2 manual sign-off checklist refreshed post-S35-06 presentation polish. Checks **1–18** remain PASS via headless proxy re-run. Checks **1–13** filter grew to **85/85** (was **61/61** @ S34-11 — suite expansion from S35 harness coverage; no regressions). Checks **14–18** filter unchanged **58/58** @ `8de98b1`. S35-06 adds inline COMMS legend on top bar (checks 9–11), datalink lag helper in platform catalog, message-log NPE hint, and `design/ux/onboarding-baltic.md` stub — covered by `C2CommsOnboardingTests` **4/4** (presentation proxy, not in checks 1–13 merge filter). Merge authority remains headless gates; live Editor re-capture optional.

## Acceptance criteria status

| AC | Status | Evidence |
|----|--------|----------|
| `c2-manual-signoff-*.md` refreshed for S35 changes (checks 1–18) | **PASS** | `production/qa/c2-manual-signoff-2026-06-02.md` @ `8de98b1`, verdict **PASS WITH NOTES 18/18** |
| Evidence doc with verdict + limitation notes | **PASS** | This document |
| Headless checks 1–13 filter PASS | **PASS** | **85/85** (`PlayModeSmoke\|C2Selection\|OobTree\|LossesScoring\|BalticReplay\|FuelState\|AttackMenu`) |
| Headless checks 14–18 filter PASS | **PASS** | **58/58** (`PlatformImport\|Doctrine\|C2TopBar\|PlatformCatalogViewer\|PlatformComms\|PlatformLinkCatalog`) |
| S35-06 comms legend + onboarding acknowledged | **PASS** | Checks 9–11 notes; `C2CommsOnboardingTests` **4/4**; `design/ux/onboarding-baltic.md` |
| Lean PASS WITH NOTES (no Editor host) | **PASS** | Headless Linux agent; S31/S32/S33/S34 protocol placeholder PNG references retained |
| ZERO touch `DelegationBridge.cs` | **PASS** | Empty diff vs `HEAD` |

## S35-06 presentation delta (checks 9–11 focus)

| Change | Surface | Headless proxy |
|--------|---------|----------------|
| Inline COMMS legend (`NOMINAL` / `DEGRADED` / `DENIED` text labels) | `C2TopBarPanel.uxml` + `.uss` | `C2_top_bar_panel_declares_comms_legend_with_text_labels` |
| Datalink lag helper (`LatencyMsNominal` → share-lag ticks) | `PlatformCatalogPanel.uxml` | `Platform_catalog_comms_section_declares_datalink_lag_helper` |
| NPE mission hint on C2 entry | `MessageLogPanel.uxml` | `Message_log_panel_declares_baltic_onboarding_hint` |
| Onboarding spec stub | `design/ux/onboarding-baltic.md` | `Onboarding_baltic_spec_declares_mission_goal_and_first_actions` |

Checks **9–11** behavioral proxy unchanged (`BalticReplayHarnessCommsTests`, `MapPanelBinderTests`); S35-06 closes presentation legibility gap identified in playtest think-aloud (fun-hypothesis validation 2026-06-19).

## Checklist refresh map

| Check range | S34-11 baseline | S35-07 refresh | Proxy / evidence |
|-------------|-----------------|----------------|------------------|
| 1–8 | PASS @ `d3db76d` | Re-confirmed PASS (no regression) | Checks 1–13 filter **85/85** @ `8de98b1` |
| 9–11 | PASS (comms degrade/deny behavior) | **Refreshed** — S35-06 inline COMMS legend + NPE copy | `BalticReplayHarnessCommsTests` + `MapPanelBinderTests`; `C2CommsOnboardingTests` **4/4** |
| 12–13 | PASS @ `d3db76d` | Re-confirmed PASS | `FuelState*` + `AttackMenu*` in checks 1–13 filter |
| 14–18 | PASS @ `d3db76d` | Re-confirmed PASS (no S35 platform/import changes) | Checks 14–18 filter **58/58** |

## Headless regression (CI merge authority)

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone

# Checks 1–13 proxy (S35-07 verify filter)
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests \
  --filter "PlayModeSmoke|C2Selection|OobTree|LossesScoring|BalticReplay|FuelState|AttackMenu" -v minimal

# Checks 14–18 proxy
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests \
  --filter "PlatformImport|Doctrine|C2TopBar|PlatformCatalogViewer|PlatformComms|PlatformLinkCatalog" -v minimal

# S35-06 presentation proxy (checks 9–11 + onboarding)
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests \
  --filter "C2CommsOnboarding" -v minimal

git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
```

## Gates run (agent session 2026-06-19)

| Gate | Result |
|------|--------|
| Build SHA | `8de98b150da515b205358106852eb75376ddba5f` (`8de98b1`) |
| Checks 1–13 filter `PlayModeSmoke\|C2Selection\|OobTree\|LossesScoring\|BalticReplay\|FuelState\|AttackMenu` | **85/85 PASS** (was 61/61 @ S34-11) |
| Checks 14–18 filter `PlatformImport\|Doctrine\|C2TopBar\|PlatformCatalogViewer\|PlatformComms\|PlatformLinkCatalog` | **58/58 PASS** |
| `C2CommsOnboarding` only (S35-06) | **4/4 PASS** |
| `PlatformImport` only | **10/10 PASS** |
| `PlatformCatalogViewer` only | **11/11 PASS** |
| `PlatformComms` only | **12/12 PASS** |
| `PlatformLinkCatalog` only | **13/13 PASS** |
| `Doctrine` only | **7/7 PASS** |
| `C2TopBar` only | **5/5 PASS** |
| `DelegationBridge.cs` diff vs `HEAD` | **ZERO touch** (empty diff) |
| Checklist rows 1–18 marked PASS | **18/18** |
| Unity Editor batch (`import`, `begin-execution`) | **NOT RUN** (headless Linux; documented in S34-10) |

## Advisory notes (lean mode)

- Checks 1–13 suite count increased **61 → 85** — expanded harness coverage in S35 branch; all tests PASS; no behavioral regression on checks 9–11 comms degrade/deny path.
- S35-06 presentation changes are UXML/USS/copy only; sim comms policy unchanged.
- Check 1 remains batch Play Mode evidence from S19 @ `7401fac`; no new Editor batch run on Linux agent.
- Live Editor re-capture of `*-s31-*.png`, `*-s32-*.png`, `*-s33-*.png`, `*-s34-*.png` and `Invoke-C2PlayModeSignoffBatch.ps1 -Scenario import|begin-execution` do not block headless merge.
- Optional human visual walk for comms legend legibility and click feel (checks 2–4, 9–11) non-blocking; S35-11 playtest session 7 may cover post-S35-06 axes.

## Architecture compliance

- [x] **ZERO edits** to `src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs`
- [x] S35-06 changes limited to UXML/USS/copy (`C2TopBarPanel`, `PlatformCatalogPanel`, `MessageLogPanel`) + design stub
- [x] Platform import / catalog / comms / link projections unchanged (ADR-011 read-only seams)
- [x] Comms degrade/deny behavior still driven by sim replay harness (checks 9–11 behavioral proxy)
- [x] Live Editor captures (advisory — lean PASS WITH NOTES; S35-09 — `sprint-35-presentation-evidence-2026-06-19.md`; live re-capture deferred)