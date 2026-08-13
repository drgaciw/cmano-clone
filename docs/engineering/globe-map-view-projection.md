# Globe map view projection — theater view-state & Cesium billboards

> **Scope.** The pure, headless **product-globe presentation** subsystem in
> `ProjectAegis.Delegation/Projection/`: the view-state bag a globe host binds to
> (`GlobeViewState` — camera, theater bookmarks, quick-jump, 2D/3D mode), the theater/bookmark
> helpers (`GlobeViewProjection`), the tactical-symbol → globe-marker projection
> (`CesiumBillboardProjection`), and the status apply-state (`GlobeMapApplyState` →
> `GlobeMapPresentation`). It is the globe-specific slice of the read side documented generally in
> [`c2-projection-layer.md`](c2-projection-layer.md) (which lists `CesiumBillboardProjection` in
> its catalog but does not deep-dive the view-state model). The **ion / tile-streaming gate** side
> (`GlobeIonGateProjection`, `GlobeTileStreamingConfig`, `GlobeTileStreamingApplyState`) is a
> sibling documented in [`cesium-ion-visual-gate-2026-08-01.md`](cesium-ion-visual-gate-2026-08-01.md)
> — this page links it rather than repeating it.
>
> Boundary rationale: [ADR-007 (C2 map presentation)](../architecture/adr-007-c2-map-presentation.md),
> [ADR-010 (headless-first, command-driven UI)](../architecture/adr-010-headless-first-command-driven-ui.md).
> Everything here is **presentation-only** and lives in the engine-agnostic `ProjectAegis.Delegation`
> assembly — **no Cesium package dependency, no `UnityEngine`** — so it runs and is pinned under plain
> `dotnet test`. The Unity consumer is
> [`GlobeMapProductHost`](../../unity/ProjectAegis/Assets/Scripts/Runtime/GlobeMapProductHost.cs).

---

## Where it lives

All in [`src/ProjectAegis.Delegation/Projection/`](../../src/ProjectAegis.Delegation/Projection/):

| Type | Kind | Role |
|------|------|------|
| `GlobeCameraState` | `sealed record` | WGS84 camera pose: `(Latitude, Longitude, AltitudeMeters, HeadingDeg, PitchDeg)`. |
| `GlobeTheaterBookmark` | `sealed record` | `(Id, Label, Camera, IsEmpty)` named theater / operator bookmark; `EmptySlot(id)` is an **honest empty** slot (not disabled). |
| `GlobeViewMode2d3d` | `enum` | `TwoD = 0` (top-down) / `ThreeD = 1` (pitched). |
| `GlobeViewState` | `sealed record` | The view bag: `(Camera, Bookmarks, ActiveBookmarkId, Mode2d3d)` + `Empty`. |
| `GlobeViewProjection` | `static` | Theater presets, `WithQuickJump`, `WithMode`, bookmark presentation, theater-label resolution. |
| `GlobeBookmarksPresentation` | `sealed record` | `(Bookmarks, IsEmpty, EmptyStateLine, IsDisabled)` for chrome bind. |
| `CesiumBillboardMarker` | `sealed record` | One globe marker: `(SymbolId, Affiliation, Latitude, Longitude, UnicodeGlyph, UssFrameId, Sidc, DistanceLabel?)`. |
| `CesiumBillboardProjection` | `static` | Tactical `MapSymbolEntry` list → `CesiumBillboardMarker` list (APP-6 glyphs + WGS84 geo + optional LOD distance). |
| `GlobeMapApplyState` | `static` | Fold view + markers into the bindable `GlobeMapPresentation` (status line, theater, mode, bookmarks). |
| `GlobeMapPresentation` | `sealed record` | Applied fields for the status strip + `Empty`. |

---

## The view-state model

`GlobeViewState` is a plain immutable bag shared by the UI-Toolkit status chrome and (when the
package is present) a Cesium host. All transitions are pure record `with`-copies — a "quick-jump"
or mode toggle returns a **new** state and never mutates the sim or the input.

`GlobeViewProjection` supplies the product defaults and transitions:

| Member | Behaviour |
|--------|-----------|
| `TheaterPresets` | Three built-in theaters: **Baltic** (`60.0, 24.8`, alt 1,000,000 m), **GIUK** (`65.0, -20.0`, alt 1,500,000 m), **Pacific** (`20.0, 140.0`, alt 2,000,000 m), each pitched `-45°`. |
| `DefaultBalticTheater()` | Product default state: camera over the Baltic bbox, the three presets as bookmarks, active `baltic`, `ThreeD`. |
| `WithQuickJump(state, bookmarkId)` | Returns a copy whose camera + `ActiveBookmarkId` match the bookmark (case-insensitive id). **Unknown / blank / empty-slot ids return the input unchanged** — jump is a no-op, never an error. |
| `WithMode(state, mode)` | Toggles 2D↔3D by setting pitch only: `TwoD → -90°` (top-down), `ThreeD → -45°`. Lat/lon/alt/heading are preserved. |
| `PresentBookmarks(bookmarks)` / `EmptyBookmarksPresentation()` | Chrome-ready bookmark list. An empty list **or** an all-empty-slot list collapses to the **honest empty** presentation (`IsEmpty = true`, `IsDisabled = false`, copy `"No saved views — press Ctrl+1 to save one"`) — empty is *not* disabled (CMD-28.11). |
| `ResolveTheaterLabel(state)` | Active bookmark's label → else first non-empty labelled bookmark → else `"Globe"`. |

> **Honest-empty contract (CMD-28.11).** An empty bookmarks list is a real, enabled state with an
> explanatory line, never a greyed-out control. `GlobeTheaterBookmark.EmptySlot` and the
> `IsDisabled = false` flag make this explicit so a bound panel can't misrepresent "no saved views"
> as "feature unavailable".

---

## `CesiumBillboardProjection` — symbols → globe markers

Turns the tactical `MapSymbolEntry` list (the same rows behind the 2D map picture) into APP-6
billboard markers:

- **Deterministic order.** Symbols are sorted by `Affiliation` then `SymbolId`, both
  `StringComparer.Ordinal` — never dictionary/hash enumeration order.
- **Glyph/frame.** `ResolveGlyph` uses the symbol's `App6Sidc` when present, else resolves from
  `Affiliation` + `IsDestroyed` via `App6Sidc` (the same atlas-optional APP-6 resolver as the 2D
  map; see [`c2-projection-layer.md`](c2-projection-layer.md)).
- **Geo resolution (`ResolveGeo`).** Prefer the symbol's optional WGS84 `Latitude`/`Longitude` when
  **both** are set; else map `NormalizedX`/`NormalizedY` into the Baltic demo bbox
  (lat `59.5–60.5`, lon `24.0–25.5`); else fall back to a **deterministic hash placement**
  (`MapPictureProjection.Place(symbolId, layoutSeed)`) into that bbox. Same seed → same layout.
- **Camera LOD (`ProjectWithCamera`).** Attaches a `DistanceLabel` per marker from a spherical
  (haversine) ground distance to the camera (`EarthRadiusMeters = 6_371_000`), formatted by a
  simple LOD rule: `nm` when the camera is high (≥1,500,000 m alt) or the target is far (≥500,000 m),
  otherwise `km`. This is a display readout, **not** a navigation solution.
- **Fallbacks.** `ProjectSeed()` returns a single friendly seed marker when there are no symbols;
  `ProjectDemoPair()` is a fixed Baltic friendly+hostile pair for an Editor spike when runtime
  symbols aren't available.

---

## `GlobeMapApplyState` — the bindable status

`Apply(view, markers)` folds the view + projected markers into `GlobeMapPresentation`, whose
`StatusLine` has the fixed form:

```text
GLOBE · Baltic · 2 markers · 3D
        theater   count       mode(2D/3D)
```

A `null` view resolves to `"Globe"` / `3D` / honest-empty bookmarks. `ProjectAndApply(view,
symbols, layoutSeed = 7)` is the one-call product path: it runs
`CesiumBillboardProjection.ProjectWithCamera(symbols, view.Camera, layoutSeed)` and then `Apply`,
so a host can refresh straight from the tactical symbol list.

---

## Consumer — `GlobeMapProductHost` (Unity)

[`GlobeMapProductHost`](../../unity/ProjectAegis/Assets/Scripts/Runtime/GlobeMapProductHost.cs) is
a UI-Toolkit `MonoBehaviour` that binds this subsystem. It is **pure chrome — no Cesium package
dependency** — and is active only when `DelegationBridgeHost.UseGlobeMap` is true. Each
`LateUpdate` it:

1. reads the host's `LastMapSymbols`,
2. projects them with `CesiumBillboardProjection.ProjectWithCamera(..., ViewState.Camera)` (or
   `ProjectDemoPair()` when there are none),
3. calls `GlobeMapApplyState.Apply(ViewState, markers)` and binds `StatusLine` + the honest-empty
   bookmarks line.

`ApplyViewState` / `QuickJump` mutate only the host's presentation `GlobeViewState` (via
`GlobeViewProjection.WithQuickJump`) — never the sim.

---

## Determinism & invariants

- **Presentation-only.** Nothing here reads or writes the `DecisionLog` or sim state — inputs are a
  view-state bag + a `MapSymbolEntry` list. It cannot perturb the replay hash
  (`17144800277401907079`). *(Presentation boundary cites ADR-007 / ADR-010 — not ADR-018.)*
- **No package / engine dependency.** The projections have no Cesium and no `UnityEngine` reference,
  so they are CI-safe and headless-testable; the Cesium package is only needed by an actual globe
  host, gated separately (see the [ion visual gate](cesium-ion-visual-gate-2026-08-01.md)).
- **Deterministic.** Ordinal symbol sort + `MapPictureProjection.Place` hash placement + a single
  `layoutSeed` → identical output for identical inputs. No RNG, no wall-clock.
- **Pure transitions.** `WithQuickJump` / `WithMode` return record copies; an unknown quick-jump id
  is a no-op returning the same state.
- **No secrets.** The ion token is a **presence flag only** on the sibling gate types — token
  values are never stored on any of these records (see the ion-gate doc).

---

## Extend it

| You want to… | Do this |
|--------------|---------|
| Add a theater preset | Add a `GlobeTheaterBookmark` constant + factory in `GlobeViewProjection` and include it in `TheaterPresets`; add a `WithQuickJump` assertion. |
| Change the status-line format | Edit `GlobeMapApplyState.Apply` only (the presentation string lives there, not in the projections). |
| Feed real WGS84 positions | Populate `MapSymbolEntry.Latitude`/`Longitude` upstream; `ResolveGeo` prefers them over the Baltic hash placement automatically. |
| Wire tile streaming / an ion token | Do **not** extend these types — use the [ion visual gate](cesium-ion-visual-gate-2026-08-01.md) contracts (`GlobeIonGateProjection` / `GlobeTileStreamingConfig`). Keep token values out of the repo. |

---

## See also

| Doc | For |
|-----|-----|
| [c2-projection-layer.md](c2-projection-layer.md) | The general read-model layer + APP-6 glyph resolution these markers reuse. |
| [cesium-ion-visual-gate-2026-08-01.md](cesium-ion-visual-gate-2026-08-01.md) | The sibling ion-token / tile-streaming presence gate. |
| [cesium-phase-b-spike-checklist.md](cesium-phase-b-spike-checklist.md) · [cesium-unity-package-pin.md](cesium-unity-package-pin.md) | ADR-007 Cesium spike gate + package pin. |
| [determinism-and-replay.md](determinism-and-replay.md) | Why the read path must stay pure. |

## Tests

`src/ProjectAegis.Delegation.Tests/Projection/` (NUnit), part of the solution baseline:

| Test | Pins |
|------|------|
| `GlobeViewProjectionTests.DefaultBalticTheater_camera_over_baltic_bbox_with_3d_mode` | Default state: Baltic camera, `ThreeD`, active `baltic`, label `"Baltic"`. |
| `GlobeViewProjectionTests.Theater_presets_include_Baltic_GIUK_Pacific` | The three presets + their camera coords. |
| `GlobeViewProjectionTests.WithQuickJump_moves_camera_to_bookmark_without_mutating_input` | Jump copies camera/active id; input record unchanged. |
| `GlobeViewProjectionTests.WithQuickJump_unknown_id_returns_same_state` | Unknown id is a no-op. |
| `GlobeViewProjectionTests.WithMode_two_d_sets_top_down_pitch` | `TwoD → -90°`, lat/lon preserved. |
| `GlobeViewProjectionTests.EmptyBookmarksPresentation_is_honest_empty_not_disabled` / `PresentBookmarks_*` | Honest-empty (not disabled) bookmark presentation. |
| `GlobeViewProjectionTests.Project_uses_explicit_wgs84_when_symbol_has_lat_lon` / `Project_falls_back_to_baltic_hash_without_lat_lon` | Geo resolution precedence. |
| `GlobeViewProjectionTests.ProjectWithCamera_attaches_distance_labels` | LOD distance labels attached. |
| `GlobeMapApplyStateTests.Apply_formats_status_line_globe_theater_markers_mode` / `Apply_after_quick_jump_to_pacific_updates_status_theater` / `Apply_two_d_mode_label` / `Apply_null_view_uses_globe_defaults` / `ProjectAndApply_projects_symbols_and_formats_status` / `Quick_jump_does_not_require_sim_and_is_pure` | Status-line fold + purity. |

> The ion-gate / tile-streaming contracts are pinned separately by `GlobeIonGateProjectionTests`
> and `GlobeTileStreamingConfigTests` — see the [ion visual gate](cesium-ion-visual-gate-2026-08-01.md).
