# Story ME-004 — Phase 2 residual P1 (ME-W3)

**Epic:** mission-editor-phase2-completion  
**Wave:** ME-W3  
**Status:** Complete (2026-07-09)  
**Type:** Feature (partial implement) + honesty defer  

## Acceptance

Either implement with tests **or** document Phase 2.4+ defer for:

- [x] **AME-4.5** sides/factions — **Partial+ headless:** `UpsertSide` / `TryRemoveSide` + bus + CLI `side_list` / `side_upsert` / `side_delete`
- [x] **AME-3.5** operations timeline — **Partial+ headless:** `UpsertTimelineEntry` / `TryRemoveTimelineEntry` + CLI `timeline_list` / `timeline_upsert` / `timeline_delete`; full Gantt UI **Phase 2.4+ deferred**
- [x] **AME-3.6** mining/cargo — **Phase 2.4+ deferred** (honesty; new archetypes need validation formulas)
- [x] **AME-4.4** layers/minimap — **Phase 2.4+ deferred** (Unity map chrome residual)
- [x] **AME-7.3** semantic diff — **Partial+:** `ScenarioSemanticDiff.Summarize` + CLI `scenario_diff_summary`

No false “shipped” without evidence.

## Closeout notes (ME-W3)

| Track | Outcome | Evidence |
|-------|---------|----------|
| **W3-a** | Sides CRUD headless | `ScenarioDocumentEditor.UpsertSide` / `TryRemoveSide`; `SideCommands`; tests `ScenarioSideEditorTests`, `SideCliTests` |
| **W3-b** | Timeline headless | `UpsertTimelineEntry` / `TryRemoveTimelineEntry`; `TimelineCommands`; tests `ScenarioTimelineEditorTests`, `TimelineCliTests` |
| **W3-c** | Semantic diff | `ScenarioSemanticDiff.Summarize`; `ScenarioDiffSummaryCommand` / `scenario_diff_summary`; tests `ScenarioSemanticDiffTests` |
| **Honesty** | Mining + layers | AME-3.6 + AME-4.4 documented Phase 2.4+ deferred in doc 11 |

**Doc:** `Game-Requirements/requirements/11-Agentic-Mission-Editor.md` — AME-3.5/4.5/7.3 Partial+ headless; AME-3.6/4.4 Phase 2.4+ deferred.  
**Not claimed:** Unity Gantt, faction placement UI, layers/minimap chrome, mining/cargo archetypes, NL prose semantic summaries.

## Ownership

- Sides (W3-a): Editor/bus + `SideCommands`
- Timeline (W3-b): Editor/bus + `TimelineCommands`
- Semantic diff (W3-c): `ScenarioSemanticDiff` + `ScenarioDiffSummaryCommand`
- Docs honesty: doc 11 + this story + epic ME-004
