using ProjectAegis.Data.Catalog;
using Xunit;

namespace ProjectAegis.Data.Tests.Catalog;

/// <summary>
/// RED→GREEN calibration: gauntlet ladder multi-seed outcomes (ticks 6/16/40)
/// must satisfy <c>gauntlet.expect</c> after denser catalog ORBATs.
/// Defects: GAUNTLET-20260723-T1-B, T3-EVENT, T3-IDROE, T5-ROE.
/// </summary>
public sealed class GauntletLadderOracleExpectCalibrationTests
{
    [Fact]
    public void T1_patrol_b_multi_seed_kills_variance_passes_expect()
    {
        // seed=7 → 2 kills, seed=123 → 1 kill under ticks=6 denser ORBAT
        var policy = File.ReadAllText(PolicyPath("gauntlet-t1-patrol-b"));
        var csv =
            """
            scenarioId,seed,side,score,kills,missilesFired,denials,fingerprint
            gauntlet-t1-patrol-b,42,BLUE,400,4,6,0,PolicyUpdate|1|0|0|1|roe|WeaponsFree|WeaponsFree
            gauntlet-t1-patrol-b,7,BLUE,200,2,9,0,PolicyUpdate|1|0|0|1|roe|WeaponsFree|WeaponsFree
            gauntlet-t1-patrol-b,123,BLUE,100,1,10,0,PolicyUpdate|1|0|0|1|roe|WeaponsFree|WeaponsFree
            """;

        var result = GauntletOracleEvaluator.EvaluateFromPolicyAndCsv(policy, csv);
        Assert.True(result.Passed, string.Join("; ", result.Failures));
    }

    [Fact]
    public void T3_event_chain_high_score_passes_expect()
    {
        var policy = File.ReadAllText(PolicyPath("gauntlet-t3-event-chain"));
        var csv =
            """
            scenarioId,seed,side,score,kills,missilesFired,denials,fingerprint
            gauntlet-t3-event-chain,42,BLUE,600,6,20,0,PolicyUpdate|1|0|0|1|roe|WeaponsFree|WeaponsFree
            gauntlet-t3-event-chain,7,BLUE,400,4,24,0,PolicyUpdate|1|0|0|1|roe|WeaponsFree|WeaponsFree
            gauntlet-t3-event-chain,123,BLUE,500,5,20,0,PolicyUpdate|1|0|0|1|roe|WeaponsFree|WeaponsFree
            """;

        var result = GauntletOracleEvaluator.EvaluateFromPolicyAndCsv(policy, csv);
        Assert.True(result.Passed, string.Join("; ", result.Failures));
    }

    [Fact]
    public void T3_id_roe_weapons_tight_denial_storm_passes_expect()
    {
        // ID-required / WeaponsTight: zero kills, high denials across seeds (deterministic)
        var policy = File.ReadAllText(PolicyPath("gauntlet-t3-id-roe"));
        var csv =
            """
            scenarioId,seed,side,score,kills,missilesFired,denials,fingerprint
            gauntlet-t3-id-roe,42,BLUE,-560,0,0,112,PolicyUpdate|1|0|0|1|roe|WeaponsTight|WeaponsTight
            gauntlet-t3-id-roe,7,BLUE,-560,0,0,112,PolicyUpdate|1|0|0|1|roe|WeaponsTight|WeaponsTight
            gauntlet-t3-id-roe,123,BLUE,-560,0,0,112,PolicyUpdate|1|0|0|1|roe|WeaponsTight|WeaponsTight
            """;

        var result = GauntletOracleEvaluator.EvaluateFromPolicyAndCsv(policy, csv);
        Assert.True(result.Passed, string.Join("; ", result.Failures));
    }

    [Fact]
    public void T5_roe_change_long_run_denial_storm_passes_expect()
    {
        // ticks=40 denser ORBAT + WeaponsTight window → denials≈360, score≈-1700
        var policy = File.ReadAllText(PolicyPath("gauntlet-t5-roe-change"));
        var csv =
            """
            scenarioId,seed,side,score,kills,missilesFired,denials,fingerprint
            gauntlet-t5-roe-change,42,BLUE,-1700,1,2,360,CommsStateChange|1|3|3|net|Nominal|Degraded|roe
            gauntlet-t5-roe-change,7,BLUE,-1700,1,6,360,CommsStateChange|1|3|3|net|Nominal|Degraded|roe
            gauntlet-t5-roe-change,123,BLUE,-1700,1,2,360,CommsStateChange|1|3|3|net|Nominal|Degraded|roe
            """;

        var result = GauntletOracleEvaluator.EvaluateFromPolicyAndCsv(policy, csv);
        Assert.True(result.Passed, string.Join("; ", result.Failures));
    }

    private static string PolicyPath(string scenarioId)
    {
        var repo = FindRepoRoot();
        return Path.Combine(repo, "data", "scenarios", $"{scenarioId}.policy.json");
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "ProjectAegis.sln")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        return dir!.FullName;
    }
}
