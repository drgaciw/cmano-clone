using System.Text.Json;
using NUnit.Framework;

namespace ProjectAegis.MissionEditor.Cli.Tests;

/// <summary>
/// CLI contract for <c>gauntlet_oracle_eval</c> — drives
/// <see cref="GauntletOracleEvalCommand.Run"/> which calls
/// <c>GauntletOracleEvaluator.EvaluateFromPolicyAndCsv</c>.
/// </summary>
[TestFixture]
public sealed class GauntletOracleEvalCommandTests
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
        other-scenario,1,BLUE,0,0,0,0,fp
        """;

    [Test]
    public void Pass_case_minimal_policy_and_csv_within_bounds_exit_0()
    {
        var dir = CreateTempDir();
        try
        {
            var policyPath = Path.Combine(dir, "pass.policy.json");
            var csvPath = Path.Combine(dir, "results.csv");
            var outPath = Path.Combine(dir, "oracle-eval.json");
            File.WriteAllText(policyPath, PassPolicy);
            File.WriteAllText(csvPath, PassCsv);

            using var writer = new StringWriter();
            var exit = GauntletOracleEvalCommand.Run(policyPath, null, csvPath, outPath, writer);
            Assert.That(exit, Is.EqualTo(0));

            using var doc = JsonDocument.Parse(writer.ToString());
            var root = doc.RootElement;
            Assert.That(root.GetProperty("ok").GetBoolean(), Is.True);
            Assert.That(root.GetProperty("allPassed").GetBoolean(), Is.True);

            var scenarios = root.GetProperty("scenarios").EnumerateArray().ToArray();
            Assert.That(scenarios, Has.Exactly(1).Items);
            Assert.That(scenarios[0].GetProperty("scenario").GetString(), Is.EqualTo("gauntlet-t1-patrol-a"));
            Assert.That(scenarios[0].GetProperty("passed").GetBoolean(), Is.True);
            Assert.That(scenarios[0].GetProperty("failures").EnumerateArray(), Is.Empty);
            Assert.That(scenarios[0].GetProperty("rows").GetInt32(), Is.EqualTo(1));

            Assert.That(File.Exists(outPath), Is.True);
            using var outDoc = JsonDocument.Parse(File.ReadAllText(outPath));
            Assert.That(outDoc.RootElement.GetProperty("allPassed").GetBoolean(), Is.True);
        }
        finally
        {
            CleanupDir(dir);
        }
    }

    [Test]
    public void Fail_case_missing_expect_exit_1()
    {
        var dir = CreateTempDir();
        try
        {
            var policyPath = Path.Combine(dir, "no-expect.policy.json");
            var csvPath = Path.Combine(dir, "results.csv");
            File.WriteAllText(policyPath, """{ "id": "gauntlet-t1-patrol-a", "gauntlet": { "intent": "only" } }""");
            File.WriteAllText(csvPath, PassCsv);

            using var writer = new StringWriter();
            var exit = GauntletOracleEvalCommand.Run(policyPath, null, csvPath, null, writer);
            Assert.That(exit, Is.EqualTo(1));

            using var doc = JsonDocument.Parse(writer.ToString());
            var root = doc.RootElement;
            Assert.That(root.GetProperty("ok").GetBoolean(), Is.False);
            Assert.That(root.GetProperty("allPassed").GetBoolean(), Is.False);
            var scenario = root.GetProperty("scenarios")[0];
            Assert.That(scenario.GetProperty("passed").GetBoolean(), Is.False);
            Assert.That(scenario.GetProperty("failures").EnumerateArray().Select(e => e.GetString() ?? "").Any(f => f.Contains("missing gauntlet.expect", StringComparison.Ordinal)), Is.True);
        }
        finally
        {
            CleanupDir(dir);
        }
    }

    [Test]
    public void Fail_case_missiles_out_of_bounds_exit_1()
    {
        var dir = CreateTempDir();
        try
        {
            var policyPath = Path.Combine(dir, "strict.policy.json");
            var csvPath = Path.Combine(dir, "results.csv");
            File.WriteAllText(policyPath, """
                {
                  "id": "gauntlet-t1-patrol-a",
                  "gauntlet": {
                    "expect": { "maxMissilesFired": 0, "requireNonEmptyFingerprint": true }
                  }
                }
                """);
            File.WriteAllText(csvPath, PassCsv);

            using var writer = new StringWriter();
            var exit = GauntletOracleEvalCommand.Run(policyPath, null, csvPath, null, writer);
            Assert.That(exit, Is.EqualTo(1));

            using var doc = JsonDocument.Parse(writer.ToString());
            Assert.That(doc.RootElement.GetProperty("allPassed").GetBoolean(), Is.False);
            var failures = doc.RootElement.GetProperty("scenarios")[0]
                .GetProperty("failures").EnumerateArray()
                .Select(e => e.GetString() ?? "")
                .ToArray();
            Assert.That(failures.Any(f => f.Contains("missilesFired", StringComparison.Ordinal)), Is.True);
        }
        finally
        {
            CleanupDir(dir);
        }
    }

    [Test]
    public void Policy_dir_aggregates_each_policy_json()
    {
        var dir = CreateTempDir();
        try
        {
            File.WriteAllText(Path.Combine(dir, "a.policy.json"), PassPolicy);
            File.WriteAllText(Path.Combine(dir, "b.policy.json"), """
                {
                  "id": "other-scenario",
                  "gauntlet": {
                    "expect": {
                      "side": "BLUE",
                      "maxMissilesFired": 0,
                      "requireNonEmptyFingerprint": true
                    }
                  }
                }
                """);
            // Non-matching suffix must be ignored (non-recursive *.policy.json only).
            File.WriteAllText(Path.Combine(dir, "skip.json"), """{ "id": "ignored" }""");

            var csvPath = Path.Combine(dir, "results.csv");
            File.WriteAllText(csvPath, PassCsv);

            using var writer = new StringWriter();
            var exit = GauntletOracleEvalCommand.Run(null, dir, csvPath, null, writer);
            Assert.That(exit, Is.EqualTo(0));

            using var doc = JsonDocument.Parse(writer.ToString());
            Assert.That(doc.RootElement.GetProperty("allPassed").GetBoolean(), Is.True);
            var scenarios = doc.RootElement.GetProperty("scenarios").EnumerateArray()
                .Select(s => s.GetProperty("scenario").GetString() ?? string.Empty)
                .OrderBy(s => s, StringComparer.Ordinal)
                .ToArray();
            Assert.That(scenarios, Is.EqualTo(new[] { "gauntlet-t1-patrol-a", "other-scenario" }));
        }
        finally
        {
            CleanupDir(dir);
        }
    }

    [Test]
    public void Help_text_is_registered()
    {
        using var writer = new StringWriter();
        GauntletOracleEvalCommand.PrintHelp(writer);
        var help = writer.ToString();
        Assert.That(help, Does.Contain("gauntlet_oracle_eval"));
        Assert.That(help, Does.Contain("--policy"));
        Assert.That(help, Does.Contain("--policy-dir"));
        Assert.That(help, Does.Contain("--csv"));
    }

    private static string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"aegis-gauntlet-oracle-eval-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static void CleanupDir(string dir)
    {
        if (Directory.Exists(dir))
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
