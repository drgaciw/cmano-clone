namespace ProjectAegis.Delegation.Tests.Orchestration;

using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Sim.Engage;
using NUnit.Framework;

/// <summary>S117 / P0-8 — session forced 1× API.</summary>
[TestFixture]
public sealed class SimulationSessionWeaponsReleaseTests
{
    [Test]
    public void ForceRealTimeForWeaponsRelease_sets_session_and_clock()
    {
        var session = new SimulationSession(31, new StubEngagementResolver());
        session.SetTimeAccelerationFactor(16);
        Assert.That(session.TimeAccelerationFactor, Is.EqualTo(16));

        session.ForceRealTimeForWeaponsRelease();

        Assert.That(session.IsWeaponsReleaseForced1x, Is.True);
        Assert.That(session.TimeAccelerationFactor, Is.EqualTo(1));
        Assert.That(session.Sim.Clock.AccelerationFactor, Is.EqualTo(1));
    }

    [Test]
    public void Player_accel_blocked_while_forced_until_clear()
    {
        var session = new SimulationSession(32, new StubEngagementResolver());
        session.ForceRealTimeForWeaponsRelease();
        session.SetTimeAccelerationFactor(64);

        Assert.That(session.TimeAccelerationFactor, Is.EqualTo(1));

        session.ClearWeaponsReleaseForced1x();
        session.SetTimeAccelerationFactor(4);
        Assert.That(session.TimeAccelerationFactor, Is.EqualTo(4));
    }
}
