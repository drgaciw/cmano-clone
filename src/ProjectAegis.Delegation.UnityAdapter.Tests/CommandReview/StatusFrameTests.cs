using NUnit.Framework;
using ProjectAegis.Delegation.Comms;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Delegation.UnityAdapter.CommandReview;
using ProjectAegis.Sim.Policy;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.CommandReview;

[TestFixture]
public sealed class StatusFrameTests
{
    [Test]
    public void Known_sources_preserve_contact_emissions_ew_and_component_distinctions()
    {
        var bridge = BridgeWith("u1", "u2");
        var contacts = new SliceAContactFrame(
            KillChainContactSnapshot.Empty,
            new ContactProvenanceSnapshot(new[]
            {
                new ContactProvenanceState("c1", new("u1", "h1", "organic/radar"),
                    ContactProvenanceConfidence.High, ContactProvenanceFreshness.Stale, 9,
                    new("Identified", "h1", 4, 4.5), true,
                    ContactProvenanceQualityState.Stale | ContactProvenanceQualityState.SilentComms),
            }),
            ProjectAegis.Delegation.SensorToShooter.SensorToShooterSnapshot.Empty,
            Array.Empty<ContactPictureEntry>(),
            new Dictionary<string, ProjectAegis.Delegation.Skills.C2AuthorityProjection>());

        var frame = StatusFrameBridge.Build(bridge, new KnownSnapshot(), contacts);

        Assert.That(frame.Contacts.Single().Confidence, Is.EqualTo(ContactProvenanceConfidence.High));
        Assert.That(frame.Contacts.Single().SourceRef, Is.EqualTo("organic/radar"));
        Assert.That(frame.Contacts.Single().IsOutOfComms, Is.True);
        Assert.That(frame.Units.Single(u => u.UnitId == "u1").Sensors.State, Is.EqualTo(StatusKnowledge.Degraded));
        Assert.That(frame.Units.Single(u => u.UnitId == "u1").Mounts.State, Is.EqualTo(StatusKnowledge.Offline));
        Assert.That(frame.Units.Single(u => u.UnitId == "u2").Recovery.State, Is.EqualTo(StatusKnowledge.Unknown));
        Assert.That(frame.Emissions.Single(e => e.UnitId == "u1").KnownEmitters, Is.EqualTo(new[] { "radar-a" }));
        Assert.That(frame.ElectronicWarfare.Single(e => e.UnitId == "u1").State, Is.EqualTo(StatusKnowledge.Nominal));
        Assert.That(frame.PlatformDegradation!.Units.Single(u => u.UnitId == "u1").ActiveDegradeCodes,
            Does.Contain(ProjectAegis.Delegation.PlatformDegrade.PlatformDegradeCode.Sensor));
    }

    [Test]
    public void Runtime_log_maps_damage_comms_readiness_and_sequence_correlation_without_optional_source()
    {
        var bridge = BridgeWith("u1", "u2").EnableMvpEngagement();
        bridge.Orchestrator.DecisionLog.AppendPlatformDamageChange(new(0, 7.5, 7, new TargetId("u1"), 100, 55, "hit", 2));
        bridge.Orchestrator.DecisionLog.AppendPlatformDamageChange(new(0, 7.75, 7, new TargetId("u2"), 100, 80, "sensor-check", 1));
        bridge.Orchestrator.DecisionLog.AppendCommsStateChange(new(0, 8, 8, "u1", CommsState.Nominal, CommsState.Denied, "jammed"));

        var frame = StatusFrameBridge.Build(bridge, new BareSnapshot(9), SliceAContactFrame.Empty);
        var unit = frame.Units.Single(u => u.UnitId == "u1");

        Assert.That(unit.Platform.State, Is.EqualTo(StatusKnowledge.Degraded));
        Assert.That(unit.Comms.State, Is.EqualTo(StatusKnowledge.Offline));
        Assert.That(unit.Readiness.State, Is.EqualTo(StatusKnowledge.Unknown));
        Assert.That(unit.Sensors.State, Is.EqualTo(StatusKnowledge.Unknown));
        Assert.That(frame.Units.Single(u => u.UnitId == "u2").Sensors.State, Is.EqualTo(StatusKnowledge.Unknown));
        Assert.That(frame.Emissions.Single(e => e.UnitId == "u2").State, Is.EqualTo(StatusKnowledge.Unknown));
        Assert.That(frame.PlatformDegradation, Is.Null, "unknown components cannot be projected as healthy");
        Assert.That(frame.Correlations.Select(c => c.SequenceId), Is.Ordered);
        Assert.That(frame.Correlations.Last().SimTime, Is.EqualTo(8));
    }

    [Test]
    public void Presenter_filters_clutter_only_and_keeps_text_redundancy()
    {
        var frame = StatusFrameBridge.Build(BridgeWith("u1", "u2"), new KnownSnapshot(), SliceAContactFrame.Empty);
        var all = StatusPresenter.Build(frame, StatusDisplayFilter.All);
        var problems = StatusPresenter.Build(frame, StatusDisplayFilter.ProblemsOnly);

        Assert.That(all.UnitRows, Has.Count.EqualTo(2));
        Assert.That(problems.UnitRows, Has.Count.EqualTo(1));
        Assert.That(problems.UnitRows[0].Text, Does.Contain("SENSORS DEGRADED").And.Contain("MOUNTS OFFLINE"));
        Assert.That(frame.Units, Has.Count.EqualTo(2), "display filtering must not alter semantic facts");
    }

    [Test]
    public void Emissions_fallback_keeps_comms_denial_scoped_to_its_unit()
    {
        var bridge = BridgeWith("u1", "u2", "u3");
        bridge.Orchestrator.DecisionLog.AppendCommsStateChange(new(0, 7, 7, "u2", CommsState.Degraded, CommsState.Nominal, "restored"));
        bridge.Orchestrator.DecisionLog.AppendCommsStateChange(new(0, 8, 8, "u1", CommsState.Nominal, CommsState.Denied, "jammed"));
        bridge.Orchestrator.DecisionLog.AppendCommsStateChange(new(0, 11, 11, "u2", CommsState.Nominal, CommsState.Denied, "future"));

        var frame = StatusFrameBridge.Build(bridge, new BareSnapshot(10), SliceAContactFrame.Empty);

        Assert.That(frame.Emissions.Single(e => e.UnitId == "u1").Detail, Does.Contain("COMMS_DENIED"));
        Assert.That(frame.Emissions.Single(e => e.UnitId == "u2").Detail, Does.Not.Contain("COMMS_DENIED"));
        Assert.That(frame.Emissions.Single(e => e.UnitId == "u3").Detail, Does.Not.Contain("COMMS_DENIED"));
        Assert.That(frame.Emissions.All(e => e.State == StatusKnowledge.Unknown), Is.True);
    }

    [Test]
    public void Zero_hp_platform_is_offline_and_remains_visible_in_problem_rows()
    {
        var bridge = BridgeWith("u1");
        bridge.Orchestrator.DecisionLog.AppendPlatformDamageChange(new(0, 9, 9, new TargetId("u1"), 40, 0, "hit", 3));

        var frame = StatusFrameBridge.Build(bridge, new KnownSnapshot(), SliceAContactFrame.Empty);
        var unit = frame.Units.Single();

        Assert.That(unit.Platform.State, Is.EqualTo(StatusKnowledge.Offline));
        Assert.That(unit.Platform.SourceSimTime, Is.EqualTo(9));
        Assert.That(unit.Recovery.State, Is.EqualTo(StatusKnowledge.Unknown));
        Assert.That(StatusPresenter.Build(frame, StatusDisplayFilter.ProblemsOnly).UnitRows.Single().Text,
            Does.Contain("PLATFORM OFFLINE"));
    }

    [TestCase(double.NaN)]
    [TestCase(double.PositiveInfinity)]
    [TestCase(-1)]
    public void Invalid_snapshot_time_is_rejected(double time)
    {
        Assert.Throws<ArgumentOutOfRangeException>((Action)(() =>
            StatusFrameBridge.Build(BridgeWith("u1"), new BareSnapshot(time), SliceAContactFrame.Empty)));
    }

    [Test]
    public void Future_contact_frame_is_rejected_even_when_integer_tick_matches()
    {
        var contacts = SliceAContactFrame.Empty with { SimTick = 5, SimTime = 5.9 };
        Assert.Throws<ArgumentException>((Action)(() =>
            StatusFrameBridge.Build(BridgeWith("u1"), new BareSnapshot(5.1), contacts)));
    }

    [Test]
    public void Invalid_contact_frame_time_is_rejected()
    {
        var contacts = SliceAContactFrame.Empty with { SimTime = double.NaN };
        Assert.Throws<ArgumentException>((Action)(() =>
            StatusFrameBridge.Build(BridgeWith("u1"), new BareSnapshot(5), contacts)));
    }

    [Test]
    public void Future_log_records_are_excluded_from_facts_and_correlations()
    {
        var bridge = BridgeWith("u1");
        bridge.Orchestrator.DecisionLog.AppendPlatformDamageChange(new(0, 11, 11, new TargetId("u1"), 100, 1, "future", 3));
        bridge.Orchestrator.DecisionLog.AppendCommsStateChange(new(0, 12, 12, "u1", CommsState.Nominal, CommsState.Denied, "future"));

        var frame = StatusFrameBridge.Build(bridge, new BareSnapshot(10), SliceAContactFrame.Empty);

        Assert.That(frame.Units.Single().Platform.State, Is.EqualTo(StatusKnowledge.Unknown));
        Assert.That(frame.Units.Single().Comms.State, Is.EqualTo(StatusKnowledge.Unknown));
        Assert.That(frame.Correlations, Is.Empty);
    }

    [Test]
    public void Mismatched_unit_and_sensor_identity_are_rejected()
    {
        Assert.Throws<InvalidOperationException>((Action)(() => StatusFrameBridge.Build(
            BridgeWith("u1"), new InvalidIdentitySnapshot(unitMismatch: true), SliceAContactFrame.Empty)));
        Assert.Throws<InvalidOperationException>((Action)(() => StatusFrameBridge.Build(
            BridgeWith("u1"), new InvalidIdentitySnapshot(sensorMismatch: true), SliceAContactFrame.Empty)));
    }

    [Test]
    public void Future_source_fact_is_rejected()
    {
        Assert.Throws<InvalidOperationException>((Action)(() => StatusFrameBridge.Build(
            BridgeWith("u1"), new InvalidIdentitySnapshot(futureFact: true), SliceAContactFrame.Empty)));
        Assert.Throws<InvalidOperationException>((Action)(() => StatusFrameBridge.Build(
            BridgeWith("u1"), new InvalidIdentitySnapshot(futureSensor: true), SliceAContactFrame.Empty)));
    }

    private static DelegationBridge BridgeWith(params string[] ids)
    {
        var bridge = new DelegationBridge(1);
        for (var i = 0; i < ids.Length; i++) bridge.Registry.RegisterUnit(new EntityKey(i + 1), ids[i]);
        return bridge;
    }

    private class BareSnapshot(double time) : ISimWorldSnapshot
    {
        public double SimTime => time;
        public int ContactCount => 0;
        public int ActiveEngagementCount => 0;
        public bool IsMemberAlive(TargetId memberId) => true;
        public TargetId? PrimaryHostileContactId => null;
        public bool HasFireControlTrackOnPrimaryContact => false;
        public bool ObserverRadarEmconActive => false;
    }

    private sealed class KnownSnapshot : BareSnapshot, IStatusUnitSource, IStatusSensorSource, IStatusElectronicWarfareSource
    {
        public KnownSnapshot() : base(10) { }
        public bool TryGetUnitStatus(string unitId, out StatusUnitFacts facts)
        {
            facts = unitId == "u1"
                ? new(unitId, StatusKnowledge.Nominal, StatusKnowledge.Degraded, StatusKnowledge.Offline,
                    StatusKnowledge.Nominal, StatusKnowledge.Nominal, StatusFact.Unknown("recovery"))
                : new(unitId, StatusKnowledge.Nominal, StatusKnowledge.Nominal, StatusKnowledge.Nominal,
                    StatusKnowledge.Nominal, StatusKnowledge.Nominal, StatusFact.Unknown("recovery"));
            return true;
        }
        public bool TryGetSensorStatus(string unitId, out StatusSensorFacts facts)
        {
            facts = new(unitId, EmconState.Active, new[] { "radar-a" });
            return unitId == "u1";
        }
        public bool TryGetElectronicWarfareStatus(string unitId, out StatusFact fact)
        {
            fact = StatusFact.Nominal("EW monitored");
            return unitId == "u1";
        }
    }

    private sealed class InvalidIdentitySnapshot(bool unitMismatch = false, bool sensorMismatch = false,
        bool futureFact = false, bool futureSensor = false)
        : BareSnapshot(10), IStatusUnitSource, IStatusSensorSource
    {
        public bool TryGetUnitStatus(string unitId, out StatusUnitFacts facts)
        {
            var platform = futureFact
                ? new StatusFact(StatusKnowledge.Nominal, "platform", SourceSimTime: 11)
                : StatusFact.Nominal("platform");
            facts = new StatusUnitFacts(unitMismatch ? "other" : unitId, platform,
                StatusFact.Nominal("sensors"), StatusFact.Nominal("mounts"), StatusFact.Nominal("comms"),
                StatusFact.Nominal("mobility"), StatusFact.Nominal("readiness"), StatusFact.Unknown("recovery"));
            return true;
        }

        public bool TryGetSensorStatus(string unitId, out StatusSensorFacts facts)
        {
            facts = new StatusSensorFacts(sensorMismatch ? "other" : unitId, EmconState.Active,
                Array.Empty<string>(), futureSensor ? 11 : null);
            return true;
        }
    }
}
