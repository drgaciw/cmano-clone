namespace ProjectAegis.MissionEditor.Cli;

using System.Text.Json;
using System.Text.Json.Serialization;
using ProjectAegis.Data.Catalog;
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
        var config = new ValidationConfig();
        var (allowed, report) = ScenarioValidationExportGate.EvaluateExport(scenario, catalog, config);
        if (!allowed)
        {
            if (!quiet)
            {
                output.WriteLine(ValidationReportJsonDto.Serialize(report, config));
            }

            return 1;
        }

        var policyId = ResolvePolicyId(scenario);
        var seed = (int)Math.Min(scenario.Metadata.Seed, int.MaxValue);
        var result = BalticReplayHarness.Run(seed, policyId, ticks, mvpEngagement: true, catalog);

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
        };

        if (!quiet)
        {
            output.WriteLine(JsonSerializer.Serialize(dto, JsonOptions));
        }

        return 0;
    }

    private static string ResolvePolicyId(ScenarioDocumentDto scenario)
    {
        if (!string.IsNullOrWhiteSpace(scenario.Metadata.PolicyId))
        {
            return scenario.Metadata.PolicyId!;
        }

        var dbRef = scenario.Metadata.DbRef ?? scenario.Metadata.DbSnapshotId;
        if (string.Equals(dbRef, CatalogValidationDefaults.BalticSnapshotId, StringComparison.OrdinalIgnoreCase) ||
            (dbRef?.Contains("baltic", StringComparison.OrdinalIgnoreCase) ?? false))
        {
            return "baltic-patrol-catalog";
        }

        return "baltic-patrol";
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
    }
}