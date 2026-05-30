using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Orchestration;

[TestFixture]
public sealed class LoopPolicyGateTests
{
    [Test]
    public void ResolvePlayerInfoModel_defaults_to_full_transparency()
    {
        Assert.That(
            LoopPolicyGate.ResolvePlayerInfoModel(null),
            Is.EqualTo(PlayerInfoModel.FullTransparency));
    }

    [TestCase(PersonalityEditPolicy.Anytime, SimulationPhase.Executing, AutonomyLevel.FullAutonomous, true)]
    [TestCase(PersonalityEditPolicy.PlanningOnly, SimulationPhase.Planning, AutonomyLevel.FullAutonomous, true)]
    [TestCase(PersonalityEditPolicy.PlanningOnly, SimulationPhase.Executing, AutonomyLevel.FullAutonomous, false)]
    [TestCase(PersonalityEditPolicy.TieredRebrief, SimulationPhase.Executing, AutonomyLevel.Assisted, true)]
    [TestCase(PersonalityEditPolicy.TieredRebrief, SimulationPhase.Executing, AutonomyLevel.SemiAutonomous, false)]
    public void CanEditPersonality_matrix(
        PersonalityEditPolicy editPolicy,
        SimulationPhase phase,
        AutonomyLevel autonomy,
        bool expectedAllowed)
    {
        var profile = new ScenarioPolicyProfile(
            EffectivePolicy.DefaultFree,
            personalityEditPolicy: editPolicy);

        var verdict = LoopPolicyGate.CanEditPersonality(profile, phase, autonomy);

        Assert.That(verdict.Allowed, Is.EqualTo(expectedAllowed));
    }

    [Test]
    public void CanEditAutonomy_always_allowed()
    {
        var profile = new ScenarioPolicyProfile(
            EffectivePolicy.DefaultFree,
            personalityEditPolicy: PersonalityEditPolicy.PlanningOnly);

        var verdict = LoopPolicyGate.CanEditAutonomy(profile, SimulationPhase.Executing);

        Assert.That(verdict.Allowed, Is.True);
    }
}
