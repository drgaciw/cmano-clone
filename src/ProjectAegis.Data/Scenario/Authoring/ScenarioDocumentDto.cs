namespace ProjectAegis.Data.Scenario.Authoring;

using System.Text.Json;

/// <summary>Canonical scenario shape for validation engine v1+ (ADR-008, GDD §3.2).</summary>
public sealed class ScenarioDocumentDto
{
    public ScenarioMetadataDto Metadata { get; init; } = new();

    public ScenarioFeaturesDto? Features { get; init; }

    public IReadOnlyList<ScenarioSideDto> Sides { get; init; } = Array.Empty<ScenarioSideDto>();

    public ScenarioOrbatDto? Orbat { get; init; }

    public IReadOnlyList<ScenarioReferencePointDto> ReferencePoints { get; init; } =
        Array.Empty<ScenarioReferencePointDto>();

    public IReadOnlyList<ScenarioMissionDto> Missions { get; init; } = Array.Empty<ScenarioMissionDto>();

    public IReadOnlyList<ScenarioOperationsTimelineEntryDto> OperationsTimeline { get; init; } =
        Array.Empty<ScenarioOperationsTimelineEntryDto>();

    public IReadOnlyList<ScenarioEventDto> Events { get; init; } = Array.Empty<ScenarioEventDto>();

    public IReadOnlyDictionary<string, JsonElement>? Variables { get; init; }

    /// <summary>Derived-only; preserved on round-trip but not sim/validation input.</summary>
    public ScenarioEditorStateDto? EditorState { get; init; }
}

public sealed class ScenarioOperationsTimelineEntryDto
{
    public string Id { get; init; } = "";

    public int SimTick { get; init; }

    public string MissionId { get; init; } = "";

    public string Action { get; init; } = "";
}

public sealed class ScenarioEventDto
{
    public string Id { get; init; } = "";

    public string TriggerType { get; init; } = "";

    public int Priority { get; init; } = 100;

    public IReadOnlyList<ScenarioEventConditionDto> Conditions { get; init; } =
        Array.Empty<ScenarioEventConditionDto>();

    public IReadOnlyList<ScenarioEventActionDto> Actions { get; init; } =
        Array.Empty<ScenarioEventActionDto>();
}

public sealed class ScenarioEventConditionDto
{
    public string Type { get; init; } = "";

    public string? TargetId { get; init; }
}

public sealed class ScenarioEventActionDto
{
    public string Type { get; init; } = "";

    public string? TargetId { get; init; }
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
