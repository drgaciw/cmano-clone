namespace ProjectAegis.Delegation.UnityAdapter.Tests.Baltic;

using ProjectAegis.Delegation.UnityAdapter.Baltic;
using NUnit.Framework;

[TestFixture]
public sealed class ReplayGoldenBalticMagazineTests
{
    [Test]
    public void Magazine_depletion_emits_MagazineEmpty_abort()
    {
        var result = BalticReplayHarness.Run(42, "baltic-patrol-magazine", ticks: 3);
        Assert.That(result.Fingerprint, Does.Contain("MagazineChange|"));
        Assert.That(result.Fingerprint, Does.Contain("MagazineEmpty"));
        var magazineLines = result.Fingerprint.Split('\n').Count(l => l.Contains("MagazineChange|", StringComparison.Ordinal));
        Assert.That(magazineLines, Is.GreaterThanOrEqualTo(1));
    }
}