using System.Xml.Linq;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

[TestFixture]
public sealed class AirOpsReadyHostContractTests
{
    [Test]
    public void Air_ops_panel_host_binds_ready_aggregate_with_fingerprint()
    {
        var source = UiIaSourceReader.ReadRuntime("AirOpsPanelHost.cs");
        Assert.That(source, Does.Contain("AirOpsReadyAggregateBinder.Bind"));
        Assert.That(source, Does.Contain("AirOpsReadyCueClasses"));
        Assert.That(source, Does.Contain("ApplyReadyAggregateCueClass"));
        Assert.That(source, Does.Contain("_lastBindFingerprint"));
        Assert.That(source, Does.Contain("_readyLabels.Fingerprint"));
        Assert.That(source, Does.Contain("air-ops-ready-line"));
    }

    [Test]
    public void Air_ops_panel_layout_declares_ready_summary_line()
    {
        var xml = XDocument.Parse(UiIaSourceReader.ReadUnder(
            "unity", "ProjectAegis", "Assets", "UI", "AirOps", "AirOpsPanel.uxml"));
        Assert.That(
            xml.Descendants().Count(e => (string?)e.Attribute("name") == "air-ops-ready-line"),
            Is.EqualTo(1));
    }
}
