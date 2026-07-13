# ProjectAegis.Delegation.UnityAdapter

The **adapter seam** between the Unity / DOTS rendering layer and the engine-agnostic
`ProjectAegis.Delegation` core. It has **no `UnityEngine` reference**, so every class here is
exercised by plain `dotnet test` (the headless C2-proxy `PlayModeSmokeHarnessTests` and the
Baltic replay-golden suite both live in this project's test assembly).

The adapter does three things:

| Namespace | Role |
|-----------|------|
| `Bridge/` | Facade + integration contracts. `DelegationBridge` drives a tick; `ISimWorldSnapshot` / `IOrderSink` are the two seams the sim must implement; the `*Bridge` classes project sim + order-log state into read-only **C2 view models**. |
| `Presentation/` | `C2PresentationController` — presentation-only selection state (which unit/contact is selected, graph-surfacing highlights). Never mutates the sim or order log. |
| `Baltic/` | Headless scenario runners: `BalticReplayHarness` (single seed/scenario, used by the replay-golden gate) and `BalticBatchRunner` (multi-seed/scenario CSV export). |

> **Invariant — `DelegationBridge.cs` is zero-touch through Release v1.** Do not add hot-path
> logic to it. See [`AGENTS.md` → Hard Invariants](../../AGENTS.md#hard-invariants--never-break-these).

---

## Integration contract

The sim/ECS layer supplies one object per tick that implements **`ISimWorldSnapshot`**, and one
that implements **`IOrderSink`** to receive the resulting orders (a single object can implement
both — see the `HeadlessSnapshot` in `BalticReplayHarness`).

**`ISimWorldSnapshot`** — what the sim must expose:

| Member | Meaning |
|--------|---------|
| `SimTime` | Current sim time (seconds). The bridge derives `simTick = max(0, (long)SimTime)`. |
| `ContactCount` / `ActiveEngagementCount` | Picture size, for HUD + top-bar. |
| `IsMemberAlive(TargetId)` | Alive state for a registered member (return `false` if unknown). |
| `PrimaryHostileContactId` / `HasFireControlTrackOnPrimaryContact` | Engage/sensor MVP target + track quality (`null` when `ContactCount == 0`). |
| `ObserverRadarEmconActive` | Whether the observer's radar EMCON permits active illumination. |
| `PrimaryHostileDestroyed` *(default `false`)* | Lets patrol policies pre-filter `Engage` proposals. |
| `PrimaryBlueForceContactId` / `PrimaryBlueForceContactDestroyed` *(default `null`/`false`)* | Red-side victim selection for Baltic v3. |

**`IOrderSink.ApplyOrder(EntityKey entity, in Order order)`** — apply a delegation order to your
movement / weapons / EW systems. Non-engage orders are routed through `OrderDispatcher`; engage
orders are resolved inside the MVP engage `Session` when enabled.

### Data flow

```text
ECS / sim systems
    │  implement ISimWorldSnapshot (contacts, engagements, member alive)
    ▼
DelegationBridge.Tick(snapshot, orderSink)
    │  ObservedStateBuilder → DelegationOrchestrator.Tick  [→ SimulationSession engage]
    ▼
IOrderSink.ApplyOrder(entityKey, order)  →  your movement / weapons / EW systems
```

The tick is deterministic given the same `(scenario, seed)` — see
[`determinism-and-replay.md`](../../docs/engineering/determinism-and-replay.md).

---

## Quick start (C# / tests)

```csharp
var bridge = new DelegationBridge(globalSeed: 42);
var friendly = bridge.Registry.RegisterUnit(new EntityKey(1), "friendly-1");
var opposing = bridge.Registry.RegisterUnit(new EntityKey(2), "opposing-1");

bridge.ConfigureSimulationMode(
    new SimulationModeProfile(SimulationModeKind.Mixed, PlayerControlsFriendlySide: true),
    friendly: [friendly.Target],
    opposing: [opposing.Target],
    PersonalityCatalog.All[0].Traits);

bridge.BeginExecution();
bridge.Tick(mySnapshot, myOrderSink);   // returns DelegationTickResult
```

Enable the MVP engage pipeline (headless engagement resolution after delegation) either at
construction (`new DelegationBridge(seed, mvpEngagement: true, scenarioPolicyId: "baltic-patrol")`)
or after construction via `bridge.EnableMvpEngagement(...)`.

---

## Player interaction API (human control)

These are the human-in-the-loop entry points used by the C2 UI (all return `false` / no-op when a
replay viewer is attached, so replay stays read-only):

| Method | Use |
|--------|-----|
| `TryEnqueueHumanOrder(entity, kind, simTime, risk?)` | Queue a manual order on a human-controlled unit. Applies comms order-delay and logs a `PlayerOrderRecord`. |
| `GetAttackMenuOptions(unitId, snapshot)` | Attack-menu entries (with DLZ preview) for UI binding. |
| `TryEnqueueAttackOption(entity, optionId, snapshot, out failureReason)` | Resolve an attack-menu selection into a player engage order (req 14 / doc 20). |
| `TryTakeDirectControl(entity, simTime)` / `TryReleaseDirectControl(entity, simTime)` | Toggle direct (manual) control of a unit. |
| `FinalizeScenario(missionSucceeded, objectivesMetRatio)` | End-of-scenario trust-signal rollup. |

---

## C2 presentation feed layer

The `*Bridge` classes are **thin, read-only facades**: they collect registry members + snapshot +
`DecisionLog` and delegate to the `*Projection` classes in the core
`ProjectAegis.Delegation.Projection` namespace, returning immutable view-model records. They never
mutate the sim or the order log, so a Unity panel can call them each frame safely.

| Bridge | Produces | Feeds |
|--------|----------|-------|
| `OobTreeBridge.Build(snapshot, registry)` | `IReadOnlyList<OobTreeEntry>` | Order-of-battle tree (members + alive state). |
| `MapPictureBridge.Build(snapshot, registry, log, layoutSeed)` | `IReadOnlyList<MapSymbolEntry>` | Map symbols (own units + projected contacts). |
| `MessageLogBridge.ProjectFrom(log)` / `.ProjectCombatMessages(log)` | `IReadOnlyList<MessageLogLine>` | Full AAR message log / compact combat strip. |
| `MissionListBridge.ProjectFrom(timeline)` | `IReadOnlyList<MissionListEntry>` | Mission list from the scenario mission timeline. |
| `SensorC2Bridge.Build(snapshot, log)` | `SensorC2Snapshot` | Sensor C2 HUD view model. |
| `UnitDetailBridge.BuildPrimary/BuildSelected(...)` | `UnitDetailEntry?` | Unit detail panel (default or selected unit). |

### Selection state — `C2PresentationController`

Holds presentation-only selection (`SelectedUnitId`, `SelectedContactId`,
`SelectedContactSummary`) plus read-only dependency-graph surfacing for the selected unit
(`LastGraphHighlightIds`, `LastGraphLinkChainDisplay`, computed from `ICatalogReader` projections
per ADR-010). Selecting a different unit/contact clears stale graph highlights so a bound panel
never shows highlights for a unit that is no longer selected.

```csharp
var c2 = new C2PresentationController();
c2.ApplyDefaultSelection(oobTree);              // pick a default friendly unit if none selected
c2.SelectFriendlyUnit("friendly-1");
c2.ApplyGraphSurfacing(catalogReader);          // read-only sensor/weapon/link chain highlights
var detail = c2.ResolveUnitDetail(snapshot, bridge.Registry, bridge);
```

### Host seam — `IC2PresentationFeed`

`IC2PresentationFeed` is the read-only surface a Unity panel host binds to (implemented by
`DelegationBridgeHost` in `unity/ProjectAegis/`, so GitNexus can trace host → adapter edges). It
exposes the last-projected OOB tree, map symbols, sensor C2, top bar, and unit detail, plus
`SelectUnit` / `SelectContact` and the graph-surfacing fields.

---

## Baltic headless harness

Two static runners drive the delegation + engage pipeline end-to-end with no Unity dependency.

- **`BalticReplayHarness.Run(seed, scenarioPolicyId, ticks, ...)`** → `Result` (fingerprint, both
  world hashes, checkpoints, message log, sensor C2, scoring CSV row, decision log, fire order).
  This is the runner behind the replay-golden CI gate and the console demo. Golden workflow,
  hashing model, and regeneration steps are documented in
  [`determinism-and-replay.md`](../../docs/engineering/determinism-and-replay.md).
- **`BalticBatchRunner.Run(request)` / `.ExportCsv(rows)` / `.DiscoverScenarioIds()`** →
  multi-seed / multi-scenario agent-vs-agent CSV export (GDD `agentic-infrastructure.md`). Driven
  by the demo `--batch` verb; see [`tools/batch-replay/README.md`](../../tools/batch-replay/README.md).

```bash
# single replay (golden regeneration)
dotnet run --project src/ProjectAegis.Delegation.Demo -- --seed 42 --scenario baltic-patrol --ticks 6

# batch CSV
dotnet run --project src/ProjectAegis.Delegation.Demo -- --batch --all-scenarios --seeds 42 --ticks 4 --csv-out all.csv
```

> **Baltic v3 isolation.** `baltic-v3-*` scenarios (OOB `u1`, `hostile-1`, `ucav-blue`, `ucav-red`)
> use independent policies and goldens; never touch v2 goldens when editing v3.

---

## Constraints

- **No `UnityEngine` reference** — keep this project testable with `dotnet test`. Unity-only code
  belongs in `unity/ProjectAegis/`.
- **`DelegationBridge` hot path is frozen** (zero-touch invariant); add new behavior in the core or
  in new adapter types, not in `DelegationBridge.Tick`.
- **Presentation is read-only** — `*Bridge` and `C2PresentationController` must never mutate the sim
  or order log; doing so would desync replay.
- **Determinism** — anything that reaches a hash or fingerprint must follow the rules in
  [`determinism-and-replay.md`](../../docs/engineering/determinism-and-replay.md).

---

## Unity Editor wiring

See [`unity/ProjectAegis/README.md`](../../unity/ProjectAegis/README.md) — build the adapter DLLs,
copy to `Plugins`, and use the optional `DelegationBridgeHost` MonoBehaviour to drive `Tick` and
expose `IC2PresentationFeed` to UI Toolkit panels.

## Related docs

| Topic | Doc |
|-------|-----|
| Delegation core (orchestrator, decision pipeline, traits) | [`ProjectAegis.Delegation/README.md`](../ProjectAegis.Delegation/README.md) |
| Deterministic sim core + world hashes | [`ProjectAegis.Sim/README.md`](../ProjectAegis.Sim/README.md) |
| Determinism & replay golden workflow | [`docs/engineering/determinism-and-replay.md`](../../docs/engineering/determinism-and-replay.md) |
| Console demo / golden regeneration | [`ProjectAegis.Delegation.Demo/README.md`](../ProjectAegis.Delegation.Demo/README.md) |
| Batch replay CSV | [`tools/batch-replay/README.md`](../../tools/batch-replay/README.md) |
| Hard invariants + verification block | [`AGENTS.md`](../../AGENTS.md#hard-invariants--never-break-these) |
