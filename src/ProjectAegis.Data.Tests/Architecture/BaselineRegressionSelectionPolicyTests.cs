using System.Text.RegularExpressions;
using NUnit.Framework;
using ProjectAegis.Data.Verification;

namespace ProjectAegis.Data.Tests.Architecture;

/// <summary>
/// DRG-204 (ARCH-NFR-3 / VER-06): Baseline-regression policy, GitNexus-informed fast-PR
/// transitive test selection with fail-closed conservative fallback, and DRG-323 safe-reindex guard.
/// </summary>
[TestFixture]
public sealed class BaselineRegressionSelectionPolicyTests
{
    private static GitNexusIndexState HealthyIndex() =>
        new(
            HealthStatus: GitNexusIndexHealthStatus.Healthy,
            IndexedCommitSha: "4b5b4ca1",
            TargetCommitSha: "4b5b4ca1",
            NodeCount: 40296,
            EdgeCount: 82300,
            EmbeddingCount: 12000,
            HasGarbageIdsOrEmptyNames: false,
            VectorSearchStatus: "vector-index");

    [TestCase("src/ProjectAegis.Data/Evidence/EvidenceLedger.cs", new[]
    {
        "ProjectAegis.Data.Tests",
        "ProjectAegis.Data.Excel.Tests",
        "ProjectAegis.Sim.Tests",
        "ProjectAegis.Delegation.Tests",
        "ProjectAegis.Ai.Host.Tests",
        "ProjectAegis.Delegation.UnityAdapter.Tests",
        "ProjectAegis.Delegation.Demo.Tests",
        "ProjectAegis.MissionEditor.Cli.Tests",
    })]
    [TestCase("src/ProjectAegis.Sim/Engage/MvpEngagementResolver.cs", new[]
    {
        "ProjectAegis.Sim.Tests",
        "ProjectAegis.Delegation.Tests",
        "ProjectAegis.Ai.Host.Tests",
        "ProjectAegis.Delegation.UnityAdapter.Tests",
        "ProjectAegis.Delegation.Demo.Tests",
        "ProjectAegis.MissionEditor.Cli.Tests",
    })]
    [TestCase("src/ProjectAegis.Delegation/Ifc/IfcMissionThreadVerifier.cs", new[]
    {
        "ProjectAegis.Delegation.Tests",
        "ProjectAegis.Ai.Host.Tests",
        "ProjectAegis.Delegation.UnityAdapter.Tests",
        "ProjectAegis.Delegation.Demo.Tests",
        "ProjectAegis.MissionEditor.Cli.Tests",
    })]
    [TestCase("src/ProjectAegis.Delegation.UnityAdapter/Presentation/C2PresentationController.cs", new[]
    {
        "ProjectAegis.Delegation.UnityAdapter.Tests",
        "ProjectAegis.Delegation.Demo.Tests",
        "ProjectAegis.MissionEditor.Cli.Tests",
    })]
    [TestCase("src/ProjectAegis.Ai.Contracts/Contracts.cs", new[]
    {
        "ProjectAegis.Ai.Contracts.Tests",
        "ProjectAegis.Ai.Host.Tests",
        "ProjectAegis.Delegation.UnityAdapter.Tests",
        "ProjectAegis.Delegation.Demo.Tests",
        "ProjectAegis.MissionEditor.Cli.Tests",
    })]
    [TestCase("src/ProjectAegis.MissionEditor.Cli/Program.cs", new[]
    {
        "ProjectAegis.MissionEditor.Cli.Tests",
    })]
    [TestCase("src/ProjectAegis.Delegation.Demo/README.md", new[]
    {
        "ProjectAegis.Delegation.Demo.Tests",
    })]
    public void FastPr_maps_changed_assembly_to_exact_transitive_test_closure_deterministically(
        string changedPath,
        string[] expectedSelectedProjects)
    {
        var requestA = new TestSelectionRequest(
            RequestedTier: TestRunTier.FastPrInnerLoop,
            ChangedPaths: new[] { changedPath, "src/ProjectAegis.Delegation.Demo/README.md" },
            IndexState: HealthyIndex(),
            ImpactRisk: GitNexusRiskLevel.Low,
            IsWorkingTreeClean: true);

        var requestB = new TestSelectionRequest(
            RequestedTier: TestRunTier.FastPrInnerLoop,
            ChangedPaths: new[] { "src/ProjectAegis.Delegation.Demo/README.md", changedPath },
            IndexState: HealthyIndex(),
            ImpactRisk: GitNexusRiskLevel.Low,
            IsWorkingTreeClean: true);

        var planA = GitNexusTestSelectionEngine.BuildPlan(requestA);
        var planB = GitNexusTestSelectionEngine.BuildPlan(requestB);

        Assert.That(requestA.IsWorkingTreeClean, Is.True);
        Assert.That(requestA.ImpactedSymbols, Is.Null);
        Assert.That(planA.RunFullSolution, Is.False);
        Assert.That(planA.IsConservativeFallback, Is.False);
        Assert.That(planA.RequireArchitectureFences, Is.True);
        Assert.That(planA.RecommendedTestFilters, Is.Not.Null);
        Assert.That(planA.FallbackReasons, Is.Empty);
        Assert.That(planA.SelectedTestProjects, Is.EqualTo(planB.SelectedTestProjects));

        var expectedUnion = expectedSelectedProjects
            .Union(new[] { "ProjectAegis.Delegation.Demo.Tests" }, StringComparer.Ordinal)
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToArray();
        Assert.That(planA.SelectedTestProjects, Is.EqualTo(expectedUnion));
    }

    [TestCase(TestRunTier.BlockingMergeBaseline)]
    [TestCase(TestRunTier.ReleaseVerification)]
    public void BlockingMerge_and_Release_tiers_always_require_full_solution_even_for_single_leaf_change(
        TestRunTier tier)
    {
        var plan = GitNexusTestSelectionEngine.BuildPlan(new TestSelectionRequest(
            RequestedTier: tier,
            ChangedPaths: new[] { "src/ProjectAegis.Delegation.Demo/README.md" },
            IndexState: HealthyIndex(),
            ImpactRisk: GitNexusRiskLevel.Low,
            IsWorkingTreeClean: true));

        Assert.That(plan.RunFullSolution, Is.True);
        Assert.That(plan.RequireReplayGoldenAndPlayModeSmoke, Is.True);
        Assert.That(plan.RequireArchitectureFences, Is.True);
        Assert.That(plan.SelectedTestProjects, Is.EqualTo(GitNexusTestSelectionEngine.AllSolutionTestProjects));
    }

    [TestCase(GitNexusIndexHealthStatus.Missing, "4b5b4ca1", 40296, 82300, 12000, false, "vector-index", GitNexusRiskLevel.Low, "src/ProjectAegis.Delegation.Demo/README.md")]
    [TestCase(GitNexusIndexHealthStatus.Stale, "4b5b4ca1", 40296, 82300, 12000, false, "vector-index", GitNexusRiskLevel.Low, "src/ProjectAegis.Delegation.Demo/README.md")]
    [TestCase(GitNexusIndexHealthStatus.Healthy, "oldsha00", 40296, 82300, 12000, false, "vector-index", GitNexusRiskLevel.Low, "src/ProjectAegis.Delegation.Demo/README.md")]
    [TestCase(GitNexusIndexHealthStatus.Healthy, "4b5b4ca1", 40296, 82300, 12000, true, "vector-index", GitNexusRiskLevel.Low, "src/ProjectAegis.Delegation.Demo/README.md")]
    [TestCase(GitNexusIndexHealthStatus.Healthy, "4b5b4ca1", 40296, 82300, 0, false, "vector-index", GitNexusRiskLevel.Low, "src/ProjectAegis.Delegation.Demo/README.md")]
    [TestCase(GitNexusIndexHealthStatus.Healthy, "4b5b4ca1", 40296, 82300, 12000, false, "unavailable", GitNexusRiskLevel.Low, "src/ProjectAegis.Delegation.Demo/README.md")]
    [TestCase(GitNexusIndexHealthStatus.Unavailable, "4b5b4ca1", 40296, 82300, 12000, false, "vector-index", GitNexusRiskLevel.Low, "src/ProjectAegis.Delegation.Demo/README.md")]
    [TestCase(GitNexusIndexHealthStatus.SharedStoreConflict, "4b5b4ca1", 40296, 82300, 12000, false, "vector-index", GitNexusRiskLevel.Low, "src/ProjectAegis.Delegation.Demo/README.md")]
    [TestCase(GitNexusIndexHealthStatus.Healthy, "4b5b4ca1", 40296, 82300, 12000, false, "vector-index", GitNexusRiskLevel.High, "src/ProjectAegis.Delegation.Demo/README.md")]
    [TestCase(GitNexusIndexHealthStatus.Healthy, "4b5b4ca1", 40296, 82300, 12000, false, "vector-index", GitNexusRiskLevel.Critical, "src/ProjectAegis.Delegation.Demo/README.md")]
    [TestCase(GitNexusIndexHealthStatus.Healthy, "4b5b4ca1", 40296, 82300, 12000, false, "vector-index", GitNexusRiskLevel.Unknown, "src/ProjectAegis.Delegation.Demo/README.md")]
    [TestCase(GitNexusIndexHealthStatus.Healthy, "4b5b4ca1", 40296, 82300, 12000, false, "vector-index", GitNexusRiskLevel.Low, "")]
    [TestCase(GitNexusIndexHealthStatus.Healthy, "4b5b4ca1", 40296, 82300, 12000, false, "vector-index", GitNexusRiskLevel.Low, "unknown/unmapped.txt")]
    [TestCase(GitNexusIndexHealthStatus.Healthy, "4b5b4ca1", 40296, 82300, 12000, false, "vector-index", GitNexusRiskLevel.Low, "ProjectAegis.sln")]
    [TestCase(GitNexusIndexHealthStatus.Healthy, "4b5b4ca1", 40296, 82300, 12000, false, "vector-index", GitNexusRiskLevel.Low, "Directory.Build.props")]
    [TestCase(GitNexusIndexHealthStatus.Healthy, "4b5b4ca1", 40296, 82300, 12000, false, "vector-index", GitNexusRiskLevel.Low, "production/test-floor.json")]
    [TestCase(GitNexusIndexHealthStatus.Healthy, "4b5b4ca1", 40296, 82300, 12000, false, "vector-index", GitNexusRiskLevel.Low, "data/scenarios/baltic-patrol.policy.json")]
    [TestCase(GitNexusIndexHealthStatus.Healthy, "4b5b4ca1", 40296, 82300, 12000, false, "vector-index", GitNexusRiskLevel.Low, "tests/regression/README.md")]
    [TestCase(GitNexusIndexHealthStatus.Healthy, "4b5b4ca1", 40296, 82300, 12000, false, "vector-index", GitNexusRiskLevel.Low, ".buildkite/pipeline.yml")]
    [TestCase(GitNexusIndexHealthStatus.Healthy, "4b5b4ca1", 40296, 82300, 12000, false, "vector-index", GitNexusRiskLevel.Low, "src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs")]
    public void FastPr_fails_closed_to_full_solution_when_gitnexus_index_unhealthy_stale_corrupted_or_unavailable(
        GitNexusIndexHealthStatus status,
        string indexedSha,
        int nodeCount,
        int edgeCount,
        int embeddingCount,
        bool garbageNames,
        string vectorStatus,
        GitNexusRiskLevel risk,
        string path)
    {
        var paths = string.IsNullOrEmpty(path) ? Array.Empty<string>() : new[] { path };
        var plan = GitNexusTestSelectionEngine.BuildPlan(new TestSelectionRequest(
            RequestedTier: TestRunTier.FastPrInnerLoop,
            ChangedPaths: paths,
            IndexState: new GitNexusIndexState(status, indexedSha, "4b5b4ca1", nodeCount, edgeCount, embeddingCount, garbageNames, vectorStatus),
            ImpactRisk: risk,
            IsWorkingTreeClean: true));

        Assert.That(plan.RunFullSolution, Is.True);
        Assert.That(plan.IsConservativeFallback, Is.True);
        Assert.That(plan.FallbackReasons, Is.Not.Empty);
        Assert.That(plan.SelectedTestProjects, Is.EqualTo(GitNexusTestSelectionEngine.AllSolutionTestProjects));
    }

    [Test]
    public void Drg323_reindex_policy_forbids_dirty_tree_non_main_branch_active_mcp_and_shared_store()
    {
        var strictFeature = GitNexusReindexPolicy.EvaluateStrictFeatureBranchPolicy(
            new GitNexusReindexContext(
                BranchName: "sprint/s6-gauntlet-g2-gate",
                IsWorkingTreeClean: true,
                HasActiveMcpServer: false,
                HasSharedStorePointer: false,
                AllowBranchOverride: true,
                AllowDirtyOverride: true));
        Assert.That(strictFeature.Allowed, Is.False);
        Assert.That(strictFeature.RefusalReasons, Is.Not.Empty);

        var dirtyMain = GitNexusReindexPolicy.EvaluateStrictFeatureBranchPolicy(
            new GitNexusReindexContext("main", IsWorkingTreeClean: false, HasActiveMcpServer: false, HasSharedStorePointer: false));
        Assert.That(dirtyMain.Allowed, Is.False);

        var activeMcp = GitNexusReindexPolicy.Evaluate(
            new GitNexusReindexContext("main", IsWorkingTreeClean: true, HasActiveMcpServer: true, HasSharedStorePointer: false));
        Assert.That(activeMcp.Allowed, Is.False);

        var sharedStore = GitNexusReindexPolicy.Evaluate(
            new GitNexusReindexContext("main", IsWorkingTreeClean: true, HasActiveMcpServer: false, HasSharedStorePointer: true));
        Assert.That(sharedStore.Allowed, Is.False);

        var cleanMain = GitNexusReindexPolicy.Evaluate(
            new GitNexusReindexContext("main", IsWorkingTreeClean: true, HasActiveMcpServer: false, HasSharedStorePointer: false));
        Assert.That(cleanMain.Allowed, Is.True);
        Assert.That(cleanMain.MustUseFullForceWithEmbeddings, Is.True);
        Assert.That(cleanMain.CanonicalCommand, Is.EqualTo(GitNexusReindexPolicy.CanonicalSafeReindexCommand));
        Assert.That(GitNexusReindexPolicy.CanonicalAnalyzeArgs, Does.Contain("--force"));
        Assert.That(GitNexusReindexPolicy.CanonicalAnalyzeArgs, Does.Contain("--embeddings"));
    }

    [Test]
    public void Test_floor_json_parses_real_repo_manifest_and_enforces_monotonicity_and_release_gates()
    {
        var repoRoot = FindRepoRoot();
        var jsonPath = Path.Combine(repoRoot, "production", "test-floor.json");
        var json = File.ReadAllText(jsonPath);

        var parsed = BaselineRegressionPolicy.TryParseTestFloorJson(json, out var manifest, out var parseError);
        Assert.That(parsed, Is.True, parseError);
        Assert.That(manifest, Is.Not.Null);
        Assert.That(manifest!.SolutionMinPass, Is.EqualTo(4361));
        Assert.That(manifest.ReplayGoldenMin, Is.EqualTo(6));
        Assert.That(manifest.PlayModeSmokeMin, Is.EqualTo(20));
        Assert.That(manifest.BalticV2Hash, Is.EqualTo(BaselineRegressionPolicy.ImmutableBalticV2Hash));

        var regressionLower = manifest with { SolutionMinPass = 4360 };
        var monoLower = BaselineRegressionPolicy.ValidateFloorMonotonicity(manifest, regressionLower);
        Assert.That(monoLower.Passed, Is.False);

        var hashDrift = manifest with { BalticV2Hash = "00000000000000000000" };
        Assert.That(BaselineRegressionPolicy.ValidateFloorMonotonicity(manifest, hashDrift).Passed, Is.False);

        var warnDrift = manifest with { BaselineWarnings = 1 };
        Assert.That(BaselineRegressionPolicy.ValidateFloorMonotonicity(manifest, warnDrift).Passed, Is.False);

        var passingInput = new BaselineGateEvaluationInput(
            BuildErrors: 0,
            BuildWarnings: 0,
            ObservedPassed: 5016,
            ObservedFailed: 0,
            ObservedSkipped: 0,
            ReplayGoldenPassed: 17,
            PlayModeSmokePassed: 27,
            ArchitectureFencesPassed: 31,
            ArchitectureFencesFailed: 0,
            ObservedBalticV2Hash: BaselineRegressionPolicy.ImmutableBalticV2Hash,
            HasTrackedDb3Files: false,
            DelegationBridgeHotpathDiffCount: 0);
        Assert.That(
            BaselineRegressionPolicy.EvaluateBaselineGate(manifest, passingInput, sprintEffectiveSolutionFloor: 4987).Passed,
            Is.True);

        var underSprintFloor = passingInput with { ObservedPassed = 4986 };
        Assert.That(
            BaselineRegressionPolicy.EvaluateBaselineGate(manifest, underSprintFloor, sprintEffectiveSolutionFloor: 4987).Passed,
            Is.False);

        var bridgeTouched = passingInput with { DelegationBridgeHotpathDiffCount = 1 };
        Assert.That(BaselineRegressionPolicy.EvaluateBaselineGate(manifest, bridgeTouched).Passed, Is.False);

        var trackedDb3 = passingInput with { HasTrackedDb3Files = true };
        Assert.That(BaselineRegressionPolicy.EvaluateBaselineGate(manifest, trackedDb3).Passed, Is.False);

        var lowFences = passingInput with { ArchitectureFencesPassed = 30 };
        Assert.That(BaselineRegressionPolicy.EvaluateBaselineGate(manifest, lowFences).Passed, Is.False);
    }

    [Test]
    public void FastPr_docs_change_selects_both_data_and_delegation_tests()
    {
        var plan = GitNexusTestSelectionEngine.BuildPlan(new TestSelectionRequest(
            RequestedTier: TestRunTier.FastPrInnerLoop,
            ChangedPaths: new[] { "docs/engineering/editor-verb-honesty.md" },
            IndexState: HealthyIndex(),
            ImpactRisk: GitNexusRiskLevel.Low,
            IsWorkingTreeClean: true));

        Assert.That(plan.RunFullSolution, Is.False);
        Assert.That(plan.IsConservativeFallback, Is.False);
        Assert.That(plan.SelectedTestProjects, Is.EquivalentTo(new[]
        {
            "ProjectAegis.Data.Tests",
            "ProjectAegis.Delegation.Tests",
        }));
    }

    [Test]
    public void FastPr_normalizes_leading_dot_slash_without_false_fallback()
    {
        var plan = GitNexusTestSelectionEngine.BuildPlan(new TestSelectionRequest(
            RequestedTier: TestRunTier.FastPrInnerLoop,
            ChangedPaths: new[] { "./src/ProjectAegis.Delegation.Demo/README.md" },
            IndexState: HealthyIndex(),
            ImpactRisk: GitNexusRiskLevel.Low,
            IsWorkingTreeClean: false));

        Assert.That(plan.RunFullSolution, Is.False);
        Assert.That(plan.IsConservativeFallback, Is.False);
        Assert.That(plan.FallbackReasons, Is.Empty);
        Assert.That(plan.SelectedTestProjects, Is.EqualTo(new[] { "ProjectAegis.Delegation.Demo.Tests" }));
    }

    [Test]
    public void Test_floor_and_baseline_gate_reject_negative_integer_fields()
    {
        const string negativeFloorJson = """
        {
          "solutionMinPass": -5,
          "replayGoldenMin": 6,
          "playModeSmokeMin": 20,
          "balticV2Hash": "17144800277401907079",
          "baselineWarnings": 0,
          "baselinePluginDllCount": 14
        }
        """;
        Assert.That(BaselineRegressionPolicy.TryParseTestFloorJson(negativeFloorJson, out _, out _), Is.False);

        const string negativeWarningsJson = """
        {
          "solutionMinPass": 4361,
          "replayGoldenMin": 6,
          "playModeSmokeMin": 20,
          "balticV2Hash": "17144800277401907079",
          "baselineWarnings": -1,
          "baselinePluginDllCount": 14
        }
        """;
        Assert.That(BaselineRegressionPolicy.TryParseTestFloorJson(negativeWarningsJson, out _, out _), Is.False);

        var validFloor = new TestFloorManifest(4361, 6, 20, BaselineRegressionPolicy.ImmutableBalticV2Hash, 0, 14);
        var negativeSentinelInput = new BaselineGateEvaluationInput(
            BuildErrors: -1,
            BuildWarnings: -1,
            ObservedPassed: 5016,
            ObservedFailed: -1,
            ObservedSkipped: -1,
            ReplayGoldenPassed: 17,
            PlayModeSmokePassed: 27,
            ArchitectureFencesPassed: 31,
            ArchitectureFencesFailed: -1,
            ObservedBalticV2Hash: BaselineRegressionPolicy.ImmutableBalticV2Hash,
            HasTrackedDb3Files: false,
            DelegationBridgeHotpathDiffCount: 0);
        var verdict = BaselineRegressionPolicy.EvaluateBaselineGate(validFloor, negativeSentinelInput);
        Assert.That(verdict.Passed, Is.False);
        Assert.That(verdict.Violations, Is.Not.Empty);
    }

    [Test]
    public void Drg204_policy_doc_exists_is_indexed_and_cites_existing_repo_paths_and_scripts()
    {
        var repoRoot = FindRepoRoot();
        var docPath = Path.Combine(repoRoot, "docs", "engineering", "baseline-regression-and-test-selection-policy.md");
        var readmePath = Path.Combine(repoRoot, "docs", "engineering", "README.md");

        Assert.That(File.Exists(docPath), Is.True, "docs/engineering/baseline-regression-and-test-selection-policy.md must exist");
        var docText = File.ReadAllText(docPath);
        var readmeText = File.ReadAllText(readmePath);

        Assert.That(readmeText, Does.Contain("baseline-regression-and-test-selection-policy.md"));
        Assert.That(docText, Does.Contain("ARCH-NFR-3"));
        Assert.That(docText, Does.Contain("VER-06"));
        Assert.That(docText, Does.Contain("DRG-204"));
        Assert.That(docText, Does.Contain("DRG-323"));
        Assert.That(docText, Does.Contain("tools/gitnexus-safe-reindex.sh"));
        Assert.That(docText, Does.Contain("tools/buildkite/dotnet-ci.sh"));
        Assert.That(docText, Does.Contain("production/test-floor.json"));
        Assert.That(docText, Does.Contain("17144800277401907079"));

        var pathMatches = Regex.Matches(docText, @"`((?:src|tools|docs|production|data/scenarios|tests/regression|\.github/workflows|\.buildkite)/[^`\s]+\.(?:cs|sh|ps1|json|md|yml))`");
        Assert.That(pathMatches.Count, Is.GreaterThanOrEqualTo(6));
        foreach (Match match in pathMatches)
        {
            var rel = match.Groups[1].Value;
            var full = Path.Combine(repoRoot, rel.Replace('/', Path.DirectorySeparatorChar));
            Assert.That(File.Exists(full), Is.True, $"Cited repo path does not exist: {rel}");
        }
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "ProjectAegis.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("ProjectAegis.sln not found");
    }
}
