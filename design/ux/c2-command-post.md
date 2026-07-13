# UX Specification: C2 Command Post (Play Mode)

> **Status:** Draft — Sprint 4 (rev 2 cascade 2026-07-08: multi-select, order states, alerting per req 20 rev 2)  
> **Author:** ux-design / milsim review  
> **Last Updated:** 2026-07-08 (corrects prior future-dated 2026-08-03)  
> **Screen / Flow Name:** `C2CommandPost`  
> **S39-09 residual note (lean, isolated Evidence/UX track):** Cross-ref for C2/Platform deeper polish (S39-03: density, tooltips, filter surfacing). See S39-07 playtest 11 entry (playtests/README.md) + art-bible.md; proxy evidence + s37 PNGs cover PNG/playtest 11. Minimal only; no new sections. (polish-scope-boundary-2026-06-19.md + S39 qa-plan)  
> **Platform Target:** PC (keyboard/mouse primary); gamepad partial (Sprint 4+)  
> **Related GDDs:** [command-and-control-ui.md](../gdd/command-and-control-ui.md), [sensor-detection-ew.md](../gdd/sensor-detection-ew.md)  
> **Related Requirements:** [20-Command-And-Control-UI.md](../../Game-Requirements/requirements/20-Command-And-Control-UI.md)  
> **Accessibility Tier:** Standard (colorblind-safe affiliation, keyboard OOB/log focus)

> **Implementation status (2026-06-02):** Bottom message log, tabbed left drawer (OOB / missions / contacts), sensor strip — **implemented** via UI Toolkit hosts. Globe map + right unit panel — **this spec**.

---

## 1. Purpose & Player Need

**Player need:** Run a theater fight for hours without drowning in noise—see who owns what, why fires were denied, and what contacts mean, at a glance.

**Player goal:** Select a unit or contact, read effective status and doctrine in under 2 seconds, and issue or delegate the next action without leaving the map mental model.

**Game goal:** Surface deterministic sim state (order log projections, policy, contacts) without UI owning gameplay state; every command becomes a logged intent.

---

## 2. Player Context on Arrival

| Question | Answer |
|----------|--------|
| Just doing? | Planning (RTwP) or executing with time compression |
| Emotional state | Analytical stress—high information load |
| Cognitive load | High—tracking multiple tracks and timelines |
| Already knows | Side, ROE posture, active missions from planning |
| Likely trying to do | Check EMCON/track, confirm kill, assign mission, delegate agent |
| Afraid of | Missing a contact transition, silent policy denial, agent runaway |

**Emotional target:** Calm command authority—dense data, clear hierarchy, no surprise state changes without log evidence.

---

## 3. Navigation Position

```
Main Menu → Scenario Select → Mission Planning (RTwP) → [C2 Command Post] → AAR / Replay
```

**Modal behavior:** Overlay-live — sim runs behind panels; pause freezes tick (doc 03).

| Entry | Trigger |
|-------|---------|
| Primary | Begin Execution from planning |
| Secondary | Load mid-scenario save (future) |
| Deep link | Replay scrub jumps map + log selection (future) |

---

## 4. Layout Specification (1920×1080 baseline)

### 4.1 Wireframe

```
┌──────────────────────────────────────────────────────────────────────────────┐
│ TIME 00:42:10  │  ▶ 1x 4x 8x  │  SIDE: BLUE  │  MODE: Mixed  │  ⏸ PAUSE   │  TOP BAR
├──────────┬───────────────────────────────────────────────────────┬───────────┤
│ LEFT     │                                                       │ RIGHT     │
│ DRAWER   │              GLOBE / MAP (PRIMARY)                    │ UNIT      │
│ [OOB]    │         units · contacts · mission areas              │ DETAIL    │
│ [MISS]   │         delegation badges · EMCON overlay (P1)        │ PANEL     │
│ [CONT]   │                                                       │           │
├──────────┴───────────────────────────────────────────────────────┴───────────┤
│ MESSAGE LOG (full AAR categories, scroll, click → select unit / sequenceId)   │
└──────────────────────────────────────────────────────────────────────────────┘
```

### 4.2 Zone definitions

| Zone | Size | MVP | Data source |
|------|------|-----|-------------|
| Top bar | 100% × 48px | **Done** | `C2TopBarProjection` (sim time, compression, `SimulationModeProfile`) |
| Left drawer | 240px × flex | **Done** (Toolkit) | `OobTreeBridge`, `MissionListBridge`, `SensorC2Bridge` |
| Map | center flex | **Done** (placeholder) | `MapPictureProjection` |
| Right panel | 320px × flex | **Done** | `UnitDetailProjection` |
| Bottom log | 100% × 120px | **Done** (full categories) | `MessageLogBridge.ProjectFrom` |

### 4.3 Component inventory (delta — not yet built)

| Component | Type | Zone | MVP |
|-----------|------|------|-----|
| GlobeView | Map | Map | P0 stub → Cesium/URP globe Sprint 5 |
| UnitDetailPanel | Panel | Right | P0 read-only |
| DelegationBadge | Overlay | Map | P0 icon on unit |
| TimeCompressionMenu | Menu | Top | P0 presets from doc 03 |
| MessageLogRowSelect | List row | Bottom | P1 → focus unit |
| SelectionBox *(rev 2)* | Map overlay | Map | P0 drag-box multi-select |
| OrderStateChip *(rev 2)* | Chip | Right | P0 lifecycle state per order (`accepted…aborted`) |
| ToastStack *(rev 2)* | Overlay | Map top-right | P0 max 3 + `+N` overflow; click focuses unit/`sequenceId` |
| LegendOverlay *(rev 2)* | Overlay | Map | P1 symbology + COMMS legend toggle |

---

## 5. States & Variants

| State | Trigger | Visual | Behavior |
|-------|---------|--------|----------|
| Planning | Before Begin Execution | Map dimmed; drawer read-only | No engage intents |
| Running | Sim ticking | All zones live | Projections refresh per tick |
| Paused | Pause hotkey | Top bar PAUSE lit | Tick frozen; log still scrollable |
| No selection | No unit picked | Right panel placeholder | "Select a unit on map or OOB" |
| Unit selected | OOB row / map pick | Right panel populated | Doctrine, magazine, EMCON, sensors summary |
| Multi-selected *(rev 2)* | Drag-box / shift-click / ctrl-click OOB | Right panel shows group summary (count, domains, worst fuel/magazine) | Context actions show per-unit eligibility before commit |
| Replay | Replay mode | Time scrubber (future) | Read-only commands; toasts + auto-pause suppressed |

---

## 6. Interaction Map (MVP)

| Input | Action | Notes |
|-------|--------|-------|
| Click OOB row | Select friendly unit | Drives right panel + map focus (map when built) |
| Tab toolbar | Switch OOB / missions / contacts | Implemented in `C2LeftDrawerPanelHost` |
| Click message log row | Select `sequenceId` / unit | P1 — explain denial tooltip |
| Space | Pause / resume | Doc 03 |
| `1–4` | Time compression preset | Doc 03 |
| Esc | Close modal / cancel intent preview | Assisted mode |
| Drag-box on map *(rev 2)* | Multi-select units | `SelectionBox`; shift-click add/remove |
| Ctrl+click OOB row *(rev 2)* | Add/remove from selection set | Mirrors map multi-select |
| `N` / `P` *(rev 2)* | Cycle next/previous friendly unit | Remap stub `input.cycle_unit` |
| `F` *(rev 2)* | Center on primary hostile | Remap stub `input.focus_primary_threat` (accessibility §6.3) |
| Enter / Esc *(rev 2)* | Confirm / cancel weapons-release gate | ROE positive-control intents only |
| Log filter toggles *(rev 2)* | Show/hide category | Per doc 17 categories; persists per session |

**Gamepad:** Deferred to Sprint 5; focus order: top bar → left drawer → log → right panel.

---

## 7. Data Requirements

| Data | Source | Update | UI rule |
|------|--------|--------|---------|
| Contacts | `SensorC2Snapshot` | Per tick | Display only |
| OOB | `OobTreeBridge` | Per tick | Display only |
| Missions | Scenario `MissionTimeline` | Static + runtime emissions in log | List static; highlight fired events in log |
| Message log | `MessageLogProjection` | Per tick | Display only |
| Unit detail | `UnitDetailProjection` | On selection | Display only |
| Orders | Command events | On player action | Fire `PlayerOrderRecord` — never mutate sim from UI |

---

## 8. Events Fired

| Action | Event | Receiver |
|--------|-------|----------|
| Engage (context menu) | `PlayerOrder` / engage intent | Delegation bridge |
| Pause agent | `AgentPauseRequested` | Orchestrator (future C5) |
| Time compression change | `ModeChangeRecord` | Order log |
| Doctrine edit (P1) | Policy snapshot change | Policy evaluator |
| Cancel queued/plotted order *(rev 2)* | `PlayerOrderCancelled` | Bridge command API → order log |
| Critical-tier event *(rev 2)* | `AutoPauseRequested` (pause-reason stack) | Sim control — command, not mutation |

---

## 9. Accessibility (screen-level)

- Affiliation: shape + color (NATO-style frame, not color-only)
- Message log: scalable font; max 12 visible rows with scroll (configurable)
- Focus: left drawer ListView keyboard navigable (Unity 6 default)
- Reduced motion: instant tab switch, no map pan inertia
- Toasts *(rev 2)*: AA text contrast on toast surface; appear/dismiss without slide/scale under `.reduced-motion`; never color-only severity (icon + label)

---

## 10. Acceptance Criteria (Sprint 4–5)

1. Left drawer three tabs show live data in Play Mode with `baltic-patrol` / `baltic-patrol-mission`.
2. Message log shows non-combat categories (CONTACT, POLICY_DENIAL) when scenario produces them.
3. Right panel shows unit id, alive/destroyed, magazine summary, EMCON line for selected `u1`.
4. No MonoBehaviour writes to `DecisionLog` except via bridge command API.
5. Map zone may be placeholder (grid + unit dots) until globe ships — right panel must work without map.
6. *(rev 2)* Drag-box selects ≥ 2 units; group engage issues one logged intent per eligible unit with pre-commit eligibility list (req 20 AC-7).
7. *(rev 2)* Queued order shows lifecycle chip in right panel; cancel emits logged `PlayerOrderCancelled` (req 20 AC-8).
8. *(rev 2)* Critical event with auto-pause on: sim pauses via pause-reason stack; toast click focuses source unit (req 20 AC-9).

---

## 11. Open Questions

| Question | Owner | Resolution |
|----------|-------|------------|
| Globe: Cesium vs custom URP terrain? | Engineering + TA | ADR pending |
| Single UIDocument vs multi-document layout? | UI | Current: multi-host; consolidate Sprint 5 |
| Player journey map missing | Design | Create `design/player-journey.md` |