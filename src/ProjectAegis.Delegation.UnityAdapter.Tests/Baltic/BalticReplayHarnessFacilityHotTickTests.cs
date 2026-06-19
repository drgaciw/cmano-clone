namespace ProjectAegis.Delegation.UnityAdapter.Tests.Baltic;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Delegation.UnityAdapter.Baltic;
using ProjectAegis.Sim.Catalog;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

/// <summary>
/// S31-05 isolated facility hot-tick Hit → HP ledger fixture — not in ReplayGolden 6/6 catalog.
/// </summary>
[TestFixture]
public sealed class BalticReplayHarnessFacilityHotTickTests
{
    private const string PolicyId = "baltic-patrol-combat-domains-facility-hot-tick";
    private const int Seed = 42;
    private const int Ticks = 4;

    private static InMemoryCatalogReader BalticPatrolWithFacilityDamage() =>
        new(
            InMemoryCatalogReader.BalticPatrolFixture().GetSortedSensorBindings(),
            "p0-baltic-fixture+facility-hot-tick",
            CatalogValidationDefaults.BalticPlatforms(),
            damage: [new CatalogPlatformDamage("runway-1", MaxHp: 100, WithdrawThresholdPct: 0)]);

    [Test]
    public void Policy_loads_combat_domains_facility_domain_and_pkKill_zero_for_hit_path()
    {
        ScenarioPolicyRepository.EnsureDefaultJsonLoaded();
        var profile = ScenarioPolicyRepository.TryGet(PolicyId);

        Assert.That(profile, Is.Not.Null);
        Assert.That(profile!.EngageDefaults, Is.Not.Null);
        Assert.That(profile.EngageDefaults!.CombatDomainsEnabled, Is.True);
        Assert.That(profile.EngageDefaults.CombatDomain, Is.EqualTo(CombatDomain.Facility));
        Assert.That(profile.EngageDefaults.PkKill, Is.EqualTo(0.0));
        Assert.That(profile.CatalogWithdrawTargets, Has.Count.EqualTo(1));
        Assert.That(profile.CatalogWithdrawTargets[0].PlatformId, Is.EqualTo("runway-1"));
    }

    [Test]
    public void Policy_is_not_in_replay_golden_regression_catalog()
    {
        var catalogPolicyIds = ReplayGoldenRegressionCatalog.All.Select(c => c.PolicyId).ToArray();

        Assert.That(catalogPolicyIds, Does.Not.Contain(PolicyId));
        Assert.That(catalogPolicyIds, Has.Length.EqualTo(6));
    }

    [Test]
    public void Facility_hot_tick_fixture_replay_is_deterministic()
    {
        var catalog = BalticPatrolWithFacilityDamage();
        var a = BalticReplayHarness.Run(Seed, PolicyId, Ticks, catalog: catalog);
        var b = BalticReplayHarness.Run(Seed, PolicyId, Ticks, catalog: catalog);

        Assert.That(b.Fingerprint, Is.EqualTo(a.Fingerprint));
        Assert.That(b.WorldHash, Is.EqualTo(a.WorldHash));
        Assert.That(b.DetectionWorldHash, Is.EqualTo(a.DetectionWorldHash));
        Assert.That(b.FingerprintSha256, Is.EqualTo(a.FingerprintSha256));
    }

    [Test]
    public void Facility_hot_tick_fixture_emits_platform_damage_change_rows_for_runway()
    {
        var result = BalticReplayHarness.Run(
            Seed,
            PolicyId,
            Ticks,
            catalog: BalticPatrolWithFacilityDamage());

        Assert.That(result.Fingerprint, Does.Contain("PlatformDamageChange|"));
        Assert.That(result.Fingerprint, Does.Contain("runway-1"));
        Assert.That(
            result.Fingerprint,
            Does.Contain(PlatformDamageChangeReasonCodes.Hit).Or.Contain(EngagementOutcomeCodes.Kill));
    }
}