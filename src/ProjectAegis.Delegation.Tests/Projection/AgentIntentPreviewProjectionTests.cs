using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

/// <summary>Req 20 P0: Assisted intent-preview ghost visibility rules.</summary>
[TestFixture]
public sealed class AgentIntentPreviewProjectionTests
{
    [Test]
    public void Project_shows_ghost_for_assisted_agent()
    {
        var state = new DelegationStateProjection(
            "u1",
            DelegationOwnerKind.Agent,
            AutonomyLevel.Assisted,
            PersonalityId: "cautious",
            Paused: false);

        var preview = AgentIntentPreviewProjection.Project(state);

        Assert.That(preview.ShowGhost, Is.True);
        Assert.That(preview.Detail, Is.EqualTo("ASSISTED_INTENT"));
    }

    [Test]
    public void Project_hides_ghost_when_paused_or_not_assisted()
    {
        var paused = new DelegationStateProjection(
            "u1", DelegationOwnerKind.Agent, AutonomyLevel.Assisted, "", Paused: true);
        var fullAuto = new DelegationStateProjection(
            "u1", DelegationOwnerKind.Agent, AutonomyLevel.FullAutonomous, "", Paused: false);
        var human = new DelegationStateProjection(
            "u1", DelegationOwnerKind.Human, AutonomyLevel.Assisted, "", Paused: false);

        Assert.That(AgentIntentPreviewProjection.Project(paused).ShowGhost, Is.False);
        Assert.That(AgentIntentPreviewProjection.Project(fullAuto).ShowGhost, Is.False);
        Assert.That(AgentIntentPreviewProjection.Project(human).ShowGhost, Is.False);
    }

    [Test]
    public void Project_uses_engage_preview_detail_when_present()
    {
        var state = new DelegationStateProjection(
            "u1", DelegationOwnerKind.Agent, AutonomyLevel.Assisted, "", Paused: false);
        var engage = new EngagePreview("DLZ: In (Nominal)", CanFire: true, AbortPreviewCode: null);

        var preview = AgentIntentPreviewProjection.Project(state, engage);

        Assert.That(preview.ShowGhost, Is.True);
        Assert.That(preview.Detail, Is.EqualTo("DLZ: In (Nominal)"));
    }
}
