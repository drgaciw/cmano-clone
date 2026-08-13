# Map view controls — basemap layer stack, LOD clustering & scale/measure tools

> **Scope.** The pure, headless **map view-control** helpers in
> [`ProjectAegis.Delegation/Projection/`](../../src/ProjectAegis.Delegation/Projection) that sit
> *around* the tactical picture: the CMD-28.2 **basemap layer stack** (which raster layers are drawn),
> the REQ-20 Phase N **APP-6 symbol LOD clusterer** (how a multi-thousand-symbol picture is
> decluttered by camera altitude), and the CMD-20 / CMD-28.4 / CMD-28.5 **scale-bar / measure /
> unit-cycle** math. These are camera- and layer-facing controls — distinct from *what* the map draws
> ([`c2-projection-layer.md`](c2-projection-layer.md) read-model catalog, `MapPictureProjection`) and
> from the *product globe camera* ([`globe-map-view-projection.md`](globe-map-view-projection.md)).
>
> Every type here is **pure presentation** (ADR-010 §2–3 / ADR-007): UI-local, no sim mutation, no
> `DecisionLog` entry, and — for the layer stack store — no file I/O. Nothing in this subsystem touches
> the Baltic v2 replay hash. UI is a **client**, never sim authority.

---

## TL;DR

```csharp
// 1. Basemap layers — default stack, toggle, persist, present as a checklist.
var stack   = MapLayerStackState.WithDefaults();          // all on except Day/Night
stack       = stack.Toggle(MapLayerId.DayNight);          // returns a NEW instance
var store   = new MapLayerStackStore();
store.Capture(stack);                                     // UI-local bag (string->bool)
stack       = store.Restore(MapLayerStackState.WithDefaults());
var layerUi = MapLayerStackApplyState.Apply(stack);       // "[x] Satellite  (none)" + "LAYERS: 8/8"

// 2. LOD clustering — declutter by camera altitude.
var band    = MapLodPolicy.ResolveBand(cameraAltitudeMeters: 2_000_000); // -> Overview
var lod     = MapLodApplyState.Apply(symbols, band);      // clusters + reduction stats

// 3. Scale / measure / cycle helpers.
var scale   = MapScaleProjection.Project(cameraAltitudeMeters: 867_550, metersPerScreenUnit: 1852);
var range   = MapMeasureProjection.Measure(0, 0, 0, 1, metersPerUnit: 1852);
var next    = UnitCycleProjection.Next(unitIds, currentId);
```

All three pillars are static, deterministic, and Unity-free; the Unity hosts
[`MapPlaceholderPanelHost`](../../unity/ProjectAegis/Assets/Scripts/Runtime/MapPlaceholderPanelHost.cs)
and [`C2MenuPanelHost`](../../unity/ProjectAegis/Assets/Scripts/Runtime/C2MenuPanelHost.cs) only bind
the resulting strings/records onto widgets.

---

## 1. Basemap layer stack (CMD-28.2)

Which raster layers the operator has enabled. Eight stable layers, an immutable ordered state, a
default factory, a UI-local persistence bag, and a checklist presentation.

### Types

| Type | Kind | Role |
|------|------|------|
| [`MapLayerId`](../../src/ProjectAegis.Delegation/Projection/MapLayerId.cs) | `enum` | Eight stable ids in draw order: `Satellite`, `Relief`, `Borders`, `Terrain`, `Roads`, `LandCover`, `Placenames`, `DayNight`. |
| [`MapLayerEntry`](../../src/ProjectAegis.Delegation/Projection/MapLayerEntry.cs) | `sealed record` | One row: `Id`, `Label`, `IsVisible`, `ShortcutHint` (a discovery string like `"none"`, **not** input routing). |
| [`MapLayerStackState`](../../src/ProjectAegis.Delegation/Projection/MapLayerStackState.cs) | `sealed class` | Immutable ordered stack. Mutators return a new instance. |
| [`MapLayerStackProjection`](../../src/ProjectAegis.Delegation/Projection/MapLayerStackProjection.cs) | `static` | `DefaultStack()` factory. |
| [`MapLayerStackStore`](../../src/ProjectAegis.Delegation/Projection/MapLayerStackStore.cs) | `sealed class` | In-memory `string→bool` visibility bag for UI-local persistence. |
| [`MapLayerStackApplyState`](../../src/ProjectAegis.Delegation/Projection/MapLayerStackApplyState.cs) | `static` | `Apply()` → `MapLayerStackPresentation` (checklist lines + summary). |

### Behaviour

- **Default stack.** `WithDefaults()` (delegates to `MapLayerStackProjection.DefaultStack()`) returns
  all eight layers **visible except `DayNight`** (off by default). `Layers` is listed in draw order.
- **Immutable mutation.** `Toggle(id)` / `SetVisible(id, bool)` copy the array and flip one entry,
  returning a **new** `MapLayerStackState`. A no-op (unknown id, or `SetVisible` to the current value)
  returns `this` unchanged — cheap idempotency. `VisibleCount` and `TryGet(id, out entry)` are the read
  accessors.
- **Persistence bag.** `MapLayerStackStore` is a deliberately tiny `string→bool` map keyed by
  `MapLayerId.ToString()` (ordinal). `Capture(state)` snapshots visibility; `Restore(baseState)`
  re-applies it onto a base (typically `WithDefaults()`), **ignoring unknown keys** and returning the
  base unchanged when the bag is empty. `ApplyVisibilitySnapshot` / `ToVisibilitySnapshot` on the state
  are the round-trip primitives. This is UI-local only — **not replay, not `DecisionLog`, not file I/O**;
  hosts own any actual persistence.
- **Presentation.** `MapLayerStackApplyState.Apply(state)` emits one `MapLayerChecklistLine` per layer
  with a pre-formatted `DisplayLine` — `"[x] Satellite  (none)"` when visible, `"[ ] …"` when hidden —
  plus a `SummaryLabel` of `"LAYERS: <visible>/<total>"` (e.g. `LAYERS: 8/8`). Null/empty state → the
  shared `MapLayerStackPresentation.Empty` (`LAYERS: 0/0`). `ProjectAndApplyDefaults()` is the
  one-call headless smoke path.

### Host wiring

`MapPlaceholderPanelHost` holds a live `_layerStack` plus a `MapLayerStackStore`, calls
`store.Restore(WithDefaults())` on init, exposes `LayerStack` / `SetLayerStack(state)`, and rebinds the
summary via `MapLayerStackApplyState.Apply`. `C2MenuPanelHost.ResolveLayerStack()` reads the map host's
`LayerStack` (falling back to `WithDefaults()`) and `ApplySummary` renders the same presentation into
the menu.

---

## 2. APP-6 symbol LOD clustering (REQ-20 Phase N)

Declutter a multi-thousand-symbol tactical picture by **camera altitude**, without touching the
underlying symbols. The north-star is *5 000 symbols at 60 fps*, so the clusterer is a pure grid bucket
with a wall-clock budget test.

### Types

| Type | Kind | Role |
|------|------|------|
| [`MapLodBand`](../../src/ProjectAegis.Delegation/Projection/MapLodBand.cs) | `enum` | `Overview` (coarsest) → `Theater` → `Tactical` → `Close` (1:1, no clustering). |
| [`MapLodPolicy`](../../src/ProjectAegis.Delegation/Projection/MapLodPolicy.cs) | `static` | Altitude→band thresholds + suggested grid divisions. |
| [`MapSymbolLodClusterer`](../../src/ProjectAegis.Delegation/Projection/MapSymbolLodClusterer.cs) | `static` | The grid-bucket clusterer. |
| [`MapSymbolClusterEntry`](../../src/ProjectAegis.Delegation/Projection/MapSymbolClusterEntry.cs) | `sealed record` | One cluster or passthrough symbol. |
| [`MapLodApplyState`](../../src/ProjectAegis.Delegation/Projection/MapLodApplyState.cs) | `static` | `Apply()` → `MapLodApplyResult` with reduction stats. |

### Altitude → band → grid

`MapLodPolicy.ResolveBand(altitudeMeters)` uses fixed thresholds (camera altitude in metres, higher →
coarser); `DefaultGridDivisions(band)` suggests the grid (callers may override):

| Band | Altitude window (m) | Default grid divisions |
|------|---------------------|:----------------------:|
| `Close`    | `< 50 000`                | `1` (grid ignored — 1:1) |
| `Tactical` | `50 000 – < 250 000`      | `48` |
| `Theater`  | `250 000 – < 1 000 000`   | `24` |
| `Overview` | `≥ 1 000 000`             | `16` |

Negative/zero altitude maps to `Close`. The `< / ≥` boundaries are exact: an altitude *exactly at*
`CloseMaxAltitudeMeters` is already `Tactical`, and so on up the ladder.

### Clustering rules (`MapSymbolLodClusterer.Cluster`)

- **`Close` is identity.** Returns one non-cluster `MapSymbolClusterEntry` per symbol (`Count == 1`,
  `IsCluster == false`), ordered by `SymbolId` (ordinal).
- **Coarser bands grid-bucket** each symbol into a `gridDivisions × gridDivisions` cell. The cell is
  computed from **lat/lon when both are present** (`lon+180/360`, `lat+90/180`), else from the symbol's
  normalized `x/y` in `[0,1]`. `gridDivisions < 1` throws.
- **Deterministic representative & order.** Within a cell the representative is the **lowest ordinal
  `SymbolId`**; the cluster id is `g:<gx>:<gy>` (multi-member) or `s:<symbolId>` (single). Output is
  sorted by `ClusterId` then `RepresentativeSymbolId` (ordinal) — same input → byte-identical output.
- **Cluster fields** are folded from members: averaged normalized position, averaged lat/lon (null when
  no member carries coordinates), **majority `Affiliation`** (ties broken by lowest ordinal), and an
  APP-6 glyph resolved from the representative via `CesiumBillboardProjection.ResolveGlyph` (SIDC
  preferred, affiliation fallback) — the *same* glyph path the rest of the map uses.

### Reduction stats & panel integration

`MapLodApplyState.Apply(symbols, band[, gridDivisions])` runs the clusterer and returns a
`MapLodApplyResult` with `InputCount`, `OutputCount`, `ReductionRatio` (`out/in`, `1.0` for empty
input), `Band`, and `GridDivisions`. The count is surfaced through the map panel additively:
`MapPanelApplyState.BindAndApply(…, lodOutputCount)` sets `MapPanelPresentation.LodOutputCount`, which
**defaults to the bound symbol count** (i.e. *no reduction reported*) when the caller passes no LOD —
so existing call sites are unchanged.

Covered by [`MapSymbolLodClustererTests`](../../src/ProjectAegis.Delegation.Tests/Projection/MapSymbolLodClustererTests.cs):
band thresholds, `Close` identity, cross-run determinism, `Overview` reduction (≤ `divisions²`
clusters, member counts sum back to the input), representative/majority/glyph rules, the additive
`LodOutputCount` default, and a **5 000-symbol `Overview` p95 < 50 ms** wall-clock budget.

---

## 3. Scale, measure & unit-cycle helpers

Small pure-math helpers hosts bind as labels; no state, no allocation beyond the returned record.
Covered by [`MapScaleAndCycleTests`](../../src/ProjectAegis.Delegation.Tests/Projection/MapScaleAndCycleTests.cs).

| Helper | Req | Behaviour |
|--------|-----|-----------|
| [`MapScaleProjection`](../../src/ProjectAegis.Delegation/Projection/MapScaleProjection.cs) | CMD-20 | `Project(cameraAltitudeMeters, metersPerScreenUnit)` → `MapScaleState` with a `SCALE … NM` bar (precision steps at `<10` / `<100` / `≥100` NM, `SCALE —` when non-positive) and a `CAM ALT … m`/`km` readout. `MetersPerNauticalMile = 1852`. |
| `MapMeasureProjection` | CMD-28.4 | `Measure(fromX, fromY, toX, toY, metersPerUnit)` → range (m + NM) and compass `BearingDegrees` (`atan2(dx, dy)` normalized to `0–360°`) with a `RNG … NM  BRG …°` label. |
| `UnitCycleProjection` | CMD-28.5 | `Next` / `Previous` wrap over an ordered id list; a null/empty list → `null`, a null current → the first/last element, an unknown current → the first/last element. |

(`MapMeasureProjection` and `UnitCycleProjection` live in the same file as `MapScaleProjection`.)

---

## Map symbol render rows (context)

The controls above feed the render-row binder
[`MapPanelBinder`](../../src/ProjectAegis.Delegation/Projection/MapPanelBinder.cs) →
`MapPanelState` / `MapSymbolDisplayRow`, which maps each `MapSymbolEntry` to a USS style class by
`Affiliation` (`map-symbol--friendly` / `--hostile` / `--neutral` / `--suspect` / `--pending` /
`--unknown`, `--friendly-dead` when destroyed), marks the selected unit/contact, and — under a degraded
or denied comms state — appends a **ghost** row or a `--frozen` style (see
[`comms-degradation-runtime.md`](comms-degradation-runtime.md)). Glyphs resolve through the APP-6 atlas
(`App6GlyphAtlas` / `App6Sidc`, atlas-optional). `MapPanelApplyState` then folds symbol/selected/ghost
counts (plus optional envelope-ring / datalink-edge / doctrine-overlay counts and the LOD output count)
into `MapPanelPresentation`. The picture *content* itself is documented under
[`c2-projection-layer.md`](c2-projection-layer.md).

---

## Invariants

| Invariant | Why |
|-----------|-----|
| **Pure presentation** | No sim mutation, no engine types. Hosts bind strings/records; UI is a client (ADR-010 §2–3, ADR-007), never sim authority. |
| **UI-local, not logged** | Layer visibility and camera/LOD state never enter the `DecisionLog` — an explicit ADR-010 exception for UI-local presentation state. The layer store is also **not file I/O**; hosts own persistence. |
| **Deterministic** | Clustering output is sorted by `ClusterId`/`RepresentativeSymbolId`; layer mutation copies deterministically. Same input → identical output (tested). |
| **Replay-safe** | Nothing here participates in the fingerprinted sim state, so it cannot move the Baltic v2 hash `17144800277401907079` (see [determinism-and-replay.md](determinism-and-replay.md)). |
| **Additive extension** | New layers, LOD bands, or presentation fields extend enums/records without altering existing values — e.g. `LodOutputCount` defaults to the symbol count so old call sites are untouched. |

---

## How to extend

- **Add a basemap layer:** append a `MapLayerId` value (keep existing ordinals stable), add its
  `MapLayerEntry` to `MapLayerStackProjection.DefaultStack()`, and the checklist/summary follow
  automatically. Store keys are the enum names, so an old snapshot simply lacks the new key (defaults
  apply).
- **Retune LOD:** adjust `MapLodPolicy` thresholds / `DefaultGridDivisions`, or pass an explicit
  `gridDivisions` to `MapLodApplyState.Apply`. Keep the 5k p95 budget green.
- **Add a measure/scale readout:** add a static helper next to `MapScaleProjection` returning a record
  with a pre-formatted label; do not push formatting into hosts.

---

## See also

| Doc | For |
|-----|-----|
| [c2-projection-layer.md](c2-projection-layer.md) | The C2 read-model layer these controls sit within (`Projection → Binder → State`, the map *picture* content, APP-6 symbology, how to add a panel). |
| [globe-map-view-projection.md](globe-map-view-projection.md) | The product-globe **camera** side (theater bookmarks, 2D/3D mode, Cesium billboards) — the other half of the map view. |
| [comms-degradation-runtime.md](comms-degradation-runtime.md) | The comms state that drives the map's ghost / frozen symbol styling. |
| [determinism-and-replay.md](determinism-and-replay.md) | The replay-hash invariant this presentation-only subsystem must never disturb. |
