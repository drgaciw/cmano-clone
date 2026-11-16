using NUnit.Framework;

namespace ProjectAegis.Delegation.Demo.Tests;

public sealed class ExperimentBatchReportTests
{
    [Test]
    public void Report_states_labels_do_not_change_sim_and_preserves_fingerprints()
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

        var rows = ExperimentSeedGridBatch.Run(definition);
        var csv = ExperimentSeedGridBatch.ExportCsv(rows, definition);
        var report = ExperimentBatchReport.Build(rows, definition);
        var csvAgain = ExperimentSeedGridBatch.ExportCsv(rows, definition);

        Assert.That(report, Does.Contain(ExperimentBatchReport.LabelDisclaimer));
        Assert.That(report, Does.Contain("label.doctrineId=hold"));
        Assert.That(report, Does.Contain("label.doctrineId=press"));
        Assert.That(report, Does.Contain("processIsolation=required-until-static-state-audited"));
        Assert.That(csvAgain, Is.EqualTo(csv));
        Assert.That(rows[0].Result.FingerprintSha256, Is.EqualTo(rows[1].Result.FingerprintSha256));
        Assert.That(report, Does.Contain(rows[0].Result.FingerprintSha256));
    }
}
