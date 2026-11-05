namespace ProjectAegis.Data.Scenario.Authoring;

using System.Text.Json;

/// <summary>Non-sim authoring sections of canonical scenario.json (GDD §3.2).</summary>
public sealed class ScenarioFeaturesDto
{
    public bool RealismMagazines { get; init; } = true;

    public double MaxTimeCompression { get; init; } = 1.0;
}

public sealed class ScenarioSideDto
{
    public string Id { get; init; } = "";

    public string Name { get; init; } = "";

    public string? Briefing { get; init; }

    public string? DoctrineDefaultId { get; init; }

    public int Score { get; init; }

    public IReadOnlyList<string> Postures { get; init; } = Array.Empty<string>();
}

public sealed class ScenarioOrbatDto
{
    public IReadOnlyList<ScenarioOrbatUnitDto> Units { get; init; } = Array.Empty<ScenarioOrbatUnitDto>();

    public IReadOnlyList<ScenarioOrbatGroupDto> Groups { get; init; } = Array.Empty<ScenarioOrbatGroupDto>();

    public IReadOnlyList<ScenarioOrbatBaseDto> Bases { get; init; } = Array.Empty<ScenarioOrbatBaseDto>();

    public IReadOnlyList<ScenarioOrbatCargoDto> Cargo { get; init; } = Array.Empty<ScenarioOrbatCargoDto>();
}

public sealed class ScenarioOrbatUnitDto
{
    public string Id { get; init; } = "";

    public string SideId { get; init; } = "";

    public string? PlatformDbId { get; init; }

    public double Lat { get; init; }

    public double Lon { get; init; }

    public string? BaseId { get; init; }

    public string? GroupId { get; init; }
}

public sealed class ScenarioOrbatGroupDto
{
    public string Id { get; init; } = "";

    public string SideId { get; init; } = "";

    public IReadOnlyList<string> UnitIds { get; init; } = Array.Empty<string>();
}

public sealed class ScenarioOrbatBaseDto
{
    public string Id { get; init; } = "";

    public string SideId { get; init; } = "";

    public double Lat { get; init; }

    public double Lon { get; init; }
}

public sealed class ScenarioOrbatCargoDto
{
    public string Id { get; init; } = "";

    public string UnitId { get; init; } = "";

    public string CargoType { get; init; } = "";
}

public sealed class ScenarioReferencePointDto
{
    public string Id { get; init; } = "";

    public string Type { get; init; } = "point";

    public IReadOnlyList<ScenarioWaypointDto> Geometry { get; init; } = Array.Empty<ScenarioWaypointDto>();

    public double? RadiusNm { get; init; }
}

/// <summary>Derived-only UI state; never read by sim or validation (GDD §3.3, AC-9).</summary>
public sealed class ScenarioEditorStateDto
{
    public double CameraLat { get; init; }

    public double CameraLon { get; init; }

    public double CameraZoom { get; init; } = 1.0;

    public IReadOnlyDictionary<string, bool>? LayerVisibility { get; init; }
}
