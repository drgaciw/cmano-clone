# Sprint 16 Closeout Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.
>
> **Story closure:** REQUIRED SUB-SKILL: Run `/story-done [path] --review full` after each story task block (spawns **QL-TEST-COVERAGE** + **LP-CODE-REVIEW** per `.claude/docs/director-gates.md`).

**Goal:** Close Sprint 16 (PR #69 merge + DATA P0 + backlog) to **100%** with refreshed PR-gate QA, complete `RunCatalog*` / `CatalogWrite*` CLI test coverage, and **full** story-done gates — not lean confirm.

**Architecture:** Sprint 16 is a **convergence sprint**: merge Wave 5/requirements program (PR #69) onto `main`, then verify DATA P0 slices DATA-0..DATA-3 landed on trunk. Catalog operations expose through `MissionEditor.Cli` (`catalog_write_propose`, `catalog_write_approve`, `catalog_entity_map`, `catalog_intelligence_run`, `catalog_import_markdown`) backed by `CatalogWriteGate` + `ICatalogReader` in `ProjectAegis.Data`. No new engage/delegation paths.

**Tech Stack:** .NET 8, NUnit/xUnit, `Microsoft.Data.Sqlite`, `MissionEditor.Cli`, GitNexus CLI, Hindsight (optional retain).

**Baseline (2026-06-09 confirm):** Sprint 16 marked complete @ **368** tests — [sprint-16-story-done-confirm-2026-06-09.md](../../production/qa/sprint-16-story-done-confirm-2026-06-09.md). **Gaps to 100%:** (1) no formal sprint plan file, (2) CLI tests missing for write-propose/approve/entity-map/intelligence, (3) P0 plan checkboxes stale, (4) story-done ran confirm doc not **full** director gates, (5) re-baseline on current `main` (**443** tests post S17–21).

**Sprint map:**

| Task ID | Deliverable | Story file(s) for `/story-done --review full` |
|---------|-------------|-----------------------------------------------|
| s16-pr-gate | PR merge gate QA | *(task-level — no story file; close via Task 6 closeout)* |
| s16-data-p0 | DATA-0..2 on main | `production/epics/platform-db-basepd-slice/story-001-catalog-basepd.md`, `story-002-catalog-json-import.md` |
| s16-data-3 | ScenarioPackage bind | `story-003-catalog-bulk-import.md`, `story-004-catalog-provenance.md` |
| s16-cli | Catalog CLI + tests | New: `production/epics/platform-db-basepd-slice/story-008-catalog-cli-write-gate.md` |
| s16-backlog | Backlog stub → closed | Update `production/sprints/sprint-16-backlog.md` |

---

## File map (this plan)

| File | Responsibility |
|------|----------------|
| `production/sprints/sprint-16-pr-data-p0.md` | **Create** — canonical Sprint 16 plan (replaces backlog-only stub) |
| `src/ProjectAegis.MissionEditor.Cli.Tests/CatalogWriteCommandTests.cs` | **Create** — propose → approve round-trip CLI tests |
| `src/ProjectAegis.MissionEditor.Cli.Tests/CatalogEntityMapCommandTests.cs` | **Create** — entity map JSON contract |
| `src/ProjectAegis.MissionEditor.Cli.Tests/CatalogIntelligenceRunCommandTests.cs` | **Create** — intelligence orchestrator CLI smoke |
| `production/qa/sprint-16-pr-gate-2026-06-08.md` | **Create** — refreshed PR gate on current `main` |
| `production/agentic/sprint-16-closeout-2026-06-08.md` | **Create** — program exit artifact |
| `docs/superpowers/plans/2026-05-30-database-intelligence-p0.md` | **Modify** — sync DATA-0..5 checkboxes to shipped state |
| `production/sprint-status.yaml` | **Modify** — tests_passed, evidence, `story_done_verdict: complete-full` |

---

## Task 0: Sprint 16 gap audit (read-only)

**Files:** None (audit only)

- [ ] **Step 1: Read sprint-status Sprint 16 block**

```powershell
Select-String -Path production/sprint-status.yaml -Pattern "sprint: 16" -Context 0,40
```

Expected: `status: complete`, tasks s16-pr-gate through s16-data-3 `done`; note `tests_passed: 368`.

- [ ] **Step 2: Compare to current main test count**

```powershell
dotnet test ProjectAegis.sln -v minimal
```

Expected: **443/443 PASS** (or current solution total — record in closeout doc).

- [ ] **Step 3: Inventory CLI catalog commands vs tests**

| CLI verb | Handler | Test file today |
|----------|---------|-----------------|
| `catalog_write_propose` | `CatalogWriteProposeCommand.Run` | **MISSING** |
| `catalog_write_approve` | `CatalogWriteApproveCommand.Run` | **MISSING** |
| `catalog_entity_map` | `CatalogEntityMapCommand.Run` | **MISSING** |
| `catalog_intelligence_run` | `CatalogIntelligenceRunCommand.Run` | **MISSING** |
| `catalog_import_markdown` | `CatalogImportMarkdownCommand.Run` | `CatalogImportMarkdownCommandTests.cs` ✓ |

- [ ] **Step 4: GitNexus pre-flight (CatalogWriteGate)**

```powershell
npx gitnexus impact CatalogWriteGate -d upstream -r cmano-clone
```

Report blast radius in PR/closeout body. **Expected:** CRITICAL — extend-only; no signature changes to existing callers.

---

## Task 1: Formal Sprint 16 plan file

**Files:**
- Create: `production/sprints/sprint-16-pr-data-p0.md`
- Modify: `production/sprints/sprint-16-backlog.md` (point to formal plan; mark themes done)

- [ ] **Step 1: Write sprint plan**

Create `production/sprints/sprint-16-pr-data-p0.md` with:

```markdown
# Sprint 16 — PR merge + DATA P0 + backlog

**Dates:** 2026-06-04  
**Status:** Complete (full closeout 2026-06-08)  
**Goal:** Merge PR #69; verify DATA P0 DATA-0..3 on main; ratify CLI catalog gate; close backlog.

## Must have

| ID | Task | Acceptance |
|----|------|------------|
| S16-01 | PR gate | 365+ tests, replay + PlayMode PASS — evidence doc |
| S16-02 | Merge PR #69 | main @ 810b8d7+ |
| S16-03 | DATA P0 DATA-1/2 | ICatalogReader + migrations on main |
| S16-04 | DATA-3 ScenarioPackage | ScenarioPackageLoader tests PASS |
| S16-05 | Catalog CLI tests | propose/approve/entity_map/intelligence_run |
| S16-06 | /story-done --review full | QL-TEST-COVERAGE + LP-CODE-REVIEW ADEQUATE |

## Verification

dotnet test ProjectAegis.sln
dotnet test src/ProjectAegis.Data.Tests --filter "Catalog|CatalogWrite|ScenarioPackage"
dotnet test src/ProjectAegis.MissionEditor.Cli.Tests --filter "Catalog"
```

- [ ] **Step 2: Update backlog stub header**

In `production/sprints/sprint-16-backlog.md`, set **Status:** Complete — see `sprint-16-pr-data-p0.md`.

- [ ] **Step 3: Commit**

```bash
git add production/sprints/sprint-16-pr-data-p0.md production/sprints/sprint-16-backlog.md
git commit -m "docs(sprint16): add formal PR+DATA P0 sprint plan"
```

---

## Task 2: CLI test — `catalog_write_propose` + `catalog_write_approve` round-trip

**Files:**
- Create: `src/ProjectAegis.MissionEditor.Cli.Tests/CatalogWriteCommandTests.cs`
- Reference: `src/ProjectAegis.MissionEditor.Cli/CatalogWriteProposeCommand.cs`, `CatalogWriteApproveCommand.cs`
- Reference: `src/ProjectAegis.Data.Tests/WriteGate/CatalogWriteGateTests.cs` (gate semantics)

- [ ] **Step 1: Write failing test**

```csharp
using System.Text.Json;
using ProjectAegis.Data.Catalog;
using ProjectAegis.MissionEditor.Cli;
using Xunit;

namespace ProjectAegis.MissionEditor.Cli.Tests;

public sealed class CatalogWriteCommandTests
{
    [Fact]
    public void catalog_write_propose_then_approve_commits_sensor_binding()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-cli-write-{Guid.NewGuid():N}.db");
        try
        {
            using (var proposeOut = new StringWriter())
            {
                Assert.Equal(0, CatalogWriteProposeCommand.Run(
                    dbPath, "u-cli-test", "radar-cli", 0.55, proposeOut));
                var proposeJson = proposeOut.ToString();
                Assert.Contains("\"ok\": true", proposeJson);
                using var doc = JsonDocument.Parse(proposeJson);
                var batchId = doc.RootElement.GetProperty("batchId").GetString();
                Assert.False(string.IsNullOrWhiteSpace(batchId));

                using var approveOut = new StringWriter();
                Assert.Equal(0, CatalogWriteApproveCommand.Run(dbPath, batchId!, approveOut));
                var approveJson = approveOut.ToString();
                Assert.Contains("\"ok\": true", approveJson);
                Assert.Contains("\"snapshotId\":", approveJson);
            }

            using var reader = new SqliteCatalogReader(dbPath, "cli-test");
            Assert.True(reader.TryGetBasePd("u-cli-test", "radar-cli", out var basePd));
            Assert.Equal(0.55, basePd, 3);
        }
        finally
        {
            if (File.Exists(dbPath)) File.Delete(dbPath);
        }
    }

    [Fact]
    public void catalog_write_approve_missing_db_returns_error()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-cli-missing-{Guid.NewGuid():N}.db");
        using var output = new StringWriter();
        Assert.Equal(1, CatalogWriteApproveCommand.Run(dbPath, "batch-nope", output));
        Assert.Contains("database_not_found", output.ToString());
    }
}
```

- [ ] **Step 2: Run test to verify it compiles and passes**

```powershell
dotnet test src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj --filter "FullyQualifiedName~CatalogWriteCommandTests" -v minimal
```

Expected: **PASS** (commands already implemented — test documents contract).

- [ ] **Step 3: Commit**

```bash
git add src/ProjectAegis.MissionEditor.Cli.Tests/CatalogWriteCommandTests.cs
git commit -m "test(cli): catalog_write_propose/approve round-trip"
```

---

## Task 3: CLI test — `catalog_entity_map`

**Files:**
- Create: `src/ProjectAegis.MissionEditor.Cli.Tests/CatalogEntityMapCommandTests.cs`

- [ ] **Step 1: Write test**

```csharp
using System.Text.Json;
using ProjectAegis.MissionEditor.Cli;
using Xunit;

namespace ProjectAegis.MissionEditor.Cli.Tests;

public sealed class CatalogEntityMapCommandTests
{
    [Fact]
    public void catalog_entity_map_emits_sorted_entity_table_metadata()
    {
        using var output = new StringWriter();
        Assert.Equal(0, CatalogEntityMapCommand.Run(output));
        var json = output.ToString();
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetProperty("ok").GetBoolean());
        var entities = doc.RootElement.GetProperty("entities");
        Assert.True(entities.GetArrayLength() >= 3);
        var names = entities.EnumerateArray()
            .Select(e => e.GetProperty("entity").GetString())
            .Where(n => n is not null)
            .Cast<string>()
            .ToList();
        Assert.Equal(names.OrderBy(n => n, StringComparer.Ordinal), names);
        Assert.Contains("CatalogSensorBinding", names);
    }
}
```

- [ ] **Step 2: Run test**

```powershell
dotnet test src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj --filter "FullyQualifiedName~CatalogEntityMapCommandTests" -v minimal
```

Expected: **PASS**

- [ ] **Step 3: Commit**

```bash
git add src/ProjectAegis.MissionEditor.Cli.Tests/CatalogEntityMapCommandTests.cs
git commit -m "test(cli): catalog_entity_map JSON contract"
```

---

## Task 4: CLI test — `catalog_intelligence_run`

**Files:**
- Create: `src/ProjectAegis.MissionEditor.Cli.Tests/CatalogIntelligenceRunCommandTests.cs`

- [ ] **Step 1: Write test**

```csharp
using System.Text.Json;
using ProjectAegis.MissionEditor.Cli;
using Xunit;

namespace ProjectAegis.MissionEditor.Cli.Tests;

public sealed class CatalogIntelligenceRunCommandTests
{
    [Fact]
    public void catalog_intelligence_run_passes_on_baltic_fixture_without_db_flag()
    {
        using var output = new StringWriter();
        var exit = CatalogIntelligenceRunCommand.Run(databasePath: null, output);
        Assert.Equal(0, exit);
        var json = output.ToString();
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetProperty("ok").GetBoolean());
        var agents = doc.RootElement.GetProperty("agents");
        Assert.True(agents.GetArrayLength() >= 1);
        var tools = doc.RootElement.GetProperty("mcpTools").EnumerateArray()
            .Select(t => t.GetString()).ToList();
        Assert.Contains("catalog_write_propose", tools);
        Assert.Contains("catalog_import_markdown", tools);
    }
}
```

- [ ] **Step 2: Run test**

```powershell
dotnet test src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj --filter "FullyQualifiedName~CatalogIntelligenceRunCommandTests" -v minimal
```

Expected: **PASS**

- [ ] **Step 3: Commit**

```bash
git add src/ProjectAegis.MissionEditor.Cli.Tests/CatalogIntelligenceRunCommandTests.cs
git commit -m "test(cli): catalog_intelligence_run headless smoke"
```

---

## Task 5: Story stub for CLI slice + `/story-done --review full`

**Files:**
- Create: `production/epics/platform-db-basepd-slice/story-008-catalog-cli-write-gate.md`

- [ ] **Step 1: Create story file**

```markdown
# Story 008 — Catalog CLI write gate (Sprint 16)

**Epic:** platform-db-basepd-slice  
**Sprint:** 16  
**Type:** Integration  
**Status:** Complete  
**TR-ID:** TR-editor-001 (partial), req-06 DBI MCP tools  
**ADR:** ADR-006

## Acceptance Criteria

1. `catalog_write_propose` returns `{ ok, batchId, recordCount }` JSON.
2. `catalog_write_approve` commits batch and binds snapshot metadata.
3. `catalog_entity_map` lists deterministic entity→table mapping.
4. `catalog_intelligence_run` runs `DatabaseIntelligenceOrchestrator` headless.
5. All four covered by `ProjectAegis.MissionEditor.Cli.Tests` + existing `CatalogWriteGateTests`.

## Test Evidence

`src/ProjectAegis.MissionEditor.Cli.Tests/CatalogWriteCommandTests.cs`  
`src/ProjectAegis.MissionEditor.Cli.Tests/CatalogEntityMapCommandTests.cs`  
`src/ProjectAegis.MissionEditor.Cli.Tests/CatalogIntelligenceRunCommandTests.cs`  
`src/ProjectAegis.Data.Tests/WriteGate/CatalogWriteGateTests.cs`
```

- [ ] **Step 2: Run `/story-done` with full review**

Invoke skill with: `/story-done production/epics/platform-db-basepd-slice/story-008-catalog-cli-write-gate.md --review full`

**Full mode gates (mandatory):**
1. **QL-TEST-COVERAGE** — qa-lead reviews AC ↔ test mapping; verdict must be **ADEQUATE** (not GAPS/INADEQUATE).
2. **LP-CODE-REVIEW** — lead-programmer reviews CLI + gate wiring vs ADR-006; verdict **APPROVED** or **APPROVED WITH NOTES** (no REJECT blockers).

- [ ] **Step 3: User approval → mark story Complete + Completion Notes**

Per story-done Phase 7 — update `Status: Complete`, `Last Updated: 2026-06-08`, Completion Notes with gate verdicts.

---

## Task 6: PR gate QA refresh (current `main`)

**Files:**
- Create: `production/qa/sprint-16-pr-gate-2026-06-08.md`

- [ ] **Step 1: Run PR gate matrix**

```powershell
dotnet build ProjectAegis.sln -c Release
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter "ReplayGolden" -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter "PlayModeSmokeHarnessTests" -v minimal
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj --filter "Catalog|CatalogWrite|ScenarioPackage" -v minimal
dotnet test src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj --filter "Catalog" -v minimal
npx gitnexus detect-changes --repo cmano-clone
```

- [ ] **Step 2: Write evidence doc**

Template:

```markdown
# Sprint 16 — PR gate refresh (2026-06-08)

**Trunk:** main @ `<SHA>`
**Supersedes:** sprint-16-pr-gate-2026-06-04.md (365 tests pre-merge)

| Gate | Result |
|------|--------|
| dotnet test solution | PASS — N/N |
| ReplayGolden* | PASS |
| PlayMode smoke | 7/7 PASS |
| Data catalog filter | PASS |
| CLI catalog filter | PASS |
| gitnexus detect-changes | PASS / note |

**Verdict:** READY — Sprint 16 PR gate ratified on current trunk.
```

- [ ] **Step 3: Commit**

```bash
git add production/qa/sprint-16-pr-gate-2026-06-08.md
git commit -m "qa(sprint16): refresh PR gate evidence on main"
```

---

## Task 7: `/story-done --review full` — platform-db stories 001–004 (DATA P0)

Run sequentially (shared ADR-006 / CatalogWriteGate blast radius):

| Order | Story path | Focus |
|-------|------------|-------|
| 1 | `production/epics/platform-db-basepd-slice/story-001-catalog-basepd.md` | ICatalogReader + basePd |
| 2 | `production/epics/platform-db-basepd-slice/story-002-catalog-json-import.md` | CatalogJsonImporter |
| 3 | `production/epics/platform-db-basepd-slice/story-003-catalog-bulk-import.md` | CatalogBulkImporter |
| 4 | `production/epics/platform-db-basepd-slice/story-004-catalog-provenance.md` | Provenance columns |

For **each** story:

- [ ] **Step 1:** Read story + `tr-registry.yaml` TR-sensor-002 / TR-logistics-004 entries
- [ ] **Step 2:** Run automated verification (tests cited in story Test Evidence)
- [ ] **Step 3:** `/story-done [path] --review full` — spawn QL-TEST-COVERAGE + LP-CODE-REVIEW
- [ ] **Step 4:** On COMPLETE verdict, append Completion Notes (date, criteria X/Y, gate verdicts)
- [ ] **Step 5:** Update `production/sprint-status.yaml` if story maps to s16-data-p0 / s16-data-3

**Escalation:** If QL returns INADEQUATE on story 001–004, add regression test in `ProjectAegis.Data.Tests` before re-running story-done — do not override BLOCKING verdict.

---

## Task 8: Sync P0 plan + gap analysis checkboxes

**Files:**
- Modify: `docs/superpowers/plans/2026-05-30-database-intelligence-p0.md`
- Modify: `production/agentic/sprint-16-data-p0-kickoff-2026-06-04.md` (optional — mark pre-ship `[x]`)

- [ ] **Step 1: Mark DATA-0..DATA-5 complete in P0 plan**

In `2026-05-30-database-intelligence-p0.md`, set all slice checkboxes `[x]` with note "shipped Sprint 16–17 @ main".

- [ ] **Step 2: Commit**

```bash
git add docs/superpowers/plans/2026-05-30-database-intelligence-p0.md
git commit -m "docs(data): mark DATA P0 plan slices complete"
```

---

## Task 9: Sprint 16 program closeout + sprint-status

**Files:**
- Create: `production/agentic/sprint-16-closeout-2026-06-08.md`
- Modify: `production/sprint-status.yaml`
- Modify: `production/qa/sprint-16-story-done-confirm-2026-06-09.md` (append full-review note) OR supersede with closeout doc

- [ ] **Step 1: Write closeout artifact**

Include:
- Task table s16-pr-gate … s16-data-3 **DONE**
- PR #69 merge SHA
- DATA P0 slice evidence paths
- CLI test file list (Tasks 2–4)
- Full story-done gate summary (QL/LP verdicts per story)
- Test count on current main
- `s16-pr-ci`: DONE via local-gate fallback (billing external)

- [ ] **Step 2: Update sprint-status.yaml**

```yaml
sprint: 16
tests_passed: 443  # or current
story_done_verdict: complete-full
story_done_confirmed: "2026-06-08"
evidence:
  - production/agentic/sprint-16-closeout-2026-06-08.md
  - production/qa/sprint-16-pr-gate-2026-06-08.md
```

- [ ] **Step 3: Hindsight retain (optional)**

```powershell
.\tools\hindsight\Invoke-Hindsight.ps1 -Operation retain -BankId dev-cmano-clone -Content "[OUTCOME:] Sprint 16 100% — PR #69, DATA P0, CatalogWrite CLI tests, story-done full gates."
```

- [ ] **Step 4: Final verification**

```powershell
dotnet test ProjectAegis.sln -v minimal
npx gitnexus detect-changes --repo cmano-clone
```

- [ ] **Step 5: Commit**

```bash
git add production/agentic/sprint-16-closeout-2026-06-08.md production/sprint-status.yaml production/qa/
git commit -m "chore(sprint16): 100% closeout with full story-done gates"
```

---

## Self-review (plan author checklist)

| Spec requirement | Task |
|------------------|------|
| PR merge + gate QA | Task 6 |
| DATA P0 on main | Task 7 (stories 001–004) |
| Backlog closed | Task 1 |
| RunCatalog* / CatalogWrite* CLI + tests | Tasks 2–5 |
| `/story-done --review full` | Tasks 5, 7 |
| pr-gate qa evidence | Task 6 |
| 100% sprint-status | Task 9 |

**Placeholder scan:** No TBD steps. All test code is concrete.

---

## Execution handoff

**Plan saved to:** `docs/superpowers/plans/2026-06-08-sprint-16-closeout.md`

**Two execution options:**

1. **Subagent-Driven (recommended)** — one subagent per Task 2–9; orchestrator runs Task 0 audit first; `/story-done --review full` after Tasks 5 and 7.

2. **Inline Execution** — run Tasks 0→9 sequentially in this session with checkpoints after Tasks 4, 6, and 9.

**Which approach?**