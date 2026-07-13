# Scenario Editor — Requirements Improvement Plan

**Date:** 2026-07-01
**Method:** GitNexus knowledge-graph review of the `cmano-clone` repo (Scenario cluster: 114 symbols / 86% cohesion; Authoring, Validation, WriteGate clusters; process traces for scenario create/validate/export flows) plus direct source and documentation review.
**Caveat:** The GitNexus index was 25 commits behind HEAD at review time. Findings were cross-checked against current source, but run `npx gitnexus analyze` before the next review round.

**Subject:** `Game-Requirements/requirements/11-Agentic-Mission-Editor.md` (the scenario editor's requirement doc, Status: Draft, last updated 2026-05-29) versus the as-built headless scenario editor and the approved GDD (`design/gdd/agentic-mission-editor.md`).

**Verdict in one sentence:** The implementation and the GDD have moved well ahead of requirement doc 11 — the doc lacks the ID/AC/status conventions the corpus now uses (docs 01 and 21), mis-describes decisions the design review already settled, and says nothing about several features that have already shipped headless.

---

## 1. The scenario editor as built

### 1.1 Architecture

A headless, file-based editor split across two assemblies, driven by CLI verbs that MCP hosts invoke. There is no interactive/GUI editor.

| Layer | Location | Role |
|---|---|---|
| Domain + mutation API | `src/ProjectAegis.Data/Scenario/Authoring/` | `ScenarioDocumentEditor` (CRUD, edit-version guard, snapshot/rollback, AI/umpire stubs) |
| Validation | `src/ProjectAegis.Data/Validation/` | `ScenarioValidationEngine`, `Rules/ValidationRules.cs`, `ScenarioValidationExportGate` |
| Tool surface | `src/ProjectAegis.MissionEditor.Cli/` + `tools/mission-editor/mcp-tools.json` | Verb dispatch (`Program.cs`), MCP manifest (schemaVersion 2) |
| Tests | `src/ProjectAegis.Data.Tests/Scenario/`, `src/ProjectAegis.MissionEditor.Cli.Tests/` | Round-trip, edit-version conflict, manifest-verb assertions |

### 1.2 What is solidly implemented

- **Document model:** scenario metadata (`DbRef`, `DbSnapshotId`, `TlBranch`, `Seed`, `PolicyId`, `EditVersion`, `MaxTechnologyLevel`, `UnitReadiness`, `NearFutureUnits`) + missions of three types — Patrol, Strike, Ferry (`ScenarioDocumentDto.cs`, `ScenarioMetadataDto.cs`). Canonical camelCase indented JSON on disk; tolerant loader (comments, trailing commas).
- **CRUD:** `CreateNew`, `AddPatrolMission`/`AddStrikeMission`/`AddFerryMission`, `Update*Mission`, `TryRemoveMission`, `Load`/`Save` (`ScenarioDocumentEditor.cs`).
- **Optimistic concurrency:** integer `EditVersion` checked by `ScenarioEditVersionGuard`; mismatch throws `ScenarioEditConflictException` (code `CONFLICT`, carries current version + SHA-256 file hash); every mutation increments the version and re-hashes. Covered by tests including stale-version → conflict exit code.
- **Validation & export gating:** ~12 deterministic rules (TL-branch presence/validity/snapshot match, release-train match, db-ref resolution, mission-has-units, patrol zone ≥3 waypoints, strike-has-targets, ferry destination, air-ready launch, ferry & strike reachability via Haversine vs combat radius, incompatible host, broken ref). Findings severity-sorted with stable `ReportHash`; export blocked at Error severity (`ScenarioValidationExportGate.cs`, `ValidationConfig.cs`); `scenario_export_brief` refuses to write unless validation passes.
- **Tool surface:** 18 scenario/mission verbs dispatched in `Program.cs`, including `scenario_create`, `scenario_validate`, `scenario_export_brief`, `scenario_simulate_sample`, `scenario_publish`, `scenario_migrate_preview`, `scenario_umpire_snapshot`, `scenario_ai_scaffold`, `scenario_event_trace`, `mission_add_patrol`, `mission_add_strike`, `mission_update_patrol`, `mission_update_strike`, `mission_delete`, `mission_plan_suggest`. `McpToolsManifestTests.cs` asserts required verbs exist in the manifest.

### 1.3 What is absent or stubbed

| Area | State | Evidence |
|---|---|---|
| Sides/factions, unit placement, map/geo editing | **Absent.** Units/targets/bases are string IDs only; the only geo data is patrol waypoint lat/lon | `ScenarioDocumentDto.cs` |
| Events & triggers | **Stub.** `EventIds`/`AddEvent` store strings; `scenario_event_trace` emits canned text; no trigger/condition/action model | `ScenarioDocumentEditor.cs:21-34` |
| Undo/rollback | **Not wired.** In-memory `Dictionary` snapshot on a single editor instance; not persisted, not reachable from any CLI verb | `ScenarioDocumentEditor.cs:262-281` |
| Ferry missions | **Half-wired.** Domain add/update + validation exist, but no CLI verb and no MCP tool (only patrol/strike exposed) | `Program.cs`, `mcp-tools.json` |
| AI / umpire / red-team / TCA analysis | **Demonstrative stubs** emitting fixed observable strings ("keyword heuristics", "Headless NL plan stub") | `AiAuthoringServices`, `MissionPlanSuggestCommand` |
| `IncompatibleHostRule`, `BrokenRefRule` | Self-described "simplistic"/"demo" (broken-ref only fires on a `ref:` prefix convention) | `ValidationRules.cs:374-404` |
| DB migration preview | Keys "upgrade" off substrings (`next`/`v2`/`upgrade`) against fixtures; real migration work in flight in `.worktrees/track25-scenario-db-migration` | `PreviewDbMigration` |
| Scenario document schema | **No formal JSON Schema** for the document (the schema in `mcp-tools.json` covers tool *inputs* only) | — |
| Example scenarios | **None committed.** `data/scenarios/` holds only `*.policy.json`; the document format appears only in test temp files | `data/scenarios/` |

---

## 2. Requirements-vs-reality gap analysis

| # | Gap | Evidence | Affected doc-11 section |
|---|---|---|---|
| G1 | No requirement IDs — FR groups §1–10 only, P0/P1 tags buried in prose; nothing addressable like `PLE-1.1` (doc 21) or `DBI-2.4` (doc 06) | `21-Platform-Editor.md` vs doc 11 | Functional Requirements §1–10 |
| G2 | ACs 1–7 are narrative and partly untestable; review log flagged AC-2/4/6/7. GDD already carries testable AC-1…AC-12 with fixtures and test paths — never back-filled | `design/gdd/reviews/agentic-mission-editor-review-log.md`; `design/gdd/agentic-mission-editor.md` | Acceptance Criteria |
| G3 | No per-requirement status column and no implementation-mapping table (doc 21 has both); live status exists only in the index and implementation tracker | `21-Platform-Editor.md` "Implementation Mapping" | whole doc |
| G4 | Stale framing: blocking "Validation **Agent**" — design review re-cast it as a deterministic Validation **Engine**. Determinism contract (world-state hash, `fire_order`), `editVersion` conflict-reject semantics, the six concrete validation codes, and the TeleportUnit logged-transform live in the GDD but not in doc 11 | GDD + review log vs doc 11 §6 (Agentic authoring agents) | §5, §6, Data Model |
| G5 | Five Open Questions still "open" although several (fuel convention, agent terminology) are settled in the GDD; no ADR links (doc 21 links ADR-011) | doc 11 "Open Questions" vs GDD | Open Questions / Decisions Needed |
| G6 | Code shipped ahead of spec: umpire/adjudication snapshot, DB migration preview, publish manifest, event static analysis (TCA stub), live validation all exist headless but are absent or one-line in doc 11 | `Program.cs` verbs; `implementation-tracker-2026-07-01.md` §"Scenario editor program (post-S80)" | §5, §9, new sections needed |
| G7 | Research-identified requirement areas never landed: umpire workspace (turn snapshots, before/after diffs, freeze/step/inject/resume, role permissions), migration reversibility/rollback, live continuous validation, event static analysis (dead triggers, circular deps), command palette / multi-select / staged edit transactions, publish governance (semver, review gates, ORBAT provenance) | `docs/research/scenario-editor-research.md` | §2, §5, §9, NFR |
| G8 | Spec-silence on the concrete document format: no JSON Schema requirement, no committed example fixtures, no ferry tool-surface requirement (implementation gap G-ferry would have been caught by a spec) | §1.3 above | Data Model, §9 |
| G9 | Strength to preserve: CMO-manual traceability (§3.3.17, §4.1.5, §5.x, §7.1–7.3) is rated "Full" in `cmo-manual-traceability.md` — don't regress it while restructuring | `cmo-manual-traceability.md` | Traceability |

---

## 3. Improvement roadmap

### Phase 1 — Structural conformance to house style (doc 11 only)

1. **Introduce an `AME-x.y` ID namespace** across Functional Requirements §1–10, one ID per requirement statement (mirroring `PLE-x.y` in doc 21). Move inline P0/P1 tags into a Priority column.
2. **Rewrite Acceptance Criteria as testable checkbox ACs** with fixture/output/test-path bindings. Start by adopting the GDD's AC-1…AC-12 verbatim where they cover the same ground; replace untestable AC-2/4/6/7 ("feels like theater planning") with measurable proxies.
3. **Add an Implementation Mapping table** (Component → New/Reuse/Extend → Status) naming the real components: `ScenarioDocumentEditor`, `ScenarioEditVersionGuard`, `ScenarioValidationEngine` + rules, `ScenarioValidationExportGate`, the CLI verb list, `mcp-tools.json`.
4. **Add a Status footer and per-FR status linkage** to the implementation tracker, as doc 21 does.

### Phase 2 — Back-fill approved design decisions

5. Replace "Validation Agent" with **deterministic Validation Engine** everywhere; keep "agents" only for the non-blocking advisory roles.
6. Import the GDD's **determinism contract** (world-state hash, `fire_order` ordering) and **`editVersion` optimistic-concurrency semantics** (conflict-reject with `CONFLICT` code + file hash) as normative requirements — they are implemented and tested but unspecified.
7. Specify the **six validation codes** and the **TeleportUnit logged-transform** rule from the GDD.
8. **Resolve the five Open Questions into ADRs** (use the project's `/architecture-decision` skill) and link them from doc 11 the way doc 21 links ADR-011. Where the GDD already decided (fuel convention, agent terminology), the ADR records the decision; the rest get explicit owner + due date.

### Phase 3 — Spec what shipped, flag what didn't

9. Add FR subsections (with `AME-*` IDs) for the shipped-but-unspecified capabilities: **umpire/adjudication workspace** (snapshot, diff, freeze/step/inject/resume, role permissions), **DB migration preview + reversibility/rollback persistence**, **publish manifest & governance** (semver, review gate, provenance), **live/continuous validation during authoring**, **event static analysis** (dead triggers, unreachable states, circular deps).
10. Require a **formal JSON Schema for the scenario document** (currently only tool inputs are schematized) and **committed example scenario fixtures** under `data/scenarios/`.
11. Add an explicit **tool-surface requirement** enumerating the required CLI/MCP verbs per mission type — this makes the missing ferry verb (`mission_add_ferry`/`mission_update_ferry`) a visible spec violation rather than a silent gap. Same for undo/rollback not being wired to the CLI.
12. Mark **stub-vs-real status honestly** per feature (events/triggers, AI authoring, umpire, TCA analysis, demo validation rules) so the doc distinguishes "specified and shipped" from "specified, demonstrative stub only".
13. Explicitly **scope or phase the unbuilt pillars** — sides/factions, unit placement, map-first geo editing. Doc 11 §2 currently implies map-first authoring exists; either phase it (Phase 2/3 delivery) or re-scope the MVP text to the headless reality.

### Phase 4 — Corpus hygiene

14. Update `Game-Requirements-Index.md` (doc 11 status) and confirm `cmo-manual-traceability.md` rows survive the restructure (G9).
15. Update the implementation tracker to reference the new `AME-*` IDs.
16. Re-run `npx gitnexus analyze` so the next review works from a current index.

---

## 4. Priority and effort

| Phase | Priority | Effort | Rationale |
|---|---|---|---|
| 1 — Structural conformance | **P0** | ~½ day | Unblocks all traceability; pure editing, no decisions needed |
| 2 — Back-fill decisions | **P0** | ~½ day + ADR reviews | Decisions already made; doc is currently *wrong*, not just incomplete |
| 3 — Spec what shipped | **P1** | 1–2 days | Requires reading shipped code (inventory above gives the map); some scoping judgment |
| 4 — Corpus hygiene | **P1** | ~1 hour | Mechanical follow-through |

**Risks / open items**

- The `.worktrees/track25-scenario-db-migration` work may land mid-rewrite and change the migration-preview surface — coordinate Phase 3 item 9 with that track.
- Platform DB `combat_radius_nm` round-trip sign-off is still open in the review log and feeds the reachability rules; note it in doc 11's traceability rather than silently depending on it.
- Rewriting §6 (Agentic authoring agents) must not delete the aspirational P1/P2 agent features — recast them as advisory, keep the deterministic engine as the only blocking path.

---

**Sources:** `Game-Requirements/requirements/11-Agentic-Mission-Editor.md` · `Game-Requirements/requirements/21-Platform-Editor.md` · `Game-Requirements/requirements/01-Project-Overview.md` · `Game-Requirements/cmo-manual-traceability.md` · `Game-Requirements/implementation-tracker-2026-07-01.md` · `design/gdd/agentic-mission-editor.md` · `design/gdd/agentic-mission-editor-concept.md` · `design/gdd/reviews/agentic-mission-editor-review-log.md` · `docs/research/scenario-editor-research.md` · source under `src/ProjectAegis.Data/Scenario/Authoring/`, `src/ProjectAegis.Data/Validation/`, `src/ProjectAegis.MissionEditor.Cli/` · GitNexus graph (`cmano-clone`, clusters Scenario/Authoring/Validation/WriteGate).
