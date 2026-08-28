using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.Delegation.Comms;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

[TestFixture]
public sealed class ContactProvenanceProjectionTests
{
    [Test]
    public void Empty_log_yields_empty_snapshot()
    {
        var snapshot = ContactProvenanceProjection.Project(new DecisionLog(), currentSimTick: 0);

        Assert.That(snapshot.Contacts, Is.Empty);
        Assert.That(ContactProvenanceFingerprint.Compute(snapshot), Is.EqualTo("cp:empty"));
    }

    [Test]
    public void Fresh_track_publishes_source_confidence_and_last_known()
    {
        var log = new DecisionLog();
        log.AppendContactChange(Change(5, "c1", "hostile-1", "Unknown", "Classified"));

        var snapshot = ContactProvenanceProjection.Project(log, currentSimTick: 5);

        Assert.That(snapshot.Contacts, Has.Count.EqualTo(1));
        var row = snapshot.Contacts[0];
        Assert.That(row.ContactId, Is.EqualTo("c1"));
        Assert.That(row.Source.ObserverId, Is.EqualTo("u1"));
        Assert.That(row.Source.TargetId, Is.EqualTo("hostile-1"));
        Assert.That(row.Source.SourceRef, Is.EqualTo("observer:u1|target:hostile-1"));
        Assert.That(row.Confidence, Is.EqualTo(ContactProvenanceConfidence.Medium));
        Assert.That(row.Freshness, Is.EqualTo(ContactProvenanceFreshness.Fresh));
        Assert.That(row.AgeTicks, Is.EqualTo(0UL));
        Assert.That(row.LastKnown.LifecycleState, Is.EqualTo("Classified"));
        Assert.That(row.LastKnown.TargetId, Is.EqualTo("hostile-1"));
        Assert.That(row.LastKnown.LastSimTick, Is.EqualTo(5UL));
        Assert.That(row.LastKnown.LastSimTime, Is.EqualTo(5.0));
        Assert.That(row.OutOfCommsUnknown, Is.False);
        Assert.That(row.QualityState, Is.EqualTo(ContactProvenanceQualityState.None));
    }

    [Test]
    public void Stale_track_marks_named_stale_state_and_age()
    {
        var log = new DecisionLog();
        log.AppendContactChange(Change(1, "c1", "hostile-1", "Unknown", "Classified"));

        var snapshot = ContactProvenanceProjection.Project(
            log,
            currentSimTick: 1UL + (ulong)ContactProvenanceProjection.DefaultStaleThresholdTicks + 1);

        Assert.That(snapshot.Contacts[0].Freshness, Is.EqualTo(ContactProvenanceFreshness.Stale));
        Assert.That(snapshot.Contacts[0].AgeTicks, Is.EqualTo((ulong)ContactProvenanceProjection.DefaultStaleThresholdTicks + 1));
        Assert.That(snapshot.Contacts[0].QualityState.HasFlag(ContactProvenanceQualityState.Stale), Is.True);
        Assert.That(snapshot.Contacts[0].QualityState, Is.Not.EqualTo(ContactProvenanceQualityState.None));
    }

    [Test]
    public void Denied_comms_marks_out_of_comms_unknown_and_silent_comms()
    {
        var log = new DecisionLog();
        log.AppendContactChange(Change(3, "c1", "hostile-1", "Unknown", "Detected"));
        log.AppendCommsStateChange(new CommsStateChangeRecord(
            0,
            3.0,
            3,
            "c2-net",
            CommsState.Nominal,
            CommsState.Denied,
            "jam"));

        var snapshot = ContactProvenanceProjection.Project(log, currentSimTick: 3);

        Assert.That(snapshot.Contacts[0].OutOfCommsUnknown, Is.True);
        Assert.That(snapshot.Contacts[0].QualityState.HasFlag(ContactProvenanceQualityState.SilentComms), Is.True);
    }

    [Test]
    public void Catalog_miss_is_named_when_target_not_in_reader()
    {
        var log = new DecisionLog();
        log.AppendContactChange(Change(2, "c1", "unknown-ship-9", "Unknown", "Detected"));

        var catalog = new InMemoryCatalogReader(
            Array.Empty<CatalogSensorBinding>(),
            platforms: [new CatalogPlatformEntry("hostile-1", 0, 0, 0)]);

        var snapshot = ContactProvenanceProjection.Project(log, currentSimTick: 2, catalog: catalog);

        Assert.That(snapshot.Contacts[0].QualityState.HasFlag(ContactProvenanceQualityState.CatalogMiss), Is.True);
        Assert.That(snapshot.Contacts[0].LastKnown.TargetId, Is.EqualTo("unknown-ship-9"));
    }

    [Test]
    public void Cloned_unit_instance_resolves_platform_id_and_does_not_catalog_miss()
    {
        const string instanceId = "hostile-clone-1";
        const string platformId = "em-sovremenny-i-pr-956-sarych";
        var log = new DecisionLog();
        log.AppendContactChange(Change(2, "c1", instanceId, "Unknown", "Detected"));

        var catalog = new InMemoryCatalogReader(
            Array.Empty<CatalogSensorBinding>(),
            platforms: [new CatalogPlatformEntry(platformId, 0, 0, 0, Domain: "surface")]);

        var orbat = new[]
        {
            new ScenarioOrbatUnitDto
            {
                Id = instanceId,
                PlatformId = platformId,
                SideId = "red",
            },
        };

        var snapshot = ContactProvenanceProjection.Project(
            log,
            currentSimTick: 2,
            catalog: catalog,
            orbatUnits: orbat);

        Assert.That(snapshot.Contacts[0].QualityState.HasFlag(ContactProvenanceQualityState.CatalogMiss), Is.False);
        Assert.That(snapshot.Contacts[0].QualityState, Is.EqualTo(ContactProvenanceQualityState.None));
        Assert.That(snapshot.Contacts[0].LastKnown.TargetId, Is.EqualTo(instanceId));
    }

    [Test]
    public void Orbat_mapped_platform_missing_from_catalog_still_catalog_miss()
    {
        const string instanceId = "hostile-clone-2";
        const string missingPlatformId = "missing-platform-42";
        var log = new DecisionLog();
        log.AppendContactChange(Change(2, "c1", instanceId, "Unknown", "Detected"));

        var catalog = new InMemoryCatalogReader(
            Array.Empty<CatalogSensorBinding>(),
            platforms: [new CatalogPlatformEntry("hostile-1", 0, 0, 0)]);

        var orbat = new[]
        {
            new ScenarioOrbatUnitDto
            {
                Id = instanceId,
                PlatformId = missingPlatformId,
                SideId = "red",
            },
        };

        var snapshot = ContactProvenanceProjection.Project(
            log,
            currentSimTick: 2,
            catalog: catalog,
            orbatUnits: orbat);

        Assert.That(snapshot.Contacts[0].QualityState.HasFlag(ContactProvenanceQualityState.CatalogMiss), Is.True);
        Assert.That(snapshot.Contacts[0].LastKnown.TargetId, Is.EqualTo(instanceId));
    }

    [Test]
    public void Without_orbat_instance_id_still_catalog_misses_when_not_catalog_key()
    {
        const string instanceId = "hostile-clone-3";
        const string platformId = "em-sovremenny-i-pr-956-sarych";
        var log = new DecisionLog();
        log.AppendContactChange(Change(2, "c1", instanceId, "Unknown", "Detected"));

        var catalog = new InMemoryCatalogReader(
            Array.Empty<CatalogSensorBinding>(),
            platforms: [new CatalogPlatformEntry(platformId, 0, 0, 0)]);

        var snapshot = ContactProvenanceProjection.Project(log, currentSimTick: 2, catalog: catalog);

        Assert.That(snapshot.Contacts[0].QualityState.HasFlag(ContactProvenanceQualityState.CatalogMiss), Is.True);
    }

    [Test]
    public void Degraded_comms_accelerates_staleness_via_silent_comms()
    {
        var log = new DecisionLog();
        log.AppendContactChange(Change(1, "c1", "hostile-1", "Unknown", "Classified"));
        log.AppendCommsStateChange(new CommsStateChangeRecord(
            0,
            1.0,
            1,
            "c2-net",
            CommsState.Nominal,
            CommsState.Degraded,
            "datalink-lag"));

        var display = new ScenarioCommsDisplaySettings(2, 0.06f, 0.04f, degradedOrderDelayTicks: 0, degradedStaleThresholdDivisor: 2);
        var effectiveStale = ContactProvenanceProjection.DefaultStaleThresholdTicks / 2;
        var staleTick = 1UL + (ulong)effectiveStale + 1;

        var snapshot = ContactProvenanceProjection.Project(
            log,
            currentSimTick: staleTick,
            commsDisplay: display);

        Assert.That(snapshot.Contacts[0].Freshness, Is.EqualTo(ContactProvenanceFreshness.Stale));
        Assert.That(snapshot.Contacts[0].QualityState.HasFlag(ContactProvenanceQualityState.SilentComms), Is.True);
        Assert.That(snapshot.Contacts[0].OutOfCommsUnknown, Is.False);
    }

    [Test]
    public void Identical_inputs_yield_identical_fingerprint()
    {
        var log = new DecisionLog();
        log.AppendContactChange(Change(1, "c2", "hostile-2", "Unknown", "Detected"));
        log.AppendContactChange(Change(1, "c1", "hostile-1", "Unknown", "Classified"));
        log.AppendContactChange(Change(4, "c1", "hostile-1", "Classified", "Identified"));

        var a = ContactProvenanceProjection.Project(log, currentSimTick: 4);
        var b = ContactProvenanceProjection.Project(log, currentSimTick: 4);

        Assert.That(ContactProvenanceFingerprint.Compute(a), Is.EqualTo(ContactProvenanceFingerprint.Compute(b)));
        Assert.That(a.Contacts.Select(c => c.ContactId), Is.EqualTo(new[] { "c1", "c2" }));
    }

    [Test]
    public void Unordered_contact_picture_sorts_before_fingerprint()
    {
        var contacts = new[]
        {
            new ContactPictureEntry("c2", "hostile-2", "u2", "Detected", 2, 2.0),
            new ContactPictureEntry("c1", "hostile-1", "u1", "Classified", 1, 1.0),
        };

        var a = ContactProvenanceProjection.Project(contacts, currentSimTick: 2);
        var b = ContactProvenanceProjection.Project(contacts.Reverse().ToArray(), currentSimTick: 2);

        Assert.That(ContactProvenanceFingerprint.Compute(a), Is.EqualTo(ContactProvenanceFingerprint.Compute(b)));
    }

    [Test]
    public void Provenance_dtos_omit_selection_hover_camera_and_panel_visibility()
    {
        var types = new[]
        {
            typeof(ContactProvenanceState),
            typeof(ContactProvenanceSnapshot),
            typeof(ContactProvenanceSource),
            typeof(ContactProvenanceLastKnown),
        };
        string[] forbidden = ["selection", "hover", "camera", "visible", "visibility", "selected"];

        foreach (var type in types)
        {
            foreach (var property in type.GetProperties())
            {
                var name = property.Name.ToLowerInvariant();
                Assert.That(
                    forbidden.Any(token => name.Contains(token, StringComparison.Ordinal)),
                    Is.False,
                    $"{type.Name}.{property.Name} is UI-derived truth");
            }
        }
    }

    private static ContactChangeRecord Change(
        ulong tick,
        string contactId,
        string targetId,
        string previous,
        string next) =>
        new(0, tick, tick, "u1", contactId, targetId, previous, next);
}
