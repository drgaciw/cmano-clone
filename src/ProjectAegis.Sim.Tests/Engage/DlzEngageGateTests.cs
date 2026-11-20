using ProjectAegis.Sim.Engage;
using NUnit.Framework;

namespace ProjectAegis.Sim.Tests.Engage;

public sealed class DlzEngageGateTests
{
    private static readonly WeaponEnvelope Envelope = new(1_000, 100_000);

    [Test]
    public void Early_personality_allows_approaching_band()
    {
        Assert.That(DlzEngageGate.AllowsLaunch(90_000, Envelope, DlzPersonality.Early), Is.True);
    }

    [Test]
    public void Normal_personality_blocks_approaching_band()
    {
        Assert.That(DlzEngageGate.AllowsLaunch(90_000, Envelope), Is.False);
    }

    [Test]
    public void Late_personality_requires_inner_zone_range()
    {
        Assert.That(DlzEngageGate.AllowsLaunch(80_000, Envelope, DlzPersonality.Late), Is.False);
        Assert.That(DlzEngageGate.AllowsLaunch(50_000, Envelope, DlzPersonality.Late), Is.True);
    }
}