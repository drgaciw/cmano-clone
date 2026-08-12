# Asmdefs & assembly layers

**Skill:** `unity-csharp-architect` · **Parent:** [`../SKILL.md`](../SKILL.md) §5.1 (assemblies & layers)  
**Program:** UCA-M2 · **Audience:** agents adding assemblies, plugins, or dependency edges  
**Implements:** DRG-129 (UCA-05)

> **Law (one line):** Prefer existing `ProjectAegis.*` assemblies; new asmdefs require an **edge list** in the PR; **no `UnityEngine`** in headless core / UnityAdapter; UI never depends on sim internals.

**Related:** [`presentation-boundary.md`](presentation-boundary.md) · [`headless-command-ui.md`](headless-command-ui.md) · [`testing-unity.md`](testing-unity.md) · `/c-sharp-engineer` (layering / DIP)

---

## 1. Two graphs (do not conflate)

Aegis has **two** assembly graphs that meet at precompiled plugins:

| Graph | Where | Mechanism |
| --- | --- | --- |
| **.NET solution** | `src/ProjectAegis.*` | `.csproj` `ProjectReference` — `dotnet build` / `dotnet test` |
| **Unity player/editor** | `unity/ProjectAegis/Assets/**/*.asmdef` | Unity asmdefs + **precompiled** DLLs under `Assets/Plugins` |

```text
src/ (.NET, no UnityEngine)
  Data ──► Sim ──► Delegation ──► Delegation.UnityAdapter
                      │                    │
                      └──── tests ─────────┘
                                           │
              netstandard2.1 publish ──────┘
                                           ▼
unity/ …/Assets/Plugins/ProjectAegis/*.dll   (precompiled)
                                           │
              ProjectAegis.Unity.Runtime (asmdef, thin hosts)
              ProjectAegis.Unity.Runtime.Cesium (optional)
              ProjectAegis.Unity.Editor (Editor only)
              ProjectAegis.Unity.Tests (EditMode/PlayMode)
```

**Publish rule** (`unity/ProjectAegis/README.md`): publish `ProjectAegis.Delegation.UnityAdapter` for **netstandard2.1** and copy **all** publish-output DLLs into Unity. Do **not** drop `net8.0` outputs into `Assets/Plugins` — Unity 6.3 loads netstandard2.1 plugins only.

---

## 2. .NET layer table (allowed edges)

| Assembly | Path | Allowed deps | Forbidden |
| --- | --- | --- | --- |
| `ProjectAegis.Data` | `src/ProjectAegis.Data/` | .NET BCL, SQLite packages | `UnityEngine`, sim/delegation |
| `ProjectAegis.Sim` | `src/ProjectAegis.Sim/` | **Data** only | `UnityEngine`, Delegation, UI |
| `ProjectAegis.Delegation` | `src/ProjectAegis.Delegation/` | **Data**, **Sim** | `UnityEngine`, Unity hosts |
| `ProjectAegis.Delegation.UnityAdapter` | `src/ProjectAegis.Delegation.UnityAdapter/` | **Data**, **Delegation** (and transitive Sim) | **`UnityEngine`** (csproj law) |
| `ProjectAegis.MissionEditor.Cli` | `src/ProjectAegis.MissionEditor.Cli/` | Data / tools stack | Runtime Unity asmdefs |
| `*.Tests` under `src/` | `src/ProjectAegis.*.Tests/` | SUT + NUnit | Production Unity-only APIs |

**UnityAdapter role:** thin bridge — `ISimWorldSnapshot` in, order enqueue / `IOrderSink` out; bridges, `C2PresentationController`, pure presenters. **No `UnityEngine` dependency** (see csproj Description).

**Zero-touch:** `DelegationBridge` hotpath stays frozen through Release v1 — new behavior goes in projections, new adapter types, or Unity hosts, not the bridge hotpath.

---

## 3. Unity asmdef inventory (real files)

| Asmdef name | Path | References | Notes |
| --- | --- | --- | --- |
| `ProjectAegis.Unity.Runtime` | `Assets/Scripts/Runtime/ProjectAegis.Unity.Runtime.asmdef` | *(empty list — uses plugins)* | Thin MB / UI Toolkit hosts (`*PanelHost`, `DelegationBridgeHost`, …) |
| `ProjectAegis.Unity.Runtime.Cesium` | `Assets/Scripts/Runtime/Cesium/…Cesium.asmdef` | Runtime + `CesiumForUnity` | Gated by `CESIUM_FOR_UNITY` / package define |
| `ProjectAegis.Unity.Editor` | `Assets/Editor/ProjectAegis.Unity.Editor.asmdef` | Runtime + Addressables (+ Editor) | **`includePlatforms: ["Editor"]` only** |
| `ProjectAegis.Unity.Tests` | `Assets/Tests/ProjectAegis.Unity.Tests.asmdef` | Runtime + TestRunner; precompiled `ProjectAegis.Delegation.dll`, `ProjectAegis.Delegation.UnityAdapter.dll`, nunit | `UNITY_INCLUDE_TESTS`; `autoReferenced: false` |

Precompiled headless DLLs are **not** asmdef project refs — they are plugin assemblies referenced from Tests via `precompiledReferences` / runtime load.

---

## 4. Allowed / forbidden edges (checklist)

| Prefer / allow | Forbid |
| --- | --- |
| Host MB in `Unity.Runtime` → calls published UnityAdapter types | Unity host → live ECS / `SimulationSession` internals |
| Editor asmdef → Runtime (authoring tools) | Editor → shortcut that **bypasses** command / enqueue façade for authoritative actions |
| Cesium optional asmdef depending on Runtime | Circular asmdefs (`A` ↔ `B`) |
| Headless tests under `src/*Tests` for pure logic | `UnityEngine` usings in `src/ProjectAegis.{Data,Sim,Delegation,Delegation.UnityAdapter}` |
| Document every **new** edge in the PR | New asmdef “because folders” without consumers |
| netstandard2.1 plugin publish path | net8.0 DLL copy into `Assets/Plugins` |

### C2 panel layering (within allowed assemblies)

```text
Projection/* (Delegation)     pure read models
    → Bridge/* (UnityAdapter)  *Bridge.Build glue
    → Binder / PanelState      display rows
    → *PanelHost (Unity.Runtime)  thin MB / UIDocument
```

See `docs/engineering/c2-projection-layer.md` and [`presentation-boundary.md`](presentation-boundary.md).

---

## 5. When to add a new asmdef

Add only when **all** hold:

1. **Boundary need** — platform gate (Editor-only), optional package (`Cesium`), or test isolation.
2. **Clear owner** — one team/surface; not “misc utilities.”
3. **Edge list** ready — every assembly this will reference and who will reference it.
4. **No cycle** — acyclic after add.
5. **Headless stays headless** — never pull `UnityEngine` into `src/` projects to “share code.”

### PR edge-list template (required for new/changed asmdefs)

```markdown
## Asmdef / assembly edges
- **Added / changed:** `ProjectAegis.Unity.Something`
- **References:** A → B, A → C
- **Referenced by:** (none | list)
- **Platforms:** Runtime | Editor | Tests
- **Why not existing assembly:** …
- **UnityEngine in src/?** No
```

---

## 6. c-sharp-engineer cross-cut

| Concern | Assembly expectation |
| --- | --- |
| **Layering** | Pure logic in .NET projects; MB only in Unity Runtime/Editor |
| **DIP** | Depend on `ISimWorldSnapshot`, command façade, projection DTOs — not concrete session guts |
| **SRP** | One assembly ≈ one stability / platform boundary |
| **Testing** | Headless tests in `src/*Tests`; Unity tests only for host/lifecycle |

Handoff: architect edge list (this skill) → `/c-sharp-engineer` implement → `/c-sharp-reviewer` edge + API audit.

---

## 7. Agent checklist (before Done)

- [ ] Change fits an **existing** assembly unless PR has edge list for a new one
- [ ] No `UnityEngine` in Data / Sim / Delegation / UnityAdapter
- [ ] No UI → sim-internals edge; no circular asmdefs
- [ ] Editor code stays in Editor asmdef (`includePlatforms: Editor`)
- [ ] Plugin DLLs are **netstandard2.1** publish outputs, not net8.0
- [ ] `DelegationBridge` hotpath untouched (zero-touch) unless waived
- [ ] Headless logic remains `dotnet test`-able
- [ ] PR cites this skill §5.1 + ADR-010/001 as applicable — **not** Git ADR-018 for presentation

---

## 8. See also

| Doc | Use |
| --- | --- |
| [`../SKILL.md`](../SKILL.md) §5.1 | Parent assembly rules |
| [`presentation-boundary.md`](presentation-boundary.md) | Snapshot / projection wall |
| [`headless-command-ui.md`](headless-command-ui.md) | Command path assembly roles |
| [`testing-unity.md`](testing-unity.md) | Where tests live per assembly |
| `unity/ProjectAegis/README.md` | Plugin publish / copy procedure |
| `docs/engineering/c2-projection-layer.md` | Projection → binder → host |
| `docs/architecture/adr-001-sim-assembly-boundary.md` | Snapshot in / Order out |
| `docs/architecture/adr-010-headless-first-command-driven-ui.md` | UI is a client |

**UCA-M2 note:** Structure pack doctrine for assembly review. Do not re-host full ADRs here — link them.
