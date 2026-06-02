using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

public sealed class UnitDetailProjectionTests
{
    [Test]
    public void ProjectSelected_reflects_magazine_change_and_alive_state()
    {
        var log = new DecisionLog();
        var unit = new TargetId("u1");
        log.AppendMagazineChange(new MagazineChangeRecord(
            1, 1.0, 1, unit, 0, -1, "fire"));
        var policy = new ScenarioPolicyProfile(
            EffectivePolicy.DefaultFree,
            unitRadarEmcon: new Dictionary<string, EmconState> { ["u1"] = EmconState.Active });

        var detail = UnitDetailProjection.ProjectSelected(
            unit,
            _ => true,
            log,
            policy,
            simTimeSeconds: 100);

        Assert.That(detail!.MagazineLabel, Does.Contain("Δ-1"));
        Assert.That(detail.EmconLabel, Does.Contain("ACTIVE"));
        Assert.That(detail.DoctrineLabel, Does.Contain("WeaponsFree"));
    }

    [Test]
    public void ProjectSelected_fuel_line_uses_scenario_logistics_thresholds()
    {
        var policy = new ScenarioPolicyProfile(
            EffectivePolicy.DefaultFree,
            logistics: new ScenarioLogisticsSettings(10, 20));
        var detail = UnitDetailProjection.ProjectSelected(
            new TargetId("u1"),
            _ => true,
            new DecisionLog(),
            policy,
            simTimeSeconds: 15);
        Assert.That(detail!.FuelLabel, Does.Contain("JOKER"));
    }

    [Test]
    public void ProjectPrimary_picks_lowest_unit_id()
    {
        var members = new[] { new TargetId("u2"), new TargetId("u1") };
        var detail = UnitDetailProjection.ProjectPrimary(
            members,
            _ => true,
            new DecisionLog(),
            null,
            simTimeSeconds: 0);
        Assert.That(detail!.UnitId, Is.EqualTo("u1"));
    }
}