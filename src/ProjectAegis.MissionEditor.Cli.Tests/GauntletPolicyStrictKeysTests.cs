namespace ProjectAegis.MissionEditor.Cli.Tests;

using ProjectAegis.MissionEditor.Cli;
using Xunit;

public class GauntletPolicyStrictKeysTests
{
    private static string Policy(string gauntletBody) =>
        $$"""{ "friendlyRoe": "WeaponsFree", "id": "p1", "gauntlet": { {{gauntletBody}} } }""";

    private const string ValidCore =
        """
        "intent": "t", "oracle": "o", "runId": "r", "tier": 1,
        "catalogRefs": ["k-31-visby-2009"],
        "units": [{ "unitId": "u1", "platformId": "k-31-visby-2009", "domain": "surface", "side": "blue" }],
        "expect": { "side": "BLUE", "minKills": 1, "maxMissilesFired": 5, "minDenials": 2,
                    "maxDenials": 10, "minScore": 50.0, "maxScore": 90.0,
                    "requireNonEmptyFingerprint": true }
        """;

    [Fact]
    public void Valid_ladder_policy_has_no_errors_or_warnings()
    {
        var report = GauntletPolicyStrictKeys.Check(Policy(ValidCore));
        Assert.Empty(report.Errors);
        Assert.Empty(report.Warnings);
    }

    [Fact]
    public void Legacy_gauntlet_emcon_is_warning_not_error()
    {
        var report = GauntletPolicyStrictKeys.Check(Policy(ValidCore + """, "emcon": "phased" """));
        Assert.Empty(report.Errors);
        var w = Assert.Single(report.Warnings);
        Assert.Contains("emcon", w);
        Assert.Contains("top-level", w); // suggests the real engine-bound block
    }

    [Fact]
    public void Unknown_gauntlet_key_is_error_listing_allowed_keys()
    {
        var report = GauntletPolicyStrictKeys.Check(Policy(ValidCore + """, "emconPhases": [] """));
        var e = Assert.Single(report.Errors);
        Assert.Contains("emconPhases", e);
        Assert.Contains("expect", e); // allowed-keys listing present
    }

    [Fact]
    public void Unknown_expect_key_is_error()
    {
        var body = ValidCore.Replace("\"minScore\": 50.0", "\"minScore\": 50.0, \"minimumScore\": 1");
        var report = GauntletPolicyStrictKeys.Check(Policy(body));
        var e = Assert.Single(report.Errors);
        Assert.Contains("gauntlet.expect.minimumScore", e);
    }

    [Fact]
    public void Unknown_unit_key_is_error()
    {
        var body = ValidCore.Replace("\"side\": \"blue\"", "\"side\": \"blue\", \"emcon\": \"Off\"");
        var report = GauntletPolicyStrictKeys.Check(Policy(body));
        var e = Assert.Single(report.Errors);
        Assert.Contains("gauntlet.units[0].emcon", e);
    }

    [Fact]
    public void ExpectCi_block_is_allowed_and_checked()
    {
        var report = GauntletPolicyStrictKeys.Check(
            Policy(ValidCore + """, "expectCi": { "side": "BLUE", "bogusKey": 1 } """));
        var e = Assert.Single(report.Errors);
        Assert.Contains("gauntlet.expectCi.bogusKey", e);
    }

    [Fact]
    public void Missing_or_invalid_gauntlet_block_yields_no_report_entries()
    {
        // Absence is handled by the evaluator ("missing gauntlet.expect"), not strict keys.
        Assert.Empty(GauntletPolicyStrictKeys.Check("""{ "id": "p1" }""").Errors);
        Assert.Empty(GauntletPolicyStrictKeys.Check("not json").Errors);
    }
}
