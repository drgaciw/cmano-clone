using System.Reflection;
using System.Text.Json;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Scenario.Policy;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Catalog;

public sealed class GauntletPolicyStrictKeysTests
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

    [Test]
    public void Valid_ladder_policy_has_no_errors_or_warnings()
    {
        var report = GauntletPolicyStrictKeys.Check(Policy(ValidCore));
        Assert.That(report.Errors, Is.Empty);
        Assert.That(report.Warnings, Is.Empty);
    }

    [Test]
    public void Legacy_gauntlet_emcon_is_error_not_warning()
    {
        var report = GauntletPolicyStrictKeys.Check(Policy(ValidCore + """, "emcon": "phased" """));
        Assert.That(report.Warnings, Is.Empty);
        var e = ProjectAegis.Data.Tests.AssertEx.Single(report.Errors);
        Assert.That(e, Does.Contain("emcon"));
        Assert.That(e, Does.Contain("top-level"));
        Assert.That(e, Does.Contain("BUG-gauntlet-emcon-dimension-not-exercised"));
    }

    [Test]
    public void Unknown_gauntlet_key_is_error_listing_allowed_keys()
    {
        var report = GauntletPolicyStrictKeys.Check(Policy(ValidCore + """, "emconPhases": [] """));
        var e = ProjectAegis.Data.Tests.AssertEx.Single(report.Errors);
        Assert.That(e, Does.Contain("emconPhases"));
        Assert.That(e, Does.Contain("expect"));
        Assert.That(e, Does.Contain("dimensionsClaimed"));
    }

    [Test]
    public void Unknown_expect_key_is_error()
    {
        var body = ValidCore.Replace("\"minScore\": 50.0", "\"minScore\": 50.0, \"minimumScore\": 1");
        var report = GauntletPolicyStrictKeys.Check(Policy(body));
        var e = ProjectAegis.Data.Tests.AssertEx.Single(report.Errors);
        Assert.That(e, Does.Contain("gauntlet.expect.minimumScore"));
    }

    [Test]
    public void Unknown_unit_key_is_error()
    {
        var body = ValidCore.Replace("\"side\": \"blue\"", "\"side\": \"blue\", \"emcon\": \"Off\"");
        var report = GauntletPolicyStrictKeys.Check(Policy(body));
        var e = ProjectAegis.Data.Tests.AssertEx.Single(report.Errors);
        Assert.That(e, Does.Contain("gauntlet.units[0].emcon"));
    }

    [Test]
    public void ExpectCi_block_is_allowed_and_checked()
    {
        var report = GauntletPolicyStrictKeys.Check(
            Policy(ValidCore + """, "expectCi": { "side": "BLUE", "bogusKey": 1 } """));
        var e = ProjectAegis.Data.Tests.AssertEx.Single(report.Errors);
        Assert.That(e, Does.Contain("gauntlet.expectCi.bogusKey"));
    }

    [Test]
    public void Qa_metadata_keys_are_allowed()
    {
        var report = GauntletPolicyStrictKeys.Check(Policy(ValidCore + """
            , "expectProvenance": { "csv": "x.csv" }
            , "expectCiProvenance": { "csv": "y.csv" }
            , "dimensionsClaimed": ["emcon"]
            , "forge": { "recipe": "r1" }
            """));
        Assert.That(report.Errors, Is.Empty);
        Assert.That(report.Warnings, Is.Empty);
    }

    [Test]
    public void Missing_or_invalid_gauntlet_block_yields_no_report_entries()
    {
        Assert.That(GauntletPolicyStrictKeys.Check("""{ "id": "p1" }""").Errors, Is.Empty);
        Assert.That(GauntletPolicyStrictKeys.Check("not json").Errors, Is.Empty);
    }

    [Test]
    public void Allowed_keys_are_derived_from_dto_property_names_not_hand_list()
    {
        // Derivation proof: whitelist == camelCase of ScenarioGauntletJsonDto / expect / unit props.
        // Adding a DTO property automatically widens Check without editing a string encyclopedia.
        var gauntletKeys = CamelCaseProps(typeof(ScenarioGauntletJsonDto));
        var expectKeys = CamelCaseProps(typeof(ScenarioGauntletExpectJsonDto));
        var unitKeys = CamelCaseProps(typeof(ScenarioGauntletUnitJsonDto));

        Assert.That(GauntletPolicyStrictKeys.AllowedGauntletKeys.OrderBy(k => k, StringComparer.Ordinal), Is.EqualTo(gauntletKeys));
        Assert.That(GauntletPolicyStrictKeys.AllowedExpectKeys.OrderBy(k => k, StringComparer.Ordinal), Is.EqualTo(expectKeys));
        Assert.That(GauntletPolicyStrictKeys.AllowedUnitKeys.OrderBy(k => k, StringComparer.Ordinal), Is.EqualTo(unitKeys));

        Assert.That(GauntletPolicyStrictKeys.AllowedGauntletKeys, Does.Contain("dimensionsClaimed"));
        Assert.That(GauntletPolicyStrictKeys.AllowedGauntletKeys, Does.Contain("forge"));
        Assert.That(GauntletPolicyStrictKeys.AllowedGauntletKeys, Does.Not.Contain("emcon"));
    }

    private static IEnumerable<string> CamelCaseProps(Type type) =>
        type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(p => JsonNamingPolicy.CamelCase.ConvertName(p.Name))
            .OrderBy(k => k, StringComparer.Ordinal);
}
