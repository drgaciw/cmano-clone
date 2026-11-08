using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

/// <summary>Shared repo-root file reads for UiIa source contracts (no UI Toolkit).</summary>
internal static class UiIaSourceReader
{
    public static string RequireRepoRoot()
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

        Assert.Fail("Could not locate repo root (missing unity/ProjectAegis).");
        return string.Empty;
    }

    public static string ReadRuntime(string fileName)
    {
        var path = Path.Combine(
            RequireRepoRoot(),
            "unity",
            "ProjectAegis",
            "Assets",
            "Scripts",
            "Runtime",
            fileName);
        Assert.That(File.Exists(path), Is.True, path);
        return File.ReadAllText(path);
    }

    public static string ReadUnder(params string[] relativeParts)
    {
        var parts = new List<string> { RequireRepoRoot() };
        parts.AddRange(relativeParts);
        var path = Path.Combine(parts.ToArray());
        Assert.That(File.Exists(path), Is.True, path);
        return File.ReadAllText(path);
    }
}
