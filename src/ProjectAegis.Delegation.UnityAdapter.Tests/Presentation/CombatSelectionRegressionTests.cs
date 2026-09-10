using NUnit.Framework;
using ProjectAegis.Delegation.BdaAssess;
using ProjectAegis.Delegation.CombatEvents;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Delegation.UnityAdapter.Presentation;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

public sealed class CombatSelectionRegressionTests
{
    [Test]
    public void Fractional_replay_cutoff_rejects_later_contact_frame_in_same_tick()
    {
        var contacts = SliceAContactFrame.Empty with { SimTick = 5, SimTime = 5.9 };
        var frame = CombatPresentationFrameBridge.Build(new DecisionLog(), contacts, 5.1);
        Assert.That(frame.Contacts, Is.SameAs(SliceAContactFrame.Empty));
        Assert.That(CombatPresentationFrameBridge.Build(new DecisionLog(), contacts, 5.9).Contacts,
            Is.SameAs(contacts));
    }

    [Test]
    public void Contact_assessment_without_engagement_is_visible_only_for_its_observer()
    {
        var contacts = SliceAContactFrame.Empty with
        {
            Contacts = new[] {
                new ContactPictureEntry("c1", "target", "observer1", "Identified", 5, 5),
                new ContactPictureEntry("c2", "target", "observer2", "Identified", 5, 5) },
        };
        var frame = new CombatPresentationFrame(CombatEventSnapshot.Empty, contacts,
            new BdaAssessSnapshot(new[] {
                new BdaAssessContactState("c1", "target", "observer1", BdaAssessStateKind.Damaged,
                    BdaAssessSourceKind.PlatformDamage, 5, 5, 1) }), 5);
        var assessed = CombatSelectionPresenter.Build(frame, null, null, "c1");
        Assert.That(assessed.BdaLine, Does.Contain("Damaged").And.Contain("PlatformDamage"));
        Assert.That(assessed.StatusLine, Does.Contain("no recorded engagement"));
        Assert.That(CombatSelectionPresenter.Build(frame, null, null, "c2").BdaLine, Does.Contain("UNKNOWN"));
    }

    [Test]
    public void Inspecting_and_clearing_combat_history_preserves_command_selection()
    {
        var controller = new C2PresentationController();
        controller.SelectFriendlyUnit("command-unit");
        controller.CombatInspection.Select("an-engagement-key");
        Assert.That(controller.SelectedUnitId, Is.EqualTo("command-unit"));
        controller.CombatInspection.Select(null);
        Assert.That(controller.SelectedUnitId, Is.EqualTo("command-unit"));
        Assert.That(controller.SelectedContactId, Is.Null);
    }
}
