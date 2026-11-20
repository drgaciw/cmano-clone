using ProjectAegis.Data.Catalog;
using ProjectAegis.Sim.Core;
using ProjectAegis.Sim.Swarm;
using ProjectAegis.Sim.Swarm.SoftKill;
using NUnit.Framework;

namespace ProjectAegis.Sim.Tests.Swarm.SoftKill;

/// <summary>DRG-107 / SWARM-C3: EMP/jam soft-kill effects (SWARM-18).</summary>
public sealed class SwarmSoftKillTests
{
    private static SwarmUnitIntegrity Sample(string id = "swarm-1") =>
        new(id, CatalogSwarmPlatformDefaults.GenericSwarmPlatformId, 40, 40);

    private static (SwarmController Controller, SwarmSoftKillApplicator SoftKill) Create()
    {
        var c = new SwarmController(SimSeed.FromScenario(107));
        c.Register(Sample(), 57.0, 20.0);
        return (c, new SwarmSoftKillApplicator(c));
    }

    [Test]
    public void Emp_evaluator_freeze_until_is_deterministic()
    {
        Assert.That(SwarmEmpEvaluator.ComputeFreezeUntil(10.0, 30.0), Is.EqualTo(40.0));
        Assert.That(SwarmEmpEvaluator.ComputeFreezeUntil(10.0, 0), Is.EqualTo(10.0));
        Assert.That(SwarmEmpEvaluator.IsModeFrozen(10.0, freezeUntilSimTime: 40.0), Is.True);
        Assert.That(SwarmEmpEvaluator.IsModeFrozen(40.0, freezeUntilSimTime: 40.0), Is.False);
        Assert.That(SwarmEmpEvaluator.IsModeFrozen(41.0, freezeUntilSimTime: 40.0), Is.False);
        Assert.That(SwarmEmpEvaluator.MergeFreezeUntil(40.0, 50.0), Is.EqualTo(50.0));
        Assert.That(SwarmEmpEvaluator.MergeFreezeUntil(40.0, 30.0), Is.EqualTo(40.0));
    }

    [Test]
    public void Jam_evaluator_maps_severity_to_link_state()
    {
        Assert.That(SwarmJamEvaluator.LinkStateForSeverity(SwarmJamSeverity.None), Is.EqualTo(SwarmLinkState.Connected));
        Assert.That(SwarmJamEvaluator.LinkStateForSeverity(SwarmJamSeverity.Degraded), Is.EqualTo(SwarmLinkState.Degraded));
        Assert.That(SwarmJamEvaluator.LinkStateForSeverity(SwarmJamSeverity.Lost), Is.EqualTo(SwarmLinkState.Lost));
        Assert.That(SwarmJamEvaluator.ReasonForSeverity(SwarmJamSeverity.Degraded), Is.EqualTo(SwarmJamEvaluator.ReasonDegraded));
        Assert.That(SwarmJamEvaluator.ReasonForSeverity(SwarmJamSeverity.Lost), Is.EqualTo(SwarmJamEvaluator.ReasonLost));
    }

    [Test]
    public void Emp_freezes_mode_switches_until_expiry()
    {
        var (c, sk) = Create();
        c.IssueMode("swarm-1", SwarmOperationalMode.Assault, simTick: 1, simTime: 0.0);

        Assert.That(
            sk.ApplyEmp(
                    "swarm-1",
                    simTick: 2,
                    simTime: 10.0,
                    freezeDurationSeconds: 30.0,
                    recommendScatter: true),
            Is.True);

        Assert.That(c.GetMode("swarm-1"), Is.EqualTo(SwarmOperationalMode.Scatter));
        Assert.That(sk.IsModeFrozen("swarm-1", simTime: 10.0), Is.True);
        Assert.That(sk.IsModeFrozen("swarm-1", simTime: 39.9), Is.True);
        Assert.That(sk.GetModeFreezeUntil("swarm-1"), Is.EqualTo(40.0));

        // Subsequent mode change blocked while frozen.
        Assert.That(
            sk.TryIssueMode(
                    "swarm-1",
                    SwarmOperationalMode.Assault,
                    simTick: 3,
                    simTime: 20.0,
                    out var reject),
            Is.False);
        Assert.That(reject, Is.EqualTo(SwarmEmpEvaluator.ReasonModeBlocked));
        Assert.That(c.GetMode("swarm-1"), Is.EqualTo(SwarmOperationalMode.Scatter));

        // At exact expiry boundary, freeze lifts (exclusive until).
        Assert.That(sk.IsModeFrozen("swarm-1", simTime: 40.0), Is.False);
        Assert.That(
            sk.TryIssueMode(
                    "swarm-1",
                    SwarmOperationalMode.Rejoin,
                    simTick: 4,
                    simTime: 40.0,
                    out var rejectAfter),
            Is.True);
        Assert.That(rejectAfter, Is.Null);
        Assert.That(c.GetMode("swarm-1"), Is.EqualTo(SwarmOperationalMode.Rejoin));
    }

    [Test]
    public void Emp_blocks_subsequent_mode_change_until_expiry_event_log()
    {
        var (c, sk) = Create();
        Assert.That(sk.ApplyEmp("swarm-1", 1, 5.0, freezeDurationSeconds: 10.0, recommendScatter: false), Is.True);

        Assert.That(sk.TryIssueMode("swarm-1", SwarmOperationalMode.Screen, 2, 5.0, out _), Is.False);
        Assert.That(sk.TryIssueMode("swarm-1", SwarmOperationalMode.Screen, 3, 14.9, out _), Is.False);
        Assert.That(sk.TryIssueMode("swarm-1", SwarmOperationalMode.Screen, 4, 15.0, out _), Is.True);
        Assert.That(c.GetMode("swarm-1"), Is.EqualTo(SwarmOperationalMode.Screen));

        Assert.That(sk.EventLog.Any(e => e.Reason == SwarmEmpEvaluator.ReasonModeFreeze), Is.True);
        Assert.That(sk.EventLog.Any(e => e.Reason == SwarmEmpEvaluator.ReasonModeBlocked), Is.True);
        AssertEx.All(sk.EventLog, e => Assert.That(string.IsNullOrWhiteSpace(e.Reason), Is.False));
    }

    [Test]
    public void Emp_recommend_scatter_logs_explicit_reason()
    {
        var (c, sk) = Create();
        c.IssueMode("swarm-1", SwarmOperationalMode.Assault, 1, 0);

        Assert.That(sk.ApplyEmp("swarm-1", 2, 1.0, freezeDurationSeconds: 5.0, recommendScatter: true), Is.True);

        Assert.That(c.GetMode("swarm-1"), Is.EqualTo(SwarmOperationalMode.Scatter));
        Assert.That(sk.EventLog.Any(e => e.Reason == SwarmEmpEvaluator.ReasonRecommendScatter), Is.True);
        Assert.That(c.ModeOrderLog.Any(m => m.Mode == SwarmOperationalMode.Scatter), Is.True);
    }

    [Test]
    public void Emp_merge_extends_freeze_window()
    {
        var (_, sk) = Create();
        Assert.That(sk.ApplyEmp("swarm-1", 1, 0.0, freezeDurationSeconds: 10.0, recommendScatter: false), Is.True);
        Assert.That(sk.GetModeFreezeUntil("swarm-1"), Is.EqualTo(10.0));

        Assert.That(sk.ApplyEmp("swarm-1", 2, 5.0, freezeDurationSeconds: 20.0, recommendScatter: false), Is.True);
        Assert.That(sk.GetModeFreezeUntil("swarm-1"), Is.EqualTo(25.0));
        Assert.That(sk.IsModeFrozen("swarm-1", 24.9), Is.True);
    }

    [Test]
    public void Jam_sets_degraded_link_state()
    {
        var (c, sk) = Create();
        Assert.That(c.GetLinkState("swarm-1"), Is.EqualTo(SwarmLinkState.Connected));

        Assert.That(sk.ApplyJam("swarm-1", 1, 1.0, SwarmJamSeverity.Degraded), Is.True);

        Assert.That(c.GetLinkState("swarm-1"), Is.EqualTo(SwarmLinkState.Degraded));
        Assert.That(
            sk.EventLog.Any(e =>
                    e.Kind == SwarmSoftKillKind.Jam && e.Reason == SwarmJamEvaluator.ReasonDegraded),
            Is.True);
    }

    [Test]
    public void Jam_sets_lost_at_higher_severity()
    {
        var (c, sk) = Create();

        Assert.That(sk.ApplyJam("swarm-1", 1, 1.0, SwarmJamSeverity.Lost), Is.True);

        Assert.That(c.GetLinkState("swarm-1"), Is.EqualTo(SwarmLinkState.Lost));
        Assert.That(
            sk.EventLog.Any(e =>
                    e.Kind == SwarmSoftKillKind.Jam && e.Reason == SwarmJamEvaluator.ReasonLost),
            Is.True);

        // Lost link still blocks controller IssueMode (SWARM-12).
        Assert.Throws<InvalidOperationException>(() =>
            c.IssueMode("swarm-1", SwarmOperationalMode.Scatter, 2, 2.0));
    }

    [Test]
    public void Jam_recovery_after_clear_restores_connected()
    {
        var (c, sk) = Create();
        Assert.That(sk.ApplyJam("swarm-1", 1, 1.0, SwarmJamSeverity.Degraded), Is.True);
        Assert.That(c.GetLinkState("swarm-1"), Is.EqualTo(SwarmLinkState.Degraded));

        Assert.That(sk.ClearJam("swarm-1", 2, 2.0), Is.True);
        Assert.That(c.GetLinkState("swarm-1"), Is.EqualTo(SwarmLinkState.Connected));
        Assert.That(
            sk.EventLog.Any(e =>
                    e.Kind == SwarmSoftKillKind.ClearJam && e.Reason == SwarmJamEvaluator.ReasonClear),
            Is.True);

        // After clear, mode orders accepted again.
        Assert.That(sk.TryIssueMode("swarm-1", SwarmOperationalMode.Assault, 3, 3.0, out _), Is.True);
        Assert.That(c.GetMode("swarm-1"), Is.EqualTo(SwarmOperationalMode.Assault));
    }

    [Test]
    public void Jam_recovery_from_lost_restores_orders()
    {
        var (c, sk) = Create();
        Assert.That(sk.ApplyJam("swarm-1", 1, 1.0, SwarmJamSeverity.Lost), Is.True);
        Assert.That(c.GetLinkState("swarm-1"), Is.EqualTo(SwarmLinkState.Lost));

        Assert.That(sk.ClearJam("swarm-1", 2, 5.0), Is.True);
        Assert.That(c.GetLinkState("swarm-1"), Is.EqualTo(SwarmLinkState.Connected));
        c.IssueMode("swarm-1", SwarmOperationalMode.Hold, 3, 5.0);
        Assert.That(c.GetMode("swarm-1"), Is.EqualTo(SwarmOperationalMode.Hold));
    }

    [Test]
    public void Emp_clear_allows_mode_change_before_natural_expiry()
    {
        var (c, sk) = Create();
        Assert.That(sk.ApplyEmp("swarm-1", 1, 0.0, freezeDurationSeconds: 100.0, recommendScatter: false), Is.True);
        Assert.That(sk.TryIssueMode("swarm-1", SwarmOperationalMode.Assault, 2, 10.0, out _), Is.False);

        Assert.That(sk.ClearEmpFreeze("swarm-1", 3, 10.0), Is.True);
        Assert.That(sk.IsModeFrozen("swarm-1", 10.0), Is.False);
        Assert.That(sk.TryIssueMode("swarm-1", SwarmOperationalMode.Assault, 4, 10.0, out _), Is.True);
        Assert.That(c.GetMode("swarm-1"), Is.EqualTo(SwarmOperationalMode.Assault));
        Assert.That(sk.EventLog.Any(e => e.Reason == SwarmEmpEvaluator.ReasonClear), Is.True);
    }

    [Test]
    public void Soft_kill_unknown_unit_fails_closed()
    {
        var (_, sk) = Create();
        Assert.That(sk.ApplyEmp("missing", 1, 0.0), Is.False);
        Assert.That(sk.ApplyJam("missing", 1, 0.0, SwarmJamSeverity.Degraded), Is.False);
        Assert.That(sk.ClearJam("missing", 1, 0.0), Is.False);
        Assert.That(sk.ClearEmpFreeze("missing", 1, 0.0), Is.False);
        Assert.That(sk.TryIssueMode("missing", SwarmOperationalMode.Hold, 1, 0.0, out var reason), Is.False);
        Assert.That(reason, Is.EqualTo("unknown-unit"));
        Assert.That(sk.EventLog, Is.Empty);
    }

    [Test]
    public void Same_seed_path_is_deterministic()
    {
        static List<string> Run()
        {
            var c = new SwarmController(SimSeed.FromScenario(107));
            c.Register(Sample(), 57.0, 20.0);
            var sk = new SwarmSoftKillApplicator(c);
            sk.ApplyEmp("swarm-1", 1, 0.0, 15.0, recommendScatter: true);
            sk.TryIssueMode("swarm-1", SwarmOperationalMode.Assault, 2, 5.0, out _);
            sk.ApplyJam("swarm-1", 3, 6.0, SwarmJamSeverity.Degraded);
            sk.ClearJam("swarm-1", 4, 7.0);
            sk.TryIssueMode("swarm-1", SwarmOperationalMode.Rejoin, 5, 16.0, out _);
            return sk.EventLog.Select(e => $"{e.SequenceId}:{e.Kind}:{e.Reason}:{e.SimTime}").ToList();
        }

        Assert.That(Run(), Is.EqualTo(Run()));
    }

    [Test]
    public void ClearJam_can_refresh_from_geometry()
    {
        var (c, sk) = Create();
        c.BindHost("swarm-1", "host-1");
        c.PublishHostState("host-1", latDeg: 57.0, lonDeg: 20.2, alive: true);
        Assert.That(sk.ApplyJam("swarm-1", 1, 1.0, SwarmJamSeverity.Lost), Is.True);
        Assert.That(c.GetLinkState("swarm-1"), Is.EqualTo(SwarmLinkState.Lost));

        Assert.That(sk.ClearJam("swarm-1", 2, 2.0, refreshFromGeometry: true, jammed: false), Is.True);
        // Host ~0.2 deg away → Connected under default link bands.
        Assert.That(c.GetLinkState("swarm-1"), Is.EqualTo(SwarmLinkState.Connected));
    }
}
