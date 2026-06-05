# Sprint 4 — C2 map prep & milestone close

> **Dates:** 2026-06-18 → 2026-07-02 (proposed)  
> **Goal:** Close MVP milestone gates; start doc-20 globe/map UX spec (no globe implementation yet).

## Prerequisites

- [x] Sprint 3 complete — left drawer projections + tab host
- [x] Vertical slice gate **PROCEED** ([gate-2026-06-02](../vertical-slice/gate-2026-06-02.md))

## Committed (2026-06-02)

| Item | Status |
|------|--------|
| UX spec [c2-command-post.md](../../design/ux/c2-command-post.md) | **Done** |
| GDD [combat-domains-damage.md](../../design/gdd/combat-domains-damage.md) | **Done** |
| GDD [command-and-control-ui.md](../../design/gdd/command-and-control-ui.md) | **Done** |
| `UnitDetailProjection` + `RightUnitPanelHost` | **Done** |

## Planned work

| Item | Owner | Gate |
|------|-------|------|
| Globe map placeholder / Cesium ADR | Engineering | Sprint 5 |
| Unity manual QA (drawer + right panel) | QA | PLAYMODE-SMOKE signed |
| `/design-system` scoring & losses (system 17) | Design | Next GDD |
| C5 player override story (ADR-001) | Engineering | Story ready |
| Milestone review prep | Producer | 2026-07-15 |

## Definition of done

- UX spec for map zone exists under `design/ux/`
- 0 test regressions; replay PASS
- Milestone checklist updated in [vertical-slice-mvp](../milestones/vertical-slice-mvp.md)