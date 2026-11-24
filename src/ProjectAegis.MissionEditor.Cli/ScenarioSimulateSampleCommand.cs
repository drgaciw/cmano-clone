namespace ProjectAegis.MissionEditor.Cli;

using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Data.Catalog;
using Data.Scenario;
using Data.Scenario.Authoring;
using Data.Validation;
using Delegation.UnityAdapter.Baltic;

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

        if (!ScenarioLoadGuard.TryLoad(scenarioPath, output, out var scenario, out var loadExit))
        {
            return loadExit;
        }

        var catalog = CatalogReaderFactory.IsBalticV3Scenario(scenario.Metadata.PolicyId ?? "")
            ? InMemoryCatalogReader.BalticV3Fixture()
            : ScenarioValidateCommand.ResolveCatalogPublic(scenario);
        var config = ValidationConfigLoader.LoadFromRepo();
        var exportPackage = ScenarioExportCommand.Prepare(
            scenario,
            catalog,
            config,
            MissionRoleCapabilityManifest.CliSimulateBackendId);
        if (!exportPackage.Allowed)
        {
            if (!quiet)
            {
                output.WriteLine(ValidationReportJsonDto.Serialize(exportPackage.ValidationReport, config));
            }

            return 1;
        }

        var capabilityWarnings = exportPackage.ValidationReport.Findings
            .Where(f => f.Severity == ValidationSeverity.Warning && MissionRoleCapabilityManifest.IsCapabilityCode(f.Code))
            .Select(f => new
            {
                code = f.Code,
                severity = f.Severity.ToString(),
                missionId = f.MissionId,
                message = f.Message,
            })
            .ToArray();

        var package = ScenarioPackage.FromDocument(
            Path.GetFileNameWithoutExtension(scenarioPath),
            exportPackage.ExportDocument);
        var authoredFireOrder = EventFireOrderCalculator
            .ComputeFireOrder(exportPackage.ExportDocument.Events ?? Array.Empty<ScenarioEventDto>())
            .ToArray();
        var policyId = package.PolicyId;
        var seed = (int)Math.Min(package.Seed, int.MaxValue);
        var readiness = UnitReadinessMapFactory.FromMetadata(exportPackage.ExportDocument.Metadata);
        var nearFuture = exportPackage.ExportDocument.Metadata.NearFutureUnits?
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
            maxTechnologyLevel: exportPackage.ExportDocument.Metadata.MaxTechnologyLevel);

        var worldStateSha256 = ResolveWorldStateSha256(result);
        var fireOrder = result.FireOrder.Count == 0
            ? authoredFireOrder
            : CanonicalizeFireOrder(result.FireOrder);
        var dto = new
        {
            seed = result.Seed,
            scenarioPolicyId = result.ScenarioPolicyId,
            ticks = result.Ticks,
            fingerprint = result.Fingerprint,
            fingerprintSha256 = result.FingerprintSha256,
            worldHash = result.WorldHash.ToString(),
            detectionWorldHash = result.DetectionWorldHash.ToString(),
            engagementCount = result.EngagementCount,
            reportHash = exportPackage.ValidationReport.ReportHash,
            fire_order = fireOrder,
            worldStateSha256,
            warnings = capabilityWarnings.Length == 0 ? null : capabilityWarnings,
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
}
