# C2 projection layer — order-log read-models for the tactical picture

The `Projection/` folder in [`ProjectAegis.Delegation`](../../src/ProjectAegis.Delegation/Projection/)
(~75 files) is the **read side** of the simulation: the pure, engine-agnostic view-model layer
that turns the append-only order log and per-tick sim indicators into everything the C2
(command-and-control) UI draws — the message log, contact/facility picture, OOB tree, tactical
map, sensor panel, losses/scoring, catalog browsers, and APP-6 map symbology.

This guide explains the layering (`Projection → Binder → State`), the hard "read-only" contract
that keeps the UI from corrupting a deterministic run, the projection catalog, and how to add a
new panel without breaking replay.

> **Who consumes this?** Under Unity, the [`UnityAdapter`](../../src/ProjectAegis.Delegation.UnityAdapter/README.md)
> hosts [`C2PresentationController`](../../src/ProjectAegis.Delegation.UnityAdapter/Presentation/C2PresentationController.cs)
> and the `*Bridge`/`*PanelBinder` glue that drives UI Toolkit panels. Headless tests and the
> Baltic batch/replay harness call the same projections directly — there is no Unity dependency
> in this folder, so the whole read model runs under `dotnet test`.

Related: [Delegation README — order log & projections](../../src/ProjectAegis.Delegation/README.md#order-log--projections) ·
[determinism-and-replay.md](determinism-and-replay.md) ·
[abort-reason-catalog.md](abort-reason-catalog.md) ·
[ADR-003 order-log schema](../architecture/adr-003-order-log-schema.md) ·
[ADR-007 C2 map presentation](../architecture/adr-007-c2-map-presentation.md) ·
[ADR-010 headless-first command-driven UI](../architecture/adr-010-headless-first-command-driven-ui.md).

---

## The core rule: projections never mutate

Every type here is a **pure function of already-recorded state**. A projection reads a
[`DecisionLog`](../../src/ProjectAegis.Delegation/Decision/DecisionLog.cs) (the append-only
[`IOrderLog`](../../src/ProjectAegis.Delegation/Decision/IOrderLog.cs), ADR-003) and/or a
read-only per-tick snapshot, and returns an immutable `record`. It **must not**:

- write to the order log, the sim, or the catalog;
- hold mutable static state, wall-clock time, or ambient randomness;
- change the order-log fingerprint that the replay goldens assert.

This is what lets the UI rebuild the entire tactical picture from the log at any tick (including
during a scrub/replay) without perturbing the simulation. Determinism is the load-bearing
invariant of the whole codebase — see [determinism-and-replay.md](determinism-and-replay.md).
Because projections only read, they are safe to run on any thread and to re-run every frame.

Two consequences worth internalizing:

- **Ordering is explicit and total.** Projections that fold the log sort by `(SimTick/SimTime,
  SequenceId)` and break ties with `StringComparer.Ordinal` on ids — never rely on dictionary
  or hash-set enumeration order. See `ContactPictureProjection`, `OobTreeProjection`,
  `MapPictureProjection`.
- **Layout that has no sim source is derived deterministically**, not randomly.
  `MapPictureProjection.Place(key, seed)` hashes `"{seed}:{key}"` via `DeterministicHash` to a
  stable normalized `(x, y)` — so the map is reproducible until the sim publishes real world
  coordinates.

---

## Layering: `Projection → Binder → State`

Panels are built in up to three engine-agnostic stages. Not every panel needs all three, but
the shape is consistent:

| Stage | Suffix | Role | Example |
|-------|--------|------|---------|
| **Projection** | `*Projection` | Fold the order log / snapshot into a semantic model (domain rows, tallies, tracks). Static class, `Project(...)`/`Build(...)`. | `MessageLogProjection.Project(log)` → `IReadOnlyList<MessageLogLine>` |
| **Binder** | `*PanelBinder` | Map the semantic model into display rows: style classes (USS), formatted text, selection/ghost flags. Static class, `Bind(...)`. | `MessageLogPanelBinder.Bind(lines)` → `MessageLogPanelState` |
| **State** | `*PanelState` / `*State` | The immutable view-model the UI host renders. `sealed record` of display rows. | `MessageLogPanelState(IReadOnlyList<MessageLogDisplayRow>)` |

Why split projection from binder? The **projection** is the reusable, testable "what happened"
model (also consumed by CSV exporters, the AAR, and other panels). The **binder** owns the
presentation concerns that are allowed to be opinionated — USS class names, `"[CATEGORY] text"`
formatting, ghost-track offsets — without polluting the semantic model. Keeping USS strings in
the binder means a restyle never touches projection logic or tests.

```text
DecisionLog (append-only order log, ADR-003)
    │  + per-tick ISimWorldSnapshot indicators (EMCON, fire-control, engagement count)
    ▼
*Projection.Project(...)      → semantic model  (MessageLogLine, ContactPictureEntry, …)
    ▼
*PanelBinder.Bind(...)        → display rows     (styles, glyphs, formatted text)
    ▼
*PanelState (sealed record)   → rendered by C2PresentationController / UI Toolkit
```

### Example: message log, end to end

`MessageLogProjection` switches on `OrderLogEntry.Kind` and emits a stable **category** per row
(the same categories the alert tiering keys on — see below). A subset of the mapping, verified
against [`MessageLogProjection.cs`](../../src/ProjectAegis.Delegation/Projection/MessageLogProjection.cs):

| Order-log entry kind | Category | Notes |
|----------------------|----------|-------|
| `EngagementOutcome` = `Kill` | `KILL_CONFIRMED` | Inbound-threat criticality lives here, not on launch |
| `EngagementOutcome` = `Intercept` / `Hit` / `Miss` | `INTERCEPT_SUCCESS` / `HIT` / `MISS` | |
| `Engagement` (launched) | `WEAPON_LAUNCH` | Fires on friendly launches too → `Routine` alert tier |
| `Engagement` (aborted) | `ENGAGE_ABORT` | Carries the [abort reason code](abort-reason-catalog.md) |
| `PolicyDenial` | `POLICY_DENIAL` | "Why can't I fire?" — links the explain |
| `ContactChange` | `CONTACT` | |
| `MagazineChange` | `MAGAZINE` | Signed delta + reason code |
| `ModeChange` | `MODE` | |
| `PlayerOrder` | `PLAYER_ORDER` | |
| `CommsStateChange` | `COMMS` | |
| `FuelStateChange` / `FuelBurn` | `FUEL` | |

Unrecognized entry kinds project to `null` (dropped) rather than throwing, so a new order-log
kind never crashes the log panel — it just isn't surfaced until a case is added.

`MessageLogPanelBinder.Bind` then formats each line as `"[{Category}] {Text}"` into a
`MessageLogPanelState`. The **`MessageLogLine` carries no duplicate storage** — it references
the log's `SequenceId`/`SimTime`, so the message log is a projection, not a second event store.

---

## C2 rev-2 alert & lifecycle contracts

The req-20 rev-2 UI delta added a few small, presentation-only taxonomy contracts here so the
parallel UI tracks and the Unity host share one source of truth (ADR-010). All are read-only
lookups; none touch sim or order-log state.

- **`AlertSeverity`** — alert tier: `Critical` (toast + optional auto-pause) → `Notable`
  (log highlight) → `Routine` (log only). Tier is **never colour-only** (accessibility).
- **`AlertSeverityMap.ForCategory(category)`** — the single mapping from a `MessageLogLine`
  category to an `AlertSeverity`. It is **case-insensitive and fails safe**: unknown/null
  categories default to `Routine`, so adding a new message category never silently escalates it
  to a toast. `WEAPON_LAUNCH` is deliberately `Routine` (it fires on friendly launches);
  inbound criticality is carried by `KILL_CONFIRMED` / `POLICY_DENIAL`.
- **`OrderLifecycleState`** — the player-order lifecycle surfaced to the UI:
  `Accepted → Queued → Executing → Completed | Denied | Aborted` (last three terminal).
  `Denied` links the "Why can't I fire?" explain to the matching `POLICY_DENIAL`.

The remappable input-action IDs the UI binds to (`input.cycle_unit`,
`input.focus_primary_threat`, `input.cancel`) live in the sibling `Input/` folder
([`C2InputActions`](../../src/ProjectAegis.Delegation/Input/C2InputActions.cs)), not here.

---

## APP-6 / MIL-STD-2525C map symbology (ADR-007)

The map layer resolves tactical symbols in a data-driven, atlas-optional way:

- **[`App6Sidc`](../../src/ProjectAegis.Delegation/Projection/App6Sidc.cs)** maps an affiliation
  (`Friendly` / `Hostile` / `Neutral` / `Suspect` / `Pending`) + destroyed flag to three things:
  a **unicode fallback glyph**, a **USS frame class** (`map-app6-frame--*`), and a **15-char
  SIDC** string. It can also parse the Standard Identity character out of an existing SIDC
  (`'F'/'A'/'D'/'M'/'J'/'K'/'L'` → Friendly, `'H'` → Hostile, …). Anything missing or malformed
  resolves to the neutral **`FallbackSidc` / `FallbackGlyph` (`●`)** — never an exception.
- **[`App6GlyphAtlas`](../../src/ProjectAegis.Delegation/Projection/App6GlyphAtlas.cs)** decides
  what the UI actually paints: if a sprite atlas is loaded and has the frame, it returns a
  `DisplayGlyph` that uses the **atlas frame class**; otherwise it **degrades to the unicode
  glyph**. This is the ADR-007 Phase C "atlas-optional" contract — headless tests and unstyled
  hosts still get a legible glyph.
- **[`MapPictureProjection`](../../src/ProjectAegis.Delegation/Projection/MapPictureProjection.cs)**
  builds `MapSymbolEntry` rows from the OOB (friendly) and contact picture (hostile), placing
  each with the deterministic hash-layout above.
- **[`MapPanelBinder`](../../src/ProjectAegis.Delegation/Projection/MapPanelBinder.cs)** applies
  affiliation/selection/comms USS classes and, under `CommsState.Degraded`, appends a **ghost
  row** (`ghost:{id}`, offset by the scenario's `GhostOffset`, labelled with the lag ticks) to
  visualize track staleness; `CommsState.Denied` marks symbols `--frozen`.

---

## Tactical map overlays & the overlay-count HUD (CMD-21/32/33/34)

On top of the symbol picture, the map surfaces three optional **overlays** for the selected
unit and friendly force. Each is a pure projection; the Unity host folds them into
count-only HUD labels. These landed with the S107/S108 UI-maturity train and are extended
additively (new UXML labels only) — see the `MapPlaceholderPanel.uxml` overlay-count row.

| Overlay | Projection (pure, `src/…/Projection/`) | Row record | Tracked as |
|---------|----------------------------------------|------------|------------|
| **Envelope rings** | [`TacticalOverlayProjection.ProjectSelectedUnitEnvelopes`](../../src/ProjectAegis.Delegation/Projection/TacticalOverlayProjection.cs) | `EnvelopeRingEntry(UnitId, RingKind, Domain, RangeNm, IsSelectedUnit)` | CMD-21 (Phase A baseline) / CMD-34 |
| **Datalink edges** | [`DatalinkUnitPairFeed.ProjectEdges`](../../src/ProjectAegis.Delegation/Projection/DatalinkUnitPairFeed.cs) → [`DatalinkPictureProjection`](../../src/ProjectAegis.Delegation/Projection/DatalinkPictureProjection.cs) | `DatalinkEdgeEntry(FromUnitId, ToUnitId, LinkType, Status)` | CMD-32 |
| **Doctrine rows** | [`DoctrineMapOverlayProjection.Project`](../../src/ProjectAegis.Delegation/Projection/DoctrineMapOverlayProjection.cs) | `DoctrineMapOverlayEntry(UnitId, RoeLabel, SourceLabel, NormalizedX?, NormalizedY?)` | CMD-33 |

**How each overlay is derived (read-only, deterministic):**

- **Envelope rings** — for the selected unit only, emit exactly two rings: a `Sensor` ring and
  a `Weapon` ring (`RingKind` = `"Sensor"`/`"Weapon"`). Ranges come from
  [`CatalogEnvelopeRangeResolver.ResolveSelectedUnitRanges`](../../src/ProjectAegis.Delegation/Projection/CatalogEnvelopeRangeResolver.cs),
  which reads the weapon envelope from `ICatalogReader.TryGetWeaponEnvelope` and converts
  meters → nautical miles (`1 nm = 1852 m`). When the catalog is missing/unknown or the max
  range is non-positive, it falls back to `DefaultSensorRangeNm = 40` / `DefaultWeaponRangeNm = 20`
  so a ring is always drawable. No selection ⇒ **empty** list.
- **Datalink edges** — build a simple mesh over the **sorted, distinct, alive friendly unit
  ids** (`u0→u1, u1→u2, …`) keyed by a catalog link. Link resolution prefers a caller-supplied
  `preferredLinkId`, then the first `Tactical` catalog link, then the first link of any type.
  Fewer than two units, or no catalog links, ⇒ **empty** mesh. `ProjectEdges` stamps status
  `Up` on the produced pairs.
- **Doctrine rows** — one row per alive friendly unit carrying its effective ROE label and
  inheritance source, ordered by `UnitId` (`StringComparer.Ordinal`). When map symbols are
  supplied, each row is annotated with the matching symbol's normalized `(x, y)` (friendly OOB
  symbols use `UnitId` as `SymbolId`); otherwise positions stay `null`.

**Apply-state (count-only presentation):**
[`MapPanelApplyState.Apply`](../../src/ProjectAegis.Delegation/Projection/MapPanelApplyState.cs)
folds the bound `MapPanelState` plus the (nullable) overlay lists into an immutable
`MapPanelPresentation`. Overlays are **count-only at the presentation seam** — `Apply` reports
`EnvelopeRingCount` / `DatalinkEdgeCount` / `DoctrineOverlayCount` via a null-safe `CountNonNull`
(null **or** empty ⇒ `0`; null elements are skipped). `LodOutputCount` defaults to the bound
symbol count when no LOD reduction is supplied (REQ-20 Phase N). The overloads are additive, so
existing call sites that pass no overlays keep the old three-arg behavior.

**Unity host wiring:** `MapPlaceholderPanelHost.ApplyOverlayCounts()`
([`unity/…/Runtime/MapPlaceholderPanelHost.cs`](../../unity/ProjectAegis/Assets/Scripts/Runtime/MapPlaceholderPanelHost.cs))
runs each projection, calls `MapPanelApplyState.Apply`, stores the counts on
`LastEnvelopeRingCount` / `LastDatalinkEdgeCount` / `LastDoctrineOverlayCount`, and writes the
`ENVELOPES: n` / `DATALINKS: n` / `DOCTRINE: n` label text. Label lookups are **null-safe `Q`**
by name (`envelope-ring-count`, `datalink-edge-count`, `doctrine-overlay-count`), so a panel
whose UXML omits a label simply skips it — **no scene or panel rebuild is required** to add one.
The default `MapPlaceholderPanel.uxml` currently ships the `envelope-ring-count` and
`datalink-edge-count` labels; `doctrine-overlay-count` is host-supported but not yet in the
default UXML.

> **Boundary reminder:** these overlays are presentation reads only. Ranges/links/doctrine come
> from the catalog and projections; the host never writes to `DecisionLog`, the sim, or the
> catalog (ADR-010 / ADR-007). Envelope/datalink/doctrine work is a *subset* of the full CMD-30
> overlay control, not the whole Phase N EW product.

---

## Basemap layer stack HUD (CMD-28.2)

The map also carries a **basemap layer stack** — an ordered checklist of raster/vector basemap
layers the operator can toggle (Satellite, Relief, Borders, …). Unlike the overlays above, this
is **not derived from the order log or sim at all**: it is pure **UI-local presentation state**,
the same ADR-010 exception that lets [selection](#selection-state-unity-host) live on the host.
Toggling a layer must never touch `DecisionLog`, the sim, or replay.

**Types (all in `Projection/`):**

| Type | Role |
|------|------|
| [`MapLayerId`](../../src/ProjectAegis.Delegation/Projection/MapLayerId.cs) | Stable enum of the 8 layers in draw order: `Satellite`, `Relief`, `Borders`, `Terrain`, `Roads`, `LandCover`, `Placenames`, `DayNight` |
| [`MapLayerEntry`](../../src/ProjectAegis.Delegation/Projection/MapLayerEntry.cs) | One row: `(Id, Label, IsVisible, ShortcutHint)`. `ShortcutHint` is a presentation-only discovery string (`"none"` today) — **not** input routing |
| [`MapLayerStackProjection.DefaultStack()`](../../src/ProjectAegis.Delegation/Projection/MapLayerStackProjection.cs) | Pure factory for the default ordered stack: **all layers visible except `DayNight`** (off by default) |
| [`MapLayerStackState`](../../src/ProjectAegis.Delegation/Projection/MapLayerStackState.cs) | The ordered stack. Mutations are **immutable-style**: `Toggle` / `SetVisible` / `ApplyVisibilitySnapshot` return a *new* instance (or `this` when nothing changed). Also `WithDefaults()`, `VisibleCount`, `Count`, `TryGet`, `ToVisibilitySnapshot()` |
| [`MapLayerStackApplyState.Apply(state)`](../../src/ProjectAegis.Delegation/Projection/MapLayerStackApplyState.cs) | Folds the stack into an immutable `MapLayerStackPresentation` (`Lines`, `DisplayLines`, `VisibleCount`, `TotalCount`, `SummaryLabel`). Null/empty ⇒ `Empty` (`"LAYERS: 0/0"`). `ProjectAndApplyDefaults()` is the headless smoke path |
| [`MapLayerStackStore`](../../src/ProjectAegis.Delegation/Projection/MapLayerStackStore.cs) | In-memory `string→bool` visibility bag keyed by `MapLayerId` name (`Capture` / `Restore` / `Get`/`SetSnapshot`). **UI-local only — not replay, not `DecisionLog`, not file I/O** |

**Presentation format (owned by `Apply`, not the host):** each checklist line renders as
`"[x] {Label}  ({shortcut})"` (`[ ]` when hidden), and the HUD summary is `"LAYERS: {visible}/{total}"`.
Keeping the formatting in `MapLayerStackApplyState` means a host binds text directly onto a
`Label`/`Toggle` row without re-deriving it — the same projection/apply split used everywhere else.

**Unity host wiring:** `MapPlaceholderPanelHost` restores the stack on `Awake` via
`_layerStore.Restore(MapLayerStackState.WithDefaults())`, then `ApplyLayerStackHud()` runs
`MapLayerStackApplyState.Apply(_layerStack)` and writes the `SummaryLabel` into the null-safe
`layer-count` label (exposing `LastLayerVisibleCount` / `LastLayerTotalCount` /
`LastLayerSummaryLabel`). `ToggleLayer(MapLayerId)` flips one layer, captures the new visibility
into the in-memory store, and forces a refresh; `SetLayerStack(state)` replaces the whole stack
(tests / prefs restore). As with `doctrine-overlay-count`, the `layer-count` label is
**host-supported but not shipped in the default `MapPlaceholderPanel.uxml`** — adding it is a
UXML-only change, no scene or host rebuild. Coverage lives in
[`MapLayerStackTests.cs`](../../src/ProjectAegis.Delegation.Tests/Projection/MapLayerStackTests.cs).

> **Boundary reminder:** the layer stack is UI chrome, not the map picture. It changes *what
> basemap tiles are drawn*, never *what units/contacts exist* — so it stays off the deterministic
> path entirely (no `DecisionLog`, no seed, no replay fingerprint), exactly like host selection.

---

## Map scale, measure & unit-cycle helpers (CMD-20 / 28.4 / 28.5)

The remaining map-HUD helpers live together in
[`MapScaleProjection.cs`](../../src/ProjectAegis.Delegation/Projection/MapScaleProjection.cs)
as three small **pure-math static classes**. Like the layer stack, none of them read the order
log or the sim — their inputs are the **camera** (altitude, meters-per-screen-unit) and UI-local
ids/coordinates — so they are safe to re-run every frame and never touch the replay fingerprint.

| Helper | Track | `Project`/entry point | Immutable result |
|--------|-------|-----------------------|------------------|
| [`MapScaleProjection`](../../src/ProjectAegis.Delegation/Projection/MapScaleProjection.cs) | CMD-20 | `Project(cameraAltitudeMeters, metersPerScreenUnit)` | `MapScaleState(ScaleBarLabel, CameraAltitudeLabel, ScaleNauticalMiles, CameraAltitudeMeters)` |
| `MapMeasureProjection` | CMD-28.4 | `Measure(fromX, fromY, toX, toY, metersPerUnit)` | `MapMeasureResult(RangeMeters, RangeNauticalMiles, BearingDegrees, Label)` |
| `UnitCycleProjection` | CMD-28.5 | `Next(unitIds, currentId)` / `Previous(unitIds, currentId)` | `string?` (next/prev id) |

**How each is derived (read-only, deterministic):**

- **Scale bar / altitude** — `metersPerScreenUnit` is converted to nautical miles
  (`MetersPerNauticalMile = 1852.0`); both inputs are clamped to `>= 0`. `FormatScaleBar`
  picks precision by magnitude (`SCALE 120 NM` ≥ 100, `SCALE 42.5 NM` ≥ 10, else `SCALE 3.14 NM`)
  and `FormatAltitude` switches to km at ≥ 1 000 000 m (`CAM ALT 868 km` vs `CAM ALT 12000 m`).
  A non-positive scale/altitude renders the em-dash placeholder (`SCALE —` / `CAM ALT —`) rather
  than `0`, so an un-initialized camera reads as "unknown", not "zero range".
- **Range / bearing** — Euclidean distance in screen units × `metersPerUnit` → meters → nm;
  bearing is `atan2(dx, dy)` normalized to `[0, 360)` so **due-north is `000.0°`** (compass
  convention, not the math-`atan2` `x`-axis convention). The formatted `Label` is
  `"RNG {nm:0.00} NM  BRG {brg:000.0}°"`.
- **Unit cycle** — walks an already-ordered id list with `StringComparer.Ordinal` and wraps at
  both ends. Fail-safe: an empty/`null` list ⇒ `null`; a `null`/empty `currentId` ⇒ the first
  (`Next`) or last (`Previous`) id; an id not in the list ⇒ the first (`Next`) or last
  (`Previous`) id. It expects an already-ordered id list (e.g. the sorted friendly-unit ids the
  OOB/datalink feeds build) — it does not sort internally.

**Unity host wiring:** [`MapScaleHudPanelHost`](../../unity/ProjectAegis/Assets/Scripts/Runtime/MapScaleHudPanelHost.cs)
(a separate `UIDocument` panel from `MapPlaceholderPanelHost`, DRG-67) binds the two labels
`map-scale-bar` / `map-scale-altitude` under a `map-scale-hud-root` element. The host does **not**
call `Project` itself — the camera/globe layer pushes a computed `MapScaleState` in via
`Apply(state)` (also the headless/test path), and `LastState` exposes the last applied value.
Label lookups are **null-safe `Q`** and the panel wires as long as *either* label resolves, so a
trimmed UXML that omits one label still works; there is no scale HUD in a default shipping UXML
today, so the panel is host-ready but must be added to a UXML to appear. `MapMeasureProjection`
and `UnitCycleProjection` are host-agnostic pure helpers intended for the range/bearing measure
tool and next/prev-unit cycling respectively; today they are exercised only by unit tests (no
production host or input-action call site yet), so treat them as ready-to-wire building blocks.

Coverage: [`MapScaleAndCycleTests.cs`](../../src/ProjectAegis.Delegation.Tests/Projection/MapScaleAndCycleTests.cs).

> **Boundary reminder:** these are camera/interaction helpers, not sim reads. They convert
> screen-space geometry and camera state into labels/ids; nothing here writes to `DecisionLog`,
> the sim, or the catalog, and none of it participates in the replay fingerprint.

---

## Projection catalog

Grouped by the panel/surface they feed. All live in
[`src/ProjectAegis.Delegation/Projection/`](../../src/ProjectAegis.Delegation/Projection/).

### Tactical picture & map
| Type | Produces |
|------|----------|
| `MessageLogProjection` → `MessageLogPanelBinder` | CMANO-style message log (`MessageLogLine` → `MessageLogPanelState`) |
| `ContactPictureProjection` | Active contact tracks from `ContactChange` rows; `ProjectWithBda` merges order-log BDA "Lost" rows |
| `SensorC2Projection` | Contact picture + per-tick indicators (radar EMCON, fire-control track, active engagements) via `ISensorC2WorldIndicators` |
| `MapPictureProjection` → `MapPanelBinder` | Tactical map symbols + ghost/frozen comms overlays |
| `MapPanelApplyState` → `MapPanelPresentation` | Headless apply seam: theater label, symbol/selection/ghost tallies, overlay counts + LOD count |
| `TacticalOverlayProjection` / `CatalogEnvelopeRangeResolver` | Selected-unit sensor/weapon envelope rings (`EnvelopeRingEntry`), catalog range → nm resolution (CMD-21/34) |
| `DatalinkUnitPairFeed` → `DatalinkPictureProjection` | Friendly unit-pair datalink mesh edges (`DatalinkEdgeEntry`, CMD-32) |
| `DoctrineMapOverlayProjection` | Per-unit ROE/source doctrine map rows (`DoctrineMapOverlayEntry`, CMD-33) |
| `MapLayerStackProjection` / `MapLayerStackState` → `MapLayerStackApplyState` | UI-local basemap layer checklist + `LAYERS: n/n` HUD (`MapLayerStackPresentation`, CMD-28.2); `MapLayerStackStore` holds visibility (not sim/DecisionLog) |
| `MapScaleProjection` | Scale bar (NM) + camera-altitude labels from camera state (`MapScaleState`, CMD-20; camera-driven, not sim) |
| `MapMeasureProjection` | Range/bearing measure-tool geometry (`MapMeasureResult`, CMD-28.4; north = `000.0°`) |
| `UnitCycleProjection` | Next/prev unit id over an ordered list, wrapping + fail-safe (CMD-28.5) |
| `App6Sidc` / `App6GlyphAtlas` / `App6AtlasCatalog` / `App6*` | APP-6/2525C glyph + SIDC + atlas resolution |
| `ContactSummaryProjection` | Single-contact inspector line |
| `CesiumBillboardProjection` | Cesium globe billboards (ADR-007 map path) |

### Force status & inspectors
| Type | Produces |
|------|----------|
| `OobTreeProjection` → `OobTreePanelBinder` | Order-of-battle tree (sorted member ids + alive state) |
| `UnitDetailProjection` → `UnitDetailPanelBinder` | Selected-unit detail pane (incl. attack menu) |
| `FacilityPictureProjection` | Facility picture + capacity/damage states |
| `MissionListProjection` → `MissionListPanelBinder` | Mission board rows |
| `FuelStateProjection` / `CommsStateProjection` | Fuel band / comms state panels |

### Combat, BDA & scoring
| Type | Produces |
|------|----------|
| `OrderLogBdaProjection` | Battle-damage-assessment contact-damage states from the log |
| `OrderLogFacilityDamageProjection` | Facility damage change records |
| `EngagePreviewProjection` / `EngageAttackOptions` / `EngageAttackOrderResolver` | Attack menu preview + order resolution |
| `LossesScoringProjection` → `LossesScoringCsvExporter` | Score tally + headless CSV export (doc 17) |

### Catalog / import surfacing (Mission Editor & data QA)
| Type | Produces |
|------|----------|
| `CatalogPlatformBrowseProjection`, `PlatformCatalog{List,Detail,Filter}Projection`, `PlatformCommsListProjection`, `CatalogLinkListProjection`, `PlatformLinkListProjection` | Catalog browsers |
| `CatalogImportProvenanceProjection`, `CatalogImportQuarantineProjection`, `MountLoadoutQuarantineProjection`, `PlatformImportStagingProjection` | Import provenance / quarantine surfacing |
| `DoctrineInheritanceProjection` → `DoctrineInheritancePanelBinder` | Doctrine inheritance panel |

### C2 chrome & shared contracts
| Type | Role |
|------|------|
| `C2TopBarProjection` / `C2PlanningChromeProjection` | Top bar + planning chrome view models |
| `C2SelectionResolver` | Default/valid selection id resolution (pure) |
| `AlertSeverity` / `AlertSeverityMap` | Alert tiering (above) |
| `OrderLifecycleState` | Player-order lifecycle enum (above) |

---

## Losses/scoring CSV export (headless AvA)

`LossesScoringProjection.Project(log, baseScore)` tallies
`score = baseScore + kills×100 − denials×5`, plus `HostileKills`, `MissilesFired`
(sum of negative magazine deltas), and `PolicyDenials`.
[`LossesScoringCsvExporter`](../../src/ProjectAegis.Delegation/Projection/LossesScoringCsvExporter.cs)
formats one CSV row per side for agent-vs-agent batch runs:

```
scenarioId,seed,side,score,kills,missilesFired,denials,fingerprint
```

The last column is the order-log `ComputeFingerprint()` — so a batch CSV doubles as a
determinism ledger: same `(scenario, seed)` ⇒ same fingerprint. Values are CSV-escaped
(quotes doubled, newlines flattened) for safe ingestion by the QA Gauntlet oracle
(see [qa-gauntlet.md](qa-gauntlet.md)).

---

## Selection state (Unity host)

Selection is presentation state, so it lives with the host, not in the log:
[`C2PresentationController`](../../src/ProjectAegis.Delegation.UnityAdapter/Presentation/C2PresentationController.cs)
(Unity adapter) holds an ordered, de-duplicated
[`SelectionSet`](../../src/ProjectAegis.Delegation.UnityAdapter/Presentation/SelectionSet.cs)
exposed read-only as `Selection` (`IReadOnlySelectionSet`), with `SelectedUnitId` as the anchor.

Mutate selection only through `SelectFriendlyUnit` / `SelectHostileContact` /
`ApplyDefaultSelection` so the coordinated side effects stay correct — in particular, moving
selection **clears stale graph-surfacing highlights** (`LastGraphHighlightIds` /
`LastGraphLinkChainDisplay`) so a bound graph panel never shows highlights for a unit that is no
longer selected (fixed under `qa-loop-08`). Graph surfacing itself
(`ApplyGraphSurfacing(catalog)`) reads only from `ICatalogReader.GetSortedDependencyEdges()` —
no `DelegationBridge`, no sim mutation ([ADR-010](../architecture/adr-010-headless-first-command-driven-ui.md), headless-first). The pure default-selection and
symbol→id resolution helpers live in the engine-agnostic
[`C2SelectionResolver`](../../src/ProjectAegis.Delegation/Projection/C2SelectionResolver.cs).

See the [adapter README — selection state](../../src/ProjectAegis.Delegation.UnityAdapter/README.md#selection-state--c2presentationcontroller).

---

## Adding a new panel

1. **Model first.** Add a `*Projection` (static class, `Project(DecisionLog …)` or
   `Build(snapshot, indicators)`) returning an immutable `record`. Fold the log with explicit
   `(SimTick/SimTime, SequenceId)` + `StringComparer.Ordinal` ordering. Read only.
2. **If it surfaces a new order-log entry kind**, add a case to `MessageLogProjection` (and a
   category to `AlertSeverityMap` if it should alert) — don't invent a parallel event store.
   Keep the default arm safe (`_ => null` / `Routine`).
3. **Bind** presentation concerns in a `*PanelBinder` → `*PanelState`. USS class strings and
   text formatting go here, never in the projection.
4. **Symbology?** Reuse `App6Sidc` / `App6GlyphAtlas`; do not hard-code glyphs in the binder.
5. **Test** in
   [`src/ProjectAegis.Delegation.Tests/Projection/`](../../src/ProjectAegis.Delegation.Tests/Projection/)
   (one `*Tests.cs` per type — 39 fixtures today). Assert on the projected model, and for
   anything determinism-sensitive, assert stable ordering and (where relevant) that the order-log
   `ComputeFingerprint()` is unchanged by projecting.
6. **Verify** with the standard block — projections are part of the solution baseline:

```bash
dotnet build ProjectAegis.sln
dotnet test  src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj -v minimal
```

## Common pitfalls

- **Mutating from a projection or binder.** The most damaging bug class: it desyncs the UI from
  the log and can perturb replay. Projections read; hosts hold selection/UI state.
- **Relying on enumeration order** of a `Dictionary`/`HashSet` in a projection — replay
  non-determinism. Always apply a total, ordinal ordering before returning.
- **Throwing on an unknown enum/category.** Follow the fail-safe defaults (`_ => null`,
  `Routine`, `FallbackSidc`) so new order-log kinds degrade gracefully.
- **Hard-coding glyphs or SIDC strings** in a binder instead of going through `App6Sidc` —
  breaks the atlas-optional contract and the affiliation table.
- **Leaving stale cross-panel state** on the host across a selection change (see the
  `qa-loop-08` graph-surfacing clear).

---

## See also

| Topic | Doc |
|-------|-----|
| Delegation core & the order log | [`src/ProjectAegis.Delegation/README.md`](../../src/ProjectAegis.Delegation/README.md) |
| Unity adapter, `C2PresentationController`, `SelectionSet` | [`src/ProjectAegis.Delegation.UnityAdapter/README.md`](../../src/ProjectAegis.Delegation.UnityAdapter/README.md) |
| Determinism rules, hashing, golden workflow | [determinism-and-replay.md](determinism-and-replay.md) |
| Abort-reason codes surfaced in the message log | [abort-reason-catalog.md](abort-reason-catalog.md) |
| Batch CSV → oracle QA loop | [qa-gauntlet.md](qa-gauntlet.md) |
| Order-log schema | [ADR-003](../architecture/adr-003-order-log-schema.md) |
| C2 map / APP-6 presentation | [ADR-007](../architecture/adr-007-c2-map-presentation.md) |
