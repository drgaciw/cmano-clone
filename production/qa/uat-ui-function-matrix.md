# UAT UI Function Matrix — Round 3 (2026-11-21)

Denominator for DoD criterion **C11** (`uat-dod-rubric.md`). A row is closed when its result is **Pass**, **Defect** (with ID) or **Owner** (with reason). "No exception" alone is not a pass: the observable column says what changed.

How controls are driven: MCP `script-execute` with `unity/ProjectAegis/.ai-game-dev/snippets/UatHarness.cs` — `Inventory()` lists every Button / Toggle / DropdownField / TextField / Slider / Foldout / ListView per `UIDocument`; `Press()` sends a `NavigationSubmitEvent` (a code-sent `ClickEvent` does not trigger UI Toolkit buttons); `SetValue()` sets field values; `Text()` snapshots visible labels for before/after diffs. Panels missing from `DelegationSmoke.unity` are added at runtime by `SpawnMissing()` (Play Mode only; the scene is never saved).

## Coverage summary

| Area | Surfaces | Controls | Pass | Defect | Owner | Open |
|------|----------|----------|------|--------|-------|------|
| Game panels in committed scene | 11 docs | 58 (first inventory) | — | — | — | 58 |
| Game panels spawned for test (D15/D18) | 24 hosts | pending inventory | — | — | — | — |
| Runtime editor panels (Platform Editor/Import/Catalog, Scenario Editor) | 4 | pending inventory | — | — | — | — |
| Editor windows and menu items | 1 window + 17 menu items | pending | — | — | — | — |
| Mission Editor CLI | 62 verbs | 62 | ~40 | 13 (CLI-1…CLI-13) | — | 9 not run |

## Findings so far

| ID | Area | Summary | Severity | Status |
|----|------|---------|----------|--------|
| D17 | Scene builder | `DelegationSmokeSceneBuilder` wires AxisControl, MapScaleHud and GroundOps hosts to `.uxml`/`.uss` files that do not exist | S3 | **Fixed** — minimal UXML/USS authored for the element names each host queries; guard `SceneBuilderUiAssetPathTests` (RED→GREEN). Assets never existed (hosts added as stubs in `dcd82447`) |
| D18 | Scenes | 10 hosts are in no scene, prefab or builder, so the game can never show them: `ScenarioEditorShellHost`, `PlatformEditorShellHost`, `BattleGraphicPanelHost`, `MissionListPanelHost`, `OobTreePanelHost`, `SensorC2PanelHost`, `SensorC2HudHost`, `OsintStagingPanelHost`, `JevAttentionPanelHost`, `CombatDomainsHotTickHost` | S2 | Owner decision (with D15) |
| D19 | Data | Opening the shipped catalog for read rewrites `assets/data/catalog/*.db` (`CatalogReaderFactory.TryCreateCatalogReaderAtPath` → `EnsureGenericSwarmPlatform`); makes `Read_open_of_shipped_catalog_leaves_database_file_bytes_unchanged` fail whenever the CLI suite runs first | S2 | **Fixed** — completes #899: two write paths closed (factory `EnsureGenericSwarmPlatform` on shipped DBs; `CatalogDiffProposalAgent` opening `CatalogWriteGate`, which migrates, during `catalog_intelligence_run`). `CatalogReaderFactoryShippedBytesTests` (RED→GREEN). Full headless suite leaves both shipped DB hashes unchanged (C12, headless half) |
| CLI-1 | CLI | Malformed JSON crashes `scenario_validate` / `scenario_simulate_sample` (exit 134) | S2 | **Fixed** — `LOAD_FAILED` error envelope, exit 1; `MalformedScenarioCliTests` (RED→GREEN) |
| CLI-1b | CLI | Same unguarded `LoadFromFile` in `SideCommands.cs:20`, `ScenarioPublishCommand.cs:19`, `TimelineCommands.cs:21`, `MissionListCommand.cs:31`, `ScenarioNearFutureSpawnCommand.cs:24`, `EventCommands.cs:197` | S2 | **Fixed** (`4dbc9389`, shared `ScenarioLoadGuard` + `MalformedScenarioCliTests` 11/11 PASS) |
| CLI-5 | CLI | `scenario_migrate_preview` successor IDs differ per process (`string.GetHashCode`) — determinism invariant | S2 | **Fixed** — FNV-1a matching `DeterministicHash.OrdinalHash`; pinned-literal test (RED→GREEN) |
| CLI-5b | Data/CLI | Same bug class: `CmoMarkdownImportProposer.cs:403` (`StringComparer.Ordinal.GetHashCode`) and `MissionPlanSuggestCommand.cs:362` (default idempotency key from `intent.GetHashCode`) | S2 | **Fixed** (`42d7c8fa`, `StableHash.Fnv1a32`, `ImportBatchClockDeterminismTests` 4/4 + `MissionPlanSuggestCommandTests` PASS) |
| D20 | Repo | Folder `.meta` files missing from git (`Assets/UI/AxisControl`, `GroundOps`, `MapScaleHud`, `AuthorityRoe`, `BattleGraphic`, `EngageExplain`, `MissionPackage`), so each clone generates new folder GUIDs | S4 | **Fixed** — metas committed (`f91bd975`; no prior-red test possible, C8 exception) |
| D21 | Game UI | `EngageExplainPanelHost.TryWireElements` calls `panel.Insert(1, …)` on an empty root and throws `ArgumentOutOfRangeException` **every frame** until a visual tree exists (`LateUpdate` retries) — 1,700+ console exceptions when the host starts without its UXML | S4 | **Fixed, verified** — `PanelInsertGuard.ClampIndex`; also guarded `CoverageMapView`. 0 exceptions with 24 hosts spawned (`a60982ec`) |
| D22 | Game UI / C2 | Player could command the enemy: selecting `hostile-1` in the OOB enabled Fire / Salvo / Hold; `TrySelectAttackOption` called `EnsureHumanControl` on the red AI unit and queued its attack (`[PLAYER_ORDER] Player ordered Engage for hostile-1`). `TryIssueBoatCommand` had the same gap | S2 | **Fixed** — `DelegationBridge.IsOpposingAiUnit`; attack enqueue and menu refuse `NOT_PLAYER_CONTROLLED`; host refuses before `EnsureHumanControl`. 2 tests RED→GREEN. **Verified in Play Mode**: hostile-1 buttons disabled with tooltip `NOT_PLAYER_CONTROLLED`, u1 enabled |
| D23 | Game UI / a11y | Contact rows in the left drawer only react to a mouse `ClickEvent`; no Return/Space or list-selection path (OOB rows have one), so keyboard users cannot select a contact | S3 | **Fixed, verified** — rows focusable, Return/Space and `selectionChanged`; Return on a row opens Contact Detail (`c65d9a0b`) |
| D24 | Game UI | Selection is exclusive by design (qa-loop-08), but when it moves to a contact, Unit Detail keeps showing the old unit with Fire enabled; pressing Fire returns `DENIED: No unit selected (NO_SELECTION)` | S3 | **Fixed, verified** — `UnitDetailSelectionGate`; Fire/Salvo/Hold disabled with `NO_SELECTION` after a contact is selected (`8c90cbf6`) |
| D24b | Design | Contact Detail asks "select a friendly unit for range assessment", which exclusive selection makes impossible; Unit Detail shows `ENGAGE … READY` while Contact Detail shows `Technical targetability: NO` / not cleared to engage | — | **Owner** (design question) |
| D25 | Game UI | Unit-order toolbar: a successful order clears the status line (`SetStatus(null)`), so Hold / RTB / EMCON / Sensors / Engage / Plot Course give no visible confirmation (orders are accepted: `TryIssueSelectedCommand` returns true) | S4 | **Fixed, verified** — `ISSUED: hold → u1` (`ee56a6ca`) |
| D26 | Editors | In-game Scenario Editor shell: `LoadRequested` / `SaveRequested` / `UndoRequested` / `PlayRequested` / `SampleRequested` have no subscribers, so Load does nothing and Save / Undo / Redo / Play / Sample stay disabled; shell text says "bind … in a later wave" | — | **Owner** (feature incomplete; with D18) |
| D27 | Game UI | Speed `−` / `+` while paused change nothing visible (top bar shows only `TIME: PAUSED`) | S4 | **Fixed, verified** — `TIME: PAUSED (2x)` (`28035771`) |
| D29 | Game UI | With the D22/D24 reason appended, disabled attack button labels are clipped (`e 1 round (NOT_PLAYER_CONTROLLE`) — needs wrapping like D11. Screenshot `evidence/uat-2026-11-21/r3-d22-hostile-attack-disabled.png` | S4 | **Fixed, verified** (`9556f093`) — labels wrap in full (`r4-d29-d12-hostile.png`) |
| D30 | Game UI | Contact Detail opens over Unit Detail with a translucent background, so Unit Detail text shows through; Message Log covers the bottom of Contact Detail ("Open contact explanation"). Screenshot `r3-d23-d24-d25-contact-selected.png` | S3 | **Fixed, verified** (`8e72ed13` opaque + clears Message Log; `e3d3cd47` D30b: code-built Engagement/Posture lines were rgb(27,27,27)) — `r4-d30b-contact-lines.png` |
| D31 | Game UI | Left drawer Contacts tab: Sort / Filter dropdowns render as blank grey boxes and the contact list area is visibly empty while it holds `c1` (selection works by keyboard/code). Same screenshot | S3 | **Fixed, verified** (`ab8c84ae` colours + list height; `58394db0` D31b: Unity's 150px label left the value 0px wide) — `r4-d31b-drawer.png` |
| D12 | Game UI | **Confirmed** (was unconfirmed): "Collapse Left Drawer" button clipped at the top edge of the drawer (both screenshots) | S4 | **Fixed, verified** (`069f4b31`) — `r4-d29-d12-hostile.png` |
| D32 | Tests | Unity PlayMode `CoverageMapViewTests.Source_declares_…` fails: path `…/cmano-clone/ProjectAegis/Assets/…` (missing `unity/`). | S3 | **Fixed, verified (Round 8)** — resolved via `UnityEngine.Application.dataPath`; Unity PlayMode 22/22 PASS |
| D33 | Tests | Unity PlayMode `SliceAContactPanelSmokeTests.Contact_panel_binds_once_per_frame_and_clears_on_unit_selection`: expects contact `SourceLine`/`CommsLine` "UNKNOWN", gets real observer provenance `"SRC: u1 ..."` and `"COMMS: no degradation reported"` | S3 | **Fixed, verified (Round 8)** — updated provenance & nominal comms assertions; Unity PlayMode 22/22 PASS |
| D22b | C2 / invariant | **Regression from the D22 fix:** (1) the opposing-unit rule (`AgentController` id not starting `friendly-`) also blocks the player's own units delegated to agents — 2 Unity PlayMode `RightUnitPanelCommandFeedbackTests` fail; (2) it edited `DelegationBridge.cs`, which the Phase 0 smoke holds byte-identical to main (ZERO-touch invariant) → `Phase0_smoke_script_exists_and_passes_quick_mode` fails | S2 | **Fixed, verified** (`3d18f60d`) — `PlayerSideGuard` uses `BalticV3SideRegistry` + friendly mesh, not agent names; `DelegationBridge.cs` back to identical with main (Phase 0 smoke PASS, bridge diff 0). Root cause of the 2 Unity test failures: default selection picked `hostile-1` (alphabetical) — `C2SelectionResolver` now skips red units (**behaviour change**: default selection is now `u1`). Unity PlayMode RightUnit tests pass; Play Mode: default u1, hostile-1 refused, u1 fires. Limitation: a red unit with an unregistered non-legacy id is not detected |
| D34 | Tests | `Player_builds_strip_unity_mcp_ready_define` reads `ProjectSettings.asset` from disk, and the Unity-MCP NuGet resolver writes `UNITY_MCP_READY` into every target's defines whenever the Editor runs — so the guard always fails in a live working copy even though the committed file is correct. Correction: that Server/LinuxHeadless assertion was an uncommitted round 2 edit of mine, not committed code | S4 | **Fixed** — guard reads `git show HEAD:` (disk fallback); committed with the isolation hardening; 1258/1258 with the dirty working copy |
| D35 | Repo / Addressables | No `AddressableAssetsData` is committed, although the APP-6 atlas is documented as Addressables-loaded; the menu item generates the settings from scratch on each machine (moved out of the project to the scratchpad after the test) | S3 | **Owner** (commit generated settings or drop the Addressables path) |
| D36 | Game UI | Clicking the red unit's own map marker (`hostile-1`, FULL PICTURE fog) selects contact id `hostile-1`, which is not a contact → Contact Detail shows `CONTACT: —` | S3 | Plan Task 7 |
| D37 | Scene / spec C5 | Doctrine Inheritance panel hidden (`showPanel: 0`, set by `7caae00b` without a reason) although PLAYMODE-SMOKE requires it on `-mission-roe`; when shown it matched the spec exactly (ROE WeaponsTight, EMCON ACTIVE, SOURCE Mission) but covered the Unit Detail attack buttons | S3 | **Fixed** (Jev 6b: show_and_reposition 0.99) — docked bottom-left in the left drawer (`f0276b53`), shown in scene (`716b3dfd`); **Play Mode verified 6d**: panel at x 8–252 inside the 260-wide left drawer, clear of the map and Unit Detail (`r6-d37-d38-comms-doctrine-after.png`); exposed D39 |
| D38 | Game UI / spec C5 | `-comms` top bar goes NOMINAL → DEGRADED (2 s) → DENIED (4 s) as specified, but the 12-row Message Log showed no COMMS rows: newer AGENT_DECISION rows evicted them | S3 | **Fixed** (Jev 6b: fix_now 0.99) — `MessageLogRowWindow` pins COMMS rows within the cap (`80caba9c`); headless RED→GREEN; **Play Mode verified 6d**: `-comms` at 5 s shows COMMS DENIED and both transition rows pinned among the 12 rows, 0 console errors (`r6-d37-d38-comms-doctrine-after.png`) |
| D39 | Game UI | After the D37 width fix (244px, overflow hidden) the Doctrine panel's `ROE Override` dropdown is clipped at the panel edge: Unity's default ~150px field label plus the 160px dropdown minimum exceed the 226px inner width | S3 | **Fixed** (Jev 6d: fix_now 0.89) — test-first, 3 commits (`7a092eb4`, fix rounds `3d4e5e85`, `8f4c2e16`): override row is a column, field label stacked above a full-width input (220 units; `WeaponsFree` needs 90.5), label colour rgb(180,190,200); each round RED→GREEN headless and measured in Play Mode (`r7-d39-doctrine-override-final.png`) |
| D40 | Game UI / spec C5 (pre-existing) | At sim 5 s the Message Log shows `[POLICY_UPDATE] Policy emcon: Active → Passive` with no unit, but Unit Detail and the Doctrine panel still show `EMCON: ACTIVE`. Source: `PlayModeSmokeOrbatSeeder.AdvanceDecisionLog` appends a canned smoke-feed row that changes no policy, in every scenario | S3 | **Fixed, verified (Round 8)** — removed canned EMCON row from `PlayModeSmokeOrbatSeeder.AdvanceDecisionLog`; RED→GREEN `AdvanceDecisionLog_does_not_emit_synthetic_emcon_policy_update`; verified in all 5 Play Mode scenarios (`r8-d40-comms-clean.png`) |
| D20b | Repo | git-based guard `UnityFolderMetaTrackedTests` (RED on the pre-D20 commit, listing 8 folders) found `PendingApproval.meta` and two MissionPackagePanel metas still untracked | S4 | **Fixed** (`e7510162` test, `a17c1adf` metas) — D20 now has a real red-first test |
| D28 | Editor menu | `Project Aegis/Play Baltic Escort` rebuilds and saves `BalticEscort.unity` before playing; the rebuilt file gets new fileIDs and object order, so every Play leaves the committed scene modified in git (122-line diff, same content) | S4 | **Fixed, verified (Round 8)** — `Project Aegis/Play Baltic Escort` plays committed scene with 0 diff on `BalticEscort.unity` |
| CLI-2…CLI-13 | CLI | See `evidence/uat-2026-11-21/mission-editor-cli-results.md` | S3–S4 | Triage |

## Game panels — committed scene (first inventory, Play Mode, before Begin Execution)

| Panel | Controls | Disabled at start |
|-------|----------|-------------------|
| C2TopBar | 4 buttons (Begin Execution, −, +, pause/resume) | Begin Execution, pause/resume |
| C2LeftDrawer | 1 button, 3 toggles, 2 dropdowns, 1 text field | — |
| MapPlaceholder | 17 buttons, 6 toggles, 1 dropdown, 4 text fields | — |
| RightUnitDetail | 3 buttons (Fire 1 round, Fire salvo, Hold fire) | — |
| ContactDetail | 2 buttons, 2 toggles | both explanation links |
| DoctrineInheritance | 1 button, 1 dropdown | Apply |
| MessageLog | 1 button (collapse) | — |
| AttentionToast | 2 buttons (ACK, DISMISS) | — |
| PlatformCatalog | 8 buttons, 2 text fields | — |
| PlatformImport | 8 buttons, 1 toggle, 1 text field | Reject, Approve |

## Round 3 results — Play Mode (2026-11-21, after Editor restart)

Full inventory: 210 controls in 34 `UIDocument`s (10 in the committed scene + 24 hosts spawned for test), plus 300 generated list rows (combat event history, command review) sampled 2 per list. Raw TSV: `UatHarness.Inventory()` / `Sweep()` output.

### Interactive flow (C9)

| Function | Observable | Result |
|----------|-----------|--------|
| Attention toast ACK | Toast clears; RESUME becomes enabled | Pass |
| RESUME / PAUSE | Sim clock advances / freezes (00:00:35 → 00:00:38, then frozen 1.5 s); button text flips | Pass |
| Speed `+` (running) | `TIME: 1x` → `TIME: 2x`; 3 s sim per 1.5 s real | Pass |
| Speed `−` (running) | `TIME: 2x` → `TIME: 1x` | Pass |
| Speed `−` / `+` (paused) | No visible change | Defect D27 |
| OOB row select | Unit Detail shows the unit | Pass |
| Contact row select (mouse) | Contact Detail renders full evidence chain | Pass |
| Contact row select (keyboard) | — | Defect D23 |
| Fire 1 round / salvo / hold (own unit) | `QUEUED: …` in Unit Detail | Pass |
| Fire for hostile unit | Must be refused | Defect D22 → **Fixed, verified** |
| Fire after selecting a contact | Must not offer a stale unit | Defect D24 |
| Unit-order toolbar (6 orders, u1) | Orders accepted; no visible confirmation | Pass (function) / Defect D25 (feedback) |

### Panel sweep (press every enabled, visible control; record label/state diff and errors)

| Panel | Exercised | Changed something | Notes |
|-------|-----------|-------------------|-------|
| C2LeftDrawer | 8 | 8 | Tabs, sort, filter, text find, collapse all respond |
| MapPlaceholder | 14 + 2 sampled rows | 7 | Tactical/Operational/Theater, foldouts, command-review decisions respond; Follow selection / Earlier / Latest events no visible change at t=0 (no selection, no events) — re-test with history |
| MessageLog | 1 | 1 | Collapse / expand |
| RightUnitDetail | 3 | 3 | See C9 |
| ContactDetail | 6 | 5 | Foldouts, explanation link; engagement link disabled (no engagement) |
| DoctrineInheritance | 0 | — | Hidden in this scenario (ROE dropdown has no choices) — re-test on `-mission-roe` |
| BattleGraphic (spawned) | 3 | 0 text | Zoom buttons change no labels — visual only, screenshot check (C10) |
| PlatformEditorShell (spawned) | 2 | 1 | Catalog / Import tabs switch; the in-scene PlatformCatalog / PlatformImport documents have every control hidden — reachable only through the shell, which no scene contains (D18) |
| ScenarioEditorShell (spawned) | 6 | 1 | Tabs and filters respond; Load does nothing (D26) |
| LiveEdit (spawned) | 1 | 0 | Commit: no visible change |
| AgentRoster, AirOps, BoatOps, PendingApproval, MissionList/Package, ScenarioLibrary, SensorC2, OobTree, GroundOps, DeckHangar (spawned) | 0 | — | Every action button disabled with an empty list in `baltic-patrol-classify` (no aircraft, boats, packages or pending approvals). Re-test in a scenario that populates them (BalticEscort for EscortMission/AirOps) |

### Editor windows and menu items

| Item | Result |
|------|--------|
| Scenario Map Authoring window (opened from menu; 38 controls) | Pass — Open, 4 domain filters, platform pick, Place + Commit, Begin Place, Cancel Gesture, RP polygon, Refresh Findings (7 codes), Rebuild (28 units, 6 RPs), Save — all against a scratch copy; repo scenario untouched. Note: each commit writes the file immediately, so `Dirty: no` is accurate. Browse opens a native modal file dialog → **Owner** |
| Scenario Editor ▸ Add Aircraft / Ships / Subs / Ground Units, Open Baltic UI Dev Scenario | Pass (5/5, 0 errors) |
| Play Baltic Escort → Escort Command Post (21 controls) | Pass (21/21, 0 errors): begin, resume, speed, 4 postures, radar active/passive, investigate, engage (correctly refused without fire-control track), hold/proceed convoy, 3 alert modes, focus, acknowledge, pause, restart. Side effect D28 |
| Fix UIDocument PanelSettings / Strip AudioListeners / Ensure UI Maturity Hosts | Pass (scene reverted after each). Ensure Hosts adds the 17 hosts of D15 |
| Build DelegationSmoke (comms QA / classify QA), Build Baltic Escort Scene, Addressables ▸ Ensure App6 MapPresentation Group | Pass (0 errors each; scenes reverted). The DelegationSmoke builders produce 30 roots versus 14 in the committed scene (same gap as D15). The Addressables item creates `Assets/AddressableAssetsData/` (28 files) from scratch because none is committed → D35. Build CesiumSpike not run (creates a new scene; same builder path) |
| Build ▸ Build Linux64 Player (Mono) | **Not run** — blocked on C1 (player defines) and needs the Editor for minutes; owner after C1 |
| MCP ▸ Pin Local Host :8081 | **Not run** — rewrites MCP config already verified by tests |

### Five-scenario re-run after the round 3 fixes (C4, C5)

| Scenario | Console errors | Spec check |
|----------|----------------|-----------|
| `baltic-patrol` | 0 | Top bar, OOB, Unit Detail render; `TIME: PAUSED (1x)` |
| `baltic-patrol-classify` | 0 | Map shows `c1 Classified`; hostile-1 attack disabled (`NOT_PLAYER_CONTROLLED`) |
| `baltic-patrol-comms` | 0 | `COMMS: DENIED`; u1 `STATUS: UNKNOWN (Out of comms)` |
| `baltic-patrol-mission-roe` | 0 | u1 `DOCTRINE: WeaponsTight (mission)`, `EMCON: ACTIVE` (D2b holds after D24) |
| `baltic-patrol-mission` | 0 | Unit Detail renders; Doctrine Inheritance panel hidden (no ROE choices) |

### Unity test assemblies

`ProjectAegis.Unity.Tests` (PlayMode, 20 tests): **18 pass, 2 fail** after the D22b re-fix — D32 (your local edit), D33 (likely pre-existing). No EditMode project tests exist (the only EditMode test is Addressables sample code).

### C11 classification of rows without a visible effect

| Row | Classification |
|-----|----------------|
| AgentRoster, AirOps, BoatOps, PendingApproval, MissionList/Package, ScenarioLibrary, SensorC2, OobTree, GroundOps, DeckHangar actions | **Owner** — reachable only through hosts the committed scene lacks (D15/D18), and no `baltic-patrol-*` scenario populates them (no aircraft, boats, packages, approvals); disabled-state verified, actions not exercisable |
| Doctrine Inheritance (ROE dropdown, Apply) | **Owner** — hidden in all five scenarios (empty ROE choices); needs a scenario with inheritable doctrine or a design answer |
| Map: Follow selection / Earlier events / Latest events | **Pass (no-op by state)** at t=0 with no selection and an empty event page; not re-run with history — residual risk noted |
| LiveEdit commit | **Owner** — no visible effect and no live-edit target in this scenario |
| BattleGraphic zoom ×3 | **Owner (C10)** — visual only, no label change |
| Map-marker click-select | Round 5: `u1` marker → Unit Detail u1 **Pass**; `c1` marker → Contact Detail c1 **Pass**; `hostile-1` unit marker → empty Contact Detail **Defect D36** |

### Shipped data (C12)

Catalog DB hashes identical before and after this Play Mode session (`82d26212…`, `75ea357a…`) and after the full headless suite.
