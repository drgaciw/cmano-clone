using System.Text.Json;
using Json.Schema;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Demo.Tests;

public sealed class ExperimentDefinitionSchemaTests
{
    private const string ValidSeedGrid = """
        {
          "schemaVersion": 1,
          "scenarioId": "baltic-patrol",
          "ticks": 4,
          "seeds": [42, 7],
          "side": "BLUE",
          "mvpEngagement": true,
          "doctrineId": "doctrine-hold",
          "roeId": "weapons-tight",
          "forceRatioId": "1-to-1",
          "grid": {
            "method": "cartesian",
            "parameters": ["seed", "doctrineId", "roeId"]
          },
          "sweeps": {
            "doctrineIds": ["doctrine-hold", "doctrine-press"],
            "roeIds": ["weapons-tight"]
          }
        }
        """;

    private static readonly EvaluationOptions EvaluationOptions = new()
    {
        OutputFormat = OutputFormat.List,
    };

    private static readonly Lazy<JsonSchema> CachedSchema = new(() =>
    {
        var schemaPath = ResolveSchemaPath();
        return JsonSchema.FromText(File.ReadAllText(schemaPath));
    });

    [Test]
    public void Seed_grid_document_conforms_and_loads()
    {
        var results = CachedSchema.Value.Evaluate(JsonDocument.Parse(ValidSeedGrid).RootElement, EvaluationOptions);
        Assert.That(results.IsValid, Is.True, Describe(results));

        var definition = ExperimentDefinitionLoader.Parse(ValidSeedGrid);
        Assert.That(definition.ScenarioId, Is.EqualTo("baltic-patrol"));
        Assert.That(definition.Ticks, Is.EqualTo(4));
        Assert.That(definition.Seeds, Is.EqualTo(new[] { 42, 7 }));
        Assert.That(definition.SchemaVersion, Is.EqualTo(1));
        Assert.That(definition.GridMethod, Is.EqualTo(ExperimentGridMethod.Cartesian));
        Assert.That(definition.DoctrineIds, Is.EqualTo(new[] { "doctrine-hold", "doctrine-press" }));
    }

    [Test]
    public void Latin_hypercube_document_conforms()
    {
        const string json = """
            {
              "scenarioId": "baltic-patrol",
              "ticks": 1,
              "seeds": [1],
              "grid": { "method": "latin-hypercube" }
            }
            """;

        var results = CachedSchema.Value.Evaluate(JsonDocument.Parse(json).RootElement, EvaluationOptions);
        Assert.That(results.IsValid, Is.True, Describe(results));
        Assert.That(ExperimentDefinitionLoader.Parse(json).GridMethod, Is.EqualTo(ExperimentGridMethod.LatinHypercube));
    }

    [Test]
    public void Missing_seeds_fails_schema_and_loader()
    {
        const string json = """
            { "scenarioId": "baltic-patrol", "ticks": 4 }
            """;

        var results = CachedSchema.Value.Evaluate(JsonDocument.Parse(json).RootElement, EvaluationOptions);
        Assert.That(results.IsValid, Is.False);
        Assert.Throws<ExperimentDefinitionException>(() => ExperimentDefinitionLoader.Parse(json));
    }

    [Test]
    public void Unknown_property_fails_schema_and_loader()
    {
        const string json = """
            { "scenarioId": "baltic-patrol", "ticks": 4, "seeds": [1], "monteCarlo": true }
            """;

        var results = CachedSchema.Value.Evaluate(JsonDocument.Parse(json).RootElement, EvaluationOptions);
        Assert.That(results.IsValid, Is.False);
        var ex = Assert.Throws<ExperimentDefinitionException>(() => ExperimentDefinitionLoader.Parse(json));
        Assert.That(ex!.Message, Does.Contain("monteCarlo"));
    }

    [Test]
    public void Tick_count_below_one_fails_schema_and_loader()
    {
        const string json = """
            { "scenarioId": "baltic-patrol", "ticks": 0, "seeds": [1] }
            """;

        var results = CachedSchema.Value.Evaluate(JsonDocument.Parse(json).RootElement, EvaluationOptions);
        Assert.That(results.IsValid, Is.False);
        Assert.Throws<ExperimentDefinitionException>(() => ExperimentDefinitionLoader.Parse(json));
    }

    private static string Describe(EvaluationResults results)
    {
        var details = (results.Details ?? [])
            .Where(d => !d.IsValid && d.Errors is { Count: > 0 })
            .SelectMany(d => d.Errors!.Select(e => $"{d.InstanceLocation}: {e.Key} {e.Value}"));
        return string.Join(Environment.NewLine, details);
    }

    private static string ResolveSchemaPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 12; i++)
        {
            var candidate = Path.Combine(dir.FullName, "data", "experiments", "experiment-definition.schema.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent ?? throw new FileNotFoundException("experiment-definition.schema.json was not found.");
        }

        throw new FileNotFoundException("experiment-definition.schema.json was not found.");
    }
}
