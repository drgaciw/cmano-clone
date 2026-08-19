# Track B — CMO 2D kinematic map picture (2026-08-17)

**Status:** Implemented partial (CMD-38 draft; REQ-20 not appended)  
**Parent:** `docs/superpowers/specs/2026-08-17-cmd-38-kinematic-map-picture-draft.md`  
**Constraint:** ADR-010 presentation client; DelegationBridge Tick/hotpath untouched; Track C VFX layer untouched.

## Contract

| Seam | Change |
|------|--------|
| `ISimWorldSnapshot` | Additive defaults: `TryGetKinematicPose`, `GetPlottedCourse`. Existing stubs keep hash fallback. |
| `UnitKinematicPose` / `CourseWaypoint` | Core DTOs (lat/lon **or** normalized xy + course/speed). |
| `MapPictureProjection.Project` | Optional pose map. Authoritative pose places the symbol; missing pose still hashes. |
| `MapPictureProjection.ProjectCourses` | Polyline from current pose through waypoints; destroyed units emit none. |
| `MapSymbolEntry` | Optional `HasAuthoritativePose`, `CourseDeg`, `SpeedNmPerHour`. |
| `MapPictureBridge` | `Build` forwards snapshot poses; new `BuildCourses`. |
| `PlayModeKinematicMover` | Headless smoke mover (4 nm theater, 22 kt cruise, 0°=north/−Y). |

## Play Mode motion

`SimplePlayModeSimHost` seeds `u1` / `hostile-1` / `c1` at the same hash start as before, then advances course/speed each stub tick **before** `RunTick`. Icons therefore change normalized position over sim time. `plot_course` / `Move` calls `PlotCourseAhead` (lookahead polyline); `Hold` clears it and stops. `MapPlaceholderPanelHost` optionally lerps display positions between tick poses (cosmetic; `symbolLerpSeconds`). Course segments draw on `map-overlay-course-layer` — after static rings/edges, not on Track C fire-line/impact layers.

## Tests

- `MapPictureProjectionTests` — hash fallback, xy pose, lat/lon pose, course vertices, destroyed skip
- `MapSymbolPresentationLerpTests` — midpoint slide; no lerp for hash/destroyed
- `MapCanvasOverlayGeometryTests.ProjectCourseSegments_*`
- `MapPictureBridgeTests` — snapshot pose + courses
- `PlayModeKinematicMoverTests` — advance, determinism, destroyed freeze, plot/halt
- `MapCanvasCourseOverlayRendererContractTests` — layer isolation + host wiring

## Residual gaps

- BalticReplayHarness / ECS still do not publish lat/lon (hash unless a snapshot implementer opts in).
- Cesium globe world-anchor is unchanged (Phase B later).
- Plot Course destination is a deterministic lookahead, not a player-clicked waypoint list.
- Owner Game View signoff still required; this wave is headless + host wiring.
- Unity plugin DLLs must be refreshed (`./tools/copy-delegation-assemblies.sh`) for Editor Play Mode.
