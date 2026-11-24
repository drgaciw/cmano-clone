using NUnit.Framework;
using ProjectAegis.Delegation.UnityAdapter.Presentation;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

[TestFixture]
public sealed class MessageLogPolicyAndDeepLinkHostContractTests
{
    [Test]
    public void MessageLogPanelHost_wires_policy_drawer_alert_banner_and_non_mutating_deeplink()
    {
        var source = UiIaSourceReader.ReadRuntime("MessageLogPanelHost.cs");

        Assert.That(source, Does.Contain("message-log-policy-toggle"));
        Assert.That(source, Does.Contain("message-log-policy-drawer"));
        Assert.That(source, Does.Contain("message-log-alert-banner"));
        Assert.That(source, Does.Contain("message-log-deeplink-drawer"));
        Assert.That(source, Does.Contain("IsReplayViewerAttached"));
        Assert.That(source, Does.Contain("InspectMessageLogDecision"));
        Assert.That(source, Does.Contain("MessageCategoryPolicyBinder"));
        Assert.That(source, Does.Contain("DecisionLogDeepLinkBinder"));
        Assert.That(source, Does.Contain("MessageLogRowWindow.Select("));
        Assert.That(source, Does.Contain("bridgeHost.NotificationPolicy"));
        Assert.That(source, Does.Contain("bridgeHost.ActiveMessageAlerts"));
        Assert.That(source, Does.Contain("MessageLogAlertBannerProjection.Project("));

        var bridgeHostSource = UiIaSourceReader.ReadRuntime("DelegationBridgeHost.cs");
        Assert.That(bridgeHostSource, Does.Contain("public IReadOnlyList<MessageNotificationAlert> ActiveMessageAlerts => _notificationCoordinator.ActiveAlerts;"));

        // Existing contracts preserved
        Assert.That(source, Does.Contain("label.focusable = true"));
        Assert.That(source, Does.Contain("KeyboardDiscoverySurfaces.MessageLogListElementName"));
        Assert.That(source, Does.Contain(FocusRingTokens.RingColorVariable));
        Assert.That(source, Does.Contain("MessageLogFuelBandBinder.Bind"));
        Assert.That(source, Does.Contain("FuelBandCueClasses.All"));

        // No sim authority or forbidden APIs
        Assert.That(source, Does.Not.Contain("using ProjectAegis.Sim"));
        Assert.That(source, Does.Not.Contain("FindObjectOfType"));
        Assert.That(source, Does.Not.Contain("FindObjectsByType"));
        Assert.That(source, Does.Not.Contain("Resources.Load"));
        Assert.That(source, Does.Not.Contain("DelegationBridge.Tick"));
        Assert.That(source, Does.Not.Contain("ApplyOrder"));
    }

    [Test]
    public void Hold_boundary_DRG268_and_DRG269_are_not_wired_into_MessageLog_or_Coordinator()
    {
        var messageHost = UiIaSourceReader.ReadRuntime("MessageLogPanelHost.cs");
        var deepLink = UiIaSourceReader.ReadUnder(
            "src", "ProjectAegis.Delegation", "Projection", "DecisionLogDeepLinkProjection.cs");
        var coordinator = UiIaSourceReader.ReadUnder(
            "src", "ProjectAegis.Delegation", "Projection", "MessageLogNotificationCoordinator.cs");
        var engageExplainHost = UiIaSourceReader.ReadRuntime("EngageExplainPanelHost.cs");
        var pendingApprovalHost = UiIaSourceReader.ReadRuntime("PendingApprovalPanelHost.cs");

        foreach (var source in new[] { messageHost, deepLink, coordinator })
        {
            Assert.That(source, Does.Not.Contain("EngageExplainPanelHost"));
            Assert.That(source, Does.Not.Contain("EngageExplainProjection"));
            Assert.That(source, Does.Not.Contain("InspectCombatEvent"));
            Assert.That(source, Does.Not.Contain("DisplayCombatKey"));
            Assert.That(source, Does.Not.Contain("PendingApprovalQueue"));
            Assert.That(source, Does.Not.Contain("PendingApprovalPanelHost"));
        }

        Assert.That(engageExplainHost, Does.Not.Contain("MessageLogPanelHost"));
        Assert.That(pendingApprovalHost, Does.Not.Contain("MessageLogNotificationCoordinator"));
    }

    [Test]
    public void MessageLogPanel_uxml_and_uss_declare_M4_elements_and_preserve_ASSET006()
    {
        var uxml = UiIaSourceReader.ReadUnder(
            "unity", "ProjectAegis", "Assets", "UI", "MessageLog", "MessageLogPanel.uxml");
        var uss = UiIaSourceReader.ReadUnder(
            "unity", "ProjectAegis", "Assets", "UI", "MessageLog", "MessageLogPanel.uss");

        foreach (var name in new[]
        {
            "message-log-root",
            "message-log-onboarding-hint",
            "message-list",
            "message-log-policy-toggle",
            "message-log-policy-drawer",
            "message-log-alert-banner",
            "message-log-deeplink-drawer",
        })
        {
            Assert.That(uxml, Does.Contain($"name=\"{name}\""), $"Missing UXML element {name}");
        }

        Assert.That(uss, Does.Contain("ASSET-006"));
        Assert.That(uss, Does.Contain("AegisTokens.uss"));
        Assert.That(uss, Does.Contain("var(--focus-ring)"));
        Assert.That(uss, Does.Contain("var(--focus-ring-width)"));
        Assert.That(uss, Does.Contain(".message-log-row--selectable:focus"));
        Assert.That(uss, Does.Not.Contain("-unity-font: resource("));
    }
}
