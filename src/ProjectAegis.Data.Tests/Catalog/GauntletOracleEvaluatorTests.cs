using ProjectAegis.Data.Catalog;
using Xunit;

namespace ProjectAegis.Data.Tests.Catalog;

public sealed class GauntletOracleEvaluatorTests
{
    private const string PassPolicy = """
        {
          "id": "gauntlet-t1-patrol-a",
          "friendlyRoe": "WeaponsFree",
          "gauntlet": {
            "intent": "patrol",
            "oracle": "blue wins",
            "expect": {
              "side": "BLUE",
              "minKills": 1,
              "maxMissilesFired": 4,
              "minScore": 0,
              "maxScore": 100,
              "requireNonEmptyFingerprint": true
            }
          }
        }
        """;

    private const string PassCsv = """
        scenarioId,seed,side,score,kills,missilesFired,denials,fingerprint
        gauntlet-t1-patrol-a,42,BLUE,100,1,1,0,PolicyUpdate|1|ContactChange|u1|hostile-1
        """;

    [Fact]
    public void EvaluateFromPolicyAndCsv_pass_when_row_within_expect_bounds()
    {
        var result = GauntletOracleEvaluator.EvaluateFromPolicyAndCsv(PassPolicy, PassCsv);
        Assert.True(result.Passed, string.Join("; ", result.Failures));
        Assert.Empty(result.Failures);
    }

    [Fact]
    public void EvaluateFromPolicyAndCsv_fail_closed_when_missiles_exceed_max()
    {
        var policy = """
            {
              "id": "x",
              "gauntlet": {
                "expect": { "maxMissilesFired": 0, "requireNonEmptyFingerprint": true }
              }
            }
            """;
        var csv = """
            scenarioId,seed,side,score,kills,missilesFired,denials,fingerprint
            x,42,BLUE,100,1,1,0,fp-nonempty
            """;
        var result = GauntletOracleEvaluator.EvaluateFromPolicyAndCsv(policy, csv);
        Assert.False(result.Passed);
        Assert.Contains(result.Failures, f => f.Contains("missilesFired", StringComparison.Ordinal));
    }

    [Fact]
    public void EvaluateFromPolicyAndCsv_fail_closed_on_empty_fingerprint()
    {
        var policy = """
            {
              "id": "x",
              "gauntlet": {
                "expect": { "requireNonEmptyFingerprint": true }
              }
            }
            """;
        var csv = """
            scenarioId,seed,side,score,kills,missilesFired,denials,fingerprint
            x,42,BLUE,0,0,0,0,
            """;
        var result = GauntletOracleEvaluator.EvaluateFromPolicyAndCsv(policy, csv);
        Assert.False(result.Passed);
        Assert.Contains(result.Failures, f => f.Contains("empty fingerprint", StringComparison.Ordinal));
    }

    [Fact]
    public void EvaluateFromPolicyAndCsv_fail_when_expect_missing()
    {
        var policy = """{ "id": "x", "gauntlet": { "intent": "only" } }""";
        var result = GauntletOracleEvaluator.EvaluateFromPolicyAndCsv(policy, PassCsv);
        Assert.False(result.Passed);
        Assert.Contains(result.Failures, f => f.Contains("missing gauntlet.expect", StringComparison.Ordinal));
    }

    [Fact]
    public void EvaluateFromPolicyAndCsv_fail_when_required_fingerprint_substring_missing()
    {
        var policy = """
            {
              "id": "inject",
              "gauntlet": {
                "expect": {
                  "requireNonEmptyFingerprint": true,
                  "requireFingerprintSubstrings": [ "CommsStateChange", "Degraded" ]
                }
              }
            }
            """;
        var csv = """
            scenarioId,seed,side,score,kills,missilesFired,denials,fingerprint
            inject,42,BLUE,0,0,0,0,EventFired|1|0|0|inject-marker|ladder_seeded_inject
            """;
        var result = GauntletOracleEvaluator.EvaluateFromPolicyAndCsv(policy, csv);
        Assert.False(result.Passed);
        Assert.Contains(result.Failures, f => f.Contains("CommsStateChange", StringComparison.Ordinal));
    }

    [Fact]
    public void EvaluateFromPolicyAndCsv_fail_when_required_true_launched_shooter_missing()
    {
        var policy = """
            {
              "id": "md",
              "gauntlet": {
                "expect": {
                  "requireNonEmptyFingerprint": true,
                  "requireTrueLaunchedShooters": [ "jas-39c-gripen-2005", "a-19-gotland-2022" ]
                }
              }
            }
            """;
        // Only surface launched — multi-domain bar fails closed
        var csv = """
            scenarioId,seed,side,score,kills,missilesFired,denials,fingerprint
            md,42,BLUE,100,1,2,0,Engagement|1|1|1|k-31-visby-2009|1|True|Launched EngagementOutcome|2|1|1|1|em-sovremenny-i-pr-956-sarych|Kill|0.1
            """;
        var result = GauntletOracleEvaluator.EvaluateFromPolicyAndCsv(policy, csv);
        Assert.False(result.Passed);
        Assert.Contains(result.Failures, f => f.Contains("jas-39c-gripen-2005", StringComparison.Ordinal));
        Assert.Contains(result.Failures, f => f.Contains("a-19-gotland-2022", StringComparison.Ordinal));
    }

    [Fact]
    public void EvaluateFromPolicyAndCsv_pass_when_inject_and_multidomain_tokens_present()
    {
        var policy = """
            {
              "id": "ok",
              "gauntlet": {
                "expect": {
                  "requireNonEmptyFingerprint": true,
                  "requireFingerprintSubstrings": [ "CommsStateChange", "Degraded" ],
                  "requireTrueLaunchedShooters": [ "jas-39c-gripen-2005", "a-19-gotland-2022" ]
                }
              }
            }
            """;
        var csv = """
            scenarioId,seed,side,score,kills,missilesFired,denials,fingerprint
            ok,42,BLUE,50,1,4,0,CommsStateChange|38|2|2|net|Nominal|Degraded|seeded Engagement|25|1|1|jas-39c-gripen-2005|1|True|Launched Engagement|28|1|1|a-19-gotland-2022|2|True|Launched
            """;
        var result = GauntletOracleEvaluator.EvaluateFromPolicyAndCsv(policy, csv);
        Assert.True(result.Passed, string.Join("; ", result.Failures));
    }
}
