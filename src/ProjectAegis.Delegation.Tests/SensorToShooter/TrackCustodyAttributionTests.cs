using ProjectAegis.Data.Catalog;
using ProjectAegis.Delegation.Comms;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.SensorToShooter;
using ProjectAegis.Delegation.TrackCustody;
using ProjectAegis.Sim.Catalog;
using ProjectAegis.Sim.Core;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.SensorToShooter;

[TestFixture]
public sealed class TrackCustodyAttributionTests
{
    [Test]
    public void Held_track_without_loss_reports_none_breakdown()
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

        var snapshot = SensorToShooterProjection.Project(
            log,
            currentSimTick: 9,
            fireControl: new StubFireControl("c1"),
            shooters: shooters,
            catalog: InMemoryCatalogReader.BalticPatrolFixture());

        Assert.That(snapshot.Chains, Has.Count.EqualTo(1));
        var chain = snapshot.Chains[0];
        Assert.That(chain.IsComplete, Is.True);
        Assert.That(chain.TrackCustodyBreakdown, Is.EqualTo(TrackCustodyBreakdown.None));
        Assert.That(chain.CustodyBreakdown, Is.EqualTo(TrackCustodyBreakdown.None));
        Assert.That(chain.TrackCustodyLossReason, Is.EqualTo(TrackCustodyLossReason.None));
        Assert.That(chain.CustodyLossReason, Is.EqualTo(TrackCustodyLossReason.None));
        Assert.That(chain.TrackCustodyBreakdownLabel, Is.Empty);
    }

    [Test]
    public void Degraded_track_due_to_jamming_reports_jamming_degraded()
    {
        var log = new DecisionLog();
        log.AppendContactChange(Change(1, "c1", "hostile-1", "Unknown", "Classified"));
        log.AppendCommsStateChange(new CommsStateChangeRecord(
            0,
            2.0,
            2,
            "c2-net",
            CommsState.Nominal,
            CommsState.Denied,
            "jamming"));

        var snapshot = SensorToShooterProjection.Project(
            log,
            currentSimTick: 2,
            fireControl: new StubFireControl("c1"),
            shooters: new FixedShooterSource(
                new SensorToShooterShooterCandidate("u1", ScenarioEngageDefaults.MvpFallback, 2)),
            catalog: InMemoryCatalogReader.BalticPatrolFixture());

        Assert.That(snapshot.Chains, Has.Count.EqualTo(1));
        var chain = snapshot.Chains[0];
        Assert.That(chain.TrackCustodyBreakdown, Is.EqualTo(TrackCustodyBreakdown.JammingDegraded));
        Assert.That(chain.TrackCustodyBreakdown, Is.EqualTo(TrackCustodyBreakdown.JammingSjThreshold));
        Assert.That(chain.TrackCustodyLossReason, Is.EqualTo(TrackCustodyLossReason.JammingDegraded));
        Assert.That(chain.TrackCustodyBreakdownLabel, Is.EqualTo(TrackCustodyBreakdownLabels.JammingDegraded));
    }

    [Test]
    public void Lost_track_due_to_horizon_masking_reports_line_of_sight_loss()
    {
        var log = new DecisionLog();
        log.AppendContactChange(Change(1, "c1", "hostile-1", "Unknown", "Detected"));
        log.AppendContactChange(Change(4, "c1", "hostile-1", "Detected", "Lost"));

        var snapshot = SensorToShooterProjection.Project(
            log,
            currentSimTick: 4,
            shooters: new FixedShooterSource(
                new SensorToShooterShooterCandidate("u1", ScenarioEngageDefaults.MvpFallback, 2)));

        Assert.That(snapshot.Chains, Has.Count.EqualTo(1));
        var chain = snapshot.Chains[0];
        Assert.That(chain.IsComplete, Is.False);
        Assert.That(chain.PrimaryBreakCause, Is.EqualTo(SensorToShooterBreakCause.LostSensor));
        Assert.That(chain.TrackCustodyBreakdown, Is.EqualTo(TrackCustodyBreakdown.LineOfSightLoss));
        Assert.That(chain.TrackCustodyBreakdown, Is.EqualTo(TrackCustodyBreakdown.HorizonMasking));
        Assert.That(chain.TrackCustodyLossReason, Is.EqualTo(TrackCustodyLossReason.LineOfSightLoss));
        Assert.That(chain.TrackCustodyBreakdownLabel, Is.EqualTo(TrackCustodyBreakdownLabels.LineOfSightLoss));
    }

    [Test]
    public void Stale_timeout_due_to_loss_of_sight_reports_line_of_sight_loss()
    {
        var log = new DecisionLog();
        log.AppendContactChange(Change(1, "c1", "hostile-1", "Unknown", "Classified"));

        var staleTick = 1UL + (ulong)KillChainContactStateProjection.DefaultStaleThresholdTicks + 1;
        var snapshot = SensorToShooterProjection.Project(
            log,
            currentSimTick: staleTick,
            fireControl: new StubFireControl("c1"),
            shooters: new FixedShooterSource(
                new SensorToShooterShooterCandidate("u1", ScenarioEngageDefaults.MvpFallback, 2)),
            catalog: InMemoryCatalogReader.BalticPatrolFixture());

        Assert.That(snapshot.Chains, Has.Count.EqualTo(1));
        var chain = snapshot.Chains[0];
        Assert.That(chain.IsComplete, Is.False);
        Assert.That(chain.PrimaryBreakCause, Is.EqualTo(SensorToShooterBreakCause.StaleTrack));
        Assert.That(chain.TrackCustodyBreakdown, Is.EqualTo(TrackCustodyBreakdown.LineOfSightLoss));
        Assert.That(chain.TrackCustodyBreakdown, Is.EqualTo(TrackCustodyBreakdown.HorizonMasking));
    }

    [Test]
    public void Platform_destruction_of_target_reports_platform_destroyed()
    {
        var log = new DecisionLog();
        log.AppendContactChange(Change(1, "c1", "hostile-1", "Unknown", "Identified"));
        log.AppendPlatformDamageChange(new PlatformDamageChangeRecord(
            0,
            2,
            2,
            new TargetId("hostile-1"),
            100,
            0,
            PlatformDamageChangeReasonCodes.Kill,
            3));

        var snapshot = SensorToShooterProjection.Project(
            log,
            currentSimTick: 2,
            fireControl: new StubFireControl("c1"),
            shooters: new FixedShooterSource(
                new SensorToShooterShooterCandidate("u1", ScenarioEngageDefaults.MvpFallback, 2)),
            catalog: InMemoryCatalogReader.BalticPatrolFixture());

        Assert.That(snapshot.Chains, Has.Count.EqualTo(1));
        var chain = snapshot.Chains[0];
        Assert.That(chain.IsComplete, Is.False);
        Assert.That(chain.TrackCustodyBreakdown, Is.EqualTo(TrackCustodyBreakdown.PlatformDestroyed));
        Assert.That(chain.TrackCustodyLossReason, Is.EqualTo(TrackCustodyLossReason.PlatformDestroyed));
        Assert.That(chain.TrackCustodyBreakdownLabel, Is.EqualTo(TrackCustodyBreakdownLabels.PlatformDestroyed));
    }

    [Test]
    public void Platform_destruction_of_observer_reports_platform_destroyed()
    {
        var log = new DecisionLog();
        log.AppendContactChange(Change(1, "c1", "hostile-1", "Unknown", "Identified"));
        log.AppendPlatformDamageChange(new PlatformDamageChangeRecord(
            0,
            2,
            2,
            new TargetId("u1"),
            100,
            0,
            PlatformDamageChangeReasonCodes.Kill,
            3));

        var snapshot = SensorToShooterProjection.Project(
            log,
            currentSimTick: 2,
            fireControl: new StubFireControl("c1"),
            shooters: new FixedShooterSource(
                new SensorToShooterShooterCandidate("u1", ScenarioEngageDefaults.MvpFallback, 2)),
            catalog: InMemoryCatalogReader.BalticPatrolFixture());

        Assert.That(snapshot.Chains, Has.Count.EqualTo(1));
        var chain = snapshot.Chains[0];
        Assert.That(chain.TrackCustodyBreakdown, Is.EqualTo(TrackCustodyBreakdown.PlatformDestroyed));
    }

    [Test]
    public void Engagement_outcome_kill_reports_platform_destroyed()
    {
        var log = new DecisionLog();
        log.AppendContactChange(Change(1, "c1", "hostile-1", "Unknown", "Identified"));
        log.AppendEngagementOutcome(new EngagementOutcomeRecord(
            0,
            2.0,
            2,
            new TargetId("u1"),
            new TargetId("hostile-1"),
            1,
            EngagementOutcomeCodes.Kill,
            0.0));

        var snapshot = SensorToShooterProjection.Project(
            log,
            currentSimTick: 2,
            fireControl: new StubFireControl("c1"),
            shooters: new FixedShooterSource(
                new SensorToShooterShooterCandidate("u1", ScenarioEngageDefaults.MvpFallback, 2)),
            catalog: InMemoryCatalogReader.BalticPatrolFixture());

        Assert.That(snapshot.Chains, Has.Count.EqualTo(1));
        var chain = snapshot.Chains[0];
        Assert.That(chain.TrackCustodyBreakdown, Is.EqualTo(TrackCustodyBreakdown.PlatformDestroyed));
    }

    [Test]
    public void Downstream_break_with_intact_custody_reports_breakdown_none()
    {
        var log = new DecisionLog();
        log.AppendContactChange(Change(1, "c1", "hostile-1", "Unknown", "Detected"));
        log.AppendContactChange(Change(5, "c1", "hostile-1", "Detected", "Classified"));
        log.AppendContactChange(Change(9, "c1", "hostile-1", "Classified", "Identified"));

        var snapshot = SensorToShooterProjection.Project(
            log,
            currentSimTick: 9,
            fireControl: null, // Break targetability with NoFireControl
            shooters: new FixedShooterSource(
                new SensorToShooterShooterCandidate("u1", ScenarioEngageDefaults.MvpFallback, 2)),
            catalog: InMemoryCatalogReader.BalticPatrolFixture());

        var chain = snapshot.Chains[0];
        Assert.That(chain.IsComplete, Is.False);
        Assert.That(chain.PrimaryBreakCause, Is.EqualTo(SensorToShooterBreakCause.NoFireControl));
        Assert.That(chain.TrackCustodyBreakdown, Is.EqualTo(TrackCustodyBreakdown.None));
    }

    [Test]
    public void Track_custody_projection_rows_and_ledger_populate_breakdown()
    {
        var log = new DecisionLog();
        log.AppendContactChange(Change(1, "c1", "hostile-1", "Unknown", "Classified"));
        log.AppendContactChange(Change(4, "c1", "hostile-1", "Classified", "Lost"));

        var snapshot = TrackCustodyProjection.Project(log, currentSimTick: 4);

        Assert.That(snapshot.Rows, Has.Count.EqualTo(1));
        var row = snapshot.Rows[0];
        Assert.That(row.Custody, Is.EqualTo(TrackCustodyState.Dropped));
        Assert.That(row.Breakdown, Is.EqualTo(TrackCustodyBreakdown.LineOfSightLoss));
        Assert.That(row.TrackCustodyBreakdown, Is.EqualTo(TrackCustodyBreakdown.HorizonMasking));
        Assert.That(row.LossReason, Is.EqualTo(TrackCustodyLossReason.LineOfSightLoss));
        Assert.That(row.BreakdownLabel, Is.EqualTo(TrackCustodyBreakdownLabels.LineOfSightLoss));

        var entry = snapshot.Entries.Single(e => e.Custody == TrackCustodyState.Dropped);
        Assert.That(entry.Breakdown, Is.EqualTo(TrackCustodyBreakdown.LineOfSightLoss));
        Assert.That(entry.BreakdownLabel, Is.EqualTo(TrackCustodyBreakdownLabels.LineOfSightLoss));
    }

    [Test]
    public void Fingerprint_is_replay_stable_with_custody_breakdown()
    {
        var log = new DecisionLog();
        log.AppendContactChange(Change(1, "c1", "hostile-1", "Unknown", "Classified"));
        log.AppendCommsStateChange(new CommsStateChangeRecord(
            0,
            2.0,
            2,
            "c2-net",
            CommsState.Nominal,
            CommsState.Denied,
            "jamming"));

        var shooters = new FixedShooterSource(
            new SensorToShooterShooterCandidate("u1", ScenarioEngageDefaults.MvpFallback, 2));
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();

        var a = SensorToShooterProjection.Project(log, 2, new StubFireControl("c1"), shooters, catalog);
        var b = SensorToShooterProjection.Project(log, 2, new StubFireControl("c1"), shooters, catalog);

        Assert.That(
            SensorToShooterProjection.ComputeFingerprint(a),
            Is.EqualTo(SensorToShooterProjection.ComputeFingerprint(b)));
        Assert.That(a.Chains[0].TrackCustodyBreakdown, Is.EqualTo(TrackCustodyBreakdown.JammingDegraded));
    }

    [Test]
    public void Enum_aliases_and_labels_match_specification()
    {
        Assert.That((int)TrackCustodyBreakdown.None, Is.EqualTo(0));
        Assert.That((int)TrackCustodyBreakdown.JammingDegraded, Is.EqualTo(1));
        Assert.That((int)TrackCustodyBreakdown.JammingSjThreshold, Is.EqualTo(1));
        Assert.That((int)TrackCustodyBreakdown.LineOfSightLoss, Is.EqualTo(2));
        Assert.That((int)TrackCustodyBreakdown.HorizonMasking, Is.EqualTo(2));
        Assert.That((int)TrackCustodyBreakdown.PlatformDestroyed, Is.EqualTo(3));

        Assert.That((int)TrackCustodyLossReason.JammingDegraded, Is.EqualTo(1));
        Assert.That((int)TrackCustodyLossReason.LineOfSightLoss, Is.EqualTo(2));
        Assert.That((int)TrackCustodyLossReason.PlatformDestroyed, Is.EqualTo(3));

        Assert.That(TrackCustodyBreakdownLabels.Format(TrackCustodyBreakdown.JammingDegraded), Is.EqualTo("jamming"));
        Assert.That(TrackCustodyBreakdownLabels.Format(TrackCustodyBreakdown.LineOfSightLoss), Is.EqualTo("line of sight loss"));
        Assert.That(TrackCustodyBreakdownLabels.Format(TrackCustodyBreakdown.PlatformDestroyed), Is.EqualTo("platform destroyed"));
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
