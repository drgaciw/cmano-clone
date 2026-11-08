namespace ProjectAegis.Delegation.Demo;

/// <summary>How an <see cref="ExperimentDefinition"/> combines its seed grid with opaque sweeps.</summary>
public enum ExperimentGridMethod
{
    /// <summary>Full product of the active axes.</summary>
    Cartesian = 0,

    /// <summary>Deterministic discrete Latin-hypercube sample. Size is the longest active axis.</summary>
    LatinHypercube = 1,
}

/// <summary>Swept axis named by the experiment-definition schema.</summary>
public enum ExperimentParameter
{
    /// <summary>Scenario seed. Always executed for every entry in <see cref="ExperimentDefinition.Seeds"/>.</summary>
    Seed = 0,

    /// <summary>Opaque doctrine id.</summary>
    DoctrineId = 1,

    /// <summary>Opaque rules-of-engagement id.</summary>
    RoeId = 2,

    /// <summary>Opaque force-ratio id.</summary>
    ForceRatioId = 3,
}

/// <summary>
/// Typed experiment definition (DRG-257 / INF-6.1). Doctrine, ROE, and force-ratio ids are opaque
/// labels. The seed-grid runner records them and does not apply them to the simulation.
/// </summary>
public sealed record ExperimentDefinition(
    string ScenarioId,
    int Ticks,
    IReadOnlyList<int> Seeds,
    string Side,
    bool MvpEngagement,
    string? DoctrineId,
    string? RoeId,
    string? ForceRatioId,
    ExperimentGridMethod GridMethod,
    IReadOnlyList<ExperimentParameter> Parameters,
    IReadOnlyList<string> DoctrineIds,
    IReadOnlyList<string> RoeIds,
    IReadOnlyList<string> ForceRatioIds,
    int SchemaVersion = 1);

/// <summary>One point on the expanded seed grid.</summary>
public sealed record ExperimentGridPoint(int Seed, string DoctrineId, string RoeId, string ForceRatioId);

/// <summary>Raised when an experiment definition JSON document does not match the schema.</summary>
public sealed class ExperimentDefinitionException : Exception
{
    /// <summary>Creates an exception with the validation message.</summary>
    public ExperimentDefinitionException(string message)
        : base(message)
    {
    }

    /// <summary>Creates an exception that wraps a JSON parse failure.</summary>
    public ExperimentDefinitionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
