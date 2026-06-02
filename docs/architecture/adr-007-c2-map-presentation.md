# ADR-007: C2 Map Presentation (Phased Globe)

**Status:** Accepted  
**Date:** 2026-06-02

## Context

Doc 20 requires a WGS84 globe with thousands of symbols. Unity 6.3 LTS is pinned; sim positions will come from ECS/DOTS later. UI Toolkit panels (left drawer, message log, unit detail) already bind headless **projections** without owning game state.

## Decision

**Phase A (MVP / Sprint 5):** UI Toolkit **tactical map placeholder** — normalized `(x,y)` symbols from `MapPictureProjection` (deterministic hash placement until sim publishes lat/lon).

**Phase B (Production):** Integrate **Cesium for Unity** or Unity Geo sync for WGS84 globe; symbols become world-anchored entities fed by the same projection DTOs.

**Phase C:** APP-6 / NATO symbology library as data-driven USS + icon atlas; LOD clustering per doc 20.

Presentation rules:

- Map UI never writes to `DecisionLog` or sim world.
- Symbol list is a per-tick projection: `MapPictureBridge.Build(snapshot, registry, log, seed)`.
- Globe and placeholder share `MapSymbolEntry` contract.

## Alternatives considered

| Option | Rejected because |
|--------|------------------|
| UGUI for map only | Split stack; Toolkit already used for C2 |
| Immediate full Cesium | Blocks sprint; no lat/lon on wire yet |
| OnGUI tactical map | Legacy; inconsistent with Sprint 2 Toolkit hosts |

## Consequences

- Add `MapPictureProjection` tests; golden layout stable per seed.
- ADR does not change replay hashes (read-only projection).
- Future ADR may pin Cesium package version when Phase B starts.

## Engine compatibility

| Item | Unity 6.3 LTS |
|------|----------------|
| UI Toolkit absolute positioning | Supported (`style.left` Length.Percent) |
| Cesium for Unity | Evaluate in Phase B against Dedicated Server build |

## GDD requirements addressed

- [command-and-control-ui.md](../../design/gdd/command-and-control-ui.md) — map zone P0
- [c2-command-post.md](../../design/ux/c2-command-post.md) — layout zones