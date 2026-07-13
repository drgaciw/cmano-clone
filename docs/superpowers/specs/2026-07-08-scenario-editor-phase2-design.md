# Scenario Editor Phase 2 — Product Design

**Status:** Approved (user 2026-07-08); P2.1 plan: `docs/superpowers/plans/2026-07-08-scenario-editor-phase2-1-plan.md`  

**Date:** 2026-07-08  
**Author:** Collaborative brainstorm (user + orchestrator)  
**Requirement:** `Game-Requirements/requirements/11-Agentic-Mission-Editor.md` (Phase 2 AME-*)  
**GDD:** `design/gdd/agentic-mission-editor.md`  
**Prior program:** Headless + AC-8 complete (`scenario-editor-completion` SE-W0–W3); S81–S88 headless  
**Boundary predecessor:** `production/scenario-editor-scope-boundary-2026-07-04.md` (headless only)  
**This design:** **Phase 2.1 vertical slice** — Map-first ORBAT + geometries + live validation in Unity Edit Mode  

---

## 1. Problem

Headless scenario authoring is trustworthy (canonical file, Validation Engine, CLI/MCP, host load). Solo expert designers still lack the **map-first** surface that defines CMO-class scenario design. Phase 2 must add that surface **without** forking the scenario model or weakening determinism.

## 2. Goals

| Goal | Success signal |
|------|----------------|
| Map-first authoring | Designer places/moves ORBAT and draws RP geometries on theater map |
| Live validation | Findings panel shows same error codes as `scenario_validate` after edits |
| Canonical truth | All commits land in `scenario.json` via `ScenarioDocumentEditor` |
| Headless parity | Mutations remain CLI/MCP-expressible (or pure derived `editorState`) |
| Invariant safety | ZERO `DelegationBridge` hotpath; frozen Baltic hash/goldens; CatalogWriteGate extend-only |

## 3. Non-goals (this design / first slice)

- Visual event graph editor (P2.3)
- Full Mission Board polish (P2.2)
- NL Mission Planner / multi-agent staff (Phase 3)
- CMO `.scen` import (ADR-013), Lua (ADR-014)
- Standalone Scenario Lab app as first host (optional later, ADR-017)
- Multiplayer collab real-time editing
- Stage advance to Launch

## 4. Decisions (locked in brainstorm)

| Decision | Choice |
|----------|--------|
| Program scope | Phase 2 product vision |
| Primary author | Solo expert designer |
| Primary surface | Map-first |
| Host | Unity Edit Mode inside Project Aegis |
| First shippable slice | Map + ORBAT + zones + live validation |
| Architecture approach | **A** — extend C2 map presentation into Edit Mode over shared Data core |

## 5. Player fantasy

You are the **theater designer**. You live on the map: place the force, draw the zones, attach missions from selection. The tool never silently ships a broken scenario—findings appear as you edit, with the same codes and messages as the headless Validation Engine.

## 6. Pillars

1. **Canonical file is truth** — map gestures commit to `orbat` / `referencePoints` / `missions`, not private Unity-only state.  
2. **Validation Engine stays pure** — live findings call the same engine as CLI; no LLM in any blocking path.  
3. **Map-first for experts** — density and speed over wizard-heavy onboarding in P2.1.  
4. **Headless parity** — GUI is a front-end; CLI/MCP remain first-class.

### Anti-pillars

- Do **not** store authoritative geometry only in scene GameObjects.  
- Do **not** invent a second scenario schema for the GUI.  
- Do **not** ship visual event graph in the first Phase 2 slice.

## 7. Architecture (Approach A)

```
┌─────────────────────────────┐         ┌──────────────────────────────┐
│  Unity Edit Mode Host       │         │  ProjectAegis.Data (shared)  │
│  MapAuthoringSurface        │ commit  │  ScenarioDocumentEditor      │
│  EditModeController         │────────►│  ScenarioValidationEngine    │
│  LiveFindingsPresenter      │◄────────│  scenario.json (canonical)   │
│  SelectionInspector         │ findings│  CLI / MCP (parity)          │
│  editorState (derived only) │         │                              │
└─────────────────────────────┘         └──────────────────────────────┘
```

### Components

| Component | Responsibility |
|-----------|----------------|
| **EditModeController** | Play ↔ Edit FSM; routes input; blocks play from reading draft-only state incorrectly |
| **MapAuthoringSurface** | ORBAT glyphs + RP geometries; gesture-end commits |
| **ScenarioEditCommandBus** | Serializes mutations into `ScenarioDocumentEditor` (editVersion, undo) |
| **LiveFindingsPresenter** | Invokes pure `ScenarioValidationEngine`; lists codes; jump-to-entity |
| **SelectionInspector** | Selected unit / RP / mission fields |
| **ScenarioSession** | Load/save path, dirty flag, CONFLICT recovery |

### Hard rules

- `editorState` is **derived-only** (camera, layers). Never an input to Validation Engine or sim (AC-9).  
- **Play Mode** must not treat unsaved edit buffers as authoritative world state.  
- **ZERO** production edits to `DelegationBridge.cs` hotpath.  
- Scenario mutations are **file-based** via Data authoring APIs — not CatalogWriteGate write paths.

## 8. First vertical slice (P2.1) — detailed scope

### In scope

| Capability | Notes |
|------------|--------|
| Load/save scenario | Same loaders as AC-8; dirty + save |
| Place / move / clone units | Commit into ORBAT model |
| Draw/edit RP: polygon, circle, line | Invalid draws stay marked invalid (not silently dropped) |
| Selection + inspect | Unit / RP / mission summary |
| Attach mission from selection | Use existing mission types (Strike/Patrol/Support/Ferry) via editor APIs |
| Live Findings panel | Debounced re-validate after commit; codes match CLI |
| editVersion + undo | Reuse existing stack/sidecar patterns |
| CLI/MCP parity check | Any new mutation has verb or is pure camera/layer derived |

### Out of P2.1 (explicit)

| Capability | Target |
|------------|--------|
| Mission Board list/wizard/templates | P2.2 |
| Visual event graph + full static analysis | P2.3 (also lifts AC-7 Partial) |
| Sides/factions deep UI | Later Phase 2 |
| Mining/cargo archetypes | Later Phase 2 |
| NL agents, CMO import, Lua | Phase 3 / ADRs |

## 9. Data flow

1. Load `scenario.json` via Data loaders.  
2. Map presents ORBAT + `referencePoints`.  
3. Gesture end → command bus → `ScenarioDocumentEditor` mutation → bump `editVersion` → undo snapshot.  
4. Debounced pure `Validate` → Findings panel.  
5. Save writes canonical file; `editorState` only for camera/layers.  
6. Export/play/sample remain blocked on error-severity findings (AC-12).

## 10. Error handling

| Case | Behavior |
|------|----------|
| Validation errors | Do not block save; block export/play/sample |
| Stale editVersion | CONFLICT; no last-write-wins |
| Invalid geometry | Remain visible and marked invalid |
| Load failure | Clear error; no partial silent map |
| Play with dirty invalid file | Prefer explicit confirm or force-validate; never ignore blocking findings for sample |

## 11. Testing strategy

| Layer | What |
|-------|------|
| Unit | Command bus + editor mutation helpers |
| Engine | Reuse Validation goldens; GUI must not reimplement rules |
| PlayMode / EditMode | Load fixture → place unit → save → reload; bad fixture shows findings |
| Regression | Full solution floor; ReplayGolden 6/6; PlayModeSmoke; hash grep; ZERO bridge |

## 12. Phasing

| Slice | Content | Exit |
|-------|---------|------|
| **P2.1** | This design | Map ORBAT + zones + live findings shippable |
| **P2.2** | Mission Board | List/wizard/clone/templates |
| **P2.3** | Event graph | Visual graph + full static analysis; retire AC-7 stub |
| **P3** | Agents / import / Lua | Per ADR-013/014/015 |

## 13. Standing invariants (carry-forward)

| Invariant | Rule |
|-----------|------|
| Test floor | ≥1232 monotonic |
| ReplayGolden | 6/6 |
| Hash | `17144800277401907079` |
| DelegationBridge | ZERO hotpath edits |
| CatalogWriteGate | Extend-only |
| Baltic corpora | Frozen |
| Stage | Release unless separate decision |

## 14. Open questions (non-blocking for first plan)

1. Exact reuse boundary between C2 globe/map stack and Edit Mode surface (spike in first plan task).  
2. Debounce interval for live validate (propose 200–400 ms; tune).  
3. Whether attach-mission UI is radial-from-selection or inspector dropdown (prefer inspector for density).  

## 15. Acceptance criteria (design-level)

1. Designer can place/move a unit and draw a patrol polygon; both appear in saved `scenario.json`.  
2. Creating an invalid strike (unreachable) produces `STRIKE_UNREACHABLE` (or existing code) in Findings without running CLI manually.  
3. CLI `scenario_validate` on the saved file yields the same error codes.  
4. `editorState` absent or non-authoritative for sim/validation paths.  
5. No `DelegationBridge` production hotpath change; hash and ReplayGolden hold.  

## 16. Next step after user reviews this file

Invoke **writing-plans** for **P2.1 only** (map + ORBAT + zones + live findings), producing  
`docs/superpowers/plans/YYYY-MM-DD-scenario-editor-phase2-1-plan.md` with worktree isolation and parallel tracks.

---

## Spec self-review (2026-07-08)

| Check | Result |
|-------|--------|
| Placeholders | None (open questions listed explicitly, non-blocking) |
| Consistency | Pillars match architecture and slice; event graph deferred consistently |
| Scope | Single vertical slice P2.1 focused enough for one plan |
| Ambiguity | Host Approach A locked; Mission Board and graph out of P2.1 |
