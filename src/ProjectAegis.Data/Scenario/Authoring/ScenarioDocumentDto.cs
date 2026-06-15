namespace ProjectAegis.Data.Scenario.Authoring;

/// <summary>Canonical scenario shape for validation engine v1 (ADR-008 / GDD §3.2).</summary>
public sealed class ScenarioDocumentDto
{
    public ScenarioMetadataDto Metadata { get; init; } = new();

    public ScenarioFeaturesDto? Features { get; init; }

    public IReadOnlyList<ScenarioSideDto> Sides { get; init; } = Array.Empty<ScenarioSideDto>();

    public ScenarioOrbatDto? Orbat { get; init; }

    public IReadOnlyList<ScenarioReferencePointDto> ReferencePoints { get; init; } = Array.Empty<ScenarioReferencePointDto>();

    public IReadOnlyList<ScenarioMissionDto> Missions { get; init; } = Array.Empty<ScenarioMissionDto>();

    public IReadOnlyList<ScenarioOperationTimelineEntryDto> OperationsTimeline { get; init; } = Array.Empty<ScenarioOperationTimelineEntryDto>();

    public IReadOnlyList<ScenarioEventDto> Events { get; init; } = Array.Empty<ScenarioEventDto>();

    public Dictionary<string, string>? Variables { get; init; }

    /// <summary>Derived-only UI state; never read by sim or validation (AC-9).</summary>
    public ScenarioEditorStateDto? EditorState { get; init; }
}

public sealed class ScenarioMissionDto
{
    public string Id { get; init; } = "";

    public string Type { get; init; } = "";

    public IReadOnlyList<string> AssignedUnitIds { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> TargetIds { get; init; } = Array.Empty<string>();

    public string? FerryDestinationBaseId { get; init; }

    public string? SupportRole { get; init; }

    public IReadOnlyList<ScenarioWaypointDto> PatrolZone { get; init; } = Array.Empty<ScenarioWaypointDto>();

    public IReadOnlyList<ScenarioWaypointDto>? StationGeometry { get; init; }

    public string? RoeOverride { get; init; }

    public string? EmconOverride { get; init; }
}

public sealed class ScenarioWaypointDto
{
    public double Lat { get; init; }

    public double Lon { get; init; }
}