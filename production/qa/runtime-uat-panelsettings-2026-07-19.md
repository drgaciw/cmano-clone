# Runtime UAT — DelegationSmoke empty Game view (2026-07-19)

## Runtime observation (user Play Mode screenshot)

- Scene: **DelegationSmoke** in Play Mode
- Hierarchy: C2 hosts present (`C2TopBar`, `C2LeftDrawer`, `MapPlaceholder`, `RightUnitDetail`, `MessageLog`, `DoctrineInheritance`, `PlatformCatalog`, `PlatformImport`)
- Game view: **empty sky/ground only** (no C2 chrome)
- Console: MCP setup logs; no C# compile errors in that shot

## Root cause

All **8** scene `UIDocument` components had `m_PanelSettings: {fileID: 0}`.

Without `PanelSettings`, UI Toolkit never composites to the Game view (`rootVisualElement` stays unusable). Hosts + UXML/USS were already wired; presentation layer was effectively unplugged.

## Fix shipped

| Item | Path |
|------|------|
| Runtime bootstrap | `Assets/Scripts/Runtime/UiDocumentPanelSettingsBootstrap.cs` |
| Per-host Awake ensure | All C2 / platform / mission list hosts call `EnsureDocument` |
| Editor assign + asset create | `DelegationSmokeSceneBuilder.FixPanelSettingsBatch` / menu **Project Aegis → Fix UIDocument PanelSettings** |
| Shared asset | `Assets/UI/C2RuntimePanelSettings.asset` |
| Scene rewired | `Assets/Scenes/DelegationSmoke.unity` — 8/8 UIDocuments reference the asset |
| Smoke doc | `PLAYMODE-SMOKE.md` step 10 |

Batch log: `production/qa/panelsettings-fix-batch3.log` — **PanelSettings assigned on 8/8 UIDocument(s)**.

## Verification

| Gate | Result |
|------|--------|
| Fix batch | 8/8 assigned; asset created |
| `C2PlayModeSignoffBatchRunner.RunBatch` | **PASS** (Play Mode entered, 120 sim ticks, no console errors) — `playmode-after-panelsettings.log` |
| `PlayModeSmokeHarnessTests` | **20 passed** |
| Scenario CLI `scenario_validate` golden_clean | **passed / canExport true** exit 0 |
| Mission CLI `mission_list` golden_clean | **ok** — patrol-1 Assigned |
| Cli.Tests filter ScenarioValidate\|MissionList\|MissionBoard | **10 passed** |

## Editor UAT inventory (orchestration)

| Domain | Product path | Play Mode visual | UAT status after fix |
|--------|--------------|------------------|----------------------|
| **Scenario** | Mission Editor CLI + Edit Mode **Scenario Map Authoring** (IMGUI) | Not in Game view | CLI validate green; map window independent |
| **Mission** | CLI mission_* verbs; runtime **MISSIONS** tab only | Mission Board Unity chrome deferred | CLI list green; headless board tests green |
| **Platform** | Play hosts PlatformCatalog / PlatformImport + CLI platform_* | Wired in smoke | Visual unblocked by PanelSettings; bind catalog DB path for full Export/Import UAT |

## Remaining gaps (not this fix)

1. Platform panel `databasePathForExport` / import paths empty in scene — Export/Diff need DB bind for full UAT.
2. Standalone `MissionListPanelHost` not in smoke builder; mission authoring Unity window deferred.
3. OsintStaging host not smoke-wired.
4. Visual UI Toolkit assertion still weak in automated tests (headless harness is projection-level).

## Operator next step

1. Enter Play Mode on **DelegationSmoke** — expect top bar / drawers (not empty sky).
2. Optional platform deep UAT: assign catalog DB on PlatformCatalog/Import hosts, then Export / Propose.
3. Scenario map: **Project Aegis → Scenario Map Authoring**.
