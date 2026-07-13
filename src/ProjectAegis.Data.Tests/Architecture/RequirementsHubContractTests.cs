namespace ProjectAegis.Data.Tests.Architecture;

using System.Text.RegularExpressions;
using Xunit;

/// <summary>
/// Adversarial pin for req 01 OV-SC-G5: hub FR-01…FR-19 map + Related Index targets exist.
/// Docs-only corpus honesty — no Unity. Pattern: NoDynamicExecutionGateTests repo walk.
/// </summary>
public sealed class RequirementsHubContractTests
{
    [Fact]
    public void hub_lists_FR_01_through_FR_19_and_related_index_targets_exist()
    {
        var hubPath = ResolveRepoFile("Game-Requirements", "requirements", "01-Project-Overview.md");
        Assert.True(hubPath != null, "Could not locate 01-Project-Overview.md");
        var hub = File.ReadAllText(hubPath!);

        for (var i = 1; i <= 19; i++)
        {
            Assert.Contains($"FR-{i:D2}", hub, StringComparison.Ordinal);
        }

        Assert.Contains("21-Platform-Editor.md", hub, StringComparison.Ordinal);
        Assert.Matches(
            new Regex(@"FR-19.*21-Platform-Editor\.md", RegexOptions.Singleline),
            hub);

        var idx = hub.IndexOf("## Related Requirements Index", StringComparison.Ordinal);
        Assert.True(idx >= 0, "Related Requirements Index section missing");
        var indexSection = hub[idx..];

        var linkMatches = Regex.Matches(indexSection, @"\((\d{2}-[^)]+\.md)\)");
        Assert.True(linkMatches.Count >= 20, $"Expected ≥20 Related Index links, got {linkMatches.Count}");

        var reqDir = Path.GetDirectoryName(hubPath)!;
        foreach (Match m in linkMatches)
        {
            var rel = m.Groups[1].Value;
            var target = Path.Combine(reqDir, rel);
            Assert.True(File.Exists(target), $"Broken Related Index target: {rel}");
        }

        // Every on-disk requirements/*.md except hub itself must appear in Related Index
        foreach (var file in Directory.GetFiles(reqDir, "*.md"))
        {
            var name = Path.GetFileName(file);
            if (string.Equals(name, "01-Project-Overview.md", StringComparison.Ordinal))
            {
                continue;
            }

            Assert.Contains(name, indexSection, StringComparison.Ordinal);
        }
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
