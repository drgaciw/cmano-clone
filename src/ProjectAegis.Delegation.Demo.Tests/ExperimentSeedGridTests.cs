using NUnit.Framework;
using ProjectAegis.Delegation.Demo;

namespace ProjectAegis.Delegation.Demo.Tests;

public sealed class ExperimentSeedGridTests
{
    [Test]
    public void Cartesian_crosses_seeds_with_opaque_sweeps()
    {
        var definition = Definition(
            seeds: [10, 20],
            method: ExperimentGridMethod.Cartesian,
            parameters: ExperimentSeedGrid.AllParameters,
            doctrineIds: ["alpha", "bravo"]);

        var points = ExperimentSeedGrid.Expand(definition);

        Assert.That(points.Select(p => (p.Seed, p.DoctrineId)).ToArray(), Is.EqualTo(new[]
        {
            (10, "alpha"),
            (10, "bravo"),
            (20, "alpha"),
            (20, "bravo"),
        }));
    }

    [Test]
    public void Parameter_list_collapses_axes_that_are_not_selected()
    {
        var definition = Definition(
            seeds: [10, 20],
            method: ExperimentGridMethod.Cartesian,
            parameters: [ExperimentParameter.Seed],
            doctrineIds: ["alpha", "bravo"],
            doctrineId: "hold");

        var points = ExperimentSeedGrid.Expand(definition);

        Assert.That(points.Select(p => (p.Seed, p.DoctrineId)).ToArray(), Is.EqualTo(new[]
        {
            (10, "hold"),
            (20, "hold"),
        }));
    }

    [Test]
    public void Latin_hypercube_samples_the_longest_axis()
    {
        var definition = Definition(
            seeds: [10, 20, 30],
            method: ExperimentGridMethod.LatinHypercube,
            parameters: [ExperimentParameter.Seed, ExperimentParameter.DoctrineId],
            doctrineIds: ["alpha", "bravo"]);

        var points = ExperimentSeedGrid.Expand(definition);

        Assert.That(points.Select(p => (p.Seed, p.DoctrineId)).ToArray(), Is.EqualTo(new[]
        {
            (10, "alpha"),
            (20, "bravo"),
            (30, "alpha"),
        }));
    }

    private static ExperimentDefinition Definition(
        int[] seeds,
        ExperimentGridMethod method,
        IReadOnlyList<ExperimentParameter> parameters,
        IReadOnlyList<string>? doctrineIds = null,
        string? doctrineId = null)
    {
        return new ExperimentDefinition(
            "baltic-patrol",
            4,
            seeds,
            "BLUE",
            true,
            doctrineId,
            null,
            null,
            method,
            parameters,
            doctrineIds ?? [],
            [],
            []);
    }
}
