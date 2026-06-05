using ProjectAegis.Delegation.UnityAdapter.Baltic;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Baltic;

[TestFixture]
public sealed class BalticReplayHarnessStaleTests
{
    [Test]
    public void Stale_threshold_one_emits_detected_to_lost()
    {
        var result = BalticReplayHarness.Run(11, "baltic-patrol-stale", ticks: 3);
        Assert.That(result.Fingerprint, Does.Contain("ContactChange|"));
        Assert.That(result.Fingerprint, Does.Contain("|Detected|Lost"));
    }
}