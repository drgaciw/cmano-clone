using NUnit.Framework;
using ProjectAegis.Delegation.Demo;
using ProjectAegis.Delegation.UnityAdapter.Baltic;

namespace ProjectAegis.Delegation.Demo.Tests;

public sealed class ExperimentSeedGridBatchTests
{
    [Test]
    public void Same_definition_repeats_fingerprint_columns_for_identical_seeds()
    {
        var definition = ExperimentDefinitionLoader.Parse("""
            {
              "scenarioId": "baltic-patrol",
              "ticks": 4,
              "seeds": [42],
              "side": "BLUE",
              "doctrineId": "hold",
              "grid": {
                "method": "cartesian",
                "parameters": ["seed", "doctrineId"]
              },
              "sweeps": { "doctrineIds": ["hold", "press"] }
            }
            """);

        var first = ExperimentSeedGridBatch.Run(definition);
        var secondCsv = ExperimentSeedGridBatch.ExportCsv(ExperimentSeedGridBatch.Run(definition), definition);
        var firstCsv = ExperimentSeedGridBatch.ExportCsv(first, definition);
        var direct = BalticReplayHarness.Run(42, "baltic-patrol", 4, mvpEngagement: true);

        Assert.That(first, Has.Count.EqualTo(2));
        Assert.That(first[0].Point.DoctrineId, Is.EqualTo("hold"));
        Assert.That(first[1].Point.DoctrineId, Is.EqualTo("press"));
        Assert.That(first[0].Result.FingerprintSha256, Is.EqualTo(first[1].Result.FingerprintSha256));
        Assert.That(first[0].Result.FingerprintSha256, Is.EqualTo(direct.FingerprintSha256));
        Assert.That(first[0].Result.WorldHash, Is.EqualTo(direct.WorldHash));
        Assert.That(secondCsv, Is.EqualTo(firstCsv));
        Assert.That(firstCsv, Does.StartWith(ExperimentSeedGridBatch.Header));
        Assert.That(ExperimentSeedGridBatch.Header, Does.Contain("fingerprint,fingerprintSha256,worldHash,detectionWorldHash"));
        Assert.That(firstCsv, Does.Contain(direct.FingerprintSha256));
        Assert.That(firstCsv.TrimEnd().Split('\n'), Has.Length.EqualTo(3));
    }

    [Test]
    public void Unknown_scenario_is_rejected_before_a_run()
    {
        var definition = ExperimentDefinitionLoader.Parse("""
            { "scenarioId": "not-a-scenario", "ticks": 1, "seeds": [1] }
            """);

        var ex = Assert.Throws<ExperimentDefinitionException>(() => ExperimentSeedGridBatch.Run(definition));
        Assert.That(ex!.Message, Does.Contain("not-a-scenario"));
    }
}
