using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

[TestFixture]
public sealed class JevRoeGateHostContractTests
{
    [Test]
    public void PendingApprovalPanelHost_wires_jev_suggested_order_through_coordination_facade_only()
    {
        var source = UiIaSourceReader.ReadRuntime("PendingApprovalPanelHost.cs");

        Assert.That(source, Does.Contain("pending-jev-suggested-order"));
        Assert.That(source, Does.Contain("pending-jev-suggested-order-accept"));
        Assert.That(source, Does.Contain("pending-jev-suggested-order-dismiss"));
        Assert.That(source, Does.Contain("LastJevSuggestedOrder"));
        Assert.That(source, Does.Contain("TrySubmitJevSuggestedDecision"));
        Assert.That(source, Does.Contain("ClearJevSuggestedOrder"));

        Assert.That(source, Does.Not.Contain("TryEnqueueHumanOrder"));
        Assert.That(source, Does.Not.Contain("IOrderSink"));
        Assert.That(source, Does.Not.Contain("ApplyOrder"));
        Assert.That(source, Does.Not.Contain("MessageLogNotificationCoordinator"));
        Assert.That(source, Does.Not.Contain("CatalogWriteGate"));
    }

    [Test]
    public void PendingApprovalPanel_uxml_and_uss_declare_jev_suggested_order_elements()
    {
        var uxml = UiIaSourceReader.ReadUnder(
            "unity", "ProjectAegis", "Assets", "UI", "PendingApproval", "PendingApprovalPanel.uxml");
        var uss = UiIaSourceReader.ReadUnder(
            "unity", "ProjectAegis", "Assets", "UI", "PendingApproval", "PendingApprovalPanel.uss");

        foreach (var elementName in new[]
        {
            "pending-jev-suggested-order",
            "pending-jev-suggested-order-title",
            "pending-jev-suggested-order-body",
            "pending-jev-suggested-order-accept",
            "pending-jev-suggested-order-dismiss",
        })
        {
            Assert.That(uxml, Does.Contain($"name=\"{elementName}\""), $"Missing UXML element {elementName}");
        }

        Assert.That(uss, Does.Contain(".c2-pending-jev-suggested-order"));
    }

    [Test]
    public void DelegationBridgeHost_routes_jev_suggested_order_exclusively_through_coordination_bridge()
    {
        var bridgeHostSource = UiIaSourceReader.ReadRuntime("DelegationBridgeHost.cs");

        Assert.That(bridgeHostSource, Does.Contain("JevRoeOrderGateBinder.Evaluate"));
        Assert.That(bridgeHostSource, Does.Contain("LastJevSuggestedOrder"));
        Assert.That(bridgeHostSource, Does.Contain("TrySubmitJevSuggestedDecision"));
        Assert.That(bridgeHostSource, Does.Contain("CoordinationCommandBridge.Submit"));
        Assert.That(bridgeHostSource, Does.Not.Contain("CatalogWriteGate"));
    }
}
