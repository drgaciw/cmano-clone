namespace ProjectAegis.Data.Scenario.Authoring;

/// <summary>Non-authoritative editor UI state (GDD §3.2 / AC-9 derived-only).</summary>
public sealed class ScenarioEditorStateDto
{
    public double CameraLat { get; init; } = 57.0;

    public double CameraLon { get; init; } = 20.0;

    public double CameraZoom { get; init; } = 1.0;

    public bool LayersVisible { get; init; } = true;
}

public sealed class ScenarioFeaturesDto
{
    public bool RealismMagazines { get; init; } = true;

    public int MaxTimeCompression { get; init; } = 256;
}

public sealed class ScenarioSideDto
{
    public string Id { get; init; } = "";

    public string Name { get; init; } = "";

    public string? DefaultRoe { get; init; }

    public string? DefaultEmcon { get; init; }

    public IReadOnlyList<string> Postures { get; init; } = Array.Empty<string>();
}

public sealed class ScenarioOrbatDto
{
    public IReadOnlyList<ScenarioOrbatUnitDto> Units { get; init; } = Array.Empty<ScenarioOrbatUnitDto>();

    public IReadOnlyList<ScenarioOrbatBaseDto> Bases { get; init; } = Array.Empty<ScenarioOrbatBaseDto>();
}

public sealed class ScenarioOrbatUnitDto
{
    public string Id { get; init; } = "";

    public string SideId { get; init; } = "";

    public string PlatformId { get; init; } = "";

    public double Lat { get; init; }

    public double Lon { get; init; }

    public string? ParentUnitId { get; init; }

    public string? RoeOverride { get; init; }

    public string? EmconOverride { get; init; }
}

public sealed class ScenarioOrbatBaseDto
{
    public string Id { get; init; } = "";

    public string SideId { get; init; } = "";

    public double Lat { get; init; }

    public double Lon { get; init; }
}

public sealed class ScenarioReferencePointDto
{
    public string Id { get; init; } = "";

    public string Type { get; init; } = "point";

    public IReadOnlyList<ScenarioWaypointDto> Geometry { get; init; } = Array.Empty<ScenarioWaypointDto>();

    public double? RadiusNm { get; init; }
}

public sealed class ScenarioOperationTimelineEntryDto
{
    public string MissionId { get; init; } = "";

    public int ActivateAtTick { get; init; }
}

public sealed class ScenarioEventDto
{
    public string Id { get; init; } = "";

    public int Priority { get; init; } = 100;

    public ScenarioEventTriggerDto Trigger { get; init; } = new();

    public IReadOnlyList<ScenarioEventConditionDto> Conditions { get; init; } = Array.Empty<ScenarioEventConditionDto>();

    public IReadOnlyList<ScenarioEventActionDto> Actions { get; init; } = Array.Empty<ScenarioEventActionDto>();
}

public sealed class ScenarioEventTriggerDto
{
    public string Type { get; init; } = "Time";

    public int? AtTick { get; init; }
}

public sealed class ScenarioEventConditionDto
{
    public string Type { get; init; } = "";

    public string? UnitId { get; init; }

    public string? ZoneId { get; init; }
}

public sealed class ScenarioEventActionDto
{
    public string Type { get; init; } = "";

    public string? MissionId { get; init; }

    public string? UnitId { get; init; }

    public double? Lat { get; init; }

    public double? Lon { get; init; }
}
