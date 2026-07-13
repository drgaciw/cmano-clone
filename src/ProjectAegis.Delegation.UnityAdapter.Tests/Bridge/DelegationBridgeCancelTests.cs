namespace ProjectAegis.Delegation.UnityAdapter.Tests.Bridge;

using System.Linq;
using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using NUnit.Framework;

/// <summary>Phase 2b (req 20 rev 2 §Order lifecycle, TR-c2-006 / AC-8): the additive
/// <see cref="DelegationBridge.TryCancelHumanOrder"/> affordance emits a logged cancellation intent and
/// drives the lifecycle projection to Aborted — without altering any existing bridge method.</summary>
[TestFixture]
public sealed class DelegationBridgeCancelTests
{
    [Test]
    public void TryCancelHumanOrder_emits_a_logged_cancellation_and_projects_Aborted()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: true, scenarioPolicyId: "baltic-patrol");
        var u1 = bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
        u1.Target.Slot.SetActive(new HumanController());
        bridge.BeginExecution();

        Assert.That(bridge.TryEnqueueHumanOrder(new EntityKey(1), OrderKind.Move, simTime: 10.0), Is.True);

        var beforeCancel = OrderLifecycleProjection.Project(bridge.Orchestrator.DecisionLog);
        var key = beforeCancel.Keys.Single(k => k.UnitId == "u1");

        // Cancel the queued order before it drains (AC-8: cancel emits a logged cancellation intent).
        Assert.That(bridge.TryCancelHumanOrder(new EntityKey(1), simTime: 12.0, out var reason), Is.True);
        Assert.That(reason, Is.Null);
        Assert.That(bridge.Orchestrator.DecisionLog.PlayerOrderCancellations, Has.Count.EqualTo(1));
        Assert.That(bridge.Orchestrator.DecisionLog.PlayerOrderCancellations.Single().UnitId.Value, Is.EqualTo("u1"));

        var afterCancel = OrderLifecycleProjection.Project(bridge.Orchestrator.DecisionLog);
        Assert.That(afterCancel[key], Is.EqualTo(OrderLifecycleState.Aborted));
    }

    [Test]
    public void TryCancelHumanOrder_returns_false_when_nothing_is_pending()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: true, scenarioPolicyId: "baltic-patrol");
        var u1 = bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
        u1.Target.Slot.SetActive(new HumanController());
        bridge.BeginExecution();

        Assert.That(bridge.TryCancelHumanOrder(new EntityKey(1), simTime: 12.0, out var reason), Is.False);
        Assert.That(reason, Is.EqualTo("no-pending-order"));
        Assert.That(bridge.Orchestrator.DecisionLog.PlayerOrderCancellations, Is.Empty,
            "no cancellation intent is logged when there was nothing to cancel");
    }

    [Test]
    public void TryCancelHumanOrder_is_a_noop_in_replay()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: true, scenarioPolicyId: "baltic-patrol");
        var u1 = bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
        u1.Target.Slot.SetActive(new HumanController());
        bridge.BeginExecution();
        bridge.TryEnqueueHumanOrder(new EntityKey(1), OrderKind.Move, simTime: 10.0);

        bridge.AttachReplayViewer = true;
        Assert.That(bridge.TryCancelHumanOrder(new EntityKey(1), simTime: 12.0, out var reason), Is.False);
        Assert.That(reason, Is.EqualTo("replay"));
        Assert.That(bridge.Orchestrator.DecisionLog.PlayerOrderCancellations, Is.Empty);
    }

    [Test]
    public void TryCancelHumanOrder_clears_NextEngageSalvoOverride_for_Engage()
    {
        // Codex P2 on PR #257: canceling a queued Engage must clear sticky Session.NextEngageSalvoOverride
        // so a later engage cannot inherit the canceled salvo (add-only path in TryCancelHumanOrder).
        var bridge = new DelegationBridge(42, mvpEngagement: true, scenarioPolicyId: "baltic-patrol");
        var u1 = bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
        u1.Target.Slot.SetActive(new HumanController());
        bridge.BeginExecution();

        Assert.That(bridge.Session, Is.Not.Null);
        bridge.Session!.NextEngageSalvoOverride = 1;

        Assert.That(bridge.TryEnqueueHumanOrder(new EntityKey(1), OrderKind.Engage, simTime: 10.0), Is.True);
        Assert.That(bridge.TryCancelHumanOrder(new EntityKey(1), simTime: 12.0, out var reason), Is.True);
        Assert.That(reason, Is.Null);
        Assert.That(bridge.Session.NextEngageSalvoOverride, Is.Null);
    }

    [Test]
    public void TryCancelHumanOrder_does_not_clear_salvo_override_for_non_Engage()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: true, scenarioPolicyId: "baltic-patrol");
        var u1 = bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
        u1.Target.Slot.SetActive(new HumanController());
        bridge.BeginExecution();

        Assert.That(bridge.Session, Is.Not.Null);
        bridge.Session!.NextEngageSalvoOverride = 3;

        Assert.That(bridge.TryEnqueueHumanOrder(new EntityKey(1), OrderKind.Move, simTime: 10.0), Is.True);
        Assert.That(bridge.TryCancelHumanOrder(new EntityKey(1), simTime: 12.0, out _), Is.True);
        Assert.That(bridge.Session.NextEngageSalvoOverride, Is.EqualTo(3),
            "canceling a non-Engage must leave a pending engage salvo override intact");
    }
}
