namespace ProjectAegis.Delegation.UnityAdapter.Tests.Bridge;

using Controllers;
using Core;
using Sim;
using UnityAdapter.Bridge;
using ProjectAegis.Sim.Glossary;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

/// <summary>
/// DRG-415: off-grid gating is a comms rule, not an engagement feature. With
/// <c>mvpEngagement: false</c> (no <see cref="DelegationBridge.Session"/>) a scenario's commsGrid
/// transitions must still refuse new direct orders, attack (Engage) orders, and direct-control
/// requests with <see cref="FireAbortReason.OffGrid"/>, and back-on-grid must restore them.
/// Exercises the orchestrator / command façade only; no bridge hot-path tick.
/// </summary>
[TestFixture]
public sealed class OffGridMvpEngagementDisabledTests
{
    private static readonly EntityKey Unit = new(1);

    private static DelegationBridge BuildBridge()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: false)
        {
            Orchestrator =
            {
                ScenarioPolicy = new ScenarioPolicyProfile(EffectivePolicy.DefaultFree)
                {
                    CommsGridTransitions =
                    [
                        new ScenarioCommsGridTransition(10, "u1", "OffGrid", "below comms depth"),
                        new ScenarioCommsGridTransition(20, "u1", "OnGrid", "periscope depth"),
                    ],
                },
            },
        };
        var unit = bridge.Registry.RegisterUnit(Unit, "u1");
        unit.Target.Slot.SetActive(new HumanController());
        bridge.BeginExecution();
        return bridge;
    }

    private static ObservedState State(double t) => new(t, 0, 0, new Dictionary<TargetId, bool>());

    [Test]
    public void Comms_grid_resolves_without_an_engagement_session()
    {
        var bridge = BuildBridge();
        Assert.That(bridge.Session, Is.Null);
        Assert.That(bridge.Orchestrator.CommsGrid, Is.Not.Null);
        Assert.That(bridge.Orchestrator.EvaluateOffGrid("u1", 5), Is.Null);
        Assert.That(bridge.Orchestrator.EvaluateOffGrid("u1", 12), Is.EqualTo(FireAbortReason.OffGrid));
    }

    [Test]
    public void Off_grid_player_command_is_refused_with_COMMS_OFF_GRID()
    {
        var bridge = BuildBridge();
        Assert.That(C2PlayerCommandBridge.TryIssue(bridge, Unit, "hold", simTime: 12, out var reason), Is.False);
        Assert.That(reason, Is.EqualTo(AbortReasonCatalog.Doctrine.COMMS_OFF_GRID));
        Assert.That(bridge.Orchestrator.DecisionLog.PlayerOrders, Is.Empty);
    }

    [Test]
    public void Off_grid_unit_cannot_be_taken_under_direct_control()
    {
        var bridge = BuildBridge();
        Assert.That(bridge.TryTakeDirectControl(Unit, simTime: 12), Is.False);
        Assert.That(bridge.TryTakeDirectControl(Unit, simTime: 21), Is.True);
    }

    [TestCase(OrderKind.Hold)]
    [TestCase(OrderKind.Engage)]
    public void Direct_and_attack_orders_issued_off_grid_are_dropped_with_OffGrid_denial(OrderKind kind)
    {
        var bridge = BuildBridge();
        bridge.Orchestrator.Tick(State(10));
        bridge.TryEnqueueHumanOrder(Unit, kind, simTime: 12);

        bridge.Orchestrator.Tick(State(12));

        Assert.That(bridge.Orchestrator.ExecutedOrders, Is.Empty);
        var denial = bridge.Orchestrator.DecisionLog.PolicyDenials.Single();
        Assert.That(denial.Reason, Is.EqualTo(FireAbortReason.OffGrid));
        Assert.That(denial.AttemptedKind, Is.EqualTo(kind));
    }

    [TestCase(OrderKind.Hold)]
    [TestCase(OrderKind.Engage)]
    public void Back_on_grid_restores_orders_and_player_commands(OrderKind kind)
    {
        var bridge = BuildBridge();
        bridge.Orchestrator.Tick(State(21));
        Assert.That(bridge.Orchestrator.EvaluateOffGrid("u1", 21), Is.Null);
        Assert.That(C2PlayerCommandBridge.TryIssue(bridge, Unit, "hold", simTime: 21, out var reason), Is.True);
        Assert.That(reason, Is.Null);

        bridge.TryEnqueueHumanOrder(Unit, kind, simTime: 22);
        bridge.Orchestrator.Tick(State(22));

        Assert.That(bridge.Orchestrator.ExecutedOrders, Is.Not.Empty);
        Assert.That(bridge.Orchestrator.DecisionLog.PolicyDenials, Is.Empty);
    }
}
