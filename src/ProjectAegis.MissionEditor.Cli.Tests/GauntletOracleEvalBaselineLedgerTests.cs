using System.Text.Json;
using NUnit.Framework;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Evidence;

namespace ProjectAegis.MissionEditor.Cli.Tests;

[TestFixture]
public sealed class GauntletOracleEvalBaselineLedgerTests
{
    private const string PolicyJson = """
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
              "requireNonEmptyFingerprint": true
            }
          }
        }
        """;

    private const string CsvText = """
        scenarioId,seed,side,score,kills,missilesFired,denials,fingerprint
        gauntlet-t1-patrol-a,42,BLUE,100,1,1,0,PolicyUpdate|1|0|0|1|roe|WeaponsTight|WeaponsFree Engagement|2|10|10|u1|501|True|Launched
        """;

    [Test]
    public void Baseline_context_out_and_ledger_out_emit_correlated_pending_acceptance_and_evidence()
    {
        var dir = CreateTempDir();
        try
        {
            var policyPath = Path.Combine(dir, "gauntlet-t1-patrol-a.policy.json");
            var csvPath = Path.Combine(dir, "results.csv");
            var outPath = Path.Combine(dir, "oracle-eval.json");
            var ctxOutPath = Path.Combine(dir, "baseline-context.json");
            var ledgerOutPath = Path.Combine(dir, "evidence-ledger.json");

            File.WriteAllText(policyPath, PolicyJson);
            File.WriteAllText(csvPath, CsvText);

            using var writer = new StringWriter();
            var exit = GauntletOracleEvalCommand.Run(
                policyPath,
                null,
                csvPath,
                outPath,
                writer,
                GauntletOracleEvaluator.ProfileLadder,
                baselineContextPath: null,
                baselineContextOutPath: ctxOutPath,
                ledgerOutPath: ledgerOutPath,
                catalogPath: null,
                commitSha: "4b5b4ca1",
                seed: 42,
                ticks: 10);

            Assert.That(exit, Is.EqualTo(0));
            Assert.That(File.Exists(ctxOutPath), Is.True);
            Assert.That(File.Exists(ledgerOutPath), Is.True);

            using var ctxDoc = JsonDocument.Parse(File.ReadAllText(ctxOutPath));
            Assert.That(ctxDoc.RootElement.GetProperty("schemaVersion").GetInt32(), Is.EqualTo(1));
            Assert.That(ctxDoc.RootElement.GetProperty("commit").GetString(), Is.EqualTo("4b5b4ca1"));
            Assert.That(ctxDoc.RootElement.GetProperty("seed").GetInt32(), Is.EqualTo(42));
            Assert.That(ctxDoc.RootElement.GetProperty("ticks").GetInt32(), Is.EqualTo(10));
            Assert.That(ctxDoc.RootElement.GetProperty("results").GetProperty("sha256").GetString(),
                Is.EqualTo(EvidenceLedgerSerializer.ComputeFileSha256Hex(csvPath)));

            var ledger = EvidenceLedgerSerializer.FromJson(File.ReadAllText(ledgerOutPath));
            Assert.That(ledger.OracleEvaluation?.Passed, Is.True);
            var engagementEntry = ledger.Entries.Single(e => e.Kind == MissionThreadEvidenceKind.Engagement);
            Assert.That(engagementEntry.SimTick, Is.EqualTo(10UL));
            Assert.That(engagementEntry.Engagement!.ShooterUnitId, Is.EqualTo("u1"));
            Assert.That(engagementEntry.Engagement.EngagementId, Is.EqualTo(501UL));
            Assert.That(engagementEntry.Engagement.Launched, Is.True);
            var authorityEntry = ledger.Entries.Single(e => e.Kind == MissionThreadEvidenceKind.Authority);
            Assert.That(authorityEntry.SimTick, Is.EqualTo(0UL));
            Assert.That(authorityEntry.Authority!.RoeLevel, Is.EqualTo("WeaponsFree"));
            Assert.That(authorityEntry.Authority.PolicySnapshotId, Is.EqualTo(1L));
            Assert.That(ledger.AcceptanceRecords, Is.Not.Empty);
            Assert.That(ledger.AcceptanceRecords[0].OwnerAcceptance.Status, Is.EqualTo(OwnerAcceptanceStatus.Pending));
        }
        finally
        {
            CleanupDir(dir);
        }
    }

    [Test]
    public void Baseline_context_hash_mismatch_fails_closed_with_exit_1()
    {
        var dir = CreateTempDir();
        try
        {
            var policyPath = Path.Combine(dir, "gauntlet-t1-patrol-a.policy.json");
            var csvPath = Path.Combine(dir, "results.csv");
            var ctxPath = Path.Combine(dir, "baseline-context.json");

            File.WriteAllText(policyPath, PolicyJson);
            File.WriteAllText(csvPath, CsvText);

            var policySha = EvidenceLedgerSerializer.ComputeFileSha256Hex(policyPath);
            var badContextJson = $$"""
                {
                  "schemaVersion": 1,
                  "commit": "4b5b4ca1",
                  "seed": 42,
                  "ticks": 10,
                  "catalog": { "path": "none", "sha256": "none" },
                  "policies": [ { "path": "{{policyPath.Replace("\\", "\\\\")}}", "sha256": "{{policySha}}" } ],
                  "results": { "path": "{{csvPath.Replace("\\", "\\\\")}}", "sha256": "0000000000000000000000000000000000000000000000000000000000000000" }
                }
                """;
            File.WriteAllText(ctxPath, badContextJson);

            using var writer = new StringWriter();
            var exit = GauntletOracleEvalCommand.Run(
                policyPath,
                null,
                csvPath,
                null,
                writer,
                GauntletOracleEvaluator.ProfileLadder,
                baselineContextPath: ctxPath);

            Assert.That(exit, Is.EqualTo(1));
            using var doc = JsonDocument.Parse(writer.ToString());
            Assert.That(doc.RootElement.GetProperty("ok").GetBoolean(), Is.False);
            Assert.That(doc.RootElement.GetProperty("allPassed").GetBoolean(), Is.False);
            Assert.That(doc.RootElement.GetProperty("error").GetString(), Does.Contain("sha256").IgnoreCase);
        }
        finally
        {
            CleanupDir(dir);
        }
    }

    [Test]
    public void Baseline_context_with_empty_policies_or_empty_policy_sha_fails_closed_with_exit_1()
    {
        var dir = CreateTempDir();
        try
        {
            var policyPath = Path.Combine(dir, "gauntlet-t1-patrol-a.policy.json");
            var csvPath = Path.Combine(dir, "results.csv");
            var emptyPoliciesCtx = Path.Combine(dir, "baseline-empty-policies.json");
            var emptyPolicyShaCtx = Path.Combine(dir, "baseline-empty-policy-sha.json");

            File.WriteAllText(policyPath, PolicyJson);
            File.WriteAllText(csvPath, CsvText);
            var csvSha = EvidenceLedgerSerializer.ComputeFileSha256Hex(csvPath);

            File.WriteAllText(emptyPoliciesCtx, $$"""
                {
                  "schemaVersion": 1,
                  "commit": "4b5b4ca1",
                  "seed": 42,
                  "ticks": 10,
                  "catalog": { "path": "none", "sha256": "none" },
                  "policies": [],
                  "results": { "path": "{{csvPath.Replace("\\", "\\\\")}}", "sha256": "{{csvSha}}" }
                }
                """);

            File.WriteAllText(emptyPolicyShaCtx, $$"""
                {
                  "schemaVersion": 1,
                  "commit": "4b5b4ca1",
                  "seed": 42,
                  "ticks": 10,
                  "catalog": { "path": "none", "sha256": "none" },
                  "policies": [ { "path": "{{policyPath.Replace("\\", "\\\\")}}", "sha256": "" } ],
                  "results": { "path": "{{csvPath.Replace("\\", "\\\\")}}", "sha256": "{{csvSha}}" }
                }
                """);

            using var writer1 = new StringWriter();
            var exit1 = GauntletOracleEvalCommand.Run(
                policyPath,
                null,
                csvPath,
                null,
                writer1,
                GauntletOracleEvaluator.ProfileLadder,
                baselineContextPath: emptyPoliciesCtx);
            Assert.That(exit1, Is.EqualTo(1));

            using var writer2 = new StringWriter();
            var exit2 = GauntletOracleEvalCommand.Run(
                policyPath,
                null,
                csvPath,
                null,
                writer2,
                GauntletOracleEvaluator.ProfileLadder,
                baselineContextPath: emptyPolicyShaCtx);
            Assert.That(exit2, Is.EqualTo(1));
        }
        finally
        {
            CleanupDir(dir);
        }
    }

    [Test]
    public void Partial_pass_reports_actual_passed_and_failed_scenario_counts_in_ledger()
    {
        var dir = CreateTempDir();
        try
        {
            var policyDir = Path.Combine(dir, "policies");
            Directory.CreateDirectory(policyDir);
            File.WriteAllText(Path.Combine(policyDir, "a.policy.json"), PolicyJson);
            File.WriteAllText(Path.Combine(policyDir, "b.policy.json"), PolicyJson.Replace("gauntlet-t1-patrol-a", "gauntlet-t1-patrol-b", StringComparison.Ordinal));

            var csvPath = Path.Combine(dir, "results.csv");
            var ledgerOutPath = Path.Combine(dir, "ledger.json");
            File.WriteAllText(csvPath, """
                scenarioId,seed,side,score,kills,missilesFired,denials,fingerprint
                gauntlet-t1-patrol-a,42,BLUE,100,1,1,0,PolicyUpdate|1|0|0|1|roe|WeaponsTight|WeaponsFree
                gauntlet-t1-patrol-b,42,BLUE,0,0,1,0,PolicyUpdate|1|0|0|1|roe|WeaponsTight|WeaponsFree
                """);

            using var writer = new StringWriter();
            var exit = GauntletOracleEvalCommand.Run(
                null,
                policyDir,
                csvPath,
                null,
                writer,
                GauntletOracleEvaluator.ProfileLadder,
                baselineContextPath: null,
                ledgerOutPath: ledgerOutPath);

            Assert.That(exit, Is.EqualTo(1));
            var ledger = EvidenceLedgerSerializer.FromJson(File.ReadAllText(ledgerOutPath));
            Assert.That(ledger.AcceptanceRecords, Has.Count.EqualTo(1));
            Assert.That(ledger.AcceptanceRecords[0].Counts.DeclaredFloor, Is.EqualTo(2));
            Assert.That(ledger.AcceptanceRecords[0].Counts.ObservedPassed, Is.EqualTo(1));
            Assert.That(ledger.AcceptanceRecords[0].Counts.ObservedFailed, Is.EqualTo(1));
        }
        finally
        {
            CleanupDir(dir);
        }
    }

    private static string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"aegis-gauntlet-baseline-{Guid.NewGuid():N}");
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
