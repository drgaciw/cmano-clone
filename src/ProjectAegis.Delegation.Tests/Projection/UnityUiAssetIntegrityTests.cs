using System.Text.RegularExpressions;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

/// <summary>
/// Guardrails for Unity UI assets that surface as Editor Console errors when missing.
/// </summary>
public sealed class UnityUiAssetIntegrityTests
{
    private static readonly Regex UnityFontResource = new(
        @"-unity-font:\s*resource\(""([^""]+)""\)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Test]
    public void USS_font_resource_paths_resolve_under_Assets_or_are_absent()
    {
        var root = FindUnityProjectRoot();
        Assert.That(root, Is.Not.Null, "unity/ProjectAegis not found");

        var uiRoot = Path.Combine(root!, "Assets", "UI");
        Assert.That(Directory.Exists(uiRoot), Is.True, uiRoot);

        var missing = new List<string>();
        foreach (var uss in Directory.EnumerateFiles(uiRoot, "*.uss", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(uss);
            foreach (Match m in UnityFontResource.Matches(text))
            {
                var rel = m.Groups[1].Value.Replace('\\', '/');
                // resource("Fonts/X") → Assets/Resources/Fonts/X.* or Assets/Fonts/X.*
                var candidates = new[]
                {
                    Path.Combine(root!, "Assets", "Resources", rel + ".ttf"),
                    Path.Combine(root!, "Assets", "Resources", rel + ".otf"),
                    Path.Combine(root!, "Assets", "Resources", rel + ".ttf.meta"),
                    Path.Combine(root!, "Assets", rel + ".ttf"),
                    Path.Combine(root!, "Assets", rel + ".otf"),
                };
                if (!candidates.Any(File.Exists))
                {
                    missing.Add($"{Path.GetRelativePath(root!, uss)} → resource(\"{rel}\")");
                }
            }
        }

        Assert.That(
            missing,
            Is.Empty,
            "USS references font resources that are not present under Assets. " +
            "Remove the -unity-font rule or add the font under Assets/Resources/.\n" +
            string.Join("\n", missing));
    }

    [Test]
    public void DelegationSmoke_scene_does_not_serialize_AudioListener()
    {
        var root = FindUnityProjectRoot();
        Assert.That(root, Is.Not.Null);
        var scene = Path.Combine(root!, "Assets", "Scenes", "DelegationSmoke.unity");
        Assert.That(File.Exists(scene), Is.True, scene);

        var text = File.ReadAllText(scene);
        Assert.That(
            text,
            Does.Not.Contain("AudioListener:"),
            "DelegationSmoke must not include AudioListener when the audio module is disabled " +
            "(Console: 'AudioListener component deleted: Component belongs to a disabled built-in package').");
    }

    private static string? FindUnityProjectRoot()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "unity", "ProjectAegis");
            if (Directory.Exists(Path.Combine(candidate, "Assets")))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        return null;
    }
}
