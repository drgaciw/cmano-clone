using System.Xml.Linq;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

[TestFixture]
public sealed class SensorToShooterHostContractTests
{
    [Test]
    public void Panel_host_consumes_cached_frame_and_fingerprint_not_live_log()
    {
        var source = UiIaSourceReader.ReadRuntime("SensorToShooterPanelHost.cs");
        Assert.That(source, Does.Contain("bridgeHost.LastSliceAContacts"));
        Assert.That(source, Does.Not.Contain("Bridge.Orchestrator"));
        Assert.That(source, Does.Contain("ComputeFingerprint"));
        Assert.That(source, Does.Contain("ReferenceEquals"), "Unchanged frames must not rebuild panel text.");
    }

    [Test]
    public void Panel_layout_exposes_four_link_rows_and_next_action()
    {
        var xml = XDocument.Parse(UiIaSourceReader.ReadUnder(
            "unity", "ProjectAegis", "Assets", "UI", "SensorToShooter", "SensorToShooterPanel.uxml"));
        Assert.That(xml.Descendants().Any(e => e.Name.LocalName == "ScrollView"), Is.True);
        foreach (var name in new[]
        {
            "sts-contact-line", "sts-status-line", "sts-sensor-line", "sts-track-line",
            "sts-targetability-line", "sts-shooter-line", "sts-next-action-line",
        })
        {
            Assert.That(xml.Descendants().Count(e => (string?)e.Attribute("name") == name), Is.EqualTo(1), name);
        }
    }

    [Test]
    public void Scene_builder_wires_sensor_to_shooter_host()
    {
        var builder = UiIaSourceReader.ReadUnder(
            "unity", "ProjectAegis", "Assets", "Editor", "DelegationSmokeSceneBuilder.cs");
        Assert.That(builder, Does.Contain("SensorToShooterPanelHost"));
        Assert.That(builder, Does.Contain("\"SensorToShooter\""));
        Assert.That(builder, Does.Contain("Assets/UI/SensorToShooter/SensorToShooterPanel.uxml"));
    }
}
