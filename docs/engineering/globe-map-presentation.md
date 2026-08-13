# Globe map presentation — view state, theater bookmarks & billboards

The product tactical map can render on a **Cesium 3D globe** (ADR-007 Phase B/C). Per the
headless-first contract (**ADR-010**), the globe is a **client**: all of its state — camera pose,
theater bookmarks, 2D/3D mode, and the tactical markers — is produced by **pure, engine-agnostic
projections** in [`ProjectAegis.Delegation/Projection/`](../../src/ProjectAegis.Delegation/Projection/),
and the Unity hosts merely bind those read-models. Nothing here mutates the sim or the order log, so
it is fully unit-tested without opening the Editor or installing the Cesium package.

This guide covers that globe presentation stack: the headless view-state model + projections, and the
two Unity hosts that consume them. It complements the read-only [C2 projection layer](c2-projection-layer.md)
(the panels/HUD) and the Cesium spike docs
([cesium-phase-b-spike-checklist.md](cesium-phase-b-spike-checklist.md),
[cesium-unity-package-pin.md](cesium-unity-package-pin.md)).

> **Presentation boundary (ADR-010 §2–3, ADR-007).** The globe is a view. Theater quick-jump, 2D/3D
> toggle, and bookmarking are **view-only** — they never call the sim, never append to the
> `DecisionLog`, and never affect replay. Markers are a pure projection of the map symbols the bridge
> already computed. The status-strip host has **no Cesium package dependency**, so it stays CI-safe.

---

## Where it lives

### Headless projection (the source of truth)

| File | Role |
|------|------|
| [`GlobeViewState.cs`](../../src/ProjectAegis.Delegation/Projection/GlobeViewState.cs) | The view bag: `GlobeCameraState` (WGS84 lat/lon/alt + heading/pitch), `GlobeTheaterBookmark` (id/label/camera + honest `IsEmpty` slot), `GlobeViewMode2d3d` (`TwoD`/`ThreeD`), and `GlobeViewState` (camera + bookmarks + active id + mode). |
| [`GlobeViewProjection.cs`](../../src/ProjectAegis.Delegation/Projection/GlobeViewProjection.cs) | Pure view helpers: the three built-in theater presets, `WithQuickJump`, `WithMode` (2D/3D pitch), `PresentBookmarks` (empty-not-disabled), and `ResolveTheaterLabel`. |
| [`GlobeMapApplyState.cs`](../../src/ProjectAegis.Delegation/Projection/GlobeMapApplyState.cs) | `Apply` / `ProjectAndApply` → the `GlobeMapPresentation` record (status line + theater + marker count + mode + bookmarks + camera) that hosts bind verbatim. |
| [`CesiumBillboardProjection.cs`](../../src/ProjectAegis.Delegation/Projection/CesiumBillboardProjection.cs) | Map symbols → `CesiumBillboardMarker` (APP-6 glyph/frame via `App6Sidc`, WGS84 geo, optional LOD distance label). |

### Unity hosts (thin binders — `unity/ProjectAegis/Assets/Scripts/Runtime/`)

| File | Role |
|------|------|
| [`GlobeMapProductHost.cs`](../../unity/ProjectAegis/Assets/Scripts/Runtime/GlobeMapProductHost.cs) | UI-Toolkit status strip (`GLOBE …` line + bookmarks-empty label). **No Cesium dependency** — safe for CI / default smoke. Binds `GlobeMapApplyState.Apply`. |
| [`Cesium/CesiumGlobeBridge.cs`](../../unity/ProjectAegis/Assets/Scripts/Runtime/Cesium/CesiumGlobeBridge.cs) | Editor-only real Cesium foundation (georeference/anchors/billboards when the package is active). Positions from the same `CesiumBillboardProjection`. |
| `DelegationBridgeHost.UseGlobeMap` | The flag that activates the globe hosts vs the placeholder map panel; `LastMapSymbols` is the marker source. |

**Tests (headless):** [`GlobeViewProjectionTests`](../../src/ProjectAegis.Delegation.Tests/Projection/GlobeViewProjectionTests.cs)
and [`GlobeMapApplyStateTests`](../../src/ProjectAegis.Delegation.Tests/Projection/GlobeMapApplyStateTests.cs).

---

## The view-state model

`GlobeViewState` is an immutable record — every "navigation" helper returns a **new** state:

- `GlobeCameraState(Latitude, Longitude, AltitudeMeters, HeadingDeg, PitchDeg)` — WGS84 pose.
- `GlobeTheaterBookmark(Id, Label, Camera, IsEmpty)` — a saved view. `EmptySlot(id)` is an **honest
  empty slot** (`IsEmpty = true`), never a disabled control (CMD-28.11 empty-not-disabled contract).
- `GlobeViewMode2d3d` — `TwoD` (top-down) vs `ThreeD` (pitched).
- `GlobeViewState.Empty` — zeroed camera, no bookmarks, 3D.

`GlobeViewProjection.DefaultBalticTheater()` is the product default: camera over the Baltic bbox with
the three preset bookmarks and Baltic active.

### Theater presets & navigation

| Helper | Behaviour |
|--------|-----------|
| `TheaterPresets` | Built-in bookmarks: **Baltic** (`60.0, 24.8`), **GIUK** (`65.0, -20.0`), **Pacific** (`20.0, 140.0`), each pitched `-45°`. |
| `WithQuickJump(state, bookmarkId)` | Returns a new state with the camera + active id set to the matching **non-empty** bookmark; unknown/blank id returns the input unchanged (case-insensitive match). |
| `WithMode(state, mode)` | Toggles 2D/3D by setting pitch to `-90°` (2D top-down) or `-45°` (3D); lat/lon/alt/heading unchanged. |
| `PresentBookmarks(bookmarks)` | Filters out empty slots; an empty/all-empty list yields the honest empty presentation (`EmptyBookmarksLine`, **not disabled**). |
| `ResolveTheaterLabel(state)` | Active bookmark's label → else first non-empty label → else `"Globe"`. |

---

## Status presentation

`GlobeMapApplyState.Apply(view, markers)` folds the view + markers into a `GlobeMapPresentation`
whose status line has the fixed form:

```
GLOBE · Baltic · 2 markers · 3D
```

(`theater · marker-count · mode`). Null markers/view degrade gracefully (`0 markers`, `3D`, `"Globe"`).
`ProjectAndApply(view, symbols, layoutSeed)` is the product path: it first projects symbols to markers
via `CesiumBillboardProjection.ProjectWithCamera`, then applies. The Unity host binds
`presentation.StatusLine` directly — **no re-formatting on the Unity side**.

---

## Markers (billboards)

`CesiumBillboardProjection` turns the bridge's `MapSymbolEntry` list into `CesiumBillboardMarker`s:

- **Glyph/frame** — `ResolveGlyph` prefers the symbol's APP-6 `App6Sidc` when present, else resolves
  from affiliation + destroyed state (shared APP-6/2525C resolver with the [C2 projection layer](c2-projection-layer.md)).
- **Geo** — `ResolveGeo` uses the symbol's WGS84 lat/lon when both are set; otherwise it maps the
  normalized map position, or a deterministic `MapPictureProjection.Place(symbolId, seed)` hash, into
  the Baltic demo bbox — so a symbol without real geo still lands deterministically.
- **Ordering** — markers are sorted by `(Affiliation, SymbolId)` ordinal (deterministic).
- **LOD label** — `ProjectWithCamera` adds an approximate haversine ground-distance label whose units
  coarsen with camera altitude (`nm` when high/far, `km`/`0.0 km` when close). This is a display LOD
  hint, **not** a nav solution.
- **Fallbacks** — `ProjectSeed` (one friendly seed marker) and `ProjectDemoPair` (Baltic friendly +
  hostile) cover the no-symbols / Editor-spike cases.

---

## Unity hosts

`GlobeMapProductHost` is a `UIDocument`-backed MonoBehaviour that builds a small status strip
programmatically (no UXML asset required), binds `GlobeMapApplyState.Apply` each `LateUpdate`, and
hides itself when `DelegationBridgeHost.UseGlobeMap` is false. `ApplyViewState` / `QuickJump` are the
presentation-only entry points (they call the pure projection and refresh — no sim contact). It has
**no `com.cesium.unity` dependency**, so it compiles and runs in CI and the default headless smoke.

`CesiumGlobeBridge` is the Editor-only real-Cesium foundation: it creates the georeference/anchors and
places billboards from the **same** `CesiumBillboardProjection` when the Cesium package is active. The
Cesium ion token is read from the Inspector or the `CESIUM_ION_TOKEN` / `ProjectAegis_CesiumIonToken`
environment variables and is **never committed** (see the [Cesium spike checklist](cesium-phase-b-spike-checklist.md)).

> **Security.** The ion access token is a secret sourced from env/Inspector at runtime; it is not
> stored in the repo, scenes, or projections. Do not hardcode or log it.

---

## Determinism & boundary invariants

- **View-only.** Quick-jump, 2D/3D toggle, and bookmark presentation return new `GlobeViewState`
  values; none touch the sim, `DecisionLog`, or replay — the Baltic v2 hash is unaffected.
- **Pure projections.** Marker projection is deterministic (ordinal sort, seeded hash placement, pure
  haversine) with no RNG or wall-clock; the same symbols + camera always yield the same markers.
- **Honest empty, not disabled.** Empty bookmark lists render the empty-state copy, never a disabled
  control (CMD-28.11).
- **CI-safe host.** The status host works without the Cesium package; only `CesiumGlobeBridge` needs
  it, and only in the Editor.

---

## How to extend

1. **New theater preset** — add a `GlobeTheaterBookmark` factory + append to `TheaterPresets`; the
   status line and quick-jump pick it up automatically.
2. **New camera helper** — add a pure `With…` method returning a new `GlobeViewState` (keep it
   view-only); cover it in `GlobeViewProjectionTests`.
3. **New marker field** — extend `CesiumBillboardMarker` + the projection; keep it a pure function of
   `MapSymbolEntry` + camera. Bind it in the host without re-computing on the Unity side.
4. **Unity host changes** — follow the presentation-boundary rules in
   [`unity/ProjectAegis/.claude/README.md`](../../unity/ProjectAegis/.claude/README.md); the host binds
   read-models only and must not become a sim authority (ADR-010).

Verify headlessly: `dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj --filter "GlobeViewProjection|GlobeMapApplyState"`.

---

## Related references

| Where | What |
|-------|------|
| [c2-projection-layer.md](c2-projection-layer.md) | The wider read-only C2 projection layer (panels/HUD/symbology) this globe view sits alongside. |
| [cesium-phase-b-spike-checklist.md](cesium-phase-b-spike-checklist.md) · [cesium-unity-package-pin.md](cesium-unity-package-pin.md) | The Cesium globe spike gate + pinned package. |
| [ADR-007](../architecture/adr-007-c2-map-presentation.md) · [ADR-010](../architecture/adr-010-headless-first-command-driven-ui.md) | The C2 map-presentation decision + the headless-first / UI-is-a-client contract. |
