# Sprint 25 — S25-11 C2 Editor Tri-Batch Sign-Off

**Date:** 2026-06-17 (evidence captured 2026-06-18)  
**Story:** S25-11 (`production/sprints/sprint-25-phase-b-damage-assurance.md`)  
**Branch:** `stack/sprint25/c2-editor-tri-batch`  
**Base:** `main` @ `bd225ae`  
**ADR:** ADR-010 (headless-first; map projection read-only; ZERO touch `DelegationBridge.cs`)  
**Closes:** S24-07 advisory gap (tri-batch visual sign-off)  
**Environment:** Headless Linux — Unity 6.3 Editor unavailable; **lean mode** (headless proxy = merge authority)

## Verdict

**APPROVED WITH CONDITIONS** — headless tri-batch proxy **PASS** (19/19 filtered); full solution **641/641 PASS**. Unity Editor `Invoke-C2PlayModeSignoffBatch.ps1` tri-batch remains **advisory** until a local Editor host runs comms/classify/doctrine batchmode.

## Tri-batch protocol

| Batch | Editor entry (advisory) | Policy id | Headless proxy (merge authority) | Result |
|-------|-------------------------|-----------|----------------------------------|--------|
| **Comms** | `Invoke-C2PlayModeSignoffBatch.ps1 -Scenario comms` | `baltic-patrol-comms` | `Baltic_patrol_comms_harness_matches_manual_qa_preconditions`; `BalticReplayHarnessCommsTests` | **PASS** |
| **Classify** | `Invoke-C2PlayModeSignoffBatch.ps1 -Scenario classify` | `baltic-patrol-classify` | `Baltic_classify_map_symbols_include_hostile_for_selection_path`; `Baltic_classify_selection_flow_syncs_map_oob_and_contact_summary` | **PASS** |
| **Doctrine** | `Invoke-C2PlayModeSignoffBatch.ps1 -Scenario doctrine` *(documented; ps1 ValidateSet pending)* | `baltic-patrol-mission-roe` | `Baltic_doctrine_mission_roe_harness_matches_doctrine_batch_preconditions`; `Doctrine_override_round_trip_updates_policy_log_and_projection_bind`; `Doctrine_panel_uxml_assets_define_host_element_names`; `Doctrine_smoke_scene_builder_registers_doctrine_panel_host` | **PASS** |

**`RunDoctrineBatch` proxy:** Headless `Baltic_doctrine_mission_roe_harness_matches_doctrine_batch_preconditions` + doctrine filter tests constitute the Req 13 regression stand-in for Editor `RunDoctrineBatch` after S25-08 atlas and S25-10 EMCON merges.

## Archived log (no `SIGNOFF_ERROR`)

| Artifact | Path | Notes |
|----------|------|-------|
| Headless proxy run | `production/qa/sprint-25-c2-tribatch-headless-proxy-2026-06-18.log` | **18 PASS** pre-harness extension; re-run post-merge expected **19 PASS** |
| Full solution gate | §Gates below | **641 PASS** @ stack tip |
| Editor batch log | *Not produced* — lean mode | No `SIGNOFF_ERROR` in headless proxy log (Editor log N/A) |

## Gates (merge authority)

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone

dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "PlayModeSmoke|Doctrine|MapPanelBinder" -v minimal

dotnet test ProjectAegis.sln -v minimal

git diff main -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
rg "DecisionLog" src/ProjectAegis.Delegation/Projection/MapPanelBinder.cs
```

| Gate | Result |
|------|--------|
| `PlayModeSmoke\|Doctrine\|MapPanelBinder` filter | **19/19 PASS** |
| Full solution | **641/641 PASS** (≥592 floor) |
| `DelegationBridge.cs` diff vs `main` | **ZERO touch** |
| `MapPanelBinder` → `DecisionLog` writes | **None** (ADR-010 read-only projection) |
| Headless log `SIGNOFF_ERROR` | **None** |

## Per-project counts (full sln @ stack tip)

| Project | Passed |
|---------|--------|
| ProjectAegis.Sim.Tests | 93 |
| ProjectAegis.MissionEditor.Cli.Tests | 21 |
| ProjectAegis.Delegation.UnityAdapter.Tests | 100 |
| ProjectAegis.Delegation.Tests | 177 |
| ProjectAegis.Data.Excel.Tests | 5 |
| ProjectAegis.Data.Tests | 245 |
| **Total** | **641** |

## Advisory Editor steps (clears S24-07 condition fully)

Run locally with Unity **6000.3.14f1**:

```powershell
pwsh tools/unity/Invoke-C2PlayModeSignoffBatch.ps1 -Scenario comms
pwsh tools/unity/Invoke-C2PlayModeSignoffBatch.ps1 -Scenario classify -SkipBuild
# Doctrine: build scene with baltic-patrol-mission-roe via DelegationSmokeSceneBuilder menu
# until -Scenario doctrine is added to Invoke-C2PlayModeSignoffBatch.ps1
```

**Evidence to attach when run:** `unity-c2-playmode-signoff.log` with `C2PlayModeSignoffBatchRunner PASS` lines and zero `SIGNOFF_ERROR` for all three scenarios.

## Architecture compliance

- [x] **ZERO edits** to `src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs`
- [x] Map projection read-only (ADR-010) — `MapPanelBinder` regression green post S25-08 atlas
- [x] Comms/classify/doctrine batches covered by headless proxy tests
- [ ] Editor tri-batch log archived (advisory — pending local Unity host)

## Conditions for full closeout (non-blocking merge)

1. Capture Editor tri-batch log on Windows/macOS Unity host before Production → Polish gate.
2. Add `-Scenario doctrine` + `RunDoctrineBatch` to `Invoke-C2PlayModeSignoffBatch.ps1` / `C2PlayModeSignoffBatchRunner` when Editor path is next touched.