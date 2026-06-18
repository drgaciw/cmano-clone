# Agents — headless req-06 catalog intelligence pipeline

The Database Intelligence agent pipeline (requirement 06) runs a fixed sequence of
**deterministic, headless** checks over the catalog and surfaces findings. There is
**no LLM and no wall-clock in this path** — every agent is a pure function of the
catalog it reads, so a given catalog always produces the same report.

These agents are *advisory*: they emit findings and pass/fail flags only. They never
mutate the catalog. Any change still flows through the [write gate](../WriteGate/README.md)
as a human/TL-approved batch.

## Contract (`IDatabaseIntelligenceAgent`)

```csharp
public interface IDatabaseIntelligenceAgent
{
    string AgentId { get; }
    DatabaseAgentReport Run(DatabaseAgentContext context);
}
```

| Type | Shape |
|------|-------|
| `DatabaseAgentContext` | `(ICatalogReader Catalog, string? DatabasePath = null)` — the catalog to inspect; `DatabasePath` is only needed by agents that open SQLite directly. |
| `DatabaseAgentReport` | `(string AgentId, bool Passed, IReadOnlyList<DatabaseAgentFinding> Findings)` |
| `DatabaseAgentFinding` | `(string Code, string Message, string Severity)` — severity is `"info"`, `"warning"`, or `"error"`. |

Convention: an agent `Passed` is `false` only when it produced an `"error"` finding
(`info`/`warning` do not fail the run).

## Pipeline (`DatabaseIntelligenceOrchestrator`)

`Run(ICatalogReader catalog, string? databasePath = null)` executes the agents in
order and returns `DatabaseIntelligenceRunResult(bool Passed, IReadOnlyList<DatabaseAgentReport> Reports)`,
where `Passed` is the conjunction of every agent report.

The default pipeline (req-06 P0; retrieval step is deferred) is, in order:

| # | Agent (`AgentId`) | What it checks | Fails run? |
|---|-------------------|----------------|------------|
| 1 | `CatalogEntityResolutionAgent` (`entity_resolution`) | Sensor bindings have non-empty `platform_id`/`sensor_id`; flags ids containing spaces as alias-required. | Yes, on empty id (`ENTITY_ID_EMPTY` = error). |
| 2 | `CatalogRulesValidationAgent` (`rules_validation`) | Runs `CatalogImportGate.PartitionForImport` and reports every quarantined row with its rejection reason. | Yes, if any row is quarantined (`RULE_GATE_REJECT` = error). |
| 3 | `CatalogConsistencyAgent` (`consistency_normalization`) | Flags `base_pd` outliers more than `OutlierDeltaThreshold` (0.35) from the catalog median. | No (`BASE_PD_OUTLIER` = warning). |
| 4 | `CatalogDiffProposalAgent` (`diff_proposal`) | Opens the SQLite catalog and lists pending write-gate batches (propose-only view). | No (info only). |

You can override the pipeline by passing your own `IDatabaseIntelligenceAgent[]` to
the constructor; an empty/`null` array falls back to the default sequence above.

### Finding codes

| Code | Severity | Emitted by |
|------|----------|------------|
| `ENTITY_ID_EMPTY` | error | entity_resolution |
| `ENTITY_ID_ALIAS_REQUIRED` | warning | entity_resolution |
| `RULE_GATE_REJECT` | error | rules_validation |
| `BASE_PD_OUTLIER` | warning | consistency_normalization |
| `STAGED_BATCH_PENDING` | info | diff_proposal |
| `DIFF_CLEAN` | info | diff_proposal (no pending batches) |
| `DIFF_SKIPPED` | info | diff_proposal (no SQLite path supplied) |

## Determinism

- Every agent sorts inputs/findings ordinally (`StringComparer.Ordinal`) before
  emitting, so report order is reproducible regardless of catalog row order.
- `CatalogConsistencyAgent` uses a fixed numeric format (`F3`) for messages.
- No agent reads `DateTime.UtcNow` or calls an LLM. The only I/O is reading the
  catalog (and, for `diff_proposal`, opening the SQLite file read-only via the
  write gate to list staged batches).

## Usage

```csharp
// Inspect an in-memory or SQLite catalog
var result = new DatabaseIntelligenceOrchestrator().Run(catalog, databasePath);
if (!result.Passed)
{
    foreach (var report in result.Reports.Where(r => !r.Passed))
        foreach (var finding in report.Findings.Where(f => f.Severity == "error"))
            Console.Error.WriteLine($"{report.AgentId}: {finding.Code} {finding.Message}");
}

// Convenience: run against the bundled Baltic Patrol catalog (SQLite if present,
// else the in-memory fixture). Disposes the reader when it owns one.
var baltic = DatabaseIntelligenceOrchestrator.RunBalticDefault();
```

`RunBalticDefault()` resolves a catalog via
`CatalogReaderFactory.TryCreateBalticPatrolReader()` and falls back to
`InMemoryCatalogReader.BalticPatrolFixture()`, passing the resolved DB path only
when the reader is SQLite-backed.

## Tests

```bash
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj --filter DatabaseIntelligence -v minimal
```

`src/ProjectAegis.Data.Tests/Agents/DatabaseIntelligenceOrchestratorTests.cs`
exercises the default pipeline and its pass/fail semantics.

## See also

- [ProjectAegis.Data overview](../README.md)
- [Catalog — records, readers & quarantine](../Catalog/README.md) (`CatalogImportGate`, `ICatalogReader`)
- [WriteGate — staged catalog writes](../WriteGate/README.md) (pending-batch listing)
- [Requirement 06 — Database Intelligence](../../../Game-Requirements/requirements/06-Database-Intelligence.md)
- [ADR-006 — data-layer boundary](../../../docs/architecture/adr-006-data-layer-boundary.md)
