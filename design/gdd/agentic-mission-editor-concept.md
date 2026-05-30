# Feature Concept: Agentic Mission & Scenario Editor

> **Status:** Concept (brainstorm output — feeds `/design-system agentic-mission-editor`)
> **Created:** 2026-05-30
> **Source:** `Game-Requirements/requirements/11-Agentic-Mission-Editor.md`
> **Concept:** `design/gdd/game-concept.md` · **Index:** `design/gdd/systems-index.md` (system 11)

---

## One-Line

A scenario editor whose **single source of truth is a declarative scenario file** — map drawing, natural language, and MCP are all front-ends that emit the *same* canonical objects — so determinism, git-diff, headless generation, and AI co-authoring are structural properties, not bolted-on features.

## The Core Decision: Spine

The requirements doc pulls in two directions — **CMO parity** (match a 20-year-deep desktop tool) and an **agentic leap** (NL, staff-officer agents, MCP). The failure mode is "a worse CMO plus a half-baked AI layer." The spine resolves this:

**Intent-compiler foundation, with staff-officers and timeline as first-class views.**

- **A — Intent compiler (foundation):** one declarative scenario file is canonical. The editor is a *compiler + validator*, not a drawing tool. Determinism, git-diff, MCP, and headless all fall out of this for free.
- **B — Staff officers (view):** author by briefing + approving agent diffs; manual edit is the override path. This is the game's *agentic-command* pillar applied to authoring.
- **C — Timeline (view):** missions + events + triggers as expressions on one scrubable time axis. Best determinism/debug showcase.

B and C are **views over the canonical file**, never competing sources of truth.

### The one rule that makes it work

> Map drawing, NL, and MCP all emit the **same** canonical objects. There is no private UI state the simulation or validation can read. `editorState` (camera, layer toggles, last-validation cache) is **purely derived** and is never an input to the sim or validation engine.

Violating this rule re-introduces the UI-vs-file gap that breaks determinism, git diff, and headless parity simultaneously. It is the load-bearing constraint of the whole design.

---

## v1 Scope (matches doc MVP)

**Foundation built for real in v1 (not throwaway):**

- Canonical scenario file format (`*.aegis-scenario` — stable-key-ordered JSON + manifest)
- Validation engine (the v1 keystone — see Risks)
- MCP tool layer (core tools)
- Headless authoring/sample path

**Features shipped in v1:**

- Edit mode + map ORBAT placement (view over `orbat`/`referencePoints`)
- Mission types: **Strike, Patrol, Support, Ferry** with doctrine/ROE/EMCON inheritance + override
- Reference-point geometry CRUD
- Basic declarative events (typed DSL)
- Save / load with validation
- **Validation Agent** (P0-blocking)
- Core MCP tools: `scenario_create`, `scenario_load/save`, `mission_add/update/delete`, `mission_assign_units`, `reference_point_set`, `event_add/validate`, `scenario_validate`, `scenario_simulate_sample`, `scenario_export_brief`

**Deferred to Phase 2 / 3:**

- Polished operations-timeline view (C surface)
- Mission Planner Agent, Red Force Agent (B surfaces beyond Validation)
- Deep NL authoring
- CMO import
- Lua

---

## Resolved Open Questions (doc §Open Questions)

| Doc Q | Decision | Rationale |
|---|---|---|
| **Q2 Lua** | **Typed event DSL only in v1; no Lua.** Sandboxed-Lua escape block is a Phase 3 *maybe*. | Determinism is a named pillar and replays are evidence. Raw Lua = unsandboxed state + non-reproducible ordering + opaque diffs — the exact CMO pain point. Don't pay the determinism tax speculatively. |
| **Q5 Editor host** | **Shared engine-agnostic core (`ProjectAegis.ScenarioEditor.Core`, no `UnityEngine` dep); Unity = thin host in v1; standalone "Scenario Lab" = future second host.** | Forced by the spine: headless, MCP, and determinism require editor logic to be engine-agnostic C# (same pattern as `ProjectAegis.Sim` / `.Delegation`). A Unity-coupled core would kill headless + MCP. |
| **Q1 CMO import** | **Defer entirely (v1–P2); design the schema to permit it later.** | Legal/licensing unresolved + pure scope risk vs MVP. Keep schema expressive (sides, RP geometry, mission params, event TCA) so a future best-effort importer is possible at zero cost now. |
| **Q3 Agent-authored labeling** | *Open — recommend "yes, label in metadata"* (provenance already required per doc §6). | Transparency aligns with the audit/provenance requirement; cheap to store the approving user/agent id. Confirm at GDD time. |
| **Q4 Event graph complexity cap** | *Open — recommend a soft warning threshold, not a hard cap.* | Determinism + perf NFR (5,000+ unit ORBAT) wants a guardrail, but hard caps frustrate designers. Decide threshold during perf budgeting. |

---

## Data Model Implications

Refines the doc's high-level model with the spine's constraints:

- **`editorState` is derived-only.** Camera, layer toggles, last-validation cache. Never read by sim or validation. (Load-bearing rule above.)
- **DB binding is in the canonical file from day one.** A scenario that doesn't pin its DB snapshot reference (req 06 / system 20) is not deterministic. Retrofitting this later is painful — do it in v1.
- **Geometry lives in `referencePoints[]`.** Map drawing emits into this array; it holds no private state.
- **Events are typed DSL objects** (`trigger`, `conditions[]`, `actions[]`) — no embedded code in v1.
- **Stable key ordering** in serialization is mandatory for human-readable git diffs (acceptance criterion #6).

---

## Top Risks for the GDD Author

1. **The Validation Agent is the v1 keystone, not a nice-to-have.** Acceptance criterion #3 (catch empty patrol area, strike with no targets, ferry without destination, DB version mismatch) is what makes the intent-compiler *trustworthy*. If validation is weak, "agents propose, humans approve" collapses and the whole differentiator dies. Treat as P0-blocking.
2. **DB version binding must be canonical from v1** (see Data Model). Determinism depends on it.
3. **Spine discipline.** Every new surface (timeline, NL, planner agent) must route through the canonical objects. The first time a view stores private state the sim reads, the determinism/diff/headless guarantees silently break.

---

## How This Maps to the Pillars

| Pillar | How the editor serves it |
|---|---|
| Simulation fidelity | Validation enforces fuel/magazine/geometry feasibility before export |
| Agentic command | Staff-officer authoring (B) — you command the editor as you command the battle |
| Determinism | Canonical file + DB pin + DSL-only logic → same file + seed = same event order |
| Near-future warfare | Mission archetypes + DB binding carry swarm/hypersonic/DEW units with no editor special-casing |

---

## Next Actions

| Order | Action | Command |
|---|---|---|
| 1 | Author full 8-section GDD for the editor | `/design-system agentic-mission-editor` |
| 2 | Capture spine + host decisions as ADRs | `/architecture-decision` (×2: canonical-file spine, engine-agnostic editor core) |
| 3 | Confirm open Q3/Q4 during GDD authoring | — |
