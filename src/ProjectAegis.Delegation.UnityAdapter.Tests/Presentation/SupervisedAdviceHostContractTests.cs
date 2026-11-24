using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

[TestFixture]
public sealed class SupervisedAdviceHostContractTests
{
    [Test]
    public void SupervisedAdvicePanelHost_is_thin_host_at_sorting_order_18_routing_via_facade()
    {
        var source = UiIaSourceReader.ReadRuntime("SupervisedAdvicePanelHost.cs");

        Assert.That(source, Does.Contain("18"));
        Assert.That(source, Does.Contain("UiDocumentPanelSettingsBootstrap.EnsureDocument"));
        Assert.That(source, Does.Contain("ProjectSupervisedAdviceCard"));
        Assert.That(source, Does.Contain("TryAcceptSupervisedAdvice"));
        Assert.That(source, Does.Contain("DismissSupervisedAdvice"));

        Assert.That(source, Does.Not.Contain("using ProjectAegis.Sim"));
        Assert.That(source, Does.Not.Contain("IOrderSink"));
        Assert.That(source, Does.Not.Contain("ApplyOrder"));
        Assert.That(source, Does.Not.Contain("DelegationBridge.Tick"));
        Assert.That(source, Does.Not.Contain("CatalogWriteGate"));
        Assert.That(source, Does.Not.Contain("FindObjectOfType"));
        Assert.That(source, Does.Not.Contain("FindObjectsByType"));
        Assert.That(source, Does.Not.Contain("Resources.Load"));
        Assert.That(source, Does.Not.Contain("ADR-018"));
    }

    [Test]
    public void SupervisedAdvicePanel_uxml_and_uss_declare_elements_and_focus_tokens()
    {
        var uxml = UiIaSourceReader.ReadUnder(
            "unity", "ProjectAegis", "Assets", "UI", "SupervisedAdvice", "SupervisedAdvicePanel.uxml");
        var uss = UiIaSourceReader.ReadUnder(
            "unity", "ProjectAegis", "Assets", "UI", "SupervisedAdvice", "SupervisedAdvicePanel.uss");

        foreach (var elementName in new[]
        {
            "supervised-advice-root",
            "supervised-advice-header",
            "supervised-advice-rationale",
            "supervised-advice-authority",
            "supervised-advice-evidence-list",
            "supervised-advice-optin-toggle",
            "supervised-advice-accept",
            "supervised-advice-dismiss",
            "supervised-advice-status",
        })
        {
            Assert.That(uxml, Does.Contain($"name=\"{elementName}\""), $"Missing UXML element {elementName}");
        }

        Assert.That(uss, Does.Contain("../AegisTokens.uss"));
        Assert.That(uss, Does.Contain("var(--focus-ring)"));
    }
}
