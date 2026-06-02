namespace ProjectAegis.Delegation.UnityAdapter.Tests.Baltic;

using ProjectAegis.Delegation.UnityAdapter.Baltic;
using NUnit.Framework;

[TestFixture]
public sealed class ReplayGoldenBalticMissionTests
{
    private static string GoldenPath =>
        Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "..",
            "tests", "regression", "replay-golden-baltic-mission-2026-06-02.txt"));

    [Test]
    public void Mission_scenario_matches_golden_fingerprint_lines()
    {
        var golden = File.ReadAllLines(GoldenPath)
            .Where(l => !l.StartsWith("#", StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(l))
            .ToArray();

        var result = BalticReplayHarness.Run(42, "baltic-patrol-mission", ticks: 4);
        foreach (var expected in golden.Where(l => l.IndexOf('|') >= 0))
        {
            Assert.That(result.Fingerprint, Does.Contain(expected.Trim()));
        }
    }
}