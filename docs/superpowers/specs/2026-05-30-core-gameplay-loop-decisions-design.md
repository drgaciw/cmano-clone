# Core Gameplay Loop — Resolved Decisions Design Spec

**Date:** 2026-05-30  
**Project:** Project Aegis (cmano-clone)  
**Status:** Approved design  
**Source requirement:** `Game-Requirements/requirements/02-Core-Gameplay-Loop.md`  
**Related requirements:** 03 (Simulation Modes), 04 (Agent Delegation), 11 (Agentic Mission Editor), 13 (Doctrine/ROE/EMCON/WRA)

---

## 1. Overview

This document records the three open questions from req 02, the decisions made during brainstorming (May 30, 2026), and how those decisions integrate with existing scenario policy infrastructure.

No new simulation systems are introduced here. The decisions constrain Phase 2–3 UX behavior and scenario metadata.

---

## 2. Decisions Locked

| # | Question | Decision | Default |
|---|----------|----------|---------|
| 1 | Planning phase: turn-based or continuous? | **Real-time with pause (CMO-style)** | N/A (fixed) |
| 2 | Player fog when agents have high autonomy? | **Scenario-configurable** | `fullTransparency` |
| 3 | Personality edits: planning-only or real-time? | **Scenario-configurable** | `anytime` |

---

## 3. Decision 1 — Planning Phase (RTwP)

### Behavior

1. Entering Phase 2 freezes the simulation clock.
2. Player completes route planning, sensor/EW posture, ROE, and agent assignment without time pressure.
3. Optional: player may unpause for time-sensitive coordination (scenario-dependent events).
4. **Begin Execution** is the sole gate into Phase 3 — no auto-start.

### Rationale

- Matches CMO player expectations and the "theater commander" fantasy.
- Avoids artificial planning-turn mechanics that add rules overhead without sim benefit.
- Aligns with deterministic session model: Phase 2 orders are committed before tick pipeline runs in Phase 3.

### Cross-doc alignment

- Req 03 Mixed Mode "dynamic autonomy during play" applies to Phase 3 only, not planning lock semantics.
- Req 11 mission editor should expose a **Begin Execution** preview gate in scenario validation.

---

## 4. Decision 2 — Player Information Model

### Default: `fullTransparency`

The player always receives:

- Full friendly-side sensor picture (subject to sim fog-of-war, not delegation fog)
- All agent decisions in the order log (req 17)
- Autonomy level changes who executes orders, not what information the commander sees

### Override modes

**`delegationFog`** — Models C2 friction for training scenarios:

- At Full Autonomous, contacts and agent actions surface as delayed summaries
- Player must **Request Status** or take direct control for full tactical picture
- Order log still records full history for AAR (Phase 4); replay is always transparent

**`tieredByAutonomy`** — Graduated visibility:

| Autonomy | Player view |
|----------|-------------|
| Manual / Assisted | Full picture |
| Semi-Autonomous | Near-real-time; detail on demand |
| Full Autonomous | Summary + alerts; override restores full picture |

### Rationale for scenario-configurable default

- Hardcore wargamers expect full situational awareness (CMO parity).
- Training and narrative scenarios benefit from optional friction without forking the sim core.
- AAR/replay remain fully transparent regardless of live-play info model — analysis integrity is preserved.

---

## 5. Decision 3 — Personality Edit Policy

### Default: `anytime`

- Personality and autonomy may be changed during Phase 3 execution without pause.
- Matches req 04 assignment UI and req 03 seamless mode switching.
- Changes are logged to the order log for determinism and replay.

### Override modes

**`planningOnly`** — Personalities locked at **Begin Execution**:

- Autonomy level may still change mid-fight (lighter tactical tweak)
- Useful for balance tournaments and agent-vs-agent baseline runs

**`tieredRebrief`** — Realism gate:

- Manual/Assisted: edit anytime
- Semi-Autonomous+: requires pause or **Rebrief Agent** action
- Rebrief incurs configurable sim-time cost (scenario policy field, future)

### Rationale for scenario-configurable default

- Player agency is a core pillar; restricting edits by default would fight req 04.
- Batch/balance scenarios need lock-down without a separate game mode.

---

## 6. Scenario Policy Integration

### Recommended approach

Extend existing scenario policy JSON (Approach 1 from brainstorming). ROE fields already live in `data/scenarios/*.policy.json` and load via `ScenarioPolicyProfile`.

### Proposed schema extension (requirements-level)

```json
{
  "id": "example-scenario",
  "friendlyRoe": "WeaponsFree",
  "opposingRoe": "WeaponsTight",
  "playerInfoModel": "fullTransparency",
  "personalityEditPolicy": "anytime"
}
```

When omitted, loaders default to `fullTransparency` and `anytime`.

### Authoring path

1. **Machine contract:** scenario policy JSON (sim/delegation load at session start)
2. **Authoring surface:** req 11 mission editor exports policy JSON on publish
3. **Not used:** per-scenario C# hooks (rejected — breaks batch determinism audits)

### Implementation note (future)

`ScenarioPolicyProfile` and `ScenarioPolicyJsonLoader` will gain the two enum fields. This spec does not prescribe C# types — that belongs in an implementation plan.

---

## 7. Phase Flow (Updated)

```
Phase 1: Select scenario + forces
    ↓
Phase 2: RTwP planning (clock frozen)
    │     assign agents, ROE, routes
    │     [optional unpause for timed events]
    ↓ Begin Execution
Phase 3: Execution
    │     info model + personality policy active
    │     time compression 1x–900x+
    ↓ scenario end / player stop
Phase 4: AAR + replay (always full transparency)
    ↓
Phase 5: Iterate → return to Phase 1 or 2
```

---

## 8. Testing Implications

| Decision | Test focus |
|----------|------------|
| RTwP | No ticks advance in Phase 2 until Begin Execution; order log empty until Phase 3 |
| `fullTransparency` default | Agent decisions visible in live UI and order log |
| `delegationFog` override | Delayed UI updates at Full Autonomous; full log in replay |
| `anytime` default | Personality change mid-tick produces deterministic order log entry |
| `planningOnly` override | Personality change rejected after Begin Execution; autonomy change allowed |

---

## 9. Cross-Document Updates Required

| Document | Action |
|----------|--------|
| `02-Core-Gameplay-Loop.md` | Updated — open questions resolved |
| `04-Agent-Delegation.md` | Clarify "real time" = default `anytime`; scenario may restrict |
| `03-Simulation-Modes.md` | No change — already supports dynamic autonomy in execution |
| `design/gdd/game-concept.md` | Optional — add RTwP note to Core Loop summary |
| Scenario policy GDD / loader | Future — add enum fields to schema |

---

**Approved:** May 30, 2026 (brainstorming session)
