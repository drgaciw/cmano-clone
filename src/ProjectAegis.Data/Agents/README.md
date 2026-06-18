# Database Intelligence Agents (read-only review pass)

`ProjectAegis.Data.Agents` — a fixed pipeline of **deterministic, read-only** review
agents that inspect the catalog and report findings. Entity resolution → rules
validation → consistency → diff proposal. They *advise*; they never mutate the
catalog. Every write still goes through `WriteGate.IWriteGate`.

**Requirement trace:** Req-06 (Database Intelligence) agent pipeline
(`Game-Requirements/requirements/06-Database-Intelligence.md`); ADR-006 (read/write
boundary). **Posture:** *headless, deterministic — no LLM in the path.* Each agent
is a pure pass over sorted catalog data, so the same catalog always produces the same
report (safe for CI and replay).

## Intent

Military reference data needs continuous quality checks (empty ids, alias collisions,
TRL/review gate rejections, statistical outliers, pending staged batches). Rather than
bake these into the commit path, they are surfaced as **findings** by independent
agents. A producer runs the pass, reviews the findings, and decides what to propose
through the write gate — keeping the catalog the human reviewer's authority.

## Architecture

```
DatabaseIntelligenceOrchestrator.Run(catalog, [databasePath])
        │   DatabaseAgentContext(catalog, databasePath)
        │   runs each agent in fixed order:
        ▼
  CatalogEntityResolutionAgent   "entity_resolution"        (empty / aliased ids)
  CatalogRulesValidationAgent    "rules_validation"         (wraps CatalogImportGate)
  CatalogConsistencyAgent        "consistency_normalization"(base_pd outliers)
  CatalogDiffProposalAgent       "diff_proposal"            (pending WriteGate batches)
        │   each returns DatabaseAgentReport(AgentId, Passed, Findings[])
        ▼
DatabaseIntelligenceRunResult(Passed = all reports Passed, Reports)
```

| Type | Role |
|------|------|
| `IDatabaseIntelligenceAgent` | Pipeline step: `AgentId` + `Run(DatabaseAgentContext) → DatabaseAgentReport`. |
| `DatabaseIntelligenceOrchestrator` | Runs the agents in order. Default set is the four below; inject a custom array to override. `RunBalticDefault()` resolves the Baltic catalog (SQLite if present, else in-memory fixture) and disposes it. |
| `DatabaseAgentContext` | `(ICatalogReader Catalog, string? DatabasePath)` — the diff agent needs the path to read staging tables. |
| `DatabaseAgentReport` | `(AgentId, bool Passed, IReadOnlyList<DatabaseAgentFinding>)`. |
| `DatabaseAgentFinding` | `(Code, Message, Severity)` — `Severity` is a string: `"info"`, `"warning"`, `"error"`. |
| `DatabaseIntelligenceRunResult` | `(bool Passed, IReadOnlyList<DatabaseAgentReport>)`. `Passed` is true only if **every** report passed. |

### The four agents

| Agent (`AgentId`) | Checks | Codes | Fails the report? |
|-------------------|--------|-------|-------------------|
| `entity_resolution` | Empty `platform_id`/`sensor_id`; ids containing spaces (need an alias). | `ENTITY_ID_EMPTY` (error), `ENTITY_ID_ALIAS_REQUIRED` (warning) | Only on `ENTITY_ID_EMPTY`. |
| `rules_validation` | Re-runs `Catalog.CatalogImportGate.PartitionForImport` (confidence ≥ 0.5, TRL ≥ 4, review = approved) and reports each quarantined row. | `RULE_GATE_REJECT` (error) | Yes, if any row is quarantined. |
| `consistency_normalization` | Flags `base_pd` values more than `OutlierDeltaThreshold` (0.35) from the catalog median. | `BASE_PD_OUTLIER` (warning) | No (warnings only). |
| `diff_proposal` | Lists pending write-gate staging batches (requires `DatabasePath`). | `STAGED_BATCH_PENDING` / `DIFF_CLEAN` / `DIFF_SKIPPED` (info) | No (info only). |

A report `Passed` is `findings.All(f => f.Severity != "error")` (rules agent uses
`findings.Count == 0`). So warnings and info never fail the pass.

## Usage

```csharp
using ProjectAegis.Data.Agents;
using ProjectAegis.Data.Catalog;

// Default Baltic pass (SQLite catalog if available, else in-memory fixture):
DatabaseIntelligenceRunResult result = DatabaseIntelligenceOrchestrator.RunBalticDefault();

foreach (var report in result.Reports)
{
    Console.WriteLine($"{report.AgentId}: {(report.Passed ? "PASS" : "FAIL")}");
    foreach (var f in report.Findings)
        Console.WriteLine($"  [{f.Severity}] {f.Code}: {f.Message}");
}

// Custom catalog + a specific subset of agents:
var orchestrator = new DatabaseIntelligenceOrchestrator(
    [ new CatalogEntityResolutionAgent(), new CatalogConsistencyAgent() ]);
var custom = orchestrator.Run(myCatalogReader, databasePath: "catalog.db");
```

## CLI / operational runbook

Surfaced headlessly as `catalog_intelligence_run` (and `catalog_entity_map` for the
entity → table mapping):

```bash
# Run the full intelligence pass (pass --db to enable the diff agent's staging read).
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  catalog_intelligence_run --db <catalog.db>

dotnet run --project src/ProjectAegis.MissionEditor.Cli -- catalog_entity_map
```

See `tools/mission-editor/README.md` for the verb table. Producers act on findings
by proposing batches through the write gate (`catalog_write_propose` /
`catalog_write_approve`); the agents themselves never write.

## Constraints & gotchas

- **Read-only by contract.** No agent mutates the catalog. The diff agent *opens*
  a `CatalogWriteGate` but only calls `ListPendingBatches`. Keep it that way — the
  whole pipeline relies on being a safe, repeatable inspection.
- **Diff agent needs a real DB path.** Without a `DatabasePath` (or a missing file),
  `diff_proposal` emits `DIFF_SKIPPED` (info) and passes — it cannot read staging
  tables from an in-memory catalog.
- **Fixed run order; `Passed` is conjunctive.** The orchestrator runs all four agents
  every time and aggregates `Passed` with logical AND. A single quarantined sensor
  row (rules agent) fails the whole result even if other agents pass.
- **Outlier threshold is heuristic (P0).** `BASE_PD_OUTLIER` uses a simple median +
  0.35 delta; it is advisory only and intentionally never fails the pass.
- **Determinism.** Agents iterate `GetSortedSensorBindings()` and ordinally re-sort
  before emitting, so findings are stable. Do not introduce wall-clock, RNG, or LLM
  calls here.
- **Don't confuse with scenario validation.** `Validation.ValidationPipeline` wraps
  this orchestrator for the *catalog* pass; the *scenario* rule engine
  (`Validation.ScenarioValidationEngine`) is separate. See `Validation/README.md`.

## Tests

| Area | Test |
|------|------|
| Baltic fixture passes the rules agent | `ProjectAegis.Data.Tests/Agents/DatabaseIntelligenceOrchestratorTests.Baltic_fixture_pipeline_passes_rules_agent` |
| Entity agent flags whitespace platform ids | `DatabaseIntelligenceOrchestratorTests.Entity_resolution_flags_whitespace_platform_ids` |

```bash
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "FullyQualifiedName~Agents" -v minimal
```
