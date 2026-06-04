using ProjectAegis.Delegation.UnityAdapter.Baltic;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Glossary;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Baltic;

[TestFixture]
public sealed class BalticReplayHarnessSpoofTests
{
    [Test]
    public void Spoof_policy_aborts_engage_with_CYBER_SPOOF_TRACK()
    {
        var result = BalticReplayHarness.Run(7, "baltic-patrol-spoof", ticks: 5, mvpEngagement: true);

        Assert.That(result.EngagementCount, Is.GreaterThan(0));
        Assert.That(result.Fingerprint, Does.Contain(AbortReasonCatalog.Cyber.CYBER_SPOOF_TRACK));
    }
}