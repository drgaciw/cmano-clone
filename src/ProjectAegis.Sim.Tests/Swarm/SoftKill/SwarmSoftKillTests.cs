using ProjectAegis.Data.Catalog;
using ProjectAegis.Sim.Core;
using ProjectAegis.Sim.Swarm;
using ProjectAegis.Sim.Swarm.SoftKill;
using Xunit;

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

    [Fact]
    public void Emp_evaluator_freeze_until_is_deterministic()
    {
        Assert.Equal(40.0, SwarmEmpEvaluator.ComputeFreezeUntil(10.0, 30.0));
        Assert.Equal(10.0, SwarmEmpEvaluator.ComputeFreezeUntil(10.0, 0));
        Assert.True(SwarmEmpEvaluator.IsModeFrozen(10.0, freezeUntilSimTime: 40.0));
        Assert.False(SwarmEmpEvaluator.IsModeFrozen(40.0, freezeUntilSimTime: 40.0));
        Assert.False(SwarmEmpEvaluator.IsModeFrozen(41.0, freezeUntilSimTime: 40.0));
        Assert.Equal(50.0, SwarmEmpEvaluator.MergeFreezeUntil(40.0, 50.0));
        Assert.Equal(40.0, SwarmEmpEvaluator.MergeFreezeUntil(40.0, 30.0));
    }

    [Fact]
    public void Jam_evaluator_maps_severity_to_link_state()
    {
        Assert.Equal(SwarmLinkState.Connected, SwarmJamEvaluator.LinkStateForSeverity(SwarmJamSeverity.None));
        Assert.Equal(SwarmLinkState.Degraded, SwarmJamEvaluator.LinkStateForSeverity(SwarmJamSeverity.Degraded));
        Assert.Equal(SwarmLinkState.Lost, SwarmJamEvaluator.LinkStateForSeverity(SwarmJamSeverity.Lost));
        Assert.Equal(SwarmJamEvaluator.ReasonDegraded, SwarmJamEvaluator.ReasonForSeverity(SwarmJamSeverity.Degraded));
        Assert.Equal(SwarmJamEvaluator.ReasonLost, SwarmJamEvaluator.ReasonForSeverity(SwarmJamSeverity.Lost));
    }

    [Fact]
    public void Emp_freezes_mode_switches_until_expiry()
    {
        var (c, sk) = Create();
        c.IssueMode("swarm-1", SwarmOperationalMode.Assault, simTick: 1, simTime: 0.0);

        Assert.True(sk.ApplyEmp(
            "swarm-1",
            simTick: 2,
            simTime: 10.0,
            freezeDurationSeconds: 30.0,
            recommendScatter: true));

        Assert.Equal(SwarmOperationalMode.Scatter, c.GetMode("swarm-1"));
        Assert.True(sk.IsModeFrozen("swarm-1", simTime: 10.0));
        Assert.True(sk.IsModeFrozen("swarm-1", simTime: 39.9));
        Assert.Equal(40.0, sk.GetModeFreezeUntil("swarm-1"));

        // Subsequent mode change blocked while frozen.
        Assert.False(sk.TryIssueMode(
            "swarm-1",
            SwarmOperationalMode.Assault,
            simTick: 3,
            simTime: 20.0,
            out var reject));
        Assert.Equal(SwarmEmpEvaluator.ReasonModeBlocked, reject);
        Assert.Equal(SwarmOperationalMode.Scatter, c.GetMode("swarm-1"));

        // At exact expiry boundary, freeze lifts (exclusive until).
        Assert.False(sk.IsModeFrozen("swarm-1", simTime: 40.0));
        Assert.True(sk.TryIssueMode(
            "swarm-1",
            SwarmOperationalMode.Rejoin,
            simTick: 4,
            simTime: 40.0,
            out var rejectAfter));
        Assert.Null(rejectAfter);
        Assert.Equal(SwarmOperationalMode.Rejoin, c.GetMode("swarm-1"));
    }

    [Fact]
    public void Emp_blocks_subsequent_mode_change_until_expiry_event_log()
    {
        var (c, sk) = Create();
        Assert.True(sk.ApplyEmp("swarm-1", 1, 5.0, freezeDurationSeconds: 10.0, recommendScatter: false));

        Assert.False(sk.TryIssueMode("swarm-1", SwarmOperationalMode.Screen, 2, 5.0, out _));
        Assert.False(sk.TryIssueMode("swarm-1", SwarmOperationalMode.Screen, 3, 14.9, out _));
        Assert.True(sk.TryIssueMode("swarm-1", SwarmOperationalMode.Screen, 4, 15.0, out _));
        Assert.Equal(SwarmOperationalMode.Screen, c.GetMode("swarm-1"));

        Assert.Contains(sk.EventLog, e => e.Reason == SwarmEmpEvaluator.ReasonModeFreeze);
        Assert.Contains(sk.EventLog, e => e.Reason == SwarmEmpEvaluator.ReasonModeBlocked);
        Assert.All(sk.EventLog, e => Assert.False(string.IsNullOrWhiteSpace(e.Reason)));
    }

    [Fact]
    public void Emp_recommend_scatter_logs_explicit_reason()
    {
        var (c, sk) = Create();
        c.IssueMode("swarm-1", SwarmOperationalMode.Assault, 1, 0);

        Assert.True(sk.ApplyEmp("swarm-1", 2, 1.0, freezeDurationSeconds: 5.0, recommendScatter: true));

        Assert.Equal(SwarmOperationalMode.Scatter, c.GetMode("swarm-1"));
        Assert.Contains(sk.EventLog, e => e.Reason == SwarmEmpEvaluator.ReasonRecommendScatter);
        Assert.Contains(c.ModeOrderLog, m => m.Mode == SwarmOperationalMode.Scatter);
    }

    [Fact]
    public void Emp_merge_extends_freeze_window()
    {
        var (_, sk) = Create();
        Assert.True(sk.ApplyEmp("swarm-1", 1, 0.0, freezeDurationSeconds: 10.0, recommendScatter: false));
        Assert.Equal(10.0, sk.GetModeFreezeUntil("swarm-1"));

        Assert.True(sk.ApplyEmp("swarm-1", 2, 5.0, freezeDurationSeconds: 20.0, recommendScatter: false));
        Assert.Equal(25.0, sk.GetModeFreezeUntil("swarm-1"));
        Assert.True(sk.IsModeFrozen("swarm-1", 24.9));
    }

    [Fact]
    public void Jam_sets_degraded_link_state()
    {
        var (c, sk) = Create();
        Assert.Equal(SwarmLinkState.Connected, c.GetLinkState("swarm-1"));

        Assert.True(sk.ApplyJam("swarm-1", 1, 1.0, SwarmJamSeverity.Degraded));

        Assert.Equal(SwarmLinkState.Degraded, c.GetLinkState("swarm-1"));
        Assert.Contains(sk.EventLog, e =>
            e.Kind == SwarmSoftKillKind.Jam && e.Reason == SwarmJamEvaluator.ReasonDegraded);
    }

    [Fact]
    public void Jam_sets_lost_at_higher_severity()
    {
        var (c, sk) = Create();

        Assert.True(sk.ApplyJam("swarm-1", 1, 1.0, SwarmJamSeverity.Lost));

        Assert.Equal(SwarmLinkState.Lost, c.GetLinkState("swarm-1"));
        Assert.Contains(sk.EventLog, e =>
            e.Kind == SwarmSoftKillKind.Jam && e.Reason == SwarmJamEvaluator.ReasonLost);

        // Lost link still blocks controller IssueMode (SWARM-12).
        Assert.Throws<InvalidOperationException>(() =>
            c.IssueMode("swarm-1", SwarmOperationalMode.Scatter, 2, 2.0));
    }

    [Fact]
    public void Jam_recovery_after_clear_restores_connected()
    {
        var (c, sk) = Create();
        Assert.True(sk.ApplyJam("swarm-1", 1, 1.0, SwarmJamSeverity.Degraded));
        Assert.Equal(SwarmLinkState.Degraded, c.GetLinkState("swarm-1"));

        Assert.True(sk.ClearJam("swarm-1", 2, 2.0));
        Assert.Equal(SwarmLinkState.Connected, c.GetLinkState("swarm-1"));
        Assert.Contains(sk.EventLog, e =>
            e.Kind == SwarmSoftKillKind.ClearJam && e.Reason == SwarmJamEvaluator.ReasonClear);

        // After clear, mode orders accepted again.
        Assert.True(sk.TryIssueMode("swarm-1", SwarmOperationalMode.Assault, 3, 3.0, out _));
        Assert.Equal(SwarmOperationalMode.Assault, c.GetMode("swarm-1"));
    }

    [Fact]
    public void Jam_recovery_from_lost_restores_orders()
    {
        var (c, sk) = Create();
        Assert.True(sk.ApplyJam("swarm-1", 1, 1.0, SwarmJamSeverity.Lost));
        Assert.Equal(SwarmLinkState.Lost, c.GetLinkState("swarm-1"));

        Assert.True(sk.ClearJam("swarm-1", 2, 5.0));
        Assert.Equal(SwarmLinkState.Connected, c.GetLinkState("swarm-1"));
        c.IssueMode("swarm-1", SwarmOperationalMode.Hold, 3, 5.0);
        Assert.Equal(SwarmOperationalMode.Hold, c.GetMode("swarm-1"));
    }

    [Fact]
    public void Emp_clear_allows_mode_change_before_natural_expiry()
    {
        var (c, sk) = Create();
        Assert.True(sk.ApplyEmp("swarm-1", 1, 0.0, freezeDurationSeconds: 100.0, recommendScatter: false));
        Assert.False(sk.TryIssueMode("swarm-1", SwarmOperationalMode.Assault, 2, 10.0, out _));

        Assert.True(sk.ClearEmpFreeze("swarm-1", 3, 10.0));
        Assert.False(sk.IsModeFrozen("swarm-1", 10.0));
        Assert.True(sk.TryIssueMode("swarm-1", SwarmOperationalMode.Assault, 4, 10.0, out _));
        Assert.Equal(SwarmOperationalMode.Assault, c.GetMode("swarm-1"));
        Assert.Contains(sk.EventLog, e => e.Reason == SwarmEmpEvaluator.ReasonClear);
    }

    [Fact]
    public void Soft_kill_unknown_unit_fails_closed()
    {
        var (_, sk) = Create();
        Assert.False(sk.ApplyEmp("missing", 1, 0.0));
        Assert.False(sk.ApplyJam("missing", 1, 0.0, SwarmJamSeverity.Degraded));
        Assert.False(sk.ClearJam("missing", 1, 0.0));
        Assert.False(sk.ClearEmpFreeze("missing", 1, 0.0));
        Assert.False(sk.TryIssueMode("missing", SwarmOperationalMode.Hold, 1, 0.0, out var reason));
        Assert.Equal("unknown-unit", reason);
        Assert.Empty(sk.EventLog);
    }

    [Fact]
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

        Assert.Equal(Run(), Run());
    }

    [Fact]
    public void ClearJam_can_refresh_from_geometry()
    {
        var (c, sk) = Create();
        c.BindHost("swarm-1", "host-1");
        c.PublishHostState("host-1", latDeg: 57.0, lonDeg: 20.2, alive: true);
        Assert.True(sk.ApplyJam("swarm-1", 1, 1.0, SwarmJamSeverity.Lost));
        Assert.Equal(SwarmLinkState.Lost, c.GetLinkState("swarm-1"));

        Assert.True(sk.ClearJam("swarm-1", 2, 2.0, refreshFromGeometry: true, jammed: false));
        // Host ~0.2 deg away → Connected under default link bands.
        Assert.Equal(SwarmLinkState.Connected, c.GetLinkState("swarm-1"));
    }
}
