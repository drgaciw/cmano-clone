# i18n Extraction Plan — S71-01/02 (Phased; P0 en-US Prep)

**Date:** 2026-06-25  
**Track:** S71-01/02 (cloud agent per `docs/reports/roadmap-execute-plan-062526.md` §3/§4)  
**Purpose:** Phased extraction steps from current hardcoded sources to future string tables. **P0 en-US only.** Cites Game-Requirements origins. No production translations or runtime changes in S71. Low risk docs-only.

**Cites (mandatory on all artifacts):**  
- `production/commercial-launch-scope-boundary-2026-06-25.md` (i18n extraction plan deliverable; P0 en-US prep; in-scope i18n spec only; GitNexus pre + verif-before 0e/1232/0f/6/6/18/18/hash `17144800277401907079`/ZERO; docs-only; stage Release)  
- `docs/reports/future-sprint-roadpmap-062526.md` §3/§6/§7/§10 (S71 i18n pipeline + extraction)  
- `docs/reports/roadmap-execute-plan-062526.md` §3/§4 (S71-01/02: extraction-plan.md; cite Game-Requirements/requirements/; Unity UI Toolkit vs UGUI)  
- `AGENTS.md` (GitNexus search+use list/detect/impact CRITICAL §5 exact before claims; detect before commit)  
- S69/S70 complete: `production/sprints/sprint-69-commercial-launch-foundation.md` + kickoff + smoke-69-closeout, `production/sprints/sprint-70-store-community-prep.md` + kickoff + smoke-70 + `production/release/release-checklist-v3.md` (i18n sections skeleton)  
- S66 v2 inputs: `production/release/release-checklist-v2.md`, `production/playtests/baltic-v2-scenario-manifest.yaml`, `production/qa/evidence/baltic-v2-playtest-index.md` (Baltic corpus for context)  
- `Game-Requirements/requirements/20-Command-And-Control-UI.md` (primary origin for C2 strings: OOB, contacts, missions menus, right-click context, side info, time compression, phase, score, comms, doctrine access, map interaction labels) + related GR 02/03/04/11/13 (gameplay/C2/sim modes)  
- Prior boundary (invariants carry): release-train-scope-boundary-2026-06-24.md  

**GitNexus pre (search+use list/detect/impact CRITICAL §5 exact):** list_repos canonical 19962/37627/2462; detect_changes (docs) risk medium/low affected; impacts upstream: CatalogWriteGate 178 CRITICAL, PatrolCandidateEngagePolicy 97 CRITICAL, DelegationBridge 127 CRITICAL, BalticReplayHarness 52 CRITICAL (exact match boundary/roadmap/execute §5/§7).  

**verification-before (RUN+READ full outputs before claims; per execute §5/§6 + boundary + roadmap §7 + AGENTS):**  
Build: 0 Error(s) 0 Warning(s).  
Tests: 1232/0f (breakdown 279+43+247+5+252+406; all Passed 0f).  
Replay: 6/6. C2: 18/18. Hash present (21+). ZERO bridge (22 usages only). GitNexus pre confirmed. Stage Release. Logs read. Pre state match.  

**Scope reminder:** P0 en-US prep/inventory/extraction-plan only. No string table implementation, no loc production work, no UI code edits. Future phases require ADR + full GitNexus CRITICAL + TDD + user ack.

## Phase 0 — Baseline & Inventory (S71-01/02 COMPLETE)

- [x] GitNexus pre + gates verification-before (executed; 19962/37627/2462 + exact CRIT counts; 0e/1232/0f/6/6/18/18/hash/ZERO).  
- [x] Read mandatory authorities (this file cites all).  
- [x] Produce i18n-pipeline-spec.md (workflow + tiers + UI Toolkit/UGUI).  
- [x] Produce i18n-string-inventory.md (C2/HUD/menu + paths from UXML/C#; no trans).  
- [x] Produce this extraction-plan (phased + GR cite).  
- [x] Update S71 plan/kickoff notes + sprint-status.yaml.  
- [x] Cite release-checklist-v3.md (i18n section) + S66 v2 + S69/S70.  

**Output:** Three production/release/ files + updates. Self-contained + evidence.

## Phase 1 — Static Scan & Key Mapping (Post-S71; Prep for Extraction)

1. UXML scan (priority UI Toolkit):  
   - `grep -r 'text="' unity/ProjectAegis/Assets/UI/ --include="*.uxml"` (C2TopBarPanel.uxml, C2LeftDrawerPanel.uxml, MissionListPanel.uxml, MessageLog etc.).  
   - Map `text="SIM 00:00:00"` → key `c2.topbar.sim_time` (P0 en-US value preserved).  
   - Named elements (name=) become part of key or context.

2. C# host scan (UI Toolkit):  
   - `rg '\.text\s*=' unity/ProjectAegis/Assets/Scripts/Runtime/*PanelHost.cs` (C2TopBarPanelHost, C2LeftDrawerPanelHost, MessageLogPanelHost, MissionListPanelHost, OsintStagingPanelHost).  
   - Capture const RootName/ListName + DisplayLine builders + status strings.  
   - Use GitNexus `query` or `context` for callers of DisplayLine / Refresh.

3. Editor / Menu / UGUI residual scan:  
   - MenuItem attributes + Debug.Log labels (DelegationSmokeSceneBuilder, C2PlayModeSignoffBatchRunner, BuildPlayer, App6AddressablesGroupSetup).  
   - `rg 'Text|TMP_Text|GUI.Label|EditorGUILayout.Label' unity/` for legacy UGUI. Flag for migration.

4. Data-driven (read-only):  
   - Scenario names from baltic-v2-scenario-manifest.yaml + S66 v2 goldens (future display name keys).  
   - Catalog / policy labels (origins GR 20 + 13; read via existing importers; CatalogWriteGate extend-only if touched).  
   - Cite GR 20 explicitly for all C2 menu/context/time/phase/score/comms strings.

**Deliverable per phase:** Updated inventory.md + keys.csv (or md table) with source path, current value (en-US), proposed key, UI layer (Toolkit/UGUI/Editor).

## Phase 2 — Extraction & Table Seeding (Deferred; Requires ADR + Ack)

1. Extract to neutral format (e.g. JSON/CSV or Unity Localization .po/resx skeleton; P0 en-US only).  
2. Replace static text in UXML with key refs (or bind via UIDocument controller in future).  
3. Replace .text = in hosts with localized lookup (no behavior change to layout/panels).  
4. Add string freeze note + version tag.  
5. Verify: build + full test + replay 6/6 + C2 18/18 + GitNexus impact on affected hosts (expect low if additive only) + hash/ZERO preserved.  
6. Update release-checklist-v3 i18n section + evidence-index.

**Gate before Phase 2:** S72 human ack + new boundary/ADR + full re-verif (TDD if UI touched).

## Phase 3 — Future Locales + Validation (S72+ or post)

- P1 locales (de/fr) after P0 freeze.  
- Cultural review + VO if applicable.  
- RTL layout tests (UI Toolkit supports).  
- Integration into release-checklist + launch/faq/support.  
- Re-run GitNexus pre on any changed symbols; verify Catalog extend-only / no Bridge.

## Traceability to Game-Requirements (mandatory cite)

- Primary: `Game-Requirements/requirements/20-Command-And-Control-UI.md`  
  - OOB / contacts / missions menus → C2LeftDrawer / MissionList strings.  
  - Right-click unit context / side info panel → Osint / detail labels.  
  - Engage / plot / throttle / time compression / phase → TopBar "Begin Execution", PHASE, TIME, SCORE.  
  - Comms / EMCON / doctrine → comms-label + legend strings.  
  - Map / globe / symbology → editor menu strings + HUD.  
- Cross: GR 02 Core Gameplay Loop (phase/mission transitions), 03 Simulation Modes (planning/execution labels), 04 Delegation (agent vs human indicators), 11 Mission Editor (catalog export/diff strings), 13 Doctrine (EMCON/WRA access labels).  
- S66 v2 Baltic corpus provides example policy names for future localized scenario titles.

All extracted keys must trace back to one or more GR reqs + implemented panels.

## Risks / Mitigations / Invariants

- Risk: Accidental runtime change (mit: docs-only; no .cs edits in S71; future TDD + GitNexus impact pre).  
- Risk: Scope to full loc (mit: explicit "P0 en-US prep; no translation" in every doc).  
- Invariant: ZERO DelegationBridge edits; Catalog extend-only (if any); hash preserved; tests >=1232; replay 6/6; C2 18/18.  
- GitNexus: Re-run pre (search+list+detect+impact CRITICAL) on every future sprint artifact touching UI strings.  

**Phased plan COMPLETE for S71-01/02.** Feeds S71-05 l10n QA plan (locale smoke strategy) + S72 gate. All cites + pre + gates executed and read.

*Self-contained. Independent subagent. 2026-06-25. Cites enforced. Low risk docs-only.*
