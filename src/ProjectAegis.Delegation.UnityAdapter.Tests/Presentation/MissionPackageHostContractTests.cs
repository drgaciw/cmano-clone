using System.Xml.Linq;
using NUnit.Framework;
using ProjectAegis.Delegation.C2Nodes;
using ProjectAegis.Delegation.UnityAdapter.Presentation;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

[TestFixture]
public sealed class MissionPackageHostContractTests
{
    [Test]
    public void Panel_host_consumes_cached_snapshot_and_fingerprint_not_live_log()
    {
        var source = UiIaSourceReader.ReadRuntime("MissionPackagePanelHost.cs");
        Assert.That(source, Does.Contain("bridgeHost.LastMissionPackage"));
        Assert.That(source, Does.Not.Contain("Bridge.Orchestrator"));
        Assert.That(source, Does.Contain("ComputeFingerprint"));
        Assert.That(source, Does.Contain("ReferenceEquals"), "Unchanged snapshots must not rebuild panel text.");
        Assert.That(source, Does.Contain("MissionPackagePanelBinder.Bind"));
        Assert.That(source, Does.Contain("labels.ActivePackageLine"));
        Assert.That(source, Does.Contain("labels.ElementLines"));
    }

    [Test]
    public void Mission_list_host_binds_package_chrome_from_cached_snapshot()
    {
        var source = UiIaSourceReader.ReadRuntime("MissionListPanelHost.cs");
        Assert.That(source, Does.Contain("bridgeHost.LastMissionPackage"));
        Assert.That(source, Does.Contain("MissionPackagePresenter.Build"));
        Assert.That(source, Does.Contain("mp-element-list"));
        Assert.That(source, Does.Contain("mp-package-list"));
    }

    [Test]
    public void Bridge_host_projects_mission_package_once_per_tick()
    {
        var source = UiIaSourceReader.ReadRuntime("DelegationBridgeHost.cs");
        Assert.That(source, Does.Contain("LastMissionPackage"));
        Assert.That(source, Does.Contain("MissionPackagePresentationSource.Project"));
    }

    [Test]
    public void Scene_builder_wires_mission_package_host()
    {
        var builder = UiIaSourceReader.ReadUnder(
            "unity", "ProjectAegis", "Assets", "Editor", "DelegationSmokeSceneBuilder.cs");
        Assert.That(builder, Does.Contain("MissionPackagePanelHost"));
        Assert.That(builder, Does.Contain("\"MissionPackage\""));
        Assert.That(builder, Does.Contain("Assets/UI/MissionPackage/MissionPackagePanel.uxml"));
    }

    [Test]
    public void Mission_list_layout_exposes_embedded_package_chrome()
    {
        var xml = XDocument.Parse(UiIaSourceReader.ReadUnder(
            "unity", "ProjectAegis", "Assets", "UI", "MissionList", "MissionListPanel.uxml"));
        Assert.That(xml.Descendants().Count(element => (string?)element.Attribute("name") == "mp-package-list"), Is.EqualTo(1));
        Assert.That(xml.Descendants().Count(element => (string?)element.Attribute("name") == "mp-element-list"), Is.EqualTo(1));
    }

    [Test]
    public void Apply_path_binds_presentation_without_bridge()
    {
        var snapshot = MissionPackageProjection.Project(
            new[]
            {
                new PackageDefinition(
                    "pkg-1",
                    "Package One",
                    new[]
                    {
                        new PackageElementDefinition("elem-1", "u1", C2NodeRole.Sensor, "organic-radar"),
                    }),
            });
        var presentation = MissionPackagePresenter.Build(snapshot);
        var labels = MissionPackagePanelBinder.Bind(presentation);
        Assert.That(labels.PackageLines[0], Does.Contain("pkg-1"));
        Assert.That(labels.ElementLines[0], Does.Contain("elem-1"));
    }
}
