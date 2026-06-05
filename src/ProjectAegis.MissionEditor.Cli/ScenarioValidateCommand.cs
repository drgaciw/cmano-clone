namespace ProjectAegis.MissionEditor.Cli;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.Data.Validation;

public static class ScenarioValidateCommand
{
    public static int Run(string scenarioPath, bool quiet, TextWriter output)
    {
        if (!File.Exists(scenarioPath))
        {
            output.WriteLine($"{{\"error\":\"file not found\",\"path\":\"{scenarioPath}\"}}");
            return 2;
        }

        var scenario = ScenarioDocumentJsonLoader.LoadFromFile(scenarioPath);
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
        var dbRef = scenario.Metadata.DbRef ?? scenario.Metadata.DbSnapshotId;
        var sqlite = CatalogReaderFactory.TryCreateBalticPatrolReader();
        if (sqlite != null && (string.IsNullOrWhiteSpace(dbRef) || sqlite.TryResolveDbRef(dbRef, out _)))
        {
            return sqlite;
        }

        return InMemoryCatalogReader.BalticPatrolFixture();
    }
}