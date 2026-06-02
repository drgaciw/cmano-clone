namespace ProjectAegis.Delegation.UnityAdapter.Tests.Baltic;

using ProjectAegis.Delegation.UnityAdapter.Baltic;
using NUnit.Framework;

[TestFixture]
public sealed class BalticReplayHarnessOutcomeTests
{
    [Test]
    public void Baltic_patrol_includes_engagement_outcome_rows()
    {
        var a = BalticReplayHarness.Run(42, "baltic-patrol", ticks: 4);
        var b = BalticReplayHarness.Run(42, "baltic-patrol", ticks: 4);

        Assert.That(a.Fingerprint, Does.Contain("EngagementOutcome|"));
        Assert.That(b.Fingerprint, Is.EqualTo(a.Fingerprint));
        Assert.That(a.EngagementCount, Is.GreaterThan(0));
    }
}