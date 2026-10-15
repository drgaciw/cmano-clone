# ADR-028: H5 Content Pipeline: Addressables for Local, Presentation-Only Content

> Prepared for [DRG-331](https://linear.app/drgamtd-workspace/issue/DRG-331/h5-draft-content-pipeline-addressables-adr). Owner answered all Open Questions on 2026-10-07 00:10 CT (see Owner Decisions). Lands as **Accepted** (2026-10-15). The DRG-347 pilot is unblocked by this ADR.

## Status

**Accepted** (2026-10-15, by owner). Previously Proposed; landed via PR #706.

## Date

2026-10-06

## Last Verified

2026-10-06. Checked against `main` `434b3cf6cf0dc64ef285d47e6f78857779369636` (#693, ADR-017 re-land). The inventory facts come from `1902dc12` and were re-checked on 2026-10-01 (`production/agentic/h5-content-inventory-review-2026-10-01.md`). No fresh `dotnet test`, Unity Editor run or Player build backs this draft.

## Decision Makers

Owner / Technical Director (DRGAMTD), the only developer. **This ADR is Accepted** (2026-10-15, by owner; previously Proposed per Owner Decision 7). Merging this PR unblocks the pilot and closes the H5 entry gate for DRG-347.

## Summary

Project Aegis adopts Unity Addressables only for **presentation-only content**, and only in a **local catalog shipped inside the Player build**. All Addressables runtime APIs sit behind a new presentation-owned loader seam inside Unity presentation assemblies. Sim-relevant data keeps reaching the simulation through the existing `ProjectAegis.Data` / `ProjectAegis.Sim` loaders and hashes and never becomes Addressables content. Remote/CDN delivery, DLC and catalog updates are out of scope and need a later ADR. The first slice is the existing APP-6 frame atlas key `Map/App6FrameAtlas`, delivered by DRG-347.

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6.3 LTS, Editor `6000.3.22f1` (`unity/ProjectAegis/ProjectSettings/ProjectVersion.txt:1`, per inventory) |
| **Package** | `com.unity.addressables` **2.9.1** (`unity/ProjectAegis/Packages/manifest.json:4`) |
| **Domain** | Presentation / content pipeline |
| **Knowledge Risk** | MEDIUM. Async handle lifetime, Player fallback and build/catalog behaviour on 2.9.1 are not proven in this repo (`h5-content-inventory-review-2026-10-01.md:23`) |
| **References Consulted** | ADR-001, ADR-007, ADR-010, ADR-017 (format), ADR-027; `production/agentic/h5-content-inventory-2026-09-27.md`; `production/agentic/h5-content-inventory-review-2026-10-01.md`; `.claude/agents/unity-addressables-specialist.md` |
| **Post-Cutoff APIs Used** | Addressables 2.x `LoadAssetAsync` / `Release` (pilot only) |
| **Verification Required** | Local Editor + Player build for the pilot (see Build and CI) |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | [ADR-001 Sim Assembly Boundary](adr-001-sim-assembly-boundary.md); [ADR-007 C2 Map Presentation](adr-007-c2-map-presentation.md); [ADR-010 Headless-First Command-Driven UI](adr-010-headless-first-command-driven-ui.md) |
| **Enables** | DRG-347 H5 local presentation pilot; proposed LIB-05.1…3 (`docs/superpowers/specs/2026-09-30-requirements-planning-reconciliation.md:24`) |
| **Blocks** | Any second Addressables group, bulk import or remote catalog (now unblocked by Acceptance) |
| **Ordering Note** | DRG-332 inventory (Done) → this ADR Accepted and merges (pilot unblock) → DRG-347 pilot → per-class expansion by amendment. Remote delivery would be a separate ADR after H3/Launch scope opens |

## Context

### Problem Statement

H5 cannot open without an accepted content-pipeline ADR (`future-sprint-roadmap-07142026.md:100` "Addressables / remote content design undesigned"; `:155` bulk import out of scope until that ADR exists). The package is installed and one atlas key is declared, but nothing loads through the Addressables runtime yet. Bulk use could pull sim-relevant bytes into a Unity-owned pipeline and move the replay hash. It could also grow the two hard-no hosts.

### Current State (as of `main` 434b3cf6)

- **One descriptor, no runtime load.** `unity/ProjectAegis/Assets/Addressables/Map/App6AtlasAddressablesManifest.json` declares group `MapPresentation`, key `Map/App6FrameAtlas` and asset `Assets/UI/MapPlaceholder/App6FrameAtlas.png`. The Editor helper `Assets/Editor/App6AddressablesGroupSetup.cs` builds the group. **No `AddressableAssetSettings`, no `addressables_content_state.bin` and no `Assets/StreamingAssets/` are committed** (inventory §Verified starting facts).
- **The consumer resolves metadata only.** `MapPlaceholderPanelHost.TryResolveAddressablesAtlas` (`Assets/Scripts/Runtime/MapPlaceholderPanelHost.cs:569–585`) reads the JSON from `Application.dataPath` through the headless `App6AddressablesCatalog.TryResolveFromManifest` (`src/ProjectAegis.Delegation/Projection/App6AddressablesCatalog.cs`). On failure, `ResolveAtlasCatalog` (`:553–567`) falls back to `App6AtlasCatalog.Default`, the in-memory USS frame registry (`App6AtlasCatalog.cs:6`). The Unicode fallback `●` lives in `App6Sidc.cs:15` / `App6GlyphAtlas.cs`. No `Addressables.` / `LoadAssetAsync` call exists in Runtime scripts (review `:23`). *Inferred, not verified:* a Player build does not ship `Assets/Addressables/**.json` under `Application.dataPath`, so a built Player probably always takes the fallback path.
- **Assembly wiring.** Only `ProjectAegis.Unity.Editor.asmdef` references `Unity.Addressables` / `Unity.Addressables.Editor`. `ProjectAegis.Unity.Runtime.asmdef` has `"references": []`. Headless assemblies have zero `UnityEngine.AddressableAssets` references (inventory §Assembly check: Violations none).
- **Content volume is small.** The largest presentation binaries are the four Roboto Mono fonts (361,968 B, `Assets/Resources/Fonts/`, no bound consumer). The whole UI tree is 135,531 B. Audio is two WAV stubs (26,968 B) with no loader. There are no globe tiles in the repo (Cesium ion is remote and not in the manifest). "Bulk" is a future class, not a current folder (inventory Q2).
- **Sim-relevant content is large and hashed.** Scenario policies (`data/scenarios/*.policy.json`, 121 files) feed `BalticReplayHarness` → `SimWorldHash`. Catalog SQLite (`assets/data/catalog/baltic_patrol.db`, 3.46 MB; `aegis_public_corpus.db`, 68.4 MB LFS), `sensors_baltic.json` and migrations feed detection and `CatalogSnapshotHasher` / `UnifiedReleaseTrainManifest`. The abort-reason glossary and `near_future_archetypes.json` feed the order-log fingerprint (inventory §Sim-relevance proof).
- **Doc drift.** `.claude/agents/unity-addressables-specialist.md:10` still says Addressables 2.3.16 / Editor 6000.3.14f1. `.github/workflows/unity-ci.yml:63` pins `unityVersion: 6000.3.14f1` against ProjectVersion `6000.3.22f1`.

### Constraints

- **ADR-010 §1, §4:** `ProjectAegis.Data`, `.Sim` and `.Delegation` stay pure .NET with no `UnityEngine`. Unity-specific execution stays behind seams in `ProjectAegis.Unity`.
- **ADR-010 §2–3:** UI is a client. It must not store authoritative state in `ScriptableObject`s or scene objects, and projection DTOs do not take part in replay hashes.
- **ADR-007:** map UI never writes to `DecisionLog` or the sim world. Placeholder and globe share `MapSymbolEntry`. APP-6 is "data-driven USS + icon atlas" (Phase C). A future ADR pins Cesium (Phase B).
- **ADR-001:** Sim owns world truth. Delegation consumes snapshots and emits orders.
- **ADR-027:** the Unity Editor MCP bridge is dev-only. Headless `dotnet test` stays the verification authority.
- **Invariants** (DRG-326; `production/test-floor.json`): `balticV2Hash` `17144800277401907079`, ReplayGolden ≥ 6, solution floor ≥ 1638, `CatalogWriteGate` extend-only, no `DelegationBridge` hotpath edits, Stage Release.
- **Specialist rule:** "Addressable load timing must not feed sim RNG or policy; presentation-only". Core sim DLLs are not Addressables content (`.claude/agents/unity-addressables-specialist.md`, invariants table).

### Requirements

- Proposed LIB-05.1…3: missing, delayed, corrupt or incompatible presentation assets pick a documented fallback and preserve sim truth, orders, RNG and replay fingerprints, measured against an agreed pilot budget (reconciliation spec `:24`).
- DRG-347: one local presentation-only pilot. It declares the Unity/package version and resource ownership, shows a visible fallback with bounded diagnostics, keeps world hash, order log and replay fingerprint identical across success and failure, and releases handles on replacement and disposal. No CDN, no catalog DB write, no sim decisions, no DLC.

## Decision Drivers

1. **Determinism and replay are untouchable.** The Baltic v2 hash `17144800277401907079` and ReplayGolden 6/6 must not change. No golden re-bless, silent or otherwise.
2. **Sim-relevant data stays out of Addressables**, per the inventory classification rule ("bytes feed `SimWorldHash`, a catalog content hash, or a replay golden" ⇒ not a candidate).
3. **The presentation wall holds** (ADR-010/007/001). Addressables APIs appear only in Unity presentation assemblies.
4. **`CatalogWriteGate` stays the only catalog write path.**
5. **Hard-no hosts don't grow.** `MapPlaceholderPanelHost` (608 lines) and `GlobeMapProductHost` (379 lines) are not the place for bulk loading.
6. **Headless-first verification.** What CI can prove without Unity must be proven there. Editor/Player-only proof is named explicitly.
7. **Proportionality.** Current presentation content is below 1 MB, so the design must not pay for a remote pipeline it doesn't need.

## Decision

**Accepted: Option A, Addressables for local, presentation-only content behind a presentation-owned loader seam.**

1. **Scope.** Only rows classified *presentation-only* in the DRG-332 inventory may become Addressables entries. They are admitted one class at a time, by amendment to this ADR. Everything classified *sim-relevant* or *ADR-decides (authoring)* is excluded (table below).
2. **Local only for Release.** A local Addressables catalog is built into the Player. No remote load paths, no remote catalog, no content-update workflow and no CDN. Remote/DLC needs a new ADR, which is Launch-adjacent and blocked while H3 is closed.
3. **Presentation wall.**
   - `UnityEngine.AddressableAssets` / `UnityEngine.ResourceManagement` may be referenced only by a **new presentation asmdef** (working name `ProjectAegis.Unity.Runtime.Content`) and by `ProjectAegis.Unity.Editor`.
   - `ProjectAegis.Sim`, `.Data`, `.Delegation` and `.Delegation.UnityAdapter` never reference them.
   - Headless resolvers such as `App6AddressablesCatalog` stay JSON-only metadata contracts.
4. **Loader seam.** The new assembly owns a single presentation content loader with these properties:
   - It caches one handle per key while views hold leases. It releases on the final lease and on disposal.
   - It cancels stale requests on scene teardown and rejects late callbacks.
   - It reports each outcome (`Loaded` / `Missing` / `Corrupt` / `Delayed` / `Canceled` / `Mismatched`) to the caller, which chooses the documented fallback and emits a bounded diagnostic.
   - It never blocks the main thread, and no sim tick waits on it.

   This lifetime model is the one proposed in `h5-content-inventory-review-2026-10-01.md:31`. Bulk loading never goes into `MapPlaceholderPanelHost` or `GlobeMapProductHost`. `MapPlaceholderPanelHost` is hard-no and is not edited (Owner Decision 3; the Open Question 3 waiver was denied).

   `IApp6AtlasAvailability` stays a headless metadata contract (`IsLoaded`, `HasFrame`, `TryGetSpriteSlice`). It does not carry a texture, so it is not what gets rendered. DRG-347 adds two injectable contracts in the new content assembly. Neither is implemented on the host, and neither uses reflection into it:

   - **`IPresentationContentLoader`**, injected into the adapter. `Load(key)` returns a `ContentLoadResult`: the outcome (`Loaded` / `Missing` / `Corrupt` / `Delayed` / `Canceled` / `Mismatched`) and, only on `Loaded`, the `Texture2D` (or sprites) for `Map/App6FrameAtlas`. Tests inject a fake loader.
   - **`IApp6FrameElementSource`**, the injectable host seam a zero-edit adapter needs. It yields the frame `VisualElement`s to paint. The production source queries the map canvas for elements that already carry the USS classes `map-app6-frame--*` (`MapSymbolPool` adds those classes today and is not edited). Tests pass an element list. The adapter never reads `MapPlaceholderPanelHost` private state (`useApp6AtlasFrames`, `preferAddressablesAtlas`, `ResolveAtlasCatalog`, `TryResolveAddressablesAtlas`).

   `App6AtlasAddressablesAdapter` (new component under `Assets/Scripts/Runtime/Content/`) is the only consumer. When the source reports frame elements, the adapter sets each frame's `style.backgroundImage` from the loaded texture and the existing slice offsets. That assignment is what UI Toolkit renders. On `Missing`, `Corrupt`, `Mismatched`, or `Canceled` it does not assign a texture, and the frame falls back to `App6AtlasCatalog.Default` / the Unicode glyph.
5. **Determinism.** Sim-relevant bytes keep their current loaders (table below). Addressables outcomes never flow into a sim, order-log, RNG or replay input. The Baltic hash stays preserved by construction, and verification is described under Validation Criteria. No ReplayGolden file under `tests/regression/` is edited or re-blessed by any H5 change.
6. **`CatalogWriteGate`.** No catalog DB, JSON import source, migration or snapshot is ever an Addressables entry. Addressables is never a write path into the catalog.
7. **First slice.** Load the existing key `Map/App6FrameAtlas` through the seam, delivered by DRG-347. Surface: new `unity/ProjectAegis/Assets/Scripts/Runtime/Content/` (+ asmdef, including `App6AtlasAddressablesAdapter`, `IPresentationContentLoader`, and `IApp6FrameElementSource`), `unity/ProjectAegis/Assets/UI/MapPlaceholder/MapPlaceholderPanel.uss` (remove the direct `App6FrameAtlas.png` `background-image` urls), the committed `AddressableAssetSettings` under `unity/ProjectAegis/Assets/AddressableAssetsData/`, and headless guard tests under `src/` test projects. Host integration is via that separate adapter; `MapPlaceholderPanelHost` is not edited (the Open Question 3 waiver was denied, see Owner Decisions).
8. **Package.** Keep `com.unity.addressables` pinned at 2.9.1. `.claude/agents/unity-addressables-specialist.md` still references Addressables 2.3.16, and `.github/workflows/unity-ci.yml` still pins Editor `6000.3.14f1` (see Doc drift above). This docs-only ADR does not correct either pin. Both remain and are tracked separately from this PR.

### Content-class table

| Content class | Repo path(s) | Classification | Addressables? | Owner (layer) | Loader (current → decided) | Hard-no impact |
|---|---|---|---|---|---|---|
| APP-6 frame atlas | `unity/ProjectAegis/Assets/UI/MapPlaceholder/App6FrameAtlas.png` + `Assets/Addressables/Map/App6AtlasAddressablesManifest.json` + frame rules in `MapPlaceholderPanel.uss` | presentation-only | **Yes, first slice** | Presentation (`ProjectAegis.Unity.Runtime.Content`) | Headless JSON metadata stays on `App6AddressablesCatalog`. Runtime: injected `IPresentationContentLoader` returns the `Texture2D` on `Loaded`; `App6AtlasAddressablesAdapter` sets `style.backgroundImage`. USS frame rules do not `url()` the PNG. Fallback: `App6AtlasCatalog.Default` / Unicode | `MapPlaceholderPanelHost` not edited (waiver denied). Adapter uses `IApp6FrameElementSource` |
| C2 UXML / USS | `unity/ProjectAegis/Assets/UI/**` (32 UXML, 34 USS, 135,531 B) | presentation-only | Not now (candidate by amendment) | Presentation panel hosts | `[SerializeField]` direct refs (unchanged), except `MapPlaceholderPanel.uss` frame rules, which drop the direct `App6FrameAtlas.png` url in the first slice. Those USS files are not Addressables entries | None |
| UI Toolkit theme / PanelSettings | `Assets/UI Toolkit/UnityThemes/…tss`, `Assets/UI/C2RuntimePanelSettings.asset` | presentation-only | No | Presentation | Engine theme / `UiDocumentPanelSettingsBootstrap` (unchanged) | None |
| Fonts | `Assets/Resources/Fonts/RobotoMono-*.ttf` (361,968 B) | presentation-only | Not now (stay in `Resources`; OQ6) | Presentation | `Resources` convention, no bound consumer | None |
| Audio | `production/assets/audio/*.wav` (2 stubs) | presentation-only | Candidate once a host exists | Presentation | None today | None |
| Production art / USS stubs, store art | `production/assets/{c2,ui,baltic,store}/` | presentation-only (authoring / marketing) | No (avoids a duplicate style tree; store art is not player content) | Art pipeline (`design/assets/asset-manifest.md`) | Not loaded by the Player | None |
| Globe / basemap tiles | none in repo; Cesium ion remote (`CesiumGlobeHost.cs`, `docs/engineering/cesium-unity-package-pin.md`) | presentation-only (remote) | **No.** Stays a Cesium streaming concern under ADR-007 Phase B | Presentation (Cesium asmdef) | Cesium ion, `GlobeTileStreamingHost` status | `GlobeMapProductHost`: untouched |
| Scenes | `Assets/Scenes/DelegationSmoke.unity` | presentation scene graph | No | Presentation | Unity scene load (unchanged) | `MapPlaceholderPanelHost` (scene wiring): untouched |
| Scenario policy JSON | `data/scenarios/*.policy.json` | **sim-relevant** | **No** | Data / Sim | `ScenarioPolicyJsonIndex` (unchanged) | ReplayGolden |
| Scenario packages / authoring JSON / campaigns | `AegisScenarioPackage.cs`, `data/scenarios/examples`, `assets/data/scenarios/`, `data/campaigns/` | authoring (own `ManifestHash`) | **No** | Data (ADR-017 shared core) | `AegisScenarioPackage` / `ScenarioDataPaths` (unchanged) | None |
| Catalog SQLite, sensor JSON, migrations, snapshots | `assets/data/catalog/**`, `src/ProjectAegis.Data/Snapshots/` | **sim-relevant** | **No** | Data | `CatalogReaderFactory`, `CatalogJsonImporter`, `CatalogSnapshotHasher`, `UnifiedReleaseTrainManifest` (unchanged) | `CatalogWriteGate`, ReplayGolden |
| Glossary, near-future archetypes | `data/glossary/abort_reason_manifest.json`, `data/catalog/near_future_archetypes.json` | **sim-relevant** (fingerprint) | **No** | Sim | `AbortReasonManifest`, `BalticReplayHarness` (unchanged) | ReplayGolden |
| Replay goldens | `tests/regression/replay-golden-*.txt` | sim-relevant (expected outputs) | **No**. Never edited by H5 | Tests | `ReplayGoldenAssertions` | ReplayGolden |
| Core sim DLLs | Unity plugin copies of headless assemblies | code | **No** (`unity-addressables-specialist.md`) | Build tooling | `tools/copy-delegation-assemblies.ps1` | `DelegationBridge` zero-touch |

### Hard-no impact summary

| Host / gate / golden | Impact under Option A |
|---|---|
| `MapPlaceholderPanelHost` | None. Waiver denied (Owner Decision 3). The host stays unedited, including `ResolveAtlasCatalog` and `TryResolveAddressablesAtlas`. The adapter does not replace that body; it binds through `IApp6FrameElementSource` |
| `GlobeMapProductHost` | None. Globe tiles are excluded |
| `DelegationBridge` hotpath | None |
| `CatalogWriteGate` | None. No catalog content in any group. Enforced by a headless guard over the JSON descriptors and the committed `AddressableAssetSettings` and groups (see Build and CI) |
| ReplayGolden / `17144800277401907079` | None. No golden re-bless. Addressables outcomes never enter a hash input |

## Alternatives Considered

### Alternative 1: No Addressables; keep the current JSON + USS fallback only

- **Description**: Do not add a runtime loader. The host continues to resolve metadata from the JSON descriptor and falls back to `App6AtlasCatalog.Default` / Unicode.
- **Pros**: Zero new code, zero risk to determinism, no package surface.
- **Cons**: Does not exercise Addressables. Does not prove the presentation wall, handle lifetime, or fallback diagnostics. Leaves the H5 content-pipeline gate closed.
- **Rejection Reason**: It does not meet DRG-347 / LIB-05 acceptance. It stays the **fallback** that Option A degrades to.

### Alternative 2: Addressables for everything, including sim-relevant data

- **Description**: Move scenario policies, catalog JSON, or sensor data into Addressables groups.
- **Pros**: One content pipeline.
- **Cons**: Pulls sim-relevant bytes into a Unity-owned pipeline. Risks moving `SimWorldHash` and ReplayGolden. Violates the inventory classification rule and ADR-001/010.
- **Rejection Reason**: Determinism is untouchable. Sim-relevant content stays on its current loaders.

### Alternative 3: Remote catalog / CDN from day one

- **Description**: Ship a remote Addressables catalog and content-update workflow in H5.
- **Pros**: Matches long-term DLC ambition.
- **Cons**: Current presentation content is under 1 MB. Remote adds catalog versioning, CDN auth, and update-flow risk for no present need. Blocked while H3 is closed.
- **Rejection Reason**: Disproportionate. Remote/DLC is a later ADR.

## Consequences

### Positive

- Presentation-only content can be loaded asynchronously with a documented fallback, without touching sim truth.
- The presentation wall is explicit: Addressables APIs live only in a new presentation assembly.
- The first slice is small and reversible. A failed load degrades to the existing Unicode / default-atlas path.
- Headless CI can still prove the assembly boundary and the settings allow-list.

### Negative

- A new assembly and two injectable contracts add surface.
- Addressables 2.9.1 async lifetime and Player catalog behaviour are not yet proven in this repo.
- Doc drift (specialist agent version, unity-ci Editor pin) remains and is tracked separately.

### Neutral

- Remote/DLC stays out of scope until a later ADR.
- `MapPlaceholderPanelHost` and `GlobeMapProductHost` are untouched.

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|-----------|
| Addressables handle leak or late callback after teardown | Medium | Medium | Loader owns lease counting and cancels on dispose. DRG-347 measures leaked handles. |
| Player build does not ship the local catalog | Medium | Medium | DRG-347 includes an Editor + Player verification step. Fallback path is the current default. |
| Accidental sim-relevant entry in a group | Low | High | Headless guard over `AddressableAssetSettings` and groups fails CI on any entry outside the allow-list. |
| USS still hardcodes the PNG url | Low | Low | First slice removes the direct `background-image` urls from frame rules. |

## Migration Plan

Additive. First slice (DRG-347) adds the content assembly, the adapter, the two contracts, the committed `AddressableAssetSettings`, and the USS url removal. No edits to `MapPlaceholderPanelHost`. Rollback is deletion of the new assembly and reversion of the USS change.

## Build and CI

| Gate | Owner | Notes |
|------|-------|-------|
| Headless assembly guard (no Addressables refs outside presentation) | CI | New test under `src/` |
| Settings / group allow-list guard | CI | Fails if any entry is outside the presentation-only allow-list |
| ReplayGolden 6/6 and `balticV2Hash` unchanged, with zero edits under `tests/regression/` | CI | No re-bless |
| Budget profiling (latency, frame cost, memory, handle leaks) | DRG-347 | Local Editor + Player |
| Fallback-selection and diagnostic policy as pure C# (outcome → fallback), unit-tested | CI | |

`unity-ci.yml` is manual and needs the `UNITY_LICENSE` secret (`.github/workflows/unity-ci.yml:1–39`). It is not a merge gate for this work.

## Validation Criteria

- [x] This ADR is Accepted (2026-10-15). Merging it unblocks the pilot and closes the H5 gate.
- [ ] Content-class table with owner and loader for each class (above).
- [ ] Hard-no impact stated per host, gate and golden (above). `MapPlaceholderPanelHost` has zero edits.
- [ ] First slice named with a non-`TBD` Surface (Decision §7), including the injectable `IApp6FrameElementSource` and the USS url removal.
- [ ] DRG-347: same seed and scenario give identical world hash, order-log fingerprint and replay fingerprint across asset success, missing, corrupt, delayed, canceled, and mismatched (wrong asset type or version). Player fallback proof includes the mismatched case.
- [ ] Headless assembly guard passes, and the settings guard fails when `AddressableAssetSettings` or a group contains an entry outside the allow-list. ReplayGolden 6/6. Hash `17144800277401907079` unchanged with no re-bless.
- [ ] USS frame rules do not `url()` `App6FrameAtlas.png`. The loaded Addressable texture is what each frame renders.

## GDD / Requirements Addressed

| Doc | Requirement | How this ADR satisfies it |
|---|---|---|
| Draft 27 Scenario Library & Campaigns (`Game-Requirements/drafts/27-Scenario-Library-Campaigns.md`) | Proposed LIB-05.1…3 content failure isolation | Seam outcomes → documented fallback; authoritative fingerprints are unaffected by construction and tested in DRG-347 |
| Roadmap §14 (`future-sprint-roadmap-07142026.md:180`) | "Addressables bulk: content pipeline ADR accepted" | This ADR is Accepted (2026-10-15). Merging it unblocks the pilot. |
| ADR-007 Phase C | APP-6 data-driven USS + icon atlas | Atlas is the first loaded class. USS frame rules no longer hardcode the PNG; the loaded texture is bound at runtime. The USS frame registry remains the metadata fallback |

## Owner Decisions (2026-10-07 00:10 CT, drg amtd)

The owner accepted all of CMANO's proposed answers:

1. **Scope:** presentation-only only. Sim-relevant and authoring content stay on current loaders.
2. **Local only:** no remote catalog, no CDN, no content-update workflow in H5.
3. **Host waiver:** denied. `MapPlaceholderPanelHost` is not edited. The adapter binds through `IApp6FrameElementSource`.
4. **Budgets:** no synchronous main-thread wait, p99 loader frame overhead ≤ 2 ms, cold readiness ≤ 250 ms, incremental memory ≤ 16 MiB, zero leaked handles after repeated teardown, measured on the laptop dev build.
5. **Package:** Pin Addressables 2.9.1 and commit `AddressableAssetSettings` in DRG-347. The specialist-doc 2.3.16 drift and the `unity-ci.yml` `6000.3.14f1` Editor pin are not part of that decision and are not corrected here; both remain and are tracked separately (see Decision §8).
6. **First slice:** the existing `Map/App6FrameAtlas` key only.
7. **Landing:** Land this ADR as Proposed in a docs-only PR now; hold the pilot (DRG-326/DRG-347) until it merges. (Subsequently Accepted 2026-10-15.)

## Open Questions (answered 2026-10-07)

1. **Which classes are in scope?** Presentation-only only.
2. **Remote in H5?** No.
3. **May we edit `MapPlaceholderPanelHost`?** No (waiver denied).
4. **What are the pilot budgets?** See Owner Decision 4.
5. **Which package version?** 2.9.1.
6. **Fonts?** Stay in `Resources` for now.
7. **H5 GO:** May this draft land as Proposed at `docs/architecture/adr-028-content-pipeline-addressables.md` now, or does it wait for the H5 GO? Landed as Proposed; later Accepted.
