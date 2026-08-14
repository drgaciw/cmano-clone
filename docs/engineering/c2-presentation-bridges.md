# C2 presentation bridges — developer guide

This is the **adapter seam** that turns simulation + order-log state into the read-only view models a
Unity C2 panel binds to. It sits *between* the pure projection layer
([c2-projection-layer.md](c2-projection-layer.md), `ProjectAegis.Delegation/Projection/`) and the Unity
`*PanelHost` MonoBehaviours: each bridge is a thin façade in
[`ProjectAegis.Delegation.UnityAdapter/Bridge/`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/)
that a panel host calls instead of reaching into the projection library directly.

Why a seam at all? Two reasons, both load-bearing:

- **Traceability.** Panel hosts call the adapter, the adapter calls the projection. That makes the
  `host → adapter → projection` edge explicit so GitNexus (and a human reader) can trace exactly which
  panel consumes which projection — instead of every MonoBehaviour statically depending on ~75
  projection types.
- **The presentation boundary (ADR-010 §2–3, ADR-007, ADR-001).** Everything on this path is
  **read-only**: bridges consume an `ISimWorldSnapshot`, a `DecisionLog`, a `TargetRegistry`, and
  (optionally) a `ScenarioPolicyProfile`, and return **immutable** projection records. They never hold a
  live session/ECS write handle and never mutate sim authority or the order log. This is what keeps the
  tactical picture replay-safe (the Baltic v2 hash `17144800277401907079` is untouched by anything a
  panel draws).

This layer landed incrementally as the **UCA "product dogfood" waves** — `MapPictureBridge` (UCA-M5 /
DRG-123), `OobTreeBridge` + `MessageLogBridge` (UCA-A4 / DRG-140/141), and the
`SensorC2` + `UnitDetail` + `MissionList` façade-hygiene wave (UCA-P1 / DRG-144/145/146) — plus the
selection state controller from the earlier C2 rev-2 polish (S37-04 / S39-03). It is the read-side
counterpart of the input path documented in
[player-command-issuance.md](player-command-issuance.md). It is verified against source and
pinned by the tests listed at the end.

- **Bridge façades:** [`MapPictureBridge`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/MapPictureBridge.cs),
  [`OobTreeBridge`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/OobTreeBridge.cs),
  [`MessageLogBridge`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/MessageLogBridge.cs),
  [`SensorC2Bridge`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/SensorC2Bridge.cs),
  [`UnitDetailBridge`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/UnitDetailBridge.cs),
  [`MissionListBridge`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/MissionListBridge.cs).
- **Panel-bind seam:** [`ISensorC2PanelBridge`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/ISensorC2PanelBridge.cs)
  + its default [`SensorC2PanelBridge`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/SensorC2PanelBridge.cs).
- **Selection state:** [`C2PresentationController`](../../src/ProjectAegis.Delegation.UnityAdapter/Presentation/C2PresentationController.cs)
  (`Presentation/`).
- **Feed contract + host:** [`IC2PresentationFeed`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/IC2PresentationFeed.cs)
  is the read-only surface a Unity host exposes; `DelegationBridgeHost` (in
  [`unity/ProjectAegis/Assets/Scripts/Runtime/DelegationBridgeHost.cs`](../../unity/ProjectAegis/Assets/Scripts/Runtime/DelegationBridgeHost.cs))
  implements it and refreshes the feed each tick.
- **Related:** the projections these wrap and their `Projection → Binder → State` layering are
  [c2-projection-layer.md](c2-projection-layer.md); the write/intent side is
  [player-command-issuance.md](player-command-issuance.md); the `DelegationBridge` /
  `ISimWorldSnapshot` / `IOrderSink` integration contract is the
  [UnityAdapter README](../../src/ProjectAegis.Delegation.UnityAdapter/README.md).

---

## Design invariants — never break these

These are load-bearing and enforced by tests. Preserve them when adding a panel or bridge.

| Invariant | Rule |
|-----------|------|
| **Read-only / no sim writes** | A bridge takes a read-only snapshot + `DecisionLog` (+ registry / policy) and returns an immutable view model. It never mutates the sim, the order log, the orchestrator, or the clock. This is the ADR-010 §2–3 presentation boundary — the UI is a *client*, not sim authority. |
| **Projection is the single source of truth** | Bridges do **not** re-implement projection logic; each one just calls the matching `*Projection` type in `ProjectAegis.Delegation/Projection/`. Formatting/filtering rules live in the projection, so headless projection tests and the Unity-facing bridge can't drift. |
| **Off the `DelegationBridge.Tick` hotpath** | Bridges are host seams called *after* `Bridge.Tick(...)` returns. The `UnitDetailBridge` overloads that take a `DelegationBridge` only call **existing** public APIs (`GetAttackMenuOptions`); they do not add logic to `Tick`. `DelegationBridge.cs` stays zero-touch (Baltic v2 hash unchanged). |
| **`null` picture is honest, not an exception** | A missing timeline / empty world projects to an **empty** list, not `null` and not a throw — e.g. `MissionListBridge.ProjectFrom(null)` returns an empty `IReadOnlyList`. Argument-contract violations (a `null` snapshot / registry / log) still throw `ArgumentNullException`. |
| **No `UnityEngine` dependency** | The whole adapter project targets `netstandard2.1` and has no `UnityEngine` reference, so every bridge is exercised by plain `dotnet test`. That's why the null-guards are hand-written `if (x is null) throw` (no `ArgumentNullException.ThrowIfNull`). |
| **Selection side effects stay coordinated** | `C2PresentationController` selection mutates only through `SelectFriendlyUnit` / `SelectHostileContact` / `ApplyDefaultSelection` so unit-vs-contact selection and graph-surfacing highlights never desync (a bound graph panel must not keep stale highlights for a no-longer-selected unit — regression `qa-loop-08`). |

---

## The bridges

Every bridge is a `static` façade (the one exception is the swappable `SensorC2Bridge.PanelBridge` seam,
below). Inputs are read-only; outputs are immutable projection records.

| Bridge | Call | Inputs | Returns | Wraps |
|--------|------|--------|---------|-------|
| `OobTreeBridge` | `Build(snapshot, registry)` | registry member ids + `snapshot.IsMemberAlive` | `IReadOnlyList<OobTreeEntry>` | `OobTreeProjection.Project` |
| `MapPictureBridge` | `Build(snapshot, registry, log, layoutSeed)` | OOB alive-state + `DecisionLog` contact picture + a deterministic `layoutSeed` | `IReadOnlyList<MapSymbolEntry>` | `OobTreeProjection` + `ContactPictureProjection` → `MapPictureProjection.Project` |
| `MessageLogBridge` | `ProjectFrom(log)` / `ProjectCombatMessages(log)` | `DecisionLog` | `IReadOnlyList<MessageLogLine>` (full AAR log, or the combat strip: `KILL_CONFIRMED` / `INTERCEPT_SUCCESS` / `HIT` / `MISS` / `MAGAZINE`) | `MessageLogProjection.Project` |
| `SensorC2Bridge` | `Build(snapshot, log)` then `BindPanel(snapshot)` | EMCON / fire-control / engagement indicators off the snapshot + `DecisionLog` contact lifecycle | `SensorC2Snapshot` → `SensorC2PanelState` | `SensorC2Projection.Build` → `SensorC2PanelBinder.Bind` |
| `UnitDetailBridge` | `BuildPrimary(...)` / `BuildSelected(unitId, ...)` | alive-state + `DecisionLog` + `ScenarioPolicyProfile` + `SimTime` (+ optional `DelegationBridge` for the live attack menu) | `UnitDetailEntry?` | `UnitDetailProjection.ProjectPrimary` / `ProjectSelected` |
| `MissionListBridge` | `ProjectFrom(timeline)` | optional `ScenarioMissionTimeline` (`null` → empty) | `IReadOnlyList<MissionListEntry>` | `MissionListProjection.Project` |

Notes:

- **`SensorC2Bridge` is two-step and swappable.** `Build` projects the snapshot; `BindPanel` maps it to
  UI Toolkit panel state through the `ISensorC2PanelBridge` seam (`PanelBridge`, defaulting to
  `SensorC2PanelBridge.Default`). Panel hosts call the seam rather than `SensorC2PanelBinder` directly so
  the `host → adapter → projection` edge stays traceable; tests can substitute a fake panel bridge.
- **`UnitDetailBridge` and the attack menu.** The `(snapshot, registry, log, policy, …)` overloads are
  pure projection. The `(snapshot, DelegationBridge, …)` overloads additionally enrich the returned entry
  with a live attack menu via `bridge.GetAttackMenuOptions(unitId, snapshot)` — an **existing** bridge
  API, returned as `entry with { AttackMenu = liveMenu }`. This reads live engage options without editing
  the Tick hotpath.
- **`MapPictureBridge` layout seed.** Placeholder symbol placement is seeded so map layout is
  deterministic for a given `(picture, seed)` — feed the host's global seed, not wall-clock.

### `C2PresentationController` (selection state)

`Presentation/C2PresentationController` is presentation-only selection state (req 20 §Selection). It holds
the ordered friendly multi-select set (`Selection` / anchor `SelectedUnitId`), the selected hostile
contact (`SelectedContactId` + projected `SelectedContactSummary`), and the read-only graph-surfacing
outputs (`LastGraphHighlightIds`, `LastGraphLinkChainDisplay`). Mutating helpers:

- `SelectFriendlyUnit(unitId)` — anchor a friendly unit, clear any contact selection, and clear graph
  highlights if the unit changed.
- `SelectHostileContact(contactId, contacts)` — select a contact (projected via `ContactSummaryProjection`)
  and clear friendly selection **and** graph highlights (so a bound graph panel doesn't show stale
  highlights for a unit that's no longer selected).
- `ApplyDefaultSelection(oob)` — pick a default friendly unit (`C2SelectionResolver.ResolveDefaultFriendlyUnit`)
  only when nothing is selected yet.
- `ResolveUnitDetail(snapshot, registry, bridge)` — routes to `UnitDetailBridge.BuildPrimary` when no unit
  is anchored, else `BuildSelected`.
- `ApplyGraphSurfacing(catalog)` — read-only dependency-graph highlights for the selected unit from
  `ICatalogReader.GetSortedDependencyEdges()` (catalog projection only — no `DelegationBridge`).

### `IC2PresentationFeed` (host contract)

`IC2PresentationFeed` is the read-only surface a Unity host publishes to its panels: `SelectedUnitId` /
`SelectedContactId`, the `Last*` view models (`LastOobTree`, `LastMapSymbols`, `LastSensorC2`,
`LastTopBar`, `LastUnitDetail`, graph highlights), and the two selection commands (`SelectUnit`,
`SelectContact`). `DelegationBridgeHost` is the implementer; keeping the contract as an interface lets
GitNexus trace host → adapter edges without a hard MonoBehaviour dependency.

---

## How a host wires it up (per tick)

`DelegationBridgeHost.RunTick(snapshot, sink)` is the canonical consumer. It runs the sim tick, then
rebuilds every read model from the **post-tick** order log + snapshot:

```csharp
var result = Bridge.Tick(snapshot, sink);                 // sim authority advances first
LastMessageLog = MessageLogBridge.ProjectFrom(Bridge.Orchestrator.DecisionLog);
LastOobTree    = OobTreeBridge.Build(snapshot, Bridge.Registry);
Presentation.ApplyDefaultSelection(LastOobTree);          // default-select once, if nothing chosen
LastSensorC2   = SensorC2Bridge.Build(snapshot, Bridge.Orchestrator.DecisionLog);
LastUnitDetail = Presentation.ResolveUnitDetail(snapshot, Bridge.Registry, Bridge);
LastMapSymbols = MapPictureBridge.Build(
    snapshot, Bridge.Registry, Bridge.Orchestrator.DecisionLog, globalSeed);
LastTopBar     = C2TopBarProjection.Project(
    snapshot.SimTime, Bridge.Phase, timeCompressionLabel, simulationModeLabel,
    Bridge.Orchestrator.DecisionLog);
```

The mission list is refreshed from the scenario policy timeline
(`MissionListBridge.ProjectFrom(Bridge.Orchestrator.ScenarioPolicy?.MissionTimeline)`), and selection
commands (`SelectUnit` / `SelectContact`) re-project the affected panels immediately. Order matters only
in that the sim `Tick` runs first — the bridges are pure reads of its result. (`C2TopBarProjection` is
called directly here because there is no dedicated top-bar bridge; it takes primitive labels, not a
snapshot handle.)

---

## Extending without breaking replay

1. **Adding a new panel?** Add the read model to the `Projection/` layer *first* (with its own headless
   projection tests), then add a thin `*Bridge` façade here that just calls it. Do not put
   formatting/filtering logic in the bridge.
2. **Need live engage/order data (not just log state)?** Follow `UnitDetailBridge`: take a
   `DelegationBridge` overload and call an **existing** public API. Never add a code path to
   `DelegationBridge.Tick`.
3. **Panel-bind indirection?** If a host needs a swappable binder (for tests or an alternate renderer),
   model it on `ISensorC2PanelBridge` + a `Default` implementation, not a hard static call.
4. **Empty/absent input** must project to an empty view model (see `MissionListBridge`), while `null`
   *required* args (`snapshot` / `registry` / `log`) throw `ArgumentNullException`.
5. **Before landing:** run the suites below plus the full solution suite (`dotnet test`), confirm
   `ReplayGolden 6/6` and the Baltic v2 hash `17144800277401907079` are unchanged, and confirm ZERO
   `DelegationBridge` Tick hotpath edits. If the task touches the adapter/presentation boundary, follow
   the `unity-csharp-architect` [`checklists/pr-finish.md`](../../production/agentic/skills/unity-csharp-architect/checklists/pr-finish.md).

---

## Tests that pin this doc

All green as of writing (UCA dogfood waves DRG-123 · DRG-140/141 · DRG-144/145/146; selection from
S37-04 / S39-03). These are NUnit fixtures in the UnityAdapter test assembly:

| Test file | Cases | Covers |
|-----------|-------|--------|
| [`MapPictureBridgeTests.cs`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/MapPictureBridgeTests.cs) | 6 | OOB + contact picture → deterministic map symbols for a given seed; null-arg guards. |
| [`OobTreeBridgeTests.cs`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/OobTreeBridgeTests.cs) | 5 | Registry members + alive-state → OOB rows; null-arg guards. |
| [`MessageLogBridgeTests.cs`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/MessageLogBridgeTests.cs) | 7 | Full AAR projection and the combat-strip subset filter; null-arg guard. |
| [`SensorC2BridgeTests.cs`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/SensorC2BridgeTests.cs) | 7 | Snapshot indicators + log → `SensorC2Snapshot`, the `BindPanel` seam, and the swappable `PanelBridge`. |
| [`UnitDetailBridgeTests.cs`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/UnitDetailBridgeTests.cs) | 7 | Primary vs. selected unit detail, attack-menu enrichment via the `DelegationBridge` overloads, null-arg guards. |
| [`MissionListBridgeTests.cs`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/MissionListBridgeTests.cs) | 2 | Scenario timeline → mission list rows; `null` timeline → empty read-only list. |
| [`C2PresentationControllerTests.cs`](../../src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/C2PresentationControllerTests.cs) | 6 | Friendly/contact select coordination, default selection, and graph-surfacing clear-on-reselect. |

Run just this subsystem:

```bash
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "FullyQualifiedName~Bridge.MapPictureBridge|FullyQualifiedName~Bridge.OobTreeBridge|FullyQualifiedName~Bridge.MessageLogBridge|FullyQualifiedName~Bridge.SensorC2Bridge|FullyQualifiedName~Bridge.UnitDetailBridge|FullyQualifiedName~Bridge.MissionListBridge|FullyQualifiedName~Presentation.C2PresentationController"
```

---

*Verified against source at the paths above. If you change a bridge signature or a projection contract,
update this doc and [c2-projection-layer.md](c2-projection-layer.md) together.*
