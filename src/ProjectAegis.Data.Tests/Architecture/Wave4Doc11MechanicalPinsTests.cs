namespace ProjectAegis.Data.Tests.Architecture;

using Xunit;

/// <summary>
/// Wave 4 charter honesty pins for Doc 11 (Agentic Mission Editor): FR reverse-ref,
/// Related link filename repair, schema/fixtures mapping status, and corpus file count.
/// Docs-only — no production changes. Pattern: RequirementsHubContractTests / Wave3.
/// </summary>
public sealed class Wave4Doc11MechanicalPinsTests
{
    private const string Doc11FileName = "11-Agentic-Mission-Editor.md";

    [Fact]
    public void doc_11_reverse_ref_FR_09()
    {
        var path = ResolveRepoFile("Game-Requirements", "requirements", Doc11FileName);
        Assert.True(path != null, $"Could not locate {Doc11FileName}");
        var text = File.ReadAllText(path!);

        Assert.Contains("FR-09", text, StringComparison.Ordinal);
        Assert.True(
            text.Contains("FR reverse-ref", StringComparison.Ordinal)
            || text.Contains("Implements hub", StringComparison.Ordinal),
            "Doc 11 must reverse-ref hub FR-09 via 'FR reverse-ref' and/or 'Implements hub'");
    }

    [Fact]
    public void doc_11_related_links_to_correct_replay_aar_filename()
    {
        var path = ResolveRepoFile("Game-Requirements", "requirements", Doc11FileName);
        Assert.True(path != null, $"Could not locate {Doc11FileName}");
        var text = File.ReadAllText(path!);

        Assert.Contains("17-Replay-AAR-And-Order-Log.md", text, StringComparison.Ordinal);
        Assert.DoesNotContain("17-Replay-And-Order-Log.md", text, StringComparison.Ordinal);

        var reqDir = Path.GetDirectoryName(path)!;
        var target = Path.Combine(reqDir, "17-Replay-AAR-And-Order-Log.md");
        Assert.True(File.Exists(target), "Related link target 17-Replay-AAR-And-Order-Log.md must exist next to doc 11");
    }

    [Fact]
    public void doc_11_schema_fixtures_mapping_is_shipped_not_new()
    {
        var path = ResolveRepoFile("Game-Requirements", "requirements", Doc11FileName);
        Assert.True(path != null, $"Could not locate {Doc11FileName}");
        var text = File.ReadAllText(path!);

        var mapIdx = text.IndexOf("## Implementation Mapping", StringComparison.Ordinal);
        Assert.True(mapIdx >= 0, "Implementation Mapping heading missing");
        var mappingSection = text[mapIdx..];

        Assert.Contains("scenario-document.schema.json", mappingSection, StringComparison.Ordinal);

        var schemaLine = mappingSection
            .Split('\n')
            .FirstOrDefault(l => l.Contains("scenario-document.schema.json", StringComparison.Ordinal));
        Assert.True(schemaLine != null, "Implementation Mapping row for scenario-document.schema.json not found");
        Assert.Contains("Shipped", schemaLine!, StringComparison.Ordinal);
        Assert.DoesNotContain("New (data workstream)", schemaLine!, StringComparison.Ordinal);

        var schemaPath = ResolveRepoFile("data", "scenarios", "scenario-document.schema.json");
        Assert.True(
            schemaPath != null && File.Exists(schemaPath),
            "data/scenarios/scenario-document.schema.json must exist on disk");
    }

    [Fact]
    public void requirements_corpus_has_exactly_21_md_files()
    {
        var anyReq = ResolveRepoFile("Game-Requirements", "requirements", Doc11FileName);
        Assert.True(anyReq != null, "Could not resolve Game-Requirements/requirements/");
        var reqDir = Path.GetDirectoryName(anyReq)!;

        var mdFiles = Directory.GetFiles(reqDir, "*.md");
        Assert.Equal(21, mdFiles.Length);
    }

    private static string? ResolveRepoFile(params string[] relativeSegments)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 12; i++)
        {
            if (dir == null)
            {
                break;
            }

            var candidate = Path.Combine(new[] { dir.FullName }.Concat(relativeSegments).ToArray());
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        return null;
    }
}
