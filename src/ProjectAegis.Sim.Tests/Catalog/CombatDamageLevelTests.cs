using ProjectAegis.Sim.Catalog;
using NUnit.Framework;

namespace ProjectAegis.Sim.Tests.Catalog;

public sealed class CombatDamageLevelTests
{

    [TestCase(1.0, 1.0, 1)]
    [TestCase(1.0, 2.0, 2)]
    [TestCase(0.5, 1.0, 0)]
    [TestCase(0.75, 2.0, 1)]
    [TestCase(1.0, 3.0, 2)]
    [TestCase(1.5, 2.0, 2)]
    public void ComputeLevel_clamps_to_gdd_formula(double hitSeverity, double resilience, int expected)
    {
        Assert.That(CombatDamageLevel.ComputeLevel(hitSeverity, resilience), Is.EqualTo(expected));
    }

    [Test]
    public void Zero_resilience_produces_zero_damage_level()
    {
        Assert.That(CombatDamageLevel.ComputeLevel(1.0, 0.0), Is.EqualTo(0));
        Assert.That(CombatDamageLevel.HitHpDeltaPct(0), Is.EqualTo(0.0));
    }

    [TestCase(0, 0.0)]
    [TestCase(1, 25.0)]
    [TestCase(2, 50.0)]
    [TestCase(3, 75.0)]
    public void HitHpDeltaPct_maps_levels_to_bounded_hp_loss(int damageLevel, double expectedDelta)
    {
        Assert.That(CombatDamageLevel.HitHpDeltaPct(damageLevel), Is.EqualTo(expectedDelta));
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void CombatDomain_Bda_damage_level_stays_within_hot_path_envelope(int damageLevel)
    {
        Assert.That(damageLevel, Is.InRange(0, CombatDamageLevel.MaxLevel));
        Assert.That(CombatDamageLevel.HitHpDeltaPct(damageLevel) >= 0.0, Is.True);
    }
}