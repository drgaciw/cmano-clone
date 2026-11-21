using System.Text.RegularExpressions;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Packaging;

/// <summary>
/// UAT D17 guard: every <c>Assets/UI/**.uxml|.uss</c> path the editor scene builders pass to
/// <c>AssetDatabase.LoadAssetAtPath</c> must exist on disk. A missing path loads as null and the
/// panel host silently renders nothing.
/// </summary>
public sealed class SceneBuilderUiAssetPathTests
{
    private static readonly Regex FullPath = new(
        "\"(Assets/UI/[^\"]+\\.(?:uxml|uss))\"", RegexOptions.Compiled);

    private static readonly Regex UiPathConst = new(
        "const\\s+string\\s+UiPath\\s*=\\s*\"(Assets/UI/[^\"]+/)\"", RegexOptions.Compiled);

    private static readonly Regex UiPathConcat = new(
        "UiPath\\s*\\+\\s*\"([^\"]+\\.(?:uxml|uss))\"", RegexOptions.Compiled);

    [TestCase("DelegationSmokeSceneBuilder.cs")]
    [TestCase("BalticEscortSceneBuilder.cs")]
    public void Referenced_uxml_and_uss_assets_exist(string builderFile)
    {
        var repoRoot = FindRepoRoot();
        Assert.That(repoRoot, Is.Not.Null, "ProjectAegis.sln not found");

        var unityRoot = Path.Combine(repoRoot!, "unity", "ProjectAegis");
        var source = File.ReadAllText(Path.Combine(unityRoot, "Assets", "Editor", builderFile));

        var paths = new SortedSet<string>(StringComparer.Ordinal);
        foreach (Match m in FullPath.Matches(source))
        {
            paths.Add(m.Groups[1].Value);
        }

        var prefix = UiPathConst.Match(source);
        if (prefix.Success)
        {
            foreach (Match m in UiPathConcat.Matches(source))
            {
                paths.Add(prefix.Groups[1].Value + m.Groups[1].Value);
            }
        }

        Assert.That(paths, Is.Not.Empty, $"{builderFile}: no Assets/UI paths parsed (guard regex stale?)");

        var missing = paths
            .Where(p => !File.Exists(Path.Combine(unityRoot, p.Replace('/', Path.DirectorySeparatorChar))))
            .ToList();

        Assert.That(missing, Is.Empty,
            $"{builderFile} references missing UI assets:\n  " + string.Join("\n  ", missing));
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
