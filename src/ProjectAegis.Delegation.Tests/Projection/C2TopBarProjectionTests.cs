using ProjectAegis.Delegation.Comms;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Sim.Engage;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

public sealed class C2TopBarProjectionTests
{
    [Test]
    public void Project_formats_sim_time_as_hh_mm_ss()
    {
        var state = C2TopBarProjection.Project(
            3661,
            SimulationPhase.Executing,
            "1x",
            "Mixed",
            new DecisionLog());

        Assert.That(state.SimTimeLabel, Is.EqualTo("SIM 01:01:01"));
    }

    [Test]
    public void Project_passes_phase_compression_and_mode_labels()
    {
        var state = C2TopBarProjection.Project(
            0,
            SimulationPhase.Planning,
            "4x",
            "HumanOnly",
            new DecisionLog());

        Assert.That(state.PhaseLabel, Is.EqualTo("PHASE: Planning"));
        Assert.That(state.CompressionLabel, Is.EqualTo("TIME: 4x"));
        Assert.That(state.ModeLabel, Is.EqualTo("MODE: HumanOnly"));
    }

    [Test]
    public void Project_merges_losses_scoring_into_score_line()
    {
        var log = new DecisionLog();
        log.AppendEngagementOutcome(new EngagementOutcomeRecord(
            1, 1, 1, new TargetId("u1"), new TargetId("hostile-1"), 1,
            EngagementOutcomeCodes.Kill, 0.1));

        var state = C2TopBarProjection.Project(
            10,
            SimulationPhase.Executing,
            "1x",
            "Mixed",
            log,
            baseScore: 50);

        Assert.That(state.ScoreLabel, Does.Contain("SCORE: 150"));
        Assert.That(state.ScoreLabel, Does.Contain("KILLS: 1"));
    }

    [Test]
    public void Project_freezes_score_counters_while_planning()
    {
        var log = new DecisionLog();
        log.AppendEngagementOutcome(new EngagementOutcomeRecord(
            1, 1, 1, new TargetId("u1"), new TargetId("hostile-1"), 1,
            EngagementOutcomeCodes.Kill, 0.1));

        var state = C2TopBarProjection.Project(
            10,
            SimulationPhase.Planning,
            "1x",
            "Mixed",
            log,
            baseScore: 50);

        Assert.That(state.ScoreLabel, Is.EqualTo("SCORE: 50  KILLS: 0  MSLS: 0"));
    }

    [Test]
    public void Project_merges_comms_state_into_top_bar_label()
    {
        var log = new DecisionLog();
        log.AppendCommsStateChange(new CommsStateChangeRecord(
            0, 2, 2, "net", CommsState.Nominal, CommsState.Denied, "down"));

        var state = C2TopBarProjection.Project(
            5,
            SimulationPhase.Executing,
            "1x",
            "Mixed",
            log);

        Assert.That(state.CommsLabel, Is.EqualTo("COMMS: DENIED"));
    }
}