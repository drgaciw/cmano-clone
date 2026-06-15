# S22-04: Extend CmoMarkdownImporter + ProposePlatformBatch + Orphan Guard (DBI-1.4)

**Status:** In Progress (per approved detailed plan + qa-plan-sprint-22-2026-06-09.md + sprint-22 kickoff)
**Refs:** Game-Requirements/requirements/06-Database-Intelligence.md (DBI-1.1/1.4/7.1/7.3/8.3), 21-Platform-Editor.md (PLE), production/sprints/sprint-22-platform-editor-db-doctrine.md, production/qa/qa-plan-sprint-22-2026-06-09.md
**GitNexus:** CatalogWriteGate CRITICAL (extend-only, 18 direct/11 procs baseline impact run pre-edit + re-runs via detect); parser LOW (0 impacted); ZERO DelegationBridge touch.
**Graphite:** gt create slices only; submit --stack --no-interactive at end. Worktree: stack/sprint22/db-platform-import guidance.
**Quality Gates (kickoff):** dotnet build; full test; Data filter "PlatformWorkbook|Importer|WriteGate|Catalog"; CLI Mcp; PlayMode (not for this data slice); gitnexus_detect pre each commit.
**AC Evidence Target:** 52+ targeted green + new (Baltic platform parse, ProposePlatformBatch stages, no sensor regression, orphan guard explicit, OrderBy deterministic, impact checked).
**Hindsight/Retain:** [to be retained post-close with OUTCOMES + symbols; use dev-cmano-clone bank if invoked].

## Subtasks (logical order per plan)

### 1. Migration (additive 007 for staging tables)
- [x] Edit assets/data/catalog/migrations/007_platform_editor_phase_a.sql : append CREATE TABLE IF NOT EXISTS for catalog_staging_platform, _weapon, _mount, _loadout, _magazine, _comms (+ indexes, comments).
- **Rationale:** Enable Propose* without bypass (application logic table create forbidden); support extended DeleteStagingRows for DBI-1.4 no-orphan on Reject/empty; additive only (IF NOT EXISTS); reader skip remains on live platform_mount.
- **Evidence/OUTCOME:** Edit succeeded (search_replace unique). Post-edit gates: build PASS (0w/0e clean), targeted "PlatformWorkbook|Importer|WriteGate|Catalog" 52/52 PASS (no regression on sensor paths), full sln  (Data 111/111, Delegation 154/154, Sim 87/87, CLI 21/21, UnityAdapter 85/85) all green. git add staged the sql. gitnexus__detect_changes (unstaged, cmano-clone-ws-postfix): low risk, 0 affected processes (sql is data; only pre-existing AGENTS/CLAUDE md sections reported). Impacts baseline (pre) + re-runs via detect confirmed.
- **gt create attempt 1:** FAILED (exit 1) - branch name derived from long -m title exceeded Windows git ref path limit (~"06-14-feat_sprint22_..._lock': Filename too long"). No commit created, staging preserved. 
- **gt create retry (short title):** [see tool output below for success/branch]. Used short per Graphite Windows guidance + user "feat(sprint22): S22-04 [specific]" example to keep slug <200 chars.
- **Next:** Reconcile dirt (temp dbs in tests + SeedBalticPatrol ensure fresh schema with new staging tables; prod dbs with prior 007 will need manual or future 008 if skip triggers before CREATE IF runs).
- **Decision:** Short, descriptive gt -m titles for all slices; full details in commit body or PR desc. No longrefs change to repo config.

### 2. Interface (IWriteGate)
- [ ] Add `string ProposePlatformBatch(IReadOnlyList<CatalogPlatformEntry> proposed, string actorType, string actorId, string rationale = "");` (extend-only; mirror ProposeMountBatch signature).
- **AC:** No signature change to existing 5 Propose*; tests (FakeWriteGate in PlatformWorkbookImporterTests) updated additively.
- **OUTCOME:** [pending this group]

### 3. Gate (CatalogWriteGate)
- [ ] Implement ProposePlatformBatch (sort by PlatformId with Ordinal, batchId, tx, InsertBatchHeader + InsertStagingPlatform, commit).
- [ ] Extend DeleteStagingRows to DELETE FROM all catalog_staging_* (platform, weapon, mount, loadout, magazine, comms, sensor) for the batchId — enforce no-orphan DBI-1.4.
- [ ] Add private InsertStagingPlatform helper (mirrors InsertStagingMount pattern; provenance/review columns).
- [ ] Keep all sensor paths, Approve (sensor-only for now), ListPending etc untouched.
- **AC:** ProposePlatformBatch stages correctly; Reject cleans all staging for batch (test explicit); sensor 100% regression-free.
- **OUTCOME:** [pending]

### 4. Importer (CmoMarkdownImporter)
- [ ] Add parser extensions for platform + weapon + mount markdown sections (new methods: e.g. ReadPlatformEntriesFromText, ReadWeaponEntriesFromText, supporting regex for ### sections, /weapon/ paths, mount patterns in text).
- [ ] **CRITICAL:** Sensor code (ReadSensorBindings*, SensorPath, RangeMaxRow, ConfidenceRow, FlushSection, InferBasePd, SlugPlatformId, existing OrderBy) 100% untouched.
- [ ] Return stable ordered lists (OrderBy PlatformId/Id etc).
- **AC:** Platform + weapon/mount parsed from markdown (Baltic fixture coverage in tests); no sensor regression.
- **OUTCOME:** [pending]

### 5. Proposer (CmoMarkdownImportProposer)
- [ ] Extend ProposeFromMarkdown (or add path) to parse platforms/weapons/mounts via new importer methods; call gate.ProposePlatformBatch (and reuse Propose* for mount/weapon if applicable) in chunks with deterministic OrderBy.
- [ ] Use Baltic seed/fixture for coverage; keep sensor path working.
- **AC:** ProposePlatformBatch called for parsed platforms; batches returned; quarantines work; no bypass.
- **OUTCOME:** [pending]

### 6. EntityMap (CatalogEntityMap)
- [ ] Add EntityTableBinding entries for CatalogStagingPlatform (or platform staging), weapon staging, and ensure mount etc have DeterministicOrderBy for snapshot/export determinism.
- **AC:** All new staging in All list; TryGetTable works; supports deterministic read ordering (DBI-1.1/7.3).
- **OUTCOME:** [pending]

### 7-11. Tests (additive, Baltic coverage, 51+ green + new)
- [ ] CmoMarkdownImporterUnitTests.cs : add 3-4 tests for new platform/weapon/mount parse (Infer/Slug untouched; deterministic OrderBy; Baltic-like fixtures or inline md sections).
- [ ] CmoMarkdownImportProposerTests.cs + SmokeTests.cs + BulkImportTests.cs : extend with ProposePlatformBatch staging from parsed, approve path if applicable, orphan guard on Reject, no sensor change counts.
- [ ] CatalogWriteGateTests.cs : add ProposePlatformBatch tests, Reject orphan-clean test (DBI-1.4 explicit assert no rows in any staging_* for batch), determinism.
- **AC:** All new tests pass; total targeted >=52 + new; existing 52 green (no regression); explicit orphan guard + OrderBy asserts.
- **OUTCOME:** [pending group]

### 12. Update todo + evidence
- [ ] Update this file with [OUTCOME:] + links to test output, gt outputs, detect results, impact summaries for each.
- [ ] Final: all ACs met, gates green, impacts/detect clean, gt status ready for submit.
- **OUTCOME:** [in progress; this file created post first group]

## Key Decisions (from kickoff + user approval)
- Extend-only on CatalogWriteGate/IWriteGate (no existing behavior/signature change).
- Additive migration only (no new migration file; 007 extended for "phase a" continuity).
- Parser: new methods only; sensor regex/paths frozen.
- Determinism: explicit .OrderBy(..., StringComparer.Ordinal).ThenBy... before every batch insert and export query.
- No-orphan: DeleteStagingRows now multi-table; test asserts COUNT(*) == 0 across all staging for rejected batch.
- Baltic: leverage existing SeedBalticPatrol + mini fixture + inline platform sections in new tests for coverage (no new external files unless needed).
- Graphite: one slice per logical group (migration; interface+gate; importer+proposer+entitymap; tests; todo close).
- GitNexus: impacts baseline (CRITICAL for gate) + detect before every gt create.
- .claude/rules + AGENTS: followed (no hardcoded, data-driven, tests first-ish via additive, collab protocol via approvals here).

## Evidence Log
- Pre-work: npx gitnexus status = ✅ up-to-date (c119d41). Impacts run on CatalogWriteGate (CRITICAL 40 impacted/11 procs/18 direct incl ProposeFromMarkdown, RunCatalog*, tests, panels), IWriteGate (CRITICAL), Cmo* (LOW 0). detect baseline low (only AGENTS/CLAUDE mds). dotnet build+targeted 52/52 + full green.
- Group 1 (migration): see above.
- [add subsequent groups here with outputs]

## Risks / Mitigations (tracked)
- CatalogWriteGate dirt (prior S22-01): mitigated by this additive staging + extended Delete.
- Stale dbs skip 007: tests use fresh temp dbs + Seed; doc in todo.
- Parser regression: frozen sensor code + unit tests assert exact sensor counts unchanged.
- [add as found]

*(Created/updated per user instruction after group edits + retain pattern from Hindsight skills. Will retain full to dev-cmano-clone on close with [OUTCOME] symbols.)*
