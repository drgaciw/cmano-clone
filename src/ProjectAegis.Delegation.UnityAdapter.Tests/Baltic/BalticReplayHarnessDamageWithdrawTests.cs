using ProjectAegis.Data.Catalog;
using ProjectAegis.Delegation.UnityAdapter.Baltic;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Baltic;

[TestFixture]
public sealed class BalticReplayHarnessDamageWithdrawTests
{
    private static InMemoryCatalogReader BalticPatrolWithSeededDamage() =>
        new(
            InMemoryCatalogReader.BalticPatrolFixture().GetSortedSensorBindings(),
            "p0-baltic-fixture+damage",
            CatalogValidationDefaults.BalticPlatforms(),
            damage: [new CatalogPlatformDamage("u1", MaxHp: 100, WithdrawThresholdPct: 25)]);

    [Test]
    public void Damage_withdraw_scenario_emits_DAMAGE_WITHDRAW_RECOMMENDED_with_seeded_catalog()
    {
        var result = BalticReplayHarness.Run(
            7,
            "baltic-patrol-damage-withdraw",
            ticks: 5,
            mvpEngagement: true,
            catalog: BalticPatrolWithSeededDamage());

        Assert.That(result.EngagementCount, Is.GreaterThan(0));
        Assert.That(result.Fingerprint, Does.Contain("DAMAGE_WITHDRAW_RECOMMENDED"));
    }

    [Test]
    public void Legacy_Baltic_catalog_without_damage_rows_does_not_emit_damage_withdraw_abort()
    {
        var result = BalticReplayHarness.Run(
            7,
            "baltic-patrol-damage-withdraw",
            ticks: 5,
            mvpEngagement: true,
            catalog: InMemoryCatalogReader.BalticPatrolFixture());

        Assert.That(result.EngagementCount, Is.GreaterThan(0));
        Assert.That(result.Fingerprint, Does.Not.Contain("DAMAGE_WITHDRAW_RECOMMENDED"));
    }
}