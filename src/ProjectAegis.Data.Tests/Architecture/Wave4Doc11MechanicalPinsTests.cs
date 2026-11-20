namespace ProjectAegis.Data.Tests.Architecture;

using NUnit.Framework;

/// <summary>
/// Wave 4 charter honesty pins for Doc 11 (Agentic Mission Editor): FR reverse-ref,
/// Related link filename repair, schema/fixtures mapping status, and corpus file count.
/// Docs-only — no production changes. Pattern: RequirementsHubContractTests / Wave3.
/// </summary>
public sealed class Wave4Doc11MechanicalPinsTests
{
    private const string Doc11FileName = "11-Agentic-Mission-Editor.md";

    [Test]
    public void doc_11_reverse_ref_FR_09()
    {
        var path = ResolveRepoFile("Game-Requirements", "requirements", Doc11FileName);
        Assert.That(path != null, Is.True, $"Could not locate {Doc11FileName}");
        var text = File.ReadAllText(path!);

        Assert.That(text, Does.Contain("FR-09"));
        Assert.That(            text.Contains("FR reverse-ref", StringComparison.Ordinal)
            || text.Contains("Implements hub", StringComparison.Ordinal), Is.True,             "Doc 11 must reverse-ref hub FR-09 via 'FR reverse-ref' and/or 'Implements hub'");
    }

    [Test]
    public void doc_11_related_links_to_correct_replay_aar_filename()
    {
        var path = ResolveRepoFile("Game-Requirements", "requirements", Doc11FileName);
        Assert.That(path != null, Is.True, $"Could not locate {Doc11FileName}");
        var text = File.ReadAllText(path!);

        Assert.That(text, Does.Contain("17-Replay-AAR-And-Order-Log.md"));
        Assert.That(text, Does.Not.Contain("17-Replay-And-Order-Log.md"));

        var reqDir = Path.GetDirectoryName(path)!;
        var target = Path.Combine(reqDir, "17-Replay-AAR-And-Order-Log.md");
        Assert.That(File.Exists(target), Is.True, "Related link target 17-Replay-AAR-And-Order-Log.md must exist next to doc 11");
    }

    [Test]
    public void doc_11_schema_fixtures_mapping_is_shipped_not_new()
    {
        var path = ResolveRepoFile("Game-Requirements", "requirements", Doc11FileName);
        Assert.That(path != null, Is.True, $"Could not locate {Doc11FileName}");
        var text = File.ReadAllText(path!);

        var mapIdx = text.IndexOf("## Implementation Mapping", StringComparison.Ordinal);
        Assert.That(mapIdx >= 0, Is.True, "Implementation Mapping heading missing");
        var mappingSection = text[mapIdx..];

        Assert.That(mappingSection, Does.Contain("scenario-document.schema.json"));

        var schemaLine = mappingSection
            .Split('\n')
            .FirstOrDefault(l => l.Contains("scenario-document.schema.json", StringComparison.Ordinal));
        Assert.That(schemaLine != null, Is.True, "Implementation Mapping row for scenario-document.schema.json not found");
        Assert.That(schemaLine, Does.Contain("Shipped"));
        Assert.That(schemaLine, Does.Not.Contain("New (data workstream)"));

        var schemaPath = ResolveRepoFile("data", "scenarios", "scenario-document.schema.json");
        Assert.That(            schemaPath != null && File.Exists(schemaPath), Is.True,             "data/scenarios/scenario-document.schema.json must exist on disk");
    }

    [Test]
    public void requirements_corpus_has_exactly_22_md_files()
    {
        var anyReq = ResolveRepoFile("Game-Requirements", "requirements", Doc11FileName);
        Assert.That(anyReq != null, Is.True, "Could not resolve Game-Requirements/requirements/");
        var reqDir = Path.GetDirectoryName(anyReq)!;

        var mdFiles = Directory.GetFiles(reqDir, "*.md");
        Assert.That(mdFiles.Length, Is.EqualTo(22));
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
