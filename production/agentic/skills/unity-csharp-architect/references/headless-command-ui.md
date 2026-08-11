# Headless-Command UI (ADR-010 operational)

> **Skill:** [`unity-csharp-architect`](../SKILL.md) · **Section:** [§4 Command-driven UI](../SKILL.md#4-command-driven-ui-adr-010-distilled)  
> **Normative ADR:** `docs/architecture/adr-010-headless-first-command-driven-ui.md` (Accepted 2026-06-03)  
> **Related:** [`presentation-boundary.md`](presentation-boundary.md) (ADR-010 §2–3, ADR-007, ADR-001) · Req 20 C2 UI  
> This file is doctrine for agents, not a re-host of the ADR.

**Law (one line):** Player intent → **command** → engine; UI is a **client** (projections in, commands out). Prove it headless first; Play Mode smoke is last.

---

## 1. Intent → command → engine

| Stage | Owns | Lives in | Must not |
| --- | --- | --- | --- |
| **Intent** | Pointer/key/UI Toolkit event, menu click, MCP/CLI verb | `ProjectAegis.Unity` hosts, CLI, MCP | Mutate sim, scenario, catalog, or order log |
| **Command** | Validated, deterministic payload (`IssueOrder`, `SetReferencePoint`, …) | Enqueue façade / player-command services → `HumanController` / queue (not direct `IOrderSink` from UI) | Depend on `UnityEngine`, scene objects, or inspector state; call `IOrderSink.ApplyOrder` from a view |
| **Engine** | Apply order / scenario edit / doctrine override; log for replay | `Sim` / `Delegation` / `Data` | Read UI selection as authority |

```text
[CLI | MCP | Unity view]  --intent-->  [command bus / HumanController.Enqueue]
                                              |
                                              v
                                    PlayerOrderExecutionQueue  (comms delay ok)
                                              |
                                              v
                         Delegation tick / sim apply  --IOrderSink.ApplyOrder-->  systems
                                              ^
                                              |
                         ISimWorldSnapshot  <----  sim / ECS host (read-only to UI)
                                              |
                                              v
                         *Bridge projections / C2 view models  -->  UI bind
```

**Shared command model:** the same command types and order semantics must be driveable from **CLI**, **MCP**, **Unity runtime UI**, and **headless tests**. If Unity is the only way to perform an authoritative action, the design is wrong.

### Command examples (from ADR-010)

| Command (conceptual) | Intent | Authority path |
| --- | --- | --- |
| `IssueOrder` | Player issues movement / hold / engage / etc. | → `Order` → `HumanController.Enqueue` → `PlayerOrderExecutionQueue` → drain → `IOrderSink.ApplyOrder` |
| `SetReferencePoint` | Set RP for mission / unit | Scenario/mission command service (headless-validatable) |
| `AssignUnitToMission` | Attach unit to mission | Mission-editor / scenario command path (CLI/MCP parity) |
| `SetDoctrineOverride` | ROE/EMCON/WRA override | Policy/doctrine command + order-log / decision trail |

Presentation-only actions (camera, basemap layers, panel layout, selection focus) are **not** commands on this bus — they must never enter replay hashes or sim authority.

---

## 2. Assembly table (allowed edges)

| Assembly / area | Allowed deps | Role in command UI |
| --- | --- | --- |
| `ProjectAegis.Data` | .NET only — **no `UnityEngine`** | Catalog, scenario packages, validation gates |
| `ProjectAegis.Sim` | .NET only | Rules, sensors, engagement, order-log semantics |
| `ProjectAegis.Delegation` | .NET only | Controllers, autonomy, decision pipeline, `PlayerOrderExecutionQueue` |
| `ProjectAegis.Delegation.UnityAdapter` | .NET only (unless ADR revises) | `ISimWorldSnapshot` in, `IOrderSink` out; C2 bridges; `C2PresentationController` |
| DOTS / Unity ECS systems | Entities / Burst / Jobs | Snapshot builders, order application buffers — not domain law owners |
| `ProjectAegis.Unity` | UnityEngine / UI Toolkit | Thin hosts: bind projections, capture intent → dispatch commands |

**Forbidden:** UI → live sim internals; Editor-only shortcuts that mutate runtime authority; Odin (or similar) on core sim/catalog/replay/scenario paths.

---

## 3. Real seams (anchors)

### 3.1 `ISimWorldSnapshot` — projections / read path

```csharp
// src/ProjectAegis.Delegation.UnityAdapter/Bridge/ISimWorldSnapshot.cs
// Per-tick world snapshot supplied by the sim/ECS layer.
public interface ISimWorldSnapshot
{
    double SimTime { get; }
    int ContactCount { get; }
    // … contacts, EMCON, preferred hostiles, etc.
}
```

- Hosts build one snapshot per tick; UI **never** holds write access to ECS chunks or session guts.
- Bridges (`MapPictureBridge`, `SensorC2Bridge`, `UnitDetailBridge`, …) project into **read-only** view models.

### 3.2 `IOrderSink` — command / write path

```csharp
// src/ProjectAegis.Delegation.UnityAdapter/Bridge/IOrderSink.cs
public interface IOrderSink
{
    void ApplyOrder(EntityKey entity, in Order order);
}
```

- **UI must not call `IOrderSink` directly.** Presentation enqueues via `C2PlayerCommandBridge` / `HumanController.Enqueue` / player-command services.
- `IOrderSink.ApplyOrder` is the **downstream** apply after queue drain / bridge tick (or headless harness equivalent).
- `OrderDispatcher` maps drained orders + `TargetRegistry` → sink.

### 3.3 `PlayerOrderExecutionQueue` + `HumanController`

```csharp
// Delegation: player orders held until ExecuteSimTick (req 19 comms delay)
// HumanController.Enqueue(order, executeSimTick) → queue
// DrainIssuedOrders(currentSimTick) → ready orders only
```

- Player-facing “issue order” UI must **enqueue**, not call movement/weapons systems from `Update`.
- Comms delay and queue drain timing are **headless-testable** — do not require Play Mode to prove.

### 3.4 Presenters engine-free where possible

| Prefer in | Examples |
| --- | --- |
| UnityAdapter / pure C# | `C2PresentationController` (selection, graph highlight — **presentation-only**), `*Bridge.Build` projections |
| Unity hosts only | UI Toolkit bind, input → command DTO, layout |

Selection is presentation state; it is **not** sim authority. Multi-select / primary unit id live on the presentation controller, not on ECS components.

---

## 4. DI / composition root (no static mutable UI state)

| Rule | Good | Bad |
| --- | --- | --- |
| Composition root | Scene/host or test fixture wires `ISimWorldSnapshot`, `IOrderSink`, command bus, presenters once | Global static `UIState.CurrentSelection` mutated from anywhere |
| Command bus | Injected interface: `ICommandBus` / app service / `HumanController` façade | Static `OrderAPI.Issue(...)` with hidden session |
| Presenters | Constructor-injected projections + sink; pure methods unit-tested | MonoBehaviour fields that cache live `SimulationSession` |
| Singletons | Avoid; if host singleton required, document as **presentation host only** + waiver | Scene singleton that is the only path to mutate doctrine |

### Sketch — good

```csharp
// Composition root (host or test)
var bridge = new DelegationBridge(globalSeed: 42);
var c2 = new C2PresentationController();
var bus = new PlayerCommandFacade(bridge); // enqueues to HumanController / services

// View (thin)
void OnIssueHoldClicked()
{
    bus.Submit(new IssueOrderCommand(/* target, OrderKind.Hold, … */));
}

// Presenter (engine-free) — unit-testable
public sealed class OrderToolbarPresenter
{
    private readonly IPlayerCommandSink _sink;
    public OrderToolbarPresenter(IPlayerCommandSink sink) => _sink = sink;

    public void OnHold(string unitId, ulong executeTick) =>
        _sink.EnqueueHold(unitId, executeTick);
}
```

### Sketch — bad

```csharp
// BAD: static mutable authority + Unity hot path mutates sim
public static class C2Globals
{
    public static Unit Selected;
    public static SimulationSession Session;
}

void Update()
{
    if (Input.GetKeyDown(KeyCode.H))
        C2Globals.Session.ForceHold(C2Globals.Selected); // bypasses queue, log, CLI parity
}
```

**c-sharp-engineer layering:** Data/Sim/Delegation pure → UnityAdapter seams → Unity hosts. Dependencies point **inward** toward pure C#; never outward from sim into UI.

---

## 5. Async (UI work)

| Rule | Detail |
| --- | --- |
| Prefer | `Task` / Unity `Awaitable` for I/O, catalog load, long validation |
| Forbidden | `async void` except **UI event handlers** that cannot return `Task` |
| Long work | Pass `CancellationToken`; cancel on panel close / host disable / scene unload |
| After await | Re-bind from **fresh projection** — do not assume cached view models are still valid |
| Sim tick | Do not `await` inside authoritative tick paths; enqueue command, let sim clock advance |

### Sketch — good

```csharp
async Awaitable OnExportClicked(CancellationToken ct)
{
    var report = await _validation.ValidateAsync(scenarioId, ct);
    _panel.Bind(report); // projection / DTO, not live session
}
```

### Sketch — bad

```csharp
async void OnExportClicked() // no CT; fire-and-forget over session
{
    await Task.Delay(500);
    Session.Export(); // races with tick; no cancel
}
```

---

## 6. Headless-first test order

Prove bottom-up. **Play Mode smoke is last, not first.**

| Order | Layer | What to prove | Typical host |
| --- | --- | --- | --- |
| **1** | Unit | Command construction, validation, queue drain, presenter pure logic | `dotnet test` / NUnit |
| **2** | Presenter / bridge | Projection bind outputs for fixed snapshot + order log | Headless (`UnityAdapter` tests) |
| **3** | Integration | CLI/MCP same command as UI path; scenario parity | `MissionEditor.Cli`, harnesses |
| **4** | Replay / golden | Order log + fingerprint unchanged for non-UI changes | Baltic harness / golden hash |
| **5** | Play Mode smoke | Host wiring only | Editor / Player when available |

**Do not** open the Editor to prove: queue drain order, doctrine override acceptance, projection field mapping.

**Do** use Play Mode only for: UI Toolkit layout, input binding, scene host registration, visual smoke.

---

## 7. Good / bad UI paths

| Concern | Good | Bad |
| --- | --- | --- |
| Issue order | Capture intent → `IssueOrder` / `Order` → enqueue on `HumanController` | `transform.position = …` or direct weapon fire from MB |
| Doctrine | `SetDoctrineOverride` command + headless test of effective policy | Toggle SO / inspector field as runtime ROE |
| Map | Bind `MapPictureProjection` / `MapSymbolEntry` | Scene objects as unit authority |
| Selection | `C2PresentationController.SelectFriendlyUnit` | Static selected entity id driving sim side effects without command |
| Mission assign | `AssignUnitToMission` via same path as CLI | Drag-drop mutates scenario JSON only in memory of the panel |
| Validation | Export uses service re-validation, not cached UI “green check” | “Panel said valid” gates play |

---

## 8. Waiver path (when headless proof is impossible)

If a change **cannot** be proven headless:

1. **PR note** (required): what is Unity-only, why headless is impossible, residual risk.
2. Prefer a **new ADR** (or ADR amendment) when the exception is structural (new serializer, scene authority, Odin on core, breaking CLI parity).
3. Checklist: still unit-test any pure extractable core; isolate Unity-only surface behind a thin host.
4. Do **not** silently skip CLI/MCP parity for authoritative actions.

Odin / third-party inspector serializers on core paths remain **not approved** (ADR-010 §5); editor-only convenience needs explicit architecture review / ADR.

---

## 9. Agent finish gates (this reference)

- [ ] Authoritative action is a **command** (or waiver + ADR/PR note).
- [ ] Presenter logic lives engine-free (UnityAdapter / pure C#) where practical.
- [ ] Tests: unit/presenter **before** Play Mode; Play Mode is smoke-only.
- [ ] DI: no new static mutable UI authority; composition root wires bus + presenters.
- [ ] Async: no `async void` except event handlers; `CancellationToken` on long UI work.
- [ ] Seams respected: read via snapshot/projections; write via order sink / command services.
- [ ] CLI/MCP/Unity share the same command model for the new action.
- [ ] PR cites ADR-010 + this skill ([§4](../SKILL.md#4-command-driven-ui-adr-010-distilled)).

---

## 10. Pointers (do not paste ADRs)

| Need | Path |
| --- | --- |
| Full decision record | `docs/architecture/adr-010-headless-first-command-driven-ui.md` |
| Skill overview / command section | `production/agentic/skills/unity-csharp-architect/SKILL.md` §4 |
| Presentation boundary | `references/presentation-boundary.md` (ADR-010/007/001 — **not** Git ADR-018) |
| Snapshot / order seams | `src/ProjectAegis.Delegation.UnityAdapter/Bridge/` (`ISimWorldSnapshot`, `IOrderSink`, `OrderDispatcher`) |
| Player order queue | `src/ProjectAegis.Delegation/Decision/PlayerOrderExecutionQueue.cs` · `Controllers/HumanController.cs` |
| Adapter README | `src/ProjectAegis.Delegation.UnityAdapter/README.md` |
| C2 product requirements | `Game-Requirements/requirements/20-Command-And-Control-UI.md` |
| Play Mode host pattern | `unity/ProjectAegis/` — `SimplePlayModeSimHost` + `DelegationBridgeHost` |

---

**UCA-M1 doctrine note:** Distill and enforce ADR-010 operationally; never re-host the full ADR body here. When product code diverges, fix the code or file an ADR — do not weaken this reference.
