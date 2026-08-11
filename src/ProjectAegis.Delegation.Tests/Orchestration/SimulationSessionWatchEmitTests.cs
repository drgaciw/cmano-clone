namespace ProjectAegis.Delegation.Tests.Orchestration;

using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Watch;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Sensors;
using NUnit.Framework;

/// <summary>S116 — session ReportContactTransitions / ReportOwnSideLoss wiring.</summary>
[TestFixture]
public sealed class SimulationSessionWatchEmitTests
{
    [Test]
    public void ReportContactTransitions_first_hostile_enqueues_and_auto_pauses()
    {
        var session = new SimulationSession(21, new StubEngagementResolver());
        var transitions = new[]
        {
            new ContactTransition(
                2, 2.0, "u1", "c-a", "hostile-alpha",
                ContactLifecycleState.Unknown, ContactLifecycleState.Detected),
        };

        session.ReportContactTransitions(transitions);

        Assert.That(session.IsSimPaused, Is.True);
        Assert.That(session.WatchQueue.Cards, Has.Count.EqualTo(1));
        Assert.That(session.WatchQueue.Cards[0].EventId, Is.EqualTo("watch:contact:hostile-alpha"));
        Assert.That(session.LastWatchPauseReason, Is.EqualTo(WatchPauseReason.HostileOrUnknownContact));
    }

    [Test]
    public void ReportContactTransitions_duplicate_target_is_idempotent()
    {
        var session = new SimulationSession(22, new StubEngagementResolver());
        var t1 = new ContactTransition(
            1, 1.0, "u1", "c1", "hostile-1",
            ContactLifecycleState.Unknown, ContactLifecycleState.Detected);
        var t2 = new ContactTransition(
            2, 2.0, "u1", "c1", "hostile-1",
            ContactLifecycleState.Unknown, ContactLifecycleState.Detected);

        session.ReportContactTransitions(new[] { t1, t2 });

        Assert.That(session.WatchQueue.Cards, Has.Count.EqualTo(1));
    }

    [Test]
    public void ReportOwnSideLoss_u1_auto_pauses()
    {
        var session = new SimulationSession(23, new StubEngagementResolver());
        session.ReportOwnSideLoss("u1", 7, "bda:lost");

        Assert.That(session.IsSimPaused, Is.True);
        Assert.That(session.WatchQueue.Cards, Has.Count.EqualTo(1));
        Assert.That(session.WatchQueue.Cards[0].EventId, Is.EqualTo("watch:loss:u1"));
        Assert.That(session.LastWatchPauseReason, Is.EqualTo(WatchPauseReason.OwnSideLossOrDamage));
    }

    [Test]
    public void ReportOwnSideLoss_hostile_is_noop()
    {
        var session = new SimulationSession(24, new StubEngagementResolver());
        session.ReportOwnSideLoss("hostile-1", 3, "bda:lost");

        Assert.That(session.IsSimPaused, Is.False);
        Assert.That(session.WatchQueue.Cards, Is.Empty);
    }
}
