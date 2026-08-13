# Tactical map overlays — selected-unit envelope rings, doctrine ROE & datalink mesh

> **Scope.** The pure, headless **content overlays** that the C2 map host draws *on top of* the
> tactical picture in [`ProjectAegis.Delegation/Projection/`](../../src/ProjectAegis.Delegation/Projection):
> the CMD-21 / CMD-34 **selected-unit envelope rings** (weapon + sensor reach, resolved from the
> catalog), the CMD-33 **doctrine ROE overlay** (per-unit effective ROE + inheritance source), and the
> CMD-32 **datalink unit-pair mesh** — all folded into count fields on `MapPanelPresentation` and bound
> by the Unity [`MapPlaceholderPanelHost`](../../unity/ProjectAegis/Assets/Scripts/Runtime/MapPlaceholderPanelHost.cs).
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
```

Every helper is `static`, deterministic, and Unity-free. The Unity host binds the resulting records /
counts onto UI Toolkit widgets and does no math of its own.

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
| [`MapPlaceholderPanelHost`](../../unity/ProjectAegis/Assets/Scripts/Runtime/MapPlaceholderPanelHost.cs) | `MonoBehaviour` | The Unity consumer — resolves inputs from the bridge, calls the projections, binds counts to labels. |

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

---

## 2. Doctrine ROE overlay (CMD-33)

A per-unit label of the **effective ROE** and where it was **inherited** from, optionally pinned to the
unit's map position so the host can draw it in place. This is the *read-side* companion to the
doctrine-resolution runtime documented in
[`doctrine-inheritance-and-override.md`](doctrine-inheritance-and-override.md).

```csharp
IReadOnlyList<DoctrineMapOverlayEntry> Project(
    IReadOnlyList<DoctrineInheritanceEntry> inheritanceEntries,
    IReadOnlyList<MapSymbolEntry>? mapSymbols = null)
```

- Input rows come from
  [`DoctrineInheritanceProjection.ProjectAllUnits`](../../src/ProjectAegis.Delegation/Projection/DoctrineInheritanceProjection.cs)
  (unit → effective ROE + inheritance source: unit override → mission ROE → side default).
- Output is **deterministic**, ordered by `UnitId` (ordinal); rows with blank `UnitId` are dropped;
  empty input yields an empty list.
- When `mapSymbols` is supplied, each entry's `NormalizedX/Y` is filled from the first `MapSymbolEntry`
  whose `SymbolId == UnitId` (friendly OOB symbols key on the unit id). No matching symbol leaves the
  position `null` (still counted, just not placed).

---

## 3. Datalink mesh (CMD-32)

`DatalinkUnitPairFeed.BuildMesh` / `ProjectEdges` produces adjacent-pair edges over the sorted friendly
OOB (`u0→u1, u1→u2, …`) keyed by a catalog link id, and maps live comms state onto edge status
(`Nominal→Up`, `Degraded→Degraded`, `Denied→Down`). The mesh construction, link-id resolution, and the
comms-status overload are fully documented in
[`c2-projection-layer.md`](c2-projection-layer.md) and
[`comms-degradation-runtime.md`](comms-degradation-runtime.md); this doc only covers how its **count** is
surfaced on the map HUD.

> **Host wiring note.** `MapPlaceholderPanelHost` currently calls `ProjectEdges(friendlyIds, links)`
> **without** a `CommsStateSnapshot`, so map-overlay edges default to `Up`. The comms-aware overload
> exists for callers that want live edge status; wiring the map host to it is a follow-up, not a
> regression.

---

## 4. Surfacing counts — `MapPanelApplyState`

The map HUD shows **counts**, not the overlay geometry — the geometry is drawn by the (Cesium/UI Toolkit)
render layer. `MapPanelApplyState.Apply` / `BindAndApply` take the three optional overlay lists plus an
optional LOD output count and fold them into `MapPanelPresentation`:

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
a dirty-flag so nothing recomputes while selection / symbols / phase are unchanged):

1. Read the `ICatalogReader` and `SelectedUnitId` from the presentation feed / bridge host.
2. `CatalogEnvelopeRangeResolver.ResolveSelectedUnitRanges(catalog, selectedUnitId, MvpDefault)` →
   `TacticalOverlayProjection.ProjectSelectedUnitEnvelopes(...)` → **rings**.
3. Collect **alive** friendly unit ids from the OOB tree → `DatalinkUnitPairFeed.ProjectEdges(ids,
   catalog.GetSortedLinks())` → **edges**.
4. `DoctrineInheritanceProjection.ProjectAllUnits(aliveUnitIds, scenarioPolicy, isFriendly: true)` →
   `DoctrineMapOverlayProjection.Project(inheritance, mapSymbols)` → **doctrine overlay**.
5. `MapPanelApplyState.Apply(panelState, rings, edges, doctrineOverlay)` → bind the three counts onto
   the `ENVELOPES: n` / `DATALINKS: n` / `DOCTRINE: n` labels (all `Q<Label>` lookups are null-safe, so a
   scene without the labels simply skips them).

The last projected counts are also exposed as `LastEnvelopeRingCount` / `LastDatalinkEdgeCount` /
`LastDoctrineOverlayCount` for headless assertions.

---

## Invariants

- **Pure / presentation-only.** No type here mutates sim state, appends to `DecisionLog`, or does file
  I/O. They read the catalog and existing projections and return value records. Replay goldens
  (Baltic v2 hash `17144800277401907079`) are untouched — these overlays are outside the fingerprinted
  path (ADR-010 §2–3).
- **Deterministic.** Doctrine rows sort by `UnitId` (ordinal); datalink pairs sort by unit id; envelope
  rings are `[Sensor, Weapon]`. No RNG, no wall-clock.
- **Fallbacks never fail closed to zero.** An absent/misconfigured catalog yields the 40 nm / 20 nm
  default rings, not empty overlays — the operator always sees *something* for a selected unit.
- **Approved-only sensor reach.** Sensor range aggregates only `Approved` sensor bindings, so unreviewed
  catalog rows can't inflate a displayed envelope.
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
- **Live comms on the map mesh:** pass a `CommsStateSnapshot` into `DatalinkUnitPairFeed.ProjectEdges`
  from the host's `CommsStateProjection` (already computed for the panel), matching the pattern in
  [`comms-degradation-runtime.md`](comms-degradation-runtime.md).

Cover any new resolver/projection with `ProjectAegis.Delegation.Tests` cases under `Projection/`
(the family currently has focused fixtures for the resolver, the ring projection, the doctrine overlay,
the datalink feed, and the apply-state fold) and confirm the full gate stays green:

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
