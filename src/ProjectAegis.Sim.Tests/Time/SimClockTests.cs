using ProjectAegis.Sim.Time;
using Xunit;

namespace ProjectAegis.Sim.Tests.Time;

public sealed class SimClockTests
{
    [Fact]
    public void Defaults_unpaused_with_acceleration_one()
    {
        var clock = new SimClock();
        Assert.False(clock.IsPaused);
        Assert.Equal(1, clock.AccelerationFactor);
        Assert.Equal(0UL, clock.SimTick);
    }

    [Fact]
    public void Pause_and_Resume_toggle_IsPaused()
    {
        var clock = new SimClock();
        clock.Pause();
        Assert.True(clock.IsPaused);
        clock.Resume();
        Assert.False(clock.IsPaused);
    }

    [Fact]
    public void SetAccelerationFactor_clamps_to_1_through_256()
    {
        var clock = new SimClock();
        clock.SetAccelerationFactor(4);
        Assert.Equal(4, clock.AccelerationFactor);

        clock.SetAccelerationFactor(0);
        Assert.Equal(1, clock.AccelerationFactor);

        clock.SetAccelerationFactor(-10);
        Assert.Equal(1, clock.AccelerationFactor);

        clock.SetAccelerationFactor(256);
        Assert.Equal(256, clock.AccelerationFactor);

        clock.SetAccelerationFactor(512);
        Assert.Equal(256, clock.AccelerationFactor);
    }

    [Fact]
    public void AdvanceOneTick_and_Reset_still_work()
    {
        var clock = new SimClock(fixedDeltaSeconds: 0.5);
        clock.AdvanceOneTick();
        clock.AdvanceOneTick();
        Assert.Equal(2UL, clock.SimTick);
        Assert.Equal(1.0, clock.SimTime, precision: 5);

        clock.Reset(10);
        Assert.Equal(10UL, clock.SimTick);
        Assert.Equal(5.0, clock.SimTime, precision: 5);
    }
}
