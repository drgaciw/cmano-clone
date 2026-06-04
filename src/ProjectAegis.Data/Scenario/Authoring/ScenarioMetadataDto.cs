namespace ProjectAegis.Data.Scenario.Authoring;

public sealed class ScenarioMetadataDto
{
    public string? DbRef { get; init; }

    public string? DbSnapshotId { get; init; }

    public int EditVersion { get; init; }

    /// <summary>Scenario RNG root for headless sample (GDD metadata.seed).</summary>
    public ulong Seed { get; init; } = 42;

    /// <summary>Maps to <c>data/scenarios/*.policy.json</c> id for harness sample runs.</summary>
    public string? PolicyId { get; init; }

    /// <summary>Per-unit launch readiness for validation (req 16).</summary>
    public Dictionary<string, ScenarioUnitReadinessDto>? UnitReadiness { get; init; }

    /// <summary>Scenario max technology level for near-future gates (req 09).</summary>
    public int MaxTechnologyLevel { get; init; } = 2;

    /// <summary>Near-future units to spawn at harness start (headless runtime).</summary>
    public List<ScenarioNearFutureUnitDto>? NearFutureUnits { get; init; }
}

public sealed class ScenarioUnitReadinessDto
{
    public bool ReadyForLaunch { get; init; } = true;
}