using NUnit.Framework;
using ProjectAegis.Delegation.BdaAssess;
using ProjectAegis.Delegation.CombatEvents;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Delegation.UnityAdapter.Presentation;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

public sealed class CombatDetailPresenterTests
{
    [Test]
    public void Refusal_explains_ammo_and_next_action_without_granting_permission()
    {
        var events = new CombatEventSnapshot(new[] {
            new CombatEvent(CombatEventPhase.AuthorizationRefused, "s", "t", "Missile", "NO_AMMO", 12, 4, 4, "abort:NO_AMMO") });
        var result = CombatDetailPresenter.Build(events, SliceAContactFrame.Empty, BdaAssessSnapshot.Empty,
            "s", "t", 12, null);
        Assert.That(result.StatusLine, Does.Contain("AuthorizationRefused"));
        Assert.That(result.HardConstraintsLine, Does.Contain("Magazine"));
        Assert.That(result.NextActionLine, Does.Contain("Reload"));
        Assert.That(result.FiringSolutionLine, Does.Contain("UNKNOWN"));
        Assert.That(result.BdaLine, Does.Contain("UNKNOWN"));
    }

    [Test]
    public void Other_shooter_same_correlation_never_supplies_selected_explanation()
    {
        var events = new CombatEventSnapshot(new[] {
            new CombatEvent(CombatEventPhase.AuthorizationRefused, "other", "t", "Gun", "NO_AMMO", 12, 4, 4, "abort:NO_AMMO") });
        Assert.That(CombatDetailPresenter.Build(events, SliceAContactFrame.Empty, BdaAssessSnapshot.Empty,
            "s", "t", 12, null), Is.EqualTo(CombatDetailPresentation.Empty));
    }

    [Test]
    public void Kill_outcome_does_not_fabricate_contact_assessment()
    {
        var events = new CombatEventSnapshot(new[] {
            new CombatEvent(CombatEventPhase.TerminalOutcome, "s", "t", "Laser", "Kill", 3, 4, 4, "outcome:Kill") });
        var result = CombatDetailPresenter.Build(events, SliceAContactFrame.Empty, BdaAssessSnapshot.Empty,
            "s", "t", 3, "contact");
        Assert.That(result.BdaLine, Does.Contain("UNKNOWN"));
        Assert.That(result.PostureLine, Does.Contain("UNKNOWN"));
    }
}
