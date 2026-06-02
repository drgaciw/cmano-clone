namespace ProjectAegis.Delegation.UnityAdapter.Tests.Baltic;

using ProjectAegis.Delegation.UnityAdapter.Baltic;
using NUnit.Framework;

[TestFixture]
public sealed class ReplayGoldenBalticInterceptCheckpointTests
{
    private static string GoldenPath =>
        Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "..",
            "tests", "regression", "replay-golden-baltic-intercept-checkpoints-2026-06-02.txt"));

    [Test]
    public void Intercept_scenario_matches_golden_checkpoint_hashes()
    {
        var golden = File.ReadAllLines(GoldenPath)
            .Where(l => l.StartsWith("REPLAY_CHECKPOINT=", StringComparison.Ordinal))
            .ToArray();

        var result = BalticReplayHarness.Run(42, "baltic-patrol-intercept", ticks: 4);
        Assert.That(result.Checkpoints, Has.Count.EqualTo(golden.Length));

        for (var i = 0; i < golden.Length; i++)
        {
            var parts = golden[i]["REPLAY_CHECKPOINT=".Length..].Split(':');
            Assert.That(result.Checkpoints[i].SimTick, Is.EqualTo(ulong.Parse(parts[0])));
            Assert.That(result.Checkpoints[i].WorldHash, Is.EqualTo(ulong.Parse(parts[1])));
            Assert.That(result.Checkpoints[i].LastSequenceId, Is.EqualTo(ulong.Parse(parts[2])));
        }
    }
}