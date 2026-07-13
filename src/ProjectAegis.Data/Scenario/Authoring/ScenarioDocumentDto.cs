namespace ProjectAegis.Data.Scenario.Authoring;

using System.Text.Json;

/// <summary>Minimal canonical scenario shape for validation engine v1 (ADR-008).</summary>
public sealed class ScenarioDocumentDto
{
    public ScenarioMetadataDto Metadata { get; init; } = new();

    public ScenarioFeaturesDto? Features { get; init; }

    public IReadOnlyList<ScenarioSideDto> Sides { get; init; } = Array.Empty<ScenarioSideDto>();

    public ScenarioOrbatDto? Orbat { get; init; }

    public IReadOnlyList<ScenarioReferencePointDto> ReferencePoints { get; init; } = Array.Empty<ScenarioReferencePointDto>();

    public IReadOnlyList<ScenarioMissionDto> Missions { get; init; } = Array.Empty<ScenarioMissionDto>();

    public IReadOnlyList<ScenarioOperationTimelineEntryDto> OperationsTimeline { get; init; } = Array.Empty<ScenarioOperationTimelineEntryDto>();

    /// <summary>Optional typed event DSL entries (AME-5.x). Null when unset.</summary>
    public IReadOnlyList<ScenarioEventDto>? Events { get; init; }

    public Dictionary<string, string>? Variables { get; init; }

    /// <summary>Derived-only editor UI state; never read by <see cref="Validation.ScenarioValidationEngine"/>.</summary>
    public Dictionary<string, JsonElement>? EditorState { get; init; }
}

/// <summary>Typed scenario event (trigger + conditions + actions).</summary>
public sealed class ScenarioEventDto
{
    public string Id { get; init; } = "";

    public string TriggerType { get; init; } = "";

    public IReadOnlyList<ScenarioEventConditionDto> Conditions { get; init; } = Array.Empty<ScenarioEventConditionDto>();

    public IReadOnlyList<ScenarioEventActionDto> Actions { get; init; } = Array.Empty<ScenarioEventActionDto>();
}

public sealed class ScenarioEventConditionDto
{
    public string Type { get; init; } = "";

    public string? UnitId { get; init; }

    public string? ZoneId { get; init; }

    public bool? Result { get; init; }
}

public sealed class ScenarioEventActionDto
{
    public string Type { get; init; } = "";

    public string? UnitId { get; init; }

    public double? Lat { get; init; }

    public double? Lon { get; init; }
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

    /// <summary>Mission-level ROE override (AME-3.2). Null inherits sideRoe or WeaponsFree.</summary>
    public string? RoeOverride { get; init; }

    /// <summary>Support mission role (Tanker/AEW/EW). Null for non-Support missions.</summary>
    public string? SupportRole { get; init; }

    public string? EmconOverride { get; init; }
}

public sealed class ScenarioWaypointDto
{
    public double Lat { get; init; }

    public double Lon { get; init; }
}
