# Database Intelligence (DBI) agent pipeline — developer guide

The **Database Intelligence** pipeline is a small set of headless, deterministic **advisory** agents
that inspect a catalog and emit coded findings — the automated "does this catalog look sane?" gate
that runs before curator drops and in CI (req-06 / DBI-8.1). Agents are **propose-only for catalog
rows**: none of them call `Propose*` / `Approve*` / commit. Row writes stay on the separate
[`CatalogWriteGate`](catalog-write-gate.md); these agents only *surface* what's staged or wrong.
That is **not** a promise that opening a SQLite file is schema-immutable — see the caveats below.

The load-bearing contract is **no LLM in the path**: every agent is a pure C# heuristic over the
catalog reader, so a run is deterministic and CI-gateable (echoing the same no-dynamic-execution
discipline as the [AI-authoring stubs](scenario-ai-authoring-and-adjudication.md)).

- **Source:** [`src/ProjectAegis.Data/Agents/`](../../src/ProjectAegis.Data/Agents/).
- **Operator surface:** the `catalog_intelligence_run` **CLI** verb
  ([`CatalogIntelligenceRunCommand`](../../src/ProjectAegis.MissionEditor.Cli/CatalogIntelligenceRunCommand.cs)).
  It is **not** registered in [`tools/mission-editor/mcp-tools.json`](../../tools/mission-editor/mcp-tools.json)
  today — MCP clients cannot invoke it until that binding is added. The JSON report's `mcpTools`
  array lists sibling catalog verbs, not a live MCP registration for this command. The thin
  [`ValidationPipeline`](../../src/ProjectAegis.Data/Validation/ValidationPipeline.cs) wrapper is
  the in-process entry.
- **Related:** the write path the diff agent inspects is [catalog-write-gate.md](catalog-write-gate.md);
  the fixtures a default run reads come from [catalog-seeding.md](catalog-seeding.md); the rules the
  validation agent enforces overlap the [CMO import](cmo-markdown-import.md) quarantine gate.

---

## Where it lives

| File | Role |
|------|------|
| [`IDatabaseIntelligenceAgent.cs`](../../src/ProjectAegis.Data/Agents/IDatabaseIntelligenceAgent.cs) | The `Run(DatabaseAgentContext) → DatabaseAgentReport` seam + the `DatabaseAgentContext` / `DatabaseAgentReport` / `DatabaseAgentFinding` records. |
| [`DatabaseIntelligenceOrchestrator.cs`](../../src/ProjectAegis.Data/Agents/DatabaseIntelligenceOrchestrator.cs) | Runs the agents in a stable order, folds their reports into a `DatabaseIntelligenceRunResult`, and provides `RunBalticDefault()`. |
| [`CatalogEntityResolutionAgent.cs`](../../src/ProjectAegis.Data/Agents/CatalogEntityResolutionAgent.cs) | Canonical `platform_id` / `sensor_id` checks. |
| [`CatalogRulesValidationAgent.cs`](../../src/ProjectAegis.Data/Agents/CatalogRulesValidationAgent.cs) | TRL/review/confidence quarantine (`CatalogImportGate`) + kill-chain + link-catalog rules. |
| [`CatalogConsistencyAgent.cs`](../../src/ProjectAegis.Data/Agents/CatalogConsistencyAgent.cs) | `base_pd` outlier detection vs the catalog median. |
| [`CatalogDiffProposalAgent.cs`](../../src/ProjectAegis.Data/Agents/CatalogDiffProposalAgent.cs) | Surfaces pending write-gate staging batches (propose-only). |

---

## The agent contract

Every agent implements one method:

```csharp
public interface IDatabaseIntelligenceAgent
{
    string AgentId { get; }
    DatabaseAgentReport Run(DatabaseAgentContext context);
}
```

- **`DatabaseAgentContext(ICatalogReader Catalog, string? DatabasePath = null)`** — the catalog to
  inspect and, optionally, the SQLite path (only the diff agent needs the path, to open the write
  gate).
- **`DatabaseAgentReport(string AgentId, bool Passed, IReadOnlyList<DatabaseAgentFinding> Findings)`**
  — an agent "passes" when it emits no `"error"`-severity finding (`warning`/`info` still pass).
- **`DatabaseAgentFinding(string Code, string Message, string Severity)`** — a stable machine code
  (e.g. `BASE_PD_OUTLIER`), a human message, and a severity string (`error` / `warning` / `info`).

Findings are sorted deterministically where an agent iterates a set (rules agent sorts quarantined
rows by `(platformId, sensorId)` ordinal), so the report is stable for a given catalog.

---

## The four agents

| Agent (`AgentId`) | What it flags | Codes / severity |
|-------------------|---------------|------------------|
| **`CatalogEntityResolutionAgent`** (`entity_resolution`) | Empty `platform_id`/`sensor_id` (**error**); a `platform_id` containing spaces needs an alias mapped before commit (**warning**). | `ENTITY_ID_EMPTY` (error), `ENTITY_ID_ALIAS_REQUIRED` (warning) |
| **`CatalogRulesValidationAgent`** (`rules_validation`) | Rows the [`CatalogImportGate.PartitionForImport`](../../src/ProjectAegis.Data/Catalog/CatalogImportGate.cs) quarantines (TRL/review/confidence), plus every finding from `KillChainRules.Evaluate` and `LinkCatalogRules.Evaluate` (in [`Data/Validation/`](../../src/ProjectAegis.Data/Validation/)). | `RULE_GATE_REJECT` (error) + kill-chain / link-catalog codes |
| **`CatalogConsistencyAgent`** (`consistency_normalization`) | Sensor bindings whose `base_pd` deviates from the catalog **median** by more than `OutlierDeltaThreshold = 0.35`. | `BASE_PD_OUTLIER` (warning) |
| **`CatalogDiffProposalAgent`** (`diff_proposal`) | The pending write-gate staging batches (`CatalogWriteGate.ListPendingBatches`) — id, record count, actor. Info-only; skips gracefully when no DB path is given. | `STAGED_BATCH_PENDING` / `DIFF_CLEAN` / `DIFF_SKIPPED` (info) |

Only the entity-resolution and rules agents can emit **error**-severity findings, so those are the
two that can fail a run; consistency and diff are advisory (`warning` / `info`).

---

## The orchestrator

`DatabaseIntelligenceOrchestrator` runs the agents in a **stable, tested order** (exposed as
`PipelineAgentOrder` for CI docs and regression tests):

```
entity_resolution → rules_validation → consistency_normalization → diff_proposal
```

It builds one `DatabaseAgentContext`, runs each agent, and returns
`DatabaseIntelligenceRunResult(Passed, Reports)` where `Passed` is the AND of every agent's
`Passed`. The default constructor wires the four built-in agents; an explicit non-empty `agents[]`
array can be injected for tests. `RunBalticDefault()` resolves the committed Baltic patrol SQLite
reader (falling back to the in-memory fixture) and its DB path, disposing the reader when it owns
one — see [catalog-seeding.md](catalog-seeding.md) for that resolution.

[`ValidationPipeline`](../../src/ProjectAegis.Data/Validation/ValidationPipeline.cs) is a thin
wrapper that simply delegates `Run` / `RunBalticDefault` to the orchestrator (the P0 catalog
validation entry point, req-06).

---

## Operator surface: `catalog_intelligence_run`

`CatalogIntelligenceRunCommand.Run(databasePath, output)` is the headless **CLI/CI** verb. It opens a
`SqliteCatalogReader` (actor `mcp-intelligence`) when given an existing DB path, else falls back to
`CatalogReaderFactory.TryCreateBalticPatrolReader()` (which can seed/update the Baltic patrol file)
or the in-memory fixture, runs the orchestrator, and writes an indented camelCase JSON report:

```jsonc
{
  "ok": true,
  "agents": [
    { "agentId": "entity_resolution", "passed": true, "findings": [ /* code, message, severity */ ] },
    { "agentId": "rules_validation", "passed": true, "findings": [] },
    { "agentId": "consistency_normalization", "passed": true, "findings": [] },
    { "agentId": "diff_proposal", "passed": true, "findings": [ /* STAGED_BATCH_PENDING ... */ ] }
  ],
  "mcpTools": [ "catalog_intelligence_run", "catalog_entity_map", "catalog_write_propose",
                "catalog_write_approve", "catalog_import_markdown" ]
}
```

The process **exit code is the gate**: `0` when the run passed, `1` when any agent emitted an
error-severity finding — so CI can fail closed on a bad catalog.

---

## Determinism & extension notes

- **Propose-only for catalog rows, not schema-immutable.** No agent calls write-gate propose/approve.
  The diff agent constructs `CatalogWriteGate` only to `ListPendingBatches()`. Opening SQLite still
  has side effects: `SqliteCatalogReader` ctor runs `ApplyMigrations()`, `CatalogWriteGate` ctor
  runs `EnsureSchema()`, and the no-`--db` factory path can seed/update the Baltic patrol catalog.
  Do not point this command at a database you assumed was immutable.
- **No LLM / deterministic.** Every agent is a pure heuristic over the catalog reader; there is no
  model call, no wall-clock, and no RNG, so a run is byte-stable for a given catalog and CI can
  pin it.
- **Stable ordering.** `PipelineAgentOrder` and per-agent ordinal sorts keep the report
  deterministic; keep both in sync when adding an agent.
- **Adding an agent** → implement `IDatabaseIntelligenceAgent` with a new stable `AgentId`, append
  it to the orchestrator's default array **and** `PipelineAgentOrder`, and use a new finding `Code`.
  Only emit `error` severity for conditions that should actually fail the CI gate; prefer
  `warning`/`info` for advisories.

---

## Tests

| Test | Covers |
|------|--------|
| [`Agents/DatabaseIntelligenceOrchestratorTests.cs`](../../src/ProjectAegis.Data.Tests/Agents/DatabaseIntelligenceOrchestratorTests.cs) | Stable agent order, aggregate pass/fail, per-agent findings. |
| [`Agents/OrchestratorKillChainGateTests.cs`](../../src/ProjectAegis.Data.Tests/Agents/OrchestratorKillChainGateTests.cs) | The kill-chain rule findings surfaced through the rules agent. |

Run just the DBI slice:

```bash
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj --filter "FullyQualifiedName~Agents"
```
