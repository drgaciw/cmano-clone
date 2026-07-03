namespace ProjectAegis.Data.Tests.Scenario;

using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Schema;
using ProjectAegis.Data.Scenario.Authoring;
using Xunit;

/// <summary>
/// AME-2.6 (qa-plan-scenario-editor-2026-07-01.md #15): automates, in CI, the JSON Schema
/// conformance check that was previously only run manually with a Python script against
/// data/scenarios/scenario-document.schema.json (draft 2020-12).
/// </summary>
public sealed class ScenarioDocumentSchemaConformanceTests
{
    private static readonly string[] FixtureFileNames =
    {
        "baltic-patrol.scenario.json",
        "strike-package.scenario.json",
        "ferry-redeploy.scenario.json",
    };

    public static IEnumerable<object[]> FixtureFiles() =>
        FixtureFileNames.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(FixtureFiles))]
    public void Fixture_document_conforms_to_schema(string fixtureFileName)
    {
        var schema = LoadSchema();
        var fixturePath = ResolveDataPath("examples", fixtureFileName);
        Assert.True(File.Exists(fixturePath), $"Fixture not found: {fixturePath}");

        var json = File.ReadAllText(fixturePath);
        var instance = JsonNode.Parse(json);

        var results = schema.Evaluate(ToElement(instance), EvaluationOptions);

        Assert.True(
            results.IsValid,
            $"Fixture '{fixtureFileName}' failed schema validation:\n{DescribeErrors(results)}");
    }

    [Fact]
    public void Editor_produced_document_conforms_to_schema()
    {
        // This is the important regression guard: it exercises the *live* DTO/writer
        // pipeline (ScenarioDocumentEditor -> ScenarioDocumentDto -> ScenarioDocumentJsonWriter)
        // against the schema, so future DTO changes that drift from the schema fail CI even
        // if nobody remembers to hand-author a new fixture.
        var editor = ScenarioDocumentEditor.CreateNew(dbRef: "baltic_patrol", seed: 42, policyId: "baltic-patrol-catalog");
        editor.AddPatrolMission(
            "patrol-schema-check",
            new[] { "u1" },
            new[]
            {
                new ScenarioWaypointDto { Lat = 57.0, Lon = 20.0 },
                new ScenarioWaypointDto { Lat = 57.1, Lon = 20.1 },
                new ScenarioWaypointDto { Lat = 57.2, Lon = 20.2 },
            });
        editor.AddStrikeMission(
            "strike-schema-check",
            new[] { "strike-flight-1" },
            new[] { "target-1" });
        editor.AddFerryMission(
            "ferry-schema-check",
            new[] { "transport-1" },
            "base-amari");

        var json = ScenarioDocumentJsonWriter.Serialize(editor.ToDto());
        var instance = JsonNode.Parse(json);

        var schema = LoadSchema();
        var results = schema.Evaluate(ToElement(instance), EvaluationOptions);

        Assert.True(
            results.IsValid,
            $"ScenarioDocumentEditor-produced document failed schema validation:\n{DescribeErrors(results)}\n\nJSON:\n{json}");
    }

    [Fact]
    public void Document_missing_required_patrolZone_on_patrol_mission_fails_schema_validation()
    {
        var schema = LoadSchema();
        var fixturePath = ResolveDataPath("examples", "baltic-patrol.scenario.json");
        var json = File.ReadAllText(fixturePath);
        var instance = JsonNode.Parse(json)!.AsObject();

        var missions = instance["missions"]!.AsArray();
        var patrolMission = missions[0]!.AsObject();
        Assert.Equal("Patrol", (string?)patrolMission["type"]);

        // Deliberately remove the required "patrolZone" property entirely — a Patrol mission
        // without it is not just invalid on content, it violates the mission $defs' "required" list.
        patrolMission.Remove("patrolZone");

        var results = schema.Evaluate(ToElement(instance), EvaluationOptions);

        Assert.False(
            results.IsValid,
            "Expected schema validation to fail for a Patrol mission with 'patrolZone' removed, " +
            "but it passed. This would mean the positive tests above are not actually exercising " +
            "the schema's required-field / minItems constraints.");
    }

    [Fact]
    public void Document_with_empty_targetIds_on_strike_mission_fails_schema_validation()
    {
        var schema = LoadSchema();
        var fixturePath = ResolveDataPath("examples", "strike-package.scenario.json");
        var json = File.ReadAllText(fixturePath);
        var instance = JsonNode.Parse(json)!.AsObject();

        var missions = instance["missions"]!.AsArray();
        var strikeMission = missions[0]!.AsObject();
        Assert.Equal("Strike", (string?)strikeMission["type"]);

        // Strike missions require targetIds to have at least 1 entry (schema allOf/if/then).
        strikeMission["targetIds"] = new JsonArray();

        var results = schema.Evaluate(ToElement(instance), EvaluationOptions);

        Assert.False(
            results.IsValid,
            "Expected schema validation to fail for a Strike mission with an empty 'targetIds' " +
            "array, but it passed. This would mean the minItems constraint for Strike missions " +
            "is not actually being enforced by this test suite.");
    }

    private static readonly EvaluationOptions EvaluationOptions = new()
    {
        OutputFormat = OutputFormat.List,
    };

    // JsonSchema.Net registers each parsed schema globally by its "$id"; parsing the same
    // schema document more than once (once per test method here) throws
    // "Overwriting registered schemas is not permitted." Cache and reuse a single
    // JsonSchema instance across all tests in this class instead.
    private static readonly Lazy<JsonSchema> CachedSchema = new(() =>
    {
        var schemaPath = ResolveDataPath(null, "scenario-document.schema.json");
        if (!File.Exists(schemaPath))
        {
            throw new FileNotFoundException($"Schema not found: {schemaPath}");
        }

        var schemaJson = File.ReadAllText(schemaPath);
        return JsonSchema.FromText(schemaJson);
    });

    private static JsonSchema LoadSchema() => CachedSchema.Value;

    /// <summary>JsonSchema.Net 9.x evaluates JsonElement instances, not JsonNode; JsonNode is
    /// used here only because it supports mutation for the negative tests below.</summary>
    private static JsonElement ToElement(JsonNode? node) =>
        node is null ? default : JsonSerializer.Deserialize<JsonElement>(node.ToJsonString());

    private static string DescribeErrors(EvaluationResults results)
    {
        var details = (results.Details ?? Enumerable.Empty<EvaluationResults>())
            .Where(d => !d.IsValid && d.Errors is { Count: > 0 })
            .SelectMany(d => d.Errors!.Select(e => $"  at {d.InstanceLocation}: {e.Key} - {e.Value}"));
        return string.Join(Environment.NewLine, details);
    }

    /// <summary>
    /// Walks upward from the test binary's output directory to find the repo-root
    /// "data/scenarios" directory (adapted from ScenarioPackageTests.ResolveFixture, which
    /// walks the same way to find "assets/data/scenarios/validation" — a sibling data tree
    /// used by other tests). Throws if not found within the search depth.
    /// </summary>
    private static string ResolveDataPath(string? subDirectory, string fileName)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 12; i++)
        {
            if (dir == null)
            {
                break;
            }

            var candidate = subDirectory == null
                ? Path.Combine(dir.FullName, "data", "scenarios", fileName)
                : Path.Combine(dir.FullName, "data", "scenarios", subDirectory, fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new FileNotFoundException(
            $"Could not resolve '{fileName}' under data/scenarios (subdirectory: '{subDirectory}') " +
            $"by walking up from '{AppContext.BaseDirectory}'.");
    }
}
