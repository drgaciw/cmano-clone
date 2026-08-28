using ProjectAegis.Data.Catalog;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.SensorToShooter;
using ProjectAegis.Delegation.Skills;
using ProjectAegis.Delegation.TargetabilityAccept;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.TargetabilityAccept;

/// <summary>
/// DRG-219: headless composition harness for wave-1 targetability acceptance snapshots.
/// </summary>
[TestFixture]
public sealed class TargetabilityAcceptProjectionTests
{
    [Test]
    public void Permitted_path_fresh_chain_complete_and_authority_allows_targeting()
    {
        var log = TargetableContactLog();
        var authority = OrganicAuthorityContext(
            roe: RoeLevel.WeaponsFree,
            fireControlSatisfied: true);
        var shooters = new FixedShooterSource(
            new SensorToShooterShooterCandidate("u1", ScenarioEngageDefaults.MvpFallback, 2));

        var snapshot = TargetabilityAcceptProjection.Project(
            log,
            currentSimTick: 9,
            authority,
            fireControl: new StubFireControl("c1"),
            shooters: shooters,
            catalog: InMemoryCatalogReader.BalticPatrolFixture());

        Assert.That(snapshot.Contacts, Has.Count.EqualTo(1));
        var row = snapshot.Contacts[0];
        Assert.That(row.ContactId, Is.EqualTo("c1"));
        Assert.That(row.TargetId, Is.EqualTo("hostile-1"));
        Assert.That(row.Disposition, Is.EqualTo(TargetabilityAcceptDisposition.Permitted));
        Assert.That(row.WithheldCauseCode, Is.EqualTo(TargetabilityAcceptCauseCodes.None));
        Assert.That(row.Provenance, Is.Not.Null);
        Assert.That(row.Provenance!.Freshness, Is.EqualTo(ContactProvenanceFreshness.Fresh));
        Assert.That(row.SensorToShooter, Is.Not.Null);
        Assert.That(row.SensorToShooter!.IsComplete, Is.True);
        Assert.That(row.Authority.Targeting.Disposition, Is.EqualTo(C2AuthorityDisposition.Permitted));
    }

    [Test]
    public void Withheld_stale_provenance_names_stale_cause()
    {
        var log = new DecisionLog();
        log.AppendContactChange(Change(1, "c1", "hostile-1", "Unknown", "Classified"));

        var staleTick = 1UL + (ulong)ContactProvenanceProjection.DefaultStaleThresholdTicks + 1;
        var snapshot = TargetabilityAcceptProjection.Project(
            log,
            staleTick,
            OrganicAuthorityContext(),
            fireControl: new StubFireControl("c1"),
            shooters: new FixedShooterSource(
                new SensorToShooterShooterCandidate("u1", ScenarioEngageDefaults.MvpFallback, 2)),
            catalog: InMemoryCatalogReader.BalticPatrolFixture());

        var row = snapshot.Contacts[0];
        Assert.That(row.Disposition, Is.EqualTo(TargetabilityAcceptDisposition.Withheld));
        Assert.That(row.WithheldCauseCode, Is.EqualTo(TargetabilityAcceptCauseCodes.Stale));
        Assert.That(row.Provenance!.Freshness, Is.EqualTo(ContactProvenanceFreshness.Stale));
    }

    [Test]
    public void Withheld_no_fire_control_names_no_fc_from_sensor_to_shooter_chain()
    {
        var log = TargetableContactLog();
        var snapshot = TargetabilityAcceptProjection.Project(
            log,
            currentSimTick: 9,
            OrganicAuthorityContext(),
            fireControl: null,
            shooters: new FixedShooterSource(
                new SensorToShooterShooterCandidate("u1", ScenarioEngageDefaults.MvpFallback, 2)),
            catalog: InMemoryCatalogReader.BalticPatrolFixture());

        var row = snapshot.Contacts[0];
        Assert.That(row.Disposition, Is.EqualTo(TargetabilityAcceptDisposition.Withheld));
        Assert.That(row.WithheldCauseCode, Is.EqualTo(TargetabilityAcceptCauseCodes.NoFireControl));
        Assert.That(row.SensorToShooter!.PrimaryBreakCause, Is.EqualTo(SensorToShooterBreakCause.NoFireControl));
    }

    [Test]
    public void Withheld_weapons_tight_names_roe_cause_from_authority_projection()
    {
        var log = TargetableContactLog();
        var snapshot = TargetabilityAcceptProjection.Project(
            log,
            currentSimTick: 9,
            OrganicAuthorityContext(roe: RoeLevel.WeaponsTight),
            fireControl: new StubFireControl("c1"),
            shooters: new FixedShooterSource(
                new SensorToShooterShooterCandidate("u1", ScenarioEngageDefaults.MvpFallback, 2)),
            catalog: InMemoryCatalogReader.BalticPatrolFixture());

        var row = snapshot.Contacts[0];
        Assert.That(row.Disposition, Is.EqualTo(TargetabilityAcceptDisposition.Withheld));
        Assert.That(row.WithheldCauseCode, Is.EqualTo(TargetabilityAcceptCauseCodes.WeaponsTight));
        Assert.That(row.Authority.Targeting.ReasonCode, Is.EqualTo(C2AuthorityProjector.ReasonWeaponsTight));
    }

    [Test]
    public void Withheld_catalog_miss_names_catalog_miss_from_provenance()
    {
        var log = new DecisionLog();
        log.AppendContactChange(Change(2, "c1", "unknown-ship-9", "Unknown", "Detected"));

        var catalog = new InMemoryCatalogReader(
            Array.Empty<CatalogSensorBinding>(),
            platforms: [new CatalogPlatformEntry("hostile-1", 0, 0, 0)]);

        var snapshot = TargetabilityAcceptProjection.Project(
            log,
            currentSimTick: 2,
            OrganicAuthorityContext(),
            fireControl: new StubFireControl("c1"),
            shooters: new FixedShooterSource(
                new SensorToShooterShooterCandidate("u1", ScenarioEngageDefaults.MvpFallback, 2)),
            catalog: catalog);

        var row = snapshot.Contacts[0];
        Assert.That(row.Disposition, Is.EqualTo(TargetabilityAcceptDisposition.Withheld));
        Assert.That(row.WithheldCauseCode, Is.EqualTo(TargetabilityAcceptCauseCodes.CatalogMiss));
        Assert.That(row.Provenance!.QualityState.HasFlag(ContactProvenanceQualityState.CatalogMiss), Is.True);
    }

    [Test]
    public void Withheld_rows_never_use_empty_cause_code()
    {
        var withheldSnapshots = new[]
        {
            ProjectWithAuthority(OrganicAuthorityContext(roe: RoeLevel.HoldFire)),
            ProjectWithAuthority(OrganicAuthorityContext(roe: RoeLevel.WeaponsTight)),
            TargetabilityAcceptProjection.Project(
                StaleContactLog(),
                1UL + (ulong)ContactProvenanceProjection.DefaultStaleThresholdTicks + 1,
                OrganicAuthorityContext(),
                fireControl: new StubFireControl("c1"),
                shooters: new FixedShooterSource(
                    new SensorToShooterShooterCandidate("u1", ScenarioEngageDefaults.MvpFallback, 2)),
                catalog: InMemoryCatalogReader.BalticPatrolFixture()),
        };

        foreach (var snapshot in withheldSnapshots)
        {
            Assert.That(snapshot.Contacts, Is.Not.Empty);
            foreach (var row in snapshot.Contacts)
            {
                Assert.That(row.Disposition, Is.EqualTo(TargetabilityAcceptDisposition.Withheld));
                Assert.That(row.WithheldCauseCode, Is.Not.Empty);
                Assert.That(row.WithheldCauseCode, Is.Not.EqualTo(TargetabilityAcceptCauseCodes.None));
            }
        }
    }

    [Test]
    public void Identical_inputs_yield_identical_fingerprint()
    {
        var log = TargetableContactLog();
        var authority = OrganicAuthorityContext();
        var shooters = new FixedShooterSource(
            new SensorToShooterShooterCandidate("u1", ScenarioEngageDefaults.MvpFallback, 2));
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();
        var fireControl = new StubFireControl("c1");

        var a = TargetabilityAcceptProjection.Project(log, 9, authority, fireControl, shooters, catalog);
        var b = TargetabilityAcceptProjection.Project(log, 9, authority, fireControl, shooters, catalog);

        Assert.That(TargetabilityAcceptFingerprint.Compute(a), Is.EqualTo(TargetabilityAcceptFingerprint.Compute(b)));
    }

    [Test]
    public void Empty_log_yields_empty_snapshot_and_empty_fingerprint()
    {
        var snapshot = TargetabilityAcceptProjection.Project(
            new DecisionLog(),
            currentSimTick: 0,
            OrganicAuthorityContext());

        Assert.That(snapshot.Contacts, Is.Empty);
        Assert.That(TargetabilityAcceptFingerprint.Compute(snapshot), Is.EqualTo("tac:empty"));
    }

    [Test]
    public void Child_snapshot_overload_composes_without_reprojecting()
    {
        var log = TargetableContactLog();
        var provenance = ContactProvenanceProjection.Project(log, currentSimTick: 9);
        var sensorToShooter = SensorToShooterProjection.Project(
            log,
            currentSimTick: 9,
            fireControl: new StubFireControl("c1"),
            shooters: new FixedShooterSource(
                new SensorToShooterShooterCandidate("u1", ScenarioEngageDefaults.MvpFallback, 2)),
            catalog: InMemoryCatalogReader.BalticPatrolFixture());
        var authority = C2AuthorityProjector.Project(OrganicAuthorityContext());

        var snapshot = TargetabilityAcceptProjection.Project(provenance, sensorToShooter, authority);

        Assert.That(snapshot.Contacts[0].Disposition, Is.EqualTo(TargetabilityAcceptDisposition.Permitted));
        Assert.That(snapshot.Contacts[0].WithheldCauseCode, Is.EqualTo(TargetabilityAcceptCauseCodes.None));
    }

    private static DecisionLog StaleContactLog()
    {
        var log = new DecisionLog();
        log.AppendContactChange(Change(1, "c1", "hostile-1", "Unknown", "Classified"));
        return log;
    }

    private static TargetabilityAcceptSnapshot ProjectWithAuthority(C2AuthorityProjectionContext authority) =>
        TargetabilityAcceptProjection.Project(
            TargetableContactLog(),
            currentSimTick: 9,
            authority,
            fireControl: new StubFireControl("c1"),
            shooters: new FixedShooterSource(
                new SensorToShooterShooterCandidate("u1", ScenarioEngageDefaults.MvpFallback, 2)),
            catalog: InMemoryCatalogReader.BalticPatrolFixture());

    private static DecisionLog TargetableContactLog()
    {
        var log = new DecisionLog();
        log.AppendContactChange(Change(1, "c1", "hostile-1", "Unknown", "Detected"));
        log.AppendContactChange(Change(5, "c1", "hostile-1", "Detected", "Classified"));
        log.AppendContactChange(Change(9, "c1", "hostile-1", "Classified", "Identified"));
        return log;
    }

    private static C2AuthorityProjectionContext OrganicAuthorityContext(
        RoeLevel roe = RoeLevel.WeaponsFree,
        bool fireControlSatisfied = true) =>
        new(
            roe,
            SkillLane.Read,
            RequiredApproval.None,
            TrackSource.Organic,
            fireControlSatisfied,
            null,
            true);

    private static ContactChangeRecord Change(
        ulong tick,
        string contactId,
        string targetId,
        string previous,
        string next) =>
        new(0, tick, tick, "u1", contactId, targetId, previous, next);

    private sealed class FixedShooterSource : ISensorToShooterShooterSource
    {
        private readonly SensorToShooterShooterCandidate[] _candidates;

        public FixedShooterSource(params SensorToShooterShooterCandidate[] candidates) =>
            _candidates = candidates;

        public IReadOnlyList<SensorToShooterShooterCandidate> GetCandidatesForTarget(string targetId) =>
            _candidates;
    }

    private sealed class StubFireControl : IKillChainFireControlSource
    {
        private readonly HashSet<string> _contactIds;

        public StubFireControl(params string[] contactIds) =>
            _contactIds = new HashSet<string>(contactIds, StringComparer.Ordinal);

        public bool HasFireControlTrack(string contactId, string targetId) =>
            _contactIds.Contains(contactId);
    }
}
