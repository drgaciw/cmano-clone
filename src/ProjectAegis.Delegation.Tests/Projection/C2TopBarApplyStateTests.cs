using ProjectAegis.Delegation.Comms;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Sim.Engage;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

/// <summary>S106 — projection → apply-state asserts applied label fields (not style constants only).</summary>
public sealed class C2TopBarApplyStateTests
{
    [Test]
    public void Apply_maps_all_projected_fields_into_presentation()
    {
        var projected = C2TopBarProjection.Project(
            3661,
            SimulationPhase.Executing,
            "2x",
            "Mixed",
            new DecisionLog(),
            baseScore: 10);

        var applied = C2TopBarApplyState.Apply(projected);

        Assert.That(applied.SimTimeLabel, Is.EqualTo(projected.SimTimeLabel));
        Assert.That(applied.PhaseLabel, Is.EqualTo(projected.PhaseLabel));
        Assert.That(applied.CompressionLabel, Is.EqualTo(projected.CompressionLabel));
        Assert.That(applied.ModeLabel, Is.EqualTo(projected.ModeLabel));
        Assert.That(applied.CommsLabel, Is.EqualTo(projected.CommsLabel));
        Assert.That(applied.ScoreLabel, Is.EqualTo(projected.ScoreLabel));
        Assert.That(applied.SimTimeLabel, Is.EqualTo("SIM 01:01:01"));
        Assert.That(applied.PhaseLabel, Is.EqualTo("PHASE: Executing"));
        Assert.That(applied.CompressionLabel, Is.EqualTo("TIME: 2x"));
        Assert.That(applied.ModeLabel, Is.EqualTo("MODE: Mixed"));
    }

    [Test]
    public void ProjectAndApply_includes_live_score_and_comms_css()
    {
        var log = new DecisionLog();
        log.AppendEngagementOutcome(new EngagementOutcomeRecord(
            1, 1, 1, new TargetId("u1"), new TargetId("hostile-1"), 1,
            EngagementOutcomeCodes.Kill, 0.1));
        log.AppendCommsStateChange(new CommsStateChangeRecord(
            0, 2, 2, "net", CommsState.Nominal, CommsState.Denied, "down"));

        var applied = C2TopBarApplyState.ProjectAndApply(
            10,
            SimulationPhase.Executing,
            "1x",
            "Mixed",
            log,
            baseScore: 50);

        Assert.That(applied.ScoreLabel, Does.Contain("SCORE: 150"));
        Assert.That(applied.ScoreLabel, Does.Contain("KILLS: 1"));
        Assert.That(applied.CommsLabel, Is.EqualTo("COMMS: DENIED"));
        Assert.That(applied.CommsCssClass, Is.EqualTo("c2-topbar-item--comms-denied"));
    }

    [Test]
    public void Apply_null_returns_empty_presentation()
    {
        var applied = C2TopBarApplyState.Apply(null);
        Assert.That(applied.SimTimeLabel, Is.Empty);
        Assert.That(applied.ScoreLabel, Is.Empty);
        Assert.That(applied.CommsCssClass, Is.EqualTo("c2-topbar-item--comms-nominal"));
    }

    [Test]
    public void ResolveCommsCssClass_degraded_and_nominal()
    {
        Assert.That(
            C2TopBarApplyState.ResolveCommsCssClass("COMMS: DEGRADED"),
            Is.EqualTo("c2-topbar-item--comms-degraded"));
        Assert.That(
            C2TopBarApplyState.ResolveCommsCssClass("COMMS: NOMINAL"),
            Is.EqualTo("c2-topbar-item--comms-nominal"));
    }

    [Test]
    public void ProjectAndApply_planning_freezes_score_counters()
    {
        var log = new DecisionLog();
        log.AppendEngagementOutcome(new EngagementOutcomeRecord(
            1, 1, 1, new TargetId("u1"), new TargetId("hostile-1"), 1,
            EngagementOutcomeCodes.Kill, 0.1));

        var applied = C2TopBarApplyState.ProjectAndApply(
            10,
            SimulationPhase.Planning,
            "1x",
            "Mixed",
            log,
            baseScore: 50);

        Assert.That(applied.ScoreLabel, Is.EqualTo("SCORE: 50  KILLS: 0  MSLS: 0"));
        Assert.That(applied.PhaseLabel, Is.EqualTo("PHASE: Planning"));
    }
}
