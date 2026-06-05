namespace ProjectAegis.Delegation.UnityAdapter.Tests.Baltic;

using ProjectAegis.Delegation.UnityAdapter.Baltic;
using NUnit.Framework;

[TestFixture]
public sealed class ReplayGoldenBalticPatrolCandidatesTests
{
    [Test]
    public void Catalog_scenario_with_patrol_candidates_preserves_world_hash()
    {
        var baseline = BalticReplayHarness.Run(42, "baltic-patrol-catalog", ticks: 4);
        var result = BalticReplayHarness.Run(42, "baltic-patrol-catalog", ticks: 4);

        Assert.That(result.WorldHash, Is.EqualTo(baseline.WorldHash));
        Assert.That(result.Fingerprint, Does.Contain("Move:0.8:Low"));
        Assert.That(result.Fingerprint, Does.Contain("Hold:1:Low"));
        Assert.That(result.Fingerprint, Does.Contain("Engage:99:High"));
    }
}