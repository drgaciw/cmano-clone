namespace ProjectAegis.Delegation.UnityAdapter.Tests.Baltic;

using ProjectAegis.Delegation.UnityAdapter.Baltic;
using NUnit.Framework;

[TestFixture]
public sealed class ReplayGoldenBalticSalvoTests
{
    [Test]
    public void Salvo_two_drains_magazine_on_first_engagement()
    {
        var result = BalticReplayHarness.Run(42, "baltic-patrol-salvo", ticks: 2);
        Assert.That(result.Fingerprint, Does.Contain("MagazineChange|"));
        Assert.That(result.Fingerprint, Does.Contain("|-2|fire"));
        Assert.That(result.Fingerprint, Does.Contain("|Kill|").Or.Contain("MagazineEmpty"));
    }
}