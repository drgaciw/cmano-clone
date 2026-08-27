using ProjectAegis.Data.Catalog;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.SensorToShooter;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.SensorToShooter;

[TestFixture]
public sealed class SensorToShooterProjectionTests
{
    [Test]
    public void Complete_chain_links_sensor_track_targetability_and_eligible_shooter()
    {
        var log = new DecisionLog();
        log.AppendContactChange(Change(1, "c1", "hostile-1", "Unknown", "Detected"));
        log.AppendContactChange(Change(5, "c1", "hostile-1", "Detected", "Classified"));
        log.AppendContactChange(Change(9, "c1", "hostile-1", "Classified", "Identified"));

        var shooters = new FixedShooterSource(
            new SensorToShooterShooterCandidate(
                "u1",
                ScenarioEngageDefaults.MvpFallback,
                2));
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();

        var snapshot = SensorToShooterProjection.Project(
            log,
            currentSimTick: 9,
            fireControl: new StubFireControl("c1"),
            shooters: shooters,
            catalog: catalog);

        Assert.That(snapshot.Chains, Has.Count.EqualTo(1));
        var chain = snapshot.Chains[0];
        Assert.That(chain.IsComplete, Is.True);
        Assert.That(chain.PrimaryBreakCause, Is.EqualTo(SensorToShooterBreakCause.None));
        Assert.That(chain.ObserverId, Is.EqualTo("u1"));
        Assert.That(chain.Links, Has.Count.EqualTo(4));
        Assert.That(chain.Links.All(l => l.IsLinked), Is.True);
        Assert.That(chain.Links[0].Kind, Is.EqualTo(SensorToShooterLinkKind.Sensor));
        Assert.That(chain.Links[0].UnitId, Is.EqualTo("u1"));
        Assert.That(chain.Links[1].Kind, Is.EqualTo(SensorToShooterLinkKind.Track));
        Assert.That(chain.Links[1].UnitId, Is.EqualTo("c1"));
        Assert.That(chain.Links[2].Kind, Is.EqualTo(SensorToShooterLinkKind.Targetability));
        Assert.That(chain.Links[3].Kind, Is.EqualTo(SensorToShooterLinkKind.EligibleShooter));
        Assert.That(chain.Links[3].UnitId, Is.EqualTo("u1"));
    }

    [Test]
    public void Stale_track_names_stale_cause_and_breaks_downstream_links()
    {
        var log = new DecisionLog();
        log.AppendContactChange(Change(1, "c1", "hostile-1", "Unknown", "Classified"));

        var staleTick = 1UL + (ulong)KillChainContactStateProjection.DefaultStaleThresholdTicks + 1;
        var shooters = new FixedShooterSource(
            new SensorToShooterShooterCandidate(
                "u1",
                ScenarioEngageDefaults.MvpFallback,
                2));

        var snapshot = SensorToShooterProjection.Project(
            log,
            currentSimTick: staleTick,
            fireControl: new StubFireControl("c1"),
            shooters: shooters,
            catalog: InMemoryCatalogReader.BalticPatrolFixture());

        var chain = snapshot.Chains[0];
        Assert.That(chain.IsComplete, Is.False);
        Assert.That(chain.PrimaryBreakCause, Is.EqualTo(SensorToShooterBreakCause.StaleTrack));
        Assert.That(chain.PrimaryCauseLabel, Is.EqualTo(SensorToShooterBreakCauseLabels.StaleTrack));

        var track = chain.Links.Single(l => l.Kind == SensorToShooterLinkKind.Track);
        Assert.That(track.IsLinked, Is.False);
        Assert.That(track.BreakCause, Is.EqualTo(SensorToShooterBreakCause.StaleTrack));
        Assert.That(track.CauseLabel, Is.EqualTo(SensorToShooterBreakCauseLabels.StaleTrack));

        var targetability = chain.Links.Single(l => l.Kind == SensorToShooterLinkKind.Targetability);
        Assert.That(targetability.IsLinked, Is.False);
        Assert.That(targetability.BreakCause, Is.EqualTo(SensorToShooterBreakCause.StaleTrack));

        var shooter = chain.Links.Single(l => l.Kind == SensorToShooterLinkKind.EligibleShooter);
        Assert.That(shooter.IsLinked, Is.False);
        Assert.That(shooter.BreakCause, Is.EqualTo(SensorToShooterBreakCause.StaleTrack));
    }

    [Test]
    public void No_fire_control_names_no_fc_on_targetability_link()
    {
        var log = new DecisionLog();
        log.AppendContactChange(Change(1, "c1", "hostile-1", "Unknown", "Detected"));
        log.AppendContactChange(Change(5, "c1", "hostile-1", "Detected", "Classified"));
        log.AppendContactChange(Change(9, "c1", "hostile-1", "Classified", "Identified"));

        var snapshot = SensorToShooterProjection.Project(
            log,
            currentSimTick: 9,
            fireControl: null,
            shooters: new FixedShooterSource(
                new SensorToShooterShooterCandidate(
                    "u1",
                    ScenarioEngageDefaults.MvpFallback,
                    2)),
            catalog: InMemoryCatalogReader.BalticPatrolFixture());

        var chain = snapshot.Chains[0];
        Assert.That(chain.IsComplete, Is.False);
        Assert.That(chain.PrimaryBreakCause, Is.EqualTo(SensorToShooterBreakCause.NoFireControl));

        var targetability = chain.Links.Single(l => l.Kind == SensorToShooterLinkKind.Targetability);
        Assert.That(targetability.IsLinked, Is.False);
        Assert.That(targetability.BreakCause, Is.EqualTo(SensorToShooterBreakCause.NoFireControl));
        Assert.That(targetability.CauseLabel, Is.EqualTo(SensorToShooterBreakCauseLabels.NoFireControl));
    }

    [Test]
    public void Lost_sensor_names_lost_sensor_and_breaks_sensor_link()
    {
        var log = new DecisionLog();
        log.AppendContactChange(Change(1, "c1", "hostile-1", "Unknown", "Detected"));
        log.AppendContactChange(Change(4, "c1", "hostile-1", "Detected", "Lost"));

        var snapshot = SensorToShooterProjection.Project(
            log,
            currentSimTick: 4,
            shooters: new FixedShooterSource(
                new SensorToShooterShooterCandidate(
                    "u1",
                    ScenarioEngageDefaults.MvpFallback,
                    2)));

        var chain = snapshot.Chains[0];
        Assert.That(chain.IsComplete, Is.False);
        Assert.That(chain.PrimaryBreakCause, Is.EqualTo(SensorToShooterBreakCause.LostSensor));

        var sensor = chain.Links.Single(l => l.Kind == SensorToShooterLinkKind.Sensor);
        Assert.That(sensor.IsLinked, Is.False);
        Assert.That(sensor.BreakCause, Is.EqualTo(SensorToShooterBreakCause.LostSensor));
        Assert.That(sensor.CauseLabel, Is.EqualTo(SensorToShooterBreakCauseLabels.LostSensor));
    }

    [Test]
    public void No_eligible_shooter_names_cause_when_targetable_but_engage_blocked()
    {
        var log = new DecisionLog();
        log.AppendContactChange(Change(1, "c1", "hostile-1", "Unknown", "Detected"));
        log.AppendContactChange(Change(5, "c1", "hostile-1", "Detected", "Classified"));
        log.AppendContactChange(Change(9, "c1", "hostile-1", "Classified", "Identified"));

        var noFcDefaults = new ScenarioEngageDefaults(
            rangeMeters: 50_000,
            envelopeMinMeters: 1_000,
            envelopeMaxMeters: 100_000,
            defaultMagazineRounds: 2,
            hasFireControlTrack: false);

        var snapshot = SensorToShooterProjection.Project(
            log,
            currentSimTick: 9,
            fireControl: new StubFireControl("c1"),
            shooters: new FixedShooterSource(
                new SensorToShooterShooterCandidate("u1", noFcDefaults, 2)),
            catalog: InMemoryCatalogReader.BalticPatrolFixture());

        var chain = snapshot.Chains[0];
        Assert.That(chain.IsComplete, Is.False);
        Assert.That(chain.PrimaryBreakCause, Is.EqualTo(SensorToShooterBreakCause.NoEligibleShooter));

        var shooter = chain.Links.Single(l => l.Kind == SensorToShooterLinkKind.EligibleShooter);
        Assert.That(shooter.IsLinked, Is.False);
        Assert.That(shooter.BreakCause, Is.EqualTo(SensorToShooterBreakCause.NoEligibleShooter));
        Assert.That(shooter.CauseLabel, Is.EqualTo(SensorToShooterBreakCauseLabels.NoEligibleShooter));
    }

    [Test]
    public void Fingerprint_is_replay_stable_for_identical_inputs()
    {
        var log = new DecisionLog();
        log.AppendContactChange(Change(1, "c2", "hostile-2", "Unknown", "Detected"));
        log.AppendContactChange(Change(1, "c1", "hostile-1", "Unknown", "Classified"));
        log.AppendContactChange(Change(4, "c1", "hostile-1", "Classified", "Identified"));

        var shooters = new FixedShooterSource(
            new SensorToShooterShooterCandidate("u1", ScenarioEngageDefaults.MvpFallback, 2));
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();
        var fc = new StubFireControl("c1");

        var a = SensorToShooterProjection.Project(log, 4, fc, shooters, catalog);
        var b = SensorToShooterProjection.Project(log, 4, fc, shooters, catalog);

        Assert.That(
            SensorToShooterProjection.ComputeFingerprint(a),
            Is.EqualTo(SensorToShooterProjection.ComputeFingerprint(b)));
        Assert.That(a.Chains.Select(c => c.ContactId), Is.EqualTo(new[] { "c1", "c2" }));
    }

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
