namespace ProjectAegis.Delegation.UnityAdapter.Tests.Baltic;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Delegation.UnityAdapter.Baltic;
using ProjectAegis.Sim.Catalog;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

/// <summary>
/// S30-08 isolated hot-tick Hit → HP ledger fixture — not in ReplayGolden 6/6 catalog.
/// </summary>
[TestFixture]
public sealed class BalticReplayHarnessHotTickHitTests
{
    private const string PolicyId = "baltic-patrol-combat-domains-hot-tick-damage";
    private const int Seed = 42;
    private const int Ticks = 4;

    private static InMemoryCatalogReader BalticPatrolWithSeededDamage() =>
        new(
            InMemoryCatalogReader.BalticPatrolFixture().GetSortedSensorBindings(),
            "p0-baltic-fixture+hot-tick-hit",
            CatalogValidationDefaults.BalticPlatforms(),
            damage: [new CatalogPlatformDamage("u1", MaxHp: 100, WithdrawThresholdPct: 25)]);

    [Test]
    public void Policy_loads_combat_domains_and_pkKill_zero_for_hit_path()
    {
        ScenarioPolicyRepository.EnsureDefaultJsonLoaded();
        var profile = ScenarioPolicyRepository.TryGet(PolicyId);

        Assert.That(profile, Is.Not.Null);
        Assert.That(profile!.EngageDefaults, Is.Not.Null);
        Assert.That(profile.EngageDefaults!.CombatDomainsEnabled, Is.True);
        Assert.That(profile.EngageDefaults.PkKill, Is.EqualTo(0.0));
        Assert.That(profile.CatalogWithdrawTargets, Has.Count.EqualTo(1));
    }

    [Test]
    public void Policy_is_not_in_replay_golden_regression_catalog()
    {
        var catalogPolicyIds = ReplayGoldenRegressionCatalog.All.Select(c => c.PolicyId).ToArray();

        Assert.That(catalogPolicyIds, Does.Not.Contain(PolicyId));
        Assert.That(catalogPolicyIds, Has.Length.EqualTo(6));
    }

    [Test]
    public void Hot_tick_hit_fixture_replay_is_deterministic()
    {
        var catalog = BalticPatrolWithSeededDamage();
        var a = BalticReplayHarness.Run(Seed, PolicyId, Ticks, catalog: catalog);
        var b = BalticReplayHarness.Run(Seed, PolicyId, Ticks, catalog: catalog);

        Assert.That(b.Fingerprint, Is.EqualTo(a.Fingerprint));
        Assert.That(b.WorldHash, Is.EqualTo(a.WorldHash));
        Assert.That(b.DetectionWorldHash, Is.EqualTo(a.DetectionWorldHash));
        Assert.That(b.FingerprintSha256, Is.EqualTo(a.FingerprintSha256));
    }

    [Test]
    public void Hot_tick_hit_fixture_emits_platform_damage_change_rows()
    {
        var result = BalticReplayHarness.Run(
            Seed,
            PolicyId,
            Ticks,
            catalog: BalticPatrolWithSeededDamage());

        Assert.That(result.Fingerprint, Does.Contain("PlatformDamageChange|"));
        Assert.That(result.Fingerprint, Does.Contain(PlatformDamageChangeReasonCodes.AmbientTick));
        Assert.That(
            result.Fingerprint,
            Does.Contain(PlatformDamageChangeReasonCodes.Hit).Or.Contain(EngagementOutcomeCodes.Kill));
    }
}