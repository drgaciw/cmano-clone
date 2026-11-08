namespace ProjectAegis.Delegation.Tests.Watch;

using ProjectAegis.Delegation.Watch;
using NUnit.Framework;

/// <summary>S115-02 / S115-04 — pure WatchAttentionQueue ordering, ack/dismiss, idempotency.</summary>
[TestFixture]
public sealed class WatchAttentionQueueTests
{
    [Test]
    public void Enqueue_orders_by_priority_then_tick_then_eventId()
    {
        var q = new WatchAttentionQueue();
        q.Enqueue(new WatchAttentionEvent("e-low", WatchAttentionKind.HostileOrUnknownContact, WatchAttentionPriority.Low, 10, "c1"));
        q.Enqueue(new WatchAttentionEvent("e-crit", WatchAttentionKind.OwnSideLossOrDamage, WatchAttentionPriority.Critical, 50, "u1"));
        q.Enqueue(new WatchAttentionEvent("e-high-early", WatchAttentionKind.HostileOrUnknownContact, WatchAttentionPriority.High, 5, "c2"));
        q.Enqueue(new WatchAttentionEvent("e-high-late", WatchAttentionKind.HostileOrUnknownContact, WatchAttentionPriority.High, 20, "c3"));

        Assert.That(q.Cards, Has.Count.EqualTo(4));
        Assert.That(q.Cards[0].EventId, Is.EqualTo("e-crit"));
        Assert.That(q.Cards[1].EventId, Is.EqualTo("e-high-early"));
        Assert.That(q.Cards[2].EventId, Is.EqualTo("e-high-late"));
        Assert.That(q.Cards[3].EventId, Is.EqualTo("e-low"));
    }

    [Test]
    public void Enqueue_is_idempotent_on_EventId()
    {
        var q = new WatchAttentionQueue();
        var evt = new WatchAttentionEvent("same", WatchAttentionKind.HostileOrUnknownContact, WatchAttentionPriority.Normal, 1, "c1");
        q.Enqueue(evt);
        q.Enqueue(evt with { TriggerTick = 99 });

        Assert.That(q.Cards, Has.Count.EqualTo(1));
        Assert.That(q.Cards[0].TriggerTick, Is.EqualTo(1UL));
    }

    [Test]
    public void TryAcknowledge_and_TryDismiss_are_presentation_only_and_restorable()
    {
        var q = new WatchAttentionQueue();
        q.Enqueue(new WatchAttentionEvent("e1", WatchAttentionKind.HostileOrUnknownContact, WatchAttentionPriority.Critical, 1, "c1"));

        Assert.That(q.HasUnresolvedPauseClass, Is.True);
        Assert.That(q.TryAcknowledge("e1"), Is.True);
        Assert.That(q.HasUnresolvedPauseClass, Is.False);
        Assert.That(q.Cards[0].IsAcknowledged, Is.True);

        Assert.That(q.TryDismiss("e1"), Is.True);
        Assert.That(q.SnapshotVisible(), Is.Empty);
        Assert.That(q.TryRestore("e1"), Is.True);
        Assert.That(q.SnapshotVisible(), Has.Count.EqualTo(1));
    }

    [Test]
    public void Unresolved_count_ignores_non_pause_class_and_acked()
    {
        var q = new WatchAttentionQueue();
        // Both kinds are pause-class today; acknowledge one.
        q.Enqueue(new WatchAttentionEvent("a", WatchAttentionKind.HostileOrUnknownContact, WatchAttentionPriority.High, 1, "c1"));
        q.Enqueue(new WatchAttentionEvent("b", WatchAttentionKind.OwnSideLossOrDamage, WatchAttentionPriority.High, 2, "u1"));
        q.TryAcknowledge("a");

        Assert.That(q.UnresolvedPauseClassCount, Is.EqualTo(1));
    }
}
