namespace ProjectAegis.Delegation.UnityAdapter.Tests.Baltic;

using ProjectAegis.Delegation.UnityAdapter.Baltic;
using NUnit.Framework;

[TestFixture]
public sealed class BalticReplayHarnessTests
{
    [Test]
    public void Run_produces_stable_fingerprint_for_fixed_seed()
    {
        var a = BalticReplayHarness.Run(42, "baltic-patrol", ticks: 4);
        var b = BalticReplayHarness.Run(42, "baltic-patrol", ticks: 4);

        Assert.That(b.Fingerprint, Is.EqualTo(a.Fingerprint));
        Assert.That(a.EngagementCount, Is.GreaterThan(0));
    }

    [Test]
    public void Run_without_engage_has_empty_engagement_log()
    {
        var result = BalticReplayHarness.Run(7, "baltic-patrol", ticks: 2, mvpEngagement: false);

        Assert.That(result.EngagementCount, Is.EqualTo(0));
    }
}