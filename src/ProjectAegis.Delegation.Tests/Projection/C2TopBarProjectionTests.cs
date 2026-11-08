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

    // --- CMD-22: Zulu / local / remaining duration ---

    [Test]
    public void Project_zulu_and_local_from_scenario_epoch_with_offset()
    {
        // Epoch 12:00:00Z + 2h32m5s sim = 14:32:05 Zulu; local offset -7h → 07:32:05
        var epoch = new DateTimeOffset(2026, 8, 1, 12, 0, 0, TimeSpan.Zero);
        var simSeconds = 2 * 3600 + 32 * 60 + 5;

        var state = C2TopBarProjection.Project(
            simSeconds,
            SimulationPhase.Executing,
            "1x",
            "Mixed",
            new DecisionLog(),
            scenarioEpochUtc: epoch,
            localUtcOffsetHours: -7);

        Assert.That(state.ZuluTimeLabel, Is.EqualTo("ZULU 14:32:05"));
        Assert.That(state.LocalTimeLabel, Is.EqualTo("LOCAL 07:32:05"));
        Assert.That(state.SimTimeLabel, Is.EqualTo("SIM 02:32:05"));
    }

    [Test]
    public void Project_zulu_from_sim_midnight_when_epoch_null()
    {
        var state = C2TopBarProjection.Project(
            3661,
            SimulationPhase.Executing,
            "1x",
            "Mixed",
            new DecisionLog());

        Assert.That(state.ZuluTimeLabel, Is.EqualTo("ZULU 01:01:01"));
        Assert.That(state.LocalTimeLabel, Is.EqualTo("LOCAL 01:01:01"));
    }

    [Test]
    public void Project_local_applies_positive_offset()
    {
        var epoch = new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);

        var state = C2TopBarProjection.Project(
            0,
            SimulationPhase.Executing,
            "1x",
            "Mixed",
            new DecisionLog(),
            scenarioEpochUtc: epoch,
            localUtcOffsetHours: 5);

        Assert.That(state.ZuluTimeLabel, Is.EqualTo("ZULU 10:00:00"));
        Assert.That(state.LocalTimeLabel, Is.EqualTo("LOCAL 15:00:00"));
    }

    [Test]
    public void Project_remaining_counts_down_from_duration()
    {
        // Duration 2h, sim at 45m → remain 01:15:00
        var state = C2TopBarProjection.Project(
            45 * 60,
            SimulationPhase.Executing,
            "1x",
            "Mixed",
            new DecisionLog(),
            scenarioDurationSeconds: 2 * 3600);

        Assert.That(state.RemainingDurationLabel, Is.EqualTo("REMAIN 01:15:00"));
    }

    [Test]
    public void Project_remaining_clamps_at_zero_when_overrun()
    {
        var state = C2TopBarProjection.Project(
            5000,
            SimulationPhase.Executing,
            "1x",
            "Mixed",
            new DecisionLog(),
            scenarioDurationSeconds: 3600);

        Assert.That(state.RemainingDurationLabel, Is.EqualTo("REMAIN 00:00:00"));
    }

    [Test]
    public void Project_null_duration_emits_remain_em_dash()
    {
        var state = C2TopBarProjection.Project(
            100,
            SimulationPhase.Executing,
            "1x",
            "Mixed",
            new DecisionLog());

        Assert.That(state.RemainingDurationLabel, Is.EqualTo("REMAIN —"));
    }
}
