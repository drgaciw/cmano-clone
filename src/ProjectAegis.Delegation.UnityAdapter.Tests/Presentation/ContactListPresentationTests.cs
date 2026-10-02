using NUnit.Framework;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Presentation;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

[TestFixture]
public sealed class ContactListPresentationTests
{
    [Test]
    public void Default_chrome_preserves_ordinal_contact_id_sort()
    {
        var snapshot = Snapshot(
            Entry("cB", "hostile-2", "Classified", 2),
            Entry("cA", "hostile-1", "Identified", 5));

        var view = ContactListPresentation.Apply(snapshot, ContactListChromeState.Default);

        Assert.That(view.ContactRows.Select(row => row.ContactId), Is.EqualTo(new[] { "cA", "cB" }));
        Assert.That(view.ContactCountLabel, Is.EqualTo("CONTACTS: 2"));
    }

    [Test]
    public void Age_sort_orders_newest_tick_first()
    {
        var snapshot = Snapshot(
            Entry("c-old", "hostile-1", "Classified", 1),
            Entry("c-new", "hostile-2", "Classified", 9));

        var view = ContactListPresentation.Apply(
            snapshot,
            new ContactListChromeState(SortKey: ContactListSortKey.Age));

        Assert.That(view.ContactRows.Select(row => row.ContactId), Is.EqualTo(new[] { "c-new", "c-old" }));
    }

    [Test]
    public void Threat_sort_puts_hostile_targets_first()
    {
        var snapshot = Snapshot(
            Entry("c-blue", "u1", "Classified", 1),
            Entry("c-red", "hostile-1", "Classified", 2));

        var view = ContactListPresentation.Apply(
            snapshot,
            new ContactListChromeState(SortKey: ContactListSortKey.Threat));

        Assert.That(view.ContactRows[0].ContactId, Is.EqualTo("c-red"));
        Assert.That(view.ContactRows[1].ContactId, Is.EqualTo("c-blue"));
    }

    [Test]
    public void Lifecycle_filter_and_text_filter_reduce_visible_rows_without_mutating_snapshot()
    {
        var snapshot = Snapshot(
            Entry("c1", "hostile-1", "Classified", 1),
            Entry("c2", "hostile-2", "Identified", 2),
            Entry("c3", "u1", "Classified", 3));

        var view = ContactListPresentation.Apply(
            snapshot,
            new ContactListChromeState(
                LifecycleFilter: ContactListLifecycleFilter.Identified,
                TextFilter: "hostile"));

        Assert.That(view.ContactRows.Select(row => row.ContactId), Is.EqualTo(new[] { "c2" }));
        Assert.That(view.ContactCountLabel, Is.EqualTo("CONTACTS: 1/3"));
        Assert.That(snapshot.Contacts, Has.Count.EqualTo(3));
    }

    [Test]
    public void Hostile_filter_keeps_engageable_targets_only()
    {
        var snapshot = Snapshot(
            Entry("c-friendly", "u1", "Classified", 1),
            Entry("c-hostile", "hostile-1", "Classified", 2));

        var view = ContactListPresentation.Apply(
            snapshot,
            new ContactListChromeState(LifecycleFilter: ContactListLifecycleFilter.Hostile));

        Assert.That(view.ContactRows.Select(row => row.ContactId), Is.EqualTo(new[] { "c-hostile" }));
    }

    private static SensorC2Snapshot Snapshot(params ContactPictureEntry[] contacts) =>
        new(contacts, contacts.Length, false, false, null, 0);

    private static ContactPictureEntry Entry(string contactId, string targetId, string lifecycle, ulong tick) =>
        new(contactId, targetId, "u1", lifecycle, tick, tick);
}
