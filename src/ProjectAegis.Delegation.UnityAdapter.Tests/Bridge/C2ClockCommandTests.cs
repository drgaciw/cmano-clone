namespace ProjectAegis.Delegation.UnityAdapter.Tests.Bridge;

using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Delegation.Watch;
using NUnit.Framework;

/// <summary>Headless clock commands — session clock stays authoritative (ADR-010).</summary>
[TestFixture]
public sealed class C2ClockCommandTests
{
    [Test]
    public void FormatCompressionLabel_paused_wins_over_factor()
    {
        Assert.That(C2ClockCommand.FormatCompressionLabel(true, 8), Is.EqualTo("TIME: PAUSED"));
        Assert.That(C2ClockCommand.FormatCompressionLabel(false, 4), Is.EqualTo("TIME: 4x"));
    }

    [Test]
    public void NextFaster_and_NextSlower_walk_presets()
    {
        Assert.That(C2ClockCommand.NextFaster(1), Is.EqualTo(2));
        Assert.That(C2ClockCommand.NextFaster(8), Is.EqualTo(8));
        Assert.That(C2ClockCommand.NextSlower(4), Is.EqualTo(2));
        Assert.That(C2ClockCommand.NextSlower(1), Is.EqualTo(1));
    }

    [Test]
    public void TrySetAcceleration_updates_session_clock()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: true);
        Assert.That(bridge.Session, Is.Not.Null);

        Assert.That(C2ClockCommand.TrySetAcceleration(bridge.Session, 4, out var reason), Is.True);
        Assert.That(reason, Is.Null);
        Assert.That(bridge.Session!.TimeAccelerationFactor, Is.EqualTo(4));
    }

    [Test]
    public void TryPause_and_TryResume_round_trip_when_queue_clear()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: true);
        var session = bridge.Session!;

        Assert.That(C2ClockCommand.TryPause(session, out _), Is.True);
        Assert.That(session.IsSimPaused, Is.True);
        Assert.That(C2ClockCommand.TryResume(session, explicitOverride: false, out _), Is.True);
        Assert.That(session.IsSimPaused, Is.False);
    }

    [Test]
    public void TryResume_blocked_while_unresolved_pause_class_unless_override()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: true);
        var session = bridge.Session!;
        session.ReportWatchAttention(new WatchAttentionEvent(
            "contact-ui",
            WatchAttentionKind.HostileOrUnknownContact,
            WatchAttentionPriority.Critical,
            1,
            "hostile-1"));

        Assert.That(session.IsSimPaused, Is.True);
        Assert.That(
            C2ClockCommand.TryResume(session, explicitOverride: false, out var blocked),
            Is.False);
        Assert.That(blocked, Is.EqualTo(C2ClockCommand.ReasonResumeBlocked));

        Assert.That(C2ClockCommand.TryResume(session, explicitOverride: true, out var ok), Is.True);
        Assert.That(ok, Is.Null);
        Assert.That(session.IsSimPaused, Is.False);
    }

    [Test]
    public void TrySetAcceleration_null_session_fails()
    {
        Assert.That(C2ClockCommand.TrySetAcceleration(null, 2, out var reason), Is.False);
        Assert.That(reason, Is.EqualTo(C2ClockCommand.ReasonNoSession));
    }
}
