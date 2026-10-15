using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

/// <summary>DRG-319: the new attention host is chrome only. It does not enqueue.</summary>
[TestFixture]
public sealed class JevAttentionPanelHostTests
{
    [Test]
    public void Host_applies_attention_rows_and_disclosure_without_orders()
    {
        var source = UiIaSourceReader.ReadRuntime("JevAttentionPanelHost.cs");

        Assert.That(source, Does.Contain("JevAttentionBinder.Project"));
        Assert.That(source, Does.Contain("JevDisclosurePresentation"));
        Assert.That(source, Does.Contain("jev-disclosure-banner"));
        Assert.That(source, Does.Contain("jev-attention-list"));
        Assert.That(source, Does.Contain("jev-model-pin"));
        Assert.That(source, Does.Contain("ScoreSourceLabel"));
        Assert.That(source, Does.Not.Contain("IOrderSink"));
        Assert.That(source, Does.Not.Contain("ApplyOrder"));
        Assert.That(source, Does.Not.Contain("TryIssue"));
        Assert.That(source, Does.Not.Contain("DelegationBridge"));
        Assert.That(source, Does.Not.Contain("CatalogWriteGate"));
        Assert.That(source, Does.Not.Contain("weapon"));
    }

    [Test]
    public void Layout_has_disclosure_and_no_order_buttons()
    {
        var xml = UiIaSourceReader.ReadUnder(
            "unity", "ProjectAegis", "Assets", "UI", "Jev", "JevAttentionPanel.uxml");
        var uss = UiIaSourceReader.ReadUnder(
            "unity", "ProjectAegis", "Assets", "UI", "Jev", "JevAttentionPanel.uss");

        Assert.That(xml, Does.Contain("name=\"jev-disclosure-banner\""));
        Assert.That(xml, Does.Contain("name=\"jev-attention-list\""));
        Assert.That(xml, Does.Contain("name=\"jev-model-pin\""));
        Assert.That(xml, Does.Not.Contain("ui:Button"));
        Assert.That(xml, Does.Not.Contain("engage"));
        Assert.That(uss, Does.Contain(".jev-attention-row--immediate"));
        Assert.That(uss, Does.Contain(".jev-attention-row--focus"));
        Assert.That(uss, Does.Contain(".jev-attention-row--watch"));
        Assert.That(uss, Does.Contain(".jev-attention-row--low"));
        Assert.That(uss, Does.Contain(".jev-attention-row--ignore"));
        Assert.That(uss, Does.Contain(".jev-disclosure--visible"));
        Assert.That(uss, Does.Contain(".jev-disclosure--hidden"));
    }
}
