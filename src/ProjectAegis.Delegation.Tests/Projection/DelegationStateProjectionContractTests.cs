using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

/// <summary>Req 20 P0 / ADR-019: D2 projection + command DTOs compile and carry fields.</summary>
[TestFixture]
public sealed class DelegationStateProjectionContractTests
{
    [Test]
    public void Projection_and_commands_are_immutable_records()
    {
        var state = new DelegationStateProjection(
            "u1",
            DelegationOwnerKind.Agent,
            AutonomyLevel.Assisted,
            PersonalityId: "aggressive",
            Paused: true);

        Assert.That(state.UnitId, Is.EqualTo("u1"));
        Assert.That(state.Owner, Is.EqualTo(DelegationOwnerKind.Agent));
        Assert.That(state.Paused, Is.True);

        var pause = new AgentPauseRequested("u1", SimTime: 1.0);
        var resume = new AgentResumeRequested("u1", SimTime: 2.0);
        var autonomy = new AutonomyLevelChangeRequested("u1", AutonomyLevel.SemiAutonomous, SimTime: 3.0);

        Assert.That(pause.UnitId, Is.EqualTo("u1"));
        Assert.That(resume.UnitId, Is.EqualTo("u1"));
        Assert.That(autonomy.AutonomyLevel, Is.EqualTo(AutonomyLevel.SemiAutonomous));
    }
}
