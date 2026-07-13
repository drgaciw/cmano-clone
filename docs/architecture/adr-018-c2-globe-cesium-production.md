# ADR-018: C2 Globe Production Path — Cesium for Unity

**Status:** Accepted  
**Date:** 2026-07-09  
**Supersedes (partial):** open question 3 in `Game-Requirements/requirements/20-Command-And-Control-UI.md`  
**Related:** ADR-007 (phased map), ADR-010 (headless-first UI)

## Context

Req 20 P0 requires a WGS84 globe (pan/zoom/rotate, theater jump, unit/contact symbols, pick + drag-box).  
ADR-007 Phase A shipped the UI Toolkit map placeholder; Phase B spike (Cesium) is evidence-green (S20/S24 checklists, `useGlobeMap` default **false** for CI).

User decision **D1** (2026-07-09, Req 20 P0 completion program): **Cesium production** for TR-c2-004.

## Decision

1. **Production globe stack = Cesium for Unity** (pinned per `docs/engineering/cesium-unity-package-pin.md`).
2. **CI-safe default remains** `useGlobeMap=false` → MapPlaceholder + headless projections.
3. **T1** promotes spike hosts into a production presentation path: symbols from existing map/APP-6 projections; selection via `C2PresentationController` / `SelectionSet`; drag-box parity with placeholder; theater quick-jump.
4. Map UI **never** mutates sim state (ADR-010).

## Alternatives

| Option | Rejected because |
|--------|------------------|
| Custom URP terrain globe | Higher cost; no spike evidence; weaker geospatial ecosystem |
| Placeholder-only forever | Fails req 20 P0 globe parity |
| Always-on Cesium in CI | Breaks headless/Editor-less gates |

## Consequences

- T1 owns globe assembly/host files; may merge independently when green.
- GitNexus re-index after globe assembly lands.
- Doc 20 open question 3 → **Resolved: Cesium**.

## Headless contracts (T1 / TR-c2-004)

| Contract | Location | Role |
|----------|----------|------|
| Theater catalog + quick-jump | `TheaterQuickJump` / `GeographicBounds` | Named theaters (Baltic bbox + GIUK / EastMed / PersianGulf); scenario aliases (`baltic-*` → Baltic) |
| NDC ↔ WGS84 | `GlobeCoordinateMapper` | Maps lat/lon into placeholder NDC (`NormalizedX` = lon fraction, `NormalizedY` = lat fraction within theater bounds) |
| Globe pick | `GlobePickResolver` | Nearest live friendly within NDC hit radius → `C2PresentationController` / `SelectionSet` |
| Drag-box parity | `GlobeCoordinateMapper.ToNormalizedRect` + `SelectionBoxResolver` | Two surface lat/lon corners → `NormalizedRect` → same multi-select path as MapPlaceholder |
| Host adapter | `IGlobeMapSurface` / `HeadlessGlobeMapSurface` | Thin surface for Cesium hosts; CI keeps `UseGlobeMap=false` |

**Mapping (authoritative):**  
`lon = LonMin + NormalizedX * LonSpan`, `lat = LatMin + NormalizedY * LatSpan`  
(inverse of `CesiumBillboardProjection` Baltic placement; Baltic bounds = lat [59.5, 60.5], lon [24.0, 25.5]).

## GDD / TR

- TR-c2-004 Globe map P0  
- `design/gdd/command-and-control-ui.md`, `design/ux/c2-map-placeholder.md` (fallback)
