namespace ProjectAegis.Data.Scenario.Authoring;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>Canonical scenario shape for validation engine v1+ (ADR-008, GDD §3.2).</summary>
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

    public Dictionary<string, JsonElement>? Variables { get; init; }

    /// <summary>Derived-only editor UI state; never read by <see cref="Validation.ScenarioValidationEngine"/>.</summary>
    [JsonConverter(typeof(ScenarioEditorStateJsonConverter))]
    public ScenarioEditorStateDto? EditorState { get; init; }
}

/// <summary>Typed scenario event (trigger + conditions + actions).</summary>
public sealed class ScenarioEventDto
{
    public string Id { get; init; } = "";

    public string TriggerType { get; init; } = "";

    /// <summary>Event priority. Null means the canonical default of 100 and is omitted from JSON.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Priority { get; init; }

    public IReadOnlyList<ScenarioEventConditionDto> Conditions { get; init; } = Array.Empty<ScenarioEventConditionDto>();

    public IReadOnlyList<ScenarioEventActionDto> Actions { get; init; } = Array.Empty<ScenarioEventActionDto>();
}

public sealed class ScenarioEventConditionDto
{
    public string Type { get; init; } = "";

    /// <summary>Canonical condition target id. Null when the condition uses <see cref="UnitId"/> or <see cref="ZoneId"/>.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TargetId { get; init; }

    public string? UnitId { get; init; }

    public string? ZoneId { get; init; }

    public bool? Result { get; init; }
}

public sealed class ScenarioEventActionDto
{
    public string Type { get; init; } = "";

    /// <summary>Canonical action target id. Null when the action uses <see cref="UnitId"/>.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TargetId { get; init; }

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

/// <summary>
/// Serializes editorState as either a free-form object or structured camera fields.
/// </summary>
public sealed class ScenarioEditorStateJsonConverter : JsonConverter<ScenarioEditorStateDto?>
{
    /// <inheritdoc />
    public override ScenarioEditorStateDto? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("editorState must be a JSON object.");
        }

        using var document = JsonDocument.ParseValue(ref reader);
        var entries = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        var cameraLat = 57.0;
        var cameraLon = 20.0;
        var cameraZoom = 1.0;
        var layersVisible = true;
        IReadOnlyDictionary<string, bool>? layerVisibility = null;
        foreach (var property in document.RootElement.EnumerateObject())
        {
            if (property.NameEquals("cameraLat"))
            {
                cameraLat = property.Value.GetDouble();
            }
            else if (property.NameEquals("cameraLon"))
            {
                cameraLon = property.Value.GetDouble();
            }
            else if (property.NameEquals("cameraZoom"))
            {
                cameraZoom = property.Value.GetDouble();
            }
            else if (property.NameEquals("layersVisible"))
            {
                layersVisible = property.Value.GetBoolean();
            }
            else if (property.NameEquals("layerVisibility"))
            {
                layerVisibility = JsonSerializer.Deserialize<Dictionary<string, bool>>(property.Value.GetRawText(), options);
            }
            else
            {
                entries[property.Name] = property.Value.Clone();
            }
        }

        return new ScenarioEditorStateDto
        {
            CameraLat = cameraLat,
            CameraLon = cameraLon,
            CameraZoom = cameraZoom,
            LayersVisible = layersVisible,
            LayerVisibility = layerVisibility,
            Entries = entries,
        };
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, ScenarioEditorStateDto? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        value.WriteCanonical(writer);
    }
}
