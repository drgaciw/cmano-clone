using ProjectAegis.Delegation.UnityAdapter.Baltic;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Baltic;

[TestFixture]
public sealed class BalticReplayHarnessEmconTests
{
    [Test]
    public void Emcon_off_scenario_has_no_contacts_and_EmconOff_engagement()
    {
        var result = BalticReplayHarness.Run(7, "baltic-patrol-emcon-off", ticks: 2);
        Assert.That(result.Fingerprint, Does.Not.Contain("ContactChange|"));
        Assert.That(result.Fingerprint, Does.Contain("EmconOff"));
    }
}