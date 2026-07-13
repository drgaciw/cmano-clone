namespace ProjectAegis.Data.Scenario.Authoring;

public sealed class ScenarioMetadataDto
{
    public string? Title { get; init; }

    public string? Description { get; init; }

    public string? Author { get; init; }

    public int SchemaVersion { get; init; } = 1;

    public string? DbRef { get; init; }

    public string? DbSnapshotId { get; init; }

    public int EditVersion { get; init; }

    /// <summary>Scenario RNG root for headless sample (GDD metadata.seed).</summary>
    public ulong Seed { get; init; } = 42;

    /// <summary>Maps to <c>data/scenarios/*.policy.json</c> id for harness sample runs.</summary>
    public string? PolicyId { get; init; }

    /// <summary>Req-06 TL branch binding at scenario load (TL-0…TL-5); validated at load, not mid-tick.</summary>
    public string? TlBranch { get; init; }

    /// <summary>Per-unit launch readiness for validation (req 16).</summary>
    public Dictionary<string, ScenarioUnitReadinessDto>? UnitReadiness { get; init; }

    /// <summary>Scenario max technology level for near-future gates (req 09).</summary>
    public int MaxTechnologyLevel { get; init; } = 2;

    /// <summary>Near-future units to spawn at harness start (headless runtime).</summary>
    public List<ScenarioNearFutureUnitDto>? NearFutureUnits { get; init; }

    /// <summary>Side-level ROE default (AME-3.2). Null means WeaponsFree.</summary>
    public string? SideRoe { get; init; }
}

public sealed class ScenarioUnitReadinessDto
{
    public bool ReadyForLaunch { get; init; } = true;
}