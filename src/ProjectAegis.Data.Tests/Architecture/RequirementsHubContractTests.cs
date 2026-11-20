namespace ProjectAegis.Data.Tests.Architecture;

using System.Text.RegularExpressions;
using NUnit.Framework;

/// <summary>
/// Adversarial pin for req 01 OV-SC-G5: hub FR-01…FR-20 map + Related Index targets exist.
/// Docs-only corpus honesty — no Unity. Pattern: NoDynamicExecutionGateTests repo walk.
/// </summary>
public sealed class RequirementsHubContractTests
{
    [Test]
    public void hub_lists_FR_01_through_FR_20_and_related_index_targets_exist()
    {
        var hubPath = ResolveRepoFile("Game-Requirements", "requirements", "01-Project-Overview.md");
        Assert.That(hubPath != null, Is.True, "Could not locate 01-Project-Overview.md");
        var hub = File.ReadAllText(hubPath!);

        for (var i = 1; i <= 20; i++)
        {
            Assert.That(hub, Does.Contain($"FR-{i:D2}"));
        }

        Assert.That(hub, Does.Contain("21-Platform-Editor.md"));
        Assert.That(            hub, Does.Match(            new Regex(@"FR-19.*21-Platform-Editor\.md", RegexOptions.Singleline)));
        Assert.That(hub, Does.Contain("22-Drone-Swarm-Platforms.md"));
        Assert.That(            hub, Does.Match(            new Regex(@"FR-20.*22-Drone-Swarm-Platforms\.md", RegexOptions.Singleline)));

        var idx = hub.IndexOf("## Related Requirements Index", StringComparison.Ordinal);
        Assert.That(idx >= 0, Is.True, "Related Requirements Index section missing");
        var indexSection = hub[idx..];

        var linkMatches = Regex.Matches(indexSection, @"\((\d{2}-[^)]+\.md)\)");
        Assert.That(linkMatches.Count >= 21, Is.True, $"Expected ≥21 Related Index links, got {linkMatches.Count}");

        var reqDir = Path.GetDirectoryName(hubPath)!;
        foreach (Match m in linkMatches)
        {
            var rel = m.Groups[1].Value;
            var target = Path.Combine(reqDir, rel);
            Assert.That(File.Exists(target), Is.True, $"Broken Related Index target: {rel}");
        }

        // Every on-disk requirements/*.md except hub itself must appear in Related Index
        foreach (var file in Directory.GetFiles(reqDir, "*.md"))
        {
            var name = Path.GetFileName(file);
            if (string.Equals(name, "01-Project-Overview.md", StringComparison.Ordinal))
            {
                continue;
            }

            Assert.That(indexSection, Does.Contain(name));
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
