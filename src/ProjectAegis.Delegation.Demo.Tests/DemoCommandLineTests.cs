using NUnit.Framework;
using ProjectAegis.Delegation.Demo;

namespace ProjectAegis.Delegation.Demo.Tests;

public sealed class DemoCommandLineTests
{
    [Test]
    public void Experiment_flag_selects_seed_grid_batch()
    {
        var parsed = DemoCommandLine.Parse(["--batch", "--experiment", "grid.experiment.json", "--csv-out", "out.csv"]);

        Assert.That(parsed.Kind, Is.EqualTo(DemoParseKind.Ok));
        Assert.That(parsed.Invocation!.Mode, Is.EqualTo(DemoMode.ExperimentBatch));
        Assert.That(parsed.Invocation.ExperimentPath, Is.EqualTo("grid.experiment.json"));
        Assert.That(parsed.Invocation.CsvOut, Is.EqualTo("out.csv"));
    }

    [Test]
    public void Experiment_without_batch_is_a_usage_error()
    {
        var parsed = DemoCommandLine.Parse(["--experiment", "grid.experiment.json"]);

        Assert.That(parsed.Kind, Is.EqualTo(DemoParseKind.Error));
        Assert.That(parsed.Error, Does.Contain("--batch"));
    }

    [Test]
    public void Experiment_rejects_seed_list_overrides()
    {
        var parsed = DemoCommandLine.Parse(["--batch", "--experiment", "grid.experiment.json", "--seeds", "1,2"]);

        Assert.That(parsed.Kind, Is.EqualTo(DemoParseKind.Error));
        Assert.That(parsed.Error, Does.Contain("--seeds"));
    }

    [Test]
    public void Legacy_batch_flags_stay_on_the_scenario_seed_path()
    {
        var parsed = DemoCommandLine.Parse(["--batch", "--scenarios", "baltic-patrol", "--seeds", "7,8", "--ticks", "4"]);

        Assert.That(parsed.Kind, Is.EqualTo(DemoParseKind.Ok));
        Assert.That(parsed.Invocation!.Mode, Is.EqualTo(DemoMode.Batch));
        Assert.That(parsed.Invocation.Scenarios, Is.EqualTo(new[] { "baltic-patrol" }));
        Assert.That(parsed.Invocation.Seeds, Is.EqualTo(new[] { 7, 8 }));
        Assert.That(parsed.Invocation.Ticks, Is.EqualTo(4));
    }
}
