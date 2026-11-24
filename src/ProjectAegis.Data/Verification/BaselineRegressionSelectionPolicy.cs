using System.Text.Json;

namespace ProjectAegis.Data.Verification;

/// <summary>Execution tier separating fast inner-loop PR checks from blocking baseline/release gates.</summary>
public enum TestRunTier
{
    FastPrInnerLoop = 1,
    BlockingMergeBaseline = 2,
    ReleaseVerification = 3,
}

/// <summary>DRG-323 GitNexus index health state observed before test selection or reindexing.</summary>
public enum GitNexusIndexHealthStatus
{
    Healthy = 1,
    Missing = 2,
    Stale = 3,
    Corrupted = 4,
    Unavailable = 5,
    SharedStoreConflict = 6,
}

/// <summary>Impact risk classification produced by GitNexus impact/change analysis.</summary>
public enum GitNexusRiskLevel
{
    Unknown = 0,
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4,
}

/// <summary>Snapshot of GitNexus index integrity checks matching <c>tools/gitnexus-safe-reindex.sh</c>.</summary>
public sealed record GitNexusIndexState(
    GitNexusIndexHealthStatus HealthStatus,
    string? IndexedCommitSha,
    string? TargetCommitSha,
    int NodeCount,
    int EdgeCount,
    int EmbeddingCount,
    bool HasGarbageIdsOrEmptyNames,
    string? VectorSearchStatus)
{
    /// <summary>True only when the index satisfies all DRG-323 integrity invariants for the target commit.</summary>
    public bool IsDrg323IntegrityValid() =>
        HealthStatus == GitNexusIndexHealthStatus.Healthy
        && !string.IsNullOrWhiteSpace(IndexedCommitSha)
        && !string.IsNullOrWhiteSpace(TargetCommitSha)
        && string.Equals(IndexedCommitSha, TargetCommitSha, StringComparison.OrdinalIgnoreCase)
        && NodeCount > 0
        && EdgeCount > 0
        && EmbeddingCount > 0
        && !HasGarbageIdsOrEmptyNames
        && (string.Equals(VectorSearchStatus, "vector-index", StringComparison.Ordinal)
            || string.Equals(VectorSearchStatus, "exact-scan", StringComparison.Ordinal));
}

/// <summary>Preconditions evaluated before running a GitNexus reindex.</summary>
public sealed record GitNexusReindexContext(
    string BranchName,
    bool IsWorkingTreeClean,
    bool HasActiveMcpServer,
    bool HasSharedStorePointer,
    bool AllowBranchOverride = false,
    bool AllowDirtyOverride = false);

/// <summary>Decision returned by <see cref="GitNexusReindexPolicy"/>.</summary>
public sealed record GitNexusReindexDecision(
    bool Allowed,
    bool MustUseFullForceWithEmbeddings,
    string CanonicalCommand,
    IReadOnlyList<string> RefusalReasons);

/// <summary>
/// Codifies DRG-323 safe reindex rules: never run incremental analyze, never run on a dirty feature
/// branch, and always close MCP handles before running <c>tools/gitnexus-safe-reindex.sh</c>.
/// </summary>
public static class GitNexusReindexPolicy
{
    public const string CanonicalSafeReindexCommand = "tools/gitnexus-safe-reindex.sh";
    public const string CanonicalAnalyzeArgs = "analyze --force --embeddings --skip-agents-md --skip-skills";

    /// <summary>Evaluates whether reindexing is permitted with optional explicit overrides.</summary>
    public static GitNexusReindexDecision Evaluate(GitNexusReindexContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        return EvaluateInternal(context, allowOverrides: true);
    }

    /// <summary>Strict policy unconditionally forbidding reindexing on non-main or dirty feature branches.</summary>
    public static GitNexusReindexDecision EvaluateStrictFeatureBranchPolicy(GitNexusReindexContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        return EvaluateInternal(context, allowOverrides: false);
    }

    private static GitNexusReindexDecision EvaluateInternal(GitNexusReindexContext context, bool allowOverrides)
    {
        var reasons = new List<string>();
        var isMain = string.Equals(context.BranchName, "main", StringComparison.Ordinal);
        if (!isMain && !(allowOverrides && context.AllowBranchOverride))
        {
            reasons.Add($"DRG-323 refuses live GitNexus reindex on non-main branch '{context.BranchName}'.");
        }

        if (!context.IsWorkingTreeClean && !(allowOverrides && context.AllowDirtyOverride))
        {
            reasons.Add("DRG-323 refuses live GitNexus reindex on a dirty working tree.");
        }

        if (context.HasActiveMcpServer)
        {
            reasons.Add("DRG-323 requires stopping active gitnexus mcp handles before reindexing.");
        }

        if (context.HasSharedStorePointer)
        {
            reasons.Add("DRG-323 forbids reindexing when .gitnexus/store.json shared store pointer is active.");
        }

        return new GitNexusReindexDecision(
            Allowed: reasons.Count == 0,
            MustUseFullForceWithEmbeddings: true,
            CanonicalCommand: CanonicalSafeReindexCommand,
            RefusalReasons: reasons);
    }
}

/// <summary>Input to <see cref="GitNexusTestSelectionEngine.BuildPlan"/>.</summary>
/// <param name="RequestedTier">Execution tier separating fast inner-loop checks from blocking baseline/release gates.</param>
/// <param name="ChangedPaths">Repository-relative modified paths.</param>
/// <param name="IndexState">GitNexus index integrity state for DRG-323 checks.</param>
/// <param name="ImpactRisk">GitNexus impact/blast-radius risk level.</param>
/// <param name="IsWorkingTreeClean">
/// Advisory metadata indicating whether the caller's working tree is clean.
/// <see cref="TestRunTier.FastPrInnerLoop"/> intentionally permits running locally on dirty developer trees,
/// whereas live GitNexus reindexing is separately forbidden on dirty trees by <see cref="GitNexusReindexPolicy"/> (DRG-323).
/// </param>
/// <param name="ImpactedSymbols">
/// Optional advisory list of symbol names reported by <c>gitnexus_impact</c> / <c>detect_changes</c>
/// for audit logging and caller correlation; path-level transitive closure and conservative fallback govern test selection.
/// </param>
/// <param name="AdditionalFilters">Optional extra xUnit/NUnit test filters appended to the plan.</param>
public sealed record TestSelectionRequest(
    TestRunTier RequestedTier,
    IReadOnlyList<string> ChangedPaths,
    GitNexusIndexState IndexState,
    GitNexusRiskLevel ImpactRisk,
    bool IsWorkingTreeClean,
    IReadOnlyList<string>? ImpactedSymbols = null,
    IReadOnlyList<string>? AdditionalFilters = null);

/// <summary>Deterministic test execution plan produced by <see cref="GitNexusTestSelectionEngine"/>.</summary>
public sealed record TestSelectionPlan(
    bool RunFullSolution,
    bool IsConservativeFallback,
    bool RequireReplayGoldenAndPlayModeSmoke,
    bool RequireArchitectureFences,
    IReadOnlyList<string> SelectedTestProjects,
    IReadOnlyList<string> RecommendedTestFilters,
    IReadOnlyList<string> FallbackReasons);

/// <summary>
/// DRG-204 deterministic GitNexus-informed fast-PR test selector with fail-closed conservative fallback
/// to the full 9-assembly solution test suite.
/// </summary>
public static class GitNexusTestSelectionEngine
{
    public const string ArchitectureFenceFilter =
        "UnitySimImportForbidTests|NamespaceSccBaselineTests|CsprojReferenceGraphPinTests";

    public static IReadOnlyList<string> AllSolutionTestProjects { get; } = new[]
    {
        "ProjectAegis.Ai.Contracts.Tests",
        "ProjectAegis.Ai.Host.Tests",
        "ProjectAegis.Data.Excel.Tests",
        "ProjectAegis.Data.Tests",
        "ProjectAegis.Delegation.Demo.Tests",
        "ProjectAegis.Delegation.Tests",
        "ProjectAegis.Delegation.UnityAdapter.Tests",
        "ProjectAegis.MissionEditor.Cli.Tests",
        "ProjectAegis.Sim.Tests",
    };

    /// <summary>Builds a deterministic <see cref="TestSelectionPlan"/> failing closed whenever safety preconditions are not met.</summary>
    public static TestSelectionPlan BuildPlan(TestSelectionRequest request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (request.RequestedTier != TestRunTier.FastPrInnerLoop)
        {
            return new TestSelectionPlan(
                RunFullSolution: true,
                IsConservativeFallback: false,
                RequireReplayGoldenAndPlayModeSmoke: true,
                RequireArchitectureFences: true,
                SelectedTestProjects: AllSolutionTestProjects,
                RecommendedTestFilters: BuildDefaultFilters(request.AdditionalFilters),
                FallbackReasons: Array.Empty<string>());
        }

        var fallbackReasons = new List<string>();
        if (!request.IndexState.IsDrg323IntegrityValid())
        {
            fallbackReasons.Add("GitNexus index failed DRG-323 integrity validation.");
        }

        if (request.ImpactRisk is GitNexusRiskLevel.Unknown or GitNexusRiskLevel.High or GitNexusRiskLevel.Critical)
        {
            fallbackReasons.Add($"GitNexus impact risk '{request.ImpactRisk}' requires full baseline fallback.");
        }

        if (request.ChangedPaths.Count == 0)
        {
            fallbackReasons.Add("No changed paths provided; falling back to full solution.");
        }

        var selected = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < request.ChangedPaths.Count; i++)
        {
            var raw = request.ChangedPaths[i];
            if (string.IsNullOrWhiteSpace(raw))
            {
                fallbackReasons.Add("Empty changed path entry encountered.");
                continue;
            }

            var normalized = raw.Trim().Replace('\\', '/');
            while (normalized.StartsWith("./", StringComparison.Ordinal))
            {
                normalized = normalized.Substring(2);
            }

            if (string.IsNullOrWhiteSpace(normalized))
            {
                fallbackReasons.Add("Empty changed path entry encountered.");
                continue;
            }

            if (IsConservativeFullSolutionPath(normalized))
            {
                fallbackReasons.Add($"High-blast or Hard-No path modified: '{normalized}'.");
                continue;
            }

            if (!TryMapPathToTransitiveTests(normalized, selected))
            {
                fallbackReasons.Add($"Unmapped path '{normalized}' requires conservative full fallback.");
            }
        }

        if (fallbackReasons.Count > 0 || selected.Count == 0)
        {
            if (fallbackReasons.Count == 0)
            {
                fallbackReasons.Add("No test projects matched changed paths.");
            }

            return new TestSelectionPlan(
                RunFullSolution: true,
                IsConservativeFallback: true,
                RequireReplayGoldenAndPlayModeSmoke: true,
                RequireArchitectureFences: true,
                SelectedTestProjects: AllSolutionTestProjects,
                RecommendedTestFilters: BuildDefaultFilters(request.AdditionalFilters),
                FallbackReasons: fallbackReasons);
        }

        var orderedProjects = selected.OrderBy(p => p, StringComparer.Ordinal).ToArray();
        var requireReplayAndSmoke = selected.Contains("ProjectAegis.Delegation.UnityAdapter.Tests");
        return new TestSelectionPlan(
            RunFullSolution: false,
            IsConservativeFallback: false,
            RequireReplayGoldenAndPlayModeSmoke: requireReplayAndSmoke,
            RequireArchitectureFences: true,
            SelectedTestProjects: orderedProjects,
            RecommendedTestFilters: BuildDefaultFilters(request.AdditionalFilters),
            FallbackReasons: Array.Empty<string>());
    }

    private static IReadOnlyList<string> BuildDefaultFilters(IReadOnlyList<string>? additionalFilters)
    {
        var filters = new List<string> { ArchitectureFenceFilter };
        if (additionalFilters is not null)
        {
            for (var i = 0; i < additionalFilters.Count; i++)
            {
                var filter = additionalFilters[i];
                if (!string.IsNullOrWhiteSpace(filter))
                {
                    filters.Add(filter.Trim());
                }
            }
        }

        return filters;
    }

    private static bool IsConservativeFullSolutionPath(string normalized)
    {
        if (normalized.Equals("ProjectAegis.sln", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("Directory.Build.props", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("Directory.Packages.props", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("global.json", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("production/test-floor.json", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (normalized.StartsWith(".buildkite/", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith(".github/workflows/", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("tools/buildkite/", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("data/scenarios/", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("assets/data/catalog/", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("tests/regression/", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return normalized.IndexOf("DelegationBridge", StringComparison.OrdinalIgnoreCase) >= 0
            || normalized.IndexOf("SimulationSession", StringComparison.OrdinalIgnoreCase) >= 0
            || normalized.IndexOf("CatalogWriteGate", StringComparison.OrdinalIgnoreCase) >= 0
            || normalized.IndexOf("/Architecture/", StringComparison.OrdinalIgnoreCase) >= 0
            || normalized.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryMapPathToTransitiveTests(string path, HashSet<string> selected)
    {
        if (StartsWithPrefix(path, "src/ProjectAegis.Data/"))
        {
            AddRange(selected,
                "ProjectAegis.Data.Tests",
                "ProjectAegis.Data.Excel.Tests",
                "ProjectAegis.Sim.Tests",
                "ProjectAegis.Delegation.Tests",
                "ProjectAegis.Ai.Host.Tests",
                "ProjectAegis.Delegation.UnityAdapter.Tests",
                "ProjectAegis.Delegation.Demo.Tests",
                "ProjectAegis.MissionEditor.Cli.Tests");
            return true;
        }

        if (StartsWithPrefix(path, "src/ProjectAegis.Ai.Contracts/test/"))
        {
            selected.Add("ProjectAegis.Ai.Contracts.Tests");
            return true;
        }

        if (StartsWithPrefix(path, "src/ProjectAegis.Ai.Contracts/"))
        {
            AddRange(selected,
                "ProjectAegis.Ai.Contracts.Tests",
                "ProjectAegis.Ai.Host.Tests",
                "ProjectAegis.Delegation.UnityAdapter.Tests",
                "ProjectAegis.Delegation.Demo.Tests",
                "ProjectAegis.MissionEditor.Cli.Tests");
            return true;
        }

        if (StartsWithPrefix(path, "src/ProjectAegis.Sim/")
            || StartsWithPrefix(path, "src/ProjectAegis.Sim.Benchmark/"))
        {
            AddRange(selected,
                "ProjectAegis.Sim.Tests",
                "ProjectAegis.Delegation.Tests",
                "ProjectAegis.Ai.Host.Tests",
                "ProjectAegis.Delegation.UnityAdapter.Tests",
                "ProjectAegis.Delegation.Demo.Tests",
                "ProjectAegis.MissionEditor.Cli.Tests");
            return true;
        }

        if (StartsWithPrefix(path, "src/ProjectAegis.Delegation/"))
        {
            AddRange(selected,
                "ProjectAegis.Delegation.Tests",
                "ProjectAegis.Ai.Host.Tests",
                "ProjectAegis.Delegation.UnityAdapter.Tests",
                "ProjectAegis.Delegation.Demo.Tests",
                "ProjectAegis.MissionEditor.Cli.Tests");
            return true;
        }

        if (StartsWithPrefix(path, "src/ProjectAegis.Data.Excel/"))
        {
            AddRange(selected,
                "ProjectAegis.Data.Excel.Tests",
                "ProjectAegis.Data.Tests",
                "ProjectAegis.MissionEditor.Cli.Tests");
            return true;
        }

        if (StartsWithPrefix(path, "src/ProjectAegis.Delegation.UnityAdapter/"))
        {
            AddRange(selected,
                "ProjectAegis.Delegation.UnityAdapter.Tests",
                "ProjectAegis.Delegation.Demo.Tests",
                "ProjectAegis.MissionEditor.Cli.Tests");
            return true;
        }

        if (StartsWithPrefix(path, "src/ProjectAegis.Ai.Host/"))
        {
            AddRange(selected,
                "ProjectAegis.Ai.Host.Tests",
                "ProjectAegis.MissionEditor.Cli.Tests");
            return true;
        }

        if (StartsWithPrefix(path, "src/ProjectAegis.MissionEditor.Cli/")
            || StartsWithPrefix(path, "src/ProjectAegis.MissionEditor.Cli.Tests/"))
        {
            selected.Add("ProjectAegis.MissionEditor.Cli.Tests");
            return true;
        }

        if (StartsWithPrefix(path, "src/ProjectAegis.Delegation.Demo/")
            || StartsWithPrefix(path, "src/ProjectAegis.Delegation.Demo.Tests/"))
        {
            selected.Add("ProjectAegis.Delegation.Demo.Tests");
            return true;
        }

        if (StartsWithPrefix(path, "docs/"))
        {
            AddRange(selected,
                "ProjectAegis.Data.Tests",
                "ProjectAegis.Delegation.Tests");
            return true;
        }

        if (StartsWithPrefix(path, "src/ProjectAegis.Data.Tests/")
            || StartsWithPrefix(path, "Game-Requirements/"))
        {
            selected.Add("ProjectAegis.Data.Tests");
            return true;
        }

        if (StartsWithPrefix(path, "src/ProjectAegis.Data.Excel.Tests/"))
        {
            selected.Add("ProjectAegis.Data.Excel.Tests");
            return true;
        }

        if (StartsWithPrefix(path, "src/ProjectAegis.Sim.Tests/"))
        {
            selected.Add("ProjectAegis.Sim.Tests");
            return true;
        }

        if (StartsWithPrefix(path, "src/ProjectAegis.Delegation.Tests/"))
        {
            selected.Add("ProjectAegis.Delegation.Tests");
            return true;
        }

        if (StartsWithPrefix(path, "src/ProjectAegis.Ai.Host.Tests/"))
        {
            selected.Add("ProjectAegis.Ai.Host.Tests");
            return true;
        }

        if (StartsWithPrefix(path, "src/ProjectAegis.Delegation.UnityAdapter.Tests/"))
        {
            selected.Add("ProjectAegis.Delegation.UnityAdapter.Tests");
            return true;
        }

        return false;
    }

    private static bool StartsWithPrefix(string path, string prefix) =>
        path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);

    private static void AddRange(HashSet<string> set, params string[] items)
    {
        for (var i = 0; i < items.Length; i++)
        {
            set.Add(items[i]);
        }
    }
}

/// <summary>Parsed <c>production/test-floor.json</c> manifest.</summary>
public sealed record TestFloorManifest(
    int SolutionMinPass,
    int ReplayGoldenMin,
    int PlayModeSmokeMin,
    string BalticV2Hash,
    int BaselineWarnings,
    int BaselinePluginDllCount);

/// <summary>Observed baseline gate measurements evaluated by <see cref="BaselineRegressionPolicy"/>.</summary>
public sealed record BaselineGateEvaluationInput(
    int BuildErrors,
    int BuildWarnings,
    int ObservedPassed,
    int ObservedFailed,
    int ObservedSkipped,
    int ReplayGoldenPassed,
    int PlayModeSmokePassed,
    int ArchitectureFencesPassed,
    int ArchitectureFencesFailed,
    string ObservedBalticV2Hash,
    bool HasTrackedDb3Files,
    int DelegationBridgeHotpathDiffCount);

/// <summary>Baseline gate evaluation result.</summary>
public sealed record BaselineGateVerdict(
    bool Passed,
    IReadOnlyList<string> Violations);

/// <summary>
/// Enforces VER-06 baseline regression invariants: monotonic test floors, Baltic v2 hash preservation,
/// 31/31 architecture fences, zero warnings/errors, clean-room catalog (no *.db3), and zero DelegationBridge hotpath edits.
/// </summary>
public static class BaselineRegressionPolicy
{
    public const int HistoricalSolutionMinFloor = 1638;
    public const string ImmutableBalticV2Hash = "17144800277401907079";
    public const int RequiredArchitectureFenceCount = 31;

    /// <summary>Parses <c>production/test-floor.json</c> (<c>netstandard2.1</c>-safe).</summary>
    public static bool TryParseTestFloorJson(
        string json,
        out TestFloorManifest? manifest,
        out string? error)
    {
        manifest = null;
        if (string.IsNullOrWhiteSpace(json))
        {
            error = "test-floor.json is empty.";
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                error = "test-floor.json root must be a JSON object.";
                return false;
            }

            if (!TryReadNonNegativeInt(root, "solutionMinPass", out var solutionMinPass)
                || !TryReadNonNegativeInt(root, "replayGoldenMin", out var replayGoldenMin)
                || !TryReadNonNegativeInt(root, "playModeSmokeMin", out var playModeSmokeMin)
                || !TryReadNonNegativeInt(root, "baselineWarnings", out var baselineWarnings)
                || !TryReadNonNegativeInt(root, "baselinePluginDllCount", out var baselinePluginDllCount))
            {
                error = "test-floor.json has missing or negative required numeric floor fields.";
                return false;
            }

            var balticV2Hash = root.TryGetProperty("balticV2Hash", out var hashEl) && hashEl.ValueKind == JsonValueKind.String
                ? hashEl.GetString()
                : null;
            if (string.IsNullOrWhiteSpace(balticV2Hash))
            {
                error = "test-floor.json is missing balticV2Hash.";
                return false;
            }

            manifest = new TestFloorManifest(
                SolutionMinPass: solutionMinPass,
                ReplayGoldenMin: replayGoldenMin,
                PlayModeSmokeMin: playModeSmokeMin,
                BalticV2Hash: balticV2Hash,
                BaselineWarnings: baselineWarnings,
                BaselinePluginDllCount: baselinePluginDllCount);
            error = null;
            return true;
        }
        catch (JsonException ex)
        {
            error = $"Invalid test-floor.json: {ex.Message}";
            return false;
        }
    }

    /// <summary>Verifies that <paramref name="candidate"/> never ratchets test floors downward or alters immutable baseline invariants.</summary>
    public static BaselineGateVerdict ValidateFloorMonotonicity(
        TestFloorManifest prior,
        TestFloorManifest candidate)
    {
        if (prior is null)
        {
            throw new ArgumentNullException(nameof(prior));
        }

        if (candidate is null)
        {
            throw new ArgumentNullException(nameof(candidate));
        }

        var violations = new List<string>();
        if (candidate.ReplayGoldenMin < 0)
        {
            violations.Add($"ReplayGoldenMin {candidate.ReplayGoldenMin} must be >= 0.");
        }

        if (candidate.PlayModeSmokeMin < 0)
        {
            violations.Add($"PlayModeSmokeMin {candidate.PlayModeSmokeMin} must be >= 0.");
        }

        if (candidate.BaselinePluginDllCount < 0)
        {
            violations.Add($"BaselinePluginDllCount {candidate.BaselinePluginDllCount} must be >= 0.");
        }

        if (candidate.SolutionMinPass < HistoricalSolutionMinFloor)
        {
            violations.Add($"SolutionMinPass {candidate.SolutionMinPass} < historical floor {HistoricalSolutionMinFloor}.");
        }

        if (candidate.SolutionMinPass < prior.SolutionMinPass)
        {
            violations.Add($"SolutionMinPass regressed from {prior.SolutionMinPass} to {candidate.SolutionMinPass}.");
        }

        if (candidate.ReplayGoldenMin < prior.ReplayGoldenMin)
        {
            violations.Add($"ReplayGoldenMin regressed from {prior.ReplayGoldenMin} to {candidate.ReplayGoldenMin}.");
        }

        if (candidate.PlayModeSmokeMin < prior.PlayModeSmokeMin)
        {
            violations.Add($"PlayModeSmokeMin regressed from {prior.PlayModeSmokeMin} to {candidate.PlayModeSmokeMin}.");
        }

        if (!string.Equals(candidate.BalticV2Hash, ImmutableBalticV2Hash, StringComparison.Ordinal))
        {
            violations.Add($"BalticV2Hash '{candidate.BalticV2Hash}' drifted from '{ImmutableBalticV2Hash}'.");
        }

        if (candidate.BaselineWarnings != 0)
        {
            violations.Add($"BaselineWarnings must be 0 (was {candidate.BaselineWarnings}).");
        }

        return new BaselineGateVerdict(violations.Count == 0, violations);
    }

    /// <summary>Evaluates observed build/test/invariant outputs against floor and sprint effective thresholds.</summary>
    public static BaselineGateVerdict EvaluateBaselineGate(
        TestFloorManifest floor,
        BaselineGateEvaluationInput observed,
        int? sprintEffectiveSolutionFloor = null)
    {
        if (floor is null)
        {
            throw new ArgumentNullException(nameof(floor));
        }

        if (observed is null)
        {
            throw new ArgumentNullException(nameof(observed));
        }

        var violations = new List<string>();
        if (floor.SolutionMinPass < 0
            || floor.ReplayGoldenMin < 0
            || floor.PlayModeSmokeMin < 0
            || floor.BaselineWarnings < 0
            || floor.BaselinePluginDllCount < 0)
        {
            violations.Add("TestFloorManifest contains negative floor value(s).");
        }

        if (sprintEffectiveSolutionFloor is < 0)
        {
            violations.Add($"sprintEffectiveSolutionFloor={sprintEffectiveSolutionFloor} must be >= 0.");
        }

        if (observed.BuildErrors != 0)
        {
            violations.Add($"BuildErrors={observed.BuildErrors} (must be 0).");
        }

        if (observed.BuildWarnings < 0 || observed.BuildWarnings > floor.BaselineWarnings)
        {
            violations.Add($"BuildWarnings={observed.BuildWarnings} out of bounds [0, {floor.BaselineWarnings}].");
        }

        if (observed.ObservedFailed != 0)
        {
            violations.Add($"ObservedFailed={observed.ObservedFailed} (must be 0).");
        }

        if (observed.ObservedSkipped != 0)
        {
            violations.Add($"ObservedSkipped={observed.ObservedSkipped} (must be 0).");
        }

        var requiredPass = Math.Max(floor.SolutionMinPass, sprintEffectiveSolutionFloor ?? 0);
        if (observed.ObservedPassed < 0 || observed.ObservedPassed < requiredPass)
        {
            violations.Add($"ObservedPassed={observed.ObservedPassed} < required floor {requiredPass}.");
        }

        if (observed.ReplayGoldenPassed < 0 || observed.ReplayGoldenPassed < floor.ReplayGoldenMin)
        {
            violations.Add($"ReplayGoldenPassed={observed.ReplayGoldenPassed} < {floor.ReplayGoldenMin}.");
        }

        if (observed.PlayModeSmokePassed < 0 || observed.PlayModeSmokePassed < floor.PlayModeSmokeMin)
        {
            violations.Add($"PlayModeSmokePassed={observed.PlayModeSmokePassed} < {floor.PlayModeSmokeMin}.");
        }

        if (observed.ArchitectureFencesFailed != 0 || observed.ArchitectureFencesPassed < RequiredArchitectureFenceCount)
        {
            violations.Add(
                $"Architecture fences failed/short: passed={observed.ArchitectureFencesPassed}, failed={observed.ArchitectureFencesFailed} (require {RequiredArchitectureFenceCount}/0).");
        }

        if (!string.Equals(observed.ObservedBalticV2Hash, ImmutableBalticV2Hash, StringComparison.Ordinal))
        {
            violations.Add($"ObservedBalticV2Hash '{observed.ObservedBalticV2Hash}' != '{ImmutableBalticV2Hash}'.");
        }

        if (observed.HasTrackedDb3Files)
        {
            violations.Add("Tracked *.db3 files detected; violates clean-room catalog policy.");
        }

        if (observed.DelegationBridgeHotpathDiffCount != 0)
        {
            violations.Add($"DelegationBridgeHotpathDiffCount={observed.DelegationBridgeHotpathDiffCount} (must be 0).");
        }

        return new BaselineGateVerdict(violations.Count == 0, violations);
    }

    private static bool TryReadNonNegativeInt(JsonElement obj, string propName, out int value)
    {
        if (obj.TryGetProperty(propName, out var el)
            && el.ValueKind == JsonValueKind.Number
            && el.TryGetInt32(out value)
            && value >= 0)
        {
            return true;
        }

        value = 0;
        return false;
    }
}
