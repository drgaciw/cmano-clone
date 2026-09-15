using System.Xml.Linq;
using NUnit.Framework;
using ProjectAegis.Delegation.SensorToShooter;
using ProjectAegis.Delegation.UnityAdapter.Presentation;

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
        Assert.That(source, Does.Contain("SensorToShooterPanelBinder.Bind"));
        Assert.That(source, Does.Contain("labels.TargetIdLine"));
        Assert.That(source, Does.Contain("labels.ObserverIdLine"));
    }

    [Test]
    public void Panel_binder_exposes_all_lines_for_host_bind()
    {
        var presentation = SensorToShooterPresenter.Build(
            "c1",
            Chain(),
            eligibilityAvailable: true);
        var labels = SensorToShooterPanelBinder.Bind(presentation);

        Assert.That(labels.ContactIdLine, Does.Contain("c1"));
        Assert.That(labels.TargetIdLine, Does.Contain("target-1"));
        Assert.That(labels.ObserverIdLine, Does.Contain("sensor-1"));
        Assert.That(labels.StatusLine, Does.Contain("COMPLETE"));
        Assert.That(labels.SensorLine, Does.Contain("LINKED").And.Contain("sensor-1"));
        Assert.That(labels.TrackLine, Does.Contain("LINKED").And.Contain("c1"));
        Assert.That(labels.TargetabilityLine, Does.Contain("LINKED"));
        Assert.That(labels.ShooterLine, Does.Contain("LINKED").And.Contain("shooter-1"));
        Assert.That(labels.NextActionLine, Does.Contain("does not issue fire orders"));
    }

    private static SensorToShooterSnapshot Chain() =>
        new(new[]
        {
            new SensorToShooterChain(
                "c1",
                "target-1",
                "sensor-1",
                true,
                SensorToShooterBreakCause.None,
                Enum.GetValues<SensorToShooterLinkKind>().Select(kind => new SensorToShooterChainLink(
                    kind,
                    true,
                    SensorToShooterBreakCause.None,
                    kind == SensorToShooterLinkKind.EligibleShooter ? "shooter-1"
                        : kind == SensorToShooterLinkKind.Track ? "c1"
                        : "sensor-1",
                    "c1",
                    "target-1",
                    "fact")).ToArray()),
        });

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
