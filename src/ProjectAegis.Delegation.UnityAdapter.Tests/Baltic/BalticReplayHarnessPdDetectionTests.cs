using ProjectAegis.Delegation.UnityAdapter.Baltic;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Baltic;

[TestFixture]
public sealed class BalticReplayHarnessPdDetectionTests
{
    [Test]
    public void Baltic_patrol_pd_detection_fingerprint_is_stable()
    {
        var a = BalticReplayHarness.Run(42, "baltic-patrol", ticks: 2);
        var b = BalticReplayHarness.Run(42, "baltic-patrol", ticks: 2);
        Assert.That(a.Fingerprint, Is.EqualTo(b.Fingerprint));
        Assert.That(a.Fingerprint, Does.Contain("ContactChange|"));
        Assert.That(a.Fingerprint, Does.Contain("Engagement|"));
    }
}