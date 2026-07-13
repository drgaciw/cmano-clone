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
}
