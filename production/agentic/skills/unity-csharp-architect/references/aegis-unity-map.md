# Aegis Unity map (“where does X live?”)

**Skill:** `unity-csharp-architect` · **Parent:** [`../SKILL.md`](../SKILL.md)  
**Program:** UCA-M3 · **Audience:** agents navigating Project Aegis (cmano-clone) Unity / .NET surfaces  
**Implements:** DRG-133 / DRG-121 (UCA-M3 Lane C)

> **Law (one line):** Put each change in the **existing** world (Sim / Data / Delegation / UnityAdapter / Unity Runtime / Editor / CLI) — never invent package roots; map reads via `MapPictureBridge` / projections, writes via `C2PlayerCommandBridge` / enqueue — **not** `IOrderSink` or `DecisionLog` from MonoBehaviours.

**Related:** [`presentation-boundary.md`](presentation-boundary.md) · [`headless-command-ui.md`](headless-command-ui.md) · [`asmdefs-and-layers.md`](asmdefs-and-layers.md) · [`mono-anti-patterns.md`](mono-anti-patterns.md) · [`testing-unity.md`](testing-unity.md) · [`editor-vs-runtime.md`](editor-vs-runtime.md) · `/c-sharp-engineer` (layering / DI / test placement)

**ADR citations (do not re-host):** presentation wall = **ADR-010** §2–3, **ADR-007**, **ADR-001** (and **ADR-006** for catalog/SQLite). **Git ADR-018** = sensor side-picture / datalink — **never** call presentation “ADR-018”. Topology of in-client vs Scenario Lab: **ADR-017** (proposed). Platform Excel round-trip: **ADR-011**.

Engineering companion: `docs/engineering/c2-projection-layer.md`.

---

## 1. Top-level worlds

| World | Path | Asm / TFM | `UnityEngine`? | Owns |
| --- | --- | --- | --- | --- |
| **Data** | `src/ProjectAegis.Data/` | `ProjectAegis.Data` | **No** | Catalog, scenario packages, SQLite (ADR-006) |
| **Data.Excel** | `src/ProjectAegis.Data.Excel/` | net8.0 edge | **No** | Workbook I/O (ClosedXML stays **out** of Data core) |
| **Sim** | `src/ProjectAegis.Sim/` | pure sim | **No** | Rules, tick truth, policy/engage math |
| **Delegation** | `src/ProjectAegis.Delegation/` | controllers, `DecisionLog`, `Projection/` | **No** | Orchestration, queues, `*Projection` / `*PanelBinder` / `*PanelState` |
| **UnityAdapter** | `src/ProjectAegis.Delegation.UnityAdapter/` | bridge seam | **No** | `ISimWorldSnapshot` in, enqueue / `IOrderSink` out; `*Bridge`; `C2PresentationController` |
| **UnityAdapter tests** | `src/ProjectAegis.Delegation.UnityAdapter.Tests/` | NUnit headless | **No** | Preferred proof for bridges / presenters / Baltic |
| **Mission Editor CLI** | `src/ProjectAegis.MissionEditor.Cli/` | net8.0 exe | **No** | Scenario/catalog verbs; CLI parity with UI commands |
| **Unity project** | `unity/ProjectAegis/` | Unity **6.3 LTS** (6000.3.x) | Yes (player/editor) | Thin hosts, UXML/USS, scenes, plugins |
| **Unity Runtime** | `unity/ProjectAegis/Assets/Scripts/Runtime/` | `ProjectAegis.Unity.Runtime` | Yes | `*PanelHost`, composition hosts |
| **Unity Cesium** | `…/Runtime/Cesium/` | `ProjectAegis.Unity.Runtime.Cesium` | Yes + Cesium | Gated (`CESIUM_FOR_UNITY` / package present) |
| **Unity Editor** | `unity/ProjectAegis/Assets/Editor/` | `ProjectAegis.Unity.Editor` (`includePlatforms: Editor`) | Editor | Scene builders, EditorWindows, batch signoff |
| **Unity Tests** | `unity/ProjectAegis/Assets/Tests/` | `ProjectAegis.Unity.Tests` | Test Runner | Play Mode / EditMode last-mile only |

```text
src/ (.NET, no UnityEngine)
  Data ──► Sim ──► Delegation ──► Delegation.UnityAdapter
                      │  Projection/*PanelBinder/*State
                      └──── *Tests ──────────────────┘
                                           │
              netstandard2.1 publish (copy script) ──┘
                                           ▼
unity/ProjectAegis/Assets/Plugins/ProjectAegis/*.dll   (gitignored)
                                           │
              ProjectAegis.Unity.Runtime  (*PanelHost)
              ProjectAegis.Unity.Runtime.Cesium (optional)
              ProjectAegis.Unity.Editor
              ProjectAegis.Unity.Tests
```

---

## 2. “Where does X live?” master table

| Need | Live here | Not here |
| --- | --- | --- |
| **MonoBehaviour panel host** | `unity/ProjectAegis/Assets/Scripts/Runtime/*Host.cs` (`ProjectAegis.Unity.Runtime`) | `src/` · invented `Assets/Scripts/Gameplay/` |
| **UXML / USS (per panel)** | `unity/ProjectAegis/Assets/UI/<PanelName>/` (+ `AegisTokens.uss`, `C2RuntimePanelSettings.asset`) | Embedding chrome in Sim/Delegation |
| **EditorWindow / design-time UI** | `unity/ProjectAegis/Assets/Editor/` (e.g. `ScenarioMapAuthoringWindow.cs`) | Player Runtime asmdef |
| **Projection DTO / pure fold** | `src/ProjectAegis.Delegation/Projection/` (`MapPictureProjection`, `MapSymbolEntry`, `*Projection`) | MonoBehaviour fields as world truth |
| **Panel binder / panel state** | `…/Projection/*PanelBinder.cs`, `*PanelState` / `*ApplyState` | Host string-formatting as only layer |
| **Presentation selection** | `…/UnityAdapter/Presentation/C2PresentationController.cs`, `SelectionSet.cs` | ECS / static globals as authority |
| **Command entry (player intent)** | `…/Bridge/C2PlayerCommandBridge.TryIssue` → `DelegationBridge.TryEnqueueHumanOrder` → `HumanController` / `PlayerOrderExecutionQueue` | MB → `IOrderSink.ApplyOrder` |
| **`ISimWorldSnapshot`** | `…/Bridge/ISimWorldSnapshot.cs` (impl: sim host / `SimplePlayModeSimHost` / Baltic harness) | Panel reading live session guts |
| **`IOrderSink`** | `…/Bridge/IOrderSink.cs` + `OrderDispatcher` (sim apply after queue drain) | UI Toolkit click handlers |
| **Map read path** | `MapPictureBridge.Build` → `MapPictureProjection` → `MapPanelBinder` → host bind / `MapSymbolPool` | UI append to `DecisionLog` (ADR-007) |
| **Catalog / SQLite** | `src/ProjectAegis.Data/` (+ approved `ICatalogReader` edges) | Presentation opening DB (ADR-006) |
| **Scenario package / authoring truth** | `src/ProjectAegis.Data/Scenario/Authoring/` (`ScenarioAuthoringSession`, `ScenarioEditCommandBus`, `ScenarioDocumentEditor`, …) | Scene-serialized ORBAT as sole authority |
| **Authoring presenters** | `src/ProjectAegis.Delegation.UnityAdapter/Authoring/` | Logic only inside `EditorWindow` |
| **CLI verb / parity** | `src/ProjectAegis.MissionEditor.Cli/` | Unity-only authoritative mutators |
| **Play Mode smoke** | `unity/ProjectAegis/PLAYMODE-SMOKE.md`, `Assets/Scenes/DelegationSmoke.unity`, hosts + `tools/unity/*` | First-line architecture proof |
| **Headless test (prefer)** | `src/ProjectAegis.Delegation.UnityAdapter.Tests/{Bridge,Presentation,Authoring,Map,Baltic,Platform,…}/` | Play Mode as only coverage |
| **Plugin DLLs** | `Assets/Plugins/ProjectAegis/` via `tools/copy-delegation-assemblies.ps1` | Checking in net8.0 outputs |
| **Zero-touch hotpath** | Do **not** edit `DelegationBridge` tick hotpath through Release v1 | “Just one more branch in Tick” |

### c-sharp-engineer placement hints

| Concern | Expectation |
| --- | --- |
| **Layering** | Pure logic in `src/`; MB only for lifecycle, UIDocument, serialization |
| **DI / composition** | Scene/host (`DelegationBridgeHost`, `SimplePlayModeSimHost`) wires deps once — no new mutable game-state singletons |
| **DIP** | Hosts depend on `IC2PresentationFeed` / bridge façades / projection DTOs — not concrete session internals |
| **Testing** | New binder/bridge/presenter → `dotnet test` next to existing folders **before** Unity tests |

---

## 3. C2 panel → host → bridge / projection (representative)

Pattern: **Projection → Bridge (adapter) → Binder/State → thin `*PanelHost` → UXML/USS**.

| Surface | Host (Runtime) | UXML/USS | Bridge / projection anchors |
| --- | --- | --- | --- |
| **Composition / feed** | `DelegationBridgeHost` (`IC2PresentationFeed`) | — | `DelegationBridge`; `C2PresentationController` on host |
| **Smoke sim** | `SimplePlayModeSimHost` | — | Implements `ISimWorldSnapshot` + `IOrderSink`; calls host tick |
| **Top bar** | `C2TopBarPanelHost` | `Assets/UI/TopBar/` | `C2TopBarProjection` / apply-state; begin-execution tests |
| **Left drawer shell** | `C2LeftDrawerPanelHost` | `Assets/UI/C2LeftDrawer/` | `LeftDrawerApplyState`; composes OOB / missions / contacts |
| **OOB tree** | `OobTreePanelHost` | `Assets/UI/OobTree/` | `OobTreeBridge` → entries → `OobTreePanelBinder` |
| **Mission list** | `MissionListPanelHost` | `Assets/UI/MissionList/` | `MissionListBridge` → `MissionListPanelBinder` |
| **Map (Phase A)** | `MapPlaceholderPanelHost` + `MapSymbolPool` | `Assets/UI/MapPlaceholder/` | **`MapPictureBridge.Build`** → `MapSymbolEntry` → `MapPanelBinder` |
| **Unit detail (right)** | `RightUnitPanelHost` | `Assets/UI/UnitDetail/` | `UnitDetailBridge` → `UnitDetailPanelBinder` |
| **Message log** | `MessageLogPanelHost` | `Assets/UI/MessageLog/` | `MessageLogBridge` → `MessageLogPanelBinder` |
| **Sensor C2** | `SensorC2PanelHost`, `SensorC2HudHost` | `Assets/UI/SensorC2/` | `SensorC2Bridge` / `SensorC2PanelBridge` |
| **Contact detail** | `ContactDetailPanelHost` | `Assets/UI/ContactDetail/` | Contact picture + `C2PresentationController` selection |
| **Doctrine** | `DoctrineInheritancePanelHost` | `Assets/UI/DoctrineInheritance/` | Doctrine projections + `DoctrineOverrideCommand` |
| **Order toolbar** | `UnitOrderToolbarHost` | `Assets/UI/UnitOrderToolbar/` | **`C2PlayerCommandBridge.TryIssue`** — not sink from MB |
| **Agent roster** | `AgentRosterPanelHost` | `Assets/UI/AgentRoster/` | `AgentRosterProjection` + command surfaces |
| **C2 menu** | `C2MenuPanelHost` | `Assets/UI/C2Menu/` | `C2MenuProjection` → navigation / command intents |
| **Logistics ops** | `MagazineLoadoutPanelHost`, `DeckHangarPanelHost`, `BoatOpsPanelHost`, `AirOpsPanelHost`, `GroundOpsPanelHost` | Matching `Assets/UI/*` folders | Logistics projections/binders; thin hosts |
| **Combat domains HUD** | `CombatDomainsHotTickHost` | `Assets/UI/CombatDomains/` | `CombatDomainsHotTickTracker` → panel binder |
| **Live edit** | `LiveEditPanelHost` | `Assets/UI/LiveEdit/` | `LiveEditPanelBinder` / `LiveEditContract` |
| **Scenario library** | `ScenarioLibraryPanelHost` | `Assets/UI/ScenarioLibrary/` | Scenario package services (Data) — not SQLite from host |
| **Scenario editor shell** | `ScenarioEditorShellHost` | `Assets/UI/ScenarioEditor/ScenarioEditorShell.*` | `ScenarioEditorShellProjection`; mutations on `ScenarioEditCommandBus` |
| **Map authoring (Editor)** | `ScenarioMapAuthoringWindow` | `ScenarioMapAuthoringPanel.*` | `MapAuthoringSurface`, `ScenarioAuthoringSession`, `LiveFindingsPresenter` |
| **Platform catalog / import / editor** | `PlatformCatalogViewerHost`, `PlatformImportPanelHost`, `PlatformEditorShellHost` | `Assets/UI/PlatformCatalog|PlatformImport|PlatformEditor/` | `PlatformCatalogExportBridge`, `PlatformWorkbookWriteBridge`, `PlatformDesignAssistantBridge`, Data.Excel |
| **OSINT staging** | `OsintStagingPanelHost` | (wired in Runtime) | Presentation + Data staging — no sim authority |
| **Axis / map scale chrome** | `AxisControlPanelHost`, `MapScaleHudPanelHost` | Runtime hosts | Presentation-only chrome |
| **Cesium globe** | `Cesium/CesiumGlobeHost`, `CesiumGlobeBridge` | Scenes + `CESIUM-SPIKE-SETUP.md` | Optional asmdef; product globe still partial (ADR-007 Phase B) |
| **UIDocument bootstrap** | `UiDocumentPanelSettingsBootstrap` | `Assets/UI/C2RuntimePanelSettings.asset` | Shared PanelSettings — required or `rootVisualElement` is null |

**Canonical map path (law):**

```text
ISimWorldSnapshot + TargetRegistry + DecisionLog
    → MapPictureBridge.Build(snapshot, registry, log, layoutSeed)
    → MapPictureProjection / MapSymbolEntry
    → MapPanelBinder / MapPanelApplyState
    → MapPlaceholderPanelHost + MapSymbolPool (bind only)
```

**Canonical command path (law):**

```text
UI intent → C2PlayerCommandBridge.TryIssue(bridge, entity, commandId, simTime, out reason)
         → DelegationBridge.TryEnqueueHumanOrder
         → HumanController / PlayerOrderExecutionQueue
         → (tick drain) OrderDispatcher → IOrderSink.ApplyOrder
```

**Canonical scenario authoring path (law):**

```text
ScenarioAuthoringSession / ScenarioDocumentEditor  (Data)
    → ScenarioEditCommandBus mutations
    → UnityAdapter Authoring presenters (MapAuthoringSurface, MissionBoardPresenter, …)
    → optional chrome: ScenarioMapAuthoringWindow | ScenarioEditorShellHost
    → CLI parity: MissionEditor.Cli verbs
```

---

## 4. UnityAdapter namespace map

Root: `src/ProjectAegis.Delegation.UnityAdapter/` — **no `UnityEngine`**.

| Folder | Role | Key types |
| --- | --- | --- |
| **`Bridge/`** | Seams + read façades + command façade | `DelegationBridge` (**zero-touch hotpath**), `ISimWorldSnapshot`, `IOrderSink`, `IC2PresentationFeed`, `C2PlayerCommandBridge`, `MapPictureBridge`, `OobTreeBridge`, `MessageLogBridge`, `MissionListBridge`, `SensorC2Bridge`, `UnitDetailBridge`, `TargetRegistry`, `OrderDispatcher`, `EntityKey`, platform bridges |
| **`Presentation/`** | Presentation-only selection / graph | `C2PresentationController`, `SelectionSet` |
| **`Authoring/`** | Engine-free mission/map authoring presenters | `EditModeController`, `MapAuthoringSurface`, `MissionBoardPresenter`, `EventGraphPresenter`, `LiveFindingsPresenter`, `ScenarioMapAuthoringHostPolicy`, `ScenarioPlatformDomainCatalog`, … |
| **`Baltic/`** | Headless scenario runners | `BalticReplayHarness`, `BalticBatchRunner` |
| **`Console/`** | Console noise helpers | `ConsoleNoiseClassifier` |
| **`Polyfills/`** | netstandard support | `IsExternalInit` |

**Matching tests:** `src/ProjectAegis.Delegation.UnityAdapter.Tests/{Bridge,Presentation,Authoring,Map,Baltic,Platform,App6,Console}/`.

**Delegation pure C2 layer (sibling):** `src/ProjectAegis.Delegation/Projection/` (~160 projection/binder/state types) + `Controllers/HumanController.cs` + `Decision/PlayerOrderExecutionQueue.cs`.

**Data authoring truth:** `src/ProjectAegis.Data/Scenario/Authoring/` (`ScenarioAuthoringSession`, `ScenarioEditCommandBus`, `ScenarioDocumentEditor`, `AegisScenarioPackage`, JSON load/write, semantic diff).

---

## 5. Plugin / DLL copy path

| Step | Path / command |
| --- | --- |
| Script | `tools/copy-delegation-assemblies.ps1` (also `.sh`) |
| Publish | `dotnet publish` UnityAdapter **netstandard2.1** |
| Destination | `unity/ProjectAegis/Assets/Plugins/ProjectAegis/` |
| Guard | `tools/Test-UnityPluginAssemblies.ps1` |
| Scaffold | `tools/init-unity-project.ps1` |
| Docs | `unity/ProjectAegis/README.md` |

**Rules:**

- Copy **all** publish-output DLLs (Data, Sim, Delegation, UnityAdapter, SQLite, System.Text.Json, …).
- **netstandard2.1 only** — never drop `net8.0` build outputs into Unity.
- Plugin DLLs are **gitignored** — re-run copy after clone or headless API changes.
- Unity Test asmdef references precompiled `ProjectAegis.Delegation.dll` + `ProjectAegis.Delegation.UnityAdapter.dll`.

Related Unity tooling: `tools/unity/Invoke-DelegationSmokeSceneSetup.ps1`, `Invoke-ManualQaHeadlessGate.ps1`, `Invoke-C2PlayModeSignoffBatch.ps1`.

---

## 6. Forbidden inventions (do not invent these roots)

| Forbidden invention | Use instead |
| --- | --- |
| `Assets/Scripts/Sim/`, `Assets/Scripts/Core/`, `Assets/Scripts/Game/` as new authority | Headless `src/ProjectAegis.*` + thin Runtime hosts |
| `src/ProjectAegis.Unity/` or `src/ProjectAegis.Presentation/` | `Delegation.UnityAdapter` + `unity/ProjectAegis/...` |
| `UnityEngine` usings in Data / Sim / Delegation / UnityAdapter | Keep headless; engine code under `Assets/Scripts` |
| New asmdef without PR **edge list** | Extend Runtime / Editor / Cesium / Tests |
| Checking in `Assets/Plugins/ProjectAegis/*.dll` as source of truth | Copy script + gitignore |
| `net8.0` DLLs under Plugins | netstandard2.1 publish only |
| SO / scene as live catalog, ORBAT, or order log | Data + Sim + DecisionLog |
| UI → live ECS / session / `DecisionLog.Append` | Snapshot / projection / command façade |
| MB → `IOrderSink.ApplyOrder` | `C2PlayerCommandBridge` / enqueue |
| Editing `DelegationBridge` hotpath for features | New projection, adapter type, or host |
| Labeling presentation as **ADR-018** | ADR-010 / 007 / 001 |
| Parallel SO “world model” | Designer config only (`scriptableobjects-data.md`) |
| Play Mode as sole architecture proof | `dotnet test` first (`testing-unity.md`) |
| Forked “Scenario Lab” core | Same Data/CLI core (ADR-017) — optional UI shell only |

---

## 7. Agent checklist

- [ ] Classified world: Sim / Data / Delegation / UnityAdapter / Runtime / Editor / CLI  
- [ ] Path exists in repo (no invented package root)  
- [ ] Reads: snapshot / `*Bridge` / `*Projection` / `*PanelBinder` — not live session  
- [ ] Map path uses `MapPictureBridge.Build` → bind; **no** `DecisionLog` write from UI  
- [ ] Commands use `C2PlayerCommandBridge` / `HumanController.Enqueue` — **not** MB `IOrderSink`  
- [ ] Selection on `C2PresentationController` (presentation-only)  
- [ ] Scenario authoring mutates via Data bus/session + Authoring presenters — not window-only truth  
- [ ] Host is thin: lifecycle + UIDocument + intent forward; logic in pure C#  
- [ ] No `UnityEngine` in `src/ProjectAegis.{Data,Sim,Delegation,Delegation.UnityAdapter}`  
- [ ] Plugin DLLs refreshed via copy script if adapter public surface changed  
- [ ] `DelegationBridge` hotpath untouched unless explicitly waived  
- [ ] ADR citations: **010 / 007 / 001** (006 if catalog; 011 PE; 017 topology) — **not** ADR-018 for presentation  
- [ ] Tests: headless first; Play Mode smoke last (`PLAYMODE-SMOKE.md`)  

---

## 8. See also

| Doc | Use |
| --- | --- |
| [`../SKILL.md`](../SKILL.md) | Parent doctrine + load triggers |
| [`presentation-boundary.md`](presentation-boundary.md) | Snapshot wall; immutability |
| [`headless-command-ui.md`](headless-command-ui.md) | Intent → command → engine |
| [`asmdefs-and-layers.md`](asmdefs-and-layers.md) | Two graphs + asmdef inventory |
| [`testing-unity.md`](testing-unity.md) | Where tests live |
| [`mono-anti-patterns.md`](mono-anti-patterns.md) | Thin host anti-patterns |
| [`editor-vs-runtime.md`](editor-vs-runtime.md) | EditorWindow vs player hosts |
| `docs/engineering/c2-projection-layer.md` | Projection → binder → state |
| `docs/architecture/adr-010-headless-first-command-driven-ui.md` | UI is a client |
| `docs/architecture/adr-007-c2-map-presentation.md` | Map presentation |
| `docs/architecture/adr-001-sim-assembly-boundary.md` | Snapshot in / Order out |
| `docs/architecture/adr-006-data-layer-boundary.md` | No SQLite from presentation |
| `docs/architecture/adr-011-platform-editor-excel-roundtrip.md` | PE Excel / write gate |
| `docs/architecture/adr-017-editor-topology-client-vs-scenario-lab.md` | Shared core topology |
| `docs/architecture/adr-018-sensor-side-picture-datalink.md` | **Datalink** — not presentation |
| `src/ProjectAegis.Delegation.UnityAdapter/README.md` | Adapter contract |
| `unity/ProjectAegis/README.md` | Plugin publish / wiring |
| `unity/ProjectAegis/PLAYMODE-SMOKE.md` | Play Mode host stack |
| `tools/copy-delegation-assemblies.ps1` | DLL copy |

**UCA-M3 note:** Path playbook only — do not re-host full ADRs. When product code moves, update **this** map; do not invent parallel roots.
