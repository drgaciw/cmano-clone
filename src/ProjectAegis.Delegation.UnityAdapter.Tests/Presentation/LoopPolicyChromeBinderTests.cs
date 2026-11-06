using NUnit.Framework;
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
}
