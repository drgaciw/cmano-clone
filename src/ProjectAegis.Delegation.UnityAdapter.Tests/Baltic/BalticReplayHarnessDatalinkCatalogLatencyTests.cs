namespace ProjectAegis.Delegation.UnityAdapter.Tests.Baltic;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Delegation.UnityAdapter.Baltic;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

/// <summary>
/// S34-07 isolated catalog-latency fixture — excluded from ReplayGolden 6/6 catalog.
/// </summary>
[TestFixture]
public sealed class BalticReplayHarnessDatalinkCatalogLatencyTests
{
    private const string PolicyId = "baltic-patrol-datalink-catalog-latency";
    private const int Seed = 42;
    private const int Ticks = 5;

    private static string GoldenPath =>
        ReplayGoldenAssertions.ResolveRegressionGoldenPath(
            "replay-golden-baltic-datalink-catalog-latency-2026-06-19.txt");

    [Test]
    public void Policy_loads_sharing_without_explicit_shareLagTicks()
    {
        ScenarioPolicyRepository.EnsureDefaultJsonLoaded();
        var profile = ScenarioPolicyRepository.TryGet(PolicyId);

        Assert.That(profile, Is.Not.Null);
        Assert.That(profile!.DatalinkDoctrine.IsSharingEnabled, Is.True);
        Assert.That(profile.DatalinkDoctrine.OrganicOnly, Is.False);
        Assert.That(profile.DatalinkDoctrine.ShareLagTicks, Is.EqualTo(0));
        Assert.That(profile.DatalinkDoctrine.ShareLagTicksSpecified, Is.False);
        Assert.That(profile.DatalinkDoctrine.ResolveSide("u1"), Is.EqualTo("blue"));
        Assert.That(profile.DatalinkDoctrine.ResolveSide("u2"), Is.EqualTo("blue"));
    }

    [Test]
    public void Harness_applies_catalog_derived_share_lag_from_NATO_TADIL_J()
    {
        ScenarioPolicyRepository.EnsureDefaultJsonLoaded();
        var profile = ScenarioPolicyRepository.TryGet(PolicyId);
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();

        Assert.That(profile, Is.Not.Null);

        var resolved = DatalinkShareLagResolver.Resolve(profile!.DatalinkDoctrine, catalog);

        Assert.That(resolved.ShareLagTicks, Is.EqualTo(3));
        Assert.That(resolved.ShareLagTicksSpecified, Is.False);
    }

    [Test]
    public void Policy_is_not_in_replay_golden_regression_catalog()
    {
        var catalogPolicyIds = ReplayGoldenRegressionCatalog.All.Select(c => c.PolicyId).ToArray();

        Assert.That(catalogPolicyIds, Does.Not.Contain(PolicyId));
        Assert.That(catalogPolicyIds, Has.Length.EqualTo(6));
    }

    [Test]
    public void Catalog_latency_fixture_replay_is_deterministic()
    {
        var a = BalticReplayHarness.Run(Seed, PolicyId, Ticks);
        var b = BalticReplayHarness.Run(Seed, PolicyId, Ticks);

        Assert.That(b.Fingerprint, Is.EqualTo(a.Fingerprint));
        Assert.That(b.WorldHash, Is.EqualTo(a.WorldHash));
        Assert.That(b.DetectionWorldHash, Is.EqualTo(a.DetectionWorldHash));
        Assert.That(b.FingerprintSha256, Is.EqualTo(a.FingerprintSha256));
    }

    [Test]
    public void Catalog_latency_fixture_defers_peer_share_until_after_lag_ticks()
    {
        var result = BalticReplayHarness.Run(Seed, PolicyId, Ticks);

        Assert.That(result.Fingerprint, Does.Contain("ContactChange|"));
        Assert.That(result.Fingerprint, Does.Contain("|u2|dl-hostile-1|hostile-1|Unknown|Detected"));
        Assert.That(result.Fingerprint, Does.Not.Contain("|u2|dl-hostile-1|hostile-1|Unknown|Detected|1|"));
        Assert.That(result.Fingerprint, Does.Not.Contain("|u2|dl-hostile-1|hostile-1|Unknown|Detected|2|"));
        Assert.That(result.Fingerprint, Does.Not.Contain("|u2|dl-hostile-1|hostile-1|Unknown|Detected|3|"));
    }

    [Test]
    public void Pinned_isolated_golden_hashes_match_harness()
    {
        var result = BalticReplayHarness.Run(Seed, PolicyId, Ticks);
        ReplayGoldenAssertions.AssertPinnedHashes(result, File.ReadAllLines(GoldenPath));
    }
}