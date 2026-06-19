namespace ProjectAegis.Data.Agents;

using ProjectAegis.Data.Catalog;

/// <summary>
/// Req-06 / DBI-8.1 pipeline: retrieval skipped (P0) → entity resolution → rules (incl. kill-chain) → consistency → diff.
/// Agent order is stable and tested — <see cref="PipelineAgentOrder"/>.
/// </summary>
public sealed class DatabaseIntelligenceOrchestrator
{
    /// <summary>Stable orchestrator step ordering for CI documentation and regression tests.</summary>
    public static readonly string[] PipelineAgentOrder =
    [
        "entity_resolution",
        "rules_validation",
        "consistency_normalization",
        "diff_proposal",
    ];

    private readonly IDatabaseIntelligenceAgent[] _agents;

    public DatabaseIntelligenceOrchestrator(IDatabaseIntelligenceAgent[]? agents = null)
    {
        _agents = agents is { Length: > 0 }
            ? agents
            :
            [
                new CatalogEntityResolutionAgent(),
                new CatalogRulesValidationAgent(),
                new CatalogConsistencyAgent(),
                new CatalogDiffProposalAgent(),
            ];
    }

    public DatabaseIntelligenceRunResult Run(ICatalogReader catalog, string? databasePath = null)
    {
        var context = new DatabaseAgentContext(catalog, databasePath);
        var reports = new List<DatabaseAgentReport>();
        foreach (var agent in _agents)
        {
            reports.Add(agent.Run(context));
        }

        var passed = reports.All(r => r.Passed);
        return new DatabaseIntelligenceRunResult(passed, reports);
    }

    public static DatabaseIntelligenceRunResult RunBalticDefault()
    {
        var catalog = CatalogReaderFactory.TryCreateBalticPatrolReader()
            ?? InMemoryCatalogReader.BalticPatrolFixture();
        var dbPath = catalog is SqliteCatalogReader sqlite
            ? CatalogReaderFactory.ResolveBalticPatrolDatabasePath()
            : null;
        if (catalog is IDisposable disposable)
        {
            using (disposable)
            {
                return new DatabaseIntelligenceOrchestrator().Run(catalog, dbPath);
            }
        }

        return new DatabaseIntelligenceOrchestrator().Run(catalog, dbPath);
    }
}

public sealed record DatabaseIntelligenceRunResult(
    bool Passed,
    IReadOnlyList<DatabaseAgentReport> Reports);