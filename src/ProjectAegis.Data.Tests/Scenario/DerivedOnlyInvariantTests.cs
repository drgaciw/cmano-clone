namespace ProjectAegis.Data.Tests.Scenario;

using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Xunit;

/// <summary>
/// AC-9 / AME-2.4 (qa-plan-scenario-editor-2026-07-01.md #9): Schema + DTO conformance lint
/// ensuring <c>editorState</c> is the ONLY node tagged <c>derived-only</c>.
/// Walks the committed schema (using x-aegis-derived-only marker) and DTO source comments.
/// Complements Architecture/DerivedOnlyInvariantTests (the no-reads entry-point lint).
/// </summary>
public sealed class DerivedOnlyInvariantTests
{
    [Fact]
    public void EditorState_is_the_only_derived_only_node_in_schema()
    {
        var schemaPath = ResolveDataPath(null, "scenario-document.schema.json");
        var schemaJson = File.ReadAllText(schemaPath);
        var schemaNode = JsonNode.Parse(schemaJson)!;

        var derivedOnlyNodes = new List<string>();
        WalkForDerivedTag(schemaNode, "", derivedOnlyNodes);

        Assert.True(
            derivedOnlyNodes.Count == 1 && string.Equals(derivedOnlyNodes[0], "editorState", StringComparison.Ordinal),
            $"Expected exactly ['editorState'] as the derived-only node per AC-9, but found: [{string.Join(", ", derivedOnlyNodes)}]. " +
            "editorState must be the only derived-only node.");
    }

    [Fact]
    public void EditorState_is_the_only_derived_only_in_dto_source()
    {
        var dtoPath = ResolveSourcePath("src/ProjectAegis.Data/Scenario/Authoring/ScenarioDocumentDto.cs");
        var source = File.ReadAllText(dtoPath);

        // Walk source comments for "Derived-only" marker on properties (matches the DTO doc comment style).
        var derivedProps = new List<string>();
        var lines = source.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].Contains("Derived-only", StringComparison.OrdinalIgnoreCase))
            {
                for (int j = i; j < Math.Min(i + 5, lines.Length); j++)
                {
                    var m = Regex.Match(lines[j], @"\b(public|private|protected)\s+[^;{]+?\s+(\w+)\s*\{");
                    if (m.Success)
                    {
                        derivedProps.Add(m.Groups[2].Value);
                        break;
                    }
                }
            }
        }

        Assert.True(
            derivedProps.Count == 1 && string.Equals(derivedProps[0], "EditorState", StringComparison.Ordinal),
            $"Expected only EditorState marked Derived-only in DTO source, found: [{string.Join(",", derivedProps)}]");
    }

    private static void WalkForDerivedTag(JsonNode? node, string path, List<string> hits)
    {
        if (node is JsonObject obj)
        {
            if (obj.TryGetPropertyValue("x-aegis-derived-only", out var tag) &&
                tag is JsonValue v &&
                v.TryGetValue<bool>(out var isDerived) &&
                isDerived)
            {
                var name = path.Split('.').LastOrDefault() ?? path;
                if (!string.IsNullOrEmpty(name)) hits.Add(name);
            }

            foreach (var kv in obj)
            {
                var childPath = string.IsNullOrEmpty(path) ? kv.Key : $"{path}.{kv.Key}";
                WalkForDerivedTag(kv.Value, childPath, hits);
            }
        }
        else if (node is JsonArray arr)
        {
            for (int i = 0; i < arr.Count; i++)
            {
                WalkForDerivedTag(arr[i], $"{path}[{i}]", hits);
            }
        }
    }

    /// <summary>
    /// Mirrors ResolveDataPath in sibling conformance test for locating data/scenarios assets.
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

    private static string ResolveSourcePath(string relative)
    {
        var segments = relative.Split('/');
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 12; i++)
        {
            if (dir == null)
            {
                break;
            }

            var candidate = Path.Combine(new[] { dir.FullName }.Concat(segments).ToArray());
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new FileNotFoundException($"Could not resolve source '{relative}'");
    }
}
