using System.Globalization;
using System.Text.Json;

namespace ProjectAegis.Delegation.Demo;

/// <summary>
/// Loads an experiment definition from JSON. Validation matches
/// <c>data/experiments/experiment-definition.schema.json</c> without taking a schema-library dependency.
/// </summary>
public static class ExperimentDefinitionLoader
{
    private static readonly HashSet<string> RootProperties = new(StringComparer.Ordinal)
    {
        "schemaVersion",
        "scenarioId",
        "ticks",
        "seeds",
        "side",
        "mvpEngagement",
        "doctrineId",
        "roeId",
        "forceRatioId",
        "grid",
        "sweeps",
    };

    private static readonly HashSet<string> GridProperties = new(StringComparer.Ordinal)
    {
        "method",
        "parameters",
    };

    private static readonly HashSet<string> SweepProperties = new(StringComparer.Ordinal)
    {
        "doctrineIds",
        "roeIds",
        "forceRatioIds",
    };

    /// <summary>Reads and parses an experiment definition file.</summary>
    public static ExperimentDefinition LoadFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        string json;
        try
        {
            json = File.ReadAllText(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new ExperimentDefinitionException($"Could not read experiment definition '{path}': {ex.Message}", ex);
        }

        return Parse(json);
    }

    /// <summary>Parses an experiment definition document.</summary>
    public static ExperimentDefinition Parse(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            throw new ExperimentDefinitionException("Experiment definition is not valid JSON: " + ex.Message, ex);
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                throw new ExperimentDefinitionException("Experiment definition must be a JSON object.");
            }

            RejectUnknown(root, RootProperties, "");
            var schemaVersion = ReadSchemaVersion(root);
            var scenarioId = ReadScenarioId(root);
            var ticks = ReadTicks(root);
            var seeds = ReadSeeds(root);
            var side = ReadSide(root);
            var mvpEngagement = ReadBool(root, "mvpEngagement", defaultValue: true);
            var doctrineId = ReadOptionalString(root, "doctrineId");
            var roeId = ReadOptionalString(root, "roeId");
            var forceRatioId = ReadOptionalString(root, "forceRatioId");
            var (method, parameters) = ReadGrid(root);
            var (doctrineIds, roeIds, forceRatioIds) = ReadSweeps(root);
            return new ExperimentDefinition(
                scenarioId,
                ticks,
                seeds,
                side,
                mvpEngagement,
                doctrineId,
                roeId,
                forceRatioId,
                method,
                parameters,
                doctrineIds,
                roeIds,
                forceRatioIds,
                schemaVersion);
        }
    }

    private static int ReadSchemaVersion(JsonElement root)
    {
        if (!root.TryGetProperty("schemaVersion", out var value))
        {
            return 1;
        }

        if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out var version) || version != 1)
        {
            throw new ExperimentDefinitionException("schemaVersion must be 1.");
        }

        return version;
    }

    private static string ReadScenarioId(JsonElement root)
    {
        var value = Require(root, "scenarioId");
        if (value.ValueKind != JsonValueKind.String)
        {
            throw new ExperimentDefinitionException("scenarioId must be a string.");
        }

        var scenarioId = value.GetString() ?? "";
        if (scenarioId.Length == 0 || !string.Equals(scenarioId, scenarioId.Trim(), StringComparison.Ordinal))
        {
            throw new ExperimentDefinitionException("scenarioId must be a non-empty string without surrounding whitespace.");
        }

        return scenarioId;
    }

    private static int ReadTicks(JsonElement root)
    {
        var value = Require(root, "ticks");
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out var ticks) || ticks < 1)
        {
            throw new ExperimentDefinitionException("ticks must be an integer >= 1.");
        }

        return ticks;
    }

    private static IReadOnlyList<int> ReadSeeds(JsonElement root)
    {
        var value = Require(root, "seeds");
        if (value.ValueKind != JsonValueKind.Array)
        {
            throw new ExperimentDefinitionException("seeds must be an array of integers.");
        }

        var seeds = new List<int>();
        var seen = new HashSet<int>();
        foreach (var item in value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Number || !item.TryGetInt32(out var seed))
            {
                throw new ExperimentDefinitionException("seeds must be an array of integers.");
            }

            if (!seen.Add(seed))
            {
                throw new ExperimentDefinitionException(
                    "Duplicate seed " + seed.ToString(CultureInfo.InvariantCulture) + ".");
            }

            seeds.Add(seed);
        }

        if (seeds.Count == 0)
        {
            throw new ExperimentDefinitionException("seeds must contain at least one seed.");
        }

        return seeds;
    }

    private static string ReadSide(JsonElement root)
    {
        var side = ReadOptionalString(root, "side") ?? "BLUE";
        if (string.IsNullOrWhiteSpace(side))
        {
            throw new ExperimentDefinitionException("side must be a non-empty string.");
        }

        return side;
    }

    private static bool ReadBool(JsonElement root, string name, bool defaultValue)
    {
        if (!root.TryGetProperty(name, out var value))
        {
            return defaultValue;
        }

        if (value.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
        {
            throw new ExperimentDefinitionException(name + " must be a boolean.");
        }

        return value.GetBoolean();
    }

    private static string? ReadOptionalString(JsonElement obj, string name)
    {
        if (!obj.TryGetProperty(name, out var value))
        {
            return null;
        }

        if (value.ValueKind != JsonValueKind.String)
        {
            throw new ExperimentDefinitionException(name + " must be a string.");
        }

        return value.GetString();
    }

    private static (ExperimentGridMethod Method, IReadOnlyList<ExperimentParameter> Parameters) ReadGrid(JsonElement root)
    {
        if (!root.TryGetProperty("grid", out var grid))
        {
            return (ExperimentGridMethod.Cartesian, ExperimentSeedGrid.AllParameters);
        }

        if (grid.ValueKind != JsonValueKind.Object)
        {
            throw new ExperimentDefinitionException("grid must be an object.");
        }

        RejectUnknown(grid, GridProperties, "grid.");
        if (!grid.TryGetProperty("method", out var methodElement) || methodElement.ValueKind != JsonValueKind.String)
        {
            throw new ExperimentDefinitionException("grid.method is required.");
        }

        var method = methodElement.GetString() switch
        {
            "cartesian" => ExperimentGridMethod.Cartesian,
            "latin-hypercube" => ExperimentGridMethod.LatinHypercube,
            _ => throw new ExperimentDefinitionException("grid.method must be 'cartesian' or 'latin-hypercube'."),
        };

        if (!grid.TryGetProperty("parameters", out var parametersElement))
        {
            return (method, ExperimentSeedGrid.AllParameters);
        }

        return (method, ReadParameters(parametersElement));
    }

    private static IReadOnlyList<ExperimentParameter> ReadParameters(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Array)
        {
            throw new ExperimentDefinitionException("grid.parameters must be an array of parameter names.");
        }

        var parameters = new List<ExperimentParameter>();
        var seen = new HashSet<ExperimentParameter>();
        foreach (var item in value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String)
            {
                throw new ExperimentDefinitionException("grid.parameters must be an array of parameter names.");
            }

            var parameter = item.GetString() switch
            {
                "seed" => ExperimentParameter.Seed,
                "doctrineId" => ExperimentParameter.DoctrineId,
                "roeId" => ExperimentParameter.RoeId,
                "forceRatioId" => ExperimentParameter.ForceRatioId,
                _ => throw new ExperimentDefinitionException(
                    "Unknown grid parameter '" + item.GetString() + "'."),
            };
            if (!seen.Add(parameter))
            {
                throw new ExperimentDefinitionException("grid.parameters contains a duplicate entry.");
            }

            parameters.Add(parameter);
        }

        if (parameters.Count == 0)
        {
            throw new ExperimentDefinitionException("grid.parameters must contain at least one parameter.");
        }

        return parameters;
    }

    private static (IReadOnlyList<string> DoctrineIds, IReadOnlyList<string> RoeIds, IReadOnlyList<string> ForceRatioIds) ReadSweeps(
        JsonElement root)
    {
        if (!root.TryGetProperty("sweeps", out var sweeps))
        {
            return ([], [], []);
        }

        if (sweeps.ValueKind != JsonValueKind.Object)
        {
            throw new ExperimentDefinitionException("sweeps must be an object.");
        }

        RejectUnknown(sweeps, SweepProperties, "sweeps.");
        return (
            ReadStringList(sweeps, "doctrineIds"),
            ReadStringList(sweeps, "roeIds"),
            ReadStringList(sweeps, "forceRatioIds"));
    }

    private static IReadOnlyList<string> ReadStringList(JsonElement obj, string name)
    {
        if (!obj.TryGetProperty(name, out var value))
        {
            return [];
        }

        if (value.ValueKind != JsonValueKind.Array)
        {
            throw new ExperimentDefinitionException(name + " must be an array of strings.");
        }

        var items = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String)
            {
                throw new ExperimentDefinitionException(name + " must be an array of strings.");
            }

            var text = item.GetString() ?? "";
            if (text.Length == 0)
            {
                throw new ExperimentDefinitionException(name + " entries must be non-empty strings.");
            }

            if (!seen.Add(text))
            {
                throw new ExperimentDefinitionException(name + " contains a duplicate entry.");
            }

            items.Add(text);
        }

        return items;
    }

    private static JsonElement Require(JsonElement obj, string name)
    {
        if (!obj.TryGetProperty(name, out var value))
        {
            throw new ExperimentDefinitionException("Missing required property '" + name + "'.");
        }

        return value;
    }

    private static void RejectUnknown(JsonElement obj, HashSet<string> allowed, string prefix)
    {
        foreach (var property in obj.EnumerateObject())
        {
            if (!allowed.Contains(property.Name))
            {
                throw new ExperimentDefinitionException("Unexpected property '" + prefix + property.Name + "'.");
            }
        }
    }
}
