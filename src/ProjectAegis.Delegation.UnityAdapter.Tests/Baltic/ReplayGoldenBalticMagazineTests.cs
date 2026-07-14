namespace ProjectAegis.Delegation.UnityAdapter.Tests.Baltic;

using ProjectAegis.Delegation.UnityAdapter.Baltic;
using ProjectAegis.Sim.Glossary;
using NUnit.Framework;

[TestFixture]
public sealed class ReplayGoldenBalticMagazineTests
{
    [Test]
    public void Magazine_depletion_emits_MagazineEmpty_abort()
    {
        // Catalog-seeded magazines may hold >1 round; run enough ticks to force depletion → NO_AMMO.
        var result = BalticReplayHarness.Run(42, "baltic-patrol-magazine", ticks: 40);
        Assert.That(result.Fingerprint, Does.Contain("MagazineChange|"));
        Assert.That(result.Fingerprint, Does.Contain(AbortReasonCatalog.Engage.NO_AMMO));
        var magazineLines = result.Fingerprint.Split('\n').Count(l => l.Contains("MagazineChange|", StringComparison.Ordinal));
        Assert.That(magazineLines, Is.GreaterThanOrEqualTo(1));
    }
}