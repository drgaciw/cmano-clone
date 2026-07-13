namespace ProjectAegis.Delegation.UnityAdapter.Tests.Baltic;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Delegation.UnityAdapter.Baltic;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

/// <summary>
/// Phase 2 joint catalog ORBAT smoke: gauntlet.units registration + magazine envelope pins.
/// Opt-in policy only — does not alter ReplayGolden seed scenarios.
/// </summary>
[TestFixture]
public sealed class BalticReplayHarnessGauntletOrbatTests
{
    private const string PolicyId = "gauntlet-joint-orbat-smoke";
    private const int Seed = 42;
    private const int Ticks = 4;

    private static readonly string[] CatalogPlatformIds =
    [
        "k-31-visby-2009",
        "jas-39c-gripen-2005",
        "a-19-gotland-2022",
    ];

    [Test]
    public void ScenarioPolicyRepository_loads_joint_orbat_smoke_with_gauntlet_units()
    {
        ScenarioPolicyRepository.EnsureDefaultJsonLoaded();
        var profile = ScenarioPolicyRepository.TryGet(PolicyId);
        Assert.That(profile, Is.Not.Null, "policy must load from data/scenarios by id");

        var dto = ProjectAegis.Data.Scenario.ScenarioPolicyJsonCatalog.TryGetJson(PolicyId);
        Assert.That(dto, Is.Not.Null);
        Assert.That(dto!.Gauntlet, Is.Not.Null);
        Assert.That(dto.Gauntlet!.Units, Is.Not.Null);
        Assert.That(dto.Gauntlet.Units!, Has.Count.GreaterThanOrEqualTo(3));

        var platformIds = dto.Gauntlet.Units!
            .Select(u => u.PlatformId)
            .ToArray();
        Assert.That(platformIds, Does.Contain("k-31-visby-2009"));
        Assert.That(platformIds, Does.Contain("jas-39c-gripen-2005"));
        Assert.That(platformIds, Does.Contain("a-19-gotland-2022"));
    }

    [Test]
    public void Joint_orbat_smoke_emits_CATALOG_UNIT_events_for_surface_air_subsurface()
    {
        var result = BalticReplayHarness.Run(Seed, PolicyId, Ticks, mvpEngagement: true);

        Assert.That(result.Fingerprint, Does.Contain("CATALOG_UNIT:k-31-visby-2009:surface"));
        Assert.That(result.Fingerprint, Does.Contain("CATALOG_UNIT:jas-39c-gripen-2005:air"));
        Assert.That(result.Fingerprint, Does.Contain("CATALOG_UNIT:a-19-gotland-2022:subsurface"));

        // Engage path still works (detection u1 -> hostile-1).
        Assert.That(result.EngagementCount, Is.GreaterThan(0));
    }

    [Test]
    public void Joint_orbat_catalog_platforms_have_magazine_weapon_max_range_positive()
    {
        var dbPath = CatalogReaderFactory.ResolveBalticPatrolDatabasePath();
        if (!File.Exists(dbPath) && CatalogReaderFactory.TryCreateBalticPatrolReader() is not SqliteCatalogReader)
        {
            Assert.Inconclusive("Repo-root baltic_patrol.db not available from test output directory.");
            return;
        }

        using var catalog = CatalogReaderFactory.TryCreateBalticPatrolReader() as SqliteCatalogReader
            ?? new SqliteCatalogReader(Path.GetFullPath(dbPath), "gauntlet-joint-orbat");

        foreach (var platformId in CatalogPlatformIds)
        {
            var magazines = catalog.GetSortedMagazines()
                .Where(m => string.Equals(m.PlatformId, platformId, StringComparison.Ordinal))
                .ToArray();
            Assert.That(magazines, Is.Not.Empty, $"platform {platformId} must have magazine rows");

            var maxRange = 0.0;
            foreach (var mag in magazines)
            {
                Assert.That(
                    catalog.TryGetWeaponEnvelope(mag.WeaponId, out var envelope),
                    Is.True,
                    $"weapon envelope missing for {platformId}/{mag.WeaponId}");
                if (envelope.MaxRangeMeters > maxRange)
                {
                    maxRange = envelope.MaxRangeMeters;
                }
            }

            Assert.That(
                maxRange,
                Is.GreaterThan(0),
                $"platform {platformId} magazine weapon max_range must be > 0");
        }
    }

    [Test]
    public void Joint_orbat_smoke_is_deterministic()
    {
        var a = BalticReplayHarness.Run(Seed, PolicyId, Ticks, mvpEngagement: true);
        var b = BalticReplayHarness.Run(Seed, PolicyId, Ticks, mvpEngagement: true);

        Assert.That(b.Fingerprint, Is.EqualTo(a.Fingerprint));
        Assert.That(b.WorldHash, Is.EqualTo(a.WorldHash));
        Assert.That(b.DetectionWorldHash, Is.EqualTo(a.DetectionWorldHash));
    }
}
