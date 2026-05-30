# Game Concept: Project Aegis

> **Status:** Draft (derived from requirements)  
> **Created:** 2026-05-29  
> **Source:** `Game-Requirements/requirements/01-Project-Overview.md`, `02-Core-Gameplay-Loop.md`

---

## One-Line Pitch

**Project Aegis** is a hardcore near-future theater command wargame in the tradition of Command: Modern Operations, rebuilt for the 2030s with deterministic simulation, full agent delegation, and agent-vs-agent research at extreme time compression.

---

## Player Fantasy

You are a **theater commander**, not a pilot or captain. You plan missions, set doctrine, delegate task forces to AI staff officers, and intervene when the battlespace demands — or let the fight run headless for analysis and balance.

---

## Core Pillars

1. **Simulation fidelity** — Sensors, EW, logistics, and engagement geometry matter.
2. **Agentic command** — Delegation is core, not an autopilot cheat.
3. **Determinism** — Same seed and orders produce the same outcome; replays are evidence.
4. **Near-future warfare** — Swarms, loyal wingmen, hypersonics, cognitive EW (optional speculative layer).

---

## Core Loop (Summary)

1. Select scenario and force composition  
2. Plan missions, doctrine, and agent assignments (**RTwP** — clock frozen until **Begin Execution**)  
3. Execute with variable time compression and oversight  
4. After-action review from order log and AAR agents  
5. Iterate forces, agents, and scenarios (including batch agent-vs-agent)

Scenario policy JSON may override player info visibility and personality-edit rules per mission (defaults: full transparency, edit anytime). See req 02.

---

## Scope (v1)

**In:** Regional theaters; air/naval/limited land; drone swarms; agent delegation; headless batch mode.  
**Out (v1):** Global campaigns; full multiplayer; battalion-scale land warfare.

---

## Reference

- Requirements: `Game-Requirements/Game-Requirements-Index.md`
- CMO parity: `Game-Requirements/cmo-manual-traceability.md`
