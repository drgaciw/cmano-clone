using System.Xml.Linq;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

[TestFixture]
public sealed class DomainAbortExplainHostContractTests
{
    [Test]
    public void Engage_explain_host_binds_domain_abort_with_fingerprint()
    {
        var source = UiIaSourceReader.ReadRuntime("EngageExplainPanelHost.cs");
        Assert.That(source, Does.Contain("DomainAbortExplainBinder.Bind"));
        Assert.That(source, Does.Contain("DomainAbortExplainBinder.BindRows"));
        Assert.That(source, Does.Contain("DomainAbortCueClasses"));
        Assert.That(source, Does.Contain("_lastDomainAbortFingerprint"));
        Assert.That(source, Does.Contain("engage-explain-domain-abort"));
        Assert.That(source, Does.Contain("LastDomainAbort"));
        Assert.That(source, Does.Not.Contain("DelegationBridge.Tick"));
    }

    [Test]
    public void Engage_explain_layout_declares_domain_abort_line()
    {
        var xml = XDocument.Parse(UiIaSourceReader.ReadUnder(
            "unity", "ProjectAegis", "Assets", "UI", "EngageExplain", "EngageExplainPanel.uxml"));
        Assert.That(
            xml.Descendants().Count(e => (string?)e.Attribute("name") == "engage-explain-domain-abort"),
            Is.EqualTo(1));
    }

    [Test]
    public void Engage_explain_styles_declare_blocked_and_clear_cues()
    {
        var uss = UiIaSourceReader.ReadUnder(
            "unity", "ProjectAegis", "Assets", "UI", "EngageExplain", "EngageExplainPanel.uss");
        Assert.That(uss, Does.Contain("domain-abort-cue--blocked"));
        Assert.That(uss, Does.Contain("domain-abort-cue--clear"));
        Assert.That(uss, Does.Contain("domain-abort-cue--unknown"));
        Assert.That(uss, Does.Contain(".engage-explain-domain-abort"));
    }
}
