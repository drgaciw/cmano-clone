using ProjectAegis.Delegation.UnityAdapter.Baltic;
using ProjectAegis.Sim.Engage;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Baltic;

[TestFixture]
public sealed class BalticReplayHarnessReadinessPolicyTests
{
    [Test]
    public void Readiness_policy_applies_AIR_NOT_READY_without_explicit_harness_map()
    {
        var result = BalticReplayHarness.Run(7, "baltic-patrol-readiness", ticks: 5, mvpEngagement: true);

        Assert.That(result.EngagementCount, Is.GreaterThan(0));
        Assert.That(result.Fingerprint, Does.Contain("AIR_NOT_READY"));
    }
}