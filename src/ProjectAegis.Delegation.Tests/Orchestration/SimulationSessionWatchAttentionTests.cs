namespace ProjectAegis.Delegation.Tests.Orchestration;

using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Tests.Helpers;
using ProjectAegis.Delegation.Watch;
using ProjectAegis.Sim.Engage;
using NUnit.Framework;

/// <summary>S115-01 / S115-03 / S115-04 — session auto-pause + gated resume.</summary>
[TestFixture]
public sealed class SimulationSessionWatchAttentionTests
{
    [Test]
    public void ReportWatchAttention_HostileContact_auto_pauses_and_sets_reason()
    {
        var session = new SimulationSession(11, new StubEngagementResolver());
        Assert.That(session.IsSimPaused, Is.False);

        session.ReportWatchAttention(new WatchAttentionEvent(
            "contact-1",
            WatchAttentionKind.HostileOrUnknownContact,
            WatchAttentionPriority.Critical,
            3,
            "hostile-alpha"));

        Assert.That(session.IsSimPaused, Is.True);
        Assert.That(session.LastWatchPauseReason, Is.EqualTo(WatchPauseReason.HostileOrUnknownContact));
        Assert.That(session.WatchQueue.Cards, Has.Count.EqualTo(1));
        Assert.That(session.WatchQueue.HasUnresolvedPauseClass, Is.True);
    }

    [Test]
    public void ReportWatchAttention_OwnSideLoss_auto_pauses()
    {
        var session = new SimulationSession(12, new StubEngagementResolver());
        session.ReportWatchAttention(new WatchAttentionEvent(
            "loss-1",
            WatchAttentionKind.OwnSideLossOrDamage,
            WatchAttentionPriority.Critical,
            7,
            "u-own-1"));

        Assert.That(session.IsSimPaused, Is.True);
        Assert.That(session.LastWatchPauseReason, Is.EqualTo(WatchPauseReason.OwnSideLossOrDamage));
    }

    [Test]
    public void Distinct_contacts_each_emit_stable_event_and_remain_pause_class()
    {
        var session = new SimulationSession(16, new StubEngagementResolver());

        session.ReportWatchAttention(new WatchAttentionEvent(
            "c-alpha", WatchAttentionKind.HostileOrUnknownContact,
            WatchAttentionPriority.Critical, 1, "hostile-alpha"));
        session.ReportWatchAttention(new WatchAttentionEvent(
            "c-bravo", WatchAttentionKind.HostileOrUnknownContact,
            WatchAttentionPriority.Critical, 2, "hostile-bravo"));
        // Re-emission of same EventId is idempotent — no duplicate card.
        session.ReportWatchAttention(new WatchAttentionEvent(
            "c-alpha", WatchAttentionKind.HostileOrUnknownContact,
            WatchAttentionPriority.Critical, 99, "hostile-alpha"));

        Assert.That(session.WatchQueue.Cards, Has.Count.EqualTo(2));
        Assert.That(session.WatchQueue.UnresolvedPauseClassCount, Is.EqualTo(2));
        Assert.That(session.IsSimPaused, Is.True);
    }

    [Test]
    public void TryResumeSim_fails_while_unresolved_pause_class_unless_override()
    {
        var session = new SimulationSession(13, new StubEngagementResolver());
        session.ReportWatchAttention(new WatchAttentionEvent(
            "c1",
            WatchAttentionKind.HostileOrUnknownContact,
            WatchAttentionPriority.High,
            1,
            "c1"));

        Assert.That(session.TryResumeSim(explicitOverride: false), Is.False);
        Assert.That(session.IsSimPaused, Is.True);

        Assert.That(session.TryResumeSim(explicitOverride: true), Is.True);
        Assert.That(session.IsSimPaused, Is.False);
        Assert.That(session.LastWatchPauseReason, Is.EqualTo(WatchPauseReason.None));
    }

    [Test]
    public void TryResumeSim_succeeds_after_acknowledge()
    {
        var session = new SimulationSession(14, new StubEngagementResolver());
        session.ReportWatchAttention(new WatchAttentionEvent(
            "c2",
            WatchAttentionKind.HostileOrUnknownContact,
            WatchAttentionPriority.High,
            2,
            "c2"));

        Assert.That(session.WatchQueue.TryAcknowledge("c2"), Is.True);
        Assert.That(session.TryResumeSim(), Is.True);
        Assert.That(session.IsSimPaused, Is.False);
    }

    [Test]
    public void ResumeSim_bypasses_gate_for_legacy_callers()
    {
        var session = new SimulationSession(15, new StubEngagementResolver());
        session.ReportWatchAttention(new WatchAttentionEvent(
            "c3",
            WatchAttentionKind.OwnSideLossOrDamage,
            WatchAttentionPriority.Critical,
            4,
            "u3"));

        session.ResumeSim();
        Assert.That(session.IsSimPaused, Is.False);
        // Reason is intentionally not cleared by the ungated path.
        Assert.That(session.LastWatchPauseReason, Is.EqualTo(WatchPauseReason.OwnSideLossOrDamage));
    }

    [Test]
    public void TickHeadless_advances_despite_auto_pause()
    {
        var session = new SimulationSession(17, new StubEngagementResolver());
        session.BeginExecution();

        session.ReportWatchAttention(new WatchAttentionEvent(
            "c-headless",
            WatchAttentionKind.HostileOrUnknownContact,
            WatchAttentionPriority.Critical,
            1,
            "hostile-x"));

        Assert.That(session.IsSimPaused, Is.True);
        var tickBefore = session.Sim.Clock.SimTick;

        // Interactive Tick would freeze; Headless path must still advance for CI/replay.
        Assert.That(session.TickHeadless(MvpObservedStates.EngageTick(1.0)), Is.True);
        Assert.That(session.Sim.Clock.SimTick, Is.GreaterThan(tickBefore));
        Assert.That(session.IsSimPaused, Is.True); // pause flag preserved for interactive resume
        Assert.That(session.LastWatchPauseReason, Is.EqualTo(WatchPauseReason.HostileOrUnknownContact));
    }
}
