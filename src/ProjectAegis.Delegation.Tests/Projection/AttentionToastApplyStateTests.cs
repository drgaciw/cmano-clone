using ProjectAegis.Delegation.Attention;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.Watch;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

[TestFixture]
public sealed class AttentionToastApplyStateTests
{
    [Test]
    public void Apply_empty_inputs_returns_empty_and_allows_resume()
    {
        var applied = AttentionToastApplyState.Apply(null, null, null);

        Assert.That(applied.HasActiveCard, Is.False);
        Assert.That(applied.QueuedCount, Is.EqualTo(0));
        Assert.That(applied.CanResume, Is.True);
        Assert.That(applied.HasUnresolvedPauseClass, Is.False);
    }

    [Test]
    public void Apply_pause_class_watch_card_is_active_and_blocks_resume()
    {
        var queue = new WatchAttentionQueue();
        var gate = new WatchAutoPauseGate();
        var evt = new WatchAttentionEvent(
            "contact-1",
            WatchAttentionKind.HostileOrUnknownContact,
            WatchAttentionPriority.Critical,
            3,
            "hostile-alpha",
            ReasonDetail: "first detect");
        queue.Enqueue(evt);
        Assert.That(gate.ShouldAutoPause(evt), Is.True);

        var applied = AttentionToastApplyState.Apply(null, queue, gate);

        Assert.That(applied.HasActiveCard, Is.True);
        Assert.That(applied.Active!.IsPauseClass, Is.True);
        Assert.That(applied.Active.Title, Does.Contain("PAUSE"));
        Assert.That(applied.Active.Body, Does.Contain("hostile-alpha"));
        Assert.That(applied.Active.AccessibleText, Does.Contain("Acknowledge"));
        Assert.That(applied.HasUnresolvedPauseClass, Is.True);
        Assert.That(applied.CanResume, Is.False);
        Assert.That(applied.PauseReasonLabel, Does.Contain("Hostile"));
    }

    [Test]
    public void Apply_pause_class_stays_ahead_of_tier_toast()
    {
        var queue = new WatchAttentionQueue();
        queue.Enqueue(new WatchAttentionEvent(
            "loss-1",
            WatchAttentionKind.OwnSideLossOrDamage,
            WatchAttentionPriority.Critical,
            1,
            "u1"));
        var tier = AttentionToastApplyState.FromAlert(AttentionTierAlertProjection.BuildAlert(
            Row("a1", AttentionTierName.SimplerDecisions, load: 40),
            AttentionTierName.Nominal));

        var applied = AttentionToastApplyState.Apply(new[] { tier }, queue, new WatchAutoPauseGate());

        Assert.That(applied.Active!.IsPauseClass, Is.True);
        Assert.That(applied.QueuedCount, Is.EqualTo(1));
        Assert.That(applied.QueueBadge, Is.EqualTo("+1 queued"));
    }

    [Test]
    public void Apply_skips_routine_tier_cards()
    {
        var routine = AttentionToastApplyState.FromAlert(AttentionTierAlertProjection.BuildAlert(
            Row("a1", AttentionTierName.Nominal, load: 5, budget: 20),
            AttentionTierName.SlowerReactions));

        var applied = AttentionToastApplyState.Apply(new[] { routine }, null, null);

        Assert.That(applied.HasActiveCard, Is.False);
        Assert.That(routine.Severity, Is.EqualTo(AlertSeverity.Routine));
    }

    [Test]
    public void Binder_diff_emits_toast_once_then_suppresses_unchanged_tier()
    {
        var binder = new AttentionToastBinder();
        var first = LogWithAttention("a1", load: 40, budget: 20);
        var firstToast = binder.Refresh(first, null, null);
        Assert.That(firstToast.HasActiveCard, Is.True);
        Assert.That(firstToast.Active!.Severity, Is.EqualTo(AlertSeverity.Critical));

        var same = LogWithAttention("a1", load: 40, budget: 20);
        var second = binder.Refresh(same, null, null);
        Assert.That(second.HasActiveCard, Is.True, "pending toast remains until acknowledged");

        Assert.That(binder.TryAcknowledge(second.Active!.CardId, null), Is.True);
        var afterAck = binder.Refresh(same, null, null);
        Assert.That(afterAck.HasActiveCard, Is.False);
    }

    [Test]
    public void Binder_acknowledge_watch_card_unblocks_resume()
    {
        var queue = new WatchAttentionQueue();
        var gate = new WatchAutoPauseGate();
        var evt = new WatchAttentionEvent(
            "contact-2",
            WatchAttentionKind.HostileOrUnknownContact,
            WatchAttentionPriority.Critical,
            4,
            "hostile-1");
        queue.Enqueue(evt);
        gate.ShouldAutoPause(evt);

        var binder = new AttentionToastBinder();
        var before = binder.Refresh(null, queue, gate);
        Assert.That(before.CanResume, Is.False);

        Assert.That(binder.TryAcknowledge("contact-2", queue), Is.True);
        var after = binder.Refresh(null, queue, gate);
        Assert.That(after.CanResume, Is.True);
        Assert.That(after.HasUnresolvedPauseClass, Is.False);
        Assert.That(after.HasActiveCard, Is.False);
    }

    [Test]
    public void ProjectLatestFromLog_keeps_last_sample_per_agent()
    {
        var log = new DecisionLog();
        log.Append(OrderLogEntry.FromDecisionRecord(Record("a1", 10, 20, simTime: 1), 1));
        log.Append(OrderLogEntry.FromDecisionRecord(Record("a1", 30, 20, simTime: 2), 2));
        log.Append(OrderLogEntry.FromDecisionRecord(Record("a2", 5, 20, simTime: 2), 3));

        var rows = AttentionToastApplyState.ProjectLatestFromLog(log);
        Assert.That(rows, Has.Count.EqualTo(2));
        Assert.That(rows[0].AgentId, Is.EqualTo("a1"));
        Assert.That(rows[0].Load, Is.EqualTo(30));
        Assert.That(rows[1].AgentId, Is.EqualTo("a2"));
    }

    private static AgentAttentionRow Row(
        string id,
        AttentionTierName tier,
        double load = 25,
        double budget = 20) =>
        new(
            id,
            budget,
            load,
            IsOverloaded: load > budget,
            HasSample: true,
            Tier: tier,
            TierLabel: AttentionTierNaming.DisplayName(tier),
            StatusLabel: "ATT: " + AttentionTierNaming.DisplayName(tier),
            LoadBadge: $"LOAD: {load:0.0}/{budget:0.0}",
            AccessibleLabel: AttentionTierNaming.AccessibleLabel(tier, load, budget));

    private static DecisionLog LogWithAttention(string agentId, double load, double budget)
    {
        var log = new DecisionLog();
        log.Append(OrderLogEntry.FromDecisionRecord(Record(agentId, load, budget, simTime: 1), 1));
        return log;
    }

    private static DecisionRecord Record(string agentId, double load, double budget, double simTime) =>
        new(
            simTime,
            new AgentId(agentId),
            new TargetId("u1"),
            AutonomyLevel.FullAutonomous,
            OrderKind.Hold,
            Array.Empty<ScoredIntent>(),
            "test",
            load,
            budget,
            0.1);
}
