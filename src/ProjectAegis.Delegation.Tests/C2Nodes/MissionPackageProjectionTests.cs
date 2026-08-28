using ProjectAegis.Delegation.C2Nodes;
using ProjectAegis.Delegation.Comms;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Sim.Catalog;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.C2Nodes;

[TestFixture]
public sealed class MissionPackageProjectionTests
{
    [Test]
    public void Empty_definitions_yield_empty_snapshot()
    {
        var snapshot = MissionPackageProjection.Project(Array.Empty<PackageDefinition>());

        Assert.That(snapshot, Is.EqualTo(MissionPackageSnapshot.Empty));
        Assert.That(MissionPackageProjection.ComputeFingerprint(snapshot), Is.EqualTo("pkg:empty"));
    }

    [Test]
    public void Composed_package_lists_all_roles_with_membership_and_availability()
    {
        var definition = BalticAsuwPackage();
        var snapshot = MissionPackageProjection.Project(
            new[] { definition },
            currentSimTick: 12,
            currentSimTime: 12.0);

        Assert.That(snapshot.ActivePackageId, Is.EqualTo("pkg-asuw-1"));
        Assert.That(snapshot.Elements, Has.Count.EqualTo(4));
        Assert.That(snapshot.Packages, Has.Count.EqualTo(1));

        Assert.That(
            snapshot.Elements.Select(e => e.Role),
            Is.EqualTo(new[]
            {
                C2NodeRole.C2,
                C2NodeRole.Relay,
                C2NodeRole.Sensor,
                C2NodeRole.Shooter,
            }));

        foreach (var element in snapshot.Elements)
        {
            Assert.That(element.Availability, Is.EqualTo(C2NodeAvailability.Available));
            Assert.That(element.Membership.PackageId, Is.EqualTo("pkg-asuw-1"));
            Assert.That(element.Membership.PackageLabel, Is.EqualTo("Baltic ASuW Package"));
            Assert.That(element.TaskOrgDetached, Is.False);
            Assert.That(element.LastSimTick, Is.EqualTo(12UL));
            Assert.That(element.LastSimTime, Is.EqualTo(12.0));
        }

        var membership = snapshot.Packages[0];
        Assert.That(
            membership.ElementIds,
            Is.EqualTo(new[] { "elem-c2-1", "elem-relay-1", "elem-sensor-1", "elem-shooter-1" }));
        Assert.That(membership.UnitIds, Is.EqualTo(new[] { "u1", "u2", "u3" }));
    }

    [Test]
    public void Unavailable_node_marks_shooter_without_dropping_package_membership()
    {
        var definition = BalticAsuwPackage();
        var log = new DecisionLog();
        log.AppendPlatformDamageChange(new PlatformDamageChangeRecord(
            0,
            7.0,
            7,
            new TargetId("u2"),
            100,
            0,
            PlatformDamageChangeReasonCodes.Hit,
            2));
        var damageSequenceId = log.PlatformDamageChanges[0].SequenceId;

        var snapshot = MissionPackageProjection.Project(
            new[] { definition },
            log,
            currentSimTick: 7,
            currentSimTime: 7.0);

        var shooter = snapshot.Elements.Single(e => e.ElementId == "elem-shooter-1");
        Assert.That(shooter.Availability, Is.EqualTo(C2NodeAvailability.Unavailable));
        Assert.That(shooter.PlatformUnitId, Is.EqualTo("u2"));
        Assert.That(shooter.CorrelationSequenceId, Is.EqualTo(damageSequenceId));

        var sensor = snapshot.Elements.Single(e => e.ElementId == "elem-sensor-1");
        Assert.That(sensor.Availability, Is.EqualTo(C2NodeAvailability.Available));

        Assert.That(snapshot.Packages[0].ElementIds, Does.Contain("elem-shooter-1"));
        Assert.That(snapshot.Packages[0].UnitIds, Does.Contain("u2"));
    }

    [Test]
    public void Authoritative_dead_platform_stays_unavailable_when_damage_row_shows_partial_hp()
    {
        var definition = BalticAsuwPackage();
        var log = new DecisionLog();
        log.AppendPlatformDamageChange(new PlatformDamageChangeRecord(
            0,
            5.0,
            5,
            new TargetId("u2"),
            100,
            50,
            PlatformDamageChangeReasonCodes.Hit,
            1));

        var snapshot = MissionPackageProjection.Project(
            new[] { definition },
            log,
            isPlatformAlive: _ => false);

        var shooter = snapshot.Elements.Single(e => e.ElementId == "elem-shooter-1");
        Assert.That(shooter.Availability, Is.EqualTo(C2NodeAvailability.Unavailable));
    }

    [Test]
    public void Explicit_isPlatformAlive_false_marks_all_elements_unavailable()
    {
        var snapshot = MissionPackageProjection.Project(
            new[] { BalticAsuwPackage() },
            isPlatformAlive: _ => false);

        Assert.That(snapshot.Elements, Is.Not.Empty);
        Assert.That(snapshot.Elements.All(e => e.Availability == C2NodeAvailability.Unavailable), Is.True);
    }

    [Test]
    public void Comms_denied_marks_relay_and_c2_unavailable()
    {
        var log = new DecisionLog();
        log.AppendCommsStateChange(new CommsStateChangeRecord(
            0, 1, 1, "c2-net", CommsState.Nominal, CommsState.Denied, "jam"));

        var snapshot = MissionPackageProjection.Project(new[] { BalticAsuwPackage() }, log);

        var relay = snapshot.Elements.Single(e => e.ElementId == "elem-relay-1");
        var c2 = snapshot.Elements.Single(e => e.ElementId == "elem-c2-1");
        var sensor = snapshot.Elements.Single(e => e.ElementId == "elem-sensor-1");
        var shooter = snapshot.Elements.Single(e => e.ElementId == "elem-shooter-1");

        Assert.That(relay.Availability, Is.EqualTo(C2NodeAvailability.Unavailable));
        Assert.That(c2.Availability, Is.EqualTo(C2NodeAvailability.Unavailable));
        Assert.That(sensor.Availability, Is.EqualTo(C2NodeAvailability.Available));
        Assert.That(shooter.Availability, Is.EqualTo(C2NodeAvailability.Available));
    }

    [Test]
    public void Comms_degraded_marks_relay_and_c2_last_known()
    {
        var log = new DecisionLog();
        log.AppendCommsStateChange(new CommsStateChangeRecord(
            0, 1, 1, "c2-net", CommsState.Nominal, CommsState.Degraded, "jam"));

        var snapshot = MissionPackageProjection.Project(new[] { BalticAsuwPackage() }, log);

        var relay = snapshot.Elements.Single(e => e.ElementId == "elem-relay-1");
        var c2 = snapshot.Elements.Single(e => e.ElementId == "elem-c2-1");
        var sensor = snapshot.Elements.Single(e => e.ElementId == "elem-sensor-1");
        var shooter = snapshot.Elements.Single(e => e.ElementId == "elem-shooter-1");

        Assert.That(relay.Availability, Is.EqualTo(C2NodeAvailability.LastKnown));
        Assert.That(c2.Availability, Is.EqualTo(C2NodeAvailability.LastKnown));
        Assert.That(sensor.Availability, Is.EqualTo(C2NodeAvailability.Available));
        Assert.That(shooter.Availability, Is.EqualTo(C2NodeAvailability.Available));
    }

    [Test]
    public void Organic_sensor_and_package_feed_are_distinct_elements_on_same_platform()
    {
        var definition = new PackageDefinition(
            "pkg-track-1",
            "Track Package",
            new[]
            {
                new PackageElementDefinition("elem-organic-radar", "u1", C2NodeRole.Sensor, "organic-radar"),
                new PackageElementDefinition("elem-package-track", "u1", C2NodeRole.Sensor, "package-track-feed"),
            });

        var snapshot = MissionPackageProjection.Project(new[] { definition });

        Assert.That(snapshot.Elements, Has.Count.EqualTo(2));
        Assert.That(snapshot.Elements.Select(e => e.PlatformUnitId).Distinct(), Is.EqualTo(new[] { "u1" }));
        Assert.That(
            snapshot.Elements.Select(e => e.Membership.Kind),
            Is.EqualTo(new[] { C2NodeMembershipKind.Organic, C2NodeMembershipKind.Package }));
        Assert.That(
            snapshot.Elements.Select(e => e.CapabilityScope),
            Is.EqualTo(new[] { "organic-radar", "package-track-feed" }));
    }

    [Test]
    public void Task_org_detach_preserves_distinct_organic_and_package_sensor_elements()
    {
        var definition = new PackageDefinition(
            "pkg-track-1",
            "Track Package",
            new[]
            {
                new PackageElementDefinition("elem-organic-radar", "u1", C2NodeRole.Sensor, "organic-radar"),
                new PackageElementDefinition("elem-package-track", "u1", C2NodeRole.Sensor, "package-track-feed"),
            });

        var log = new DecisionLog();
        log.AppendGroupMemberDetach(new GroupMemberDetachRecord(
            3,
            3.0,
            new TargetId("grp-1"),
            new TargetId("u1")));

        var snapshot = MissionPackageProjection.Project(new[] { definition }, log);

        Assert.That(snapshot.Elements, Has.Count.EqualTo(2));
        Assert.That(snapshot.Elements.All(e => e.TaskOrgDetached), Is.True);
        Assert.That(snapshot.Elements.All(e => e.Availability == C2NodeAvailability.Available), Is.True);
        Assert.That(
            snapshot.Elements.Select(e => e.CapabilityScope),
            Is.EqualTo(new[] { "organic-radar", "package-track-feed" }));
        Assert.That(snapshot.Elements.All(e => e.SourceRefs.Contains("task-org:detached")), Is.True);
    }

    [Test]
    public void Unordered_definitions_sort_before_fingerprint_is_stable()
    {
        var first = BalticAsuwPackage();
        var second = new PackageDefinition(
            "pkg-zulu",
            "Zulu Package",
            new[] { new PackageElementDefinition("elem-z-1", "u9", C2NodeRole.Relay, "package-relay") });

        var a = MissionPackageProjection.Project(new[] { second, first });
        var b = MissionPackageProjection.Project(new[] { first, second });

        Assert.That(
            MissionPackageProjection.ComputeFingerprint(a),
            Is.EqualTo(MissionPackageProjection.ComputeFingerprint(b)));
    }

    [Test]
    public void Two_identical_builds_yield_identical_fingerprints()
    {
        var definition = BalticAsuwPackage();
        var a = MissionPackageProjection.Project(new[] { definition }, currentSimTick: 4, currentSimTime: 4.0);
        var b = MissionPackageProjection.Project(new[] { definition }, currentSimTick: 4, currentSimTime: 4.0);

        Assert.That(
            MissionPackageProjection.ComputeFingerprint(a),
            Is.EqualTo(MissionPackageProjection.ComputeFingerprint(b)));
    }

    [Test]
    public void Project_does_not_change_order_log_fingerprint()
    {
        var log = new DecisionLog();
        log.AppendGroupMemberDetach(new GroupMemberDetachRecord(
            1,
            1.0,
            new TargetId("grp-1"),
            new TargetId("u1")));
        log.AppendPlatformDamageChange(new PlatformDamageChangeRecord(
            2,
            2.0,
            2,
            new TargetId("u2"),
            100,
            0,
            PlatformDamageChangeReasonCodes.Hit,
            2));

        var fingerprintBefore = log.ComputeFingerprint();
        _ = MissionPackageProjection.Project(new[] { BalticAsuwPackage() }, log);
        Assert.That(log.ComputeFingerprint(), Is.EqualTo(fingerprintBefore));
    }

    [Test]
    public void Mission_package_dtos_omit_selection_hover_camera_and_panel_visibility()
    {
        var types = new[]
        {
            typeof(C2NodeElement),
            typeof(C2NodeMembership),
            typeof(MissionPackageMembership),
            typeof(MissionPackageSnapshot),
            typeof(PackageDefinition),
            typeof(PackageElementDefinition),
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

    private static PackageDefinition BalticAsuwPackage() =>
        new(
            "pkg-asuw-1",
            "Baltic ASuW Package",
            new[]
            {
                new PackageElementDefinition("elem-sensor-1", "u1", C2NodeRole.Sensor, "package-track-feed"),
                new PackageElementDefinition("elem-shooter-1", "u2", C2NodeRole.Shooter, "package-engage"),
                new PackageElementDefinition("elem-relay-1", "u3", C2NodeRole.Relay, "package-relay"),
                new PackageElementDefinition("elem-c2-1", "u1", C2NodeRole.C2, "organic-c2"),
            });
}
