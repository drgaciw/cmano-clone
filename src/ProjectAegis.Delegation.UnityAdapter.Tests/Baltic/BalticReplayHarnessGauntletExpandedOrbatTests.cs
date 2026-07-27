using ProjectAegis.Data.Scenario;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Baltic;

/// <summary>
/// Locks denser catalog ORBATs for gauntlet smoke/pressure (post catalog expansion).
/// </summary>
[TestFixture]
public sealed class BalticReplayHarnessGauntletExpandedOrbatTests
{
    private static readonly string[] ExpandedSampleIds =
    [
        "gauntlet-t1-patrol-a",
        "gauntlet-t1-patrol-b",
        "gauntlet-t1-patrol-c",
        "gauntlet-t1-patrol-d",
        "gauntlet-t2-escort-a",
        "gauntlet-t2-escort-passive",
        "gauntlet-t2-strike-a",
        "gauntlet-t2-strike-event",
        "gauntlet-t3-emcon-phases",
        "gauntlet-t3-escort-strike",
        "gauntlet-t3-event-chain",
        "gauntlet-t3-id-roe",
        "gauntlet-t4-asymm-roe",
        "gauntlet-t4-multi-mission",
        "gauntlet-t4-random-inject",
        "gauntlet-t4-weighted",
        "gauntlet-t5-cascade",
        "gauntlet-t5-dynamic-obj",
        "gauntlet-t5-roe-change",
        "gauntlet-t5-theater",
        "gauntlet-joint-orbat-smoke",
        "gauntlet-multidomain-shooters",
        "gauntlet-theater-inject",
        "gauntlet-theater-dynamic-victory",
    ];

    [TestCase("gauntlet-t1-patrol-a", 6)]
    [TestCase("gauntlet-t2-escort-a", 8)]
    [TestCase("gauntlet-t3-emcon-phases", 12)]
    [TestCase("gauntlet-t4-random-inject", 14)]
    [TestCase("gauntlet-t5-theater", 16)]
    [TestCase("gauntlet-joint-orbat-smoke", 12)]
    public void Gauntlet_policy_declares_expanded_catalog_unit_floor(string scenarioId, int minUnits)
    {
        ScenarioPolicyRepository.EnsureDefaultJsonLoaded();
        var dto = ScenarioPolicyJsonCatalog.TryGetJson(scenarioId);
        Assert.That(dto?.Gauntlet?.Units, Is.Not.Null);
        Assert.That(dto!.Gauntlet!.Units!.Count, Is.GreaterThanOrEqualTo(minUnits), scenarioId);
        Assert.That(dto.Detection, Is.Not.Null.And.Not.Empty);
        Assert.That(
            dto.Detection!.Count,
            Is.GreaterThanOrEqualTo(Math.Min(3, minUnits / 2)),
            $"{scenarioId} detection lanes should scale with ORBAT");
    }

    [Test]
    public void Expanded_gauntlet_policies_span_many_distinct_catalog_platforms()
    {
        ScenarioPolicyRepository.EnsureDefaultJsonLoaded();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var sid in ExpandedSampleIds)
        {
            var dto = ScenarioPolicyJsonCatalog.TryGetJson(sid);
            Assert.That(dto?.Gauntlet?.Units, Is.Not.Null.And.Not.Empty, sid);
            foreach (var u in dto!.Gauntlet!.Units!)
            {
                ids.Add(u.PlatformId);
            }
        }

        Assert.That(
            ids.Count,
            Is.GreaterThanOrEqualTo(40),
            "expanded gauntlet suite should exercise a large catalog platform set");
    }
}
