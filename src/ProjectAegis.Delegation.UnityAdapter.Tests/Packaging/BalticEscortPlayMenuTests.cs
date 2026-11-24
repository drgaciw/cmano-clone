using System.Text.RegularExpressions;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Packaging;

/// <summary>
/// UAT D28 guard: <c>Project Aegis/Play Baltic Escort</c> must open the committed scene and only
/// call <c>Build()</c> when the scene file is missing. Rebuilding on every Play re-saves
/// <c>BalticEscort.unity</c> with new file IDs and dirties git.
/// </summary>
public sealed class BalticEscortPlayMenuTests
{
    private static readonly Regex BuildCall = new(@"\bBuild\s*\(\s*\)", RegexOptions.Compiled);

    private static readonly Regex GuardedBuild = new(
        @"if\s*\(\s*!\s*File\.Exists\s*\(\s*ScenePath\s*\)\s*\)\s*\{\s*Build\s*\(\s*\)\s*;\s*\}",
        RegexOptions.Compiled);

    [Test]
    public void PlayFromMenu_opens_committed_scene_and_builds_only_when_missing()
    {
        var body = ReadPlayFromMenuBody();

        Assert.That(body, Does.Contain("SaveCurrentModifiedScenesIfUserWantsTo"),
            "Play must keep the unsaved-work prompt.");
        Assert.That(body, Does.Contain("EditorSceneManager.OpenScene("),
            "Play must open the committed scene via EditorSceneManager.OpenScene.");
        Assert.That(body, Does.Contain("ScenePath"), "Play must target BalticEscortSceneBuilder.ScenePath.");

        var buildCalls = BuildCall.Matches(body).Count;
        var guardedCalls = GuardedBuild.Matches(body).Count;
        Assert.That(guardedCalls, Is.EqualTo(buildCalls),
            "Every Build() call in PlayFromMenu must sit inside 'if (!File.Exists(ScenePath)) { Build(); }'.");
    }

    private static string ReadPlayFromMenuBody()
    {
        var repoRoot = FindRepoRoot();
        Assert.That(repoRoot, Is.Not.Null, "ProjectAegis.sln not found");

        var source = File.ReadAllText(Path.Combine(
            repoRoot!, "unity", "ProjectAegis", "Assets", "Editor", "BalticEscortSceneBuilder.cs"));

        var start = source.IndexOf("void PlayFromMenu", StringComparison.Ordinal);
        Assert.That(start, Is.GreaterThanOrEqualTo(0), "PlayFromMenu not found (guard stale?)");

        var open = source.IndexOf('{', start);
        var depth = 0;
        for (var i = open; i < source.Length; i++)
        {
            if (source[i] == '{') depth++;
            else if (source[i] == '}' && --depth == 0)
            {
                return source.Substring(open, i - open + 1);
            }
        }

        Assert.Fail("PlayFromMenu body braces unbalanced");
        return string.Empty;
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
