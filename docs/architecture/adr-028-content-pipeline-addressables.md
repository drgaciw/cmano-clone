# ADR-028: H5 Content Pipeline: Addressables for Local, Presentation-Only Content

> Prepared for [DRG-331](https://linear.app/drgamtd-workspace/issue/DRG-331/h5-draft-content-pipeline-addressables-adr). Owner answered all Open Questions on 2026-10-07 00:10 CT (see Owner Decisions). Lands as **Proposed**; the DRG-347 pilot stays held until this ADR merges.

## Status

**Proposed**

## Date

2026-10-06

## Last Verified

2026-10-06. Checked against `main` `434b3cf6cf0dc64ef285d47e6f78857779369636` (#693, ADR-017 re-land). The inventory facts come from `1902dc12` and were re-checked on 2026-10-01 (`production/agentic/h5-content-inventory-review-2026-10-01.md`). No fresh `dotnet test`, Unity Editor run or Player build backs this draft.

## Decision Makers

Owner / Technical Director (DRGAMTD), the only developer. **The resolution owner must decide to Accept.** Accepting this ADR closes the H5 entry gate "Content pipeline ADR accepted" (`docs/reports/future-sprint-roadmap-07142026.md:180`; DRG-326 acceptance criteria).

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
| **Blocks** | Any second Addressables group, bulk import or remote catalog until this ADR is Accepted |
| **Ordering Note** | DRG-332 inventory (Done) → this ADR Accepted → DRG-347 pilot → per-class expansion by amendment. Remote delivery would be a separate ADR after H3/Launch scope opens |

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

**Proposed: Option A, Addressables for local, presentation-only content behind a presentation-owned loader seam.**

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

   This lifetime model is the one proposed in `h5-content-inventory-review-2026-10-01.md:31`. Hosts consume an interface such as the existing `IApp6AtlasAvailability`. Bulk loading never goes into `MapPlaceholderPanelHost` or `GlobeMapProductHost`.
5. **Determinism.** Sim-relevant bytes keep their current loaders (table below). Addressables outcomes never flow into a sim, order-log, RNG or replay input. The Baltic hash stays preserved by construction, and verification is described under Validation Criteria. No ReplayGolden file under `tests/regression/` is edited or re-blessed by any H5 change.
6. **`CatalogWriteGate`.** No catalog DB, JSON import source, migration or snapshot is ever an Addressables entry. Addressables is never a write path into the catalog.
7. **First slice.** Load the existing key `Map/App6FrameAtlas` through the seam, delivered by DRG-347. Surface: new `unity/ProjectAegis/Assets/Scripts/Runtime/Content/` (+ asmdef), the committed `AddressableAssetSettings` under `unity/ProjectAegis/Assets/AddressableAssetsData/`, and headless guard tests under `src/` test projects. The one-line host binding needs the waiver in Open Question 3.
8. **Package.** Keep `com.unity.addressables` pinned at 2.9.1. The ADR PR corrects the 2.3.16 drift in `.claude/agents/unity-addressables-specialist.md`.

### Content-class table

| Content class | Repo path(s) | Classification | Addressables? | Owner (layer) | Loader (current → decided) | Hard-no impact |
|---|---|---|---|---|---|---|
| APP-6 frame atlas | `unity/ProjectAegis/Assets/UI/MapPlaceholder/App6FrameAtlas.png` + `Assets/Addressables/Map/App6AtlasAddressablesManifest.json` | presentation-only | **Yes, first slice** | Presentation (`ProjectAegis.Unity.Runtime.Content`) | JSON metadata via `App6AddressablesCatalog` → seam `LoadAssetAsync` with fallback to `App6AtlasCatalog.Default` / Unicode | `MapPlaceholderPanelHost`: binding only, waiver OQ3 |
| C2 UXML / USS | `unity/ProjectAegis/Assets/UI/**` (32 UXML, 34 USS, 135,531 B) | presentation-only | Not now (candidate by amendment) | Presentation panel hosts | `[SerializeField]` direct refs (unchanged) | None |
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
| `MapPlaceholderPanelHost` | Replace the body of `TryResolveAddressablesAtlas` with a seam call, net non-growing. **Needs owner waiver (OQ3)**, or the alternative of a separate adapter component with zero host edits |
| `GlobeMapProductHost` | None. Globe tiles are excluded |
| `DelegationBridge` hotpath | None |
| `CatalogWriteGate` | None. No catalog content in any group. Enforced by a headless descriptor guard (see Validation Criteria) |
| ReplayGolden / `17144800277401907079` | None. No sim-relevant byte changes loader. Goldens are not edited |
| Test floor (`production/test-floor.json`) | Additive tests only |

## Alternatives Considered

### Option A: Addressables for presentation-only assets, local catalog (Recommended)

- **Pros:** Uses the installed package and the existing key. Gives DRG-347 a real async load, lifetime and fallback to prove LIB-05. Keeps every sim input on its hashed loader. Small blast radius, with one class admitted at a time.
- **Cons:** Brings in Addressables build steps, a committed settings asset and Editor/Player-only verification. Content is small today, so the near-term size/memory benefit is modest. The main value is establishing the pattern and the failure isolation.

### Option B: Addressables for all content, including scenarios and catalog

- **Description:** Pack scenario policies, catalog DBs/snapshots and glossary into groups and load them through Addressables.
- **Pros:** One delivery mechanism, and it would ease a future DLC story.
- **Cons:**
  - It puts Unity APIs on the path to sim inputs. `ProjectAegis.Data` / `.Sim` would need Unity-provided bytes, which breaks ADR-010 §1/§4 and headless `dotnet test` / CLI / MCP parity.
  - Load timing and packing would sit upstream of `SimWorldHash`, `CatalogSnapshotHasher` and the order-log fingerprint.
  - It creates a catalog write/refresh path outside `CatalogWriteGate`.
  - The Baltic hash could drift and force a golden re-bless.
- **Rejection Reason:** It breaks decision drivers 1–4. DRG-326 lists exactly this as hard-no exposure.

### Option C: Status quo (Resources / `[SerializeField]` / StreamingAssets, JSON-only metadata)

- **Description:** Keep direct serialized references and `Resources`, keep the JSON metadata resolver, and add StreamingAssets for any loose files.
- **Pros:** Zero new build machinery and no Editor-only verification burden. Today's fallback chain already exists.
- **Cons:**
  - It never exercises an async load, so LIB-05 delayed/canceled/corrupt cases and handle-release can't be proven.
  - The current "Addressables" path is metadata-only and probably never resolves in a Player (inferred).
  - `Resources` cannot be unloaded selectively, and StreamingAssets would be a second, unversioned loose-file channel.
  - It leaves the H5 gate unclosable.
- **Rejection Reason:** It does not meet DRG-347 / LIB-05 acceptance. It stays the **fallback** that Option A degrades to.

### Option D: Addressables with remote catalog / CDN for presentation (deferred, not rejected)

- **Description:** Option A plus remote load paths (e.g. future basemap/art packs).
- **Rejection Reason (for now):** It is Launch-adjacent, H3 is not open, and DRG-331 says remote delivery/DLC/catalog behaviour is excluded. Cesium ion already streams globe tiles outside Addressables. Revisit in a separate ADR.

## Consequences

### Positive

- The H5 gate can close without touching the determinism core. Sim, order-log, RNG and replay paths keep their current loaders.
- Gives one reusable, testable presentation loading seam with explicit fallbacks, instead of ad-hoc loads in hosts.
- Makes the dormant Addressables declaration real (settings asset, group, Player catalog) and turns "metadata only" into verified loading.

### Negative

- Adds an Addressables content build to local Player builds and a committed `AddressableAssetSettings` asset that must stay in sync with `App6AddressablesGroupSetup.cs`.
- Some proof (async load, handle leaks, budgets) needs a local Editor/Player. Headless CI can't produce it.
- The headless JSON descriptor and Unity's group asset are two sources of truth until one is generated from the other.

### Neutral

- Fonts, UXML/USS, audio and scenes stay on their current loaders until amended in.
- Remote content stays a separate future decision.

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|-----------|
| A sim-relevant file is later added to a group | Low | High | Headless descriptor guard test against the exclusion list (inventory Q6); ADR amendment required per class |
| Load callback or timing leaks into sim/order inputs | Low | High | Seam lives in a presentation asmdef with no Sim/Data write access; DRG-347 fault-injection fixture compares world hash and order/replay fingerprints |
| Handle leaks on scene teardown | Medium | Medium | Lease and release model, late-callback rejection, Editor profiling "zero leaked handles" check |
| Host growth (`MapPlaceholderPanelHost`) | Medium | Medium | Binding-only, net non-growing edit under explicit waiver, or zero-edit adapter |
| Editor/CI version drift (`unity-ci.yml` 6000.3.14f1 vs 6000.3.22f1) | Medium | Low | Fix the pin in the ADR PR or as a follow-up (OQ5) |
| Package upgrade changes catalog/build format | Low | Medium | Pin 2.9.1; upgrade only through `docs/engineering/unity-UPGRADE-RUNBOOK.md` (its 2.3.16→2.8.1 text is stale) |

## Performance Implications

There are no accepted budgets yet. These are candidate pilot thresholds for the owner to decide (review `:33`): no synchronous main-thread wait, p99 loader frame overhead ≤ 2 ms, cold readiness ≤ 250 ms, incremental memory ≤ 16 MiB, zero leaked handles after repeated teardown, on nominated hardware/build/compression. Sim tick cost is unaffected because no tick waits on content.

## Migration Plan / Rollout (tied to DRG-347)

0. **ADR acceptance.** The owner answers the Open Questions and marks this ADR Accepted. That closes the H5 gate. DRG-347 stays in Backlog until H5 GO.
1. **DRG-347 pilot (local, single class).**
   - Commit `AddressableAssetSettings` and the `MapPresentation` group.
   - Add the `ProjectAegis.Unity.Runtime.Content` asmdef and loader seam.
   - Load `Map/App6FrameAtlas` through it, with fallback to `App6AtlasCatalog.Default` / Unicode.
   - Add headless guard tests and a fault-injection fixture.
   - Do local Editor/Player profiling against the agreed budgets.
2. **Evidence and closeout.** Record revision-bound evidence (proposed VER-07.3…6): Player build id, fault-case results, unchanged ReplayGolden 6/6 and hash.
3. **Expansion.** Each further class (audio, fonts, UXML/USS) is added by amendment to the content-class table, reusing the seam.
4. **Remote.** New ADR only.

**Rollback plan:** set `preferAddressablesAtlas` false, or remove the seam binding. The host falls back to `App6AtlasCatalog.Default` (current behaviour). No sim, catalog or golden state needs reverting.

## Build and CI

| Verified headless (`dotnet-ci.yml`, `dotnet test`) | Needs local Unity Editor / Player |
|---|---|
| `App6AddressablesCatalog` metadata resolution (existing) | Addressables content build and Player catalog generation |
| Descriptor guard: every entry under `Assets/Addressables/**/*.json` is in the presentation allow-list and matches none of the sim-relevant exclusions | `LoadAssetAsync` success, release and late-callback rejection in Play Mode |
| Assembly guard: no `UnityEngine.AddressableAssets` / `ResourceManagement` in `src/ProjectAegis.{Sim,Data,Delegation}*` | Player-build fallback (missing or corrupt bundle) |
| ReplayGolden 6/6 and `balticV2Hash` unchanged, with zero edits under `tests/regression/` | Budget profiling (latency, frame cost, memory, handle leaks) |
| Fallback-selection and diagnostic policy as pure C# (outcome → fallback), unit-tested | Editor MCP (ADR-027) may assist authoring but is not verification authority |

`unity-ci.yml` is manual and needs the `UNITY_LICENSE` secret (`.github/workflows/unity-ci.yml:1–39`). It is not a merge gate for this work.

## Validation Criteria

- [ ] Owner Accepts the ADR (closes the H5 gate).
- [ ] Content-class table with owner and loader for each class (above).
- [ ] Hard-no impact stated per host, gate and golden (above).
- [ ] First slice named with a non-`TBD` Surface (Decision §7).
- [ ] DRG-347: same seed and scenario give identical world hash, order-log fingerprint and replay fingerprint across asset success, missing, corrupt, delayed and canceled.
- [ ] Headless assembly and descriptor guards pass. ReplayGolden 6/6. Hash `17144800277401907079` unchanged with no re-bless.

## GDD / Requirements Addressed

| Doc | Requirement | How this ADR satisfies it |
|---|---|---|
| Draft 27 Scenario Library & Campaigns (`Game-Requirements/drafts/27-Scenario-Library-Campaigns.md`) | Proposed LIB-05.1…3 content failure isolation | Seam outcomes → documented fallback; authoritative fingerprints are unaffected by construction and tested in DRG-347 |
| Roadmap §14 (`future-sprint-roadmap-07142026.md:180`) | "Addressables bulk: content pipeline ADR accepted" | This ADR, on owner acceptance |
| ADR-007 Phase C | APP-6 data-driven USS + icon atlas | Atlas is the first loaded class; USS frame registry remains the fallback |

## Owner Decisions (2026-10-07 00:10 CT, drg amtd)

The owner accepted all of CMANO's proposed answers:

1. **Scope:** Accept Option A, local-only presentation-only Addressables. Remote/CDN/DLC catalogs deferred to a separate future ADR. Pilot is `Map/App6FrameAtlas` via DRG-347.
2. **Canonical APP-6 art:** Compare the two atlas PNGs (Unity 167 B vs `production/assets/c2/` 381 B) as a DRG-347 task, and record the chosen source of truth there.
3. **Host waiver:** Denied. No `MapPlaceholderPanelHost` edit; use a separate adapter component with zero host edits.
4. **Pilot budgets:** Accept the candidate thresholds as listed, measured on the laptop dev build.
5. **Package:** Pin Addressables 2.9.1 and commit `AddressableAssetSettings` in DRG-347.
6. **Deferred classes:** Fonts, UXML/USS, audio and scenes are deferred; each needs an amendment.
7. **Landing:** Land this ADR as Proposed in a docs-only PR now; hold the pilot (DRG-326/DRG-347) until it merges.

## Open Questions (answered above)

1. **Scope and delivery:** Accept Option A (local-only, presentation-only Addressables) and defer any remote/CDN/DLC catalog to a separate future ADR?
2. **Canonical APP-6 art:** Which PNG is the source of truth for the pilot? The Unity `Assets/UI/MapPlaceholder/App6FrameAtlas.png` (167 B) or `production/assets/c2/App6FrameAtlas.png` (381 B)?
3. **Host waiver:** Allow a minimal, net non-growing edit to `MapPlaceholderPanelHost` (replace the `TryResolveAddressablesAtlas` body with a seam call) for DRG-347? Or require a separate adapter component with zero host edits?
4. **Pilot budgets:** Accept, change or reject the candidate thresholds (no sync main-thread wait, p99 ≤ 2 ms, cold ≤ 250 ms, ≤ 16 MiB, zero leaked handles)? Which reference hardware and build config?
5. **Package and settings:** Confirm the Addressables 2.9.1 pin and committing `AddressableAssetSettings` (`Assets/AddressableAssetsData/`) in DRG-347? Fix the specialist-doc 2.3.16 drift and the `unity-ci.yml` 6000.3.14f1 Editor pin in the ADR PR?
6. **Deferred classes:** Confirm that fonts (stay in `Resources`), UXML/USS, audio and scenes stay out of the first slice and need an amendment each?
7. **H5 GO:** May this draft land as Proposed at `docs/architecture/adr-028-content-pipeline-addressables.md` now, or does it wait for the H5 GO?

## Related

- Linear: DRG-331 (this ADR), DRG-326 (H5 epic), DRG-332 (inventory, Done), DRG-347 (pilot)
- `production/agentic/h5-content-inventory-2026-09-27.md`; `production/agentic/h5-content-inventory-review-2026-10-01.md`
- `docs/superpowers/specs/2026-09-30-requirements-planning-reconciliation.md` (LIB-05, VER-07)
- `docs/reports/future-sprint-roadmap-07142026.md`; `docs/reports/future-sprint-roadmap-09302026.md:46`
- [ADR-001](adr-001-sim-assembly-boundary.md), [ADR-007](adr-007-c2-map-presentation.md), [ADR-010](adr-010-headless-first-command-driven-ui.md), [ADR-017](adr-017-editor-topology-client-vs-scenario-lab.md), [ADR-027](adr-027-unity-editor-mcp-stack.md)
