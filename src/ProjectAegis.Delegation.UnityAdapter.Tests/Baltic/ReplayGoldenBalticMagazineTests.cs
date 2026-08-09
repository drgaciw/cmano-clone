namespace ProjectAegis.Delegation.UnityAdapter.Tests.Baltic;

using ProjectAegis.Delegation.UnityAdapter.Baltic;
using ProjectAegis.Sim.Glossary;
using NUnit.Framework;

[TestFixture]
public sealed class ReplayGoldenBalticMagazineTests
{
    [Test]
    public void Magazine_depletion_emits_WinchesterOrdnance_abort()
    {
        var result = BalticReplayHarness.Run(42, "baltic-patrol-magazine", ticks: 3);
        Assert.That(result.Fingerprint, Does.Contain("MagazineChange|"));
        // Pre-launch empty on a seeded magazine is logistics Winchester (hard gate), not NO_AMMO.
        Assert.That(result.Fingerprint, Does.Contain(AbortReasonCatalog.Engage.WINCHESTER_ORDNANCE));
        var magazineLines = result.Fingerprint.Split('\n').Count(l => l.Contains("MagazineChange|", StringComparison.Ordinal));
        Assert.That(magazineLines, Is.GreaterThanOrEqualTo(1));
    }
}