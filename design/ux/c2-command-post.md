# UX Specification: C2 Command Post (Play Mode)

> **Status:** Draft — Sprint 4  
> **Author:** ux-design / milsim review  
> **Last Updated:** 2026-06-02  
> **Screen / Flow Name:** `C2CommandPost`  
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
| Top bar | 100% × 48px | P0 | Sim time, compression, `SimulationModeProfile` |
| Left drawer | 240px × flex | **Done** (Toolkit) | `OobTreeBridge`, `MissionListBridge`, `SensorC2Bridge` |
| Map | center flex | **Done** (placeholder) | `MapPictureProjection` |
| Right panel | 320px × flex | **Done** | `UnitDetailProjection` |
| Top bar | 100% × 48px | **Done** | `C2TopBarProjection` |
| Bottom log | 100% × 120px | **Done** (full categories) | `MessageLogBridge.ProjectFrom` |

### 4.3 Component inventory (delta — not yet built)

| Component | Type | Zone | MVP |
|-----------|------|------|-----|
| GlobeView | Map | Map | P0 stub → Cesium/URP globe Sprint 5 |
| UnitDetailPanel | Panel | Right | P0 read-only |
| DelegationBadge | Overlay | Map | P0 icon on unit |
| TimeCompressionMenu | Menu | Top | P0 presets from doc 03 |
| MessageLogRowSelect | List row | Bottom | P1 → focus unit |

---

## 5. States & Variants

| State | Trigger | Visual | Behavior |
|-------|---------|--------|----------|
| Planning | Before Begin Execution | Map dimmed; drawer read-only | No engage intents |
| Running | Sim ticking | All zones live | Projections refresh per tick |
| Paused | Pause hotkey | Top bar PAUSE lit | Tick frozen; log still scrollable |
| No selection | No unit picked | Right panel placeholder | "Select a unit on map or OOB" |
| Unit selected | OOB row / map pick | Right panel populated | Doctrine, magazine, EMCON, sensors summary |
| Replay | Replay mode | Time scrubber (future) | Read-only commands |

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

---

## 9. Accessibility (screen-level)

- Affiliation: shape + color (NATO-style frame, not color-only)
- Message log: scalable font; max 12 visible rows with scroll (configurable)
- Focus: left drawer ListView keyboard navigable (Unity 6 default)
- Reduced motion: instant tab switch, no map pan inertia

---

## 10. Acceptance Criteria (Sprint 4–5)

1. Left drawer three tabs show live data in Play Mode with `baltic-patrol` / `baltic-patrol-mission`.
2. Message log shows non-combat categories (CONTACT, POLICY_DENIAL) when scenario produces them.
3. Right panel shows unit id, alive/destroyed, magazine summary, EMCON line for selected `u1`.
4. No MonoBehaviour writes to `DecisionLog` except via bridge command API.
5. Map zone may be placeholder (grid + unit dots) until globe ships — right panel must work without map.

---

## 11. Open Questions

| Question | Owner | Resolution |
|----------|-------|------------|
| Globe: Cesium vs custom URP terrain? | Engineering + TA | ADR pending |
| Single UIDocument vs multi-document layout? | UI | Current: multi-host; consolidate Sprint 5 |
| Player journey map missing | Design | Create `design/player-journey.md` |