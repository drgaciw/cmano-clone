using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Baltic;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Baltic;

public sealed class BalticBatchRunnerTests
{
    [Test]
    public void Run_exports_csv_for_multiple_scenarios_and_seeds()
    {
        var rows = BalticBatchRunner.Run(new BalticBatchRunner.BatchRequest(
            ["baltic-patrol", "baltic-patrol-comms"],
            [7, 8],
            4,
            true));

        Assert.That(rows, Has.Count.EqualTo(4));
        var csv = BalticBatchRunner.ExportCsv(rows);
        Assert.That(csv, Does.StartWith(LossesScoringCsvExporter.Header));
        Assert.That(csv.Split('\n', StringSplitOptions.RemoveEmptyEntries), Has.Length.EqualTo(5));
    }

    [Test]
    public void DiscoverScenarioIds_includes_comms_fixture()
    {
        var ids = BalticBatchRunner.DiscoverScenarioIds();
        Assert.That(ids, Does.Contain("baltic-patrol-comms"));
        Assert.That(ids, Does.Contain("baltic-patrol"));
    }
}