using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

[TestFixture]
public sealed class BdaContactDamageStatesTests
{
    [TestCase(0, null)]
    [TestCase(1, BdaContactDamageStates.DegradedL1)]
    [TestCase(2, BdaContactDamageStates.DegradedL2)]
    [TestCase(3, BdaContactDamageStates.Lost)]
    public void CombatDomain_Bda_damage_level_maps_to_contact_status(int damageLevel, string? expected)
    {
        Assert.That(BdaContactDamageStates.MapDamageLevel(damageLevel), Is.EqualTo(expected));
    }

    [Test]
    public void CombatDomain_Bda_damage_level_rank_escalates_monotonically()
    {
        Assert.That(
            BdaContactDamageStates.Rank(BdaContactDamageStates.DegradedL1),
            Is.LessThan(BdaContactDamageStates.Rank(BdaContactDamageStates.DegradedL2)));
        Assert.That(
            BdaContactDamageStates.Rank(BdaContactDamageStates.DegradedL2),
            Is.LessThan(BdaContactDamageStates.Rank(BdaContactDamageStates.Lost)));
    }
}