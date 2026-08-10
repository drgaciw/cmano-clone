using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Sim;
using ProjectAegis.Delegation.Tests.Helpers;
using ProjectAegis.Sim.Engage;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Orchestration;

/// <summary>S112-02 / DRG-14 — session-level sim pause / resume / acceleration controls.</summary>
[TestFixture]
public sealed class SimulationSessionClockControlsTests
{
    [Test]
    public void PauseSim_freezes_SimClock_SimTick_across_Tick()
    {
        var session = new SimulationSession(1, new StubEngagementResolver());
        session.BeginExecution();

        Assert.That(session.Tick(MvpObservedStates.EngageTick(0)), Is.True);
        var tickAfterAdvance = session.Sim.Clock.SimTick;
        Assert.That(tickAfterAdvance, Is.EqualTo(1UL));

        session.PauseSim();
        Assert.That(session.IsSimPaused, Is.True);
        Assert.That(session.Sim.Clock.IsPaused, Is.True);

        Assert.That(session.Tick(MvpObservedStates.EngageTick(1)), Is.True);
        Assert.That(session.Sim.Clock.SimTick, Is.EqualTo(tickAfterAdvance));
    }

    [Test]
    public void ResumeSim_allows_SimClock_to_advance_again()
    {
        var session = new SimulationSession(2, new StubEngagementResolver());
        session.BeginExecution();

        session.PauseSim();
        Assert.That(session.Tick(MvpObservedStates.EngageTick(0)), Is.True);
        Assert.That(session.Sim.Clock.SimTick, Is.EqualTo(0UL));

        session.ResumeSim();
        Assert.That(session.IsSimPaused, Is.False);

        Assert.That(session.Tick(MvpObservedStates.EngageTick(1)), Is.True);
        Assert.That(session.Sim.Clock.SimTick, Is.EqualTo(1UL));
    }

    [Test]
    public void SetTimeAccelerationFactor_reflects_on_Clock_and_session_property()
    {
        var session = new SimulationSession(3, new StubEngagementResolver());
        Assert.That(session.TimeAccelerationFactor, Is.EqualTo(1));

        session.SetTimeAccelerationFactor(4);
        Assert.That(session.TimeAccelerationFactor, Is.EqualTo(4));
        Assert.That(session.Sim.Clock.AccelerationFactor, Is.EqualTo(4));
    }

    [Test]
    public void Tick_with_acceleration_greater_than_one_advances_multiple_SimTicks()
    {
        var session = new SimulationSession(4, new StubEngagementResolver());
        session.BeginExecution();
        session.SetTimeAccelerationFactor(4);

        Assert.That(session.Tick(MvpObservedStates.EngageTick(0)), Is.True);
        Assert.That(session.Sim.Clock.SimTick, Is.EqualTo(4UL));
    }

    [Test]
    public void PauseSim_blocks_accelerated_Tick_as_well()
    {
        var session = new SimulationSession(5, new StubEngagementResolver());
        session.BeginExecution();
        session.SetTimeAccelerationFactor(8);
        session.PauseSim();

        Assert.That(session.Tick(MvpObservedStates.EngageTick(0)), Is.True);
        Assert.That(session.Sim.Clock.SimTick, Is.EqualTo(0UL));
    }
}
