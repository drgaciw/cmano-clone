using NUnit.Framework;
using ProjectAegis.Delegation.UnityAdapter.Presentation;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

[TestFixture]
public sealed class TimeCompressionSafeStopHostContractTests
{
    [Test]
    public void SimplePlayModeSimHost_uses_dynamic_acceleration_overload_and_refreshes_notifications_per_step()
    {
        var source = UiIaSourceReader.ReadRuntime("SimplePlayModeSimHost.cs");

        Assert.That(source, Does.Contain("bridgeHost.RefreshMessageNotifications()"));
        Assert.That(source, Does.Contain("TimeAccelerationFactor ?? 1"));
        Assert.That(source, Does.Contain("() => bridgeHost.Session?.TimeAccelerationFactor ?? 1"));
        Assert.That(source, Does.Not.Contain("using ProjectAegis.Sim"));
    }

    [Test]
    public void C2TopBarPanelHost_preserves_sandbox_banner_gates_notification_pause_and_blocks_observer_clock()
    {
        var source = UiIaSourceReader.ReadRuntime("C2TopBarPanelHost.cs");

        Assert.That(source, Does.Contain("LoopPolicyChromeBinder.TestSandboxBannerElementName"));
        Assert.That(source, Does.Contain("TestSandboxBannerName"));
        Assert.That(source, Does.Contain("HasUnresolvedNotificationPause"));
        Assert.That(source, Does.Contain("_observerChrome.IsObserver"));
        Assert.That(source, Does.Not.Contain("using ProjectAegis.Sim"));
    }

    [Test]
    public void DelegationBridgeHost_marks_existing_messages_seen_on_BeginExecution_and_wires_coordinator()
    {
        var source = UiIaSourceReader.ReadRuntime("DelegationBridgeHost.cs");

        Assert.That(source, Does.Contain("MarkExistingAsSeen"));
        Assert.That(source, Does.Contain("MessageLogNotificationCoordinator"));
        Assert.That(source, Does.Contain("LastMessageLog = MessageLogBridge.ProjectLive(Bridge)"));
        Assert.That(source, Does.Not.Contain("MessageLogBridge.ProjectFrom(Bridge.Orchestrator.DecisionLog)"));
        Assert.That(source, Does.Contain("EvaluateAndApplyNotificationStop"));
    }

    [Test]
    public void Hard_no_bridge_session_and_pending_queue_have_no_M4_safe_stop_couplings()
    {
        var bridge = UiIaSourceReader.ReadUnder(
            "src", "ProjectAegis.Delegation.UnityAdapter", "Bridge", "DelegationBridge.cs");
        var session = UiIaSourceReader.ReadUnder(
            "src", "ProjectAegis.Delegation", "Orchestration", "SimulationSession.cs");
        var pendingQueue = UiIaSourceReader.ReadUnder(
            "src", "ProjectAegis.Delegation", "Orchestration", "PendingApprovalQueue.cs");

        foreach (var source in new[] { bridge, session, pendingQueue })
        {
            Assert.That(source, Does.Not.Contain("MessageLogNotificationCoordinator"));
            Assert.That(source, Does.Not.Contain("MessageCategoryPolicy"));
            Assert.That(source, Does.Not.Contain("DecisionLogDeepLink"));
        }
    }
}
