# Presentation bridge facades — the read-side host adapter

> **Scope.** How Unity / headless C2 panel hosts turn one sim tick's read state into
> panel-ready view models **without** touching sim authority: the thin `*Bridge` facades in
> `ProjectAegis.Delegation.UnityAdapter/Bridge/` (OOB tree, message log, map picture, mission
> list, sensor C2, unit detail) and the `C2PresentationController` selection state that drives
> them. This is the **read side** of the adapter boundary — the *write side* (snapshot →
> `ObservedState` → orchestrator → `IOrderSink`) is
> [`delegation-bridge-adapter-boundary.md`](delegation-bridge-adapter-boundary.md), and the pure
> order-log → view-model **projections** these facades call are
> [`c2-projection-layer.md`](c2-projection-layer.md). This page covers only the *host-facing seam*
> between the two.
>
> These bridges are the "product dogfood" of the projection layer landed across the UCA waves
> (UCA-M5 `MapPictureBridge` / DRG-123, UCA-A4 `OobTreeBridge` + `MessageLogBridge` /
> DRG-140/141, UCA-P1 `SensorC2Bridge` + `UnitDetailBridge` + `MissionListBridge` /
> DRG-144/145/146): each panel host consumes projections through one named adapter so the whole
> read path is uniform and traceable.
>
> Boundary rationale: [ADR-010 (headless-first, command-driven UI) §2–3](../architecture/adr-010-headless-first-command-driven-ui.md),
> [ADR-007 (C2 map presentation)](../architecture/adr-007-c2-map-presentation.md),
> [ADR-001 (sim assembly boundary)](../architecture/adr-001-sim-assembly-boundary.md). The bridge
> project has **no `UnityEngine` reference**, so every facade here runs and is tested under plain
> `dotnet test` (the same headless-dogfood pattern as the write side).

---

## Why a facade at all

The projection layer already produces immutable view models from the order log
(`c2-projection-layer.md`). A panel host *could* call `OobTreeProjection.Project(...)` directly.
It goes through a named `*Bridge` instead for three reasons:

1. **One read seam per panel.** A host gets the exact inputs it needs (a snapshot, the registry,
   the decision log) and one call, instead of hand-assembling several projection calls in host
   code. `MapPictureBridge.Build`, for example, composes three projections
   (`OobTreeProjection` → `ContactPictureProjection` → `MapPictureProjection`) behind a single
   call.
2. **A traceable host → adapter → projection edge.** The facades give GitNexus (and human
   readers) a stable place to see which panel consumes which projection. The sensor-C2 panel path
   is deliberately routed through the `ISensorC2PanelBridge` seam *instead of* calling
   `SensorC2PanelBinder` directly, precisely so that edge is explicit (Spirit1 G1 traceability).
3. **A read-only firewall.** Everything a host can reach through a bridge is a pure read. The
   facades never expose a mutation path into the sim, the order log, or the `DelegationBridge`
   hotpath — which is what keeps the presentation layer from perturbing replay determinism
   (ADR-010: UI is a *client*, not sim authority).

---

## Where it lives

All in `src/ProjectAegis.Delegation.UnityAdapter/Bridge/` (plus the controller under
`Presentation/`):

| Type | Kind | Panel / role | Wave |
|------|------|--------------|------|
| `OobTreeBridge` | `static` | Order-of-battle tree (friendly units + alive state). | UCA-A4 / DRG-140 |
| `MessageLogBridge` | `static` | AAR message log + compact combat strip. | UCA-A4 / DRG-141 |
| `MapPictureBridge` | `static` | Tactical map symbols (own units + contacts). | UCA-M5 / DRG-123 |
| `MissionListBridge` | `static` | Mission timeline event list. | UCA-P1 / DRG-146 |
| `SensorC2Bridge` | `static` | Sensor C2 HUD snapshot + panel bind. | UCA-P1 / DRG-144 |
| `UnitDetailBridge` | `static` | Selected/primary unit detail + attack menu. | UCA-P1 / DRG-145 |
| `ISensorC2PanelBridge` / `SensorC2PanelBridge` | `interface` / `sealed class` | The sensor-C2 panel-bind adapter seam (default delegates to `SensorC2PanelBinder`). | UCA-P1 |
| `C2PresentationController` | `sealed class` | Presentation-only selection state (doc 20); routes unit-detail resolution through `UnitDetailBridge`. | S37+/req-20 |

The view-model records they return (`OobTreeEntry`, `MessageLogLine`, `MapSymbolEntry`,
`MissionListEntry`, `SensorC2Snapshot`, `SensorC2PanelState`, `UnitDetailEntry`,
`ContactPictureEntry`, …) all live in `ProjectAegis.Delegation/Projection/` and are documented in
[`c2-projection-layer.md`](c2-projection-layer.md); this page treats them as opaque immutable
rows and focuses on the adapter plumbing.

> **netstandard2.1 note.** These facades ship as Unity plugins, so they use explicit
> `if (x is null) throw new ArgumentNullException(...)` guards rather than
> `ArgumentNullException.ThrowIfNull` (not available on the plugin target). A `null` required
> input always throws; see each bridge's per-argument `<exception>` doc.

---

## The facade catalog

Each facade is a pure map from *read inputs* to an *immutable view model*. None of them holds
state, opens a session, or writes anything.

### `OobTreeBridge.Build(snapshot, registry)`

```csharp
OobTreeProjection.Project(registry.CollectMemberIds(), snapshot.IsMemberAlive)
```

Order-of-battle rows for the OOB panel: the registry supplies the member ids, the snapshot
supplies alive state (`IsMemberAlive`). An empty registry yields an empty `IReadOnlyList<OobTreeEntry>`;
a dead member comes back with `IsAlive == false`. Consumes **only** the registry and the
snapshot's alive query — no live ECS or session write handle.

### `MessageLogBridge`

Two reads over the `DecisionLog`:

| Method | Returns |
|--------|---------|
| `ProjectFrom(log)` | The full AAR message log — every projected order-log category (`MessageLogProjection.Project(log)`). |
| `ProjectCombatMessages(log)` | The bottom-HUD combat strip: `ProjectFrom(log)` filtered to categories `KILL_CONFIRMED`, `INTERCEPT_SUCCESS`, `HIT`, `MISS`, `MAGAZINE`. |

The category subset is the single source of truth for "what's on the combat strip"; add a new
combat category by extending that `Where` filter (and confirming the projection emits it).

### `MapPictureBridge.Build(snapshot, registry, log, layoutSeed)`

Composes three projections into the map-symbol list:

```csharp
var oob      = OobTreeProjection.Project(registry.CollectMemberIds(), snapshot.IsMemberAlive);
var contacts = ContactPictureProjection.Project(log);
return MapPictureProjection.Project(oob, contacts, layoutSeed);
```

Own-unit symbols come from the OOB projection, hostile/unknown symbols from the contact-picture
projection, and `layoutSeed` drives **deterministic** placeholder placement (same seed → same
layout, so map snapshots are reproducible). No RNG beyond that seed; no wall-clock.

### `MissionListBridge.ProjectFrom(timeline)`

```csharp
MissionListProjection.Project(timeline)   // timeline may be null
```

Mission timeline events as list rows. A `null` `ScenarioMissionTimeline` is a **valid empty
picture** (no events) — the facade does not throw on `null` here, matching
`MissionListProjection.Project`.

### `SensorC2Bridge`

Two steps, split so a host can project once and bind many times:

| Method | Returns |
|--------|---------|
| `Build(snapshot, log)` | `SensorC2Snapshot` — contacts + EMCON / fire-control / active-engagement indicators. Wraps the snapshot's indicator getters (`ObserverRadarEmconActive`, `HasFireControlTrackOnPrimaryContact`, `PrimaryHostileContactId`, `ActiveEngagementCount`) in a private `ISensorC2WorldIndicators` adapter and calls `SensorC2Projection.Build`. |
| `BindPanel(snapshot)` | `SensorC2PanelState` for UI Toolkit bind, via the `PanelBridge` seam (below). |

### `UnitDetailBridge`

The selected/primary unit detail panel. Each of `BuildPrimary` / `BuildSelected` has two overloads:

- **Projection overload** — `(snapshot, registry, log, policy, observerUnitId = "u1")` — calls
  `UnitDetailProjection.ProjectPrimary` / `ProjectSelected` with the registry member ids, the
  snapshot's alive query + `SimTime`, the decision log, and the scenario policy. Pure projection;
  returns `UnitDetailEntry?` (`null` when the unit isn't resolvable).
- **Bridge overload** — `(snapshot, bridge, observerUnitId = "u1")` — a convenience form that
  pulls the same inputs off a live `DelegationBridge` (`bridge.Registry`,
  `bridge.Orchestrator.DecisionLog`, `bridge.Orchestrator.ScenarioPolicy`) and then enriches the
  entry's attack menu:

  ```csharp
  var liveMenu = bridge.GetAttackMenuOptions(unitId, snapshot);
  return entry with { AttackMenu = liveMenu };
  ```

> **Hotpath guard.** The bridge overloads only call **existing** read APIs on `DelegationBridge`
> (`Registry`, `Orchestrator`, `GetAttackMenuOptions`). They do **not** add logic to
> `DelegationBridge.Tick` — the `DelegationBridge.cs` zero-touch invariant
> ([AGENTS.md → Hard Invariants](../../AGENTS.md#hard-invariants--never-break-these)) is
> preserved.

---

## The sensor-C2 panel-bridge seam

`SensorC2Bridge.BindPanel` does not call `SensorC2PanelBinder.Bind` directly. It routes through a
swappable seam:

```csharp
public static ISensorC2PanelBridge PanelBridge { get; set; } = SensorC2PanelBridge.Default;
// BindPanel(snapshot) => PanelBridge.BindPanel(snapshot);
```

- `ISensorC2PanelBridge` is the one-method adapter interface
  (`SensorC2PanelState BindPanel(SensorC2Snapshot)`).
- `SensorC2PanelBridge.Default` is the production implementation; it delegates straight to the
  headless `SensorC2PanelBinder.Bind`.
- The indirection exists so the host → adapter → projection edge is explicit for GitNexus tracing
  (Spirit1 G1) and so a test/host can substitute a different panel bind without touching the
  projection. It is **presentation-only** — no path through the seam writes sim state.

---

## `C2PresentationController` — selection state on top of the facades

`C2PresentationController` (`Presentation/`) is the one piece here that holds **presentation-only**
state: the current selection (doc 20 / req 20) and read-only dependency-graph highlights. It never
mutates the sim or the order log.

| Member | Behaviour |
|--------|-----------|
| `Selection` / `SelectedUnitId` | Ordered multi-select set of friendly unit ids; `SelectedUnitId` is the anchor. Mutate only via `SelectFriendlyUnit` / `SelectHostileContact` / `ApplyDefaultSelection` so hostile-contact and graph side effects stay coordinated. |
| `SelectHostileContact(id, contacts)` | Sets the selected contact (via `ContactSummaryProjection.Project`), clears the friendly selection, and clears graph surfacing so a bound graph panel never shows stale highlights for a no-longer-selected unit (qa-loop-08). |
| `ApplyDefaultSelection(oob)` | Picks a default friendly unit (`C2SelectionResolver.ResolveDefaultFriendlyUnit`) only when nothing is selected. |
| `ResolveUnitDetail(snapshot, registry, bridge)` | Routes to `UnitDetailBridge.BuildPrimary` when nothing is selected, else `UnitDetailBridge.BuildSelected(new TargetId(SelectedUnitId), …)`. This is the controller → facade → projection call chain. |
| `ApplyGraphSurfacing(catalog)` / `ClearGraphSurfacing` | Compute read-only platform-graph highlights + link-chain summary for the selected unit from `ICatalogReader.GetSortedDependencyEdges()` — catalog projections only, **no `DelegationBridge`** (ADR-010 headless-first). |

---

## Determinism & invariants

- **Read-only, no sim writes.** Every facade and the controller consume snapshots / registry /
  decision log / catalog and return immutable view models. None open a session, apply an order, or
  append to the order log — so none can move the replay hash (ADR-010: UI is a client, not sim
  authority). *(Presentation boundary cites ADR-010 §2–3 / ADR-007 / ADR-001 — not ADR-018.)*
- **Pure, seed-only non-determinism.** The only tunable is `MapPictureBridge`'s `layoutSeed`
  (deterministic placeholder placement); there is no `Random.Shared` and no `DateTime.UtcNow` in
  the read path.
- **`DelegationBridge` stays zero-touch.** The `UnitDetailBridge` bridge overloads call existing
  read APIs only; no new logic enters `DelegationBridge.Tick`.
- **`null` required input throws; optional input is an empty picture.** Required inputs (snapshot,
  registry, log) throw `ArgumentNullException`; genuinely optional inputs are empty pictures —
  a `null` `MissionListBridge` timeline is an empty list, and an unresolvable unit is a `null`
  `UnitDetailEntry`.

---

## Extend it

| You want to… | Do this |
|--------------|---------|
| Add a new panel | Add a `static` `*Bridge` facade that takes exactly the read inputs it needs and returns an immutable projection result. Compose existing projections rather than reading sim state directly; add a headless `*BridgeTests` dogfood (below). |
| Add a combat-strip category | Extend the `MessageLogBridge.ProjectCombatMessages` category filter **and** confirm `MessageLogProjection` emits that category. |
| Swap the sensor-C2 panel bind | Assign a different `ISensorC2PanelBridge` to `SensorC2Bridge.PanelBridge` (e.g. in a test); keep it presentation-only. |
| Surface a new sim indicator on a panel | Add the signal to `ISimWorldSnapshot` (a default member — see the [write-side doc](delegation-bridge-adapter-boundary.md)), map it in the relevant projection, then read it through the facade. |
| Add selection-driven state | Extend `C2PresentationController` (presentation-only); keep every graph/highlight read off the catalog/projection layer, never the `DelegationBridge`. |

---

## See also

| Doc | For |
|-----|-----|
| [c2-projection-layer.md](c2-projection-layer.md) | The pure order-log → view-model projections these facades call. |
| [delegation-bridge-adapter-boundary.md](delegation-bridge-adapter-boundary.md) | The write side of the same adapter (snapshot → orchestrator → `IOrderSink`). |
| [player-command-issuance.md](player-command-issuance.md) | The command-*issuance* seam (the write-side companion to selection state). |
| [`src/…/UnityAdapter/README.md`](../../src/ProjectAegis.Delegation.UnityAdapter/README.md) | Project overview + integration quick-start. |
| [determinism-and-replay.md](determinism-and-replay.md) | Why the read path must stay pure. |

## Tests

`src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/` and `…/Presentation/` (NUnit) — one
headless dogfood per facade, all under plain `dotnet test`:

| Test file | Pins |
|-----------|------|
| `OobTreeBridgeTests` | `Build` is projection-only: registered unit appears with alive state; dead member → `IsAlive == false`; empty registry → empty `IReadOnlyList<OobTreeEntry>`; `null` snapshot/registry throw. |
| `MessageLogBridgeTests` | Full-log projection + combat-strip category subset. |
| `MapPictureBridgeTests` | OOB + contacts compose into map symbols; deterministic `layoutSeed`. |
| `MissionListBridgeTests` | Timeline events → list rows; `null` timeline → empty list. |
| `SensorC2BridgeTests` / `SensorC2PanelBinderTests` | Snapshot indicators → `SensorC2Snapshot`; panel bind via the seam. |
| `UnitDetailBridgeTests` | Primary/selected projection + attack-menu enrich via existing bridge APIs. |
| `C2PresentationControllerTests` / `C2PresentationControllerSelectionSetTests` | Selection routing to `UnitDetailBridge`; graph-surfacing clear on selection change (qa-loop-08). |
