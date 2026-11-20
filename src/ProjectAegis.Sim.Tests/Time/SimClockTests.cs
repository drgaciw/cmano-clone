using ProjectAegis.Sim.Time;
using NUnit.Framework;

namespace ProjectAegis.Sim.Tests.Time;

public sealed class SimClockTests
{
    [Test]
    public void Defaults_unpaused_with_acceleration_one()
    {
        var clock = new SimClock();
        Assert.That(clock.IsPaused, Is.False);
        Assert.That(clock.AccelerationFactor, Is.EqualTo(1));
        Assert.That(clock.SimTick, Is.EqualTo(0UL));
    }

    [Test]
    public void Pause_and_Resume_toggle_IsPaused()
    {
        var clock = new SimClock();
        clock.Pause();
        Assert.That(clock.IsPaused, Is.True);
        clock.Resume();
        Assert.That(clock.IsPaused, Is.False);
    }

    [Test]
    public void SetAccelerationFactor_clamps_to_1_through_256()
    {
        var clock = new SimClock();
        clock.SetAccelerationFactor(4);
        Assert.That(clock.AccelerationFactor, Is.EqualTo(4));

        clock.SetAccelerationFactor(0);
        Assert.That(clock.AccelerationFactor, Is.EqualTo(1));

        clock.SetAccelerationFactor(-10);
        Assert.That(clock.AccelerationFactor, Is.EqualTo(1));

        clock.SetAccelerationFactor(256);
        Assert.That(clock.AccelerationFactor, Is.EqualTo(256));

        clock.SetAccelerationFactor(512);
        Assert.That(clock.AccelerationFactor, Is.EqualTo(256));
    }

    [Test]
    public void AdvanceOneTick_and_Reset_still_work()
    {
        var clock = new SimClock(fixedDeltaSeconds: 0.5);
        clock.AdvanceOneTick();
        clock.AdvanceOneTick();
        Assert.That(clock.SimTick, Is.EqualTo(2UL));
        Assert.That(
            Math.Round(clock.SimTime, 5, MidpointRounding.AwayFromZero),
            Is.EqualTo(Math.Round(1.0, 5, MidpointRounding.AwayFromZero)));

        clock.Reset(10);
        Assert.That(clock.SimTick, Is.EqualTo(10UL));
        Assert.That(
            Math.Round(clock.SimTime, 5, MidpointRounding.AwayFromZero),
            Is.EqualTo(Math.Round(5.0, 5, MidpointRounding.AwayFromZero)));
    }
}
