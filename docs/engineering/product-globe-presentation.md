# Product globe presentation — view state, theater bookmarks & Cesium billboards

> **Scope.** This page documents the **pure, engine-agnostic globe presentation projection family** in
> [`ProjectAegis.Delegation/Projection/`](../../src/ProjectAegis.Delegation/Projection/) that backs the
> product 3D globe (ADR-007 Phase B; CMD-06 / CMD-13 / CMD-28.10 / CMD-28.11): the `GlobeViewState`
> camera/bookmark bag, `GlobeViewProjection` (theater presets + quick-jump + 2D/3D mode),
> `CesiumBillboardProjection` (APP-6 markers over WGS84), `GlobeMapApplyState` (status-line
> presentation), and `GlobeIonGateProjection` (token-presence tile-streaming gate). It also covers the
> thin Unity hosts that bind this core
> ([`GlobeMapProductHost`](../../unity/ProjectAegis/Assets/Scripts/Runtime/GlobeMapProductHost.cs),
> `CesiumGlobeBridge`, `GlobeTileStreamingHost`).
>
> The **projection core is a headless C# library with no `UnityEngine` and no `com.cesium.unity`
> dependency** — it runs in `dotnet test` and CI. It is a *presentation client*: nothing here mutates
> the sim, the catalog, or the `DecisionLog`, so it is **off the replay fingerprint** (ADR-010 §2–3,
> ADR-001). The other tactical read-models live in [c2-projection-layer.md](c2-projection-layer.md);
> the Cesium package spike/gate lives under the [Cesium docs](#related-references).

The C2 tactical picture can render either as the flat placeholder map or as a **product 3D globe**.
The globe half is deliberately split into (1) a pure projection core that decides *what* to show —
where the camera sits, which theater bookmarks exist, which APP-6 billboards to place, and whether
tile streaming is live — and (2) a paper-thin Unity host that only *binds* those results to UI
Toolkit labels / Cesium entities. Because the core carries no Unity or Cesium types, every rule below
is unit-tested headlessly and cannot perturb determinism.

---

## Layering

```
ProjectAegis.Delegation/Projection/           # pure, headless, no UnityEngine / no Cesium
  GlobeViewState.cs                            # GlobeCameraState, GlobeTheaterBookmark, GlobeViewState, GlobeViewMode2d3d
  GlobeViewProjection.cs                       # theater presets, quick-jump, 2D/3D mode, bookmark presentation, label
  CesiumBillboardProjection.cs                 # MapSymbolEntry → CesiumBillboardMarker (APP-6 glyph + WGS84)
  GlobeMapApplyState.cs                        # (view, markers) → GlobeMapPresentation status line
  GlobeIonGateProjection.cs                    # (hasToken, packageAvailable) → GlobeIonGateState (presence only)

unity/ProjectAegis/Assets/Scripts/Runtime/     # thin Unity hosts (bind only; #if UNITY_5_3_OR_NEWER)
  GlobeMapProductHost.cs                       # UI Toolkit status strip; no Cesium package required
  GlobeTileStreamingHost.cs                    # tile-streaming status chrome
  Cesium/CesiumGlobeBridge.cs                  # real Cesium georeference/anchors when the package is active
```

The switch between flat map and globe is the host-side `DelegationBridgeHost.UseGlobeMap` flag; when
it is off, `GlobeMapProductHost.Refresh` hides its panel and does no projection work.

---

## Camera & bookmark model — `GlobeViewState`

[`GlobeViewState`](../../src/ProjectAegis.Delegation/Projection/GlobeViewState.cs) is an immutable
`record` bag shared by the Toolkit chrome and the Cesium host:

| Type | Fields |
|------|--------|
| `GlobeCameraState` | `Latitude`, `Longitude`, `AltitudeMeters`, `HeadingDeg`, `PitchDeg` (WGS84 pose). |
| `GlobeViewMode2d3d` | `TwoD` (top-down) vs `ThreeD` (pitched) — CMD-28.10. |
| `GlobeTheaterBookmark` | `Id`, `Label`, `Camera`, `IsEmpty`. `EmptySlot(id)` returns an **honest empty** slot (not a disabled one). |
| `GlobeViewState` | `Camera`, `Bookmarks`, `ActiveBookmarkId`, `Mode2d3d`. `GlobeViewState.Empty` = origin camera, no bookmarks, 3D. |

The **honest-empty** contract (CMD-28.11) is load-bearing: an empty bookmark slot reports
`IsEmpty == true` rather than being greyed out, so the UI can prompt "no saved views yet" instead of
looking broken.

---

## View projection — `GlobeViewProjection`

[`GlobeViewProjection`](../../src/ProjectAegis.Delegation/Projection/GlobeViewProjection.cs) is a pure
static helper. All mutators return a **new** `GlobeViewState` (records + `with`); none touch the sim:

- **Theater presets.** `BalticTheater()` (default, `60.0, 24.8` — matches the billboard Baltic demo
  geo), `GiukTheater()` (`65.0, -20.0`), `PacificTheater()` (`20.0, 140.0`); `TheaterPresets` is the
  ordered `[Baltic, GIUK, Pacific]` list. `DefaultBalticTheater()` seeds a `GlobeViewState` over the
  Baltic bbox with those three as bookmarks, `ActiveBookmarkId = "baltic"`, 3D mode.
- **Quick-jump.** `WithQuickJump(state, bookmarkId)` moves the camera to the named bookmark
  (case-insensitive id match, skipping empty slots) and sets `ActiveBookmarkId`. An **unknown or blank
  id returns the input state unchanged** — quick-jump never throws or clears the view.
- **2D/3D toggle.** `WithMode(state, mode)` only changes `PitchDeg` (`-90` top-down for `TwoD`, `-45`
  for `ThreeD`) and the mode flag; lat/lon/alt/heading are preserved.
- **Bookmark presentation.** `PresentBookmarks(...)` filters out empty slots; an empty or all-empty
  list collapses to `EmptyBookmarksPresentation()` (`IsEmpty = true`, `IsDisabled = false`,
  `EmptyStateLine = "No saved views — press Ctrl+1 to save one"`).
- **Theater label.** `ResolveTheaterLabel(state)` returns the active bookmark's label (or id), else the
  first non-empty bookmark's label, else `"Globe"` — null-safe.

---

## Billboards — `CesiumBillboardProjection`

[`CesiumBillboardProjection`](../../src/ProjectAegis.Delegation/Projection/CesiumBillboardProjection.cs)
turns the tactical `MapSymbolEntry` list into `CesiumBillboardMarker`s (symbol id, affiliation, WGS84
lat/lon, APP-6 unicode glyph, USS frame id, SIDC, optional distance label):

- **APP-6 symbology.** `ResolveGlyph` prefers the entry's `App6Sidc` when present, else resolves from
  `Affiliation` + `IsDestroyed` via `App6Sidc` (the same symbology used by the flat map — see
  [c2-projection-layer.md](c2-projection-layer.md)).
- **Deterministic ordering & geo.** `Project` sorts by `Affiliation` then `SymbolId` (ordinal), so
  marker order is stable. `ResolveGeo` uses the symbol's explicit lat/lon when both are set; otherwise
  it falls back to the Baltic demo bbox — either from the entry's normalized X/Y, or a hash placement
  via `MapPictureProjection.Place(symbolId, layoutSeed)` (default `layoutSeed = 7`), so a symbol
  without coordinates still lands in a stable, reproducible spot.
- **Camera-relative LOD labels.** `ProjectWithCamera(symbols, camera, layoutSeed)` attaches a coarse
  distance label per marker using a **simple spherical haversine** (`ApproximateGroundDistanceMeters`,
  `EarthRadiusMeters = 6_371_000`) — nm at high altitude / long range, else km. This is a *readout*,
  not a nav solution.
- **Fallbacks for the Editor spike.** `ProjectSeed()` (one friendly marker) and `ProjectDemoPair()`
  (fixed Baltic friendly + hostile) let the globe show something when no runtime symbols are available.

---

## Status line — `GlobeMapApplyState`

[`GlobeMapApplyState`](../../src/ProjectAegis.Delegation/Projection/GlobeMapApplyState.cs) folds a view
state + markers into a `GlobeMapPresentation` the host binds verbatim:

```csharp
// Apply(view, markers) → StatusLine of the form:
"GLOBE · Baltic · 2 markers · 3D"
```

`Apply` composes `theater = ResolveTheaterLabel(view)`, `count = non-null markers`, and
`mode = "2D"|"3D"`, plus the bookmark presentation and camera. `ProjectAndApply(view, symbols,
layoutSeed)` is the product path: it projects symbols through `CesiumBillboardProjection.ProjectWithCamera`
first, then applies. A null view falls back to `Globe`/`0 markers`/`3D`. Hosts **never re-format** the
status line — they read `GlobeMapPresentation.StatusLine`.

---

## Tile-streaming gate — `GlobeIonGateProjection`

[`GlobeIonGateProjection`](../../src/ProjectAegis.Delegation/Projection/GlobeIonGateProjection.cs)
projects whether Cesium ion tile streaming is live, from two **presence booleans only**:

```csharp
GlobeIonGateState Project(bool hasToken, bool packageAvailable, GlobeTileStreamingConfig? config = null)
// StreamingActive = hasToken && packageAvailable
// StatusLine ∈ { "GLOBE TILES · ACTIVE",
//                "GLOBE TILES · INACTIVE · NO_ION_TOKEN",
//                "GLOBE TILES · INACTIVE · PACKAGE_MISSING" }
```

> **Security pitfall — never store the ion token.** `hasToken` is a *presence flag*; the projection
> **never accepts or returns the secret value**. The Cesium ion access token is a **user secret** and
> must come from the Editor Inspector or an env var (`CESIUM_ION_TOKEN` /
> `ProjectAegis_CesiumIonToken`) at the host layer — it is **never committed** and never flows into a
> projection, log, or fingerprint. The optional `config` is recorded for apply-state binding only and
> is ignored by the gate math (presence-only contract).

---

## Unity hosts (bind only)

- [`GlobeMapProductHost`](../../unity/ProjectAegis/Assets/Scripts/Runtime/GlobeMapProductHost.cs) — a
  UI Toolkit `MonoBehaviour` status strip. It builds a programmatic panel, and on `Refresh` reads
  `bridgeHost.LastMapSymbols`, projects them (`ProjectWithCamera`, or `ProjectDemoPair` when empty),
  calls `GlobeMapApplyState.Apply`, and binds the status/bookmark labels. `ApplyViewState` /
  `QuickJump` forward to the pure projection. It requires **no Cesium package** and is safe for CI /
  default smoke; it hides itself when `UseGlobeMap` is false.
- `CesiumGlobeBridge` — creates the real `CesiumGeoreference` / globe anchors **only when the
  `com.cesium.unity` package is active** (`#if CESIUM_FOR_UNITY`), sourcing marker positions through
  the same `CesiumBillboardProjection`. Editor-only; headless builds are unaffected.
- `GlobeTileStreamingHost` — binds `GlobeIonGateProjection` status to chrome.

---

## Determinism, replay & testing

- **Presentation-only.** Nothing here writes to the sim, catalog, order log, or world hash — theater
  quick-jump, mode toggles, and billboard placement are pure view derivations. Toggling the globe
  cannot move a replay golden or the Baltic v2 hash.
- **Deterministic outputs.** Projection order (affiliation → id), hash placement
  (`MapPictureProjection.Place`), and the LOD math are all pure functions of their inputs, so the same
  symbols + camera always produce the same markers and status line.
- **No secrets, no Unity in the core.** The projection assembly has no `UnityEngine` reference; the ion
  token is presence-only.
- **Pinned by tests:**

| Concern | Test |
|---------|------|
| Theater presets, quick-jump purity, 2D/3D pitch, bookmark honesty, geo/WGS84/LOD | `src/ProjectAegis.Delegation.Tests/Projection/GlobeViewProjectionTests.cs` |
| Status-line formatting, quick-jump status, null-view defaults, project-and-apply | `src/ProjectAegis.Delegation.Tests/Projection/GlobeMapApplyStateTests.cs` |
| Ion tile-streaming gate presence states | `src/ProjectAegis.Delegation.Tests/Projection/GlobeIonGateProjectionTests.cs` |
| Unity host binds only the pure projection (source contract) | `src/ProjectAegis.Delegation.Tests/Projection/CesiumGlobeHostSourceContractTests.cs` |

---

## How to extend

- **Add a theater preset:** add a `*Theater()` factory + id const to `GlobeViewProjection` and include
  it in `TheaterPresets`. Keep the camera a plain `GlobeCameraState`; quick-jump works automatically.
- **Change billboard placement / symbology:** edit `CesiumBillboardProjection.ResolveGeo` /
  `ResolveGlyph` — keep the ordinal sort and the WGS84-when-present rule so output stays deterministic,
  and keep APP-6 resolution routed through `App6Sidc`.
- **Add a status field:** extend `GlobeMapPresentation` + `GlobeMapApplyState.Apply`; hosts bind the
  new field without re-deriving it.
- **Never** add a `UnityEngine` / Cesium type to the projection core, and **never** pass the ion token
  value into a projection — keep the presence-only contract.

---

## Related references

| Doc | Why |
|-----|-----|
| [c2-projection-layer.md](c2-projection-layer.md) | The broader order-log → view-model read-model layer and APP-6 (`App6Sidc`) symbology this reuses. |
| [cesium-phase-b-spike-checklist.md](cesium-phase-b-spike-checklist.md) | The gate checklist for de-risking the Cesium globe before production wiring. |
| [cesium-unity-package-pin.md](cesium-unity-package-pin.md) | The pinned `com.cesium.unity` version + install notes. |
| [cesium-ion-visual-gate-2026-08-01.md](cesium-ion-visual-gate-2026-08-01.md) | The ion visual gate evidence the tile-streaming status projects. |
| [../architecture/adr-007-c2-map-presentation.md](../architecture/adr-007-c2-map-presentation.md) | The C2 map-presentation decision (globe vs flat). |
