using System.Text.RegularExpressions;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

/// <summary>
/// S125-03 / W3-AUTH-01: evidence-first DoD pin. The frozen S125 DoD lives in the headless evidence doc;
/// every Must item maps to test classes that exist, and the real-Editor items stay explicitly pending
/// until screenshots are attached (no headless substitute is accepted for S125-01/02).
/// </summary>
[TestFixture]
public sealed class S125EditorHonestyEvidencePinTests
{
    private const string EvidenceDoc = "docs/superpowers/reviews/2026-10-06-s125-editor-honesty-headless-evidence.md";

    private static readonly string[] HeadlessItems = ["S125-03", "S125-04", "S125-05", "S125-06", "S125-07", "S125-08"];

    [Test]
    public void Evidence_doc_has_frozen_dod_section()
    {
        var text = Read();

        Assert.That(text, Does.Contain("## DoD freeze (W3-AUTH-01)"));
        Assert.That(text, Does.Contain("Frozen: 2026-10-06"));
        Assert.That(text, Does.Contain("production/sprints/sprint-125-editor-honesty.md"));
    }

    [Test]
    public void Real_editor_items_remain_pending()
    {
        foreach (var id in new[] { "S125-01", "S125-02" })
        {
            var row = Row(Read(), id);
            Assert.That(row, Does.Contain("PENDING — real Editor evidence"), id);
            Assert.That(row, Does.Not.Contain("DONE"), id);
        }
    }

    [Test]
    public void Every_headless_item_maps_to_existing_test_classes()
    {
        var text = Read();
        var testFiles = Directory.GetFiles(Path.Combine(RepoRoot(), "src"), "*Tests.cs", SearchOption.AllDirectories)
            .Select(Path.GetFileNameWithoutExtension)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var id in HeadlessItems)
        {
            var row = Row(text, id);
            var classes = Regex.Matches(row, @"`(\w+Tests)`").Select(m => m.Groups[1].Value).ToArray();
            Assert.That(classes, Is.Not.Empty, $"{id} lists no test classes");
            Assert.That(classes, Is.SubsetOf(testFiles), $"{id} cites a test class that does not exist");
        }
    }

    private static string Row(string text, string id) =>
        text.Split('\n').FirstOrDefault(l => l.TrimStart().StartsWith($"| {id} ", StringComparison.Ordinal))
        ?? throw new AssertionException($"{id} row missing from evidence table");

    private static string Read() => File.ReadAllText(Path.Combine(RepoRoot(), EvidenceDoc));

    private static string RepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 10; i++)
        {
            if (File.Exists(Path.Combine(dir, "ProjectAegis.sln")))
            {
                return dir;
            }

            dir = Path.GetDirectoryName(dir.TrimEnd(Path.DirectorySeparatorChar)) ?? dir;
        }

        throw new DirectoryNotFoundException("ProjectAegis.sln not found above test output.");
    }
}
