namespace ProjectAegis.Data.Scenario.Authoring;

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

public sealed class ScenarioMetadataDto
{
    public string? Title { get; init; }

    public string? Description { get; init; }

    public string? Author { get; init; }

    /// <summary>
    /// Canonical schema version. Integer manifests stay numeric JSON (<c>1</c>);
    /// dotted labels such as <c>1.0.0</c> stay strings.
    /// </summary>
    [JsonConverter(typeof(FlexibleSchemaVersionConverter))]
    public string SchemaVersion { get; init; } = "1";

    /// <summary>
    /// Integer view of <see cref="SchemaVersion"/> for package manifests.
    /// Non-integer labels such as <c>1.0.0</c> report as 1.
    /// </summary>
    [JsonIgnore]
    public int SchemaVersionNumber =>
        int.TryParse(SchemaVersion, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number)
            ? number
            : 1;

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

/// <summary>Reads schemaVersion as either a JSON number or a string.</summary>
public sealed class FlexibleSchemaVersionConverter : JsonConverter<string>
{
    /// <inheritdoc />
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return reader.GetString() ?? "1";
        }

        if (reader.TokenType == JsonTokenType.Number)
        {
            return reader.TryGetInt64(out var number)
                ? number.ToString(CultureInfo.InvariantCulture)
                : reader.GetDouble().ToString(CultureInfo.InvariantCulture);
        }

        if (reader.TokenType == JsonTokenType.Null)
        {
            return "1";
        }

        throw new JsonException("schemaVersion must be a string or number.");
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
        {
            writer.WriteNumberValue(number);
        }
        else
        {
            writer.WriteStringValue(string.IsNullOrWhiteSpace(value) ? "1" : value);
        }
    }
}