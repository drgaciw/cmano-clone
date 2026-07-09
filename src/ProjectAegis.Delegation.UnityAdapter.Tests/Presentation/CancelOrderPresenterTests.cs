using System.Linq;
using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

/// <summary>
/// T5 (req 20 §Order lifecycle, TR-c2-006 residual presentation path): UI cancel affordance wraps
/// existing <see cref="DelegationBridge.TryCancelHumanOrder"/> only — no bridge signature changes.
/// </summary>
[TestFixture]
public sealed class CancelOrderPresenterTests
{
    [Test]
    public void TryCancel_by_unit_id_emits_PlayerOrderCancelled_and_projects_Aborted()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: true, scenarioPolicyId: "baltic-patrol");
        var u1 = bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
        u1.Target.Slot.SetActive(new HumanController());
        bridge.BeginExecution();
        Assert.That(bridge.TryEnqueueHumanOrder(new EntityKey(1), OrderKind.Move, simTime: 10.0), Is.True);

        var result = CancelOrderPresenter.TryCancel(bridge, "u1", simTime: 12.0);

        Assert.That(result.Success, Is.True);
        Assert.That(result.UnitId, Is.EqualTo("u1"));
        Assert.That(result.FailureReason, Is.Null);
        Assert.That(bridge.Orchestrator.DecisionLog.PlayerOrderCancellations, Has.Count.EqualTo(1));
        Assert.That(
            bridge.Orchestrator.DecisionLog.PlayerOrderCancellations.Single().UnitId.Value,
            Is.EqualTo("u1"));

        var states = OrderLifecycleProjection.Project(bridge.Orchestrator.DecisionLog);
        var key = states.Keys.Single(k => k.UnitId == "u1");
        Assert.That(states[key], Is.EqualTo(OrderLifecycleState.Aborted));
    }

    [Test]
    public void TryCancel_by_entity_key_also_routes_through_bridge()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: true, scenarioPolicyId: "baltic-patrol");
        var u1 = bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
        u1.Target.Slot.SetActive(new HumanController());
        bridge.BeginExecution();
        bridge.TryEnqueueHumanOrder(new EntityKey(1), OrderKind.Move, simTime: 10.0);

        var result = CancelOrderPresenter.TryCancel(bridge, new EntityKey(1), "u1", simTime: 12.0);

        Assert.That(result.Success, Is.True);
        Assert.That(bridge.Orchestrator.DecisionLog.PlayerOrderCancellations, Has.Count.EqualTo(1));
    }

    [Test]
    public void TryCancel_returns_failure_when_nothing_pending()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: true, scenarioPolicyId: "baltic-patrol");
        var u1 = bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
        u1.Target.Slot.SetActive(new HumanController());
        bridge.BeginExecution();

        var result = CancelOrderPresenter.TryCancel(bridge, "u1", simTime: 12.0);

        Assert.That(result.Success, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo("no-pending-order"));
        Assert.That(bridge.Orchestrator.DecisionLog.PlayerOrderCancellations, Is.Empty);
    }

    [Test]
    public void TryCancel_unknown_unit_returns_unit_not_found()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: true, scenarioPolicyId: "baltic-patrol");
        bridge.BeginExecution();

        var result = CancelOrderPresenter.TryCancel(bridge, "ghost", simTime: 1.0);

        Assert.That(result.Success, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo("unit-not-found"));
    }

    [Test]
    public void TryCancel_null_bridge_or_unit_is_safe_failure()
    {
        Assert.That(CancelOrderPresenter.TryCancel(null, "u1", 1.0).FailureReason, Is.EqualTo("no-bridge"));
        Assert.That(CancelOrderPresenter.TryCancel(
            new DelegationBridge(1, mvpEngagement: true, scenarioPolicyId: "baltic-patrol"),
            null,
            1.0).FailureReason, Is.EqualTo("no-unit"));
        Assert.That(CancelOrderPresenter.TryCancel(
            new DelegationBridge(1, mvpEngagement: true, scenarioPolicyId: "baltic-patrol"),
            "",
            1.0).FailureReason, Is.EqualTo("no-unit"));
    }

    [Test]
    public void TryCancel_is_noop_in_replay()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: true, scenarioPolicyId: "baltic-patrol");
        var u1 = bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
        u1.Target.Slot.SetActive(new HumanController());
        bridge.BeginExecution();
        bridge.TryEnqueueHumanOrder(new EntityKey(1), OrderKind.Move, simTime: 10.0);
        bridge.AttachReplayViewer = true;

        var result = CancelOrderPresenter.TryCancel(bridge, "u1", simTime: 12.0);

        Assert.That(result.Success, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo("replay"));
        Assert.That(bridge.Orchestrator.DecisionLog.PlayerOrderCancellations, Is.Empty);
    }
}
