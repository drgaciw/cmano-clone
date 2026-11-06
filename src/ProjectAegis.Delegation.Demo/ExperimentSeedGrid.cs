namespace ProjectAegis.Delegation.Demo;

/// <summary>
/// Expands an <see cref="ExperimentDefinition"/> into an ordered seed grid.
/// Cartesian mode emits the full product. Latin-hypercube mode emits a deterministic discrete sample
/// whose length is the longest active axis. Seeds are always fully represented when the sample is at
/// least as long as the seed list; longer opaque sweeps repeat seeds by a fixed coprime step.
/// </summary>
public static class ExperimentSeedGrid
{
    /// <summary>Every schema parameter, in expansion order.</summary>
    public static IReadOnlyList<ExperimentParameter> AllParameters { get; } =
    [
        ExperimentParameter.Seed,
        ExperimentParameter.DoctrineId,
        ExperimentParameter.RoeId,
        ExperimentParameter.ForceRatioId,
    ];

    /// <summary>Expands <paramref name="definition"/> into grid points. Order is deterministic.</summary>
    public static IReadOnlyList<ExperimentGridPoint> Expand(ExperimentDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (definition.Seeds == null || definition.Seeds.Count == 0)
        {
            throw new ExperimentDefinitionException("seeds must contain at least one seed.");
        }

        var active = definition.Parameters is { Count: > 0 }
            ? definition.Parameters
            : AllParameters;
        var seeds = definition.Seeds;
        var doctrines = Levels(active, ExperimentParameter.DoctrineId, definition.DoctrineIds, definition.DoctrineId);
        var roes = Levels(active, ExperimentParameter.RoeId, definition.RoeIds, definition.RoeId);
        var forces = Levels(active, ExperimentParameter.ForceRatioId, definition.ForceRatioIds, definition.ForceRatioId);

        return definition.GridMethod switch
        {
            ExperimentGridMethod.Cartesian => ExpandCartesian(seeds, doctrines, roes, forces),
            ExperimentGridMethod.LatinHypercube => ExpandLatin(seeds, doctrines, roes, forces),
            _ => throw new ExperimentDefinitionException("Unsupported grid method."),
        };
    }

    private static IReadOnlyList<string> Levels(
        IReadOnlyList<ExperimentParameter> active,
        ExperimentParameter parameter,
        IReadOnlyList<string>? sweep,
        string? scalar)
    {
        if (!Contains(active, parameter))
        {
            return [scalar ?? ""];
        }

        if (sweep is { Count: > 0 })
        {
            return sweep;
        }

        return [scalar ?? ""];
    }

    private static bool Contains(IReadOnlyList<ExperimentParameter> active, ExperimentParameter parameter)
    {
        foreach (var item in active)
        {
            if (item == parameter)
            {
                return true;
            }
        }

        return false;
    }

    private static List<ExperimentGridPoint> ExpandCartesian(
        IReadOnlyList<int> seeds,
        IReadOnlyList<string> doctrines,
        IReadOnlyList<string> roes,
        IReadOnlyList<string> forces)
    {
        var points = new List<ExperimentGridPoint>(seeds.Count * doctrines.Count * roes.Count * forces.Count);
        foreach (var seed in seeds)
        {
            foreach (var doctrine in doctrines)
            {
                foreach (var roe in roes)
                {
                    foreach (var force in forces)
                    {
                        points.Add(new ExperimentGridPoint(seed, doctrine, roe, force));
                    }
                }
            }
        }

        return points;
    }

    private static List<ExperimentGridPoint> ExpandLatin(
        IReadOnlyList<int> seeds,
        IReadOnlyList<string> doctrines,
        IReadOnlyList<string> roes,
        IReadOnlyList<string> forces)
    {
        var sampleCount = Math.Max(seeds.Count, Math.Max(doctrines.Count, Math.Max(roes.Count, forces.Count)));
        var seedStep = LatinStep(seeds.Count, factorOrdinal: 0);
        var doctrineStep = LatinStep(doctrines.Count, factorOrdinal: 1);
        var roeStep = LatinStep(roes.Count, factorOrdinal: 2);
        var forceStep = LatinStep(forces.Count, factorOrdinal: 3);
        var points = new List<ExperimentGridPoint>(sampleCount);
        for (var i = 0; i < sampleCount; i++)
        {
            points.Add(new ExperimentGridPoint(
                seeds[(i * seedStep) % seeds.Count],
                doctrines[(i * doctrineStep) % doctrines.Count],
                roes[(i * roeStep) % roes.Count],
                forces[(i * forceStep) % forces.Count]));
        }

        return points;
    }

    /// <summary>
    /// First integer &gt;= <paramref name="factorOrdinal"/> + 1 that is coprime with
    /// <paramref name="levelCount"/>. Factor 0 (seed) therefore steps by 1 and keeps seed order
    /// when the sample is no longer than the seed list.
    /// </summary>
    private static int LatinStep(int levelCount, int factorOrdinal)
    {
        if (levelCount <= 1)
        {
            return 1;
        }

        var preferred = factorOrdinal + 1;
        for (var step = preferred; step < levelCount; step++)
        {
            if (GreatestCommonDivisor(step, levelCount) == 1)
            {
                return step;
            }
        }

        for (var step = 1; step < preferred; step++)
        {
            if (GreatestCommonDivisor(step, levelCount) == 1)
            {
                return step;
            }
        }

        return 1;
    }

    private static int GreatestCommonDivisor(int a, int b)
    {
        while (b != 0)
        {
            var remainder = a % b;
            a = b;
            b = remainder;
        }

        return Math.Abs(a);
    }
}
