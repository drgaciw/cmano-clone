namespace ProjectAegis.Delegation.Tests.Projection;

using NUnit.Framework;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;

[TestFixture]
public sealed class ContactOverrideProjectionTests
{
    [Test]
    public void Project_null_or_empty_returns_empty_snapshot()
    {
        Assert.That(ContactOverrideProjection.Project((DecisionLog?)null).Overrides, Is.Empty);
        Assert.That(ContactOverrideProjection.Project(new DecisionLog()).Overrides, Is.Empty);
        Assert.That(ContactOverrideSnapshot.Empty.Find("c1"), Is.Null);
    }

    [Test]
    public void Project_folds_reclassify_and_drop_chronologically_and_supports_reclassify_after_drop()
    {
        var log = new DecisionLog();
        log.AppendContactOverride(new ContactOverrideRecord(
            0, 1.0, 1, "c1", ContactOverrideAction.Reclassify, "Hostile", "Neutral"));
        log.AppendContactOverride(new ContactOverrideRecord(
            0, 2.0, 2, "c1", ContactOverrideAction.Drop, "Neutral", "Dropped"));

        var afterDrop = ContactOverrideProjection.Project(log);
        var entryAfterDrop = afterDrop.Find("c1");
        Assert.That(entryAfterDrop, Is.Not.Null);
        Assert.That(entryAfterDrop!.IsDropped, Is.True);
        Assert.That(entryAfterDrop.LastSimTick, Is.EqualTo(2UL));
        Assert.That(entryAfterDrop.LastSequenceId, Is.GreaterThan(0UL));
        Assert.That(entryAfterDrop.StatusLabel, Is.EqualTo("OVERRIDE: DROPPED (operator)"));
        Assert.That(afterDrop.ToDroppedContactIds(), Does.Contain("c1"));

        log.AppendContactOverride(new ContactOverrideRecord(
            0, 3.0, 3, "c1", ContactOverrideAction.Reclassify, "Dropped", "Friendly"));

        var afterRestore = ContactOverrideProjection.Project(log);
        var entryAfterRestore = afterRestore.Find("c1");
        Assert.That(entryAfterRestore, Is.Not.Null);
        Assert.That(entryAfterRestore!.IsDropped, Is.False);
        Assert.That(entryAfterRestore.AffiliationOverride, Is.EqualTo("Friendly"));
        Assert.That(entryAfterRestore.StatusLabel, Is.EqualTo("OVERRIDE: RECLASSIFIED → Friendly"));
        Assert.That(afterRestore.ToDroppedContactIds(), Does.Not.Contain("c1"));
        Assert.That(afterRestore.ToAffiliationMap()["c1"], Is.EqualTo("Friendly"));
    }

    [Test]
    public void ContactDetailProjection_and_ApplyState_surface_operator_override_status_without_mutating_sensor_fields()
    {
        var contact = new ContactPictureEntry("c1", "hostile-1", "u1", "Classified", 4, 4.0);
        var ov = new ContactOverrideEntry("c1", "Neutral", IsDropped: false, LastSimTick: 5, LastSequenceId: 10);

        var detail = ContactDetailProjection.Project(contact, currentSimTick: 8, operatorOverride: ov);
        Assert.That(detail, Is.Not.Null);
        Assert.That(detail!.TargetId, Is.EqualTo("hostile-1"));
        Assert.That(detail.LifecycleState, Is.EqualTo("Classified"));
        Assert.That(detail.OverrideStatusLine, Is.EqualTo("OVERRIDE: RECLASSIFIED → Neutral"));
        Assert.That(detail.OperatorAffiliation, Is.EqualTo("Neutral"));
        Assert.That(detail.IsOperatorDropped, Is.False);

        var presentation = ContactDetailApplyState.ProjectAndApply(
            "c1", new[] { contact }, currentSimTick: 8, operatorOverride: ov);
        Assert.That(presentation.OverrideStatusLine, Is.EqualTo("OVERRIDE: RECLASSIFIED → Neutral"));
        Assert.That(presentation.OperatorAffiliation, Is.EqualTo("Neutral"));
        Assert.That(presentation.IsOperatorDropped, Is.False);
    }

    [Test]
    public void MapPictureProjection_filters_dropped_contacts_and_applies_reclassified_affiliation()
    {
        var oob = new[] { new OobTreeEntry("u1", true) };
        var contacts = new[]
        {
            new ContactPictureEntry("c1", "h1", "u1", "Classified", 1, 1.0),
            new ContactPictureEntry("c2", "h2", "u1", "Detected", 1, 1.0),
        };
        var overrides = new ContactOverrideSnapshot(new[]
        {
            new ContactOverrideEntry("c1", "Neutral", IsDropped: false, LastSimTick: 2, LastSequenceId: 1),
            new ContactOverrideEntry("c2", null, IsDropped: true, LastSimTick: 2, LastSequenceId: 2),
        });

        var symbols = MapPictureProjection.Project(oob, contacts, layoutSeed: 7, poses: null, overrides: overrides);

        Assert.That(symbols.Any(s => s.SymbolId == "c2"), Is.False, "Dropped contact must be omitted from map symbols.");
        var c1 = symbols.Single(s => s.SymbolId == "c1");
        Assert.That(c1.Affiliation, Is.EqualTo("Neutral"));
    }

    [Test]
    public void MessageLogProjection_formats_ContactOverride_reclassify_and_drop()
    {
        var log = new DecisionLog();
        log.AppendContactOverride(new ContactOverrideRecord(
            0, 1.0, 1, "c1", ContactOverrideAction.Reclassify, "Hostile", "Unknown"));
        log.AppendContactOverride(new ContactOverrideRecord(
            0, 2.0, 2, "c2", ContactOverrideAction.Drop, "Hostile", "Dropped"));

        var lines = MessageLogProjection.Project(log);
        Assert.That(lines, Has.Count.EqualTo(2));
        Assert.That(lines[0].Category, Is.EqualTo("CONTACT"));
        Assert.That(lines[0].Text, Does.Contain("c1").And.Contain("Hostile").And.Contain("Unknown"));
        Assert.That(lines[1].Category, Is.EqualTo("CONTACT"));
        Assert.That(lines[1].Text, Does.Contain("c2").And.Contain("dropped"));
    }
}
