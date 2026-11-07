namespace ProjectAegis.Delegation.UnityAdapter.Tests.Gameplay;

using NUnit.Framework;
using ProjectAegis.Delegation.Gameplay;
using ProjectAegis.Delegation.UnityAdapter.Gameplay;

[TestFixture]
public sealed class EscortManualControlTests
{
    [Test]
    public void Repeated_manual_selection_preserves_the_queued_engagement()
    {
        var mission = new EscortMissionSession(42, EscortMissionDefinition.LoadDefault() with { FriendlyPk = 1 });
        mission.Command("alerts-manual");
        mission.Command("radar-active");
        mission.Command("begin");
        for (var i = 0; i < 420; i++) mission.Step(mission.FixedDeltaSeconds);
        Assert.That(mission.Command("engage").Accepted, Is.True);
        Assert.That(mission.Command("posture-manual").Accepted, Is.True);
        mission.Step(mission.FixedDeltaSeconds);
        Assert.That(mission.Presentation.RoundsRemaining, Is.EqualTo(3));
        Assert.That(mission.Snapshot.PrimaryHostileDestroyed, Is.True);
    }
}
