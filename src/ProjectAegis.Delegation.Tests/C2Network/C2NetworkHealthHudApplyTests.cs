using ProjectAegis.Delegation.C2Network;
using ProjectAegis.Delegation.Comms;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.C2Network;

/// <summary>S122-06 / DRG-190 — TopBar network-health text (not a sim owner; no hex color encoding).</summary>
[TestFixture]
public sealed class C2NetworkHealthHudApplyTests
{
    [Test]
    public void Format_healthy_uses_stable_net_prefix()
    {
        var line = C2NetworkHealthHudFormat.Format(C2NetworkHealthLevel.Healthy);

        Assert.That(line, Is.EqualTo("NET: HEALTHY"));
        Assert.That(line, Does.StartWith("NET:"));
        AssertNoHexColor(line);
    }

    [Test]
    public void Format_degraded_uses_stable_net_prefix()
    {
        var line = C2NetworkHealthHudFormat.Format(C2NetworkHealthLevel.Degraded);

        Assert.That(line, Is.EqualTo("NET: DEGRADED"));
        Assert.That(line, Does.StartWith("NET:"));
        AssertNoHexColor(line);
    }

    [Test]
    public void Format_partitioned_uses_stable_net_prefix()
    {
        var line = C2NetworkHealthHudFormat.Format(C2NetworkHealthLevel.Partitioned);

        Assert.That(line, Is.EqualTo("NET: PARTITIONED"));
        Assert.That(line, Does.StartWith("NET:"));
        AssertNoHexColor(line);
    }

    [Test]
    public void Format_null_level_is_em_dash()
    {
        C2NetworkHealthLevel? level = null;
        var line = C2NetworkHealthHudFormat.Format(level);

        Assert.That(line, Is.EqualTo("NET: —"));
        Assert.That(line, Does.StartWith("NET:"));
        AssertNoHexColor(line);
    }

    [Test]
    public void Format_null_snapshot_is_em_dash()
    {
        var line = C2NetworkHealthHudFormat.Format((C2NetworkHealthSnapshot?)null);

        Assert.That(line, Is.EqualTo("NET: —"));
        AssertNoHexColor(line);
    }

    [Test]
    public void FromCommsState_maps_denied_degraded_nominal_and_null()
    {
        Assert.That(
            C2NetworkHealthHudFormat.FromCommsState(CommsState.Denied),
            Is.EqualTo("NET: PARTITIONED"));
        Assert.That(
            C2NetworkHealthHudFormat.FromCommsState(CommsState.Degraded),
            Is.EqualTo("NET: DEGRADED"));
        Assert.That(
            C2NetworkHealthHudFormat.FromCommsState(CommsState.Nominal),
            Is.EqualTo("NET: HEALTHY"));
        Assert.That(
            C2NetworkHealthHudFormat.FromCommsState(null),
            Is.EqualTo("NET: —"));

        AssertNoHexColor(C2NetworkHealthHudFormat.FromCommsState(CommsState.Denied));
        AssertNoHexColor(C2NetworkHealthHudFormat.FromCommsState(CommsState.Degraded));
        AssertNoHexColor(C2NetworkHealthHudFormat.FromCommsState(CommsState.Nominal));
        AssertNoHexColor(C2NetworkHealthHudFormat.FromCommsState(null));
    }

    [Test]
    public void FromCommsLabel_maps_topbar_comms_text()
    {
        Assert.That(
            C2NetworkHealthHudFormat.FromCommsLabel("COMMS: NOMINAL"),
            Is.EqualTo("NET: HEALTHY"));
        Assert.That(
            C2NetworkHealthHudFormat.FromCommsLabel("COMMS: DEGRADED"),
            Is.EqualTo("NET: DEGRADED"));
        Assert.That(
            C2NetworkHealthHudFormat.FromCommsLabel("COMMS: DENIED"),
            Is.EqualTo("NET: PARTITIONED"));
        Assert.That(
            C2NetworkHealthHudFormat.FromCommsLabel(null),
            Is.EqualTo("NET: —"));
    }

    [Test]
    public void Apply_null_state_sets_network_health_em_dash()
    {
        var applied = C2TopBarApplyState.Apply(null);

        Assert.That(applied.NetworkHealthLine, Is.EqualTo("NET: —"));
        Assert.That(C2TopBarPresentation.Empty.NetworkHealthLine, Is.EqualTo("NET: —"));
        AssertNoHexColor(applied.NetworkHealthLine);
    }

    [Test]
    public void Apply_without_health_keeps_other_fields_and_em_dash()
    {
        var projected = C2TopBarProjection.Project(
            3661,
            SimulationPhase.Executing,
            "2x",
            "Mixed",
            new DecisionLog(),
            baseScore: 10);

        var applied = C2TopBarApplyState.Apply(projected);

        Assert.That(applied.CommsLabel, Is.EqualTo(projected.CommsLabel));
        Assert.That(applied.ScoreLabel, Is.EqualTo(projected.ScoreLabel));
        Assert.That(applied.NetworkHealthLine, Is.EqualTo("NET: —"));
        AssertNoHexColor(applied.NetworkHealthLine);
    }

    [Test]
    public void Apply_with_level_sets_network_health_line()
    {
        var projected = C2TopBarProjection.Project(
            10,
            SimulationPhase.Executing,
            "1x",
            "Mixed",
            new DecisionLog());

        var healthy = C2TopBarApplyState.Apply(projected, C2NetworkHealthLevel.Healthy);
        var degraded = C2TopBarApplyState.Apply(projected, C2NetworkHealthLevel.Degraded);
        var partitioned = C2TopBarApplyState.Apply(projected, C2NetworkHealthLevel.Partitioned);

        Assert.That(healthy.NetworkHealthLine, Is.EqualTo("NET: HEALTHY"));
        Assert.That(degraded.NetworkHealthLine, Is.EqualTo("NET: DEGRADED"));
        Assert.That(partitioned.NetworkHealthLine, Is.EqualTo("NET: PARTITIONED"));
        Assert.That(healthy.CommsLabel, Is.EqualTo(projected.CommsLabel));
        Assert.That(partitioned.ScoreLabel, Is.EqualTo(projected.ScoreLabel));
        AssertNoHexColor(healthy.NetworkHealthLine);
        AssertNoHexColor(degraded.NetworkHealthLine);
        AssertNoHexColor(partitioned.NetworkHealthLine);
    }

    [Test]
    public void Apply_from_comms_fallback_maps_denied_to_partitioned()
    {
        var projected = C2TopBarProjection.Project(
            10,
            SimulationPhase.Executing,
            "1x",
            "Mixed",
            new DecisionLog());

        var applied = C2TopBarApplyState.ApplyFromComms(projected, CommsState.Denied);

        Assert.That(applied.NetworkHealthLine, Is.EqualTo("NET: PARTITIONED"));
        Assert.That(applied.CommsLabel, Is.EqualTo(projected.CommsLabel));
        AssertNoHexColor(applied.NetworkHealthLine);
    }

    private static void AssertNoHexColor(string line)
    {
        Assert.That(line, Does.Not.Contain("#"));
        Assert.That(line.ToLowerInvariant(), Does.Not.Contain("0x"));
    }
}
