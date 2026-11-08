using NUnit.Framework;
using ProjectAegis.Delegation.Orchestration;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Bridge;

public sealed class CombatIndependentScenarioBindingTests
{
    [TestCase("slice-b-gun", "Gun")]
    [TestCase("slice-b-laser", "Laser")]
    public void Explicit_scenario_supplies_family_without_orchestrator_profile(string policy, string family)
    {
        var session = SimulationSession.BindMvpEngagementForScenario(new DelegationOrchestrator(7), policy);
        Assert.That(session.CombatWeaponFamilyId, Is.EqualTo(family));
    }
}
