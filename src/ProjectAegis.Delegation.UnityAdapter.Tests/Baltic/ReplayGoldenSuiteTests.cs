namespace ProjectAegis.Delegation.UnityAdapter.Tests.Baltic;

using ProjectAegis.Delegation.UnityAdapter.Baltic;
using NUnit.Framework;

/// <summary>Blocking replay golden suite for CI (req 17).</summary>
[TestFixture]
public sealed class ReplayGoldenSuiteTests
{
    [TestCaseSource(nameof(AllCases))]
    public void Pinned_regression_case_matches_golden_hashes(ReplayGoldenRegressionCatalog.Case testCase)
    {
        var goldenPath = ReplayGoldenAssertions.ResolveRegressionGoldenPath(testCase.GoldenFile);
        Assert.That(File.Exists(goldenPath), Is.True, $"Missing golden file: {testCase.GoldenFile}");

        var golden = File.ReadAllLines(goldenPath);
        var a = BalticReplayHarness.Run(
            testCase.Seed,
            testCase.PolicyId,
            testCase.Ticks,
            mvpEngagement: testCase.MvpEngagement);
        var b = BalticReplayHarness.Run(
            testCase.Seed,
            testCase.PolicyId,
            testCase.Ticks,
            mvpEngagement: testCase.MvpEngagement);

        Assert.That(b.Fingerprint, Is.EqualTo(a.Fingerprint));
        Assert.That(b.WorldHash, Is.EqualTo(a.WorldHash));
        ReplayGoldenAssertions.AssertPinnedHashes(a, golden);

        foreach (var fragment in testCase.FingerprintMustContain)
        {
            Assert.That(a.Fingerprint, Does.Contain(fragment));
        }
    }

    private static IEnumerable<ReplayGoldenRegressionCatalog.Case> AllCases() =>
        ReplayGoldenRegressionCatalog.All;
}