namespace ProjectAegis.Sim.Tests.Time;

using ProjectAegis.Sim.Time;
using NUnit.Framework;

/// <summary>S117 / PRD P0-8 — weapons-release forced 1× precedence.</summary>
[TestFixture]
public sealed class SimClockWeaponsReleaseTests
{
    [Test]
    public void ForceRealTimeForWeaponsRelease_sets_factor_1_and_flag()
    {
        var clock = new SimClock();
        clock.SetAccelerationFactor(16);
        clock.ForceRealTimeForWeaponsRelease();

        Assert.That(clock.AccelerationFactor, Is.EqualTo(1));
        Assert.That(clock.IsWeaponsReleaseForced1x, Is.True);
    }

    [Test]
    public void SetAccelerationFactor_while_forced_clamps_to_1()
    {
        var clock = new SimClock();
        clock.ForceRealTimeForWeaponsRelease();
        clock.SetAccelerationFactor(32);

        Assert.That(clock.AccelerationFactor, Is.EqualTo(1));
        Assert.That(clock.IsWeaponsReleaseForced1x, Is.True);
    }

    [Test]
    public void ClearWeaponsReleaseForced1x_allows_player_compression_again()
    {
        var clock = new SimClock();
        clock.ForceRealTimeForWeaponsRelease();
        clock.ClearWeaponsReleaseForced1x();
        clock.SetAccelerationFactor(8);

        Assert.That(clock.IsWeaponsReleaseForced1x, Is.False);
        Assert.That(clock.AccelerationFactor, Is.EqualTo(8));
    }
}
