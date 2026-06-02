# Command & Control UI

> **Status:** Draft — UX spec drives implementation  
> **Author:** design-system  
> **Last Updated:** 2026-06-02  
> **Requirements:** [20-Command-And-Control-UI.md](../../Game-Requirements/requirements/20-Command-And-Control-UI.md)  
> **UX Spec:** [c2-command-post.md](../ux/c2-command-post.md)  
> **Depends on:** Simulation core, sensor, engagement, delegation, order log (systems 1–10)

## Overview

Theater **command post UI**: map-first layout with persistent zones (top bar, left drawer, right detail, bottom log). Gameplay state lives in sim + delegation; UI binds projections only (ADR presentation layer).

## Player Fantasy

You command from a NATO-style C2 workstation—dense, legible, trustworthy—where agent badges and message log lines tell the same story as replay.

## Detailed Design

### Zone ownership (MVP)

| Zone | Implementation | Status |
|------|----------------|--------|
| Left drawer (OOB / missions / contacts) | `C2LeftDrawerPanelHost` | **Shipped** |
| Bottom message log | `MessageLogPanelHost` + full projection | **Shipped** |
| Sensor strip (EMCON / track / contacts) | `SensorC2PanelHost` | **Shipped** |
| Right unit detail | `RightUnitPanelHost` | **Shipped** |
| Globe map | `MapPlaceholderPanelHost` (Phase A) | **Shipped** |
| Top bar (time / compression / score) | `C2TopBarPanelHost` | **Shipped** |

### Selection model

- **Selected unit:** `TargetId` held in presentation controller (not sim state).
- OOB click sets selection; map click when map exists.
- Message log row click (P1) sets selection + highlights `sequenceId`.

### Delegation overlays (P0)

- Badge: Human | Agent | Mixed per unit (doc 04).
- Filter OOB: all / human-only / agent-only (P1).

## Formulas

N/A — presentation layer. Refresh rate = sim tick rate; panel update budget &lt; 100 ms (req 20).

## Edge Cases

| Case | Behavior |
|------|----------|
| Zero contacts | Contacts tab empty list; EMCON/track labels still update |
| Destroyed unit selected | Right panel shows DESTROYED; engage actions disabled |
| Replay mode | UI read-only; no context menu orders |
| 5000 symbols | LOD clustering (P1); MVP Baltic &lt; 50 symbols |

## Dependencies

| Upstream | Contract |
|----------|----------|
| Sensor GDD | `SensorC2Snapshot`, contact lifecycle strings |
| Order log | `MessageLogProjection` categories |
| Policy | Doctrine summary strings for right panel |
| Delegation | Agent assignment per unit |

## Tuning Knobs

| Knob | Effect |
|------|--------|
| `maxMessageLogRows` | Bottom strip height |
| `drawerWidthPx` | Left column (default 240) |
| `detailPanelWidthPx` | Right column (default 320) |

## Acceptance Criteria

1. Play Mode smoke: drawer tabs + message log + no bridge exceptions.
2. UI assemblies do not reference `ProjectAegis.Sim` engage internals—adapter projections only.
3. UX spec wireframe zones have an owner component or documented deferral.
4. Keyboard can focus OOB list and message log.

## UI Requirements

See [c2-command-post.md](../ux/c2-command-post.md) — authoritative layout and interaction map.

## TR IDs

| ID | Requirement |
|----|-------------|
| TR-c2-001 | Left drawer tabs |
| TR-c2-002 | Full message log |
| TR-c2-003 | Right unit detail |
| TR-c2-004 | Globe map P0 |