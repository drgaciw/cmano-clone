# UX Specification: C2 Tactical Map Placeholder

> **Status:** Draft — Sprint 5 (comms P1 updated 2026-06-02)  
> **Last Updated:** 2026-06-02  
> **Parent:** [c2-command-post.md](c2-command-post.md)  
> **ADR:** [adr-007-c2-map-presentation.md](../../docs/architecture/adr-007-c2-map-presentation.md)

## Purpose

Ship a **readable theater board** before WGS84 globe: friendly units, hostile contacts, and selection affordances at CMANO-like density budgets, without blocking on ECS world positions.

## Player need

Maintain spatial situational awareness—who is where relative to the fight—while the engine still runs headless-first. Under comms degradation, the player must **see** that the picture is stale—not just read the top bar.

## Layout (map zone only)

```
┌─────────────────────────────────────┐
│ [GRID]  lat/long ticks (decorative) │
│   ■ u1        ◆ c1 HOSTILE          │
│      ◌ ghost (lag)                  │
│  theater label (scenario id)        │
└─────────────────────────────────────┘
```

## Symbology (MVP placeholder)

| Affiliation | Shape | Color (USS) | Label |
|-------------|-------|-------------|-------|
| Friendly | Filled square `■` | `#4a9eff` | `unitId` |
| Hostile contact | Diamond `◆` | `#e85d5d` | `contactId` + state |
| Selected | 2px outline ring | `#f0c040` | — |
| Destroyed friendly | Dim + strikethrough | 40% opacity | — |

### Comms degradation (P1 — implemented)

| COMMS state | Hostile track | Friendly track | USS classes |
|-------------|---------------|----------------|-------------|
| Nominal | Full brightness | Full brightness | base affiliation |
| Degraded | Faded live ◆ + **ghost** ◌ at lag offset | Live only (full) | `map-symbol--stale` + `map-symbol--ghost` |
| Denied | All symbols frozen/dim | All symbols frozen/dim | `map-symbol--frozen` |

**Ghost affordance:** Offset normalized position (`commsDisplay.ghostOffsetX/Y`), italic label suffix `(lag N)`, non-clickable (`pickingMode: Ignore`). Shape matches hostile ◆ so color is not the only cue.

**Accessibility:** Shape differs by affiliation; comms uses opacity + ghost duplicate—not color-only.

**P2:** APP-6 icon atlas; globe swap per ADR-007 Phase B.

## Interaction (MVP)

| Input | Action |
|-------|--------|
| Click symbol | Select unit/contact; drives right panel + OOB highlight |
| Click ghost | No selection (stale hint only) |
| Hover | Tooltip: id, lifecycle state, last log category (P2) |
| Pan/zoom | Deferred Phase B (globe); placeholder fixed 0–1 normalized space |
| `F` | Center on primary hostile (future) |

## Data

| Field | Source |
|-------|--------|
| Symbol positions | `MapPictureProjection` (hash placement until sim coords) |
| COMMS styling | `CommsStateProjection` + `MapPanelBinder` + scenario `commsDisplay` |
| Labels | OOB + contact picture |
| Theater name | `scenarioPolicyId` on bridge host |

## Acceptance

1. Baltic Play Mode shows ≥1 friendly and ≥1 contact symbol when scenario has contacts.
2. Same seed → same symbol positions (unit test).
3. `baltic-patrol-comms`: degraded shows ghost row; denied dims all symbols.
4. Map host does not reference engage resolver or SQLite.
5. Reduced motion: no pan inertia; selection ring instant.

## Open questions

| Question | Owner |
|----------|-------|
| Cesium vs Mapbox vs custom mesh globe? | Engineering — Phase B spike (`useGlobeMap` flag) |
| When to swap hash placement for sim `WorldPosition`? | Sim + UI — ECS milestone |