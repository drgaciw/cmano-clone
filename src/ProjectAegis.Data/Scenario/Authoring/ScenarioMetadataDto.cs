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
}

public sealed class ScenarioUnitReadinessDto
{
    public bool ReadyForLaunch { get; init; } = true;
}