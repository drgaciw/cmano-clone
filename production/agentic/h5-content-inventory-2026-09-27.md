# H5 content inventory — Addressables candidates vs sim-relevant data

| Field | Value |
| --- | --- |
| Linear | [DRG-332](https://linear.app/drgamtd-workspace/issue/DRG-332/h5-content-inventory-classify-addressables-candidates-vs-sim-relevant) |
| Parent | DRG-326 (H5 Content Pipeline / Addressables). Feeds ADR story DRG-331. This file does **not** draft or number an ADR. |
| Main SHA | `1902dc1299678ea17f75014406fd9c616e5576bd` (`1902dc12`, fetched `origin/main` 2026-09-27) |
| Date | 2026-09-27 |
| Kind | Read-only inventory, no code |

Presentation wall used when classifying UI/map/globe: **ADR-010 §2–3** (`docs/architecture/adr-010-headless-first-command-driven-ui.md:47` and `:67`), **ADR-007** (`docs/architecture/adr-007-c2-map-presentation.md:10`), **ADR-001** (`docs/architecture/adr-001-sim-assembly-boundary.md:10`). Git ADR-018 is sensor side-picture / datalink and is not the presentation boundary.

**How counts were measured.** `git ls-tree -r -l HEAD <path>` blob sizes at the SHA above, `*.meta` excluded. File counts are those blobs. No Unity Editor import sizes.

**Classification rule.** Sim-relevant means the bytes feed `SimWorldHash`, a catalog content hash, or a replay golden. Those rows are not Addressables candidates unless a future ADR says otherwise. Presentation-only rows are Addressables candidates only when a player/Unity loader would ship them.

---

## Verified starting facts

- `unity/ProjectAegis/Packages/manifest.json:4` pins `com.unity.addressables` **2.9.1**. Editor pin in `unity/ProjectAegis/ProjectSettings/ProjectVersion.txt:1` is **6000.3.22f1**.
- One committed Addressables descriptor: `unity/ProjectAegis/Assets/Addressables/Map/App6AtlasAddressablesManifest.json:2` group `MapPresentation`, `:5` key `Map/App6FrameAtlas`, `:6` asset `Assets/UI/MapPlaceholder/App6FrameAtlas.png`. Blob size **210** bytes. No `AddressableAssetSettings` asset, no `addressables_content_state.bin`, and no `Assets/StreamingAssets/` directory are in the tree at this SHA.
- Editor wiring: `unity/ProjectAegis/Assets/Editor/App6AddressablesGroupSetup.cs:18` (`EnsureMapPresentationGroup` at `:36`, manifest path `:20`). It calls `UnityEditor.AddressableAssets` (`:8`). That group is created in the Editor; it is not a second committed content blob.
- Headless resolver: `src/ProjectAegis.Delegation/Projection/App6AddressablesCatalog.cs:11`. It reads the JSON manifest with `System.Text.Json` (`:61`). It does not reference `UnityEngine.AddressableAssets`. Contract constants: `src/ProjectAegis.Delegation/Projection/App6AtlasSpriteSheet.cs:21` group, `:23` key, `:25` texture path.
- Consumer: `unity/ProjectAegis/Assets/Scripts/Runtime/MapPlaceholderPanelHost.cs:19`. `preferAddressablesAtlas` `:48`, manifest relative path `:49`, resolve `:553`–`:585` via `App6AddressablesCatalog.TryResolveFromManifest`. If the manifest resolve fails, the host returns `App6AtlasCatalog.Default` (`:566`). Unicode fallback when the atlas frame is missing is `src/ProjectAegis.Delegation/Projection/App6GlyphAtlas.cs:24` (glyph from `App6Sidc.FallbackGlyph` `●` at `src/ProjectAegis.Delegation/Projection/App6Sidc.cs:15`). Story: `production/epics/sprint-27-phase-c-presentation/story-027-07-addressables-app6.md`.

The live texture blob and the production stub are different sizes: Unity `App6FrameAtlas.png` **167** bytes vs `production/assets/c2/App6FrameAtlas.png` **381** bytes. The Addressables entry points at the Unity path (`App6AtlasSpriteSheet.cs:25`), not the production stub.

---

## Inventory table

Loader column is the type plus `path:line` of the read. "Unity host" is the MonoBehaviour that consumes the bytes, or "none" when no host reads them.

| Class | Repo path(s) | Count / size | Classif. | Current loader | Unity host | Addressables? | Hard-no |
| --- | --- | --- | --- | --- | --- | --- | --- |
| APP-6 frame atlas (live entry) | `unity/ProjectAegis/Assets/UI/MapPlaceholder/App6FrameAtlas.png` | 1 file, 167 B | presentation-only | Addressables manifest + headless JSON resolver `App6AddressablesCatalog.cs:40`; host `MapPlaceholderPanelHost.cs:578` | `MapPlaceholderPanelHost` | yes (already the only group) | MapPlaceholderPanelHost |
| Addressables group descriptor | `unity/ProjectAegis/Assets/Addressables/Map/App6AtlasAddressablesManifest.json` | 1 file, 210 B | presentation-only | `File.ReadAllText` `App6AddressablesCatalog.cs:61`; editor `App6AddressablesGroupSetup.cs:116` | `MapPlaceholderPanelHost` | n/a (this is the catalog descriptor, not a packed asset) | MapPlaceholderPanelHost |
| C2 UXML layouts | `unity/ProjectAegis/Assets/UI/**/*.uxml` | 32 files inside the UI tree (68 non-meta files, 135,531 B total, includes USS/png/asset) | presentation-only | `[SerializeField] VisualTreeAsset` e.g. `MapPlaceholderPanelHost.cs:44` | per-panel `*PanelHost` under `unity/ProjectAegis/Assets/Scripts/Runtime/` | yes (local chrome; not bulk) | MapPlaceholderPanelHost for the map UXML only |
| C2 USS styles (player) | `unity/ProjectAegis/Assets/UI/**/*.uss` | 34 files (same UI tree total) | presentation-only | `[SerializeField] StyleSheet` e.g. `MapPlaceholderPanelHost.cs:45` | same hosts | yes (local chrome; not bulk) | MapPlaceholderPanelHost for map USS only |
| UI Toolkit default theme | `unity/ProjectAegis/Assets/UI Toolkit/UnityThemes/UnityDefaultRuntimeTheme.tss` | 1 file (imports `unity-theme://default`) | presentation-only | UI Toolkit theme reference; no custom loader found | panel hosts via PanelSettings | ADR-decides (engine theme, not project art) | none |
| PanelSettings | `unity/ProjectAegis/Assets/UI/C2RuntimePanelSettings.asset` | 1 file inside the UI tree | presentation-only | `UiDocumentPanelSettingsBootstrap.cs:15` path; `:22` can build a runtime instance | every `UIDocument` host | no | none |
| Roboto Mono fonts | `unity/ProjectAegis/Assets/Resources/Fonts/RobotoMono-*.ttf` | 4 files, 361,968 B (Regular 86,908; Bold 87,008; Italic 93,904; BoldItalic 94,148) | presentation-only | Unity `Resources` folder convention. Active USS `resource("Fonts/...")` lines are commented (`production/assets/c2/MessageLogPanel.uss:32`, `unity/ProjectAegis/Assets/UI/MessageLog/MessageLogPanel.uss:82`) | none currently bound; legacy Resources path | ADR-decides (Resources vs Addressables; largest local presentation binary) | none |
| Production C2 USS / token stubs | `production/assets/c2/*.uss` plus `App6FrameAtlas.png` | 11 files, 19,506 B (10 USS + 381 B png) | presentation-only | not the player loader; manifest points asset IDs here (`design/assets/asset-manifest.md:35`) | none (Unity copies live under `Assets/UI`) | no (do not pack a second copy of chrome) | none |
| Production UI USS stubs | `production/assets/ui/` | 2 files, 1,288 B (`MainMenuShell.uss`, `ScenarioSelect.uss`) | presentation-only | asset manifest only (`asset-manifest.md:89`) | none found | ADR-decides (no Unity host yet) | none |
| Production audio stubs | `production/assets/audio/sfx_policy_denial.wav`, `sfx_roe_change.wav` | 2 files, 26,968 B (11,564 + 15,404) | presentation-only | no `AudioClip` / filename reference under `src/` or `unity/ProjectAegis/Assets/Scripts` | none | yes, once a host exists; not wired today | none |
| Baltic framing art | `production/assets/baltic/baltic-theater-framing-v1.png`, `CombatDomainsHotTick.uss`, `ASSET-019-band-b-contact-overlay-spec.md` | 3 files, 17,399 B (png 13,579) | presentation-only | asset manifest (`asset-manifest.md:59`); png not referenced from `src/ProjectAegis.Sim` | none for the png; hot-tick USS has a Unity twin consumed by `CombatDomainsHotTickHost` | ADR-decides for the png (not in the Unity tree) | none |
| Store / press-kit art | `production/assets/store/` | 5 files, 9,348 B (4 png + README) | presentation-only (marketing, not the player) | asset manifest (`asset-manifest.md:72`) | none | no (store page binaries, not player content) | none |
| Design asset register | `design/assets/asset-manifest.md`, `entity-inventory.md`, `approved-criteria-2026-07-14.md`, `design/assets/specs/*.md` | 6 files, 41,538 B. Manifest progress table says **42** assets (`asset-manifest.md:15`); tables list ASSET-001 through ASSET-042 | neither (authoring docs) | human / Path A process, not a runtime loader | none | no | none |
| Scenario policy JSON | `data/scenarios/*.policy.json` | 121 files, 518,138 B (directory total 129 files / 548,030 B) | sim-relevant | `ScenarioPolicyJsonIndex.LoadFromDirectory` `src/ProjectAegis.Data/Scenario/Policy/ScenarioPolicyJsonIndex.cs:16`; default dir `ScenarioDataPaths.cs:6` | none (headless) | no | none (bytes feed ReplayGolden; do not move) |
| Other scenario JSON / schema | `data/scenarios/examples/*.scenario.json` (4), `validation/doctrine-inheritance.json`, `slice-b-combat-acceptance.json`, `scenario-document.schema.json`, `scenario-policy-ids.md` | 8 non-policy files, 29,892 B | ADR-decides (authoring; own manifest hash, not the Baltic golden path) | `AegisScenarioPackage` `src/ProjectAegis.Data/Scenario/Authoring/AegisScenarioPackage.cs:7`; `ManifestBuilder` in `ScenarioManifest.cs:11` hashes a publish payload (`:99`) | none | no | none |
| Authoring / smoke scenario JSON | `assets/data/scenarios/` | 5 files, 17,793 B | ADR-decides (same authoring family) | `AegisScenarioPackage.Read` `:37` when packaged; no `*.aegis-scenario` zip is committed | none | no | none |
| Campaign index | `data/campaigns/baltic-patrol-campaign.campaign.json` | 1 file, 663 B | ADR-decides (names scenario ids; file itself is not hashed) | `ScenarioDataPaths.TryResolveCampaignsDirectory` `ScenarioDataPaths.cs:32` | campaign library UI (projection tests only in this pass) | no | none |
| Catalog SQLite (Baltic) | `assets/data/catalog/baltic_patrol.db` | 1 file, 3,461,120 B (dominant share of `assets/data/catalog` 22 files / 3,491,132 B) | sim-relevant | `CatalogReaderFactory.ResolveBalticPatrolDatabasePath` `CatalogReaderFactory.cs:6`; `ResolveForScenario` `:46`; rows `SqliteCatalogReader.GetSortedSensorBindings` `:46` | none | no | CatalogWriteGate, ReplayGolden |
| Public-corpus SQLite | `assets/data/catalog/aegis_public_corpus.db` | 1 file, 133 B | sim-relevant (catalog DB seam) | `CatalogReaderFactory.ResolvePublicCorpusDatabasePath` `:18` | none | no | CatalogWriteGate |
| Baltic sensor JSON (import source) | `assets/data/catalog/sensors_baltic.json` and `assets/data/catalog/import/sensors_baltic.json` | 494 B and 342 B | sim-relevant | `CatalogJsonImporter.ResolveBalticSensorsJsonPath` `CatalogJsonImporter.cs:129`; seed `CatalogSeedBootstrap.SeedBalticPatrol` `CatalogSeedBootstrap.cs:50` | none | no | CatalogWriteGate |
| Catalog SQL migrations | `assets/data/catalog/migrations/*.sql` | 17 files inside the catalog directory total | sim-relevant schema | applied by `SqliteCatalogReader` open path (`:40` `ApplyMigrations`). Hash column added in `006_snapshot_content_hash.sql:3` | none | no | CatalogWriteGate |
| Loose catalog JSON under `data/catalog` | `sensors_baltic.json` (342 B), `sensor_quarantine_sample.json` (262 B), `speculative_platforms.json` (448 B), `near_future_archetypes.json` (688 B); directory 4 files / 1,740 B | see left | split — see sim-relevance proof | per-file loaders below | none | no | CatalogWriteGate for sensor/quarantine rows |
| Abort-reason glossary | `data/glossary/abort_reason_manifest.json` | 1 file, 5,078 B | sim-relevant | `AbortReasonManifest.LoadFromEmbeddedOrFile` `src/ProjectAegis.Sim/Glossary/AbortReasonManifest.cs:22`; static use `EngagementAbortReasonCodes.cs:12` | none | no | ReplayGolden (codes enter the fingerprint) |
| OSINT fact fixture | `data/osint_facts.json` | 1 file, 518 B | neither (CLI/import fixture) | `CatalogJsonImporter.ResolveRepoRelative` from tests; CLI default `src/ProjectAegis.MissionEditor.Cli/Program.cs:1053` | none | no | none |
| Replay golden text | `tests/regression/replay-golden-*.txt` plus `README.md` | 35 files, 37,603 B | sim-relevant | `ReplayGoldenAssertions.AssertPinnedHashes` `ReplayGoldenAssertions.cs:8`; path `:26`. Pinned v2 hash `17144800277401907079` is present in multiple goldens (not re-read for re-bless) | none | no | ReplayGolden |
| Globe / Cesium tiles | no tile cache, no `com.cesium.unity` in `Packages/manifest.json` | 0 content blobs | presentation-only (remote ion, not in repo) | `CesiumGlobeHost.cs:21` (compiled only with `CESIUM_FOR_UNITY`); status chrome `GlobeTileStreamingHost.cs:20`, `GlobeMapProductHost.cs:25` | `GlobeMapProductHost`, `GlobeTileStreamingHost`, `CesiumGlobeHost` | ADR-decides (remote tiles; do not vendor ion content) | GlobeMapProductHost |
| Smoke scene | `unity/ProjectAegis/Assets/Scenes/DelegationSmoke.unity`, `CESIUM-SPIKE-SETUP.md` | 2 files, 34,099 B (scene 32,942; md 1,157) | presentation-only scene graph | Unity scene load; setup notes `CESIUM-SPIKE-SETUP.md` and `docs/engineering/cesium-unity-package-pin.md:11` | hosts placed in the scene, including map/globe | ADR-decides (scene holds host wiring) | MapPlaceholderPanelHost and GlobeMapProductHost if those components are in the scene |
| QA captures | `production/qa/evidence/`, `production/qa/attachments/` | 71 non-md files (worktree count; not a player catalog) | neither (evidence) | none | none | no | none |

`design/assets/asset-manifest.md:89` and `:98` cite `design/assets/specs/post-s93-residual-assets.md`. That spec file is **not** in `design/assets/` at this SHA (six files only). The USS/WAV stubs it names do exist under `production/assets/ui` and `production/assets/audio`.

Code called out as hard-no and **not** content: `CatalogWriteGate` `src/ProjectAegis.Data/WriteGate/CatalogWriteGate.cs:9`. It was not modified. `DelegationBridge` hotpath was not modified.

---

## Sim-relevance proof

Each row below is why that class is sim-relevant. Presentation rows are omitted here.

### Scenario policy JSON

1. `ScenarioPolicyJsonIndex.EnsureDefaultJsonLoaded` (`ScenarioPolicyJsonIndex.cs:53`) walks to `data/scenarios` (`ScenarioDataPaths.cs:16`) and deserializes every `*.policy.json` (`:29`).
2. `BalticReplayHarness.RunCore` loads that profile (`BalticReplayHarness.cs:133`) and ticks a session whose `Sim.LastWorldHash` is folded by `SimWorldHash.Combine` (`BalticReplayHarness.cs:486` and `:475`; definition `src/ProjectAegis.Sim/Core/SimWorldHash.cs:14`; pipeline write `src/ProjectAegis.Sim/Core/SimTickPipeline.cs:84`).
3. Tests compare that `ulong` to `WORLD_HASH=` lines in `tests/regression/` (`ReplayGoldenAssertions.cs:10`). The v2 pin `17144800277401907079` is stored in those golden files (for example `tests/regression/replay-golden-baltic-engage-2026-06-02.txt`) and in test constants (`src/ProjectAegis.Delegation.UnityAdapter.Tests/Baltic/BalticCombatDomainsPolicyTests.cs:20`). This inventory does not re-bless them.

### Catalog SQLite, sensor JSON, migrations

1. Non-v3 policies resolve `assets/data/catalog/baltic_patrol.db` (`CatalogReaderFactory.cs:6` and `:58`). Baltic v3 policy ids (`baltic-v3-*`) use `InMemoryCatalogReader.BalticV3Fixture()` (`:53`) and do not open that file on the harness path.
2. When the DB is seeded, `CatalogSeedBootstrap.SeedBalticPatrol` (`CatalogSeedBootstrap.cs:52`) imports `assets/data/catalog/sensors_baltic.json` through `CatalogJsonImporter.ImportToSqlite`. Sensor `base_pd` from those rows feeds detection trials in `RunCore` (`BalticReplayHarness.cs:137`), which feeds `DetectionSubhash` inside `SimWorldHash.Combine` (`SimTickPipeline.cs:84`).
3. After write-gate approve, `CatalogSnapshotBinder.BindAfterApprove` (`CatalogSnapshotBinder.cs:16`) hashes sorted sensor rows with `CatalogSnapshotHasher.ComputeSha256Hex` (`CatalogSnapshotHasher.cs:11`, columns at `:21`). The hash is stored on `catalog_snapshot.content_hash_sha256` (`assets/data/catalog/migrations/006_snapshot_content_hash.sql:3`) via `DbSnapshotStore` (`CatalogSnapshotBinder.cs:36`). `UnifiedReleaseTrainManifest.ComputeManifestHash` (`UnifiedReleaseTrainManifest.cs:108`) folds those content hashes into the release-train manifest hash (`:130`).
4. `DbSnapshotStore.RecordApprovedImport` (`DbSnapshotStore.cs:338`) is a separate SHA-256 over approved ids + source file + batch. It is a catalog snapshot id, not `SimWorldHash`.
5. `data/catalog/sensors_baltic.json` (342 B) is **not** the path `ResolveBalticSensorsJsonPath` returns (`CatalogJsonImporter.cs:130` uses `assets/data/catalog/sensors_baltic.json`). Treat the `data/catalog` copy as a sibling fixture, not the seed input, until an ADR says otherwise.

### Near-future archetypes

`BalticReplayHarness.RegisterNearFutureUnits` (`:770`) runs from `RunCore` (`:229`) only when the caller passes a non-empty unit list (`:776`). It then reads `data/catalog/near_future_archetypes.json` (`:805`) and appends `NF_SPAWN:` lines to `DecisionLog` (`:791`). Those lines enter `DecisionLog.ComputeFingerprint` (`DecisionLog.cs:257`) and `OrderLogReplayFingerprint.ComputeSha256Hex` (`OrderLogReplayFingerprint.cs:10`), which goldens pin as `FINGERPRINT_SHA256=` (`ReplayGoldenAssertions.cs:18`). Default pinned cases in `ReplayGoldenRegressionCatalog.cs:14` do not show a near-future argument; the file is still on the harness path when that argument is set.

### Abort-reason glossary

`EngagementAbortReasonCodes` loads the glossary at type init (`EngagementAbortReasonCodes.cs:12`). Log codes are formatted into the order-log fingerprint (`DecisionLog.cs:285` includes `AbortReasonCode`). Goldens pin that fingerprint (`ReplayGoldenAssertions.cs:18`). Changing a code string can change `FINGERPRINT_SHA256` without changing `WORLD_HASH`.

### Replay golden files

They are the expected side of the comparison (`ReplayGoldenAssertions.cs:8`). They are not inputs to `SimWorldHash`; they freeze its outputs. Hard-no: do not edit `tests/regression/` and do not re-bless `17144800277401907079`.

### Not proven on the three hash paths (do not call these sim-relevant yet)

- `data/catalog/speculative_platforms.json` has a loader (`SpeculativePlatformCatalog.LoadFromFile`, `src/ProjectAegis.Sim/Scenario/SpeculativePlatformCatalog.cs:22`). Call sites found are tests only (`ScenarioSpeculativeGateTests.cs:87`, `SpeculativeHonestyPinsTests.cs:81`). No production caller into `RunCore` was found.
- `data/catalog/sensor_quarantine_sample.json` is a quarantine import fixture (`CatalogQuarantineTests`). `CatalogSnapshotHasher` hashes `CatalogSensorBinding` rows (`CatalogSnapshotHasher.cs:11`), not the quarantine table (`CatalogJsonImporter.cs:104`).
- Campaign JSON and scenario-document JSON carry ids and a publish `ManifestHash` (`ScenarioManifest.cs:99`). That hash is not `SimWorldHash` and is not the Baltic replay golden.
- `data/osint_facts.json` is a CLI/test fixture (`Program.cs:1053`). No read into `SimWorldHash` or `CatalogSnapshotHasher` was found.

---

## Assembly check

Search: `UnityEngine.AddressableAssets` under `src/ProjectAegis.Sim`, `src/ProjectAegis.Data`, and `src/ProjectAegis.Delegation` (`*.cs` and `*.csproj`).

| Assembly | Result |
| --- | --- |
| `ProjectAegis.Sim` | no matches. `ProjectAegis.Sim.csproj:8` states no UnityEngine. |
| `ProjectAegis.Data` | no matches. `ProjectAegis.Data.csproj:8` states no UnityEngine. |
| `ProjectAegis.Delegation` | no matches. `App6AddressablesCatalog` is JSON-only (`App6AddressablesCatalog.cs:1`). |

**Violations: none.**

The only `UnityEditor.AddressableAssets` references found are Editor-side: `unity/ProjectAegis/Assets/Editor/App6AddressablesGroupSetup.cs:8`. That file is outside the three headless assemblies.

---

## Doc drift

Live pin is **2.9.1** (`unity/ProjectAegis/Packages/manifest.json:4`). These docs match that pin (not drift):

- `docs/engine-reference/unity/VERSION.md:28` — `com.unity.addressables` 2.9.1. Last-verified note `:56` (2026-08-17).
- `Tech-Stack.md:25` — 2.9.1.
- `AGENTS.md` — no Addressables version string (`rg` on that file returned no `addressables` / `2.3.16` hits).

Stale **2.3.16** (and related) claims. Flagged for a later docs pass. **Not edited in this change.**

| File:line | Claim | Why it is stale |
| --- | --- | --- |
| `.claude/agents/unity-addressables-specialist.md:10` | Addressables **2.3.16**, editor `6000.3.14f1`, "see `Packages/manifest.json`" | Manifest is 2.9.1; `ProjectVersion.txt:1` is 6000.3.22f1 |
| `.claude/agents/unity-specialist.md:14` | Editor `6000.3.14f1` | Project version is 6000.3.22f1 |
| `.claude/agents/unity-specialist.md:17` | Addressables **2.3.16** | Manifest is 2.9.1 |
| `.claude/agents/unity-specialist.md:15` | Entities 1.4.6 / Burst 1.8.29 still listed as the stack | `Tech-Stack.md:31` says Entities packages were removed 2026-07-07; Burst in the manifest is 1.8.30 (`manifest.json:6`). Recorded because it sits on the same stack table as the Addressables pin |
| `docs/engineering/unity-UPGRADE-RUNBOOK.md:490` | from 2.3.16 to target **2.8.1** | Live pin is already 2.9.1, past both numbers |
| `docs/engineering/unity-UPGRADE-RUNBOOK.md:541` | bundles built by 2.3.16 | no committed 2.3.16 catalog; only the JSON manifest above |
| `docs/engineering/unity-UPGRADE-RUNBOOK.md:552` | remote content published against 2.3.16 | no remote Addressables catalog is committed |
| `docs/engineering/unity-remediation-CHANGESET.md:29` and `:108` | documents 2.3.16 → 2.8.1 | historical changeset text; live pin is 2.9.1 |
| `docs/reports/tech-stack-agent-skill-recommendations-2026-07-08.md:16` and `:113` | Addressables 2.3.16; says VERSION.md omits it | VERSION.md now lists 2.9.1 (`VERSION.md:28`) |
| `production/agentic/stacks/sprint27/S27-07-DONE.md:11` | "manifest.json 2.3.16 COVERED" | closeout text; current manifest is 2.9.1 |
| `production/sprints/sprint-90-agent-skill-sync.md:45` | VERSION.md should list Addressables 2.3.16 | conflicts with current VERSION.md |
| `production/qa/qa-plan-sprint-90-agent-skill-sync-2026-07-09.md:94` | checklist expects Addressables **2.3.16** | same |
| `production/sprint-status.yaml:2277` | note `com.unity.addressables 2.3.16` on story 27-7 | story is done; pin has moved. File is out of scope for this change |
| `Game-Requirements/reviews/audit-findings-2026-09-02.md:66` | finding C-09 already records 2.9.1 vs VERSION.md 2.3.16 | the finding's VERSION.md half is outdated (`VERSION.md:28` is 2.9.1); the manifest half matches |

Other `addressables` hits under `.claude/` (`team-unity`, `agent-roster`, `quick-start`, `setup-engine`, `c-sharp-architect`) name the specialist. They do not state a package version.

---

## Open questions for DRG-331

No ADR is numbered or drafted here.

1. **Local vs remote.** The only committed Addressables artifact is a local JSON descriptor for one texture. There is no remote catalog, no content-state bin, and no `StreamingAssets` tree. Cesium terrain is specified as ion streaming (`CesiumGlobeHost.cs:4`, `docs/engineering/cesium-unity-package-pin.md:12`) and is not in the manifest. DRG-331 needs an explicit local-only vs remote-catalog choice before any second group is added. Ion tokens stay out of the repo.
2. **What is actually bulk.** At this SHA the largest presentation binaries are the four fonts (362 KB) and small store/Baltic PNGs (under 14 KB each). UXML+USS together sit inside a 136 KB UI folder. Audio is two short WAVs with no player loader. There is no texture library, audio bank, or tile cache that would justify a remote Addressables catalog today. "Bulk" is a future class, not a current folder.
3. **Canonical APP-6 PNG.** Production stub is 381 B; the Addressables texture path is 167 B. Which file is the art source of truth before a group rebuild?
4. **Duplicate chrome.** Thirteen production USS files overlap names with `Assets/UI` (for example `MessageLogPanel.uss`, `AegisTokens.uss`, `CombatDomainsHotTick.uss`). Packing `production/assets` and `Assets/UI` would ship two style trees. Player loaders bind the Unity assets (`SerializeField`), not the production copies.
5. **Fonts.** Files remain under `Resources/Fonts` while USS `resource("Fonts/RobotoMono-Regular")` is commented out to avoid console errors. Moving them to Addressables without a host that requests the key does not change the running UI.
6. **Sim-relevant exclusion list for any future group.** Do not mark Addressables: `data/scenarios/*.policy.json`, `assets/data/catalog/baltic_patrol.db`, `assets/data/catalog/sensors_baltic.json`, `assets/data/catalog/migrations/`, `data/glossary/abort_reason_manifest.json`, `data/catalog/near_future_archetypes.json` (harness path), `tests/regression/replay-golden-*.txt`. Catalog writes stay on `CatalogWriteGate` (extend-only). Replay goldens stay put.
7. **Scenes and globe.** `DelegationSmoke.unity` is a host scene, not a data catalog. Globe tiles are remote and the globe hosts are hard-no. DRG-331 should say whether scenes are Addressables at all.
8. **Missing spec path.** Manifest rows for ASSET-036/037/040/041 point at `design/assets/specs/post-s93-residual-assets.md`, which is absent. That is a register gap, not a packaging decision.

---

## Unverified / inferred

- Whether `DelegationSmoke.unity` currently serializes `MapPlaceholderPanelHost` or `GlobeMapProductHost` was not opened as YAML (hard-no hosts, read-only intent). The scene is listed as ADR-decides because those types exist and the scene is the smoke scene (`CESIUM-SPIKE-SETUP.md` says the default smoke scene is not the Cesium spike).
- `SpeculativePlatformCatalog` is inferred **not** on the golden path because the only `LoadFromFile` callers found are tests. A caller outside `src/` (Unity script, tool) was not searched beyond `src/` and `unity/ProjectAegis/Assets/Scripts`.
- QA capture count (71) is a worktree file count of non-md files under `production/qa/evidence` and `production/qa/attachments`. It is evidence volume, not a git blob sum, and it is not player content.
- Gauntlet `results.csv` trees under `production/qa/gauntlet/` were not row-counted. They are run evidence, not an Addressables class.
- No claim is made that the 167 B and 381 B APP-6 PNGs differ in pixels, only that their blob sizes differ.
- `dotnet test` was not run. No test totals are claimed.

## Incomplete / not yet checked

- Per-file diff of each production USS against its `Assets/UI` twin (names overlap; contents were not hashed against each other).
- Full call graph of `ManifestBuilder` publish hashes into CLI output files on disk (no committed `*.aegis-scenario` packages were found).
- Editor `AddressableAssetSettings` that might exist only in a local Unity `Library/` (not in git). The repo itself has no settings asset.
