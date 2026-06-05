namespace ProjectAegis.Data.Validation;

using ProjectAegis.Data.Agents;
using ProjectAegis.Data.Catalog;

/// <summary>P0 catalog validation pipeline (req-06); delegates to <see cref="DatabaseIntelligenceOrchestrator"/>.</summary>
public sealed class ValidationPipeline
{
    private readonly DatabaseIntelligenceOrchestrator _orchestrator;

    public ValidationPipeline(DatabaseIntelligenceOrchestrator? orchestrator = null) =>
        _orchestrator = orchestrator ?? new DatabaseIntelligenceOrchestrator();

    public DatabaseIntelligenceRunResult Run(ICatalogReader catalog, string? databasePath = null) =>
        _orchestrator.Run(catalog, databasePath);

    public static DatabaseIntelligenceRunResult RunBalticDefault() =>
        new ValidationPipeline().Run(
            CatalogReaderFactory.TryCreateBalticPatrolReader()
                ?? InMemoryCatalogReader.BalticPatrolFixture(),
            CatalogReaderFactory.ResolveBalticPatrolDatabasePath());
}