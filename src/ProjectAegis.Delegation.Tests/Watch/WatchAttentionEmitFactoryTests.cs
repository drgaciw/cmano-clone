namespace ProjectAegis.Delegation.Tests.Watch;

using ProjectAegis.Delegation.Watch;
using ProjectAegis.Sim.Sensors;
using NUnit.Framework;

/// <summary>S116 — pure WatchAttentionEmitFactory (contact first-detect + own-side loss).</summary>
[TestFixture]
public sealed class WatchAttentionEmitFactoryTests
{
    [Test]
    public void First_hostile_Unknown_to_Detected_emits_stable_contact_event()
    {
        var t = new ContactTransition(
            5, 5.0, "u1", "c-1", "hostile-1",
            ContactLifecycleState.Unknown, ContactLifecycleState.Detected);

        Assert.That(WatchAttentionEmitFactory.TryFromFirstHostileOrUnknownContact(in t, out var evt), Is.True);
        Assert.That(evt, Is.Not.Null);
        Assert.That(evt!.EventId, Is.EqualTo("watch:contact:hostile-1"));
        Assert.That(evt.Kind, Is.EqualTo(WatchAttentionKind.HostileOrUnknownContact));
        Assert.That(evt.Priority, Is.EqualTo(WatchAttentionPriority.Critical));
        Assert.That(evt.SubjectId, Is.EqualTo("hostile-1"));
        Assert.That(evt.GroupingKey, Is.EqualTo("c-1"));
        Assert.That(evt.TriggerTick, Is.EqualTo(5UL));
    }

    [Test]
    public void Re_detect_same_target_after_Detected_does_not_emit()
    {
        var t = new ContactTransition(
            6, 6.0, "u1", "c-1", "hostile-1",
            ContactLifecycleState.Detected, ContactLifecycleState.Classified);

        Assert.That(WatchAttentionEmitFactory.TryFromFirstHostileOrUnknownContact(in t, out _), Is.False);
    }

    [Test]
    public void Own_side_u1_first_detect_does_not_emit_hostile_contact()
    {
        var t = new ContactTransition(
            1, 1.0, "sensor", "c-own", "u1",
            ContactLifecycleState.Unknown, ContactLifecycleState.Detected);

        Assert.That(WatchAttentionEmitFactory.TryFromFirstHostileOrUnknownContact(in t, out _), Is.False);
    }

    [Test]
    public void Own_side_loss_emits_stable_loss_event()
    {
        Assert.That(
            WatchAttentionEmitFactory.TryFromOwnSideLoss("u1", 9, "bda:lost", out var evt),
            Is.True);
        Assert.That(evt, Is.Not.Null);
        Assert.That(evt!.EventId, Is.EqualTo("watch:loss:u1"));
        Assert.That(evt.Kind, Is.EqualTo(WatchAttentionKind.OwnSideLossOrDamage));
        Assert.That(evt.Priority, Is.EqualTo(WatchAttentionPriority.Critical));
    }

    [Test]
    public void Hostile_loss_does_not_emit_own_side_loss()
    {
        Assert.That(
            WatchAttentionEmitFactory.TryFromOwnSideLoss("hostile-1", 3, "bda:lost", out _),
            Is.False);
    }

    [Test]
    public void Own_side_Lost_transition_emits()
    {
        var t = new ContactTransition(
            4, 4.0, "obs", "c", "u1",
            ContactLifecycleState.Identified, ContactLifecycleState.Lost);

        Assert.That(WatchAttentionEmitFactory.TryFromOwnSideLostTransition(in t, out var evt), Is.True);
        Assert.That(evt!.EventId, Is.EqualTo("watch:loss:u1"));
    }
}
