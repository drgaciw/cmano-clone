using ProjectAegis.Delegation.UnityAdapter.Baltic;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Baltic;

[TestFixture]
public sealed class BalticReplayHarnessContactTests
{
    [Test]
    public void Baltic_patrol_run_includes_contact_change_in_fingerprint()
    {
        var a = BalticReplayHarness.Run(42, "baltic-patrol", ticks: 2);
        var b = BalticReplayHarness.Run(42, "baltic-patrol", ticks: 2);
        Assert.That(a.Fingerprint, Is.EqualTo(b.Fingerprint));
        Assert.That(a.Fingerprint, Does.Contain("ContactChange|"));
        Assert.That(a.Fingerprint, Does.Contain("|Unknown|Detected"));
    }
}