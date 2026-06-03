namespace ProjectAegis.Data.Scenario.Authoring;

/// <summary>Minimal canonical scenario shape for validation engine v1 (ADR-008).</summary>
public sealed class ScenarioDocumentDto
{
    public ScenarioMetadataDto Metadata { get; init; } = new();

    public IReadOnlyList<ScenarioMissionDto> Missions { get; init; } = Array.Empty<ScenarioMissionDto>();
}

public sealed class ScenarioMissionDto
{
    public string Id { get; init; } = "";

    public string Type { get; init; } = "";

    public IReadOnlyList<string> AssignedUnitIds { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> TargetIds { get; init; } = Array.Empty<string>();

    public string? FerryDestinationBaseId { get; init; }

    public IReadOnlyList<ScenarioWaypointDto> PatrolZone { get; init; } = Array.Empty<ScenarioWaypointDto>();
}

public sealed class ScenarioWaypointDto
{
    public double Lat { get; init; }

    public double Lon { get; init; }
}