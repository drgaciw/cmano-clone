using ProjectAegis.Data.Catalog;
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
    public void Representative_multi_tier_policy_resolves_against_compact_roster()
    {
        var roster = """
            {
              "platforms": [
                { "platformId": "u1", "domain": "surface" },
                { "platformId": "hostile-1", "domain": "surface" },
                { "platformId": "hostile-far", "domain": "surface" },
                { "platformId": "k-31-visby-2009", "domain": "surface" },
                { "platformId": "jas-39c-gripen-2010", "domain": "air" },
                { "platformId": "s-100b-argus-2005", "domain": "air" }
              ]
            }
            """;
        var policy = """
            {
              "id": "gauntlet-t5-theater",
              "detection": [
                { "observerId": "u1", "sensorId": "radar-1", "targetId": "hostile-1", "contactId": "c1" },
                { "observerId": "jas-39c-gripen-2010", "sensorId": "ps-05-a", "targetId": "hostile-far", "contactId": "c2" }
              ],
              "gauntlet": {
                "catalogRefs": [
                  "u1",
                  "hostile-1",
                  "hostile-far",
                  "k-31-visby-2009",
                  "jas-39c-gripen-2010",
                  "s-100b-argus-2005"
                ]
              }
            }
            """;

        var issues = GauntletRosterValidator.ValidatePolicyAgainstRoster(policy, roster);
        Assert.Empty(issues);
    }
}
