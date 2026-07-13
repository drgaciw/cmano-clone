using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Import;
using ProjectAegis.Delegation.UnityAdapter.Baltic;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Baltic;

/// <summary>
/// Phase 2 joint ORBAT: catalog surface+air+sub platform_ids participate in detection
/// and catalog magazines are seeded for engage; surface catalog id is the combat shooter.
/// </summary>
[TestFixture]
public sealed class BalticReplayHarnessGauntletOrbatTests
{
    private static readonly string[] JointPlatformIds =
    [
        "k-31-visby-2009",
        "jas-39c-gripen-2005",
        "a-19-gotland-2022",
    ];

    [Test]
    public void Joint_orbat_catalog_platforms_appear_as_detection_actors_and_magazine_seeded()
    {
        ScenarioPolicyRepository.EnsureDefaultJsonLoaded();
        Assert.That(
            ScenarioPolicyRepository.TryGet("gauntlet-joint-orbat-smoke"),
            Is.Not.Null,
            "policy gauntlet-joint-orbat-smoke must load from data/scenarios");

        var result = BalticReplayHarness.Run(
            seed: 42,
            scenarioPolicyId: "gauntlet-joint-orbat-smoke",
            ticks: 6,
            mvpEngagement: true);

        Assert.That(result.Fingerprint, Is.Not.Null.And.Not.Empty);
        var fireOrder = BalticReplayHarness.ResolveFireOrder(null, result.DecisionLog);
        var joined = string.Join(" ", fireOrder) + " " + result.Fingerprint;

        // Registration + magazine path (catalog envelopes)
        Assert.That(joined, Does.Contain("CATALOG_UNIT:k-31-visby-2009:surface"));
        Assert.That(joined, Does.Contain("CATALOG_UNIT:jas-39c-gripen-2005:air"));
        Assert.That(joined, Does.Contain("CATALOG_UNIT:a-19-gotland-2022:subsurface"));
        Assert.That(joined, Does.Contain("MAGAZINE_SEED:k-31-visby-2009:"));
        Assert.That(joined, Does.Contain("MAGAZINE_SEED:jas-39c-gripen-2005:"));
        Assert.That(joined, Does.Contain("MAGAZINE_SEED:a-19-gotland-2022:"));

        // Detection actors (ContactChange observer = catalog platform_id)
        Assert.That(
            result.Fingerprint,
            Does.Contain("k-31-visby-2009").And.Contain("ContactChange"),
            "surface catalog platform must appear in ContactChange path");
        Assert.That(result.Fingerprint, Does.Contain("jas-39c-gripen-2005"));
        Assert.That(result.Fingerprint, Does.Contain("a-19-gotland-2022"));

        // Engage uses catalog surface shooter (not only u1)
        Assert.That(
            result.Fingerprint,
            Does.Contain("k-31-visby-2009").And.Contain("Engagement"),
            "surface catalog platform should participate in Engagement rows");

        // Catalog red target (not synthetic hostile-1)
        Assert.That(joined, Does.Contain("CATALOG_UNIT:em-sovremenny-i-pr-956-sarych:surface"));
        Assert.That(result.Fingerprint, Does.Contain("em-sovremenny-i-pr-956-sarych"));
        Assert.That(result.Fingerprint, Does.Not.Contain("|hostile-1|"));

        // MAGAZINE_SEED encodes positive max range from catalog
        foreach (var pid in JointPlatformIds)
        {
            var marker = fireOrder.FirstOrDefault(e => e.StartsWith($"MAGAZINE_SEED:{pid}:", StringComparison.Ordinal));
            Assert.That(marker, Is.Not.Null, $"missing MAGAZINE_SEED for {pid}");
            var parts = marker!.Split(':');
            Assert.That(parts.Length, Is.GreaterThanOrEqualTo(4));
            Assert.That(double.Parse(parts[^1], System.Globalization.CultureInfo.InvariantCulture), Is.GreaterThan(0));
            Assert.That(int.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture), Is.GreaterThanOrEqualTo(0));
        }
    }

    [Test]
    public void Joint_orbat_catalog_platforms_have_positive_range_magazine_weapons()
    {
        var dbPath = CatalogJsonImporter.ResolveRepoRelative(
            Path.Combine("assets", "data", "catalog", "baltic_patrol.db"));
        Assert.That(File.Exists(dbPath), Is.True, dbPath);

        using var reader = new SqliteCatalogReader(dbPath, "qa-joint-orbat");
        var mags = reader.GetSortedMagazines();
        foreach (var platformId in JointPlatformIds)
        {
            var weaponIds = mags
                .Where(m => string.Equals(m.PlatformId, platformId, StringComparison.Ordinal))
                .Select(m => m.WeaponId)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            Assert.That(weaponIds, Is.Not.Empty, $"platform {platformId} needs magazines");

            var maxRange = 0.0;
            foreach (var wid in weaponIds)
            {
                if (reader.TryGetWeaponEnvelope(wid, out var env) && env.MaxRangeMeters > maxRange)
                {
                    maxRange = env.MaxRangeMeters;
                }
            }

            Assert.That(maxRange, Is.GreaterThan(0), $"platform {platformId} needs max_range>0 weapon");
        }
    }
}
