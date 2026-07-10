using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Import;
using Xunit;

namespace ProjectAegis.Data.Tests.Catalog;

/// <summary>
/// Gauntlet oracle-0: policy detection IDs and catalogRefs must resolve against tier roster.
/// Regression for scenario-data defect: multi-domain rosters omitting Baltic harness seeds.
/// </summary>
public sealed class GauntletRosterValidatorTests
{
    [Fact]
    public void Missing_seed_platforms_in_roster_fails_resolution()
    {
        var roster = """
            {
              "platforms": [
                { "platformId": "k-31-visby-2009", "domain": "surface" }
              ]
            }
            """;
        var policy = """
            {
              "id": "gauntlet-t4-asymm-roe",
              "detection": [
                { "observerId": "u1", "sensorId": "radar-1", "targetId": "hostile-1", "contactId": "c1" }
              ],
              "gauntlet": { "catalogRefs": ["k-31-visby-2009", "u1"] }
            }
            """;

        var issues = GauntletRosterValidator.ValidatePolicyAgainstRoster(policy, roster);
        Assert.NotEmpty(issues);
        Assert.Contains(issues, i => i.Contains("u1", StringComparison.Ordinal));
        Assert.Contains(issues, i => i.Contains("hostile-1", StringComparison.Ordinal));
    }

    [Fact]
    public void Complete_roster_including_seeds_passes_resolution()
    {
        var roster = """
            {
              "platforms": [
                { "platformId": "u1", "domain": "surface" },
                { "platformId": "hostile-1", "domain": "surface" },
                { "platformId": "hostile-far", "domain": "surface" },
                { "platformId": "k-31-visby-2009", "domain": "surface" }
              ]
            }
            """;
        var policy = """
            {
              "id": "gauntlet-t4-asymm-roe",
              "detection": [
                { "observerId": "u1", "sensorId": "radar-1", "targetId": "hostile-1", "contactId": "c1" },
                { "observerId": "u1", "sensorId": "radar-2", "targetId": "hostile-far", "contactId": "c2" }
              ],
              "gauntlet": { "catalogRefs": ["u1", "hostile-1", "hostile-far", "k-31-visby-2009"] }
            }
            """;

        var issues = GauntletRosterValidator.ValidatePolicyAgainstRoster(policy, roster);
        Assert.Empty(issues);
    }

    [Fact]
    public void Live_gauntlet_1903_all_tier_policies_resolve_against_rosters()
    {
        var runRoot = CatalogJsonImporter.ResolveRepoRelative(
            Path.Combine("production", "qa", "gauntlet", "gauntlet-20260709-1903"));
        Assert.True(Directory.Exists(runRoot), $"Missing gauntlet run: {runRoot}");

        for (var tier = 1; tier <= 5; tier++)
        {
            var tierDir = Path.Combine(runRoot, $"tier-{tier}");
            var rosterPath = Path.Combine(tierDir, "roster.json");
            Assert.True(File.Exists(rosterPath), rosterPath);
            var rosterJson = File.ReadAllText(rosterPath);

            foreach (var policyPath in Directory.EnumerateFiles(tierDir, "*.policy.json"))
            {
                var policyJson = File.ReadAllText(policyPath);
                var issues = GauntletRosterValidator.ValidatePolicyAgainstRoster(policyJson, rosterJson);
                Assert.True(
                    issues.Count == 0,
                    $"Tier {tier} {Path.GetFileName(policyPath)}: {string.Join("; ", issues)}");
            }
        }
    }
}
