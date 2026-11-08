using ProjectAegis.Sim.Catalog;
using Xunit;

namespace ProjectAegis.Sim.Tests.Catalog;

public sealed class CombatDamageLevelTests
{
    [Theory]
    [InlineData(1.0, 1.0, 1)]
    [InlineData(1.0, 2.0, 2)]
    [InlineData(0.5, 1.0, 0)]
    [InlineData(0.75, 2.0, 1)]
    [InlineData(1.0, 3.0, 2)]
    [InlineData(1.5, 2.0, 2)]
    public void ComputeLevel_clamps_to_gdd_formula(double hitSeverity, double resilience, int expected)
    {
        Assert.Equal(expected, CombatDamageLevel.ComputeLevel(hitSeverity, resilience));
    }

    [Fact]
    public void Zero_resilience_produces_zero_damage_level()
    {
        Assert.Equal(0, CombatDamageLevel.ComputeLevel(1.0, 0.0));
        Assert.Equal(0.0, CombatDamageLevel.HitHpDeltaPct(0));
    }

    [Theory]
    [InlineData(0, 0.0)]
    [InlineData(1, 25.0)]
    [InlineData(2, 50.0)]
    [InlineData(3, 75.0)]
    public void HitHpDeltaPct_maps_levels_to_bounded_hp_loss(int damageLevel, double expectedDelta)
    {
        Assert.Equal(expectedDelta, CombatDamageLevel.HitHpDeltaPct(damageLevel));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void CombatDomain_Bda_damage_level_stays_within_hot_path_envelope(int damageLevel)
    {
        Assert.InRange(damageLevel, 0, CombatDamageLevel.MaxLevel);
        Assert.True(CombatDamageLevel.HitHpDeltaPct(damageLevel) >= 0.0);
    }
}