namespace ProjectAegis.MissionEditor.Cli;

using System.Security.Cryptography;
using System.Text;
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
        var config = new ValidationConfig();
        var exportPackage = ScenarioExportCommand.Prepare(scenario, catalog, config);
        if (!exportPackage.Allowed)
        {
            if (!quiet)
            {
                output.WriteLine(ValidationReportJsonDto.Serialize(exportPackage.ValidationReport, config));
            }

            return 1;
        }

        var package = ScenarioPackage.FromDocument(
            Path.GetFileNameWithoutExtension(scenarioPath),
            exportPackage.ExportDocument);
        var policyId = package.PolicyId;
        var seed = (int)Math.Min(package.Seed, int.MaxValue);
        var readiness = UnitReadinessMapFactory.FromMetadata(scenario.Metadata);
        var nearFuture = scenario.Metadata.NearFutureUnits?
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
            maxTechnologyLevel: scenario.Metadata.MaxTechnologyLevel);

        var worldStateSha256 = ResolveWorldStateSha256(result);
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
            ReportHash = exportPackage.ValidationReport.ReportHash,
            FireOrder = result.FireOrder.ToArray(),
            WorldStateSha256 = worldStateSha256,
        };

        if (!quiet)
        {
            output.WriteLine(JsonSerializer.Serialize(dto, JsonOptions));
            output.WriteLine($"SEED={result.Seed} HASH={worldStateSha256}");
            output.WriteLine(SampleCompleteRecorder.Format(
                scenarioPath,
                scenario,
                ticks,
                worldStateSha256,
                result.Seed));
        }

        return 0;
    }

    internal static string ResolveWorldStateSha256(BalticReplayHarness.Result result)
    {
        if (!string.IsNullOrEmpty(result.FingerprintSha256))
        {
            return result.FingerprintSha256;
        }

        return ComputeWorldStateSha256(result.WorldHash, result.DetectionWorldHash, result.Seed);
    }

    internal static string ComputeWorldStateSha256(ulong worldHash, ulong detectionWorldHash, int seed)
    {
        var payload = $"{worldHash}|{detectionWorldHash}|{seed}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
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

        [JsonPropertyName("fire_order")]
        public string[] FireOrder { get; init; } = [];

        public string WorldStateSha256 { get; init; } = "";
    }
}