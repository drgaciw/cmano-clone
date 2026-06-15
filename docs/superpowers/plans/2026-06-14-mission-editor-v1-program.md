# Mission Editor v1 — Agile Program Plan

> **Locked decisions:** Full GDD v1 MVP · Start after Sprint 22 · 2-week sprints, 1 FTE + parallel subagents  
> **Saved:** 2026-06-14 (adapted from Cursor plan `mission_editor_v1_program_a86fd35b`)

**Goal:** Ship the [Agentic Mission & Scenario Editor GDD v1](design/gdd/agentic-mission-editor.md) — canonical scenario file, four mission archetypes, typed events, Validation Engine, full MCP suite, Unity edit-mode shell, and mechanically testable AC-1..AC-12.

**Baseline today:** Headless spine exists ([ADR-008](docs/architecture/adr-008-mission-editor-validation-engine.md), [ADR-010](docs/architecture/adr-010-headless-first-command-driven-ui.md)): `ScenarioValidationEngine`, Patrol/Strike MCP CRUD, `scenario_validate` / `scenario_simulate_sample`, `editVersion` guards. ~30% of v1 scope.

**Production artifacts:** [mission-editor-v1.md](../../production/milestones/mission-editor-v1.md), [agentic-mission-editor-v1/EPIC.md](../../production/epics/agentic-mission-editor-v1/EPIC.md), stories ME-001..ME-053.

---

## Parallel development operating model (1 FTE + subagents)

The human FTE acts as **Producer/Tech Lead orchestrator**. Implementation is delegated to **isolated subagents** in **git worktrees**, merged via **Graphite stacks**.

### Per-sprint rhythm (2 weeks)

| Day | Orchestrator | Subagents |
|-----|--------------|-----------|
| **D1** | Kickoff: `/story-readiness`, GitNexus + Hindsight recall | — |
| **D1–D2** | Create epic stories; carve **2–3 parallel tracks** | `using-git-worktrees` → `.worktrees/sprint{N}-{track}/` |
| **D2–D8** | Daily dispatch implementer subagents | TDD per story; no cross-track file edits |
| **D8** | Spec + `c-sharp-reviewer` per completed story | Parallel only when independent |
| **D9** | Integration: merge worktrees; `gt restack` | `dotnet test ProjectAegis.sln` |
| **D10** | `/smoke-check`, `gitnexus_detect_changes`, Hindsight retain | `/story-done` |

### Worktree + Graphite conventions

| Item | Convention |
|------|--------------|
| Worktree root | `.worktrees/sprint{N}-{track}/` |
| Branch prefix | `stack/mission-editor/s{N}-{track}-{slug}` |
| PR workflow | `gt create` → `gt submit --stack --no-interactive` |

### Mandatory gates per track

- `gitnexus_impact` before symbol edits
- `gitnexus_detect_changes` before commit
- TDD RED → GREEN
- `verification-before-completion` before sprint close

---

## Epic decomposition (TR-editor-001..005)

| TR ID | Sprint target | Primary deliverable |
|-------|---------------|---------------------|
| TR-editor-001 | S23 | Full `scenario.json` schema + `*.aegis-scenario` ZIP + stable serializer (AC-6) |
| TR-editor-002 | S23–S25 | Six v1 validation rules + doctrine resolution (AC-3, AC-4) |
| TR-editor-003 | S24 | `fire_order` + world-state hash in `scenario_simulate_sample` (AC-2) |
| TR-editor-004 | S23 (extend) | `editVersion` on all mutating MCP verbs (AC-10) |
| TR-editor-005 | S25 | Full MCP suite + AC-5 headless sample |

---

## Sprint 23 — Canonical file foundation

**Goal:** Expand the intent-compiler spine so headless and Unity share one schema and git-diffable serialization.

| Track | Branch | Stories | Key files |
|-------|--------|---------|-----------|
| **A: Schema** | `s23-schema-expand-dto` | ME-001, ME-002 | `ScenarioDocumentDto.cs`, side/ORBAT/reference DTOs |
| **B: Serialization** | `s23-serialize-stable-json` | ME-003, ME-009 | `ScenarioStableJsonWriter`, `validation-config.json` |
| **C: Package** | `s23-package-aegis-zip` | ME-004 | `AegisScenarioPackage` ZIP |

**Sprint gate:** `dotnet test` green; `smoke-ac6.sh` byte-identical double-save; GitNexus LOW/MEDIUM on touched symbols.

---

## Sprint 24 — Events DSL + determinism

ME-010..ME-014: P0 triggers/actions, MCP event verbs, `fire_order`, AC-2/AC-7.

---

## Sprint 25 — ORBAT + four missions + MCP completion

ME-020..ME-025: Support/Ferry, reference points, full MCP suite, AC-4/AC-5.

---

## Sprint 26 — Export gates, brief, concurrency

ME-030..ME-034: AC-10, AC-11, AC-12, export brief, complexity warnings.

---

## Sprint 27 — Unity edit-mode shell

ME-040..ME-044: ScenarioEditorHost, map ORBAT, AC-8.

---

## Sprint 28 — Program hardening + release gate

ME-050..ME-053: Test consolidation, replay-verify, tracker update, QA exit.

---

## Risk register

| Risk | Mitigation |
|------|------------|
| Parallel agents edit same files | Strict track ownership; integrate D9 only |
| Schema churn breaks Baltic harness | Adapter for legacy minimal JSON during S23 |
| Unity not in CI | AC-8 PlayMode + manual sign-off |
| MissionEditor.Cli scope creep | Freeze non-editor verbs to separate epic |

---

## Out of scope (Phase 2/3)

NL agents, CMO import, Lua, operations timeline UI polish, mining/cargo P1, Steam Workshop.
