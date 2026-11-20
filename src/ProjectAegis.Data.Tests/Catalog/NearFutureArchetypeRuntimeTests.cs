using ProjectAegis.Data.Catalog;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Catalog;

public sealed class NearFutureArchetypeRuntimeTests
{
    [Test]
    public void PlanSpawns_accepts_cca_and_rejects_mass_at_medium_cap()
    {
        var path = ResolveCatalogPath();
        var requests = new[]
        {
            new ScenarioNearFutureUnitRequest("cca-wingman", "cca-1"),
            new ScenarioNearFutureUnitRequest("swarm-saturation", "swarm-1"),
        };

        var plans = NearFutureArchetypeRuntime.PlanSpawns(requests, 2, SwarmTier.Medium, path);

        Assert.That(plans, Has.Exactly(1).Items);
        Assert.That(plans[0].ArchetypeId, Is.EqualTo("cca-wingman"));
        Assert.That(plans[0].UnitId, Is.EqualTo("cca-1"));
    }

    private static string ResolveCatalogPath()
    {
        var dir = Directory.GetCurrentDirectory();
        for (var i = 0; i < 8; i++)
        {
            var candidate = Path.Combine(dir, "data", "catalog", "near_future_archetypes.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = Path.GetDirectoryName(dir) ?? dir;
        }

        throw new FileNotFoundException("near_future_archetypes.json");
    }
}