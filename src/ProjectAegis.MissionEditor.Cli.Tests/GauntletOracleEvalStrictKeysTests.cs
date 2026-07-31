namespace ProjectAegis.MissionEditor.Cli.Tests;

using ProjectAegis.MissionEditor.Cli;
using Xunit;

/// <summary>
/// Cli surface: unknown keys / legacy emcon warnings come from Data
/// <c>GauntletOracleEvaluator.EvaluateFromPolicyAndCsv</c> (no Cli whitelist).
/// </summary>
public class GauntletOracleEvalStrictKeysTests
{
    private const string Csv =
        "scenarioId,seed,side,score,kills,missilesFired,denials,fingerprint\n"
        + "p-strict,42,BLUE,70,1,1,6,TOKEN_A TOKEN_B\n";

    private static string WriteTemp(string name, string content)
    {
        var dir = Directory.CreateTempSubdirectory("strictkeys").FullName;
        var path = Path.Combine(dir, name);
        File.WriteAllText(path, content);
        return path;
    }

    private static string PolicyJson(string extraGauntletKey) =>
        $$"""
        {
          "id": "p-strict",
          "gauntlet": {
            "intent": "strict-key test", "tier": 1, "runId": "t"{{extraGauntletKey}},
            "expect": { "side": "BLUE", "minKills": 1, "maxScore": 90.0, "minScore": 50.0 }
          }
        }
        """;

    [Fact]
    public void Unknown_key_fails_eval_with_exit_1()
    {
        var policy = WriteTemp("p.policy.json", PolicyJson(", \"emconPhases\": []"));
        var csv = WriteTemp("r.csv", Csv);
        using var sw = new StringWriter();
        var exit = GauntletOracleEvalCommand.Run(policy, null, csv, null, sw);
        Assert.Equal(1, exit);
        Assert.Contains("emconPhases", sw.ToString());
    }

    [Fact]
    public void Legacy_emcon_warns_but_still_passes()
    {
        var policy = WriteTemp("p.policy.json", PolicyJson(", \"emcon\": \"phased\""));
        var csv = WriteTemp("r.csv", Csv);
        using var sw = new StringWriter();
        var exit = GauntletOracleEvalCommand.Run(policy, null, csv, null, sw);
        Assert.Equal(0, exit);
        Assert.Contains("warnings", sw.ToString());
        Assert.Contains("emcon", sw.ToString());
    }
}
