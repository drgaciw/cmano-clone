# Sprint 22 Implementation Plan — Platform Editor Phase A + DB Intelligence P1 + Doctrine Panel

**Sprint:** 22  
**Goal:** Complete Platform Editor Phase A write-gate coverage and CLI verbs (Req 21), advance Database Intelligence P1 with platform+weapon import support (Req 06), and deliver the Unity Doctrine Inheritance Panel (Req 13).  
**Dates:** 2026-06-23 → 2026-07-07  
**Trunk:** `main`  
**Predecessor:** Sprint 21 (complete)  
**Source:** `production/sprints/sprint-22-platform-editor-db-doctrine.md` (kickoff) + `production/qa/qa-plan-sprint-22-2026-06-09.md`

## GitNexus Mandatory Rules (from kickoff)

**Before ANY symbol edit:**
- Run `npx gitnexus impact CatalogWriteGate --direction upstream` (CRITICAL, 7 impacted/3 procs, extend-only)
- Run `npx gitnexus impact IWriteGate --direction upstream` (HIGH)
- Run `npx gitnexus impact DelegationBridge --direction upstream` (CRITICAL, 77 upstream/19 direct/5 procs — ZERO touch)
- Run `npx gitnexus impact PlatformWorkbookImporter --direction upstream`
- Run `npx gitnexus impact CmoMarkdownImporter --direction upstream`
- Run `npx gitnexus impact PolicyEvaluator --direction upstream`
- Run `npx gitnexus impact OsintCatalogMapper --direction upstream`
- After edits: `npx gitnexus detect_changes --repo cmano-clone` before commit

**Env note:** `npx gitnexus analyze` currently fails on this host (native tree-sitter-dart build). Impacts/detect must be run in isolated worktrees or noted as env limitation. All prior sprints documented this and proceeded with context + manual verification.

**Worktree strategy (recommended):**
- `stack/sprint22/platform-editor-writegate` → S22-01 + S22-02
- `stack/sprint22/db-platform-import` → S22-04
- `stack/sprint22/doctrine-panel` → S22-05
- `stack/sprint22/balance-telemetry` → S22-06
- `stack/sprint22/osint-tl-routing` → S22-07

## Stories (from sprint-status.yaml + kickoff)

### Must Have (Critical Path)

**S22-01: Extend PlatformWorkbookImporter write-gate to Mounts/Loadouts/Magazines/Comms**
- Priority: must-have
- Estimate: 3 days
- Blocker: none
- Task: Wire `IWriteGate.Propose*Batch` for 4 unsupported entity types (`CatalogMount`, `CatalogLoadout`, `CatalogMagazineEntry`, `CatalogCommsBinding`); remove from `UnsupportedChanges`. Importer commits via gate. `PlatformWorkbookRoundTripTests` + `ImporterTests` pass. GitNexus impact on `CatalogWriteGate` checked (CRITICAL) before edit.
- Acceptance: Importer commits Mounts/Loadouts/Mags/Comms via gate; tests pass; extend-only, no signature changes to `CatalogWriteGate`.

**S22-02: Add CLI verbs platform_export_xlsx / platform_import_xlsx / platform_diff_xlsx**
- Priority: must-have
- Estimate: 2 days
- Blocker: S22-01
- Task: Add verbs to `ProjectAegis.MissionEditor.Cli` following `CatalogImportMarkdownCommand` pattern; update `mcp-tools.json` + `McpToolsManifestTests`.
- Acceptance: Verbs execute (`dotnet run -- platform_export_xlsx`); manifest test passes; JSON output via `McpToolResult`; MCP tool schemas defined.

**S22-03: Author ADR-011 platform-editor-excel-roundtrip**
- Priority: must-have
- Estimate: 0.5 days
- Blocker: none
- Status: **COMPLETE** (per `progress.md` — file exists at `docs/architecture/adr-011-platform-editor-excel-roundtrip.md`, referenced in Req 21, covers Phase A decisions, write-gate pattern, ClosedXML, determinism, DBI-2.4 threshold).

### Should Have

**S22-04: Extend CmoMarkdownImporter to platform + weapon/mount entries**
- Priority: should-have
- Estimate: 3 days
- Blocker: S22-01
- Task: Parse platform + weapon/mount markdown sections; add `ProposePlatformBatch` to `IWriteGate` or reuse staging; update importer tests with Baltic fixture. No regression on sensor import.
- Acceptance: Platform + weapon entries parsed; `ProposePlatformBatch` stages correctly; orphan staging guard (DBI-1.4); GitNexus impact checked.

**S22-05: Unity Doctrine Inheritance Panel (Req 13)**
- Priority: should-have
- Estimate: 3 days
- Blocker: none
- Task: WRA/ROE/EMCON fields bound to `ResolvedUnitPolicy` projection; `SetDoctrineOverride` command dispatched from panel; headless command round-trip test; ADR-010 command/projection seam. ZERO touch to `DelegationBridge` (CRITICAL 77 upstream per gitnexus).
- Acceptance: `SetDoctrineOverride` dispatches deterministically headless; Unity panel binds projection; PlayMode smoke passes; ZERO touch verified.

### Nice to Have

**S22-06: IBalanceTelemetrySink real accumulator + win-rate flag**
- Priority: nice-to-have
- Estimate: 2 days
- Blocker: S22-04
- Task: Real accumulator; ±8% win-rate flag behind `enableBalanceDrift` feature flag (advisory-only); golden-hash test.
- Acceptance: Flag fires at ±8% threshold; no write-gate bypass; feature flag defaults `false`; golden-hash test passes.

**S22-07: OSINT OsintCatalogMapper TL routing**
- Priority: nice-to-have
- Estimate: 2 days
- Blocker: S21 complete
- Task: Consume `proposedTL`/`targetDoc` from proposals; tag `TrlLevel` + `branch` on staged bindings for doc 09/10 gates.
- Acceptance: TL routing respected on staged bindings; no `IWriteGate` bypass (DBI-8.3); determinism test passes.

## Execution Workflow (Subagent-Driven Development)

1. **Pre-work (mandatory):** GitNexus impacts on all critical symbols (document env limitation if analyze fails). Update tracker baseline. Create worktrees.
2. **Dispatch parallel implementers** for independent stories (S22-01, S22-03 already done, S22-05, S22-06, S22-07 can run in parallel after S22-01 baseline).
3. **Two-stage review per task:** Spec compliance → Code quality.
4. **Loop until all stories COMPLETE** (status: done in sprint-status.yaml + evidence in production/qa/).
5. **Final:** Update `sprint-status.yaml`, run full smoke, `/story-done` on all, close sprint.

**Parallel dispatch groups:**
- Group A (after S22-01 gate baseline): S22-02 (CLI verbs), S22-04 (CmoMarkdown), S22-05 (Doctrine panel)
- Group B: S22-06 (Balance), S22-07 (OSINT TL)
- S22-03: already complete — skip

**Definition of Done (per kickoff):**
- All Must ACs met + tests green
- GitNexus impacts/detect run (or env note)
- `/story-done` evidence in production/qa/
- Tracker + sprint-status.yaml updated
- No bypass of write-gate or DelegationBridge

## Related

- Kickoff: `production/sprints/sprint-22-platform-editor-db-doctrine.md`
- QA Plan: `production/qa/qa-plan-sprint-22-2026-06-09.md`
- QA Sign-off: `production/qa/qa-signoff-sprint-22-2026-06-09.md`
- ADR-011: `docs/architecture/adr-011-platform-editor-excel-roundtrip.md`
- Req 21: `Game-Requirements/requirements/21-Platform-Editor.md`
- Tracker: `Game-Requirements/implementation-tracker-2026-06-04.md` (row 21)