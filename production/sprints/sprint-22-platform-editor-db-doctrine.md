# Sprint 22 — Platform Editor Phase A Complete + DB Intelligence P1 + Doctrine Panel

**Dates:** 2026-06-23 → 2026-07-07  
**Trunk:** `main`  
**Predecessor:** Sprint 21 — MCP OSINT Tools + Cesium Production + Data P1 + Connector Polish

## Sprint Goal

Complete Platform Editor Phase A write-gate coverage and CLI verbs (Req 21), advance Database Intelligence P1 with platform+weapon import support (Req 06), and deliver the Unity Doctrine Inheritance Panel (Req 13).

## Capacity

- Total days: 10
- Buffer (20%): 2 days reserved for unplanned work
- Available: 8 days

## Tasks

### Must Have (Critical Path)

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|-------------|-------------------|
| S22-01 | Extend `PlatformWorkbookImporter` write-gate staging to cover Mounts, Loadouts, Magazines, Comms sheets — wire `IWriteGate.Propose*Batch` for 4 unsupported entity types; remove them from `UnsupportedChanges` | c-sharp-engineer / team-data | 3 | S21 done; `CatalogMount`/`CatalogLoadout`/`CatalogMagazineEntry`/`CatalogCommsBinding` types exist | Importer commits Mounts/Loadouts/Mags/Comms via gate; `PlatformWorkbookRoundTripTests` + `ImporterTests` pass; GitNexus impact on `CatalogWriteGate` checked (CRITICAL) before edit |
| S22-02 | Add CLI verbs `platform_export_xlsx` / `platform_import_xlsx` / `platform_diff_xlsx` to `ProjectAegis.MissionEditor.Cli` following `CatalogImportMarkdownCommand` pattern; update `mcp-tools.json` manifest + `McpToolsManifestTests` | c-sharp-engineer | 2 | S22-01 | Verbs execute (`dotnet run -- platform_export_xlsx`); manifest test passes; JSON output via `McpToolResult`; MCP tool schemas defined |
| S22-03 | Author `docs/architecture/adr-011-platform-editor-excel-roundtrip.md` — document locked decisions from Phase A (exporter, importer, write-gate staging pattern, ClosedXML adapter boundary, Phase B scope) | writer | 0.5 | S22-01 / S22-02 | File exists and is referenced in `Game-Requirements/requirements/21-Platform-Editor.md`; covers Phase A decisions |

### Should Have

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|-------------|-------------------|
| S22-04 | Extend `CmoMarkdownImporter` to parse platform + weapon/mount markdown sections; add `ProposePlatformBatch` to `IWriteGate` or reuse existing staging; update importer tests with Baltic fixture coverage | c-sharp-engineer / team-data | 3 | S22-01 (gate extension pattern established) | Platform + weapon entries parsed from markdown; `ProposePlatformBatch` stages correctly; orphan staging guard (DBI-1.4 AC); no regression on sensor import; `CatalogWriteGate` GitNexus impact checked |
| S22-05 | Unity Doctrine Inheritance Panel (Req 13) — WRA/ROE/EMCON fields bound to `ResolvedUnitPolicy` projection; `SetDoctrineOverride` command dispatched from panel; headless command round-trip test; ADR-010 command/projection seam | c-sharp-engineer / unity-specialist | 3 | ADR-010 accepted; `PolicyEvaluator` + `ResolvedUnitPolicy` headless done | `SetDoctrineOverride` dispatches deterministically headless; Unity panel binds projection correctly; PlayMode smoke passes; ZERO touch to `DelegationBridge` |

### Nice to Have

| ID | Task | Agent/Owner | Est. Days | Dependencies | Acceptance Criteria |
|----|------|-------------|-----------|-------------|-------------------|
| S22-06 | `IBalanceTelemetrySink` stub → real accumulator; ±8% win-rate flag behind `enableBalanceDrift` feature flag; advisory-only; golden-hash test | c-sharp-engineer | 2 | S22-04 (platform import needed for meaningful data) | Flag fires at ±8% threshold; no write-gate bypass; feature flag defaults `false`; golden-hash test passes |
| S22-07 | OSINT `OsintCatalogMapper` TL routing — consume `proposedTL`/`targetDoc` from proposals; tag `TrlLevel` + `branch` on staged bindings for doc 09/10 gates | c-sharp-engineer | 2 | S21 complete (IOsintConnector interface + MCP tools) | TL routing respected on staged bindings; no `IWriteGate` bypass (DBI-8.3 hard AC); determinism test passes |

## Carryover from Previous Sprint

| Task | Reason | New Estimate |
|------|--------|-------------|
| S21-03 Cesium production feed (if not fully verified) | S21 Cesium Editor PlayMode visual sign-off may need polish | 1 day |

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| `CatalogWriteGate` CRITICAL blast radius (18 impacted, 7 procs) on S22-01/S22-04 | High | High | `gitnexus impact CatalogWriteGate direction=upstream` before ANY edit; extend-only, no signature/behavior changes to existing callers |
| S22-04 platform staging introduces orphan rows violating DBI-1.4 | Medium | Medium | Enforce DBI-1.4 AC in tests; mirror sensor-import pattern exactly |
| S22-05 Unity Doctrine Panel routes through `DelegationBridge` | Medium | High | ZERO-touch `DelegationBridge` rule (CRITICAL, 77 upstream); route via command + `ResolvedUnitPolicy` projection only per ADR-010 |
| ADR-011 missing blocks design-review gate | Low | Medium | S22-03 is 0.5d — schedule day 1 of sprint |
| S22-04 deferred if S22-01 runs long | Medium | Low | S22-04 is Should Have; defer cleanly to S23 if needed |

## Dependencies on External Factors

- Sprint 21 must complete and merge before S22-01 begins (both touch `CatalogWriteGate` area)
- Tracker row 20 "Unity manual C2 sign-off (S19-01 pending)" is stale — sign-off passed at `7401fac` (13/13); update tracker before S22 gate
- Unity Editor local visual sign-offs for Cesium (S20 QA gap) should be captured in `production/qa/` before 2026-07-15 milestone review

## GitNexus Rules (Mandatory)

- **Before ANY symbol edit:** run `gitnexus__impact` upstream on target symbol; report callers/processes/risk
- **CRITICAL (extend-only):** `CatalogWriteGate` (18 impacted, 7 procs) — only add new `Propose*Batch` overloads; do not change existing signatures or behavior
- **ZERO touch:** `DelegationBridge` (CRITICAL, 77 upstream)
- After edits: `gitnexus__detect_changes repo=cmano-clone` before commit state
- Determinism: stable `OrderBy` on CanonicalId for all new batch proposals

## Parallel Worktree Layout (Suggested)

```
main
 ├── stack/sprint22/platform-editor-writegate   (S22-01 + S22-02 CLI verbs)
 ├── stack/sprint22/db-platform-import          (S22-04 CmoMarkdownImporter)
 └── stack/sprint22/doctrine-panel              (S22-05 Unity)
```

S22-03 (ADR-011) goes directly on main via a small doc PR.

## Quality Gates

```powershell
dotnet build ProjectAegis.sln
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj --filter "PlatformWorkbook|Importer|WriteGate|Catalog"
dotnet test src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj --filter Mcp
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter PlayModeSmokeHarnessTests
# S22-05 Doctrine Panel: headless command test + PlayMode smoke (ADR-010)
```

## Definition of Done

- [x] All Must Have tasks completed (S22-01, S22-02, S22-03 — 2026-06-17)
- [ ] All tasks pass acceptance criteria
- [ ] QA plan exists (`production/qa/qa-plan-sprint-22.md`)
- [ ] All Logic/Integration stories have passing unit/integration tests
- [ ] Smoke check passed (`/smoke-check sprint`)
- [ ] QA sign-off report: APPROVED or APPROVED WITH CONDITIONS (`/team-qa sprint`)
- [ ] No S1 or S2 bugs in delivered features
- [ ] Design documents updated for any deviations
- [ ] Code reviewed and merged

> ⚠️ **No QA Plan**: This sprint was started without a QA plan. Run `/qa-plan sprint`
> before the last story is implemented. The Production → Polish gate requires a QA
> sign-off report, which requires a QA plan.

## References

- Req 21 Platform Editor: `Game-Requirements/requirements/21-Platform-Editor.md`
- Req 06 Database Intelligence: `Game-Requirements/requirements/06-Database-Intelligence.md`
- Req 13 Doctrine ROE: `Game-Requirements/requirements/13-Doctrine-ROE-EMCON-WRA.md`
- ADR-010: `docs/architecture/adr-010-headless-first-command-driven-ui.md`
- Platform skeleton: `src/ProjectAegis.Data/Platform/` (`PlatformWorkbookImporter`, `PlatformWorkbookExporter`, `PlatformWorkbookDiff`, `PlatformWorkbookValidator`)
- CLI pattern: `src/ProjectAegis.MissionEditor.Cli/Commands/CatalogImportMarkdownCommand.cs`
- MCP manifest: `tools/mission-editor/mcp-tools.json`
- Write-gate: `src/ProjectAegis.Data/WriteGate/CatalogWriteGate.cs`
- Importer: `src/ProjectAegis.Data/Import/CmoMarkdownImporter.cs`
- Catalog row types: `src/ProjectAegis.Data/Catalog/` (`CatalogMount`, `CatalogLoadout`, `CatalogMagazineEntry`, `CatalogCommsBinding`)
- OSINT: `src/ProjectAegis.Data/Osint/OsintCatalogMapper.cs` (if exists), `OsintDigestRunner.cs`
- Prior sprint: `production/sprints/sprint-21-mcp-osint-cesium-data-polish.md`
- Tracker: `Game-Requirements/implementation-tracker-2026-06-04.md` (rows 06, 13, 21)

*Created following sprint-plan skill (Phase 0–6) + dispatching-parallel-agents for context gather. Lean review mode. PR-SPRINT skipped. QA plan required before implementation begins.*

## Closeout (2026-06-14)
All Must Have (S22-01/02/03) + Should Have (S22-04/05) completed and verified.
- Parallel worktrees + Graphite stacks: sprint22-platform-editor-writegate (d206018 polish), sprint22-db-platform-import (9059bb9), sprint22-doctrine-panel (s22-05 polish), stack/sprint22/adr-011 etc.
- Gates: dotnet build clean; 52/60/8 targeted (Data + PlayMode) + CLI smoke PASS; npx gitnexus detect_changes "No changes detected" (low/none risk) pre/post.
- GitNexus: impacts CRITICAL on CatalogWriteGate (extend-only only, 18-40 impacted/7-11 procs re-ran before edits/commits); ZERO DelegationBridge (enforced).
- Evidence: .worktrees/.../.agent-todo-s22-writegate.md + s22-04-todo.md full [OUTCOME]s (ACs met, determinism, DBI-1.4 guard, Baltic coverage); production/sprint-status.yaml (statuses done + closeout_note); production/retrospectives/retro-sprint-22-2026-06-14.md; ADR-011 Accepted.
- gt submit --stack executed from worktrees + main (long-ref Windows warnings on verbose polish expected/non-blocking; feat PRs + dashboard review).
- Nice-to-haves (22-6 balance-telemetry, 22-7 osint-tl-routing): backlog, stacks pre-created in gt ls for S23.
- Subagent delegation loop + main verify closed sprint cleanly. All kickoff risks mitigated. 100% core ACs + DoD evidence.

## Pre-Work (Mandatory Before Any Implementation)

1. `npx gitnexus analyze .` — refresh index after S21 merges
2. Impact analysis: `CatalogWriteGate` (CRITICAL), `PlatformWorkbookImporter`, `CmoMarkdownImporter`, `PolicyEvaluator` (for S22-05), `OsintCatalogMapper` (for S22-07)
3. Baseline gates: `dotnet build + test` (verify 403+ pass from S21)
4. Stale tracker fix: update row 20 (C2 sign-off passed S19-01)
5. Read this kickoff + S21 artifacts + Req 21/06/13 before coding

## QA Test Cases (generated by /qa-plan 2026-06-09 + back-fill per user approval)

*(Types inferred from acceptance criteria in this kickoff md + GDD ACs (no explicit `Type:` field in yaml or tables — gap noted in plan; declare before /dev-story). See `production/qa/qa-plan-sprint-22-2026-06-09.md` for full details, GDD PLE-*/DBI-*/AC1-6 mappings, GitNexus warnings, smoke scope (exact kickoff Quality Gates), and DoD. Back-filled from the approved plan for traceability (/dev-story, /code-review, /team-qa). Stories currently inline (no separate production/epics story-*.md per yaml "next_backlog" + kickoff); this section serves as the ## QA Test Cases equivalent. Update yaml via /story-done only.)*

### 22-1 / S22-01 Extend PlatformWorkbookImporter write-gate to Mounts/Loadouts/Magazines/Comms — Integration (primary; Logic secondary)
**Automated**:
- Importer commits Mounts/Loadouts/Mags/Comms via gate (kickoff AC + GDD 21 PLE-3.1 "Excel-originated changes reach live tables only after ApproveBatch"; PLE-3.2 bulk M change-log + one staging batch; 06 DBI-1.4 "no orphan staging rows"; DBI-3.1 referential).
- `PlatformWorkbookRoundTripTests` + `ImporterTests` pass (kickoff; GDD 21 PLE-1.1 deterministic export stable sort by (PlatformId, *Id), PLE-2.1 empty diff on roundtrip, PLE-2.2 stale snapshot reject).
- GitNexus impact on `CatalogWriteGate` (CRITICAL, extend-only) before edit (kickoff + AGENTS.md).
- PLE-4.2 mount/weapon incompatibility + magazine-over-capacity blocking findings; PLE-5.1-5.3 provenance roundtrip/normalize; PLE-6.1-6.3 CLI/MCP without auto-commit (GDD 21).
- DBI-2.1-2.2 enum/sanity (range≤0/Mach>25); DBI-7.1 Propose rejects empty; DBI-7.3 Canonical IDs stable (GDD 06).
**Edge cases** (from GDD + kickoff): empty batch (0 qty magazine); bad FK -> quarantine (PLE-2.3/DBI-3.4, never in sim export); bulk >10 or balanceCritical -> explicit ApproveBatch not auto (PLE-3.3/DBI-2.4); incompatible mount/weapon or depth > capacity (PLE-4.2); no CanonicalId on new Catalog* (determinism gap per prior validation — fix with stable OrderBy composite/Canonical per kickoff rule + DBI-1.2/7.3).
**Test file path**: `src/ProjectAegis.Data.Tests/Platform/PlatformWorkbookImporterTests.cs` + `WriteGate/CatalogWriteGateTests.cs` (extend); ~10-12 tests.
*Derived from acceptance criteria + GDD ACs; review GDD Formulas/Edge before writing tests.*

### 22-2 / S22-02 Add CLI verbs platform_export_xlsx / platform_import_xlsx / platform_diff_xlsx — Integration
**Automated**:
- Verbs execute (`dotnet run -- platform_export_xlsx`); `McpToolsManifestTests` pass; JSON via `McpToolResult`; MCP schemas defined (kickoff AC + GDD 21 PLE-6.1-6.3).
- Same IWriteGate path (no auto-commit, PLE-6.3).
**Edge cases**: bad snapshot, large workbook, manifest drift.
**Test file path**: `src/ProjectAegis.MissionEditor.Cli.Tests/McpToolsManifestTests.cs` (extend); 4-6 tests + CLI smoke.
*Derived...*

### 22-3 / S22-03 Author ADR-011 platform-editor-excel-roundtrip — Config/Data
**Manual** (no automated):
- ADR exists at `docs/architecture/adr-011-platform-editor-excel-roundtrip.md` and referenced in `Game-Requirements/requirements/21-Platform-Editor.md`; covers Phase A decisions (exporter, importer, write-gate staging, ClosedXML boundary, Phase B scope) (kickoff AC).
**Evidence**: doc review + sign-off (writer / lead-programmer).
**Checklist**:
- [x] ADR exists and referenced
- [x] Covers locked decisions from GDD 21 + sprint kickoff (Excel round-trip, write-gate only, Phase A scope, governance)
- [x] No contradictions with DoD/Quality Gates/GitNexus rules in kickoff
*Manual only per type.*

### 22-4 / S22-04 Extend CmoMarkdownImporter to platform + weapon/mount entries — Integration (Logic for parse + orphan guard)
**Automated**:
- Platform + weapon/mount parsed from markdown; `ProposePlatformBatch` stages correctly; no sensor regression; orphan staging guard (DBI-1.4 AC explicit); CatalogWriteGate impact checked (kickoff + 06/21).
- DBI-1.1/1.4 (post-commit GetSorted* reflects fixed order; no orphan); DBI-7.1 (Propose rejects empty); DBI-8.3 (no IWriteGate bypass).
**Edge cases**: orphan guard (DBI-1.4), bad platform/weapon compat (PLE-4.2), TRL/provenance on new entries.
**Test file path**: `src/ProjectAegis.Data.Tests/Import/CmoMarkdownImporterTests.cs` (extend with Baltic for platform); 6-8 tests.
*Derived...*

### 22-5 / S22-05 Unity Doctrine Inheritance Panel (Req 13) — UI (panel bind to projection) + Integration (dispatch)
**Automated** (headless/PlayMode extension):
- `SetDoctrineOverride` dispatches deterministically headless (command round-trip per ADR-010); PlayModeSmokeHarnessTests pass; ZERO touch to DelegationBridge in all code/tests (kickoff AC + GDD 13 AC1-6 + inheritance model).
- Snapshot immutable except logged PolicyUpdate; FireAbortReason explain; "SetDoctrineOverride" + projection bind.
**Edge cases** (GDD 13): policy change during replay (proposed yes + logged); inheritance order strict (unit > embarked > mission > group > side > scenario); Cautious agent respects hold fire with FireAbortReason (AC2).
**Test file path**: Extend `src/ProjectAegis.Delegation.UnityAdapter.Tests/.../PlayModeSmokeHarnessTests.cs` + new headless command roundtrip (ADR-010 + kickoff); 5-7 tests.
**Manual** (UI/Visual + ZERO touch verification):
**Verification method**: Playtest session + screenshot/video of panel + command dispatch; code review + gitnexus impact for ZERO touch.
**Who must sign off**: unity-specialist / c-sharp-engineer / qa-lead (kickoff DoD + team-unity).
**Evidence**: video of panel binding ResolvedUnitPolicy, SetDoctrineOverride dispatch in headless + PlayMode, inheritance diagram visible, no regression on existing C2/DelegationBridge flows (OnEnable, RunBatch); PlayMode smoke pass log.
**Checklist**:
- [ ] WRA/ROE/EMCON fields bind to ResolvedUnitPolicy projection (kickoff AC + GDD 13 inheritance + AC1-2)
- [ ] `SetDoctrineOverride` dispatched from panel, deterministically headless (AC + GDD 13 AC6, ADR-010)
- [ ] PlayMode smoke passes (kickoff Quality Gates + GDD 13 ACs)
- [ ] ZERO touch to `DelegationBridge` (CRITICAL 77 upstream per gitnexus; no new direct calls/mutations; route via command + projection per ADR-010 + kickoff risk)
- [ ] Inheritance visual (unit/mission/side) correct and explainable (GDD 13 AC1, FireAbortReason)
- [ ] No S1/S2 bugs; no policy bypass (GDD 13 agent integration, "My Weapon Won’t Fire" explainability)
*If subjective ("feels"), supplement with benchmark (e.g. dispatch < X ms headless).*
*See plan for full GDD 13 AC1-6 + inheritance order + non-func (determinism O(1) per mount, 5k+ units, localization of FireAbortReason, identical snapshot = identical denial).*

### 22-6 / S22-06 IBalanceTelemetrySink real accumulator + win-rate flag — Logic
**Automated**:
- Real accumulator (no stub); flag fires at ±8% threshold (DBI-5.3; tuneable, ≥500 runs?); feature flag `enableBalanceDrift` defaults false; no write-gate bypass (DBI-5.2/5.1 advisory only); golden-hash test passes (kickoff + 06 DBI-5).
**Edge cases**: threshold exactly at band; no write-gate side-effect (DBI-5.1/5.2).
**Test file path**: `src/ProjectAegis.Data.Tests/Telemetry/` (new/extend); 4-6 tests.
*Derived... (DBI-5 P0: no auto-adjust, advisory only, never bypass gate).*

### 22-7 / S22-07 OSINT OsintCatalogMapper TL routing — Integration
**Automated**:
- TL routing (`proposedTL`/`targetDoc` → TrlLevel + branch on staged bindings); no IWriteGate bypass (DBI-8.3 hard AC); determinism test passes (stable OrderBy) (kickoff + 06 DBI-8.1/8.3).
**Edge cases**: rejected TL never promotes; stable OrderBy.
**Test file path**: `src/ProjectAegis.Data.Tests/Osint/OsintCatalogMapperTests.cs` (extend); 3-5 tests.
*Derived... (DBI-8.1 orchestrator fixed order returns Passed only if all pass; 8.3 no bypass).*

*(End of back-filled QA Test Cases. Full automated/manual details, estimated counts, GDD cross-refs (PLE-1.1–6.3, DBI-1.1–8.1, AC1-6 + inheritance), GitNexus (CatalogWriteGate HIGH extend-only 7/3 procs, DelegationBridge CRITICAL ZERO 77/19 direct, IWriteGate HIGH, detect 34 files high risk on catalog/reader dirt), smoke (kickoff 4x dotnet + 1-7 list), playtest (S22-05 only), and DoD (kickoff 9-item + qa-plan additions) are in the companion `production/qa/qa-plan-sprint-22-2026-06-09.md`. Reconcile current platform-editor dirt (new Catalog* types, 007 migration, 21-Req doc, etc. overlapping S22-01/04) + stale index + determinism gaps (no CanonicalId on new Catalog*) before impl. Use suggested `stack/sprint22/*` worktrees for parallel slices after .gitignore fix. All per qa-plan + team-qa skills, AGENTS.md, sprint kickoff.)*
