using ProjectAegis.Sim.Engage;
using Xunit;

namespace ProjectAegis.Sim.Tests.Engage;

public sealed class DlzEngageGateTests
{
    private static readonly WeaponEnvelope Envelope = new(1_000, 100_000);

    [Fact]
    public void Early_personality_allows_approaching_band()
    {
        Assert.True(DlzEngageGate.AllowsLaunch(90_000, Envelope, DlzPersonality.Early));
    }

    [Fact]
    public void Normal_personality_blocks_approaching_band()
    {
        Assert.False(DlzEngageGate.AllowsLaunch(90_000, Envelope, DlzPersonality.Normal));
    }

    [Fact]
    public void Late_personality_requires_inner_zone_range()
    {
        Assert.False(DlzEngageGate.AllowsLaunch(80_000, Envelope, DlzPersonality.Late));
        Assert.True(DlzEngageGate.AllowsLaunch(50_000, Envelope, DlzPersonality.Late));
    }
}