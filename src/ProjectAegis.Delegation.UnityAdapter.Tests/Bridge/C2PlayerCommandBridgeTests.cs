namespace ProjectAegis.Delegation.UnityAdapter.Tests.Bridge;

using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Input;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using NUnit.Framework;

[TestFixture]
public sealed class C2PlayerCommandBridgeTests
{
    [Test]
    public void TryIssue_hold_enqueues_player_order_for_human_unit()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: false);
        var unit = bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
        unit.Target.Slot.SetActive(new HumanController());

        Assert.That(
            C2PlayerCommandBridge.TryIssue(bridge, new EntityKey(1), "hold", simTime: 3, out var reason),
            Is.True);
        Assert.That(reason, Is.Null);
        Assert.That(bridge.Orchestrator.DecisionLog.PlayerOrders, Has.Count.EqualTo(1));
        Assert.That(bridge.Orchestrator.DecisionLog.PlayerOrders[0].Kind, Is.EqualTo(OrderKind.Hold));
    }

    [Test]
    public void TryIssue_set_emcon_and_set_sensors_map_to_new_kinds()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: false);
        var unit = bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
        unit.Target.Slot.SetActive(new HumanController());

        Assert.That(C2PlayerCommandBridge.TryIssue(bridge, new EntityKey(1), "set_emcon", 1, out _), Is.True);
        Assert.That(C2PlayerCommandBridge.TryIssue(bridge, new EntityKey(1), "set_sensors", 2, out _), Is.True);
        Assert.That(bridge.Orchestrator.DecisionLog.PlayerOrders[0].Kind, Is.EqualTo(OrderKind.SetEmcon));
        Assert.That(bridge.Orchestrator.DecisionLog.PlayerOrders[1].Kind, Is.EqualTo(OrderKind.SetSensors));
    }

    [Test]
    public void TryIssue_plot_course_maps_to_Move()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: false);
        var unit = bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
        unit.Target.Slot.SetActive(new HumanController());

        Assert.That(C2PlayerCommandBridge.TryIssue(bridge, new EntityKey(1), "plot_course", 1, out _), Is.True);
        Assert.That(bridge.Orchestrator.DecisionLog.PlayerOrders[0].Kind, Is.EqualTo(OrderKind.Move));
    }

    [Test]
    public void TryIssue_unknown_command_returns_UNKNOWN_COMMAND()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: false);
        var unit = bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
        unit.Target.Slot.SetActive(new HumanController());

        Assert.That(
            C2PlayerCommandBridge.TryIssue(bridge, new EntityKey(1), "warp", 1, out var reason),
            Is.False);
        Assert.That(reason, Is.EqualTo(C2CommandIssuance.ReasonUnknownCommand));
        Assert.That(bridge.Orchestrator.DecisionLog.PlayerOrders, Is.Empty);
    }

    [Test]
    public void TryIssue_missing_entity_returns_UNKNOWN_UNIT()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: false);

        Assert.That(
            C2PlayerCommandBridge.TryIssue(bridge, new EntityKey(99), "hold", 1, out var reason),
            Is.False);
        Assert.That(reason, Is.EqualTo(C2PlayerCommandBridge.ReasonUnknownUnit));
    }

    [Test]
    public void TryIssue_non_human_controller_returns_NOT_HUMAN_CONTROL()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: false);
        bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
        // default slot is not HumanController

        Assert.That(
            C2PlayerCommandBridge.TryIssue(bridge, new EntityKey(1), "hold", 1, out var reason),
            Is.False);
        Assert.That(reason, Is.EqualTo(C2PlayerCommandBridge.ReasonNotHumanControl));
    }

    [Test]
    public void TryIssue_replay_attached_returns_REPLAY_ATTACHED()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: false);
        var unit = bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
        unit.Target.Slot.SetActive(new HumanController());
        bridge.AttachReplayViewer = true;

        Assert.That(
            C2PlayerCommandBridge.TryIssue(bridge, new EntityKey(1), "hold", 1, out var reason),
            Is.False);
        Assert.That(reason, Is.EqualTo(C2PlayerCommandBridge.ReasonReplayAttached));
        Assert.That(bridge.Orchestrator.DecisionLog.PlayerOrders, Is.Empty);
    }
}
