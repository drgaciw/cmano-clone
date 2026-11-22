using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

/// <summary>
/// DRG-773 &amp; DRG-717: C2TopBarPanelHost must consume fog-mode and loop-policy chrome
/// purely through headless projections — zero ProjectAegis.Sim.Scenario imports and zero
/// direct Orchestrator.ScenarioPolicy / PlayerInfoModel reads (ADR-010 §2–3).
/// </summary>
[TestFixture]
public sealed class C2TopBarLoopPolicyHostContractTests
{
    [Test]
    public void Top_bar_host_has_no_sim_scenario_import_or_direct_scenario_policy_read()
    {
        var source = UiIaSourceReader.ReadRuntime("C2TopBarPanelHost.cs");

        Assert.That(source, Does.Not.Contain("ProjectAegis.Sim.Scenario"),
            "DRG-773: C2TopBarPanelHost.cs must not import ProjectAegis.Sim.Scenario.");
        Assert.That(source, Does.Not.Contain("ScenarioPolicy"),
            "DRG-773: C2TopBarPanelHost.cs must not access ScenarioPolicy directly.");
        Assert.That(source, Does.Not.Contain("PlayerInfoModel"),
            "DRG-773: C2TopBarPanelHost.cs must not reference Sim PlayerInfoModel directly.");
        Assert.That(source, Does.Not.Contain("Orchestrator"),
            "DRG-773: C2TopBarPanelHost.cs must not reach into Bridge.Orchestrator directly.");
    }

    [Test]
    public void Top_bar_host_binds_loop_policy_chrome_via_projection()
    {
        var source = UiIaSourceReader.ReadRuntime("C2TopBarPanelHost.cs");

        Assert.That(source, Does.Contain("LoopPolicyChromeBinder.Project"),
            "DRG-773/717: C2TopBarPanelHost must project loop policy through LoopPolicyChromeBinder.");
        Assert.That(source, Does.Contain("LastLoopPolicyChrome"),
            "DRG-717: C2TopBarPanelHost must expose LastLoopPolicyChrome.");
        Assert.That(source, Does.Contain("edit-policy-label"),
            "DRG-717: C2TopBarPanelHost must bind the edit-policy-label element.");
    }

    [Test]
    public void Top_bar_uxml_and_uss_declare_edit_policy_label_and_classes()
    {
        var uxml = UiIaSourceReader.ReadUnder(
            "unity", "ProjectAegis", "Assets", "UI", "TopBar", "C2TopBarPanel.uxml");
        var uss = UiIaSourceReader.ReadUnder(
            "unity", "ProjectAegis", "Assets", "UI", "TopBar", "C2TopBarPanel.uss");

        Assert.That(uxml, Does.Contain("name=\"fog-mode-label\""));
        Assert.That(uxml, Does.Contain("name=\"edit-policy-label\""));
        Assert.That(uss, Does.Contain(".c2-topbar-item--edit"));
        Assert.That(uss, Does.Contain(".c2-topbar-item--edit-anytime"));
        Assert.That(uss, Does.Contain(".c2-topbar-item--edit-planning"));
        Assert.That(uss, Does.Contain(".c2-topbar-item--edit-tiered"));
        Assert.That(uss, Does.Contain(".c2-topbar-item--edit-unknown"));
    }
}
