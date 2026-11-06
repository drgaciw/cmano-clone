using NUnit.Framework;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Watch;

namespace ProjectAegis.Delegation.Tests.Watch;

[TestFixture]
public sealed class WatchInterruptionPreferenceTests
{
    private static WatchAttentionEvent Unknown => new("unknown", WatchAttentionKind.HostileOrUnknownContact, WatchAttentionPriority.High, 1, "c1");
    private static WatchAttentionEvent Critical => new("critical", WatchAttentionKind.OwnSideLossOrDamage, WatchAttentionPriority.Critical, 2, "u1");

    [Test]
    public void Default_mode_preserves_all_classic_interruptions()
    {
        var gate = new WatchAutoPauseGate();
        Assert.That(gate.InterruptionMode, Is.EqualTo(WatchInterruptionMode.All));
        Assert.That(gate.ShouldAutoPause(Unknown), Is.True);
    }

    [Test]
    public void Critical_only_mode_suppresses_high_unknown_but_keeps_critical_damage()
    {
        var gate = new WatchAutoPauseGate { InterruptionMode = WatchInterruptionMode.CriticalOnly };
        Assert.That(gate.ShouldAutoPause(Unknown), Is.False);
        Assert.That(gate.LastPauseReason, Is.EqualTo(WatchPauseReason.None));
        Assert.That(gate.ShouldAutoPause(Critical), Is.True);
        Assert.That(gate.LastPauseReason, Is.EqualTo(WatchPauseReason.OwnSideLossOrDamage));
    }

    [Test]
    public void Manual_mode_suppresses_new_interruptions_without_approving_pending_orders()
    {
        var gate = new WatchAutoPauseGate { InterruptionMode = WatchInterruptionMode.Manual };
        var queue = new PendingApprovalQueue();
        queue.Enqueue(new Order(new OrderId(1), new TargetId("u1"), 0, OrderKind.Engage, RiskLevel.High));
        Assert.That(gate.ShouldAutoPause(Critical), Is.False);
        Assert.That(gate.ShouldAutoPause(queue), Is.False);
        Assert.That(gate.CanResume(queue), Is.False);
        Assert.That(queue.Pending, Has.Count.EqualTo(1));
        Assert.That(gate.CanResume(queue, explicitOverride: true), Is.True);
        Assert.That(queue.Pending, Has.Count.EqualTo(1));
    }

    [Test]
    public void Critical_mode_still_interrupts_for_orders_requiring_approval()
    {
        var gate = new WatchAutoPauseGate { InterruptionMode = WatchInterruptionMode.CriticalOnly };
        var order = new Order(new OrderId(1), new TargetId("u1"), 0, OrderKind.Engage, RiskLevel.High);
        var queue = new PendingApprovalQueue();
        queue.Enqueue(order);
        Assert.That(gate.ShouldAutoPause(queue), Is.True);
        Assert.That(gate.ShouldAutoPause(order), Is.True);
        Assert.That(gate.ShouldAutoPause(queue.Pending[0]), Is.True);
    }

    [Test]
    public void Changing_preferences_does_not_acknowledge_existing_alerts_or_bypass_resume_gate()
    {
        var gate = new WatchAutoPauseGate();
        var queue = new WatchAttentionQueue();
        queue.Enqueue(Unknown);
        gate.InterruptionMode = WatchInterruptionMode.Manual;
        Assert.That(gate.CanResume(queue, explicitOverride: false), Is.False);
        Assert.That(queue.Cards[0].IsAcknowledged, Is.False);
        gate.InterruptionMode = WatchInterruptionMode.All;
        Assert.That(gate.ShouldAutoPause(Unknown), Is.True);
    }
}
