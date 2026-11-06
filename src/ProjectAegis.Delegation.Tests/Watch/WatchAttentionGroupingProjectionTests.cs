using NUnit.Framework;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.Watch;

namespace ProjectAegis.Delegation.Tests.Watch;

[TestFixture]
public sealed class WatchAttentionGroupingProjectionTests
{
    private static WatchAttentionCard Card(string id, string subject, ulong tick, WatchAttentionPriority priority = WatchAttentionPriority.High, string? group = null) =>
        new(new WatchAttentionEvent(id, WatchAttentionKind.HostileOrUnknownContact, priority, tick, subject, group));

    [Test]
    public void Related_contacts_form_one_group_with_separate_event_ids_and_focus_target()
    {
        var cards = new[] { Card("b", "c2", 2, group: "raid-1"), Card("a", "c1", 1, WatchAttentionPriority.Critical, "raid-1") };
        var result = WatchAttentionGroupingProjection.Project(cards);
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.Multiple((Action)(() =>
        {
            Assert.That(result[0].Count, Is.EqualTo(2));
            Assert.That(result[0].UnresolvedCount, Is.EqualTo(2));
            Assert.That(result[0].FocusSubjectId, Is.EqualTo("c1"));
            Assert.That(result[0].EventIds, Is.EqualTo(new[] { "a", "b" }));
            Assert.That(result[0].SubjectIds, Is.EqualTo(new[] { "c1", "c2" }));
            Assert.That(cards.All(c => c.IsUnresolved), Is.True);
        }));
    }

    [Test]
    public void Dismissed_cards_are_omitted_and_acknowledged_cards_do_not_count_as_unresolved()
    {
        var cards = new[]
        {
            Card("a", "c1", 1, group: "raid") with { IsAcknowledged = true },
            Card("b", "c2", 2, group: "raid"),
            Card("c", "c3", 3, group: "raid") with { IsDismissed = true },
        };
        var group = WatchAttentionGroupingProjection.Project(cards).Single();
        Assert.That(group.Count, Is.EqualTo(2));
        Assert.That(group.UnresolvedCount, Is.EqualTo(1));
        Assert.That(group.FocusSubjectId, Is.EqualTo("c2"));
        Assert.That(cards[1].IsAcknowledged, Is.False);
    }

    [Test]
    public void Grouping_and_order_are_stable_across_input_order_and_ungrouped_events_remain_separate()
    {
        var cards = new[] { Card("b", "c2", 2), Card("a", "c1", 1), Card("critical", "u1", 9, WatchAttentionPriority.Critical) };
        var a = WatchAttentionGroupingProjection.Project(cards);
        var b = WatchAttentionGroupingProjection.Project(cards.Reverse().ToArray());
        Assert.That(a.Select(g => g.GroupId), Is.EqualTo(b.Select(g => g.GroupId)));
        Assert.That(a, Has.Count.EqualTo(3));
        Assert.That(a[0].FocusSubjectId, Is.EqualTo("u1"));
        Assert.That(WatchAttentionGroupingProjection.Project(null), Is.Empty);
    }

    [Test]
    public void Group_keys_never_collide_with_unrelated_event_ids()
    {
        var result = WatchAttentionGroupingProjection.Project(new[] { Card("event", "c1", 1, group: "same"), Card("same", "c2", 2) });
        Assert.That(result, Has.Count.EqualTo(2));
    }
}
