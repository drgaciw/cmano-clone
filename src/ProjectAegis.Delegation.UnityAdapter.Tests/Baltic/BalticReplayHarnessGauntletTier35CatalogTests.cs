using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Import;
using ProjectAegis.Delegation.UnityAdapter.Baltic;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Baltic;

/// <summary>
/// Tiers 3–5 + joint ORBAT smoke: full catalog combat path including red (no hostile-1).
/// </summary>
[TestFixture]
public sealed class BalticReplayHarnessGauntletTier35CatalogTests
{
    private static readonly string[] SyntheticCombatIds = ["u1", "hostile-1", "hostile-far"];

    private static readonly string[] Tier35SampleIds =
    [
        "gauntlet-t3-emcon-phases",
        "gauntlet-t4-asymm-roe",
        "gauntlet-t5-theater",
        "gauntlet-joint-orbat-smoke",
    ];

    [TestCase("gauntlet-t3-emcon-phases")]
    [TestCase("gauntlet-joint-orbat-smoke")]
    public void Tier35_registers_catalog_red_and_joint_blue_without_synthetic_targets(string scenarioId)
    {
        ScenarioPolicyRepository.EnsureDefaultJsonLoaded();
        Assert.That(ScenarioPolicyRepository.TryGet(scenarioId), Is.Not.Null);

        var result = BalticReplayHarness.Run(42, scenarioId, ticks: 6, mvpEngagement: true);
        var fireOrder = BalticReplayHarness.ResolveFireOrder(null, result.DecisionLog);
        var events = string.Join(" ", fireOrder);
        var tokens = SplitFingerprint(result.Fingerprint);

        Assert.That(events, Does.Contain("CATALOG_UNIT:k-31-visby-2009:surface"));
        Assert.That(events, Does.Contain("CATALOG_UNIT:em-sovremenny-i-pr-956-sarych:surface"));
        Assert.That(events, Does.Contain("MAGAZINE_SEED:k-31-visby-2009:"));
        Assert.That(result.Fingerprint, Does.Contain("em-sovremenny-i-pr-956-sarych"));

        foreach (var synth in SyntheticCombatIds)
        {
            Assert.That(events, Does.Not.Contain($"CATALOG_UNIT:{synth}:"));
            Assert.That(
                result.Fingerprint,
                Does.Not.Contain($"|{synth}|"),
                $"fingerprint must not use synthetic combat id {synth} as actor/target token");
        }

        // ContactChange target is catalog red, not hostile-1
        Assert.That(
            result.Fingerprint,
            Does.Match(@"ContactChange\|[^|]*\|[^|]*\|[^|]*\|[^|]*\|[^|]*\|em-sovremenny-i-pr-956-sarych\|")
                .Or.Match(@"ContactChange\|[^|]*\|[^|]*\|[^|]*\|[^|]*\|[^|]*\|mpk-steregushchiy-pr-20380-2018\|"));

        AssertNoSameSideSuccessfulLaunches(tokens);
    }

    [TestCase("gauntlet-t3-emcon-phases")]
    [TestCase("gauntlet-joint-orbat-smoke")]
    public void Tier35_blue_surface_launches_at_catalog_red_not_red_on_red(string scenarioId)
    {
        ScenarioPolicyRepository.EnsureDefaultJsonLoaded();
        var result = BalticReplayHarness.Run(42, scenarioId, ticks: 6, mvpEngagement: true);
        var tokens = SplitFingerprint(result.Fingerprint);

        var blueLaunches = 0;
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
            Assert.That(victim, Is.Not.Null, e);

            if (IsCatalogBlue(shooter))
            {
                blueLaunches++;
                Assert.That(IsCatalogRed(victim!), Is.True, $"blue-on-red: {shooter}->{victim}");
            }
            else if (IsCatalogRed(shooter))
            {
                Assert.That(IsCatalogBlue(victim!), Is.True, $"red-on-blue: {shooter}->{victim}");
            }
            else
            {
                Assert.Fail($"unexpected shooter {shooter}");
            }
        }

        Assert.That(blueLaunches, Is.GreaterThan(0), "Visby (or other blue) must launch under WeaponsFree-friendly policies");
        AssertNoSameSideSuccessfulLaunches(tokens);
    }

    [Test]
    public void All_tier35_and_joint_policies_declare_catalog_red_units()
    {
        ScenarioPolicyRepository.EnsureDefaultJsonLoaded();
        var dbPath = CatalogJsonImporter.ResolveRepoRelative(
            Path.Combine("assets", "data", "catalog", "baltic_patrol.db"));
        using var reader = new SqliteCatalogReader(dbPath, "qa-t35");
        var mags = reader.GetSortedMagazines();
        var platforms = mags.Select(m => m.PlatformId).ToHashSet(StringComparer.Ordinal);

        foreach (var sid in Tier35SampleIds.Concat(new[]
                 {
                     "gauntlet-t3-escort-strike", "gauntlet-t3-event-chain", "gauntlet-t3-id-roe",
                     "gauntlet-t4-multi-mission", "gauntlet-t4-random-inject", "gauntlet-t4-weighted",
                     "gauntlet-t5-cascade", "gauntlet-t5-dynamic-obj", "gauntlet-t5-roe-change",
                 }).Distinct())
        {
            var dto = ProjectAegis.Data.Scenario.ScenarioPolicyJsonCatalog.TryGetJson(sid);
            Assert.That(dto?.Gauntlet?.Units, Is.Not.Null.And.Not.Empty, sid);
            var hasBlue = false;
            var hasRed = false;
            foreach (var u in dto!.Gauntlet!.Units!)
            {
                Assert.That(SyntheticCombatIds, Does.Not.Contain(u.PlatformId), sid);
                Assert.That(platforms.Contains(u.PlatformId), Is.True, $"{sid} {u.PlatformId}");
                if (string.Equals(u.Side, "red", StringComparison.OrdinalIgnoreCase))
                {
                    hasRed = true;
                }
                else
                {
                    hasBlue = true;
                }
            }

            Assert.That(hasBlue && hasRed, Is.True, $"{sid} needs blue+red catalog units");

            foreach (var det in dto.Detection ?? [])
            {
                Assert.That(SyntheticCombatIds, Does.Not.Contain(det.ObserverId), sid);
                Assert.That(SyntheticCombatIds, Does.Not.Contain(det.TargetId), sid);
            }
        }
    }

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

            Assert.That(
                (IsCatalogRed(shooter) && IsCatalogRed(victim))
                || (IsCatalogBlue(shooter) && IsCatalogBlue(victim)),
                Is.False,
                $"same-side: {shooter}->{victim}");
        }
    }

    private static string? FindNextOutcomeVictim(IReadOnlyList<string> tokens, int from)
    {
        for (var j = from; j < tokens.Count; j++)
        {
            if (tokens[j].StartsWith("EngagementOutcome|", StringComparison.Ordinal))
            {
                var parts = tokens[j].Split('|');
                return parts.Length >= 6 ? parts[5] : null;
            }

            if (tokens[j].StartsWith("Engagement|", StringComparison.Ordinal))
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
}
