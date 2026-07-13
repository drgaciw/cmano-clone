namespace ProjectAegis.MissionEditor.Cli;

using System.Globalization;
using System.Text;
using System.Text.Json;
using ProjectAegis.Data.Catalog;

/// <summary>
/// Headless CLI verb <c>gauntlet_oracle_eval</c> — post-batch oracle evaluation via
/// <see cref="GauntletOracleEvaluator.EvaluateFromPolicyAndCsv"/>.
/// </summary>
public static class GauntletOracleEvalCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>
    /// Evaluates one policy file or each <c>*.policy.json</c> under a directory against a batch results CSV.
    /// CSV rows are filtered to the policy <c>id</c> (matched against <c>scenarioId</c>) before evaluation.
    /// </summary>
    /// <returns>0 when every scenario Passed; 1 otherwise (including arg/IO errors).</returns>
    public static int Run(
        string? policyPath,
        string? policyDir,
        string? csvPath,
        string? outPath,
        TextWriter output)
    {
        var hasPolicy = !string.IsNullOrWhiteSpace(policyPath);
        var hasDir = !string.IsNullOrWhiteSpace(policyDir);

        if (hasPolicy == hasDir)
        {
            WriteSummary(output, outPath, ok: false, allPassed: false, scenarios: Array.Empty<object>(),
                error: "gauntlet_oracle_eval requires exactly one of --policy <path.json> or --policy-dir <dir>");
            return 1;
        }

        if (string.IsNullOrWhiteSpace(csvPath))
        {
            WriteSummary(output, outPath, ok: false, allPassed: false, scenarios: Array.Empty<object>(),
                error: "gauntlet_oracle_eval requires --csv <path.csv>");
            return 1;
        }

        if (!File.Exists(csvPath))
        {
            WriteSummary(output, outPath, ok: false, allPassed: false, scenarios: Array.Empty<object>(),
                error: $"CSV not found: {csvPath}");
            return 1;
        }

        string resultsCsv;
        try
        {
            resultsCsv = File.ReadAllText(csvPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            WriteSummary(output, outPath, ok: false, allPassed: false, scenarios: Array.Empty<object>(),
                error: $"Failed to read CSV: {ex.Message}");
            return 1;
        }

        IReadOnlyList<string> policyPaths;
        try
        {
            policyPaths = ResolvePolicyPaths(policyPath, policyDir);
        }
        catch (ArgumentException ex)
        {
            WriteSummary(output, outPath, ok: false, allPassed: false, scenarios: Array.Empty<object>(),
                error: ex.Message);
            return 1;
        }

        var scenarioResults = new List<object>(policyPaths.Count);
        var allPassed = true;

        foreach (var path in policyPaths)
        {
            string policyJson;
            try
            {
                policyJson = File.ReadAllText(path);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                scenarioResults.Add(new
                {
                    scenario = Path.GetFileNameWithoutExtension(path),
                    passed = false,
                    failures = new[] { $"Failed to read policy: {ex.Message}" },
                    rows = 0,
                });
                allPassed = false;
                continue;
            }

            var scenarioId = TryReadPolicyId(policyJson);
            if (string.IsNullOrWhiteSpace(scenarioId))
            {
                scenarioResults.Add(new
                {
                    scenario = Path.GetFileName(path),
                    passed = false,
                    failures = new[] { "policy missing id" },
                    rows = 0,
                });
                allPassed = false;
                continue;
            }

            var filteredRows = GauntletOracleEvaluator.ParseCsvRows(resultsCsv)
                .Where(r => string.Equals(r.ScenarioId, scenarioId, StringComparison.Ordinal))
                .ToList();
            var filteredCsv = BuildCsv(filteredRows);
            var eval = GauntletOracleEvaluator.EvaluateFromPolicyAndCsv(policyJson, filteredCsv);

            scenarioResults.Add(new
            {
                scenario = scenarioId,
                passed = eval.Passed,
                failures = eval.Failures,
                rows = filteredRows.Count,
            });

            if (!eval.Passed)
            {
                allPassed = false;
            }
        }

        WriteSummary(output, outPath, ok: allPassed, allPassed: allPassed, scenarios: scenarioResults);
        return allPassed ? 0 : 1;
    }

    public static void PrintHelp(TextWriter output)
    {
        output.WriteLine("gauntlet_oracle_eval — post-batch oracle evaluation (GauntletOracleEvaluator)");
        output.WriteLine("Usage:");
        output.WriteLine("  gauntlet_oracle_eval --policy <path.json> --csv <path.csv> [--out <oracle-eval.json>]");
        output.WriteLine("  gauntlet_oracle_eval --policy-dir <dir with *.policy.json> --csv <path.csv> [--out <oracle-eval.json>]");
        output.WriteLine("Notes:");
        output.WriteLine("  Filters CSV rows to the policy id (scenarioId column) before evaluation.");
        output.WriteLine("  Exit 0 when all scenarios Passed; exit 1 otherwise.");
        output.WriteLine("  Always prints JSON summary to stdout; --out also writes the same JSON.");
    }

    private static IReadOnlyList<string> ResolvePolicyPaths(string? policyPath, string? policyDir)
    {
        if (!string.IsNullOrWhiteSpace(policyPath))
        {
            if (!File.Exists(policyPath))
            {
                throw new ArgumentException($"Policy not found: {policyPath}");
            }

            return [Path.GetFullPath(policyPath)];
        }

        if (string.IsNullOrWhiteSpace(policyDir) || !Directory.Exists(policyDir))
        {
            throw new ArgumentException($"Policy directory not found: {policyDir}");
        }

        var files = Directory.GetFiles(policyDir, "*.policy.json", SearchOption.TopDirectoryOnly)
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToArray();
        return files;
    }

    private static string? TryReadPolicyId(string policyJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(policyJson);
            if (doc.RootElement.TryGetProperty("id", out var idEl)
                && idEl.ValueKind == JsonValueKind.String)
            {
                return idEl.GetString();
            }
        }
        catch (JsonException)
        {
            // EvaluateFromPolicyAndCsv will surface invalid JSON via failures.
        }

        return null;
    }

    private static string BuildCsv(IReadOnlyList<GauntletBatchResultRow> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine("scenarioId,seed,side,score,kills,missilesFired,denials,fingerprint");
        foreach (var row in rows)
        {
            sb.Append(row.ScenarioId).Append(',')
                .Append(row.Seed).Append(',')
                .Append(row.Side).Append(',')
                .Append(row.Score.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(row.Kills.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(row.MissilesFired.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(row.Denials.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(row.Fingerprint)
                .Append('\n');
        }

        return sb.ToString();
    }

    private static void WriteSummary(
        TextWriter output,
        string? outPath,
        bool ok,
        bool allPassed,
        IReadOnlyList<object> scenarios,
        string? error = null)
    {
        object payload = error is null
            ? new { ok, scenarios, allPassed }
            : new { ok, scenarios, allPassed, error };

        var json = JsonSerializer.Serialize(payload, JsonOptions);
        output.WriteLine(json);

        if (!string.IsNullOrWhiteSpace(outPath))
        {
            var dir = Path.GetDirectoryName(outPath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllText(outPath, json + Environment.NewLine);
        }
    }
}
