namespace ProjectAegis.Delegation.UnityAdapter.Tests.Gameplay;

using NUnit.Framework;
using ProjectAegis.Delegation.Gameplay;
using ProjectAegis.Delegation.UnityAdapter.Gameplay;

[TestFixture]
public sealed class EscortMissionFeedbackTests
{
    [Test]
    public void Engaging_a_confirmed_destroyed_contact_reports_destruction_instead_of_requesting_a_track()
    {
        var mission = new EscortMissionSession(42, EscortMissionDefinition.LoadDefault() with { FriendlyPk = 1 });
        mission.Command("alerts-manual");
        mission.Command("radar-active");
        mission.Command("begin");
        for (var i = 0; i < 420; i++)
        {
            mission.Step(mission.FixedDeltaSeconds);
        }

        Assert.That(mission.Command("engage").Accepted, Is.True);
        mission.Step(mission.FixedDeltaSeconds);
        Assert.That(mission.Snapshot.PrimaryHostileDestroyed, Is.True);
        var contact = mission.MapSymbols.Single(s => s.SymbolId == EscortMissionSession.ThreatContactId);
        Assert.That(contact.IsDestroyed, Is.True);
        Assert.That(contact.Affiliation, Is.EqualTo("Hostile"), "A confirmed kill must retain its known hostile identity.");
        var result = mission.Command("engage");
        Assert.That(result.Accepted, Is.False);
        Assert.That(result.Message, Does.Contain("already confirmed destroyed"));
    }
}
