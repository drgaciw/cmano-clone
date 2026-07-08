# Command & Control UI

> **Status:** Draft — UX spec drives implementation  
> **Author:** design-system  
> **Last Updated:** 2026-07-08 (rev 2 cascade — selection set, order lifecycle, alerting)  
> **Requirements:** [20-Command-And-Control-UI.md](../../Game-Requirements/requirements/20-Command-And-Control-UI.md) (rev 2)  
> **Review:** [requirements-20-ux-review-2026-07-08.md](../../Game-Requirements/reviews/requirements-20-ux-review-2026-07-08.md)  
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

### Selection model (rev 2 — req 20 §Selection and Command Model)

- **Selection set:** ordered list of `TargetId` held in presentation controller (not sim state). Single select is a set of one.
- OOB click sets selection; ctrl-click OOB adds/removes; map click when map exists; drag-box and shift-click on map add/remove.
- **Group orders:** a context-menu action valid for the set fans out **one intent per unit** through the existing bridge command API — no new sim surface. Per-unit eligibility computed from projections and shown before commit.
- Unit cycling (`N`/`P`) advances selection within friendly OOB order.
- Message log row click (P1) sets selection + highlights `sequenceId`.

### Order lifecycle projection (rev 2)

- Order states `accepted → queued → executing → completed | denied | aborted` exposed via projection from order log records; UI displays only, never mutates.
- Weapons-release intents pass a **confirmation gate** when ROE/doctrine requires positive control (policy projection flag); Enter confirms, Esc cancels — cancel emits no intent.
- Cancelling a queued/plotted order emits a `PlayerOrderCancelled` intent via the bridge command API (logged, doc 17).

### Alerting projection (rev 2)

- Severity tiers (**Critical / Notable / Routine**) derived from `MessageLogProjection` categories per req 20 §Alerting and Interruption; mapping table lives with the projection, not scattered in UI code.
- Auto-pause issued as a command onto the existing pause-reason stack (multitasker semantics preserved); never a direct sim mutation from UI.
- Toast queue: max 3 visible; overflow collapses to a `+N` counter; toast click focuses referenced unit/`sequenceId`.

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
| Destroyed unit inside selection set (rev 2) | Dropped from set with log note; group order fans out to survivors only |
| Alert during replay (rev 2) | Toasts and auto-pause suppressed — replay is read-only |
| Toast overflow (rev 2) | 4th+ toast collapses into `+N` counter chip; click opens filtered log |
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
| TR-c2-005 | Multi-select + group orders (rev 2) |
| TR-c2-006 | Order lifecycle states in panel + log (rev 2) |
| TR-c2-007 | Severity tiers, toasts, auto-pause command (rev 2) |
| TR-c2-008 | Per-category message log filters (rev 2) |

### Implementation status (rev 2 — 2026-07-08)

| TR | Status | Notes |
|----|--------|-------|
| TR-c2-005 | **Implemented (headless)** | Drag-box/shift/ctrl multi-select, N/P cycle, group-order fan-out (one intent per eligible unit, destroyed dropped). ADR-010 preserved — fan-out over existing bridge command API. |
| TR-c2-006 | **Partial** | Lifecycle projection + `OrderStateChip` + weapons-release gate implemented; `PlayerOrderCancelled` emission deferred to Phase 2b (needs bridge cancel affordance). Positive-control uses a `WeaponsTight` proxy pending a real policy flag. |
| TR-c2-007 | **Partial** | Severity mapping, `ToastStack` (max 3 + `+N`, click-focus, replay-suppress), and the `AutoPauseCommand` value implemented; **sim auto-pause actuation deferred to Phase 2b** (no pause-reason stack in baseline). |
| TR-c2-008 | **Implemented (headless)** | Per-category log filters (`MessageLogFilterModel`), session-lifetime. |

> Landed on `c2-req20-integration` (full sln 1551/0, C2 proxy + 3 rev-2 seams, ReplayGolden green, **DelegationBridge zero-diff**, Baltic hash `17144800277401907079` unchanged). Unity host/UXML/USS verified via projection/model tests; an Editor PlayMode pass is the remaining finisher. See [requirements-traceability](../../docs/architecture/requirements-traceability.md#c2-ui-rev-2-tr-c2-005008).