# i18n String Inventory — S71-01/02 (C2/HUD/Menu Sources; P0 en-US Only)

**Date:** 2026-06-25  
**Track:** S71-01/02 (cloud, per roadmap-execute-plan-062526.md §3/§4)  
**Scope:** C2/HUD/menu string sources with file paths. **No translations.** P0 en-US prep inventory only. Docs-only. No runtime/UI behavior changes.

**Cites (mandatory):**  
`production/commercial-launch-scope-boundary-2026-06-25.md` (i18n spec IN; no prod trans; GitNexus pre + verif-before + 0e/1232/0f/6/6/18/18/hash/ZERO; stage Release)  
`docs/reports/future-sprint-roadpmap-062526.md` §3/§6/§7/§10 (S71 i18n deliverables)  
`docs/reports/roadmap-execute-plan-062526.md` §3/§4 (string-inventory.md deliverable; cite Game-Requirements)  
`AGENTS.md` (GitNexus pre; detect before commit)  
S69/S70 complete + S66 v2: `production/sprints/sprint-69-commercial-launch-foundation.md`, `production/agentic/sprint-69-parallel-kickoff-2026-06-25.md`, `production/qa/smoke-sprint-69-closeout-2026-06-25.md`, `production/sprints/sprint-70-store-community-prep.md`, `production/release/release-checklist-v3.md`, `production/release/release-checklist-v2.md`, `production/qa/evidence/baltic-v2-playtest-index.md`, `production/playtests/baltic-v2-scenario-manifest.yaml`  
Game-Requirements/requirements/20-Command-And-Control-UI.md (C2 strings origins: OOB/contacts/missions menus, context, phase/time/score labels, comms legend).  

**GitNexus pre (executed):** list_repos 19962/37627/2462; detect_changes (docs scope medium); impacts CRITICAL §5 exact: 178/97/127/52. verification-before gates RUN+READ: build 0e/0w, 1232/0f, 6/6, 18/18, hash present, ZERO bridge. All outputs read pre-claim.

**Inventory methodology:** Static UXML text= attrs + C# Label/Button .text / DisplayLine assignments + MenuItem + editor strings. Primary UI Toolkit (UIDocument/VisualElement); residual UGUI noted. Grouped by C2/HUD/menu. Paths relative to unity/ProjectAegis/Assets/ or src/. No exhaustive enum of every dynamic; representative + high-visibility for extraction plan.

## C2 (Command & Control) Strings

**UI Toolkit dominant (C2 panels, topbar, drawers, logs).**

- `unity/ProjectAegis/Assets/UI/TopBar/C2TopBarPanel.uxml`:
  - text="SIM 00:00:00"
  - text="PHASE: Planning"
  - text="Begin Execution"
  - text="TIME: 1x"
  - text="MODE: —"
  - text="COMMS: NOMINAL"
  - text="SCORE: 0"
  - text="COMMS LEGEND:"
  - text="NOMINAL = full picture"
  - text="DEGRADED = stale contacts, reduced symbol opacity"
  - text="DENIED = no new engagements, map dimmed"

- `unity/ProjectAegis/Assets/Scripts/Runtime/C2TopBarPanelHost.cs` (dynamic + const roots):
  - RootName = "c2-topbar-root"
  - SimTimeName / PhaseName / BeginExecutionName / CompressionName / ModeName / CommsName / ScoreName
  - Phase labels (Planning / Execution via SimulationPhase)
  - BeginExecutionClicked handlers (no new strings)

- `unity/ProjectAegis/Assets/UI/C2LeftDrawer/C2LeftDrawerPanel.uxml` + `C2LeftDrawerPanelHost.cs`:
  - listView labels for mission rows, contact lists (DisplayLine populated from state)
  - Example dynamic: label.text = row.DisplayLine (rows from _missionState, _panelState)
  - Static classes: "c2-left-drawer", mission/contact categories

- `unity/ProjectAegis/Assets/Scripts/Runtime/MessageLogPanelHost.cs`:
  - "message-log-root", "message-list"
  - label.text = row.DisplayLine
  - Categories: "KILL_CONFIRMED", "MAGAZINE", "COMMS", "CONTACT", "CONTACT_CHANGE", "MISSION", "MISSION_TRANSITION"
  - Classes: message-log-row, --kill, --magazine, --comms, --contact

- `unity/ProjectAegis/Assets/Scripts/Runtime/MissionListPanelHost.cs`:
  - _missionList.makeItem → Label
  - label.text = _panelState.MissionRows[index].DisplayLine

- `unity/ProjectAegis/Assets/Scripts/Runtime/OsintStagingPanelHost.cs`:
  - _statusLine.text = "OSINT Staging - proposals from digest (S20)"
  - label.text = $"{r.CanonicalId} | {r.RelevanceScore:F2} | {r.SourceUrl}"
  - _statusLine.text = $"OSINT Staging: {_current.Count} ({src}) | select+approve commits via proxy/gate; refresh for live state"

- `unity/ProjectAegis/Assets/UI/MissionList/MissionListPanel.uxml` + related:
  - Mission list labels, selection affordances (origin: GR req 20 OOB/missions menus)

**Related C2 strings (HUD-adjacent / map overlays):**  
See C2TopBar + LeftDrawer for time/phase/score/comms HUD strip. Map sym labels via APP-6 (not string-extracted here; data-driven).

## HUD / Status / Legend Strings

- Topbar comms legend (above).
- Score, mode, compression HUD labels (C2TopBarPanel.uxml + host).
- OSINT staging status (above).
- Message log categories (above).

## Menu / Editor / Tooling Strings (UGUI + Editor residual)

- `unity/ProjectAegis/Assets/Editor/DelegationSmokeSceneBuilder.cs`:
  - MenuItem("Project Aegis/Build DelegationSmoke Scene (comms QA)")
  - MenuItem("Project Aegis/Build DelegationSmoke Scene (classify QA)")
  - "C2TopBar", "C2LeftDrawer" (scene objects)

- `unity/ProjectAegis/Assets/Editor/App6AddressablesGroupSetup.cs`:
  - MenuItem("Project Aegis/Addressables/Ensure App6 MapPresentation Group")
  - label "default"

- `unity/ProjectAegis/Assets/Editor/C2PlayModeSignoffBatchRunner.cs`:
  - SessionPending = "C2Signoff.Pending"
  - SessionScenario = "C2Signoff.Scenario"
  - SessionErrorCount = "C2Signoff.ErrorCount"
  - Debug logs: "C2PlayModeSignoffBatchRunner starting...", "complete", "PASS", "FAIL"

- `unity/ProjectAegis/Assets/Editor/BuildPlayer.cs`:
  - [MenuItem("Build/Build Linux64 Player")]

- `unity/ProjectAegis/Assets/Editor/C2PlayModeSignoffBatchRunner.cs` + other editors: console / signoff strings (C2Signoff.*)

**UGUI notes:** Limited to Editor MenuItem + older smoke/playtest builders. Primary runtime is UI Toolkit. Future extraction should prioritize UXML → key map over UGUI Text/TMP.

## Data / Manifest / Non-UI Strings (Read-Only Reference)

- Scenario/policy names (baltic-v2-*.policy.json per `production/playtests/baltic-v2-scenario-manifest.yaml` + S66 v2; e.g. "baltic-patrol", "baltic-patrol-comms" — for future localized display names).
- Catalog entries, doctrine/EMCON labels (origins in GR 13/20; read via CatalogWriteGate extend-only only).
- Replay/AAR labels (tests/regression/replay-golden-baltic-v2-*.txt headers; hash only).

**Totals (prep slice):** ~40+ representative strings across C2 topbar/legend + 5+ panels + editor menus. Full scan via UXML grep + C# .text = + GitNexus query("C2 label|text|menu") in future extraction phase. No strings in hot sim (DelegationBridge invariant).

**Exclusions (per scope):** Dynamic runtime-generated (e.g. unit names from data), log timestamps, numeric scores (not translatable), full PlayMode smoke console (editor only).

**Usage for extraction-plan:** Map UXML text + named elements + C# const/DisplayLine to stable keys (e.g. "c2.topbar.sim_time", "c2.comms.legend.nominal"). P0 en-US source = UXML text values.

**S71-01/02 inventory COMPLETE.** No translations. Cites + pre + gates verified. Feeds extraction-plan + S71-05 QA.

*Self-contained. 2026-06-25.*
