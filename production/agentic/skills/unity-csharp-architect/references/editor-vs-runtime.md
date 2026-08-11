# Editor vs Runtime

**Skill:** `unity-csharp-architect` · **Parent:** [`../SKILL.md`](../SKILL.md)  
**Program:** UCA-M3 · **Audience:** agents writing/reviewing EditorWindows, authoring hosts, Runtime shells, assembly edges  
**Implements:** DRG-133 / DRG-121 (Lane C)

> **Law (one line):** Editor chrome is **design-time only**; authoritative scenario truth and live sim live in **headless** Data / UnityAdapter Authoring / Delegation — never in `EditorWindow` guts or player-shipped Editor assemblies.

**Related:** [`aegis-unity-map.md`](aegis-unity-map.md) · [`asmdefs-and-layers.md`](asmdefs-and-layers.md) · [`headless-command-ui.md`](headless-command-ui.md) · [`presentation-boundary.md`](presentation-boundary.md) · [`testing-unity.md`](testing-unity.md) · `/c-sharp-engineer` (layering, DI, headless-first tests)

---

## ADR citation note (non-negotiable)

Some older notes labeled **presentation** as **“ADR-018”**. In git:

| Citation | Actual topic |
| --- | --- |
| **ADR-010** §2–3 | Headless-first; UI is a **client**; projections + commands |
| **ADR-007** | Map never writes sim / `DecisionLog` |
| **ADR-001** | Delegation: `ISimWorldSnapshot` in, `Order` out |
| **ADR-006** | **No SQLite** from UI / presentation / Editor chrome |
| **ADR-011** | Platform Editor Excel round-trip on write gate |
| **ADR-017** | Editor topology: shared headless core; in-client v1; optional Scenario Lab shell later |
| **Git ADR-018** | Sensor side-picture / **datalink** — **not** the presentation wall |

**Correct citations for Editor / authoring / Runtime presentation:** ADR-010 / 007 / 001 (+ ADR-006 catalog; ADR-011 PE; ADR-017 topology).  
**Never** cite Git ADR-018 for Editor↔Runtime presentation seams.

---

## 1. Law (one screen)

1. **Editor assemblies never ship in player** — `includePlatforms: ["Editor"]` on `ProjectAegis.Unity.Editor`.
2. **Editor tools must not own live sim / `DecisionLog` authority** — presentation boundary = **ADR-010 / 007 / 001**.
3. **Prefer headless authoring** — `UnityAdapter/Authoring`, `Data/Scenario/Authoring`, CLI — **before** `EditorWindow` chrome (ADR-010).
4. **No Editor → runtime presentation shortcut** that bypasses command / enqueue or projection contracts.
5. **No `Resources.Load` / `Find*` as production service location** (`/c-sharp-engineer`).
6. **Play Mode ≠ Edit Mode** — Play Mode smoke uses **Runtime hosts** + plugin DLLs.
7. **Authoring vs Execution** — `EditModeController` / scenario document vs `BeginExecution` + `DelegationBridge.Tick`.
8. **DI at host/window** — compose presenters at composition root; do not `new` sim sessions inside arbitrary Editor code.

---

## 2. Three modes (do not conflate)

| Mode | When | Owns | Logic home | Must not |
| --- | --- | --- | --- | --- |
| **Editor (design-time)** | Unity Editor, not playing | Menus, `EditorWindow`, Addressables groups, scene builders | `ProjectAegis.Unity.Editor` + thin glue over headless presenters | Ship in player; own live `DecisionLog` / sim tick; open SQLite |
| **Runtime / Play (play-time)** | Player build **or** Editor Play Mode | Thin MB / UI Toolkit hosts, C2 bind, intent → command | `ProjectAegis.Unity.Runtime` (+ optional Cesium) over plugins | Reference Editor asmdef; invent authority in `Update` |
| **Headless authoring (preferred)** | `dotnet test` / CLI / adapter | Scenario document edit, map/mission presenters, export gates | `ProjectAegis.Data`, `…UnityAdapter/Authoring`, `MissionEditor.Cli` | Depend on `UnityEngine` / `UnityEditor` |

```text
  design-time       Unity.Editor (EditorWindow chrome)     optional skin
                              │ compose / bind only
                              ▼
  preferred home    UnityAdapter/Authoring presenters      engine-free
                    Data/Scenario/Authoring packages
                    MissionEditor.Cli verbs
                              │
  play-time         Unity.Runtime hosts + plugin DLLs
                    ScenarioEditorShellHost / C2 hosts
                    BeginExecution → DelegationBridge
```

| Concern | Editor | Runtime / Play | Headless |
| --- | --- | --- | --- |
| Scenario package JSON truth | May **invoke** load/save via Data APIs | Loads via Runtime host + Data | **Owns** editor model + I/O |
| Live sim tick | **No** | Yes (host + bridge) | Tests/harnesses only |
| Player build | **Excluded** | **Included** | N/A (plugins) |
| Tests | Rare; prefer headless | Play Mode last-mile | **Default** (`dotnet test`) |

---

## 3. Assembly split (real inventory)

### 3.1 Unity asmdefs

| Asmdef | Path | Platforms | Notes |
| --- | --- | --- | --- |
| `ProjectAegis.Unity.Editor` | `Assets/Editor/ProjectAegis.Unity.Editor.asmdef` | **`includePlatforms: ["Editor"]` only** | Refs Runtime + Addressables Editor |
| `ProjectAegis.Unity.Runtime` | `Assets/Scripts/Runtime/` | Player + Editor can load | Thin hosts; precompiled plugins |
| `ProjectAegis.Unity.Runtime.Cesium` | Runtime Cesium | Optional `CESIUM_FOR_UNITY` | Map terrain chrome — not scenario authority |
| `ProjectAegis.Unity.Tests` | `Assets/Tests/` | EditMode / Play Mode | Runtime + precompiled Delegation / UnityAdapter |

### 3.2 Headless (.NET — **no `UnityEngine`**)

| Assembly | Path | Role |
| --- | --- | --- |
| `ProjectAegis.Data` | `src/ProjectAegis.Data/` | Scenario packages, catalog gates, JSON load/write |
| `ProjectAegis.Sim` | `src/ProjectAegis.Sim/` | Sim rules — not Editor chrome |
| `ProjectAegis.Delegation` | `src/ProjectAegis.Delegation/` | Controllers, queues, projections |
| `ProjectAegis.Delegation.UnityAdapter` | `src/ProjectAegis.Delegation.UnityAdapter/` | Bridges, C2 presentation, **Authoring/** |
| `ProjectAegis.MissionEditor.Cli` | `src/ProjectAegis.MissionEditor.Cli/` | Scenario/mission/ORBAT/catalog CLI verbs |
| `ProjectAegis.Data.Excel` | `src/ProjectAegis.Data.Excel/` | Workbook I/O for PE (ADR-011) |

**Publish rule:** UnityAdapter **netstandard2.1** → copy all publish-output DLLs into `Assets/Plugins`. No `net8.0` plugins.

### 3.3 Real Editor scripts (`Assets/Editor/`)

| Type | Role |
| --- | --- |
| `ScenarioMapAuthoringWindow.cs` | `EditorWindow` chrome for map authoring (binds headless session/presenters only) |
| `ScenarioMapAuthoringOpenTrigger.cs` | Menu / open trigger |
| `DelegationSmokeSceneBuilder.cs` | Smoke scene builder — not product authority |
| `C2PlayModeSignoffBatchRunner.cs` | Batch Play Mode signoff |
| `App6AddressablesGroupSetup.cs` | Addressables groups |
| `BuildPlayer.cs` | Player build entry |
| `McpPlayerBuildIsolation.cs` | MCP / player build isolation |
| `ProjectConsoleQuietBootstrap.cs` | Editor console noise bootstrap |

Pure logic these need lives in headless Authoring / Data / CLI — not only in the window.

---

## 4. Allowed / forbidden edges for Editor code

| Allow | Forbid |
| --- | --- |
| Editor → Runtime (hosts, shared UXML, non-authoritative helpers) | Runtime / player → **Editor** asmdef |
| Editor → published UnityAdapter / Data for **document** edit, validation, export gates | Editor → append `DecisionLog`, step sim, or `IOrderSink.ApplyOrder` as “preview authority” |
| Editor composes headless presenters at window open | Authoring **rules** only inside `EditorWindow` with no headless twin |
| Editor invokes CLI-parity services for scenario/mission mutations | Shortcut that **bypasses** command / enqueue / projection contracts for play-time C2 |
| Addressables / build / scene tooling that does not own replay hashes | `Resources.Load` / `FindObjectOfType` as production service location |
| Catalog **read** via approved Data readers / DTOs | **SQLite open** from Editor UI chrome (ADR-006) |

```text
OK:
  EditorWindow ──compose──► UnityAdapter.Authoring.* ──► Data.Scenario.Authoring
  EditorWindow ──menu──► MissionEditor.Cli parity verbs
  Runtime host ──Tick──► DelegationBridge ──projection──► panel bind

BAD:
  EditorWindow ──► DecisionLog.Append / SimulationSession step
  EditorWindow ──► IOrderSink.ApplyOrder
  Runtime ──► UnityEditor / Editor asmdef
  EditorWindow ──► SQLite connection (ADR-006)
```

---

## 5. Authoring stack (preferred order)

```text
1. Data scenario truth
     src/ProjectAegis.Data/Scenario/Authoring/*
       ScenarioAuthoringSession, ScenarioEditCommandBus, ScenarioDocumentEditor,
       AegisScenarioPackage, ScenarioDocumentJsonLoader/Writer, ScenarioSemanticDiff, …

2. UnityAdapter Authoring presenters (engine-free)
     src/ProjectAegis.Delegation.UnityAdapter/Authoring/
       EditModeController, MapAuthoringSurface, MissionBoardPresenter,
       EventGraphPresenter, LiveFindingsPresenter,
       ScenarioMapAuthoringHostPolicy, ScenarioExportGateState,
       SelectionInspectorModel, ScenarioPlatformDomainCatalog, …

3. CLI parity
     src/ProjectAegis.MissionEditor.Cli/*
       ScenarioCreate, Mission*, Orbat*, Catalog*, PlatformDesignPropose, …

4. Optional chrome
     a) EditorWindow — Assets/Editor/ScenarioMapAuthoringWindow.cs
     b) Runtime-in-player — ScenarioEditorShellHost + Assets/UI/ScenarioEditor/
```

| Layer | Responsibility | Test home |
| --- | --- | --- |
| Data Authoring | Package model, load/write, semantic diff, validation | `src/ProjectAegis.Data.Tests` |
| UnityAdapter Authoring | Presenters, export gate state, selection models | `…UnityAdapter.Tests/Authoring/*` |
| CLI | Headless verbs shared with humans/agents | Cli test projects |
| EditorWindow / Runtime shell | Lifecycle, UXML bind, composition root only | Thin Unity tests last |

**ADR-010 / ADR-017:** one engine-free core; UI shells (Editor or in-client) are front-ends — never a forked editor.

### DI / composition (`/c-sharp-engineer`)

| Prefer | Avoid |
| --- | --- |
| Window / host wires presenters at composition root | `new SimulationSession()` inside random menu handlers |
| Inject `EditModeController`, presenters, gate state | Static mutable “current scenario” as hidden authority |
| Same presenter used by CLI test and Editor bind | Divergent Editor-only mutation path with no headless twin |

---

## 6. Scenario map authoring path (real types)

### 6.1 Headless spine

| Type / area | Role |
| --- | --- |
| `ScenarioAuthoringSession` | Open session over scenario package |
| `ScenarioEditCommandBus` | Authoritative scenario mutations |
| `ScenarioDocumentEditor` | Document mutate under Data rules |
| `AegisScenarioPackage` | Package truth container |
| `ScenarioDocumentJsonLoader` / `Writer` | Serialize/deserialize |
| `ScenarioSemanticDiff` | Diff for review / export confidence |
| `EditModeController` | Authoring mode controller (not execution) |
| `MapAuthoringSurface` | Map authoring surface |
| `MissionBoardPresenter` | Mission board |
| `EventGraphPresenter` | Event graph |
| `LiveFindingsPresenter` | Findings / validation surfacing |
| `ScenarioMapAuthoringHostPolicy` | Host policy (headless source of truth) |
| `ScenarioExportGateState` | Export readiness — UI validation ≠ play authority |
| `SelectionInspectorModel` | Inspector selection model |
| `ScenarioPlatformDomainCatalog` | Domain catalog for platforms |

### 6.2 Editor chrome

| Type | Role |
| --- | --- |
| `ScenarioMapAuthoringWindow` | Thin EditorWindow over session + presenters (no `DelegationBridge`, no business logic in window) |
| `ScenarioMapAuthoringOpenTrigger` | Opens the window |
| UXML/USS | `Assets/UI/ScenarioEditor/ScenarioMapAuthoringPanel.*` |

### 6.3 Runtime shell (editors-in-player)

| Type / asset | Role |
| --- | --- |
| `ScenarioEditorShellHost` | Runtime host; binds `ScenarioEditorShellProjection` |
| `Assets/UI/ScenarioEditor/ScenarioEditorShell.*` | In-player scenario editor chrome |
| Mutations | Stay on `ScenarioEditCommandBus` — host is a thin binder |

### 6.4 Authoring vs execution (hard split)

| Phase | Types / calls | Meaning |
| --- | --- | --- |
| **Authoring** | `EditModeController`, document editor, export gates, command bus | Scenario package design; **no** live engagement authority |
| **Execution** | `BeginExecution` + `DelegationBridge.Tick(snapshot, orderSink)` | Live delegation / sim; C2 reads projections only |

```csharp
// GOOD: authoring on document + presenters; execution is a separate mode
// bridge.BeginExecution();
// bridge.Tick(snapshot, orderSink);

// BAD: EditorWindow.TickSimForPreview() that appends DecisionLog
// BAD: treating ScenarioExportGateState as if it were BeginExecution
```

---

## 7. Platform Editor / catalog path (real types)

### 7.1 UI assets & Runtime hosts

| Location | Role |
| --- | --- |
| `Assets/UI/PlatformEditor/` + `PlatformEditorShellHost` | PE shell (Catalog \| Import tabs) |
| `Assets/UI/PlatformCatalog/` + `PlatformCatalogViewerHost` | Catalog browser |
| `Assets/UI/PlatformImport/` + `PlatformImportPanelHost` | Import surface |

### 7.2 Bridges (glue — not authority)

| Type | Role |
| --- | --- |
| `PlatformDesignAssistantBridge` | Design-assistant flows |
| `PlatformCatalogExportBridge` | Catalog export via approved path |
| `PlatformWorkbookWriteBridge` | Workbook write through Data write gates |

### 7.3 Data / Excel / CLI (ADR-011)

| Area | Role |
| --- | --- |
| `src/ProjectAegis.Data/` catalog + write gate | **Only** approved SQLite / catalog mutation path (ADR-006) |
| `src/ProjectAegis.Data.Excel/` | ClosedXML workbook I/O edge |
| `src/ProjectAegis.Data/PlatformAssistant/` | Platform design assistant models |
| `MissionEditor.Cli` — `Catalog*`, `PlatformDesignPropose`, … | Headless verbs |

| Allow | Forbid |
| --- | --- |
| Platform UI → bridge → Data write gate / DTO | Platform UI → `new SqliteConnection(...)` |
| Export via bridges + headless tests | Silent Editor-only catalog mutation with no CLI/test twin |
| Propose designs via CLI / assistant | Treating workbook UI fields as live sim magazines |

---

## 8. Play Mode vs Edit Mode testing placement

| What you changed | Put tests in | Runner |
| --- | --- | --- |
| Authoring presenters, export gates, map surface | `…UnityAdapter.Tests/Authoring/*` | `dotnet test` |
| Scenario document, package JSON, semantic diff, command bus | `src/ProjectAegis.Data.Tests` | `dotnet test` |
| CLI verbs | Cli / tool test projects | `dotnet test` |
| Pure Runtime types without scene enter | `Assets/Tests` **EditMode** | Unity Test Runner |
| Shell hosts, full smoke | `Assets/Tests` **Play Mode** | Unity Test Runner |
| EditorWindow layout only | Prefer **no** Unity test — cover presenter headless | — |

```bash
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj
```

| Rule | Detail |
| --- | --- |
| **Headless first** | Authoring logic must not exist only inside `EditorWindow` |
| **Play Mode ≠ Edit Mode** | Play Mode loads Runtime + plugin DLLs; Editor-only types unavailable in player |
| **Play Mode last** | Host lifecycle smoke only after presenter + Data tests pass |

---

## 9. Anti-patterns (BAD / GOOD)

### 9.1 Editor mutates `DecisionLog`

#### Bad

```csharp
// BAD: EditorWindow writes authoritative decision stream
public sealed class ScenarioMapAuthoringWindow : EditorWindow
{
    DecisionLog _log;
    void OnGUI()
    {
        if (GUILayout.Button("Mark contact"))
            _log.Append(/* chrome-driven */); // FAIL: ADR-010/007
    }
}
```

#### Good

```csharp
// GOOD: compose headless authoring; DecisionLog only on execution path
public sealed class ScenarioMapAuthoringWindow : EditorWindow
{
    ScenarioAuthoringSession? _session;
    MapAuthoringSurface? _surface;
    LiveFindingsPresenter? _findings;
    // OnEnable: open session + presenters; bind UXML; mutations via ScenarioEditCommandBus
}
```

### 9.2 Editor calls `IOrderSink` as authority

#### Bad

```csharp
void PreviewEngage(IOrderSink sink, EntityKey unit, in Order order)
    => sink.ApplyOrder(unit, order); // FAIL
```

#### Good

```csharp
// Authoring: mutate package via ScenarioEditCommandBus / document editor
// Play-time (Runtime): C2PlayerCommandBridge.TryIssue → TryEnqueueHumanOrder → queue → IOrderSink
```

### 9.3 Authoring logic only in `EditorWindow`

#### Bad

```csharp
// Export rules live only in Editor GUI; File.WriteAllText of window state — no Data twin
```

#### Good

```csharp
// ScenarioExportGateState + ScenarioDocumentJsonWriter under Data/Authoring
// Tests: UnityAdapter.Tests/Authoring/* + Data.Tests — not only manual Editor clicks
```

### 9.4 Circular Editor ↔ Runtime shortcuts

#### Bad

```csharp
// Runtime reaches into UnityEditor AssetDatabase as sim authority
// Runtime assembly references Editor asmdef
```

#### Good

```csharp
// Both Editor and Runtime call ScenarioDocumentJsonLoader / ScenarioAuthoringSession
// Edges: Editor → Runtime (ok) · Editor → plugins/Data (ok) · Runtime → Editor (forbid)
```

### 9.5 `Find*` / `Resources.Load` service location

#### Bad

```csharp
var host = Object.FindObjectOfType<DelegationBridgeHost>();
var text = Resources.Load<TextAsset>("Scenarios/Baltic");
```

#### Good

```csharp
public void Construct(EditModeController edit, MapAuthoringSurface map) { /* inject */ }
// SerializeField scene refs for UIDocument, cameras, sibling hosts
```

---

## 10. Agent checklist (before Done)

- [ ] Change classified: **Editor chrome** vs **headless authoring** vs **Runtime execution**
- [ ] New logic prefers **Data Authoring** and/or **UnityAdapter/Authoring** + **CLI** before `EditorWindow`
- [ ] Editor code stays in `ProjectAegis.Unity.Editor` with **`includePlatforms: ["Editor"]`**
- [ ] No player / Runtime reference to Editor assemblies
- [ ] No Editor write to **`DecisionLog`** or live sim step for “preview authority”
- [ ] No Editor **`IOrderSink.ApplyOrder`** / enqueue bypass for play semantics
- [ ] No Editor → Runtime **presentation shortcut** that skips command / projection contracts
- [ ] No **SQLite** from Editor/UI chrome (ADR-006); catalog via Data / approved bridges
- [ ] No production **`Find*` / `Resources.Load`** service location
- [ ] Authoring vs execution split honored (`EditModeController` / document vs `BeginExecution` + `Tick`)
- [ ] DI: presenters composed at host/window
- [ ] Tests: **`dotnet test`** on Authoring/Data first; Play Mode last
- [ ] Scenario map path uses real types (`ScenarioAuthoringSession`, `MapAuthoringSurface`, …)
- [ ] Platform path uses bridges + CLI parity + write gate (ADR-011)
- [ ] PR cites **ADR-010 / 007 / 001** (+ **006** / **011** / **017** as applicable) — **not** Git ADR-018 for presentation

---

## 11. See also

| Doc | Use |
| --- | --- |
| [`../SKILL.md`](../SKILL.md) | Parent doctrine |
| [`aegis-unity-map.md`](aegis-unity-map.md) | “Where does X live?” path index |
| [`asmdefs-and-layers.md`](asmdefs-and-layers.md) | Assembly edges |
| [`presentation-boundary.md`](presentation-boundary.md) | Snapshot wall (**ADR-010/007/001**) |
| [`headless-command-ui.md`](headless-command-ui.md) | Intent → command → engine |
| [`testing-unity.md`](testing-unity.md) | Headless → EditMode → Play Mode |
| [`mono-anti-patterns.md`](mono-anti-patterns.md) | Thin hosts; no Find/Resources |
| `docs/architecture/adr-010-headless-first-command-driven-ui.md` | Headless-first |
| `docs/architecture/adr-006-data-layer-boundary.md` | No SQLite from presentation |
| `docs/architecture/adr-011-platform-editor-excel-roundtrip.md` | PE Excel |
| `docs/architecture/adr-017-editor-topology-client-vs-scenario-lab.md` | Shared core topology |
| `docs/architecture/adr-018-sensor-side-picture-datalink.md` | **Not** this topic |
| `src/ProjectAegis.Delegation.UnityAdapter/Authoring/` | Headless authoring presenters |
| `src/ProjectAegis.Data/Scenario/Authoring/` | Scenario package truth |
| `src/ProjectAegis.MissionEditor.Cli/` | CLI verbs |
| `unity/ProjectAegis/Assets/Editor/` | EditorWindow + build tooling |
| `unity/ProjectAegis/Assets/Scripts/Runtime/` | Play-time hosts |

**UCA-M3 note:** Aegis-specific Editor vs Runtime playbook. Structure pack assembly rules remain in UCA-M2 `asmdefs-and-layers.md`.
