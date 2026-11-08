using System.Text.Json;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Packaging;

/// <summary>
/// Guards <c>Packages/manifest.template.json</c> against drift from the live
/// <c>Packages/manifest.json</c>. <c>tools/init-unity-project.ps1</c> copies the
/// template when no manifest exists, so a stale pin silently downgrades fresh scaffolds.
/// </summary>
public sealed class UnityPackageManifestMirrorTests
{
    [Test]
    public void Template_dependencies_match_live_manifest()
    {
        var live = ReadDependencies("manifest.json");
        var template = ReadDependencies("manifest.template.json");

        Assert.That(template, Is.EquivalentTo(live),
            "Packages/manifest.template.json dependencies must mirror Packages/manifest.json");
    }

    private static Dictionary<string, string> ReadDependencies(string fileName)
    {
        var repoRoot = FindRepoRoot();
        Assert.That(repoRoot, Is.Not.Null);

        var path = Path.Combine(repoRoot!, "unity", "ProjectAegis", "Packages", fileName);
        Assert.That(File.Exists(path), Is.True, path);

        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        return doc.RootElement
            .GetProperty("dependencies")
            .EnumerateObject()
            .ToDictionary(p => p.Name, p => p.Value.GetString() ?? string.Empty, StringComparer.Ordinal);
    }

    private static string? FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8; i++)
        {
            if (File.Exists(Path.Combine(dir, "ProjectAegis.sln")))
            {
                return dir;
            }

            dir = Directory.GetParent(dir)?.FullName ?? dir;
        }

        return null;
    }
}
