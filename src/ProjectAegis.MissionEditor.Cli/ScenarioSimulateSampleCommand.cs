namespace ProjectAegis.MissionEditor.Cli;

using System.Text.Json;
using System.Text.Json.Serialization;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Scenario;
using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.Data.Validation;
using ProjectAegis.Delegation.UnityAdapter.Baltic;

public static class ScenarioSimulateSampleCommand
{
    public static int Run(string scenarioPath, int ticks, bool quiet, TextWriter output)
    {
        if (!File.Exists(scenarioPath))
        {
            output.WriteLine($"{{\"error\":\"file not found\",\"path\":\"{scenarioPath}\"}}");
            return 2;
        }

        var scenario = ScenarioDocumentJsonLoader.LoadFromFile(scenarioPath);
        var catalog = ScenarioValidateCommand.ResolveCatalogPublic(scenario);
        var config = ValidationConfigLoader.LoadFromRepo();
        var (exportDoc, _) = ScenarioExportTransformer.TransformForExport(scenario);
        var (allowed, report) = ScenarioSaveExportGate.CanExportOrPlay(exportDoc, catalog, config);
        if (!allowed)
        {
            if (!quiet)
            {
                output.WriteLine(ValidationReportJsonDto.Serialize(report, config));
            }

            return 1;
        }

        var fireOrder = EventFireOrderCalculator.ComputeFireOrder(exportDoc.Events);
        var package = ScenarioPackage.FromDocument(Path.GetFileNameWithoutExtension(scenarioPath), exportDoc);
        var policyId = package.PolicyId;
        var seed = (int)Math.Min(package.Seed, int.MaxValue);
        var readiness = UnitReadinessMapFactory.FromMetadata(exportDoc.Metadata);
        var nearFuture = exportDoc.Metadata.NearFutureUnits?
            .Select(u => new ScenarioNearFutureUnitRequest(u.ArchetypeId, u.UnitId))
            .ToArray();
        var result = BalticReplayHarness.Run(
            seed,
            policyId,
            ticks,
            mvpEngagement: true,
            catalog,
            unitReadiness: readiness,
            nearFutureUnits: nearFuture,
            maxTechnologyLevel: exportDoc.Metadata.MaxTechnologyLevel);

        var dto = new SimulateSampleJsonDto
        {
            Seed = result.Seed,
            ScenarioPolicyId = result.ScenarioPolicyId,
            Ticks = result.Ticks,
            Fingerprint = result.Fingerprint,
            FingerprintSha256 = result.FingerprintSha256,
            WorldHash = result.WorldHash.ToString(),
            DetectionWorldHash = result.DetectionWorldHash.ToString(),
            EngagementCount = result.EngagementCount,
            ReportHash = report.ReportHash,
            FireOrder = fireOrder.ToArray(),
            SampleComplete = true,
        };

        if (!quiet)
        {
            output.WriteLine(JsonSerializer.Serialize(dto, JsonOptions));
            output.WriteLine($"SEED={result.Seed} HASH={result.WorldHash}");
        }

        return 0;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
    };

    private sealed class SimulateSampleJsonDto
    {
        public int Seed { get; init; }

        public string ScenarioPolicyId { get; init; } = "";

        public int Ticks { get; init; }

        public string Fingerprint { get; init; } = "";

        public string FingerprintSha256 { get; init; } = "";

        public string WorldHash { get; init; } = "";

        public string DetectionWorldHash { get; init; } = "";

        public int EngagementCount { get; init; }

        public string ReportHash { get; init; } = "";

        public string[] FireOrder { get; init; } = Array.Empty<string>();

        public bool SampleComplete { get; init; }
    }
}