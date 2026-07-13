namespace ProjectAegis.Delegation.UnityAdapter.Tests.Baltic;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Baltic;
using ProjectAegis.Sim.Catalog;
using ProjectAegis.Sim.Scenario;
using ProjectAegis.Sim.Sensors;
using NUnit.Framework;

/// <summary>
/// S32-09 isolated BDA lifecycle fixture — not in ReplayGolden 6/6 catalog.
/// </summary>
[TestFixture]
public sealed class BalticReplayHarnessBdaLifecycleTests
{
    private const string PolicyId = "baltic-patrol-bda-lifecycle";
    private const int Seed = 42;
    private const int Ticks = 4;

    private static InMemoryCatalogReader BalticPatrolWithHostileBdaDamage() =>
        new(
            InMemoryCatalogReader.BalticPatrolFixture().GetSortedSensorBindings(),
            "p0-baltic-fixture+bda-lifecycle",
            CatalogValidationDefaults.BalticPlatforms(),
            damage:
            [
                new CatalogPlatformDamage(
                    "hostile-1",
                    MaxHp: 100,
                    WithdrawThresholdPct: 25,
                    Resilience: 2.0),
            ]);

    [Test]
    public void Policy_loads_combat_domains_detection_and_catalog_withdraw_for_hostile()
    {
        ScenarioPolicyRepository.EnsureDefaultJsonLoaded();
        var profile = ScenarioPolicyRepository.TryGet(PolicyId);

        Assert.That(profile, Is.Not.Null);
        Assert.That(profile!.EngageDefaults, Is.Not.Null);
        Assert.That(profile.EngageDefaults!.CombatDomainsEnabled, Is.True);
        Assert.That(profile.EngageDefaults.PkKill, Is.EqualTo(0.0));
        Assert.That(profile.DetectionTrials, Has.Count.EqualTo(1));
        Assert.That(profile.DetectionTrials[0].TargetId, Is.EqualTo("hostile-1"));
        Assert.That(profile.CatalogWithdrawTargets, Has.Count.EqualTo(1));
        Assert.That(profile.CatalogWithdrawTargets[0].PlatformId, Is.EqualTo("hostile-1"));
        Assert.That(profile.CatalogWithdrawTargets[0].CurrentHpPct, Is.EqualTo(30.0).Within(1e-6));
    }

    [Test]
    public void Policy_is_not_in_replay_golden_regression_catalog()
    {
        var catalogPolicyIds = ReplayGoldenRegressionCatalog.All.Select(c => c.PolicyId).ToArray();

        Assert.That(catalogPolicyIds, Does.Not.Contain(PolicyId));
        Assert.That(catalogPolicyIds, Has.Length.EqualTo(6));
    }

    [Test]
    public void Bda_lifecycle_fixture_replay_is_deterministic()
    {
        var catalog = BalticPatrolWithHostileBdaDamage();
        var a = BalticReplayHarness.Run(Seed, PolicyId, Ticks, catalog: catalog);
        var b = BalticReplayHarness.Run(Seed, PolicyId, Ticks, catalog: catalog);

        Assert.That(b.Fingerprint, Is.EqualTo(a.Fingerprint));
        Assert.That(b.WorldHash, Is.EqualTo(a.WorldHash));
        Assert.That(b.DetectionWorldHash, Is.EqualTo(a.DetectionWorldHash));
        Assert.That(b.FingerprintSha256, Is.EqualTo(a.FingerprintSha256));
    }

    [Test]
    public void Bda_lifecycle_fixture_promotes_contact_to_lost_consistent_with_projection()
    {
        var catalog = BalticPatrolWithHostileBdaDamage();
        var result = BalticReplayHarness.Run(Seed, PolicyId, Ticks, catalog: catalog);

        Assert.That(result.Fingerprint, Does.Contain("ContactChange|"));
        Assert.That(result.Fingerprint, Does.Contain("hostile-1"));
        Assert.That(result.Fingerprint, Does.Contain(ContactLifecycleState.Lost.ToString()));
        Assert.That(result.Fingerprint, Does.Contain("PlatformDamageChange|"));
        Assert.That(result.Fingerprint, Does.Contain(PlatformDamageChangeReasonCodes.Hit));

        var picture = ContactPictureProjection.ProjectWithBda(result.DecisionLog);
        Assert.That(picture, Is.Empty);
    }
}