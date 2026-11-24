using System.Xml.Linq;
using NUnit.Framework;
using ProjectAegis.Delegation.UnityAdapter.CommandReview;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

[TestFixture]
public sealed class CoordinationHostContractTests
{
    [Test]
    public void CoordinationPanelHost_honors_adr010_adr007_adr001_sortingOrder_16_and_SubmitGroupDecision_only()
    {
        var source = UiIaSourceReader.ReadRuntime("CoordinationPanelHost.cs");

        Assert.That(source, Does.Contain("DefaultSortingOrder = 16"));
        Assert.That(source, Does.Contain("CoordinationPresentation.Build"));
        Assert.That(source, Does.Contain("CoordinationBlockExplainer.Explain"));
        Assert.That(source, Does.Contain("SubmitGroupDecision"));
        Assert.That(source, Does.Contain("[DisallowMultipleComponent]"));
        Assert.That(source, Does.Contain("[RequireComponent(typeof(UIDocument))]"));
        Assert.That(source, Does.Contain("[SerializeField] private DelegationBridgeHost"));
        Assert.That(source, Does.Contain("UiDocumentPanelSettingsBootstrap.EnsureDocument"));
        Assert.That(source, Does.Contain("OnDisable"));
        Assert.That(source, Does.Contain("UnregisterValueChangedCallback(OnGroupDropdownChanged)"));
        Assert.That(source, Does.Contain("_holdButton.clicked -= OnHoldClicked"));
        Assert.That(source, Does.Contain("CoordinationAssistedWithdrawRedeployConfirmBinder.TryCancel();"));
        // Verify type-level contract reference exists in UnityAdapter
        Assert.That(typeof(CoordinationPresentation).FullName, Is.Not.Null);
        Assert.That(typeof(CoordinationBlockExplainer).FullName, Is.Not.Null);
    }

    [Test]
    public void CoordinationPanelHost_avoids_anti_patterns_Find_Resources_Sim_OrderSink_and_ADR018()
    {
        var source = UiIaSourceReader.ReadRuntime("CoordinationPanelHost.cs");
        var commandBridgeSource = UiIaSourceReader.ReadUnder(
            "src", "ProjectAegis.Delegation.UnityAdapter", "CommandReview", "CoordinationCommandBridge.cs");

        Assert.That(source, Does.Not.Contain("using ProjectAegis.Sim"));
        Assert.That(source, Does.Not.Contain("IOrderSink"));
        Assert.That(source, Does.Not.Contain("ApplyOrder"));
        Assert.That(source, Does.Not.Contain("DecisionLog.Append"));
        Assert.That(source, Does.Not.Contain("DelegationBridge.Tick"));
        Assert.That(source, Does.Not.Contain("SimulationSession"));
        Assert.That(source, Does.Not.Contain("CatalogWriteGate"));
        Assert.That(source, Does.Not.Contain("FindObjectOfType"));
        Assert.That(source, Does.Not.Contain("FindObjectsByType"));
        Assert.That(source, Does.Not.Contain("Resources.Load"));
        Assert.That(source, Does.Not.Contain("ADR-018"));
        Assert.That(source, Does.Not.Contain("CoordinationCommandBridge.Submit"));
        Assert.That(commandBridgeSource, Does.Contain("ReasonRetattackAdvisoryOnly"));
    }

    [Test]
    public void CoordinationPanel_uxml_and_uss_declare_required_elements_and_status_classes()
    {
        var xml = XDocument.Parse(UiIaSourceReader.ReadUnder(
            "unity", "ProjectAegis", "Assets", "UI", "Coordination", "CoordinationPanel.uxml"));
        var uss = UiIaSourceReader.ReadUnder(
            "unity", "ProjectAegis", "Assets", "UI", "Coordination", "CoordinationPanel.uss");

        foreach (var name in new[]
        {
            "coordination-panel-root",
            "coord-header-title",
            "coord-advisory-badge",
            "coord-group-dropdown",
            "coord-intent-line",
            "coord-controller-line",
            "coord-boundaries-list",
            "coord-effects-list",
            "coord-block-reason-line",
            "coord-hold-button",
            "coord-withdraw-button",
            "coord-reattack-button",
            "coord-assisted-confirm-box",
            "coord-assisted-confirm-title",
            "coord-assisted-confirm-body",
            "coord-assisted-confirm-accept",
            "coord-assisted-confirm-cancel",
            "coord-feedback-line",
        })
        {
            Assert.That(xml.Descendants().Count(e => (string?)e.Attribute("name") == name), Is.EqualTo(1), name);
        }

        Assert.That(uss, Does.Contain(".coord-status--ok"));
        Assert.That(uss, Does.Contain(".coord-status--degraded"));
        Assert.That(uss, Does.Contain(".coord-status--blocked"));
        Assert.That(uss, Does.Contain(".coord-status--advisory"));
        Assert.That(uss, Does.Not.Contain("picking-mode:"));
        Assert.That(uss, Does.Not.Contain("-unity-font: resource("));
    }
}
