using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

public sealed class DelegationBadgeAndPolicyHudTests
{
    [Test]
    public void DelegationBadge_bind_and_apply_human_agent()
    {
        var human = DelegationBadgeApplyState.Apply(
            DelegationBadgePanelBinder.Bind("DIRECT", isHumanControlled: true));
        Assert.That(human.BadgeLabel, Is.EqualTo("DELEGATION: DIRECT"));
        Assert.That(human.StateCssClass, Is.EqualTo("delegation-badge--human"));

        var agent = DelegationBadgeApplyState.Apply(
            DelegationBadgePanelBinder.Bind("AUTO", isHumanControlled: false));
        Assert.That(agent.StateCssClass, Is.EqualTo("delegation-badge--agent"));
    }

    [Test]
    public void PolicyEmcon_bind_and_apply_denied()
    {
        var bound = PolicyEmconHudPanelBinder.Bind("HOLD FIRE", "SILENT", isDenied: true);
        var applied = PolicyEmconHudApplyState.Apply(bound);

        Assert.That(applied.PolicyLine, Is.EqualTo("POLICY: HOLD FIRE"));
        Assert.That(applied.EmconLine, Is.EqualTo("EMCON: SILENT"));
        Assert.That(applied.StateCssClass, Is.EqualTo("policy-emcon-hud--denied"));
    }

    [Test]
    public void Idle_binders_are_stable()
    {
        Assert.That(DelegationBadgePanelBinder.BindIdle().StateCssClass, Does.Contain("idle"));
        Assert.That(PolicyEmconHudPanelBinder.BindIdle().PolicyLine, Is.EqualTo("POLICY: —"));
    }
}
