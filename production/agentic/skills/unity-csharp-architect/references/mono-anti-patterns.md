# MonoBehaviour anti-patterns

**Skill:** `unity-csharp-architect` · **Parent:** [`../SKILL.md`](../SKILL.md) §5.2 (MonoBehaviour hygiene)  
**Program:** UCA-M2 · **Audience:** agents writing/reviewing Unity hosts, UI Toolkit binders, C2 chrome  
**Implements:** DRG-131 (UCA-07)

> **Law (one line):** MonoBehaviours are **thin scene shells** — lifecycle, bind, and intent capture only. Logic, selection, and sim writes live behind presenters, projections, and command façades.

**Related:** [`presentation-boundary.md`](presentation-boundary.md) · [`headless-command-ui.md`](headless-command-ui.md) · [`performance-unity.md`](performance-unity.md) · `/c-sharp-engineer` (SOLID, DI, allocs, immutability)

---

## 1. Prefer / avoid (at a glance)

| Prefer | Avoid (ban in production) |
| --- | --- |
| Thin view + presenter / projection bind | God MonoBehaviour (sim + UI + I/O + selection + orders) |
| Inject deps at composition root | `Find` / `FindObjectOfType` / `FindObjectsByType` sprawl |
| Intent → command / event → enqueue façade | Direct sim mutation from `Update` / `FixedUpdate` |
| Cache components in `Awake` | `GetComponent` / `Find*` every frame |
| Pool / `NonAlloc` / `Span` / reused buffers | Per-frame LINQ, closures, string concat, `new` lists |
| Unity `== null` + destroyed-object guards | C# `is null` / `?.` alone on `UnityEngine.Object` |
| `[SerializeField] private` + injected services | Public mutable authority fields; `Resources.Load` in prod |
| `C2PresentationController` for selection | Static selected entity driving sim side effects |
| `*Bridge.Build` / `*Projection` read models | Live ECS / session guts cached on the MB |
| Enqueue façade (`C2PlayerCommandBridge` / `HumanController` path) | MB holding or calling `IOrderSink.ApplyOrder` directly |

---

## 2. Role of a MonoBehaviour (Aegis)

```text
Composition root (host / test fixture)
    │  wires snapshot source, bridges, C2PresentationController, command façade
    ▼
Thin MB / UI Toolkit host (*PanelHost, DelegationBridgeHost, …)
    │  Awake: cache components + accept injected deps
    │  OnEnable: subscribe presentation events
    │  Bind: apply IReadOnly* projection rows
    │  Input: capture intent → C2PlayerCommandBridge / façade.Enqueue
    ▼
UnityAdapter presenters + *Bridge.Build  (engine-free where practical)
    │  read: ISimWorldSnapshot → projection DTOs
    │  write intent: command → queue → IOrderSink (not from MB)
    ▼
Sim / Delegation (authoritative)
```

| MB may | MB must not |
| --- | --- |
| Own scene refs, USS, camera, layout chrome | Own sim truth, order log, ROE, magazines |
| Call `Bind(IReadOnlyList<…>)` on projection output | Hold `EntityQuery`, live session, or write handles |
| Forward clicks to a presenter / command façade | Call `IOrderSink.ApplyOrder` or append `DecisionLog` |
| Interpolate between last two **received** snapshots | Step sim / roll RNG / invent contacts in `Update` |
| Use `C2PresentationController` for selection UI | Treat selection as sim authority without a command |

Parent seams: [`presentation-boundary.md`](presentation-boundary.md) · [`headless-command-ui.md`](headless-command-ui.md).

---

## 3. Anti-pattern catalog

### 3.1 God MonoBehaviour

**Smell:** One type builds projections, mutates selection, issues orders, opens SQLite, draws map chrome, and steps the session.

#### Bad

```csharp
// BAD: god-MB — authority + chrome + Find sprawl in one type
sealed class C2GodPanel : MonoBehaviour
{
    SimulationSession _session;
    DecisionLog _log;
    EntityQuery _units;

    void Update()
    {
        _session ??= FindObjectOfType<SimulationSessionHost>().Session;
        _log.Append(/* UI chrome */); // BAD: DecisionLog write from UI
        foreach (var e in _units.ToEntityArray(Allocator.Temp))
            DrawRaw(e); // BAD: live ECS, not MapPictureBridge
        if (Input.GetKeyDown(KeyCode.H))
            _session.ForceHold(Selected); // BAD: bypass queue / sink
    }
}
```

#### Good

```csharp
// GOOD: thin host + presentation controller + bridge bind + enqueue façade
sealed class MapPanelHost : MonoBehaviour
{
    [SerializeField] private UIDocument _document;
    C2PresentationController _c2;
    // composition injects façade / bridge refs — not Find*

    public void Construct(C2PresentationController c2) => _c2 = c2;

    public void Bind(IReadOnlyList<MapSymbolEntry> symbols)
    {
        // apply USS / visual elements only
    }

    void OnHoldClicked()
    {
        if (_c2.SelectedUnitId is not { Length: > 0 } id) return;
        // C2PlayerCommandBridge.TryIssue(bridge, entity, "hold", simTime, out _);
    }
}

// Tick host (composition) — not the view
void OnSimTick(ISimWorldSnapshot snapshot, /* registry, log, seed */)
{
    var symbols = MapPictureBridge.Build(snapshot, registry, log, seed);
    _mapPanel.Bind(symbols);
}
```

**Aegis anchors:** selection → `C2PresentationController`; map rows → `MapPictureBridge.Build` → bind; orders → `C2PlayerCommandBridge` / `DelegationBridge.TryEnqueueHumanOrder` — **not** `IOrderSink` from the MB.

---

### 3.2 Find* / Resources.Load sprawl

| Allowed (narrow) | Forbidden in production |
| --- | --- |
| Editor tooling / one-shot migration helpers (explicit) | Runtime C2 / map / order hosts |
| Test fixtures that build a scene graph | “Soft” service location from any MB |
| Addressables / explicit asset refs via composition | `Resources.Load` as default asset path |

```csharp
// BAD
_sink = FindObjectOfType<OrderSinkBehaviour>();
_icon = Resources.Load<Sprite>("Icons/Friendly");

// GOOD — composition root wires Construct(...); SerializeField for UIDocument/cameras
```

---

### 3.3 Direct sim mutation from Update / FixedUpdate

| Frame loop may | Frame loop must not |
| --- | --- |
| Interpolate last two **received** snapshots | Call `RunTick` / `Step` for cosmetics |
| Animate USS, camera, selection chrome | Mutate alive / contacts / magazines |
| Read dirty flags set by tick host | `DecisionLog.Append` / direct `IOrderSink` |
| Forward input → command DTO | `transform` as unit authority |

---

### 3.4 Hot-path allocations

| Prefer | Avoid on hot paths |
| --- | --- |
| Pre-sized lists / pooled buffers (`MapSymbolPool`) | `symbols.Where(...).ToList()` every frame |
| `Physics.RaycastNonAlloc`, reuse arrays | `Physics.RaycastAll` allocating |
| `Span<T>` / reused buffers | Closure-heavy LINQ in bind loops |
| Cached labels / fixed strings | `$"Unit {id} fuel={x:F2}"` per symbol per frame |
| Rebuild projection on **sim tick** / dirty flag | Full `MapPictureBridge.Build` every `Update` |

**Depth budgets:** [`performance-unity.md`](performance-unity.md).

---

### 3.5 Unity null and destroyed objects

| Rule | Detail |
| --- | --- |
| Compare Unity objects with `== null` / `!= null` | Not `is null` / `is not null` alone |
| Guard after async / disable / scene unload | Early-out if host destroyed |
| Clear on `OnDestroy` / `OnDisable` | Drop strong refs to presenters/façades |
| Plain C# deps use normal nullability | `C2PresentationController` is not a Unity object |

```csharp
// BAD: may miss destroyed UIDocument
if (_document is null) return;

// GOOD: Unity fake-null aware
if (_document == null) return;
```

---

### 3.6 Wrong write seam (IOrderSink / DecisionLog from MB)

1. **Read:** `ISimWorldSnapshot` → `*Bridge.Build` / `*Projection` → `IReadOnly*` bind.  
2. **Write intent:** `C2PlayerCommandBridge.TryIssue` / `HumanController.Enqueue` path — **not** MB → `IOrderSink.ApplyOrder`.  
3. **Map UI never writes `DecisionLog`** (ADR-007). Selection is presentation-only on `C2PresentationController`.

---

## 4. c-sharp-engineer cross-cut

| Concern | Expectation on MonoBehaviours |
| --- | --- |
| **SRP** | View binds + forwards intent; presenter/bridge owns logic |
| **DIP** | Depend on snapshot (read), command façade (write intent), not concrete session |
| **DI** | `Construct` / host wires deps; no new mutable singletons for game state |
| **Allocations** | Zero unexplained alloc on `Update`/`FixedUpdate`/per-frame bind |
| **Immutability** | Bind `IReadOnlyList<T>` / records; do not mutate shared projection rows |
| **Inspector** | `[SerializeField] private`; no public gameplay authority fields |
| **Components** | Cache in `Awake`; never `GetComponent` in hot loops |
| **Async** | No `async void` except UI handlers; honor `CancellationToken`; re-bind after await |

---

## 5. Refactor recipe (god-MB → thin host)

1. **Classify** fields: scene chrome · presentation state · sim authority · asset refs.  
2. **Extract** presentation state to `C2PresentationController` (or pure presenter).  
3. **Extract** read path to `*Projection` / `*Bridge.Build`; bind `IReadOnly*`.  
4. **Extract** write path to `C2PlayerCommandBridge` / enqueue façade.  
5. **Wire** at composition root; delete production `Find*` / `Resources.Load`.  
6. **Prove** presenter/bridge with `dotnet test` before Play Mode smoke.  
7. **Profile** hot path per [`performance-unity.md`](performance-unity.md).

---

## 6. Agent checklist (before Done)

- [ ] MB is a **thin view** (lifecycle + bind + intent) — not a god-MB  
- [ ] No production `Find*` / `FindObjectOfType` / `Resources.Load`  
- [ ] Components cached in `Awake` from serialize/inject  
- [ ] No direct sim / `DecisionLog` / `IOrderSink.ApplyOrder` from frame loops  
- [ ] Intent goes through **command / enqueue façade**  
- [ ] Selection via `C2PresentationController` (presentation-only)  
- [ ] Map/C2 reads via `MapPictureBridge.Build` / `*Projection`  
- [ ] Hot path: no per-frame LINQ / closures / string concat  
- [ ] Unity objects checked with `== null`  
- [ ] Projection rows treated as immutable/`IReadOnly*` for the bind window  
- [ ] Deeper budgets checked against [`performance-unity.md`](performance-unity.md)  
- [ ] PR cites this skill §5.2 + ADR-010/001/007 — **not** Git ADR-018 for presentation  

---

## 7. See also

| Doc | Use |
| --- | --- |
| [`../SKILL.md`](../SKILL.md) §5.2 | Parent MonoBehaviour hygiene |
| [`presentation-boundary.md`](presentation-boundary.md) | Snapshot wall; no DecisionLog from UI |
| [`headless-command-ui.md`](headless-command-ui.md) | Intent → command → engine |
| [`performance-unity.md`](performance-unity.md) | Budgets, pooling, profiling |
| `src/ProjectAegis.Delegation.UnityAdapter/` | Bridges, `C2PresentationController` |
| `unity/ProjectAegis/Assets/Scripts/Runtime/*Host.cs` | Thin host examples |

**UCA-M2 note:** Structure pack doctrine for MonoBehaviour review/refactor.
