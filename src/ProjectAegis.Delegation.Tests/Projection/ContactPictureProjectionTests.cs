using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

[TestFixture]
public sealed class ContactPictureProjectionTests
{
    [Test]
    public void Empty_log_yields_empty_picture()
    {
        var log = new DecisionLog();
        Assert.That(ContactPictureProjection.Project(log), Is.Empty);
    }

    [Test]
    public void Detect_classify_identify_updates_single_contact_in_order()
    {
        var log = new DecisionLog();
        log.AppendContactChange(Change(1, "c1", "hostile-1", "Unknown", "Detected"));
        log.AppendContactChange(Change(2, "c1", "hostile-1", "Detected", "Classified"));
        log.AppendContactChange(Change(3, "c1", "hostile-1", "Classified", "Identified"));

        var picture = ContactPictureProjection.Project(log);
        Assert.That(picture, Has.Count.EqualTo(1));
        Assert.That(picture[0].ContactId, Is.EqualTo("c1"));
        Assert.That(picture[0].LifecycleState, Is.EqualTo("Identified"));
        Assert.That(picture[0].TargetId, Is.EqualTo("hostile-1"));
    }

    [Test]
    public void Lost_removes_contact_from_picture()
    {
        var log = new DecisionLog();
        log.AppendContactChange(Change(1, "c1", "hostile-1", "Unknown", "Detected"));
        log.AppendContactChange(Change(2, "c1", "hostile-1", "Detected", "Lost"));

        Assert.That(ContactPictureProjection.Project(log), Is.Empty);
    }

    [Test]
    public void Multiple_contacts_sorted_by_contact_id()
    {
        var log = new DecisionLog();
        log.AppendContactChange(Change(1, "c2", "hostile-2", "Unknown", "Detected"));
        log.AppendContactChange(Change(2, "c1", "hostile-1", "Unknown", "Detected"));

        var ids = ContactPictureProjection.Project(log).Select(c => c.ContactId).ToArray();
        Assert.That(ids, Is.EqualTo(new[] { "c1", "c2" }));
    }

    private static ContactChangeRecord Change(
        ulong tick,
        string contactId,
        string targetId,
        string previous,
        string next) =>
        new(0, tick, tick, "u1", contactId, targetId, previous, next);
}