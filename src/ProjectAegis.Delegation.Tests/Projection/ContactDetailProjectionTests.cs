using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

[TestFixture]
public sealed class ContactDetailProjectionTests
{
    [Test]
    public void Project_null_contact_returns_null()
    {
        Assert.That(ContactDetailProjection.Project((ContactPictureEntry?)null, 10), Is.Null);
    }

    [Test]
    public void Project_builds_provenance_classification_staleness()
    {
        var contact = new ContactPictureEntry(
            ContactId: "c1",
            TargetId: "hostile-1",
            ObserverId: "u1",
            LifecycleState: "Detected",
            LastSimTick: 5,
            LastSimTime: 5.0);

        var detail = ContactDetailProjection.Project(contact, currentSimTick: 12);

        Assert.That(detail, Is.Not.Null);
        Assert.That(detail!.ContactId, Is.EqualTo("c1"));
        Assert.That(detail.TargetId, Is.EqualTo("hostile-1"));
        Assert.That(detail.DetectionProvenanceLine, Is.EqualTo("Detected by: u1"));
        Assert.That(detail.ClassificationLine, Does.Contain("Detected"));
        Assert.That(detail.ClassificationLine, Does.Contain("hostile-1"));
        Assert.That(detail.ClassificationLine, Does.Not.Contain("SA-3"));
        Assert.That(detail.StalenessLine, Is.EqualTo("7 ticks stale"));
        Assert.That(detail.ConfidenceLine, Does.Contain("low"));
        Assert.That(detail.BdaLine, Is.EqualTo("BDA: —"));
        Assert.That(detail.WraLine, Does.StartWith("WRA:"));
        Assert.That(detail.LifecycleState, Is.EqualTo("Detected"));
    }

    [Test]
    public void Project_bda_lifecycle_maps_damage_assessment()
    {
        var contact = new ContactPictureEntry(
            "c2", "hostile-2", "u2", "Classified", LastSimTick: 3, LastSimTime: 3.0);

        var detail = ContactDetailProjection.Project(
            contact,
            currentSimTick: 3,
            bdaLifecycleOverride: BdaContactDamageStates.DegradedL1);

        Assert.That(detail!.BdaLine, Does.Contain(BdaContactDamageStates.DegradedL1));
        Assert.That(detail.BdaLine, Does.Contain("assessment"));
        Assert.That(detail.LifecycleState, Is.EqualTo(BdaContactDamageStates.DegradedL1));
        Assert.That(detail.StalenessLine, Is.EqualTo("0 ticks stale"));
    }

    [Test]
    public void Project_by_contact_id_from_picture_list()
    {
        var contacts = new[]
        {
            new ContactPictureEntry("c1", "h1", "u1", "Identified", 1, 1.0),
            new ContactPictureEntry("c2", "h2", "u1", "Detected", 2, 2.0),
        };

        var detail = ContactDetailProjection.Project("c1", contacts, currentSimTick: 10);
        Assert.That(detail!.ContactId, Is.EqualTo("c1"));
        Assert.That(detail.ConfidenceLine, Does.Contain("high"));
        Assert.That(detail.StalenessLine, Is.EqualTo("9 ticks stale"));
    }

    [Test]
    public void Project_missing_contact_id_returns_null()
    {
        var contacts = new[]
        {
            new ContactPictureEntry("c1", "h1", "u1", "Detected", 1, 1.0),
        };
        Assert.That(ContactDetailProjection.Project("missing", contacts, 5), Is.Null);
    }

    [Test]
    public void Apply_null_returns_empty_presentation()
    {
        var applied = ContactDetailApplyState.Apply(null);
        Assert.That(applied.ContactIdLine, Is.EqualTo("CONTACT: —"));
        Assert.That(applied.ClassificationLine, Is.EqualTo("CLASS: —"));
        Assert.That(applied.DetectionProvenanceLine, Is.EqualTo("Detected by: —"));
    }

    [Test]
    public void ProjectAndApply_zero_state_when_no_selection()
    {
        var applied = ContactDetailApplyState.ProjectAndApply(
            null,
            Array.Empty<ContactPictureEntry>(),
            0);
        Assert.That(applied, Is.EqualTo(ContactDetailPresentation.Empty));
    }

    [Test]
    public void ProjectAndApply_maps_entry_lines()
    {
        var contacts = new[]
        {
            new ContactPictureEntry("c9", "track-9", "sensor-a", "Classified", 4, 4.0),
        };
        var applied = ContactDetailApplyState.ProjectAndApply("c9", contacts, currentSimTick: 9);
        Assert.That(applied.ContactIdLine, Is.EqualTo("CONTACT: c9"));
        Assert.That(applied.TargetIdLine, Is.EqualTo("TARGET: track-9"));
        Assert.That(applied.DetectionProvenanceLine, Is.EqualTo("Detected by: sensor-a"));
        Assert.That(applied.StalenessLine, Is.EqualTo("5 ticks stale"));
        Assert.That(applied.LifecycleLine, Is.EqualTo("STATE: Classified"));
    }
}
