# Plan: Improve the Mission Editor Requirements (doc 11) — Reconcile Spec vs Reality

## Context

Doc `Game-Requirements/requirements/11-Agentic-Mission-Editor.md` was rewritten on 2026-07-01 with AME-IDs, AC-1–12, and ADR links — a big step up. But this session's QA implementation pass proved parts of it still describe a system that doesn't exist, and the doc hasn't absorbed what shipped today. Direction chosen: **reconcile both ways** — correct requirements to match shipped code exactly, and preserve the aspirational data model as clearly phased future requirements (asked the user; no response after 60s, proceeding with the recommended option).

Verified discrepancies (Explore agent, 2026-07-03):

1. **AME-2.1 (doc line 97)** promises a `*.aegis-scenario` ZIP package (manifest.json + scenario.json + cache.bin). Reality: a single plain JSON file. Schema `data/scenarios/scenario-document.schema.json` and `ScenarioDocumentJsonWriter` both model the single-file form.
2. **AME-2.2 (line 98)** requires metadata keys `title, description, author, schemaVersion, dbRef, seed, editVersion`. Reality (`src/ProjectAegis.Data/Scenario/Authoring/ScenarioMetadataDto.cs:3-28`, 9 props): `dbRef, dbSnapshotId, editVersion, seed, policyId, tlBranch, unitReadiness, maxTechnologyLevel, nearFutureUnits`. No title/description/author/schemaVersion anywhere. Schema matches the DTO exactly (verified), so the schema and DTO agree — only AME-2.2 is wrong.
3. **AME-2.3 (line 99)** requires top-level nodes `features, sides[], orbat, referencePoints[], missions[], operationsTimeline[], events[], variables{}, editorState`. Reality (`ScenarioDocumentDto.cs:4-9`): `metadata` + `missions[]` only. `EventIds` exists only on `ScenarioDocumentEditor` (line 22), is not serialized (`ToDto()` lines 61-66 drops it), and is lost on save/load.
4. **All 12 ACs are unchecked** (lines 288-299), but today's QA pass delivered green automated coverage for AC-1, AC-3, AC-12, and (with a caveat) AC-6. AC-6's literal "one hunk" claim is wrong: every mutation bumps `editVersion`, so a single-field edit is 2 hunks (documented in `tools/ci/smoke-ac6.sh`).
5. **AME-8.4 (ferry CLI verb) gap is now closed** — `mission_add_ferry`/`mission_update_ferry` shipped today — but doc 11 still marks it GAP, and AC-5's "blocked" status is stale.
6. **AME-8.5 (undo)** has an unresolved persistence design question (in-memory snapshot vs cross-process CLI) with no ADR tracking it.
7. **ADR-013 (CMO import), ADR-015 (transparency labeling), ADR-017 (editor topology)** are still Proposed; doc 11's Open Questions table doesn't say which decisions remain open vs settled.

Intended outcome: doc 11 becomes trustworthy — every P0 requirement describes shipped, schema-backed reality; every aspirational item is explicitly phased with its own ID; AC status reflects actual test coverage; open decisions are visibly tracked.

## Changes

### 1. Rewrite AME-2.1–2.3 as v1-reality + phased-future pairs
File: `Game-Requirements/requirements/11-Agentic-Mission-Editor.md` (§2, lines ~95-105)

- **AME-2.1** → v1 (P0, Shipped): canonical single-file JSON scenario document, normative shape = `data/scenarios/scenario-document.schema.json` (draft 2020-12), conformance enforced by `ScenarioDocumentSchemaConformanceTests.cs`. Move the `*.aegis-scenario` ZIP package to a new **AME-2.7 (Phase 2)**.
- **AME-2.2** → v1 (P0, Shipped): list the real 9 metadata keys from `ScenarioMetadataDto` with one-line meanings (note `editVersion` optimistic-lock semantics stay). Move `title/description/author/schemaVersion` to a new **AME-2.8 (Phase 2, descriptive metadata)** — note `schemaVersion` becomes required when the ZIP package (AME-2.7) lands.
- **AME-2.3** → v1 (P0, Shipped): top-level nodes are `metadata` + `missions[]` (typed Patrol/Strike/Ferry with `if/then` discrimination per the schema). Move `sides[]/orbat/referencePoints[]/operationsTimeline[]/events[]/variables{}/editorState` to a new **AME-2.9 (Phase 2, rich scenario document)** — explicitly state today's `EventIds` is unpersisted editor-session state, cross-referencing `StubScopePinTests.cs`.

### 2. Update Acceptance Criteria to match verified coverage
Same file, AC section (lines ~288-299):

- Check **AC-1, AC-3, AC-12** — each now has passing automated tests (`ScenarioValidationEngineTests.cs`, `SaveVsExportGateTests.cs`); cite the test file next to each checkbox.
- **AC-6**: amend wording from "exactly one hunk" to "≤2 hunks (field + mandatory editVersion bump), no key reordering, byte-stable re-serialization", then check it, citing `tools/ci/smoke-ac6.sh`.
- **AC-5**: remove the "blocked on ferry verb" note — annotate "unblocked 2026-07-03 (AME-8.4 shipped); fixture authoring pending".
- **AC-2**: split into AC-2a (golden-hash mechanism — exists) and AC-2b (specific `fire_order` array + `SEED=/HASH=` stdout format — unverified, Phase 2). Leave both unchecked until a test pins them.
- **AC-7** annotate: blocked on `ExplainEventTrace` stub (behavior pinned by `StubScopePinTests.cs`). **AC-11** annotate: TeleportUnit is unimplemented — a build task, not a test task.

### 3. Close the AME-8.4 gap, track the AME-8.5 decision
- **AME-8.4**: change GAP → Shipped; reference `MissionAddFerryCommand.cs`/`MissionUpdateFerryCommand.cs`, the `mcp-tools.json` entries, and `MissionAddFerryCommandTests.cs`.
- **AME-8.5**: keep GAP, but create **`docs/architecture/adr-018-undo-snapshot-persistence.md`** (Status: Proposed) using the existing template `.claude/docs/templates/architecture-decision-record.md`, capturing the in-memory-vs-persisted snapshot options from the QA plan gap item #3; link it from AME-8.5 and the Open Questions table.

### 4. Open Questions / decision-status table refresh
- Add a Status column entry per row: ADR-014 and ADR-016 = Accepted (settled); ADR-013, ADR-015, ADR-017 = **Proposed — decision pending**; new row for ADR-018.
- Do **not** flip any ADR from Proposed to Accepted — that's the user's call; the doc just makes the pending set visible.

### 5. Corpus hygiene (small row edits)
- `Game-Requirements/Game-Requirements-Index.md` and `Game-Requirements/implementation-tracker-2026-07-01.md`: update doc-11 row (revision date 2026-07-03, ferry gap closed, AC coverage 4/12 automated).
- Implementation Mapping table inside doc 11: add rows for today's shipped artifacts (ferry verbs, schema conformance tests, save-vs-export tests, AC-6 smoke script, no-Lua gate, stub pins).

## Files touched
- `Game-Requirements/requirements/11-Agentic-Mission-Editor.md` (main edit)
- `docs/architecture/adr-018-undo-snapshot-persistence.md` (new)
- `Game-Requirements/Game-Requirements-Index.md`, `Game-Requirements/implementation-tracker-2026-07-01.md` (row updates)
- No source code, schema, or test changes — schema already matches the DTO.

## Verification
1. Grep doc 11 for duplicate/missing AME-IDs (`grep -oE 'AME-[0-9]+\.[0-9]+' | sort | uniq -d`) — new IDs 2.7/2.8/2.9 must be unique.
2. Every file path cited in the doc exists (`ls` spot-check of ~12 referenced paths).
3. Cross-check the AME-2.2 key list verbatim against `ScenarioMetadataDto.cs` and the schema's `required` array — all three must agree.
4. Checked ACs each cite a test file that exists and passed in today's run; no AC is checked without a green automated test behind it.
5. Markdown tables render (quick pandoc/`glow` or visual scan); ADR-018 follows the template section headings.
6. No commits — per CLAUDE.md, wait for explicit user instruction.
