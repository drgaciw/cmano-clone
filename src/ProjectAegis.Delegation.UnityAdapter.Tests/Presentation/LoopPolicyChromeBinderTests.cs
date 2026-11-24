using NUnit.Framework;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Traits;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

[TestFixture]
public sealed class LoopPolicyChromeBinderTests
{
    [TestCase(PersonalityEditPolicy.Anytime, "EDITS: ANYTIME", "c2-topbar-item--edit-anytime")]
    [TestCase(PersonalityEditPolicy.PlanningOnly, "EDITS: PLANNING ONLY", "c2-topbar-item--edit-planning")]
    [TestCase(PersonalityEditPolicy.TieredRebrief, "EDITS: TIERED REBRIEF", "c2-topbar-item--edit-tiered")]
    public void Edit_policy_maps_to_readable_label(PersonalityEditPolicy policy, string label, string css)
    {
        var p = LoopPolicyChromeBinder.BindEditPolicy(policy);
        Assert.That(p.Label, Is.EqualTo(label));
        Assert.That(p.CssClass, Is.EqualTo(css));
    }

    [Test]
    public void Unknown_edit_policy_maps_to_placeholder()
    {
        Assert.That(LoopPolicyChromeBinder.BindEditPolicy((PersonalityEditPolicy)99).Label, Is.EqualTo("EDITS: —"));
    }

    [Test]
    public void Profile_binds_both_fields_and_is_deterministic()
    {
        var profile = new ScenarioPolicyProfile(
            EffectivePolicy.DefaultFree,
            playerInfoModel: PlayerInfoModel.DelegationFog,
            personalityEditPolicy: PersonalityEditPolicy.PlanningOnly);
        var a = LoopPolicyChromeBinder.Bind(profile);
        var b = LoopPolicyChromeBinder.Bind(profile);
        Assert.That(a.Fog.Label, Is.EqualTo("FOG: DELEGATION"));
        Assert.That(a.EditPolicy.Label, Is.EqualTo("EDITS: PLANNING ONLY"));
        Assert.That(b, Is.EqualTo(a));
    }

    [Test]
    public void Default_profile_binds_full_picture_and_anytime()
    {
        var p = LoopPolicyChromeBinder.Bind(new ScenarioPolicyProfile(EffectivePolicy.DefaultFree));
        Assert.That(p.Fog.Label, Is.EqualTo("FOG: FULL PICTURE"));
        Assert.That(p.EditPolicy.Label, Is.EqualTo("EDITS: ANYTIME"));
    }

    [Test]
    public void Project_from_bridge_reads_scenario_policy_and_updates_on_rebind()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: false);
        var initial = LoopPolicyChromeBinder.Project(bridge);
        Assert.That(initial.Fog.Label, Is.EqualTo("FOG: FULL PICTURE"));
        Assert.That(initial.EditPolicy.Label, Is.EqualTo("EDITS: ANYTIME"));

        bridge.Orchestrator.ScenarioPolicy = new ScenarioPolicyProfile(
            EffectivePolicy.DefaultFree,
            playerInfoModel: PlayerInfoModel.TieredByAutonomy,
            personalityEditPolicy: PersonalityEditPolicy.TieredRebrief);

        var rebound = LoopPolicyChromeBinder.Project(bridge);
        Assert.That(rebound.Fog.Label, Is.EqualTo("FOG: TIERED"));
        Assert.That(rebound.Fog.CssClass, Is.EqualTo("c2-topbar-item--fog-tiered"));
        Assert.That(rebound.EditPolicy.Label, Is.EqualTo("EDITS: TIERED REBRIEF"));
        Assert.That(rebound.EditPolicy.CssClass, Is.EqualTo("c2-topbar-item--edit-tiered"));
    }

    [Test]
    public void Project_from_null_bridge_falls_back_to_default_policy_labels()
    {
        var projected = LoopPolicyChromeBinder.Project(null);
        Assert.That(projected.Fog.Label, Is.EqualTo("FOG: FULL PICTURE"));
        Assert.That(projected.EditPolicy.Label, Is.EqualTo("EDITS: ANYTIME"));
        Assert.That(projected.ShowDualSideTestSandboxBanner, Is.False);
        Assert.That(projected.TestSandboxBannerLabel, Is.Empty);
    }

    [Test]
    public void Test_sandbox_banner_shown_only_for_dual_side_policy_in_mixed_mode()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: false, scenarioPolicyId: "test-sandbox-dual-side");
        var friendly = bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
        var opposing = bridge.Registry.RegisterUnit(new EntityKey(2), "hostile-1");
        bridge.ConfigureSimulationMode(
            new SimulationModeProfile(SimulationModeKind.Mixed, PlayerControlsFriendlySide: true),
            [friendly.Target],
            [opposing.Target],
            PersonalityCatalog.All[0].Traits);

        var projected = LoopPolicyChromeBinder.Project(bridge);

        Assert.That(projected.ShowDualSideTestSandboxBanner, Is.True);
        Assert.That(projected.TestSandboxBannerLabel, Is.EqualTo(LoopPolicyChromeBinder.TestSandboxBannerText));
        Assert.That(projected.TestSandboxBannerLabel, Does.Contain("TEST SANDBOX"));
    }

    [TestCase(SimulationModeKind.Human)]
    [TestCase(SimulationModeKind.AgentVsAgent)]
    public void Test_sandbox_banner_hidden_for_dual_side_policy_in_non_mixed_modes(SimulationModeKind kind)
    {
        var bridge = new DelegationBridge(42, mvpEngagement: false, scenarioPolicyId: "test-sandbox-dual-side");
        var friendly = bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
        var opposing = bridge.Registry.RegisterUnit(new EntityKey(2), "hostile-1");
        bridge.ConfigureSimulationMode(
            new SimulationModeProfile(kind, PlayerControlsFriendlySide: true),
            [friendly.Target],
            [opposing.Target],
            PersonalityCatalog.All[0].Traits);

        var projected = LoopPolicyChromeBinder.Project(bridge);

        Assert.That(projected.ShowDualSideTestSandboxBanner, Is.False);
        Assert.That(projected.TestSandboxBannerLabel, Is.Empty);
    }

    [Test]
    public void Test_sandbox_banner_hidden_for_non_sandbox_policy_in_mixed_mode()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: false, scenarioPolicyId: "baltic-patrol");
        var friendly = bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
        var opposing = bridge.Registry.RegisterUnit(new EntityKey(2), "hostile-1");
        bridge.ConfigureSimulationMode(
            new SimulationModeProfile(SimulationModeKind.Mixed, PlayerControlsFriendlySide: true),
            [friendly.Target],
            [opposing.Target],
            PersonalityCatalog.All[0].Traits);

        var projected = LoopPolicyChromeBinder.Project(bridge);

        Assert.That(projected.ShowDualSideTestSandboxBanner, Is.False);
        Assert.That(projected.TestSandboxBannerLabel, Is.Empty);
    }

    [Test]
    public void Test_sandbox_banner_hidden_when_dual_side_policy_mode_is_unconfigured()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: false, scenarioPolicyId: "test-sandbox-dual-side");

        var projected = LoopPolicyChromeBinder.Project(bridge);

        Assert.That(projected.ShowDualSideTestSandboxBanner, Is.False);
        Assert.That(projected.TestSandboxBannerLabel, Is.Empty);
    }
}
