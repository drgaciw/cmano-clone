namespace ProjectAegis.Delegation.UnityAdapter.Tests.Bridge;

using Controllers;
using Projection;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Sim.Comms;
using ProjectAegis.Sim.Glossary;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

/// <summary>C3-01 / DRG-390: off-grid units refuse new direct orders at the player-command façade.</summary>
[TestFixture]
public sealed class OffGridCommandFacadeTests
{
    private static DelegationBridge BuildBridge(params ScenarioCommsGridTransition[] transitions)
    {
        var bridge = new DelegationBridge(42, mvpEngagement: true);
        var unit = bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
        unit.Target.Slot.SetActive(new HumanController());
        bridge.Session!.CommsGrid = new CommsGridRegistry(transitions);
        return bridge;
    }

    [Test]
    public void On_grid_unit_accepts_player_command()
    {
        var bridge = BuildBridge(new ScenarioCommsGridTransition(10, "u1", "OffGrid"));
        Assert.That(C2PlayerCommandBridge.TryIssue(bridge, new EntityKey(1), "hold", simTime: 5, out var reason), Is.True);
        Assert.That(reason, Is.Null);
    }

    [Test]
    public void Off_grid_unit_rejects_player_command_with_COMMS_OFF_GRID()
    {
        var bridge = BuildBridge(new ScenarioCommsGridTransition(10, "u1", "OffGrid", "below comms depth"));
        Assert.That(C2PlayerCommandBridge.TryIssue(bridge, new EntityKey(1), "hold", simTime: 12, out var reason), Is.False);
        Assert.That(reason, Is.EqualTo(AbortReasonCatalog.Doctrine.COMMS_OFF_GRID));
        Assert.That(bridge.Orchestrator.DecisionLog.PlayerOrders, Is.Empty);
    }

    [Test]
    public void Rejoined_unit_accepts_orders_again()
    {
        var bridge = BuildBridge(
            new ScenarioCommsGridTransition(10, "u1", "OffGrid"),
            new ScenarioCommsGridTransition(20, "u1", "OnGrid"));
        Assert.That(bridge.Orchestrator.EvaluateOffGrid("u1", 15), Is.EqualTo(FireAbortReason.OffGrid));
        Assert.That(C2PlayerCommandBridge.TryIssue(bridge, new EntityKey(1), "hold", simTime: 21, out _), Is.True);
    }

    [Test]
    public void Session_evaluate_maps_to_OffGrid_fire_abort_reason()
    {
        var bridge = BuildBridge(new ScenarioCommsGridTransition(1, "u1", "OffGrid"));
        Assert.That(bridge.Session!.EvaluateOffGrid("u1", 1), Is.EqualTo(FireAbortReason.OffGrid));
        Assert.That(bridge.Session.EvaluateOffGrid("u2", 1), Is.Null);
        Assert.That(bridge.Orchestrator.LastCommsGridChanges.Single().UnitId, Is.EqualTo("u1"));
    }

    [Test]
    public void Without_comms_grid_behaviour_is_unchanged()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: true);
        var unit = bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
        unit.Target.Slot.SetActive(new HumanController());
        Assert.That(bridge.Session!.CommsGrid, Is.Null);
        Assert.That(C2PlayerCommandBridge.TryIssue(bridge, new EntityKey(1), "hold", simTime: 1, out _), Is.True);
    }

    [Test]
    public void Projection_shows_last_reported_position_for_off_grid_units()
    {
        var grid = new CommsGridRegistry(new[] { new ScenarioCommsGridTransition(10, "sub-1", "OffGrid") });
        grid.Advance(0);
        grid.ReportPosition("sub-1", 8, 58.5, 19.25);
        grid.Advance(10);
        grid.ReportPosition("sub-1", 12, 60, 21); // ignored while off grid

        var row = CommsGridProjection.Project(grid, 15).Single();
        Assert.That(row.UnitId, Is.EqualTo("sub-1"));
        Assert.That(row.HasLastReport, Is.True);
        Assert.That(row.LastLatitudeDeg, Is.EqualTo(58.5));
        Assert.That(row.LastReportTick, Is.EqualTo(8UL));
        Assert.That(row.LastLongitudeDeg, Is.EqualTo(19.25));
        Assert.That(row.OffGridSinceTick, Is.EqualTo(10UL));
        var change = grid.Advance(10);
        Assert.That(change, Is.Empty);
        Assert.That(CommsGridProjection.FormatChange(new CommsGridChange(10, "sub-1", CommsGridMembership.OnGrid, CommsGridMembership.OffGrid, "deep")),
            Is.EqualTo("T10 sub-1 OnGrid→OffGrid (deep)"));
        Assert.That(row.Label, Does.StartWith(CommsGridProjection.OffGridLabelPrefix));
        Assert.That(CommsGridProjection.Project(null, 15), Is.Empty);
    }
}
