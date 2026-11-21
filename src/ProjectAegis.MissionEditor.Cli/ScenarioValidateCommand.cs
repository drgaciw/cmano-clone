namespace ProjectAegis.MissionEditor.Cli;

using Data.Catalog;
using Data.Scenario.Authoring;
using Data.Validation;

public static class ScenarioValidateCommand
{
    public static int Run(string scenarioPath, bool quiet, TextWriter output)
    {
        if (!File.Exists(scenarioPath))
        {
            output.WriteLine($"{{\"error\":\"file not found\",\"path\":\"{scenarioPath}\"}}");
            return 2;
        }

        ScenarioDocumentDto scenario;
        try
        {
            scenario = ScenarioDocumentJsonLoader.LoadFromFile(scenarioPath);
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException or System.Text.Json.JsonException)
        {
            return McpToolResult.WriteError(output, "LOAD_FAILED", ex.Message);
        }

        var catalog = ResolveCatalogPublic(scenario);
        var config = new ValidationConfig();
        var (allowed, report) = ScenarioValidationExportGate.EvaluateExport(scenario, catalog, config);

        if (!quiet)
        {
            output.WriteLine(ValidationReportJsonDto.Serialize(report, config));
        }

        return allowed ? 0 : 1;
    }

    internal static ICatalogReader ResolveCatalogPublic(ScenarioDocumentDto scenario) =>
        ResolveCatalog(scenario);

    private static ICatalogReader ResolveCatalog(ScenarioDocumentDto scenario)
    {
        var sqlite = OpenBoundCatalog(scenario.Metadata);
        if (sqlite == null)
        {
            return InMemoryCatalogReader.BalticPatrolFixture();
        }

        var meta = scenario.Metadata;
        if (!string.IsNullOrWhiteSpace(meta.DbSnapshotId) || !string.IsNullOrWhiteSpace(meta.DbRef))
        {
            var dbRef = meta.DbRef ?? meta.DbSnapshotId!;
            if (sqlite.TryResolveDbRef(dbRef, out _))
            {
                return sqlite;
            }
        }
        else
        {
            var tlBranch = Data.Scenario.ScenarioPackage.ResolveTlBranch(meta);
            if (sqlite.TryResolveSnapshotForTlBranch(tlBranch, out _, out _))
            {
                return sqlite;
            }
        }

        return InMemoryCatalogReader.BalticPatrolFixture();
    }

    /// <summary>
    /// Opens <c>aegis_public_corpus.db</c> when the explicit binding is a public-corpus
    /// <c>dbRef</c> (same rule as <see cref="CatalogReaderFactory.ResolveDatabasePathForDbRef"/>).
    /// Every other scenario keeps the Baltic patrol catalog.
    /// </summary>
    private static ICatalogReader? OpenBoundCatalog(ScenarioMetadataDto metadata)
    {
        var dbRef = metadata.DbRef ?? metadata.DbSnapshotId;
        if (!string.IsNullOrWhiteSpace(dbRef) &&
            CatalogValidationDefaults.TryResolvePublicCorpusDbRef(dbRef.Trim(), out _))
        {
            return CatalogReaderFactory.TryCreatePublicCorpusReader();
        }

        return CatalogReaderFactory.TryCreateBalticPatrolReader();
    }
}