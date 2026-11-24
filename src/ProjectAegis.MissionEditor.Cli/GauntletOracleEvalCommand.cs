namespace ProjectAegis.MissionEditor.Cli;

using System.Globalization;
using System.Text;
using System.Text.Json;
using Data.Catalog;
using Data.Evidence;

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
        TextWriter output,
        string profile = GauntletOracleEvaluator.ProfileLadder) =>
        Run(
            policyPath,
            policyDir,
            csvPath,
            outPath,
            output,
            profile,
            baselineContextPath: null,
            baselineContextOutPath: null,
            ledgerOutPath: null,
            catalogPath: null,
            commitSha: null,
            seed: null,
            ticks: null);

    /// <summary>
    /// Evaluates one policy file or directory of <c>*.policy.json</c> files against a batch results CSV,
    /// with optional baseline-context hash verification/emission and unified <see cref="EvidenceLedger"/> output.
    /// </summary>
    /// <returns>0 when every scenario Passed and baseline-context hashes matched; 1 otherwise.</returns>
    public static int Run(
        string? policyPath,
        string? policyDir,
        string? csvPath,
        string? outPath,
        TextWriter output,
        string profile,
        string? baselineContextPath,
        string? baselineContextOutPath = null,
        string? ledgerOutPath = null,
        string? catalogPath = null,
        string? commitSha = null,
        int? seed = null,
        int? ticks = null)
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

        var csvSha256 = EvidenceLedgerSerializer.ComputeFileSha256Hex(csvPath);
        var policyFiles = new List<HashedInputFile>(policyPaths.Count);
        foreach (var path in policyPaths)
        {
            if (File.Exists(path))
            {
                policyFiles.Add(new HashedInputFile(path, EvidenceLedgerSerializer.ComputeFileSha256Hex(path)));
            }
        }

        BaselineContextProvenance? baselineContext = null;
        if (!string.IsNullOrWhiteSpace(baselineContextPath))
        {
            if (!File.Exists(baselineContextPath))
            {
                WriteSummary(output, outPath, ok: false, allPassed: false, scenarios: Array.Empty<object>(),
                    error: $"Baseline context not found: {baselineContextPath}");
                return 1;
            }

            string ctxJson;
            try
            {
                ctxJson = File.ReadAllText(baselineContextPath);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                WriteSummary(output, outPath, ok: false, allPassed: false, scenarios: Array.Empty<object>(),
                    error: $"Failed to read baseline context: {ex.Message}");
                return 1;
            }

            if (!EvidenceLedgerSerializer.TryParseBaselineContext(ctxJson, out baselineContext, out var ctxErrors))
            {
                WriteSummary(output, outPath, ok: false, allPassed: false, scenarios: Array.Empty<object>(),
                    error: $"Invalid baseline context: {string.Join("; ", ctxErrors)}");
                return 1;
            }

            var mismatch = VerifyBaselineHashes(baselineContext!, csvSha256, policyFiles, catalogPath);
            if (mismatch is not null)
            {
                WriteSummary(output, outPath, ok: false, allPassed: false, scenarios: Array.Empty<object>(),
                    error: mismatch);
                return 1;
            }
        }

        var parsedRows = GauntletOracleEvaluator.ParseCsvRows(resultsCsv);
        var scenarioResults = new List<object>(policyPaths.Count);
        var scenarioNames = new List<string>(policyPaths.Count);
        var allFailures = new List<string>();
        var allWarnings = new List<string>();
        var evidenceEntries = new List<MissionThreadEvidenceRecord>();
        var allPassed = true;
        var passedScenarioCount = 0;
        var failedScenarioCount = 0;

        foreach (var path in policyPaths)
        {
            string policyJson;
            try
            {
                policyJson = File.ReadAllText(path);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                var msg = $"Failed to read policy: {ex.Message}";
                scenarioResults.Add(new
                {
                    scenario = Path.GetFileNameWithoutExtension(path),
                    passed = false,
                    failures = new[] { msg },
                    rows = 0,
                });
                allFailures.Add(msg);
                allPassed = false;
                failedScenarioCount++;
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
                allFailures.Add($"policy missing id: {path}");
                allPassed = false;
                failedScenarioCount++;
                continue;
            }

            scenarioNames.Add(scenarioId);
            var filteredRows = parsedRows
                .Where(r => string.Equals(r.ScenarioId, scenarioId, StringComparison.Ordinal))
                .ToList();
            var filteredCsv = BuildCsv(filteredRows);
            // Strict-key fail-closed lives inside EvaluateFromPolicyAndCsv (Data).
            var eval = GauntletOracleEvaluator.EvaluateFromPolicyAndCsv(policyJson, filteredCsv, profile);

            for (var rowIdx = 0; rowIdx < filteredRows.Count; rowIdx++)
            {
                var rowSourceId = filteredRows.Count == 1 ? scenarioId : $"{scenarioId}:r{rowIdx}";
                evidenceEntries.AddRange(GauntletFingerprintEvidenceExtractor.Extract(rowSourceId, filteredRows[rowIdx].Fingerprint));
            }

            scenarioResults.Add(new
            {
                scenario = scenarioId,
                passed = eval.Passed,
                failures = eval.Failures,
                warnings = eval.EffectiveWarnings,
                rows = filteredRows.Count,
            });

            if (eval.Failures.Count > 0)
            {
                allFailures.AddRange(eval.Failures);
            }

            if (eval.EffectiveWarnings.Count > 0)
            {
                allWarnings.AddRange(eval.EffectiveWarnings);
            }

            if (eval.Passed)
            {
                passedScenarioCount++;
            }
            else
            {
                allPassed = false;
                failedScenarioCount++;
            }
        }

        if (baselineContext is null)
        {
            var resolvedCatalogPath = !string.IsNullOrWhiteSpace(catalogPath) ? catalogPath : "none";
            var resolvedCatalogSha = !string.IsNullOrWhiteSpace(catalogPath) && File.Exists(catalogPath)
                ? EvidenceLedgerSerializer.ComputeFileSha256Hex(catalogPath)
                : "none";

            baselineContext = new BaselineContextProvenance(
                SchemaVersion: 1,
                CommitSha: string.IsNullOrWhiteSpace(commitSha) ? "HEAD" : commitSha,
                Seed: seed ?? 42,
                Ticks: ticks ?? 10,
                CatalogPath: resolvedCatalogPath,
                CatalogSha256: resolvedCatalogSha,
                Policies: policyFiles,
                Results: new HashedInputFile(csvPath, csvSha256));
        }

        if (!string.IsNullOrWhiteSpace(baselineContextOutPath))
        {
            WriteFileWithDirs(baselineContextOutPath, EvidenceLedgerSerializer.ToBaselineContextJson(baselineContext) + Environment.NewLine);
        }

        if (!string.IsNullOrWhiteSpace(ledgerOutPath))
        {
            var primaryScenario = scenarioNames.FirstOrDefault() ?? "gauntlet-batch";
            var acceptanceRecord = new RevisionBoundAcceptanceRecord(
                CriterionId: "VER-07.3",
                RequirementId: "DRG-201",
                Revision: new RevisionIdentity(
                    CommitSha: baselineContext.CommitSha,
                    BuildIdentity: typeof(GauntletOracleEvalCommand).Assembly.GetName().Version?.ToString() ?? "1.0.0",
                    LocalDeltaIdentity: baselineContext.LocalDeltaIdentity ?? "unspecified"),
                Inputs: new InputDigestManifest(
                    ScenarioId: primaryScenario,
                    ScenarioSha256: policyFiles.FirstOrDefault()?.Sha256 ?? csvSha256,
                    Seed: baselineContext.Seed,
                    CatalogId: baselineContext.CatalogPath ?? "none",
                    CatalogSha256: baselineContext.CatalogSha256 ?? "none",
                    Policies: policyFiles),
                Layer: AcceptanceEvidenceLayer.Headless,
                RequiredLayer: AcceptanceEvidenceLayer.Headless,
                ExecutionCommandOrSteps: $"gauntlet_oracle_eval --profile {profile}",
                Result: allPassed ? "PASS" : "FAIL",
                ArtifactLocation: outPath ?? csvPath,
                ArtifactSha256: csvSha256,
                CollectedAtUtc: DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
                Counts: new FloorVsObservedCounts(
                    DeclaredFloor: policyPaths.Count,
                    ObservedPassed: passedScenarioCount,
                    ObservedFailed: failedScenarioCount,
                    ObservedSkipped: 0),
                OwnerAcceptance: new OwnerAcceptanceDecision(
                    Status: OwnerAcceptanceStatus.Pending,
                    Owner: null,
                    DecisionDateUtc: null,
                    DecisionReference: null,
                    AcceptedRevision: null,
                    AcceptedInputs: null));

            var ledger = EvidenceLedgerBuilder.Build(
                ledgerId: $"gauntlet-{primaryScenario}-{baselineContext.Seed}",
                scenarioId: primaryScenario,
                seed: baselineContext.Seed,
                entries: evidenceEntries,
                replayFingerprint: csvSha256,
                baselineContext: baselineContext,
                oracleEvaluation: new OracleRunCorrelation(
                    Profile: profile,
                    Passed: allPassed,
                    Scenarios: scenarioNames,
                    Failures: allFailures,
                    Warnings: allWarnings),
                acceptanceRecords: [acceptanceRecord]);

            WriteFileWithDirs(ledgerOutPath, EvidenceLedgerSerializer.ToCanonicalJson(ledger) + Environment.NewLine);
        }

        WriteSummary(output, outPath, ok: allPassed, allPassed: allPassed, scenarios: scenarioResults);
        return allPassed ? 0 : 1;
    }

    private static string? VerifyBaselineHashes(
        BaselineContextProvenance baseline,
        string csvSha256,
        IReadOnlyList<HashedInputFile> evaluatedPolicies,
        string? catalogPath)
    {
        if (baseline.Results is null
            || !string.Equals(baseline.Results.Sha256, csvSha256, StringComparison.OrdinalIgnoreCase))
        {
            return $"Baseline context results sha256 mismatch: expected '{baseline.Results?.Sha256}', actual '{csvSha256}'.";
        }

        if (evaluatedPolicies.Count > 0)
        {
            if (baseline.Policies.Count == 0)
            {
                return "Baseline context missing policies hashes while policies were evaluated.";
            }

            foreach (var evalPolicy in evaluatedPolicies)
            {
                var fileName = Path.GetFileName(evalPolicy.Path);
                var matched = baseline.Policies.FirstOrDefault(p =>
                    string.Equals(Path.GetFullPath(p.Path), Path.GetFullPath(evalPolicy.Path), StringComparison.Ordinal)
                    || string.Equals(Path.GetFileName(p.Path), fileName, StringComparison.Ordinal));
                if (matched is null)
                {
                    return $"Baseline context missing policy sha256 entry for '{fileName}'.";
                }

                if (!string.Equals(matched.Sha256, evalPolicy.Sha256, StringComparison.OrdinalIgnoreCase))
                {
                    return $"Baseline context policy sha256 mismatch for '{fileName}': expected '{matched.Sha256}', actual '{evalPolicy.Sha256}'.";
                }
            }
        }

        var checkCatalog = !string.IsNullOrWhiteSpace(catalogPath)
            ? catalogPath
            : (!string.IsNullOrWhiteSpace(baseline.CatalogPath)
               && !string.Equals(baseline.CatalogPath, "none", StringComparison.OrdinalIgnoreCase)
                ? baseline.CatalogPath
                : null);

        if (checkCatalog is not null)
        {
            if (!File.Exists(checkCatalog))
            {
                return $"Catalog file not found for baseline context verification: {checkCatalog}";
            }

            var actualCatalogSha = EvidenceLedgerSerializer.ComputeFileSha256Hex(checkCatalog);
            if (!string.Equals(baseline.CatalogSha256, actualCatalogSha, StringComparison.OrdinalIgnoreCase))
            {
                return $"Baseline context catalog sha256 mismatch: expected '{baseline.CatalogSha256}', actual '{actualCatalogSha}'.";
            }
        }

        return null;
    }

    private static void WriteFileWithDirs(string filePath, string content)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.WriteAllText(filePath, content);
    }

    public static void PrintHelp(TextWriter output)
    {
        output.WriteLine("gauntlet_oracle_eval — post-batch oracle evaluation (GauntletOracleEvaluator)");
        output.WriteLine("Usage:");
        output.WriteLine("  gauntlet_oracle_eval --policy <path.json> --csv <path.csv> [--out <oracle-eval.json>] [--profile ladder|ci] [--baseline-context <ctx.json>] [--baseline-context-out <ctx.json>] [--ledger-out <ledger.json>] [--catalog <db>] [--commit <sha>] [--seed <int>] [--ticks <int>]");
        output.WriteLine("  gauntlet_oracle_eval --policy-dir <dir with *.policy.json> --csv <path.csv> [--out <oracle-eval.json>] [--profile ladder|ci] [--baseline-context <ctx.json>] [--baseline-context-out <ctx.json>] [--ledger-out <ledger.json>]");
        output.WriteLine("Notes:");
        output.WriteLine("  Filters CSV rows to the policy id (scenarioId column) before evaluation.");
        output.WriteLine("  --profile ladder (default): gauntlet.expect (tier-tick authority).");
        output.WriteLine("  --profile ci: requires gauntlet.expectCi (CI ticks=10 smoke; no ladder fallback).");
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
