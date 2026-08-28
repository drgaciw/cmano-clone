using ProjectAegis.Delegation.PlatformDegrade;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.PlatformDegrade;

public sealed class PlatformDegradeProjectionTests
{
    private static PlatformDegradeInput HealthyFleet(ulong simTick = 100) =>
        new(
            SimTick: simTick,
            Units: new[]
            {
                new PlatformDegradeUnitInput(UnitId: "u1"),
                new PlatformDegradeUnitInput(UnitId: "u2"),
            });

    [Test]
    public void Healthy_unit_reports_none_with_stable_fingerprint()
    {
        var input = HealthyFleet();
        var snapshot = PlatformDegradeProjection.Project(input);

        Assert.That(snapshot.Kind, Is.EqualTo(PlatformDegradeKind.AdvisoryDamageControl));
        Assert.That(snapshot.SimTick, Is.EqualTo(100UL));
        Assert.That(snapshot.Units, Has.Count.EqualTo(2));
        Assert.That(snapshot.IsWeaponsReleaseAuthorization, Is.False);
        Assert.That(snapshot.IsFireOrder, Is.False);
        Assert.That(snapshot.IsAutomaticEngagement, Is.False);
        Assert.That(snapshot.StatusLine, Does.Contain("NOMINAL").IgnoreCase);
        Assert.That(snapshot.StatusLine, Does.Contain("no orders").IgnoreCase);

        var u1 = snapshot.Units.Single(r => r.UnitId == "u1");
        Assert.That(u1.ActiveDegradeCodes, Is.EqualTo(new[] { PlatformDegradeCode.None }));
        Assert.That(u1.SeverityBand, Is.EqualTo(PlatformDegradeSeverityBand.None));
        Assert.That(u1.SimTick, Is.EqualTo(100UL));

        var rerun = PlatformDegradeProjection.Project(input);
        Assert.That(
            PlatformDegradeProjection.ComputeFingerprint(snapshot),
            Is.EqualTo(PlatformDegradeProjection.ComputeFingerprint(rerun)));
    }

    [Test]
    public void Mobility_degrade_reports_mobility_code_and_severity()
    {
        var input = new PlatformDegradeInput(
            SimTick: 250,
            Units: new[]
            {
                new PlatformDegradeUnitInput(
                    UnitId: "u-mobility",
                    MobilityDegraded: true,
                    MobilitySeverity: PlatformDegradeSeverityBand.Light),
            });

        var snapshot = PlatformDegradeProjection.Project(input);
        var row = snapshot.Units.Single();

        Assert.That(row.UnitId, Is.EqualTo("u-mobility"));
        Assert.That(row.ActiveDegradeCodes, Is.EqualTo(new[] { PlatformDegradeCode.Mobility }));
        Assert.That(row.SeverityBand, Is.EqualTo(PlatformDegradeSeverityBand.Light));
        Assert.That(row.SimTick, Is.EqualTo(250UL));
        Assert.That(snapshot.IsFireOrder, Is.False);
        Assert.That(snapshot.StatusLine, Does.Contain("DEGRADED").IgnoreCase);
    }

    [Test]
    public void Multi_system_degrade_lists_all_active_codes_and_max_severity()
    {
        var input = new PlatformDegradeInput(
            SimTick: 500,
            Units: new[]
            {
                new PlatformDegradeUnitInput(
                    UnitId: "u-multi",
                    MobilityDegraded: true,
                    MobilitySeverity: PlatformDegradeSeverityBand.Light,
                    SensorDegraded: true,
                    SensorSeverity: PlatformDegradeSeverityBand.Heavy,
                    CommsDegraded: true,
                    CommsSeverity: PlatformDegradeSeverityBand.Light),
            });

        var snapshot = PlatformDegradeProjection.Project(input);
        var row = snapshot.Units.Single();

        Assert.That(row.ActiveDegradeCodes, Is.EqualTo(new[]
        {
            PlatformDegradeCode.Mobility,
            PlatformDegradeCode.Sensor,
            PlatformDegradeCode.Comms,
        }));
        Assert.That(row.SeverityBand, Is.EqualTo(PlatformDegradeSeverityBand.Heavy));
        Assert.That(snapshot.IsWeaponsReleaseAuthorization, Is.False);
        Assert.That(snapshot.IsAutomaticEngagement, Is.False);
    }

    [Test]
    public void Empty_input_returns_empty_snapshot_with_pdg_empty_fingerprint()
    {
        var nullSnapshot = PlatformDegradeProjection.Project(null);
        Assert.That(nullSnapshot.Units, Is.Empty);
        Assert.That(nullSnapshot.SimTick, Is.EqualTo(0UL));
        Assert.That(PlatformDegradeProjection.ComputeFingerprint(nullSnapshot), Is.EqualTo("pdg:empty"));

        var emptyUnits = PlatformDegradeProjection.Project(new PlatformDegradeInput(SimTick: 42, Units: Array.Empty<PlatformDegradeUnitInput>()));
        Assert.That(emptyUnits.Units, Is.Empty);
        Assert.That(PlatformDegradeProjection.ComputeFingerprint(emptyUnits), Is.EqualTo("pdg:empty"));

        var blankUnitId = PlatformDegradeProjection.Project(
            new PlatformDegradeInput(
                SimTick: 99,
                Units: new[] { new PlatformDegradeUnitInput(UnitId: string.Empty) }));

        Assert.That(blankUnitId.Units, Is.Empty);
        Assert.That(PlatformDegradeProjection.ComputeFingerprint(blankUnitId), Is.EqualTo("pdg:empty"));
    }
}
