# Database intelligence agent pipeline — req-06 reference

Developer/operator guide for the **database intelligence agent pipeline** — the
headless, deterministic chain of catalog-inspection agents that grades a catalog
for entity-resolution, rules, consistency, and pending-write hygiene. The
architectural decision lives in
[`ADR-006`](../architecture/adr-006-data-layer-boundary.md) (req-06); this doc is
the **how-to**: the orchestrator, every agent, every finding code and severity,
the pass/fail semantics, the CLI/MCP surface, and the P0 deferrals you should
not mistake for finished behaviour.

The pipeline lives in `src/ProjectAegis.Data/Agents/` (namespace
`ProjectAegis.Data.Agents`); the CLI verb that drives it is in
`src/ProjectAegis.MissionEditor.Cli/`. For the verb-level flag reference see
[`mission-editor-mcp-cli-reference.md`](mission-editor-mcp-cli-reference.md).

## Intent

Per ADR-006, the database intelligence layer inspects the canonical catalog
through an `ICatalogReader` and reports **findings** — it never mutates. It is a
read/grade pass that runs in CI and from MCP to answer "is this catalog clean,
and what is staged against it?" before a human approves writes through the
[catalog write gate](catalog-write-gate-runbook.md).

The pipeline enforces three invariants:

- **No LLM in the blocking path.** Every agent is pure C# over catalog rows. The
  "agent" framing is the req-06 P0 substitute for a future retrieval/LLM stage,
  not a model call. Safe to run inside CI and deterministic gates.
- **Read-only.** Agents call `ICatalogReader` (and, for the diff agent, the write
  gate's *list* API). None of them stage or commit.
- **Deterministic.** Rows are iterated through `GetSortedSensorBindings()` and
  the rules/diff outputs are re-sorted by ordinal `(platform_id, sensor_id)` /
  batch id, so the same catalog yields byte-identical findings on every host.

> **Naming collision.** `ValidationPipeline`
> (`src/ProjectAegis.Data/Validation/ValidationPipeline.cs`) is a thin wrapper
> that delegates to this pipeline's `DatabaseIntelligenceOrchestrator` — it is
> **not** the scenario `ScenarioValidationEngine` from
> [`scenario-validation-engine.md`](scenario-validation-engine.md) /
> [`ADR-008`](../architecture/adr-008-mission-editor-validation-engine.md).
> Both surface `DatabaseAgentFinding` / scenario `ValidationFinding` shaped
> output, but they take different inputs and gate different operations. Don't
> conflate the catalog pipeline (this doc) with the scenario export gate.

## Orchestrator

`DatabaseIntelligenceOrchestrator.Run(catalog, databasePath)` builds a
`DatabaseAgentContext(catalog, databasePath)` and runs each agent **in a fixed
order**, collecting one `DatabaseAgentReport` per agent:

| # | Agent | `AgentId` | Read source | Can fail the run? |
|---|-------|-----------|-------------|-------------------|
| 1 | `CatalogEntityResolutionAgent` | `entity_resolution` | sensor bindings | yes (error findings) |
| 2 | `CatalogRulesValidationAgent` | `rules_validation` | sensor bindings via `CatalogImportGate` | yes (any quarantined row) |
| 3 | `CatalogConsistencyAgent` | `consistency_normalization` | sensor bindings | yes (error findings only) |
| 4 | `CatalogDiffProposalAgent` | `diff_proposal` | write-gate staging (needs `databasePath`) | no (info-only) |

The default agent set is wired in the constructor; pass an explicit
`IDatabaseIntelligenceAgent[]` to override (used by tests). The run result is:

```csharp
public sealed record DatabaseIntelligenceRunResult(
    bool Passed,
    IReadOnlyList<DatabaseAgentReport> Reports);
```

`Passed` is `true` only when **every** agent report's `Passed` is `true`
(`reports.All(r => r.Passed)`).

`DatabaseIntelligenceOrchestrator.RunBalticDefault()` resolves the Baltic-patrol
SQLite catalog if present, else falls back to
`InMemoryCatalogReader.BalticPatrolFixture()`, disposing the reader when it is
`IDisposable`.

### Report contract

```csharp
public sealed record DatabaseAgentReport(
    string AgentId,
    bool Passed,
    IReadOnlyList<DatabaseAgentFinding> Findings);

public sealed record DatabaseAgentFinding(
    string Code,
    string Message,
    string Severity);   // "error" | "warning" | "info"
```

Each agent computes its own `Passed`; severity is advisory text, but the
agents below use it to decide pass/fail (see per-agent rules).

## Agents

### 1. Entity resolution — `entity_resolution`

Canonical-id sanity check on each sorted sensor binding (the P0 stand-in for a
full alias table).

| Finding code | Severity | Triggers when | Fails agent? |
|--------------|----------|---------------|--------------|
| `ENTITY_ID_EMPTY` | error | `platform_id` **or** `sensor_id` is null/blank | yes |
| `ENTITY_ID_ALIAS_REQUIRED` | warning | `platform_id` contains a space (needs an alias mapping before commit) | no |

The agent passes when there are no `error` findings, so a whitespace id raises a
warning without blocking the run.

### 2. Rules validation — `rules_validation`

Wraps `CatalogImportGate.PartitionForImport(...)` and reports every quarantined
row. This re-uses the same TRL / confidence / review-state gate the write path
enforces, so the agent's view of "what would be rejected" matches import.

| Finding code | Severity | Message (`{platform}/{sensor}: {reason}`) reason values |
|--------------|----------|---------------------------------------------------------|
| `RULE_GATE_REJECT` | error | `confidence_below_minimum` (`< 0.5`), `trl_below_minimum` (`< 4`), or `review_state_{state}` (not `Approved`) |

The agent passes only when **zero** rows are quarantined
(`findings.Count == 0`). Gate thresholds are `CatalogImportGate`'s defaults:
`DefaultMinimumConfidence = 0.5`, `DefaultMinimumTrl = 4`, `requireApproved = true`.

### 3. Consistency / normalization — `consistency_normalization`

Flags `base_pd` outliers against the catalog **median** `base_pd` (a P0
heuristic, not a statistical model).

| Finding code | Severity | Triggers when |
|--------------|----------|---------------|
| `BASE_PD_OUTLIER` | warning | `abs(base_pd - median) > 0.35` (`CatalogConsistencyAgent.OutlierDeltaThreshold`) |

Empty catalogs pass with no findings. Because outliers are `warning`, this agent
passes unless an `error` finding is present — in practice it never fails the run
today, it only surfaces tuning candidates. The median is taken as
`bindings[count / 2]` of the ascending `base_pd` list (lower-middle for even
counts), so it is order-stable.

### 4. Diff proposal — `diff_proposal`

Surfaces pending write-gate batches (propose-only output, never a mutation).
Requires a SQLite catalog path; with no DB it reports `DIFF_SKIPPED` and passes.

| Finding code | Severity | Triggers when |
|--------------|----------|---------------|
| `DIFF_SKIPPED` | info | `databasePath` missing or file absent — diff needs a SQLite catalog |
| `STAGED_BATCH_PENDING` | info | one per pending batch: `{batchId} records={n} actor={type}:{id}` |
| `DIFF_CLEAN` | info | a DB is present but there are no pending staging batches |

This agent always passes (`info` only); it opens a short-lived
`CatalogWriteGate` and calls `ListPendingBatches()` (read-only). For the batch
lifecycle these findings reference, see the
[catalog write-gate runbook](catalog-write-gate-runbook.md).

## CLI / MCP surface

The pipeline is exposed by the `catalog_intelligence_run` verb
(`CatalogIntelligenceRunCommand`):

```bash
dotnet run --project src/ProjectAegis.MissionEditor.Cli -- \
  catalog_intelligence_run [--db <catalog.db>]
```

- With `--db <path>` (and the file exists) it opens a `SqliteCatalogReader`
  tagged `mcp-intelligence`; otherwise it falls back to the Baltic-patrol
  fixture (same fallback as `RunBalticDefault`).
- Output is indented, camelCase JSON:

```json
{
  "ok": true,
  "agents": [
    { "agentId": "entity_resolution", "passed": true, "findings": [] },
    { "agentId": "rules_validation", "passed": true, "findings": [] },
    { "agentId": "consistency_normalization", "passed": true, "findings": [] },
    { "agentId": "diff_proposal", "passed": true,
      "findings": [ { "code": "DIFF_CLEAN", "message": "No pending staging batches", "severity": "info" } ] }
  ],
  "mcpTools": [
    "catalog_intelligence_run", "catalog_entity_map",
    "catalog_write_propose", "catalog_write_approve", "catalog_import_markdown"
  ]
}
```

- `ok` mirrors `DatabaseIntelligenceRunResult.Passed`.
- **Exit code:** `0` when `ok` is `true`, `1` when any agent fails. Wire this
  into CI to block on a dirty catalog.
- `mcpTools` is a static hint listing the sibling catalog verbs, not a
  per-run computed value.

## Constraints — do not break

- **Keep agents read-only and LLM-free.** They run in deterministic CI gates;
  do not add wall-clock reads, network calls, or model calls to the `Run` path.
- **Iterate via `GetSortedSensorBindings()` and re-sort outputs by ordinal key.**
  Reordering findings breaks the determinism CI relies on.
- **Don't widen pass/fail by accident.** `rules_validation` fails on *any*
  quarantined row; `entity_resolution` / `consistency_normalization` fail only
  on `error` findings; `diff_proposal` never fails. Changing a finding's
  severity changes the gate.
- **`diff_proposal` must stay propose-only.** It may open a `CatalogWriteGate`
  to *list* batches but must never `Propose*`/`Approve`.

## P0 deferrals (not yet implemented)

These are intentional gaps; don't document them as finished behaviour:

- **Retrieval / LLM stage skipped.** The orchestrator comment notes "retrieval
  skipped (P0)"; the pipeline is the four deterministic agents only.
- **Alias table deferred.** Entity resolution only flags whitespace ids
  (`ENTITY_ID_ALIAS_REQUIRED`); there is no canonical alias mapping yet.
- **Consistency is a single-metric heuristic.** Only `base_pd` vs median is
  checked, with a fixed `0.35` threshold and no per-domain normalization.

## Related

- [`mission-editor-mcp-cli-reference.md`](mission-editor-mcp-cli-reference.md) — `catalog_intelligence_run` flags
- [`catalog-write-gate-runbook.md`](catalog-write-gate-runbook.md) — the staging batches `diff_proposal` reports
- [`scenario-validation-engine.md`](scenario-validation-engine.md) — the *other* `ValidationPipeline`/validation surface
- [`ADR-006`](../architecture/adr-006-data-layer-boundary.md) — data-layer boundary (req-06)

## Verification

Docs-only. Behaviour verified against `src/ProjectAegis.Data/Agents/`,
`CatalogImportGate.cs`, `CatalogIntelligenceRunCommand.cs`, and the
`DatabaseIntelligenceOrchestratorTests` / `CatalogIntelligenceRunCommandTests`
suites.
