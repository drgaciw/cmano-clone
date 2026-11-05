using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProjectAegis.Delegation.UnityAdapter.Experiments;

/// <summary>Strict parser and serializer for the seed-grid experiment JSON contract.</summary>
public static class ExperimentDefinitionJson
{
    /// <summary>Parses a definition, reporting malformed JSON and invalid fields as clear format errors.</summary>
    public static ExperimentDefinition Parse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new FormatException("Experiment JSON must contain an object.");
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                throw new FormatException("Experiment JSON root must be an object.");

            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in root.EnumerateObject())
            {
                if (!names.Add(property.Name))
                    throw new FormatException($"Duplicate experiment field: {property.Name}.");
                if (property.Name != "scenarioId" && property.Name != "seeds" && property.Name != "ticks"
                    && property.Name != "doctrineId" && property.Name != "roeId")
                    throw new FormatException($"Unknown experiment field: {property.Name}.");
            }

            var scenarioId = ReadString(root, "scenarioId", required: true)!;
            if (!root.TryGetProperty("seeds", out var seedsJson) || seedsJson.ValueKind != JsonValueKind.Array
                || seedsJson.GetArrayLength() == 0)
                throw new FormatException("seeds must be a nonempty array of signed 32-bit integers.");
            var seeds = new List<int>();
            foreach (var value in seedsJson.EnumerateArray())
            {
                if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out var seed))
                    throw new FormatException($"seeds[{seeds.Count}] must be a signed 32-bit integer.");
                seeds.Add(seed);
            }
            if (!root.TryGetProperty("ticks", out var ticksJson) || ticksJson.ValueKind != JsonValueKind.Number
                || !ticksJson.TryGetInt32(out var ticks) || ticks <= 0)
                throw new FormatException("ticks must be a positive 32-bit integer.");

            return new ExperimentDefinition(scenarioId, seeds, ticks,
                ReadString(root, "doctrineId", required: false), ReadString(root, "roeId", required: false));
        }
        catch (JsonException exception)
        {
            throw new FormatException($"Malformed experiment JSON: {exception.Message}", exception);
        }
    }

    /// <summary>Serializes a definition with stable field names; absent metadata is omitted.</summary>
    public static string Serialize(ExperimentDefinition definition)
    {
        if (definition == null) throw new ArgumentNullException(nameof(definition));
        return JsonSerializer.Serialize(definition, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true
        });
    }

    private static string? ReadString(JsonElement root, string name, bool required)
    {
        if (!root.TryGetProperty(name, out var value))
        {
            if (required) throw new FormatException($"Missing required field: {name}.");
            return null;
        }
        if (value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(value.GetString()))
            throw new FormatException($"{name} must be a nonblank string when supplied.");
        return value.GetString();
    }
}
