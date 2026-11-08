# Unity & adapter testing placement

**Skill:** `unity-csharp-architect` · **Parent:** [`../SKILL.md`](../SKILL.md) §6–7 (phases / finish)  
**Program:** UCA-M2 · **Audience:** agents choosing where to put tests for Unity/C# work  
**Implements:** DRG-132 (UCA-08 part: testing)

> **Law (one line):** **Headless first** (`dotnet test` on adapter/delegation/sim) — EditMode for pure Unity types that still run without Play Mode — Play Mode **last** for host lifecycle smoke.

**Related:** [`asmdefs-and-layers.md`](asmdefs-and-layers.md) · [`performance-unity.md`](performance-unity.md) · [`headless-command-ui.md`](headless-command-ui.md) · `/c-sharp-engineer` testing concerns

---

## 1. Placement matrix

| What you changed | Put tests in | Runner | Notes |
| --- | --- | --- | --- |
| Projection / binder / pure DTO | `src/ProjectAegis.Delegation.Tests` or `…UnityAdapter.Tests` | `dotnet test` | No UnityEngine |
| Bridge, `C2PresentationController`, command façade | `src/ProjectAegis.Delegation.UnityAdapter.Tests/**` | `dotnet test` | Engine-free adapter |
| Sim tick / orders / determinism | `src/ProjectAegis.Sim.Tests`, Delegation.Tests | `dotnet test` | Prefer over Play Mode |
| Data catalog / scenario gates | `src/ProjectAegis.Data.Tests` | `dotnet test` | No UI |
| `MapSymbolPool`, UI Toolkit pool logic | `unity/…/Assets/Tests` **EditMode** | Unity Test Runner | Prefers no scene enter when possible |
| `DelegationBridgeHost` lifecycle, full C2 smoke | `unity/…/Assets/Tests` **Play Mode** | Unity Test Runner | Last mile only |
| Perf bind budget | `…UnityAdapter.Tests/Bridge/C2PanelPerfBenchTests.cs` | `dotnet test` | Headless wall-clock |

---

## 2. Real test homes (anchors)

### Headless (preferred)

| Tree | Examples |
| --- | --- |
| `src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/` | `DelegationBridgeTests`, `C2PlayerCommandBridgeTests`, `Map`/`Sensor`/`MessageLog` bridges, `C2PanelPerfBenchTests` |
| `src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/` | `C2PresentationControllerTests`, selection set |
| `src/ProjectAegis.Delegation.Tests/` | Controllers, queues, projections |
| `src/ProjectAegis.Sim.Tests/` | Tick pipeline, clock |

```bash
# Typical agent command (CI-friendly)
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter C2PanelPerfBenchTests
```

### Unity assembly

| File / asmdef | Role |
| --- | --- |
| `unity/ProjectAegis/Assets/Tests/ProjectAegis.Unity.Tests.asmdef` | References Runtime + precompiled Delegation/UnityAdapter DLLs |
| `MapSymbolPoolTests.cs` | EditMode — pool reuse |
| `C2DelegationSmokeTests.cs` | Play Mode — host + stub world + frames |

Unity tests use `#if UNITY_5_3_OR_NEWER` guards and precompiled **netstandard2.1** plugins (see [`asmdefs-and-layers.md`](asmdefs-and-layers.md)).

---

## 3. Headless-first recipe

1. **Extract** pure logic to UnityAdapter / Delegation (no `UnityEngine`).  
2. **Write** NUnit tests with stub `ISimWorldSnapshot` / sink (see existing `SimWorldSnapshotStub`, smoke stubs).  
3. **Prove** command path via `C2PlayerCommandBridge` / `TryEnqueueHumanOrder` — not MB `ApplyOrder`.  
4. **Only then** add EditMode (pooling, USS) or Play Mode (Awake host, multi-frame).  
5. **Play Mode** must not be the only proof of architecture-critical behavior.

### Stubbing pattern (good)

```csharp
// GOOD: stub world implements ISimWorldSnapshot + records ApplyOrder for assertions
// (see C2DelegationSmokeTests.StubWorld / SimWorldSnapshotStub)
```

### Anti-pattern

```csharp
// BAD: only Play Mode test that clicks UI and asserts sim state with no headless coverage
// BAD: test that requires FindObjectOfType into a full scenario scene for pure binder math
```

---

## 4. What each layer must prove

| Layer | Must prove | Need not prove in that layer |
| --- | --- | --- |
| Projection | Deterministic rows from log/snapshot | USS layout pixels |
| Bridge / façade | Enqueue rules, failure reasons, registry gates | Camera feel |
| Presentation controller | Selection-only side effects, graph clear on retarget | Sim magazines |
| Host MB | Bind wiring, no throw on tick, pool sync | Full engagement math |
| Perf bench | Bind under budget | GPU fill rate |

---

## 5. c-sharp-engineer testing concerns

| Concern | Expectation |
| --- | --- |
| **Seams** | Logic injectable/stubable without scene |
| **Immutability** | Assert on records / `IReadOnly*` outputs |
| **Determinism** | Fixed seeds (`layoutSeed`, scenario ids) — no wall-clock in asserts |
| **Isolation** | One behavior per test; prefer pure arrange-act-assert |
| **CI** | Default proof path is `dotnet test` on src projects |

---

## 6. Agent checklist (before Done)

- [ ] Pure logic covered under **`dotnet test`** where practical  
- [ ] No architecture-critical behavior **only** covered by Play Mode  
- [ ] New bridge/presenter has tests next to existing Bridge/Presentation folders  
- [ ] Unity tests stay in `ProjectAegis.Unity.Tests` asmdef with correct precompiled refs  
- [ ] Perf-sensitive bind changes update or re-run `C2PanelPerfBenchTests`  
- [ ] Command path tests use façade/enqueue — not direct UI→sink hacks  
- [ ] PR lists test commands / filters run  

---

## 7. See also

| Doc | Use |
| --- | --- |
| [`asmdefs-and-layers.md`](asmdefs-and-layers.md) | Test assembly edges |
| [`performance-unity.md`](performance-unity.md) | Bench budgets |
| [`headless-command-ui.md`](headless-command-ui.md) | Shared CLI/MCP/test command model |
| `docs/engineering/c2-projection-layer.md` | Projection testability |
| `unity/ProjectAegis/README.md` | Plugin publish for Unity Test Runner |
| `docs/architecture/adr-010-headless-first-command-driven-ui.md` | Headless-first law |

**UCA-M2 note:** Placement doctrine only — full PR finish checklist lands in UCA-M4 (`checklists/pr-finish.md`).
