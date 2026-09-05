using NUnit.Framework;
using ProjectAegis.Delegation.UnityAdapter.Bridge;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Bridge;

public sealed class CombatScenarioFamilyBindingTests
{
    [TestCase("slice-b-missile", "Missile")]
    [TestCase("slice-b-gun", "Gun")]
    [TestCase("slice-b-laser", "Laser")]
    public void Normal_bridge_session_preserves_explicit_scenario_weapon_family(string policy, string family)
    {
        var bridge = new DelegationBridge(7, scenarioPolicyId: policy, mvpEngagement: true);
        Assert.That(bridge.Session!.CombatWeaponFamilyId, Is.EqualTo(family));
    }
}
