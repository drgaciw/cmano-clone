using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

[TestFixture]
public sealed class OsintStagingHostContractTests
{
    [Test]
    public void OsintStagingPanelHost_binds_projection_and_bridge_without_direct_data_or_gate_access()
    {
        var source = UiIaSourceReader.ReadRuntime("OsintStagingPanelHost.cs");

        Assert.That(source, Does.Contain("OsintStagingBridge"));
        Assert.That(source, Does.Contain("OsintStagingProjection"));
        Assert.That(source, Does.Contain("UiDocumentPanelSettingsBootstrap.EnsureDocument"));
        Assert.That(source, Does.Contain("17"));
        Assert.That(source, Does.Contain("osint-detail-confidence"));
        Assert.That(source, Does.Contain("osint-detail-citation-url"));
        Assert.That(source, Does.Contain("osint-detail-citation-snippet"));
        Assert.That(source, Does.Contain("logonly-list"));
        Assert.That(source, Does.Contain("reject-selected"));

        Assert.That(source, Does.Not.Contain("CatalogWriteGate"));
        Assert.That(source, Does.Not.Contain("Sqlite"));
        Assert.That(source, Does.Not.Contain("ProjectAegis.Data"));
        Assert.That(source, Does.Not.Contain("ProjectAegis.Sim"));
        Assert.That(source, Does.Not.Contain("ApplyOrder"));
        Assert.That(source, Does.Not.Contain("IOrderSink"));
        Assert.That(source, Does.Not.Contain("DelegationBridge.Tick"));
        Assert.That(source, Does.Not.Contain("FindObjectOfType"));
        Assert.That(source, Does.Not.Contain("FindObjectsByType"));
        Assert.That(source, Does.Not.Contain("ADR-018"));
    }

    [Test]
    public void OsintStagingPanel_uxml_and_uss_declare_required_elements_and_tokens()
    {
        var uxml = UiIaSourceReader.ReadUnder(
            "unity", "ProjectAegis", "Assets", "UI", "OsintStaging", "OsintStagingPanel.uxml");
        var uss = UiIaSourceReader.ReadUnder(
            "unity", "ProjectAegis", "Assets", "UI", "OsintStaging", "OsintStagingPanel.uss");

        foreach (var elementName in new[]
        {
            "osint-staging-root",
            "status-line",
            "proposal-list",
            "logonly-list",
            "osint-detail-card",
            "osint-detail-title",
            "osint-detail-confidence",
            "osint-detail-trl",
            "osint-detail-citation-url",
            "osint-detail-citation-snippet",
            "approve-selected",
            "reject-selected",
            "refresh-button",
        })
        {
            Assert.That(uxml, Does.Contain($"name=\"{elementName}\""), $"Missing UXML element {elementName}");
        }

        Assert.That(uss, Does.Contain("../AegisTokens.uss"));
        Assert.That(uss, Does.Contain("var(--focus-ring)"));
    }
}
