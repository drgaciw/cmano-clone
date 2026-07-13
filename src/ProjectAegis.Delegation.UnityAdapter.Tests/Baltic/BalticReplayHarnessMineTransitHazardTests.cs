namespace ProjectAegis.Delegation.UnityAdapter.Tests.Baltic;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Delegation.UnityAdapter.Baltic;
using ProjectAegis.Sim.Catalog;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

/// <summary>
/// S32-08 isolated mine transit hazard hot-tick fixture — not in ReplayGolden 6/6 catalog.
/// </summary>
[TestFixture]
public sealed class BalticReplayHarnessMineTransitHazardTests
{
    private const string PolicyId = "baltic-patrol-mine-transit-hazard";
    private const int Seed = 42;
    private const int Ticks = 4;

    private static InMemoryCatalogReader BalticPatrolWithMineTransitDamage() =>
        new(
            InMemoryCatalogReader.BalticPatrolFixture().GetSortedSensorBindings(),
            "p0-baltic-fixture+mine-transit-hazard",
            CatalogValidationDefaults.BalticPlatforms(),
            damage: [new CatalogPlatformDamage("u1", MaxHp: 100, WithdrawThresholdPct: 25)]);

    [Test]
    public void Policy_loads_mine_hazard_zone_mines_transit_and_combat_domain_mine()
    {
        ScenarioPolicyRepository.EnsureDefaultJsonLoaded();
        var profile = ScenarioPolicyRepository.TryGet(PolicyId);

        Assert.That(profile, Is.Not.Null);
        Assert.That(profile!.EngageDefaults, Is.Not.Null);
        Assert.That(profile.EngageDefaults!.CombatDomainsEnabled, Is.True);
        Assert.That(profile.EngageDefaults.CombatDomain, Is.EqualTo(CombatDomain.Mine));
        Assert.That(profile.EngageDefaults.PkKill, Is.EqualTo(0.0));
        Assert.That(profile.MineHazard, Is.Not.Null);
        Assert.That(profile.MineHazard!.Mines, Has.Count.EqualTo(2));
        Assert.That(profile.MineHazard.Transit, Has.Count.EqualTo(1));
        Assert.That(profile.CatalogWithdrawTargets, Has.Count.EqualTo(1));
        Assert.That(profile.CatalogWithdrawTargets[0].PlatformId, Is.EqualTo("u1"));
    }

    [Test]
    public void Policy_is_not_in_replay_golden_regression_catalog()
    {
        var catalogPolicyIds = ReplayGoldenRegressionCatalog.All.Select(c => c.PolicyId).ToArray();

        Assert.That(catalogPolicyIds, Does.Not.Contain(PolicyId));
        Assert.That(catalogPolicyIds, Has.Length.EqualTo(6));
    }

    [Test]
    public void Mine_transit_hazard_fixture_replay_is_deterministic()
    {
        var catalog = BalticPatrolWithMineTransitDamage();
        var a = BalticReplayHarness.Run(Seed, PolicyId, Ticks, catalog: catalog);
        var b = BalticReplayHarness.Run(Seed, PolicyId, Ticks, catalog: catalog);

        Assert.That(b.Fingerprint, Is.EqualTo(a.Fingerprint));
        Assert.That(b.WorldHash, Is.EqualTo(a.WorldHash));
        Assert.That(b.DetectionWorldHash, Is.EqualTo(a.DetectionWorldHash));
        Assert.That(b.FingerprintSha256, Is.EqualTo(a.FingerprintSha256));
    }

    [Test]
    public void Mine_transit_hazard_fixture_emits_platform_damage_change_rows_for_transit_path()
    {
        var result = BalticReplayHarness.Run(
            Seed,
            PolicyId,
            Ticks,
            catalog: BalticPatrolWithMineTransitDamage());

        Assert.That(result.Fingerprint, Does.Contain("PlatformDamageChange|"));
        Assert.That(result.Fingerprint, Does.Contain("u1"));
        Assert.That(result.Fingerprint, Does.Contain(PlatformDamageChangeReasonCodes.MineTransitHazard));
    }
}