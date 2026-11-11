namespace ProjectAegis.Data.Scenario.Authoring;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>Non-authoritative editor UI state (GDD §3.2 / AC-9 derived-only).</summary>
public sealed class ScenarioEditorStateDto
{
    public double CameraLat { get; init; } = 57.0;

    public double CameraLon { get; init; } = 20.0;

    public double CameraZoom { get; init; } = 1.0;

    public bool LayersVisible { get; init; } = true;

    /// <summary>Per-layer visibility from the canonical scenario schema. Null when unset.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, bool>? LayerVisibility { get; init; }

    /// <summary>
    /// Free-form editorState object (camera, layers, unitZonePresence).
    /// When this bag is non-empty, canonical JSON writes these keys and skips the structured camera fields.
    /// </summary>
    [JsonIgnore]
    public Dictionary<string, JsonElement> Entries { get; init; } = new(StringComparer.Ordinal);

    /// <summary>Returns whether the free-form bag contains <paramref name="key"/>.</summary>
    public bool ContainsKey(string key) => Entries.ContainsKey(key);

    /// <summary>Reads a free-form editorState value by key.</summary>
    public bool TryGetValue(string key, out JsonElement value) => Entries.TryGetValue(key, out value);

    /// <summary>Writes either the free-form bag or the structured camera fields.</summary>
    public void WriteCanonical(Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        if (Entries.Count > 0)
        {
            foreach (var key in Entries.Keys.OrderBy(k => k, StringComparer.Ordinal))
            {
                writer.WritePropertyName(key);
                Entries[key].WriteTo(writer);
            }
        }
        else
        {
            writer.WriteNumber("cameraLat", CameraLat);
            writer.WriteNumber("cameraLon", CameraLon);
            writer.WriteNumber("cameraZoom", CameraZoom);
            writer.WriteBoolean("layersVisible", LayersVisible);
            if (LayerVisibility is { Count: > 0 })
            {
                writer.WritePropertyName("layerVisibility");
                writer.WriteStartObject();
                foreach (var key in LayerVisibility.Keys.OrderBy(k => k, StringComparer.Ordinal))
                {
                    writer.WriteBoolean(key, LayerVisibility[key]);
                }

                writer.WriteEndObject();
            }
        }

        writer.WriteEndObject();
    }

    /// <summary>Adapts the host's dictionary editorState onto the canonical DTO.</summary>
    public static implicit operator ScenarioEditorStateDto(Dictionary<string, JsonElement> entries) =>
        new()
        {
            Entries = entries,
        };
}

/// <summary>Non-sim authoring sections of canonical scenario.json (GDD §3.2).</summary>
public sealed class ScenarioFeaturesDto
{
    public bool RealismMagazines { get; init; } = true;

    public double MaxTimeCompression { get; init; } = 256;
}

public sealed class ScenarioSideDto
{
    public string Id { get; init; } = "";

    public string Name { get; init; } = "";

    public string? DefaultRoe { get; init; }

    public string? DefaultEmcon { get; init; }

    /// <summary>Side briefing text from the canonical scenario schema. Null when unset.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Briefing { get; init; }

    /// <summary>Default doctrine id from the canonical scenario schema. Null when unset.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DoctrineDefaultId { get; init; }

    /// <summary>Side score. Zero is omitted from JSON.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int Score { get; init; }

    private readonly IReadOnlyList<string> _postures = Array.Empty<string>();

    /// <summary>
    /// Side postures. Explicit null from JSON or object initialization is stored as an empty list.
    /// </summary>
    public IReadOnlyList<string> Postures
    {
        get => _postures;
        init => _postures = ScenarioNestedCollections.EmptyIfNull<string>(value);
    }
}

public sealed class ScenarioOrbatDto
{
    public IReadOnlyList<ScenarioOrbatUnitDto> Units { get; init; } = Array.Empty<ScenarioOrbatUnitDto>();

    /// <summary>ORBAT groups. Null when the document does not carry a groups section.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<ScenarioOrbatGroupDto>? Groups { get; init; }

    public IReadOnlyList<ScenarioOrbatBaseDto> Bases { get; init; } = Array.Empty<ScenarioOrbatBaseDto>();

    /// <summary>ORBAT cargo rows. Null when the document does not carry cargo.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<ScenarioOrbatCargoDto>? Cargo { get; init; }
}

public sealed class ScenarioOrbatUnitDto
{
    public string Id { get; init; } = "";

    public string SideId { get; init; } = "";

    public string PlatformId { get; init; } = "";

    /// <summary>Catalog platform id from the canonical scenario schema. Null when unset.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PlatformDbId { get; init; }

    public double Lat { get; init; }

    public double Lon { get; init; }

    /// <summary>Home base id. Null when unset.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? BaseId { get; init; }

    /// <summary>ORBAT group id. Null when unset.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? GroupId { get; init; }

    public string? ParentUnitId { get; init; }

    public string? RoeOverride { get; init; }

    public string? EmconOverride { get; init; }

    /// <summary>
    /// SWARM-02 optional initial integrity when <see cref="PlatformId"/> is a swarm catalog entry.
    /// Null means default to catalog <c>maxDrones</c> at spawn (Data factory).
    /// </summary>
    public int? DroneCount { get; init; }

    /// <summary>
    /// SWARM-22 / B9: optional host/mothership unit id for swarm placement.
    /// </summary>
    public string? HostUnitId { get; init; }

    /// <summary>
    /// SWARM-20 / C5: optional mission type for swarm tasking
    /// (<c>Patrol</c>, <c>Support</c>, <c>Strike</c>). Null when unset.
    /// </summary>
    public string? MissionType { get; init; }

    /// <summary>
    /// SWARM-20 / C5: optional Phase B operational mode
    /// (Hold, Assault, Screen, Scatter, Rejoin). When null and
    /// <see cref="MissionType"/> is set, authoring applies the mission default mode.
    /// </summary>
    public string? Mode { get; init; }
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

public sealed class ScenarioOperationTimelineEntryDto
{
    /// <summary>Optional entry id from the canonical scenario schema. Null when unset.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Id { get; init; }

    public string MissionId { get; init; } = "";

    public int ActivateAtTick { get; init; }

    /// <summary>Sim tick alias from the canonical scenario schema. Zero is omitted.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int SimTick { get; init; }

    /// <summary>Timeline action from the canonical scenario schema. Null when unset.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Action { get; init; }
}
