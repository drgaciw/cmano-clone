using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Import;
using ProjectAegis.Delegation.UnityAdapter.Baltic;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Baltic;

/// <summary>
/// Tiers 1–2 must fight on real cmo-db catalog platform ids (not synthetic u1/hostile-1/hostile-far).
/// </summary>
[TestFixture]
public sealed class BalticReplayHarnessGauntletTier12CatalogTests
{
    private static readonly string[] SyntheticCombatIds = ["u1", "hostile-1", "hostile-far"];

    private static readonly string[] Tier1Ids =
    [
        "gauntlet-t1-patrol-a",
        "gauntlet-t1-patrol-b",
        "gauntlet-t1-patrol-c",
        "gauntlet-t1-patrol-d",
    ];

    private static readonly string[] Tier2Ids =
    [
        "gauntlet-t2-escort-a",
        "gauntlet-t2-escort-passive",
        "gauntlet-t2-strike-a",
        "gauntlet-t2-strike-event",
    ];

    [TestCase("gauntlet-t1-patrol-a")]
    [TestCase("gauntlet-t2-escort-a")]
    public void Tier12_policy_registers_catalog_blue_shooter_and_red_target_not_synthetic(string scenarioId)
    {
        ScenarioPolicyRepository.EnsureDefaultJsonLoaded();
        var profile = ScenarioPolicyRepository.TryGet(scenarioId);
        Assert.That(profile, Is.Not.Null, $"policy {scenarioId} must load");

        var result = BalticReplayHarness.Run(
            seed: 42,
            scenarioPolicyId: scenarioId,
            ticks: 6,
            mvpEngagement: true);

        Assert.That(result.Fingerprint, Is.Not.Null.And.Not.Empty);
        // CATALOG_UNIT / MAGAZINE_SEED are EventFired (in fire-order); Engagement lives in fingerprint.
        var fireOrder = BalticReplayHarness.ResolveFireOrder(null, result.DecisionLog);
        var eventsJoined = string.Join(" ", fireOrder);
        var fpTokens = SplitFingerprint(result.Fingerprint);

        Assert.That(eventsJoined, Does.Match(@"CATALOG_UNIT:[a-z0-9][\w\-]*:surface"), "need catalog blue surface CATALOG_UNIT");
        Assert.That(eventsJoined, Does.Match(@"MAGAZINE_SEED:[a-z0-9][\w\-]*:\d+:"), "need MAGAZINE_SEED for blue");

        Assert.That(result.Fingerprint, Does.Contain("Engagement"));

        foreach (var synth in SyntheticCombatIds)
        {
            Assert.That(
                eventsJoined,
                Does.Not.Contain($"CATALOG_UNIT:{synth}:"),
                $"synthetic {synth} must not be registered as CATALOG_UNIT");
            Assert.That(
                eventsJoined,
                Does.Not.Contain($"MAGAZINE_SEED:{synth}:"),
                $"synthetic {synth} must not receive MAGAZINE_SEED");
        }

        Assert.That(
            result.Fingerprint,
            Does.Match(@"ContactChange\|[^|]*\|[^|]*\|[^|]*\|(?!u1\|)[\w\-]+\|"),
            "ContactChange observer must not be synthetic u1");

        var launched = fpTokens.Where(e =>
            e.StartsWith("Engagement|", StringComparison.Ordinal)
            && e.Contains("|True|Launched", StringComparison.Ordinal)).ToArray();
        if (launched.Length > 0)
        {
            foreach (var line in launched)
            {
                Assert.That(line, Does.Not.Contain("|u1|"), $"launch line uses synthetic shooter: {line}");
                Assert.That(
                    SyntheticCombatIds.Any(s => line.Contains($"|{s}|", StringComparison.Ordinal)),
                    Is.False,
                    $"launch line must not use synthetic combat id: {line}");
            }
        }

        Assert.That(
            result.Fingerprint,
            Does.Not.Match(@"ContactChange\|[^|]*\|[^|]*\|[^|]*\|[^|]*\|[^|]*\|hostile-1\|"),
            "ContactChange target must not be synthetic hostile-1");
        Assert.That(
            result.Fingerprint,
            Does.Not.Match(@"ContactChange\|[^|]*\|[^|]*\|[^|]*\|[^|]*\|[^|]*\|hostile-far\|"),
            "ContactChange target must not be synthetic hostile-far");

        AssertNoSameSideSuccessfulLaunches(fpTokens);
    }

    [TestCase("gauntlet-t1-patrol-b")]
    [TestCase("gauntlet-t2-escort-a")]
    public void Tier12_weapons_free_blue_launches_at_red_not_red_on_red(string scenarioId)
    {
        ScenarioPolicyRepository.EnsureDefaultJsonLoaded();
        var result = BalticReplayHarness.Run(42, scenarioId, ticks: 6, mvpEngagement: true);
        // Engagement / EngagementOutcome tokens are in the order-log fingerprint (space-separated).
        var tokens = SplitFingerprint(result.Fingerprint);

        var blueLaunches = 0;
        var redLaunches = 0;
        for (var i = 0; i < tokens.Count; i++)
        {
            var e = tokens[i];
            if (!e.StartsWith("Engagement|", StringComparison.Ordinal)
                || !e.Contains("|True|Launched", StringComparison.Ordinal))
            {
                continue;
            }

            var parts = e.Split('|');
            // Engagement|seq|tick|t|shooter|rounds|True|Launched
            Assert.That(parts.Length, Is.GreaterThanOrEqualTo(8), e);
            var shooter = parts[4];
            var victim = FindNextOutcomeVictim(tokens, i + 1);
            Assert.That(victim, Is.Not.Null.And.Not.Empty, $"missing EngagementOutcome after {e}");

            if (IsCatalogBlue(shooter))
            {
                blueLaunches++;
                Assert.That(IsCatalogRed(victim!), Is.True,
                    $"blue-on-red required: shooter={shooter} victim={victim}");
            }
            else if (IsCatalogRed(shooter))
            {
                redLaunches++;
                Assert.That(IsCatalogBlue(victim!), Is.True,
                    $"red-on-blue required (no red-on-red): shooter={shooter} victim={victim}");
            }
            else
            {
                Assert.Fail($"unexpected shooter (synthetic?): {shooter} in {e}");
            }
        }

        Assert.That(blueLaunches, Is.GreaterThan(0),
            "blue catalog shooter must successfully launch at least once under WeaponsFree");
        AssertNoSameSideSuccessfulLaunches(tokens);
    }

    /// <summary>
    /// DecisionLog fingerprint is newline-separated entries; CSV export flattens newlines to spaces.
    /// </summary>
    private static IReadOnlyList<string> SplitFingerprint(string fingerprint) =>
        fingerprint.Split(['\n', '\r', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static void AssertNoSameSideSuccessfulLaunches(IReadOnlyList<string> tokens)
    {
        for (var i = 0; i < tokens.Count; i++)
        {
            var e = tokens[i];
            if (!e.StartsWith("Engagement|", StringComparison.Ordinal)
                || !e.Contains("|True|Launched", StringComparison.Ordinal))
            {
                continue;
            }

            var shooter = e.Split('|')[4];
            var victim = FindNextOutcomeVictim(tokens, i + 1);
            if (victim == null)
            {
                continue;
            }

            var sameSideRed = IsCatalogRed(shooter) && IsCatalogRed(victim);
            var sameSideBlue = IsCatalogBlue(shooter) && IsCatalogBlue(victim);
            Assert.That(sameSideRed || sameSideBlue, Is.False,
                $"same-side engagement forbidden: {shooter} -> {victim}");
        }
    }

    private static string? FindNextOutcomeVictim(IReadOnlyList<string> tokens, int from)
    {
        for (var j = from; j < tokens.Count; j++)
        {
            var e = tokens[j];
            if (e.StartsWith("EngagementOutcome|", StringComparison.Ordinal))
            {
                // EngagementOutcome|seq|tick|t|fireId|victim|code|roll
                var parts = e.Split('|');
                return parts.Length >= 6 ? parts[5] : null;
            }

            if (e.StartsWith("Engagement|", StringComparison.Ordinal))
            {
                break;
            }
        }

        return null;
    }

    private static bool IsCatalogBlue(string id) =>
        id is "k-31-visby-2009" or "k-22-gavle-ex-goteborg-class" or "jas-39c-gripen-2005" or "a-19-gotland-2022";

    private static bool IsCatalogRed(string id) =>
        id is "em-sovremenny-i-pr-956-sarych" or "mpk-steregushchiy-pr-20380-2018";

    [Test]
    public void All_tier12_policies_declare_gauntlet_units_with_catalog_platform_ids_only()
    {
        ScenarioPolicyRepository.EnsureDefaultJsonLoaded();
        var dbPath = CatalogJsonImporter.ResolveRepoRelative(
            Path.Combine("assets", "data", "catalog", "baltic_patrol.db"));
        Assert.That(File.Exists(dbPath), Is.True);
        using var reader = new SqliteCatalogReader(dbPath, "qa-t12");
        var mags = reader.GetSortedMagazines();
        // Platforms with combat loadouts (magazine rows) are the usable set for gauntlet combat.
        var platforms = mags.Select(m => m.PlatformId).ToHashSet(StringComparer.Ordinal);

        foreach (var sid in Tier1Ids.Concat(Tier2Ids))
        {
            var dto = ProjectAegis.Data.Scenario.ScenarioPolicyJsonCatalog.TryGetJson(sid);
            Assert.That(dto, Is.Not.Null, sid);
            Assert.That(dto!.Gauntlet, Is.Not.Null, sid);
            Assert.That(dto.Gauntlet!.Units, Is.Not.Null.And.Not.Empty, $"{sid} needs gauntlet.units");

            var hasBlue = false;
            var hasRed = false;
            foreach (var u in dto.Gauntlet.Units!)
            {
                Assert.That(u.PlatformId, Is.Not.Null.And.Not.Empty, sid);
                Assert.That(
                    SyntheticCombatIds,
                    Does.Not.Contain(u.PlatformId),
                    $"{sid} unit {u.PlatformId} is synthetic");
                Assert.That(platforms.Contains(u.PlatformId), Is.True, $"{sid} unknown platform {u.PlatformId}");

                // Blue combat-usable: sensor + magazine with range>0
                if (!string.Equals(u.Side, "red", StringComparison.OrdinalIgnoreCase))
                {
                    hasBlue = true;
                    var weaponIds = mags
                        .Where(m => string.Equals(m.PlatformId, u.PlatformId, StringComparison.Ordinal))
                        .Select(m => m.WeaponId)
                        .Distinct()
                        .ToArray();
                    Assert.That(weaponIds, Is.Not.Empty, $"{sid} blue {u.PlatformId} needs magazines");
                    var maxRange = 0.0;
                    foreach (var wid in weaponIds)
                    {
                        if (reader.TryGetWeaponEnvelope(wid, out var env) && env.MaxRangeMeters > maxRange)
                        {
                            maxRange = env.MaxRangeMeters;
                        }
                    }

                    Assert.That(maxRange, Is.GreaterThan(0), $"{sid} blue {u.PlatformId} needs max_range>0");
                }
                else
                {
                    hasRed = true;
                }
            }

            Assert.That(hasBlue, Is.True, $"{sid} needs a blue unit");
            Assert.That(hasRed, Is.True, $"{sid} needs a red catalog target unit");

            // Detection trials must not use synthetic ids
            foreach (var det in dto.Detection ?? [])
            {
                Assert.That(SyntheticCombatIds, Does.Not.Contain(det.ObserverId), $"{sid} detection observer synthetic");
                Assert.That(SyntheticCombatIds, Does.Not.Contain(det.TargetId), $"{sid} detection target synthetic");
            }
        }
    }

    [Test]
    public void Tier1_patrol_a_magazine_seed_max_range_matches_catalog_db()
    {
        ScenarioPolicyRepository.EnsureDefaultJsonLoaded();
        var result = BalticReplayHarness.Run(42, "gauntlet-t1-patrol-a", ticks: 6, mvpEngagement: true);
        var fireOrder = BalticReplayHarness.ResolveFireOrder(null, result.DecisionLog);
        var seed = fireOrder.FirstOrDefault(e => e.StartsWith("MAGAZINE_SEED:", StringComparison.Ordinal));
        Assert.That(seed, Is.Not.Null);
        var parts = seed!.Split(':');
        Assert.That(parts.Length, Is.GreaterThanOrEqualTo(4));
        var platformId = parts[1];
        Assert.That(SyntheticCombatIds, Does.Not.Contain(platformId));
        var maxRange = double.Parse(parts[^1], System.Globalization.CultureInfo.InvariantCulture);
        Assert.That(maxRange, Is.GreaterThan(0));

        var dbPath = CatalogJsonImporter.ResolveRepoRelative(
            Path.Combine("assets", "data", "catalog", "baltic_patrol.db"));
        using var reader = new SqliteCatalogReader(dbPath, "qa-t12-range");
        var expected = 0.0;
        foreach (var mag in reader.GetSortedMagazines().Where(m => m.PlatformId == platformId))
        {
            if (reader.TryGetWeaponEnvelope(mag.WeaponId, out var env) && env.MaxRangeMeters > expected)
            {
                expected = env.MaxRangeMeters;
            }
        }

        Assert.That(maxRange, Is.EqualTo(expected).Within(0.01),
            $"MAGAZINE_SEED range for {platformId} must match catalog DB");
    }
}
