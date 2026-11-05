namespace ProjectAegis.Delegation.UnityAdapter.Experiments;

/// <summary>A single-scenario seed grid. Doctrine and ROE identifiers are metadata, not policy overrides.</summary>
public sealed record ExperimentDefinition
{
    /// <summary>Creates a validated definition and copies the ordered seed grid, including duplicates.</summary>
    public ExperimentDefinition(string scenarioId, IReadOnlyList<int> seeds, int ticks,
        string? doctrineId = null, string? roeId = null)
    {
        if (string.IsNullOrWhiteSpace(scenarioId))
            throw new ArgumentException("scenarioId must be a nonblank string.", nameof(scenarioId));
        if (seeds == null || seeds.Count == 0)
            throw new ArgumentException("seeds must be a nonempty array of signed 32-bit integers.", nameof(seeds));
        if (ticks <= 0)
            throw new ArgumentException("ticks must be a positive 32-bit integer.", nameof(ticks));
        if (doctrineId != null && string.IsNullOrWhiteSpace(doctrineId))
            throw new ArgumentException("doctrineId must be a nonblank string when supplied.", nameof(doctrineId));
        if (roeId != null && string.IsNullOrWhiteSpace(roeId))
            throw new ArgumentException("roeId must be a nonblank string when supplied.", nameof(roeId));

        ScenarioId = scenarioId;
        Seeds = Array.AsReadOnly(seeds.ToArray());
        Ticks = ticks;
        DoctrineId = doctrineId;
        RoeId = roeId;
    }

    /// <summary>Existing scenario policy identifier resolved by the batch runner.</summary>
    public string ScenarioId { get; }

    /// <summary>Seeds in execution order. Duplicates request repeated runs; seeds are never sorted or deduplicated.</summary>
    public IReadOnlyList<int> Seeds { get; }

    /// <summary>Positive number of ticks per run.</summary>
    public int Ticks { get; }

    /// <summary>Optional opaque doctrine label. Does not override scenario policy.</summary>
    public string? DoctrineId { get; }

    /// <summary>Optional opaque ROE label. Does not override scenario policy.</summary>
    public string? RoeId { get; }
}
