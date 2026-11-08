# Epic: Agentic Mission Editor v1

> **Status:** In Progress (Sprint 23 kickoff)
> **Priority:** P0 — Req 11 intent-compiler MVP
> **Created:** 2026-06-14
> **Milestone:** [mission-editor-v1.md](../../milestones/mission-editor-v1.md)
> **Depends on:** [agentic-mission-editor.md](../../../design/gdd/agentic-mission-editor.md), ADR-006, ADR-008, ADR-010
> **Design gate:** APPROVED 2026-06-01

## Goal

Ship GDD v1 Mission Editor MVP: canonical scenario file, four mission archetypes, typed events, Validation Engine, full MCP suite, Unity edit-mode shell, AC-1..AC-12.

## TR Traceability

| TR ID | Sprint | Deliverable |
|-------|--------|-------------|
| TR-editor-001 | S23 | Full `scenario.json` schema + `*.aegis-scenario` ZIP + stable serializer (AC-6) |
| TR-editor-002 | S23–S25 | Six v1 validation rules + doctrine resolution (AC-3, AC-4) |
| TR-editor-003 | S24 | `fire_order` + world-state hash in `scenario_simulate_sample` (AC-2) |
| TR-editor-004 | S23+ | `editVersion` on all mutating MCP verbs (AC-10) |
| TR-editor-005 | S25 | Full MCP suite + AC-5 headless sample |

## AC Coverage Matrix

| AC | Sprint | Stories |
|----|--------|---------|
| AC-1 | S23 (extend) | ME-001, ME-020 |
| AC-2 | S24 | ME-012, ME-013 |
| AC-3 | S23–S25 | ME-009, ME-020, ME-022 |
| AC-4 | S25 | ME-025 |
| AC-5 | S25 | ME-024 |
| AC-6 | S23 | ME-003, ME-004 |
| AC-7 | S24 | ME-014 |
| AC-8 | S27 | ME-044 |
| AC-9 | S23 | ME-009 |
| AC-10 | S23–S26 | ME-033 |
| AC-11 | S26 | ME-031 |
| AC-12 | S26 | ME-030 |

## Stories (ME-001..ME-053)

Sprint 23 (active): **ME-001**, **ME-002**, **ME-003**, **ME-004**, **ME-009**

See `story-me-*.md` in this directory. Reserved IDs (005–008, 015–019, etc.) hold scope for later sprints or Phase 2/3 deferrals.

## Sprint 23 Parallel Tracks

| Track | Branch | Stories | Key files |
|-------|--------|---------|-----------|
| **A: Schema** | `stack/mission-editor/s23-schema-expand-dto` | ME-001, ME-002 | `ScenarioDocumentDto.cs`, side/ORBAT/reference DTOs |
| **B: Serialization** | `stack/mission-editor/s23-serialize-stable-json` | ME-003, ME-009 | `ScenarioStableJsonWriter`, `validation-config.json` |
| **C: Package** | `stack/mission-editor/s23-package-aegis-zip` | ME-004 | `AegisScenarioPackage` ZIP |

## Acceptance (Epic Exit)

1. All AC-1..AC-12 pass in CI.
2. `dotnet test ProjectAegis.sln` + PlayMode smoke green.
3. Req 11 tracker **MVP Complete** (ME-052).
4. No sim/validation reads `editorState` (AC-9).
