namespace ProjectAegis.Delegation.UnityAdapter.Tests.Baltic;

using ProjectAegis.Delegation.UnityAdapter.Baltic;
using NUnit.Framework;

[TestFixture]
public sealed class ReplayGoldenBalticCheckpointTests
{
    private static string GoldenPath =>
        Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "..",
            "tests", "regression", "replay-golden-baltic-replay-checkpoints-2026-06-02.txt"));

    [Test]
    public void Replay_scenario_matches_golden_checkpoint_hashes()
    {
        var golden = File.ReadAllLines(GoldenPath)
            .Where(l => l.StartsWith("REPLAY_CHECKPOINT=", StringComparison.Ordinal))
            .ToArray();

        var result = BalticReplayHarness.Run(42, "baltic-patrol-replay", ticks: 4);
        Assert.That(result.Checkpoints, Has.Count.EqualTo(golden.Length));

        for (var i = 0; i < golden.Length; i++)
        {
            var parts = golden[i]["REPLAY_CHECKPOINT=".Length..].Split(':');
            var tick = ulong.Parse(parts[0]);
            var worldHash = ulong.Parse(parts[1]);
            var lastSeq = ulong.Parse(parts[2]);

            Assert.That(result.Checkpoints[i].SimTick, Is.EqualTo(tick));
            Assert.That(result.Checkpoints[i].WorldHash, Is.EqualTo(worldHash));
            Assert.That(result.Checkpoints[i].LastSequenceId, Is.EqualTo(lastSeq));
        }
    }
}