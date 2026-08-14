# Tactical map overlays — selected-unit envelope rings, doctrine ROE & datalink mesh

> **Scope.** The pure, headless **content overlays** that the C2 map host draws *on top of* the
> tactical picture in [`ProjectAegis.Delegation/Projection/`](../../src/ProjectAegis.Delegation/Projection):
> the CMD-21 / CMD-34 **selected-unit envelope rings** (weapon + sensor reach, resolved from the
> catalog), the CMD-33 **doctrine ROE overlay** (per-unit effective ROE + inheritance source), and the
> CMD-32 **datalink unit-pair mesh** — all folded into count fields on `MapPanelPresentation` and bound
> by the Unity [`MapPlaceholderPanelHost`](../../unity/ProjectAegis/Assets/Scripts/Runtime/MapPlaceholderPanelHost.cs).
>
> **As of DRG-160 the same projected ring/edge geometry is also *drawn*** onto the map canvas: a pure
> [`MapCanvasOverlayGeometry`](../../src/ProjectAegis.Delegation/Projection/MapCanvasOverlayGeometry.cs)
> projects the rings/edges into normalized canvas shapes, and the pooled Unity
> [`MapCanvasOverlayRenderer`](../../unity/ProjectAegis/Assets/Scripts/Runtime/MapCanvasOverlayRenderer.cs)
> reconciles them onto UI Toolkit elements. The map HUD still shows the counts; the canvas now shows the
> shapes too (see [§6](#6-canvas-draw-layer-drg-160)).
>
> These are the map's **data content**, distinct from the *camera / layer controls*
> ([`map-view-controls.md`](map-view-controls.md) — basemap layer stack, LOD clustering, scale/measure),
> the *product-globe camera* ([`globe-map-view-projection.md`](globe-map-view-projection.md)), and the
> general read-model catalog ([`c2-projection-layer.md`](c2-projection-layer.md), `MapPictureProjection`).
>
> Every type here is **pure presentation** (ADR-010 §2–3 / ADR-007): a read of the catalog / order-log
> projections into value records, UI-local, no sim mutation, no `DecisionLog` entry, no file I/O.
> Nothing in this subsystem touches the Baltic v2 replay hash. The map is a **client**, never sim
> authority.

---

## TL;DR

```csharp
// 1. Envelope rings — resolve the selected unit's reach from the catalog, then project two rings.
var (sensorNm, weaponNm) = CatalogEnvelopeRangeResolver.ResolveSelectedUnitRanges(
    catalog, selectedUnitId, CatalogWeaponIds.MvpDefault);   // (40, 20) fallbacks when unresolved
var rings = TacticalOverlayProjection.ProjectSelectedUnitEnvelopes(
    selectedUnitId, sensorNm, weaponNm);                     // [] when no selection; else 2 rings

// 2. Doctrine ROE overlay — one row per alive friendly unit, optional map position.
var inheritance = DoctrineInheritanceProjection.ProjectAllUnits(unitIds, scenarioPolicy, isFriendly: true);
var doctrine    = DoctrineMapOverlayProjection.Project(inheritance, mapSymbols); // sorted by UnitId

// 3. Datalink mesh — adjacent-pair edges over the friendly OOB (see c2-projection-layer.md).
var edges = DatalinkUnitPairFeed.ProjectEdges(friendlyUnitIds, catalog.GetSortedLinks());

// 4. Fold the counts onto the map presentation (content is count-only for the HUD).
var view = MapPanelApplyState.Apply(panelState, rings, edges, doctrine);
// view.EnvelopeRingCount / DatalinkEdgeCount / DoctrineOverlayCount

// 5. Draw the geometry onto the canvas (DRG-160): index live symbol positions, then project shapes.
var positions  = MapCanvasOverlayGeometry.BuildUnitPositionIndex(panelState.Symbols);
var ringShapes = MapCanvasOverlayGeometry.ProjectRings(rings, positions);   // circles at unit centers
var edgeShapes = MapCanvasOverlayGeometry.ProjectEdges(edges, positions);   // line segments unit→unit
overlayRenderer.Sync(ringShapes, edgeShapes);                               // pooled UI Toolkit reconcile
```

Every headless helper is `static`, deterministic, and Unity-free — including the DRG-160
`MapCanvasOverlayGeometry`. The Unity host binds the resulting records / counts onto UI Toolkit widgets
and delegates all math (range→radius, position lookup) to the pure geometry; `MapCanvasOverlayRenderer`
is the only Unity-side type and does element pooling only, no math.

---

## Type map

| Type | Kind | Role |
|------|------|------|
| [`CatalogEnvelopeRangeResolver`](../../src/ProjectAegis.Delegation/Projection/CatalogEnvelopeRangeResolver.cs) | `static` | Resolves `(SensorNm, WeaponNm)` for the selected unit from catalog rows (meters → nm), with fixed fallbacks. |
| [`TacticalOverlayProjection`](../../src/ProjectAegis.Delegation/Projection/TacticalOverlayProjection.cs) | `static` | Turns resolved ranges into `EnvelopeRingEntry` rings (Sensor + Weapon) for the selected unit. |
| [`EnvelopeRingEntry`](../../src/ProjectAegis.Delegation/Projection/EnvelopeRingEntry.cs) | `sealed record` | `(UnitId, RingKind, Domain, RangeNm, IsSelectedUnit)` — one ring. |
| [`DoctrineMapOverlayProjection`](../../src/ProjectAegis.Delegation/Projection/DoctrineMapOverlayProjection.cs) | `static` | Maps `DoctrineInheritanceEntry` rows onto optional map positions (CMD-33). |
| [`DoctrineMapOverlayEntry`](../../src/ProjectAegis.Delegation/Projection/DoctrineMapOverlayEntry.cs) | `sealed record` | `(UnitId, RoeLabel, SourceLabel, NormalizedX?, NormalizedY?)` — one doctrine row. |
| [`DatalinkUnitPairFeed`](../../src/ProjectAegis.Delegation/Projection/DatalinkUnitPairFeed.cs) | `static` | Adjacent-pair datalink mesh + comms-aware edge status (CMD-32). Detailed in [`c2-projection-layer.md`](c2-projection-layer.md) / [`comms-degradation-runtime.md`](comms-degradation-runtime.md). |
| [`MapPanelApplyState`](../../src/ProjectAegis.Delegation/Projection/MapPanelApplyState.cs) | `static` | Folds overlay lists into count fields on `MapPanelPresentation` (CMD-21/32/33/34). |
| [`MapPanelPresentation`](../../src/ProjectAegis.Delegation/Projection/MapPanelApplyState.cs) | `sealed record` | Map HUD view model: theater/symbol/selection counts **plus** `EnvelopeRingCount`, `DatalinkEdgeCount`, `DoctrineOverlayCount`, `LodOutputCount`. |
| [`MapEnvelopePlatformResolver`](../../src/ProjectAegis.Delegation/Projection/MapEnvelopePlatformResolver.cs) | `static` | Maps a scenario **unit id** to its catalog **platform id** for envelope range lookup — direct hit, else longest `platform-` prefix match, else the unit id unchanged (DRG-160 / #495). |
| [`MapCanvasOverlayGeometry`](../../src/ProjectAegis.Delegation/Projection/MapCanvasOverlayGeometry.cs) | `static` | Pure geometry (DRG-160): `nm → normalized radius`, live-symbol position index, and projection of rings/edges into normalized canvas shapes. |
| [`MapCanvasRingShape`](../../src/ProjectAegis.Delegation/Projection/MapCanvasOverlayGeometry.cs) | `sealed record` | `(Key, CenterX, CenterY, RadiusNormalized, RingKind, StyleClass)` — one normalized ring circle. |
| [`MapCanvasEdgeShape`](../../src/ProjectAegis.Delegation/Projection/MapCanvasOverlayGeometry.cs) | `sealed record` | `(Key, FromX, FromY, ToX, ToY, Status, StyleClass)` — one normalized edge segment. |
| [`MapCanvasOverlayRenderer`](../../unity/ProjectAegis/Assets/Scripts/Runtime/MapCanvasOverlayRenderer.cs) | `sealed class` (Unity) | Pooled UI Toolkit renderer (DRG-160): reconciles ring/edge shape lists onto keyed `VisualElement`s behind the unit symbols. |
| [`MapPlaceholderPanelHost`](../../unity/ProjectAegis/Assets/Scripts/Runtime/MapPlaceholderPanelHost.cs) | `MonoBehaviour` | The Unity consumer — resolves inputs from the bridge, calls the projections, binds counts to labels, and drives the canvas draw layer. |

---

## 1. Envelope rings (CMD-21 / CMD-34)

Two concentric range rings for the **selected unit** — the outer **sensor** reach and the inner
**weapon** reach. The two steps are deliberately split: `CatalogEnvelopeRangeResolver` answers *how far*
in nautical miles, and `TacticalOverlayProjection` answers *what rings to draw*.

### 1.1 Resolving ranges — `CatalogEnvelopeRangeResolver`

All ranges are stored in the catalog in **meters** and converted to **nautical miles**
(`1 nm = 1852 m`, `MetersToNauticalMiles`). Three entry points:

| Method | Returns | Behaviour |
|--------|---------|-----------|
| `TryResolveWeaponRangeNm(catalog, weaponId, out nm)` | `bool` | `true` + `MaxRangeMeters → nm` from [`ICatalogReader.TryGetWeaponEnvelope`](../../src/ProjectAegis.Data/Catalog/ICatalogReader.cs). `false` when catalog is null, the weapon id is blank/unknown, or `MaxRangeMeters <= 0`. |
| `TryResolveSensorRangeNm(catalog, platformId, out nm)` | `bool` | `true` + the **max** over the platform's *approved* sensor bindings of `combatRadiusNm × clamp(basePd, 0.05, 1.0)` (kill-chain envelope parity). `false` when catalog is null, `platformId` blank, combat radius non-positive, or no approved binding exists. |
| `ResolveSelectedUnitRanges(catalog, unitId, weaponId = MvpDefault)` | `(SensorNm, WeaponNm)` | Convenience fold used by the host — see below. |

`ResolveSelectedUnitRanges` starts from the fallbacks and overrides each independently when the catalog
resolves:

- **Sensor** ← `TryResolveSensorRangeNm(catalog, unitId, …)` (only attempted when `unitId` is non-blank).
- **Weapon** ← `TryResolveWeaponRangeNm(catalog, weaponId, …)`.
- Fallbacks: `DefaultSensorRangeNm = 40.0` nm, `DefaultWeaponRangeNm = 20.0` nm.

> **Sensor path was added in DRG-159 (#495).** Before that, `unitId` was reserved and sensor range
> always used the 40 nm fallback. The weapon path and RNG-free purity are unchanged. Sensor resolution
> only reads **approved** bindings ([`CatalogReviewStates.Approved`](../../src/ProjectAegis.Data/Catalog/CatalogReviewStates.cs),
> case-insensitive) so unreviewed/quarantined fittings never widen a ring.

Worked example against the Baltic patrol fixture:

| Weapon id | `MaxRangeMeters` | Resolved weapon nm |
|-----------|------------------|--------------------|
| `mvp-default` | `100_000` | `100000 / 1852 ≈ 53.996` |
| `kill-chain-long-range` | `200_000` | `200000 / 1852 ≈ 107.99` |
| *(unknown / blank)* | — | `20.0` fallback |

### 1.2 Projecting rings — `TacticalOverlayProjection.ProjectSelectedUnitEnvelopes`

```csharp
IReadOnlyList<EnvelopeRingEntry> ProjectSelectedUnitEnvelopes(
    string? selectedUnitId, double sensorRangeNm, double weaponRangeNm, string domain = "Unknown")
```

- Returns **empty** when `selectedUnitId` is null/whitespace (nothing selected → no rings).
- Otherwise returns exactly **two** rings for that unit: `RingKind = "Sensor"` then `"Weapon"`, both with
  `IsSelectedUnit: true`.
- `Domain` is a free label (`"Air" | "Surface" | "Underwater" | "Land" | "Unknown"`); a blank domain
  normalises to `"Unknown"`. The host currently leaves it at the default.

The ring's `RangeNm` is drawn directly — the projection does **no** clamping or unit conversion; that all
happened in the resolver.

### 1.3 Mapping unit → platform — `MapEnvelopePlatformResolver` (DRG-160)

Catalog envelope rows are keyed by **platform id**, but a scenario can field multiple *instances* of one
platform under suffixed ids (e.g. `u1-alpha`, `u1-bravo` off platform `u1`). Before resolving ranges the
host now maps the selected **unit id** to a catalog **platform id** with
[`MapEnvelopePlatformResolver.Resolve(catalog, unitId)`](../../src/ProjectAegis.Delegation/Projection/MapEnvelopePlatformResolver.cs):

| Input | Result |
|-------|--------|
| `unitId` null/blank | `null` |
| `catalog` null | `unitId` unchanged (host still gets fallback rings) |
| `unitId` resolves a sensor range **or** combat radius directly | `unitId` unchanged |
| `unitId` is a suffixed instance (`platform-…`) | the **longest** matching `platformId` prefix over `GetSortedMobility()` then `GetSortedMounts()` |
| no prefix matches | `unitId` unchanged (falls through to fallbacks) |

It reads only the existing [`ICatalogReader`](../../src/ProjectAegis.Data/Catalog/ICatalogReader.cs)
surface (sensor/combat-radius probes + sorted mobility/mount rows) — no ORBAT lookup and no catalog API
widening. The resolved platform id is what the host feeds into `ResolveSelectedUnitRanges`, so an instance
unit draws its base platform's reach instead of falling back to 40 nm / 20 nm.

---

## 2. Doctrine ROE overlay (CMD-33)

A per-unit label of the **effective ROE** and where it was **inherited** from, optionally pinned to the
unit's map position so the host can draw it in place. `DoctrineMapOverlayProjection.Project(inheritance,
mapSymbols?)` takes rows from
[`DoctrineInheritanceProjection.ProjectAllUnits`](../../src/ProjectAegis.Delegation/Projection/DoctrineInheritanceProjection.cs)
and emits `DoctrineMapOverlayEntry(UnitId, RoeLabel, SourceLabel, NormalizedX?, NormalizedY?)` rows in
ordinal `UnitId` order, filling positions from the first `MapSymbolEntry` whose `SymbolId == UnitId`
(no matching symbol → position `null`, still counted).

> The doctrine **resolution** chain (how effective ROE + inheritance source are computed) and the
> projection's own internals are documented in
> [`doctrine-inheritance-and-override.md`](doctrine-inheritance-and-override.md) §3 — this doc only
> covers its role as a map **content overlay** and how its count reaches the HUD. The map host builds the
> inheritance rows from **alive friendly** OOB units against the loaded scenario policy.

---

## 3. Datalink mesh (CMD-32)

`DatalinkUnitPairFeed.BuildMesh` / `ProjectEdges` produces adjacent-pair edges over the sorted friendly
OOB (`u0→u1, u1→u2, …`) keyed by a catalog link id, and maps live comms state onto edge status
(`Nominal→Up`, `Degraded→Degraded`, `Denied→Down`). The mesh construction, link-id resolution, and the
comms-status overload are fully documented in
[`c2-projection-layer.md`](c2-projection-layer.md) and
[`comms-degradation-runtime.md`](comms-degradation-runtime.md); this doc only covers how its **count** is
surfaced on the map HUD.

> **Host wiring note (updated DRG-160).** `MapPlaceholderPanelHost` now calls
> `ProjectEdges(friendlyIds, links, commsSnapshot: comms)` with the live `CommsStateSnapshot` it already
> projects via [`CommsStateProjection`](../../src/ProjectAegis.Delegation/Projection/CommsStateProjection.cs),
> so map-overlay edges carry real `Up` / `Degraded` / `Down` status (previously they always defaulted to
> `Up`). Comms state also joins the host's dirty-flag, so a comms transition triggers a map refresh — see
> [§5](#5-host-wiring--mapplaceholderpanelhostapplyoverlaycounts).

---

## 4. Surfacing counts — `MapPanelApplyState`

The map HUD shows **counts**; the overlay *geometry* is drawn separately by the UI Toolkit render layer
([§6](#6-canvas-draw-layer-drg-160), DRG-160). `MapPanelApplyState.Apply` / `BindAndApply` take the three
optional overlay lists plus an optional LOD output count and fold them into `MapPanelPresentation`:

| Field | Source | Null/empty behaviour |
|-------|--------|----------------------|
| `EnvelopeRingCount` | `rings` | non-null element count; `0` when null/empty |
| `DatalinkEdgeCount` | `edges` | non-null element count; `0` when null/empty |
| `DoctrineOverlayCount` | `doctrineOverlay` | non-null element count; `0` when null/empty |
| `LodOutputCount` | `lodOutputCount` | when `null`, defaults to the **bound symbol count** (no reduction reported); `0` when `state` is null |

The overloads are additive (older call sites that pass no overlays still compile and report zero counts),
and `CountNonNull` tolerates null elements in a host-supplied list. `MapPanelPresentation.Empty` is the
null-state view (all zeros).

`LodOutputCount` is owned by the LOD clusterer documented in
[`map-view-controls.md`](map-view-controls.md); it rides on the same presentation record but is a
view-control concern, not a content overlay.

---

## 5. Host wiring — `MapPlaceholderPanelHost.ApplyOverlayCounts`

The Unity host is the only place these projections are composed at runtime. Each map refresh (guarded by
a dirty-flag so nothing recomputes while selection / symbols / phase / **comms state** are unchanged):

1. Read the `ICatalogReader` and `SelectedUnitId` from the presentation feed / bridge host, and map the
   unit to a platform id with `MapEnvelopePlatformResolver.Resolve(catalog, selectedUnitId)`
   ([§1.3](#13-mapping-unit--platform--mapenvelopeplatformresolver-drg-160)).
2. `CatalogEnvelopeRangeResolver.ResolveSelectedUnitRanges(catalog, catalogPlatformId, MvpDefault)` →
   `TacticalOverlayProjection.ProjectSelectedUnitEnvelopes(...)` → **rings**.
3. Collect **alive** friendly unit ids from the OOB tree → `DatalinkUnitPairFeed.ProjectEdges(ids,
   catalog.GetSortedLinks(), commsSnapshot: comms)` → **edges** (the live `CommsStateSnapshot` is projected
   once per refresh via `CommsStateProjection` and reused).
4. `DoctrineInheritanceProjection.ProjectAllUnits(aliveUnitIds, scenarioPolicy, isFriendly: true)` →
   `DoctrineMapOverlayProjection.Project(inheritance, mapSymbols)` → **doctrine overlay**.
5. `MapPanelApplyState.Apply(panelState, rings, edges, doctrineOverlay)` → bind the three counts onto
   the `ENVELOPES: n` / `DATALINKS: n` / `DOCTRINE: n` labels (all `Q<Label>` lookups are null-safe, so a
   scene without the labels simply skips them).
6. **`ApplyCanvasOverlays(rings, edges)`** (DRG-160) → index live symbol positions, project ring/edge
   canvas shapes, and `MapCanvasOverlayRenderer.Sync(...)` them onto the canvas draw layer
   ([§6](#6-canvas-draw-layer-drg-160)). A null renderer (no canvas resolved) is a safe no-op.

The last projected counts are also exposed as `LastEnvelopeRingCount` / `LastDatalinkEdgeCount` /
`LastDoctrineOverlayCount` for headless assertions.

> **Comms in the dirty-flag.** DRG-160 adds `ProjectCommsSnapshot().State` to the host's change check, so a
> comms transition (`Nominal → Degraded → Denied`) now forces a refresh and re-colors the edges even when
> selection and symbols are otherwise unchanged.

---

## 6. Canvas draw layer (DRG-160)

Before DRG-160 the overlays reached the operator only as HUD *counts*. DRG-160 adds an actual draw layer
that renders the rings and edges **on the map canvas**, split into a pure geometry projection and a thin
pooled Unity renderer.

### 6.1 Geometry — `MapCanvasOverlayGeometry`

Pure, deterministic, Unity-free. It turns the headless overlay entries plus the current symbol positions
into normalized canvas shapes (all coordinates are `0–1` fractions of the canvas box):

| Member | Signature / value | Behaviour |
|--------|-------------------|-----------|
| `DefaultTheaterWidthNm` | `800.0` | Placeholder Baltic theater width used to scale nm → normalized radius. |
| `NmToNormalizedRadius(rangeNm, theaterWidthNm = 800)` | `float` | `rangeNm / theaterWidthNm`, clamped to `[0, 1]`; returns `0` for non-positive inputs. |
| `BuildUnitPositionIndex(symbols)` | `IReadOnlyDictionary<string,(float X,float Y)>` | Indexes **live** `MapSymbolDisplayRow`s by `SymbolId → (NormalizedX, NormalizedY)`. Skips null/blank rows, `IsGhost` rows, and any `ghost:`-prefixed id; **first live row wins** per id. |
| `ProjectRings(rings, positions, theaterWidthNm = 800)` | `IReadOnlyList<MapCanvasRingShape>` | One circle per `EnvelopeRingEntry` whose `UnitId` is in the index and whose radius is `> 0`; centered on the unit, styled `--sensor` / `--weapon` by `RingKind`. |
| `ProjectEdges(edges, positions)` | `IReadOnlyList<MapCanvasEdgeShape>` | One segment per `DatalinkEdgeEntry` **both** of whose endpoints are in the index; styled `--up` / `--degraded` / `--down` by `Status`. Edges to a missing unit are silently skipped. |

Style-class constants live on the type (`RingStyleSensor` / `RingStyleWeapon`,
`EdgeStyleUp` / `EdgeStyleDegraded` / `EdgeStyleDown`) and match the USS selectors below. Shape `Key`s are
stable (`"{unitId}:{ringKind}"`, `"{from}->{to}"`) so the renderer can reconcile in place.

> **Normalized radius is a placeholder.** Ring radius scales off the fixed 800 nm `DefaultTheaterWidthNm`,
> not a real basemap projection — it gives a proportional on-canvas circle for the flat Baltic placeholder,
> and is the intended seam to swap for a georeferenced scale when the product map lands.

### 6.2 Renderer — `MapCanvasOverlayRenderer` (Unity)

The only Unity-side type. It owns two `pickingMode = Ignore` child layers inserted at the **front** of the
canvas child list (`ring-layer` at index 0, `edge-layer` at index 1) so both draw **behind** the unit
symbols the host adds afterwards; rings sit behind edges.

- **`Sync(rings, edges)`** reconciles each layer to the shape list, keyed by `Shape.Key`: it creates a
  `VisualElement` for a new key, reuses the existing element when the shape record is unchanged (record
  equality short-circuits restyle), re-applies geometry/style when it changed, and prunes elements whose
  key is no longer present. Null lists are treated as empty.
- **Ring layout** sets absolute position + a 50%-radius border on a transparent box: `width/height =
  radius × 200%` of the canvas and `left/top = (center − radius) × 100%`.
- **Edge layout** places a 2 px bar at the `from` point, `width = segment length × 100%`, rotated by
  `atan2(dy, dx)` about its left-center origin; a zero-length segment is hidden (`display: none`).
- **`Clear()`** detaches both layers; **`RingCount` / `EdgeCount`** expose the live pooled counts for
  diagnostics / tests.

The renderer is guarded by `#if UNITY_5_3_OR_NEWER`; headless tests exercise the geometry
(`MapCanvasOverlayGeometryTests`) and the platform resolver (`MapEnvelopePlatformResolverTests`) directly.

### 6.3 USS classes — `MapPlaceholderPanel.uss`

The DRG-160 selectors (added to
[`MapPlaceholderPanel.uss`](../../unity/ProjectAegis/Assets/UI/MapPlaceholder/MapPlaceholderPanel.uss))
own only color / border / picking; all positioning is inline from the renderer:

| Class | Role |
|-------|------|
| `.map-overlay-layer` | Absolute full-bleed, `picking-mode: ignore` (the ring/edge layers). |
| `.map-overlay-ring` | Transparent fill, 1 px border. |
| `.map-overlay-ring--sensor` / `--weapon` | Blue / red border tint. |
| `.map-overlay-edge` | 2 px bar. |
| `.map-overlay-edge--up` / `--degraded` / `--down` | Green / amber / grey link status. |

---

## Invariants

- **Pure / presentation-only.** No type here mutates sim state, appends to `DecisionLog`, or does file
  I/O. They read the catalog and existing projections and return value records. Replay goldens
  (Baltic v2 hash `17144800277401907079`) are untouched — these overlays are outside the fingerprinted
  path (ADR-010 §2–3).
- **Deterministic.** Doctrine rows sort by `UnitId` (ordinal); datalink pairs sort by unit id; envelope
  rings are `[Sensor, Weapon]`. Canvas geometry preserves input order and keys shapes by unit id. No RNG,
  no wall-clock — including the DRG-160 geometry and platform resolver.
- **Fallbacks never fail closed to zero.** An absent/misconfigured catalog yields the 40 nm / 20 nm
  default rings, not empty overlays — the operator always sees *something* for a selected unit. The
  platform resolver likewise returns the unit id unchanged rather than dropping it.
- **Approved-only sensor reach.** Sensor range aggregates only `Approved` sensor bindings, so unreviewed
  catalog rows can't inflate a displayed envelope.
- **Math stays headless.** `MapCanvasOverlayGeometry` (pure) does all range→radius and position math;
  `MapCanvasOverlayRenderer` only pools/positions `VisualElement`s and is compiled under
  `#if UNITY_5_3_OR_NEWER`, so the whole subsystem is testable without Unity.
- **Draw layer is picking-transparent and rear-most.** Overlay layers set `picking-mode: ignore` and
  insert behind the unit symbols, so rings/edges never intercept clicks or hide selectable symbols.
- **Additive host surface.** New overlays extend `MapPanelApplyState` / `MapPanelPresentation` with new
  optional params + count fields; existing call sites keep compiling and reporting zero.

---

## Extending

- **A new ring kind (e.g. jammer reach):** add the resolver (return nm from a catalog field, with a
  fallback const), extend `TacticalOverlayProjection` to emit the extra `EnvelopeRingEntry`, and add a
  `RingKind*` const. Keep the resolver `Try…` pattern (`false` on missing/invalid) so the fold can fall
  back cleanly.
- **A new count on the HUD:** add an optional param to the `MapPanelApplyState.Apply` / `BindAndApply`
  overload chain and a defaulted field on `MapPanelPresentation` (mirror `DoctrineOverlayCount`), then
  bind it in `ApplyOverlayCounts`. Do **not** reorder existing record fields.
- **Live comms on the map mesh:** already wired (DRG-160) — the host passes its `CommsStateProjection`
  snapshot into `DatalinkUnitPairFeed.ProjectEdges` and adds comms `State` to the dirty-flag, matching the
  pattern in [`comms-degradation-runtime.md`](comms-degradation-runtime.md).
- **A new drawn shape kind:** add a `MapCanvas*Shape` record + a `Project…` helper on
  `MapCanvasOverlayGeometry` (keep it pure and keyed), a `Style*` const matching a USS selector, and a
  reconcile block on `MapCanvasOverlayRenderer` (mirror the ring/edge pool). Keep the renderer math-free.
- **A real basemap scale:** replace `NmToNormalizedRadius`'s fixed `DefaultTheaterWidthNm` with a
  georeferenced projection; it is deliberately isolated as the single seam for that swap.

Cover any new resolver/projection/geometry with `ProjectAegis.Delegation.Tests` cases under `Projection/`
(the family currently has focused fixtures for the range resolver, the ring projection, the doctrine
overlay, the datalink feed, the apply-state fold, and the DRG-160 canvas geometry + platform resolver) and
confirm the full gate stays green:

```bash
dotnet build ProjectAegis.sln                       # 0 warnings / 0 errors
dotnet test  ProjectAegis.sln -v minimal            # full suite, 0 failures
```

---

## Related

| Doc | Relationship |
|-----|--------------|
| [`map-view-controls.md`](map-view-controls.md) | Camera / layer controls (layer stack, LOD clustering, scale/measure) — the *view* side; owns `LodOutputCount`. |
| [`c2-projection-layer.md`](c2-projection-layer.md) | The full read-model catalog and the map picture content (`MapPictureProjection`); the datalink feed lives here. |
| [`globe-map-view-projection.md`](globe-map-view-projection.md) | The product-globe camera / billboard presentation. |
| [`doctrine-inheritance-and-override.md`](doctrine-inheritance-and-override.md) | How effective ROE + inheritance source (the doctrine overlay's input) is resolved. |
| [`comms-degradation-runtime.md`](comms-degradation-runtime.md) | The comms clock behind datalink edge status. |
