using System.Text.RegularExpressions;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Bridge;

/// <summary>
/// Headless source hygiene that blocked Linux player compile (2026-08-17).
/// </summary>
public sealed class UnityCsharpScriptHygieneTests
{
    private static readonly Regex InvalidHashComment = new(
        @"^\s*#\s+(?!(if|elif|else|endif|define|undef|region|endregion|pragma|warning|nullable|error|line)\b)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Test]
    public void Unity_CSharp_sources_do_not_use_hash_as_line_comment()
    {
        var root = FindRepoRoot();
        Assert.That(root, Is.Not.Null);

        var scriptsRoot = Path.Combine(root!, "unity", "ProjectAegis", "Assets");
        Assert.That(Directory.Exists(scriptsRoot), Is.True, scriptsRoot);

        var offenders = new List<string>();
        foreach (var path in Directory.EnumerateFiles(scriptsRoot, "*.cs", SearchOption.AllDirectories))
        {
            if (path.Contains($"{Path.DirectorySeparatorChar}Packages{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            var lines = File.ReadAllLines(path);
            for (var i = 0; i < lines.Length; i++)
            {
                if (InvalidHashComment.IsMatch(lines[i]))
                {
                    offenders.Add($"{Rel(root!, path)}:{i + 1}: {lines[i].Trim()}");
                }
            }
        }

        Assert.That(offenders, Is.Empty, string.Join(Environment.NewLine, offenders));
    }

    [Test]
    public void C2MenuPanelHost_opens_with_line_comment_not_hash_directive()
    {
        var root = FindRepoRoot();
        Assert.That(root, Is.Not.Null);

        var path = Path.Combine(
            root!,
            "unity",
            "ProjectAegis",
            "Assets",
            "Scripts",
            "Runtime",
            "C2MenuPanelHost.cs");
        Assert.That(File.Exists(path), Is.True, path);

        var first = File.ReadLines(path).FirstOrDefault() ?? string.Empty;
        Assert.That(first.TrimStart().StartsWith("//", StringComparison.Ordinal), Is.True, first);
        Assert.That(first.TrimStart().StartsWith("#", StringComparison.Ordinal), Is.False, first);
    }

    [Test]
    public void DelegationSmokeSceneBuilder_disambiguates_UnityEngine_Object()
    {
        var root = FindRepoRoot();
        Assert.That(root, Is.Not.Null);

        var path = Path.Combine(
            root!,
            "unity",
            "ProjectAegis",
            "Assets",
            "Editor",
            "DelegationSmokeSceneBuilder.cs");
        Assert.That(File.Exists(path), Is.True, path);

        var text = File.ReadAllText(path);
        Assert.That(text, Does.Contain("using Object = UnityEngine.Object;"));
    }

    private static string Rel(string root, string path) =>
        Path.GetRelativePath(root, path).Replace('\\', '/');

    private static string? FindRepoRoot()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "unity", "ProjectAegis")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        return null;
    }
}
