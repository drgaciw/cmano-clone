# Mission Editor Phase 2 Completion Plan

> **For agentic workers:** REQUIRED SUB-SKILLS: `dispatching-parallel-agents`, `subagent-driven-development` (or `executing-plans`), `using-git-worktrees`, `test-driven-development`.  
> **Scope decision (user 2026-07-08):** **Phase 2 complete only** — not Phase 3 (NL agents, CMO import, Lua).  
> **Canonical copy after approval:** write to `docs/superpowers/plans/2026-07-08-mission-editor-phase2-completion-plan.md`.

**Goal:** Finish remaining **Phase 2 (P1)** mission/scenario editor requirements so a solo expert designer can author on map + Mission Board + event graph with live validation, honest ACs, and headless/CLI parity — without breaking standing invariants.

**Architecture:** Shared Data core (`ScenarioDocumentEditor` + Validation Engine + bus) remains truth; headless `ProjectAegis.Delegation.UnityAdapter.Authoring` facades + thin Unity Edit Mode host (Approach A). No `DelegationBridge` hotpath; no CatalogWriteGate scenario writes.

**Tech Stack:** .NET 8, ProjectAegis.Data / MissionEditor.Cli / UnityAdapter, Unity 6.3 Edit Mode (optional evidence), Graphite (`gt`), GitNexus.

---

## Baseline (truth as of 2026-07-08 @ main `1703536`)

| Program | Status |
|---------|--------|
| S81–S88 headless | Complete (Partial+ events) |
| `scenario-editor-completion` SE-W0–W3 | Complete (AC-1…12 except AC-7 Partial; AC-8 host load met) |
| **P2.1** map ORBAT + RP + live findings | **Merged** — APIs, bus, headless surface, CLI/MCP; **Unity EditorWindow deferred** |
| GitNexus | 23,665 symbols / 44,368 edges @ main |

### AC residual (doc 11)

| AC | Status | Phase 2 action |
|----|--------|----------------|
| AC-1…6, 8…12 | Green | Preserve |
| **AC-7** | **Partial stub** | **Lift in P2.3** (full debugger + static analysis) |

### AME Phase 2 gaps (in program)

| AME | Gap | Wave |
|-----|-----|------|
| AME-4.2–4.3 | Map place/draw **shipped headless**; Unity host deferred | **ME-W0** host |
| AME-4.4 | Layer toggles / minimap | ME-W0 optional / polish |
| AME-4.5 | Sides/factions UI | ME-W3 or later slice |
| AME-3.4 | Mission Board UI | **ME-W1 (P2.2)** |
| AME-3.5 | Operations timeline UI | ME-W3 |
| AME-3.6 | Mining/cargo archetypes | ME-W3 |
| AME-5.5 / AC-7 | Event debugger full projection | **ME-W2 (P2.3)** |
| AME-5.7 / 10.5 | Event static analysis beyond TCA stub | **ME-W2** |
| AME-6.9 / 10.4 | Live validation UX (engine done) | ME-W0 host + P2.1 findings panel |
| AME-7.3 | Semantic diff | Optional ME-W3 |

### Explicit out of this plan (Phase 3 / ADR)

- AME-9.x NL agents (Mission Planner, Red Force, …)
- ADR-013 CMO import execution
- ADR-014 Lua shim
- Steam Workshop, multiplayer collab

---

## Standing invariants (every wave)

| Invariant | Rule |
|-----------|------|
| Test floor | ≥1462 (current) monotonic — never regress |
| ReplayGolden | 6/6 |
| Hash | `17144800277401907079` |
| PlayModeSmoke | ≥18/18 |
| `DelegationBridge.cs` | ZERO production hotpath edits |
| CatalogWriteGate | Extend-only |
| Baltic corpora | Frozen |
| Stage | Release |

**Pre-edit hubs:** GitNexus `impact` on `ScenarioDocumentEditor`, `ScenarioValidationEngine` before symbol edits (single owner per wave for Editor).

---

## Program packaging

| Artifact | Path |
|----------|------|
| Epic | `production/epics/mission-editor-phase2-completion/EPIC.md` (new) |
| Scope boundary | `production/mission-editor-phase2-completion-scope-boundary-2026-07-08.md` |
| Plan (repo) | `docs/superpowers/plans/2026-07-08-mission-editor-phase2-completion-plan.md` |
| Design parent | `docs/superpowers/specs/2026-07-08-scenario-editor-phase2-design.md` |
| P2.1 boundary | `production/scenario-editor-phase2-1-scope-boundary-2026-07-08.md` |

| Wave | Story | Goal | Est. | Parallel? |
|------|-------|------|------|-----------|
| **ME-W0** | ME-001 | P2.1 residual: Unity Editor host + doc honesty for map AME | 2–4 d | Docs ∥ host |
| **ME-W1** | ME-002 | **P2.2 Mission Board** (list/wizard/clone/templates) | 5–8 d | Data board model ∥ CLI ∥ UA presenter |
| **ME-W2** | ME-003 | **P2.3 Event graph + AC-7 lift + static analysis** | 6–10 d | Engine/debugger ∥ graph model ∥ CLI |
| **ME-W3** | ME-004 | Phase 2 residual P1: sides / timeline / mining **or** defer with honesty | 4–8 d | Content tracks parallel if unlocked |
| **ME-W4** | ME-005 | Program gate + human ack + tracker/roadmap | 1–2 d | Serial |

**Exit for “Phase 2 complete”:** ME-W0 + ME-W1 + ME-W2 + ME-W4 **required**. ME-W3 either ships or explicitly documents remaining AME as “Phase 2.4+ / deferred” with honest doc 11 maturity (no false shipped).

---

## ME-W0 — P2.1 residual + honesty (parallel)

**Goal:** Ship Task 9 Editor host (or production checklist PASS with Editor evidence) and align doc 11 / GDD Implementation Mapping for headless-shipped AME-4.2/4.3 + live findings.

### Parallel tracks

| Track | Domain | Owner type | Deliverable | File ownership |
|-------|--------|------------|-------------|----------------|
| **W0-a** | Unity host | unity / c-sharp | `unity/.../Editor/ScenarioMapAuthoringWindow.cs` binding session/bus/surface only | `unity/ProjectAegis/Assets/Editor/**` |
| **W0-b** | Headless host tests | c-sharp-test | Expand Authoring tests if host facade needs glue | `UnityAdapter.Tests/Authoring/**` |
| **W0-c** | Docs honesty | tech-writer | Update doc 11 AME-4.2/4.3 maturity; fix stale “map not started”; AC-8 note already met | `Game-Requirements/requirements/11-*.md` only |
| **W0-d** | Evidence | qa | Fill/complete `production/qa/scenario-editor-p2-1-editor-host-checklist.md` or Editor batch evidence | `production/qa/**` |

**Serial:** none beyond merge order (docs can land before host).

**Acceptance**

1. Designer can open scenario in Edit Mode host (or documented Editor path) and place unit + RP with findings.  
2. Doc 11 does not claim map-first “not started” if headless APIs shipped.  
3. Invariants hold.

**Out:** Mission Board, event graph.

---

## ME-W1 — P2.2 Mission Board (parallel)

**Goal:** AME-3.4 — single Mission Board: list by side/type/status, add wizard, clone, template library; flight-plan preview optional/minimal.

### Architecture notes

- Board is a **view over** `ScenarioDocumentEditor.Missions` + bus `Attach*FromSelection` / existing `Add*` / `Update*` / `TryRemoveMission`.  
- Templates = pure Data helpers that emit `ScenarioMissionDto` then bus commit.  
- No second mission schema.

### Parallel tracks

| Track | Domain | Deliverable | File ownership |
|-------|--------|-------------|----------------|
| **W1-a** | Data board model | `MissionBoardQuery` (filter/sort by type/side/status), `MissionTemplateCatalog`, `CloneMission` on editor/bus | `ProjectAegis.Data/Scenario/Authoring/**` — **Editor single owner** |
| **W1-b** | CLI/MCP | `mission_list`, `mission_clone`, `mission_add_from_template` (names finalize in design note) | `MissionEditor.Cli/**`, `mcp-tools.json` |
| **W1-c** | UA presenter | `MissionBoardPresenter` + tests (headless) | `UnityAdapter/Authoring/**` |
| **W1-d** | Unity UI (after W1-c) | UI Toolkit / IMGUI board panel in Edit Mode | `unity/...` only |
| **W1-e** | Tests | Board filter/clone/template round-trip; CLI CONFLICT | `*.Tests` |

**Serial prereq:** W1-a APIs before W1-b/c/d. W1-b and W1-c parallel after W1-a. W1-d after W1-c.

**Acceptance**

1. List missions filtered by type; clone patrol → new id + editVersion bump.  
2. Wizard/template creates Strike/Patrol/Support/Ferry via same editor APIs as CLI.  
3. Findings refresh after board mutation (reuse LiveFindingsPresenter).  
4. MCP manifest lists new verbs.

**Out:** Full operations timeline Gantt (W3), mining types (W3).

---

## ME-W2 — P2.3 Event graph + AC-7 + static analysis (parallel)

**Goal:** Lift AC-7 Partial; ship event static analysis beyond TCA stub (AME-5.7 / 10.5); visual/graph authoring surface headless-first + optional Unity graph view.

### Parallel tracks

| Track | Domain | Deliverable | File ownership |
|-------|--------|-------------|----------------|
| **W2-a** | Event debugger full | Order-log-aligned debugger JSON: `sim_tick`, `sequence_id`, `action_results`, unmet conditions | `EventDebuggerTrace.cs`, fixtures — **no Editor mission APIs** |
| **W2-b** | Static analysis engine | Dead triggers, unreachable, circular deps, contradictory conditions; ADR-016 caps already soft/hard | `ProjectAegis.Data/Validation/**` or Authoring analysis module |
| **W2-c** | Event CRUD bus/CLI | Typed `event_add` / `event_update` / `event_delete` if missing; MCP parity | Cli + Editor event methods |
| **W2-d** | Graph view model | Headless event graph DTO (nodes/edges) for UI; jump-to from findings | `UnityAdapter/Authoring/**` |
| **W2-e** | AC-7 / StubScope | Retire or narrow stub pins; AC-7 checkbox honest green | Tests + doc 11 |
| **W2-f** | Docs | GDD/doc 11 maturity for AME-5.x | Docs only |

**Serial:** W2-a and W2-b can parallel (different symbols). W2-e after W2-a. W2-d after model stable. **Editor owner** if W2-c touches `ScenarioDocumentEditor`.

**Acceptance**

1. AC-7 fixture asserts full debugger shape (not stub-only).  
2. Static analysis emits distinct codes for dead/unreachable/circular; tests green.  
3. Headless graph model serializes from events; optional Unity panel.  
4. Export/sample still blocked on error findings; TeleportUnit AC-11 unchanged.

**Out:** Lua (ADR-014).

---

## ME-W3 — Remaining Phase 2 P1 (parallel or honesty-defer)

**Goal:** Either implement or **honestly defer** with maturity labels:

| Item | Prefer | Track |
|------|--------|-------|
| AME-4.5 Sides/factions authoring | Implement if schema DTOs enough; else partial headless CRUD | W3-a |
| AME-3.5 Operations timeline | Minimal Gantt list + activateAtTick edit | W3-b |
| AME-3.6 Mining/cargo | Domain + validation + CLI only (no full UI) | W3-c |
| AME-4.4 Layers/minimap | Map host polish | W3-d |
| AME-7.3 Semantic diff | Optional CLI `scenario_diff_summary` | W3-e |

**Gate rule:** If capacity slips, **ME-W4 must still pass** with doc 11 listing deferred AME as Phase 2.4+ — never “shipped” without tests.

---

## ME-W4 — Program gate + human ack (serial)

1. Full verification block (build/test/ReplayGolden/PlayModeSmoke/hash/bridge).  
2. Scope boundary sign-off + epic status Complete.  
3. Tracker / roadmap alias update (dated snapshot).  
4. Human ack phrase: e.g. **“Mission editor Phase 2 complete”**.  
5. GitNexus reindex after land.

---

## Parallel agent dispatch model

Per wave, use **one isolated worktree + one agent per track** when tracks do not share write ownership of `ScenarioDocumentEditor.cs`.

```
Coordinator (this session / producer)
  ├─ worktree me-w1-data     → W1-a (Editor owner)
  ├─ worktree me-w1-cli      → W1-b (after W1-a merge or API stub)
  └─ worktree me-w1-ua       → W1-c (after W1-a)
```

**Conflict rules**

| File | Rule |
|------|------|
| `ScenarioDocumentEditor.cs` | **One owner per wave** |
| `ScenarioValidationEngine.cs` / Rules | ME-W2-b primary |
| `DelegationBridge.cs` | **Never** |
| `mcp-tools.json` + `Program.cs` | CLI track owner |
| Doc 11 | Docs track; no silent AC flips without evidence |

**Agent prompt template (each track)**

1. Worktree path + branch name  
2. Exact files owned  
3. TDD steps + acceptance  
4. Invariants  
5. Report: DONE / DONE_WITH_CONCERNS + SHAs + test filters  

**Integration:** Coordinator merges tracks bottom-up with `gt`; full suite before wave closeout.

---

## Verification (every wave exit)

```bash
dotnet build ProjectAegis.sln
dotnet test ProjectAegis.sln -v minimal          # 0 fail; count ≥ prior floor
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/... --filter PlayModeSmokeHarnessTests
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/... --filter ReplayGolden
# hash + ZERO DelegationBridge in wave diff
node .gitnexus/run.cjs impact ScenarioDocumentEditor --direction upstream --summary-only
```

---

## Risk register

| Risk | Mitigation |
|------|------------|
| Editor hub CRITICAL blast radius | Single owner; impact pre-edit; additive APIs only |
| Unity Editor absent in CI VM | Headless board/graph + checklist; optional Editor evidence local |
| Scope creep to Phase 3 | Explicit out; reject NL/import/Lua stories |
| AC-7 pin tests block lift | Update StubScope pins in same PR as debugger |
| ME-W3 too large | Honesty defer at W4 |

---

## Success definition (user-facing)

Phase 2 complete means:

1. Map-first ORBAT/RP (P2.1) + **usable Edit Mode host path**  
2. **Mission Board** authoring for four v1 mission types  
3. **Event graph / debugger** lifts AC-7; static analysis real  
4. Doc 11 + tracker honest; AC-1…12 green or Partial only with explicit residual  
5. Invariants green; human ack recorded  

---

## Recommended execution order

1. Approve this plan → write repo copy under `docs/superpowers/plans/`  
2. Publish epic + scope boundary (ME-W0-c parallel)  
3. ME-W0 → ME-W1 → ME-W2 → (ME-W3 or defer) → ME-W4  
4. Prefer **subagent-driven** within wave; **dispatching-parallel-agents** across independent tracks  

---

## Next after plan approval

1. Create `production/epics/mission-editor-phase2-completion/` + scope boundary  
2. Spawn ME-W0 parallel agents (host + docs) in worktrees  
3. writing-plans detail for **ME-W1 Mission Board only** if W1 needs bite-sized TDD tasks before code  
