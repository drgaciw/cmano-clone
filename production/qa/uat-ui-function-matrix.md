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
| CLI-1b | CLI | Same unguarded `LoadFromFile` in `SideCommands.cs:20`, `ScenarioPublishCommand.cs:19`, `TimelineCommands.cs:21`, `MissionListCommand.cs:31`, `ScenarioNearFutureSpawnCommand.cs:24`, `EventCommands.cs:197` | S2 | Open (round 3: shared `TryLoad` helper) |
| CLI-5 | CLI | `scenario_migrate_preview` successor IDs differ per process (`string.GetHashCode`) — determinism invariant | S2 | **Fixed** — FNV-1a matching `DeterministicHash.OrdinalHash`; pinned-literal test (RED→GREEN) |
| CLI-5b | Data/CLI | Same bug class: `CmoMarkdownImportProposer.cs:403` (`StringComparer.Ordinal.GetHashCode`) and `MissionPlanSuggestCommand.cs:362` (default idempotency key from `intent.GetHashCode`) | S2 | Open (round 3) |
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

Per-control rows are added from `UatHarness.Inventory()` output when Play Mode is available.
