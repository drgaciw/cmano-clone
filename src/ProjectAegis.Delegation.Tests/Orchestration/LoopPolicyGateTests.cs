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

    [TestCase(PersonalityEditPolicy.Anytime, SimulationPhase.Executing, true)]
    [TestCase(PersonalityEditPolicy.PlanningOnly, SimulationPhase.Planning, true)]
    [TestCase(PersonalityEditPolicy.PlanningOnly, SimulationPhase.Executing, false)]
    [TestCase(PersonalityEditPolicy.TieredRebrief, SimulationPhase.Planning, true)]
    [TestCase(PersonalityEditPolicy.TieredRebrief, SimulationPhase.Executing, true)]
    public void CanRebriefAgent_matrix(
        PersonalityEditPolicy editPolicy,
        SimulationPhase phase,
        bool expectedAllowed)
    {
        var profile = new ScenarioPolicyProfile(
            EffectivePolicy.DefaultFree,
            personalityEditPolicy: editPolicy);

        var verdict = LoopPolicyGate.CanRebriefAgent(profile, phase);

        Assert.That(verdict.Allowed, Is.EqualTo(expectedAllowed));
        Assert.That(verdict.DenialReason, expectedAllowed ? Is.Null : Is.Not.Empty);
    }

    [Test]
    public void CanRebriefAgent_defaults_to_allow_without_scenario_policy()
    {
        Assert.That(LoopPolicyGate.CanRebriefAgent(null, SimulationPhase.Executing).Allowed, Is.True);
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
