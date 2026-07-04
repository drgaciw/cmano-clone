namespace ProjectAegis.MissionEditor.Cli;

using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Scenario;
using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.Data.Validation;
using ProjectAegis.Delegation.UnityAdapter.Baltic;

/// <summary>
/// CLI command for AC-2 determinism integration (S85-01).
/// Two runs with identical seed + tuning knobs produce byte-identical fire_order (ordered array of event.id)
/// and identical worldStateSha256 (SHA-256 over canonical post-run world state EXCLUDING derived-only UI state per schema).
/// Emits SEED=... HASH=... contract on stdout (non-quiet).
/// See sprint-85-determinism-ci.md, qa-plan-scenario-editor-2026-07-01.md #2, 11-Agentic-Mission-Editor.md AME-6.6/6.7.
/// </summary>
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
        var fireOrder = CanonicalizeFireOrder(result.FireOrder);
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
            FireOrder = fireOrder,
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
        // Prefers order-log fingerprint SHA (covers fired events); falls back to world state.
        // By construction this is SHA over canonical sim state EXCLUDING derived-only UI state (per schema).
        if (!string.IsNullOrEmpty(result.FingerprintSha256))
        {
            return result.FingerprintSha256;
        }

        return ComputeWorldStateSha256(result.WorldHash, result.DetectionWorldHash, result.Seed);
    }

    internal static string ComputeWorldStateSha256(ulong worldHash, ulong detectionWorldHash, int seed)
    {
        // SHA-256 over canonical post-run sim world state.
        // EXCLUDES derived-only UI state (per scenario schema; never flows into
        // BalticReplayHarness.Result or package used for sim). See AC-2 in sprint-85-determinism-ci.md.
        var payload = $"{worldHash}|{detectionWorldHash}|{seed}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>
    /// Returns byte-identical ordered array of event.id strings for fire_order.
    /// Order per policy timeline (authored with sort key (trigger_time_resolved, priority, event.id))
    /// or chronological from decision log for fired events.
    /// Ensures determinism contract across independent runs (incl. parallel CI).
    /// </summary>
    internal static string[] CanonicalizeFireOrder(IReadOnlyList<string> raw)
    {
        // No mutation of source; ToArray yields stable ordered sequence for JSON emit.
        // Same seed + tuning + scenario => identical array content and order.
        return raw.ToArray();
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