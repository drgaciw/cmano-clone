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
        var sqlite = CatalogReaderFactory.TryCreateBalticPatrolReader();
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
            var tlBranch = ProjectAegis.Data.Scenario.ScenarioPackage.ResolveTlBranch(meta);
            if (sqlite.TryResolveSnapshotForTlBranch(tlBranch, out _, out _))
            {
                return sqlite;
            }
        }

        return InMemoryCatalogReader.BalticPatrolFixture();
    }
}