namespace ProjectAegis.Delegation.UnityAdapter.Tests.Bridge;

using System.Linq;
using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Baltic;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using NUnit.Framework;

/// <summary>
/// Phase 2 integration proxy for req 20 rev 2 (C2 UI delta) — headless equivalent of the Unity
/// panel-host wiring for the three riskiest cross-track seams: T1 multi-select group fan-out, T2
/// order-lifecycle projection, and T3 Critical-alert auto-pause. Complements
/// <see cref="PlayModeSmokeHarnessTests"/> (kept green) rather than replacing it. All three tests use
/// only the pure/headless track models plus <see cref="BalticReplayHarness"/> / a live
/// <see cref="DelegationBridge"/> — no Unity types.
/// </summary>
[TestFixture]
public sealed class C2Rev2IntegrationProxyTests
{
    /// <summary>
    /// T1 (req 20 §Selection, TR-c2-005): a multi-select built on the shared
    /// <see cref="C2PresentationController.Selection"/> (Phase 0 <see cref="SelectionSet"/>) flows
    /// through <see cref="GroupOrderPlan"/> into <see cref="GroupOrderFanOut"/>, which issues exactly
    /// one <see cref="DelegationBridge.TryEnqueueHumanOrder"/> intent per eligible unit — no batched
    /// or duplicated bridge calls, ZERO diff to DelegationBridge.cs.
    /// </summary>
    [Test]
    public void MultiSelect_group_fan_out_issues_one_intent_per_eligible_unit()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: true, scenarioPolicyId: "baltic-patrol");
        var u1 = bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
        u1.Target.Slot.SetActive(new HumanController());
        var u2 = bridge.Registry.RegisterUnit(new EntityKey(2), "u2");
        u2.Target.Slot.SetActive(new HumanController());
        var u3 = bridge.Registry.RegisterUnit(new EntityKey(3), "u3");
        u3.Target.Slot.SetActive(new HumanController());
        bridge.BeginExecution();

        // req 20 §Selection (T1): drag-box / shift-click multi-select via the presentation
        // controller's shared SelectionSet (Phase 0 contract) — mirrors what
        // MapPlaceholderPanelHost.OnCanvasPointerUp / C2LeftDrawerPanelHost.OnOobRowClicked do.
        var controller = new C2PresentationController();
        controller.SelectFriendlyUnits(new[] { "u1", "u2", "u3" });
        Assert.That(controller.Selection.OrderedTargetIds, Is.EqualTo(new[] { "u1", "u2", "u3" }));

        // req 20 §Selection (T1, AC-7): fan a single group-order action out to every eligible unit
        // in the selection. All three are alive/known here, so all three are eligible.
        var alive = new HashSet<string>(StringComparer.Ordinal) { "u1", "u2", "u3" };
        var plan = GroupOrderPlan.Build(
            controller.Selection.OrderedTargetIds,
            id => alive.Contains(id)
                ? GroupOrderUnitVerdict.Eligible(id)
                : GroupOrderUnitVerdict.Ineligible(id, GroupOrderIneligibleReason.Destroyed));
        Assert.That(plan.HasAnyEligible, Is.True);

        var result = GroupOrderFanOut.ExecuteHumanOrder(plan, bridge, OrderKind.Hold, simTime: 1.0);

        Assert.That(result.Dispatched, Is.EquivalentTo(new[] { "u1", "u2", "u3" }));
        Assert.That(result.Failed, Is.Empty);
        Assert.That(
            bridge.Orchestrator.DecisionLog.PlayerOrders,
            Has.Count.EqualTo(3),
            "one PlayerOrder intent issued per eligible unit — exactly one bridge call each, no batching");
        Assert.That(
            bridge.Orchestrator.DecisionLog.PlayerOrders.Select(o => o.UnitId.Value),
            Is.EquivalentTo(new[] { "u1", "u2", "u3" }));
        Assert.That(
            bridge.Orchestrator.DecisionLog.PlayerOrders,
            Has.All.Matches<ProjectAegis.Delegation.Decision.PlayerOrderRecord>(r => r.Kind == OrderKind.Hold));
    }

    /// <summary>
    /// T2 (req 20 §Order lifecycle, TR-c2-006): a real player intent issued through
    /// <see cref="DelegationBridge.TryEnqueueHumanOrder"/> (the same zero-diff bridge call
    /// <see cref="GroupOrderFanOut"/> and the attack menu use) starts life as Accepted; as the sim's
    /// own engagement resolution appends its Engagement/EngagementOutcome evidence to the SAME
    /// <see cref="ProjectAegis.Delegation.Decision.DecisionLog"/> the bridge owns,
    /// <see cref="OrderLifecycleProjection"/> — the exact projection <c>MessageLogPanelHost.Refresh</c>
    /// calls every frame — walks the SAME <see cref="OrderLifecycleProjection.OrderKey"/> through
    /// Accepted → Executing → Completed.
    /// </summary>
    [Test]
    public void Order_transitions_through_OrderLifecycleProjection_states_via_live_bridge_intent()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: true, scenarioPolicyId: "baltic-patrol");
        var u1 = bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
        u1.Target.Slot.SetActive(new HumanController());
        bridge.BeginExecution();

        // Real bridge intent (T1/T2 shared surface) — logs a PlayerOrderRecord; DecisionLog assigns
        // the actual SequenceId (never assume it's 1 — earlier bridge setup may have logged entries).
        Assert.That(bridge.TryEnqueueHumanOrder(new EntityKey(1), OrderKind.Engage, simTime: 10.0), Is.True);

        var afterOrder = OrderLifecycleProjection.Project(bridge.Orchestrator.DecisionLog);
        Assert.That(afterOrder, Has.Count.EqualTo(1), "exactly the one order just enqueued should be tracked");
        var key = afterOrder.Keys.Single();
        Assert.That(key.UnitId, Is.EqualTo("u1"));
        Assert.That(afterOrder[key], Is.EqualTo(OrderLifecycleState.Accepted));

        // Downstream evidence the sim's own engagement resolution appends to this same log
        // (BalticReplayHarness exercises this path end-to-end for AI-issued orders; here we pin it
        // to the player-issued order above so the OrderKey correlation is deterministic).
        bridge.Orchestrator.DecisionLog.AppendEngagement(new ProjectAegis.Delegation.Decision.EngagementRecord(
            0, 11.0, 11, new TargetId("u1"), EngagementId: 900, Launched: true));
        var afterLaunch = OrderLifecycleProjection.Project(bridge.Orchestrator.DecisionLog);
        Assert.That(afterLaunch[key], Is.EqualTo(OrderLifecycleState.Executing));

        bridge.Orchestrator.DecisionLog.AppendEngagementOutcome(
            new ProjectAegis.Delegation.Decision.EngagementOutcomeRecord(
                0, 12.0, 12, new TargetId("u1"), new TargetId("hostile-1"),
                EngagementId: 900, OutcomeCode: "KILL", PkDraw: 0.2));
        var afterOutcome = OrderLifecycleProjection.Project(bridge.Orchestrator.DecisionLog);
        Assert.That(afterOutcome[key], Is.EqualTo(OrderLifecycleState.Completed));
    }

    /// <summary>
    /// T3 (req 20 §Alerting and Interruption, TR-c2-007): a live Baltic comms run produces a
    /// POLICY_DENIAL ("Fire denied...") message; <see cref="AlertProjection"/> tags it Critical via
    /// the Phase 0 <see cref="AlertSeverityMap"/>, and <see cref="AutoPausePolicy"/> yields an
    /// <see cref="AutoPauseCommand"/> for it — the same pipeline <c>MessageLogPanelHost</c> +
    /// <c>ToastStackPanelHost</c> would drive, minus the (blocked, per T3 report) pause-reason-stack
    /// dispatch itself.
    /// </summary>
    [Test]
    public void Critical_alert_yields_AutoPauseCommand_via_AutoPausePolicy()
    {
        var result = BalticReplayHarness.Run(42, "baltic-patrol-comms", ticks: 6, mvpEngagement: true);
        Assert.That(result.Messages.Any(m => m.Text.Contains("Denied", StringComparison.Ordinal)), Is.True);

        var alerts = AlertProjection.Project(result.Messages);
        var criticalAlerts = alerts.Where(a => a.Severity == AlertSeverity.Critical).ToArray();
        Assert.That(criticalAlerts, Is.Not.Empty, "POLICY_DENIAL lines must project to Critical severity");

        var command = AutoPausePolicy.Evaluate(criticalAlerts, isReplay: false);
        Assert.That(command, Is.Not.Null);
        Assert.That(command!.Reason, Is.EqualTo(AutoPausePolicy.CriticalAlertReason));
        Assert.That(command.TriggerSequenceId, Is.EqualTo(criticalAlerts[0].SequenceId));

        // req 20 §Replay suppression: the same batch must never auto-pause during replay playback.
        var replayCommand = AutoPausePolicy.Evaluate(criticalAlerts, isReplay: true);
        Assert.That(replayCommand, Is.Null);
    }
}
