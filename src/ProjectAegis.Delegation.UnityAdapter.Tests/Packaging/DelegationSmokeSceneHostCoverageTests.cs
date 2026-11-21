using System.Text.RegularExpressions;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Packaging;

/// <summary>
/// Guards the committed <c>DelegationSmoke.unity</c> against drifting from
/// <c>DelegationSmokeSceneBuilder.EnsureUiMaturityHostsOnOpenScene</c>. A stale scene silently
/// loses panel hosts (unit-order toolbar, menus, attention toast, ...), so Play Mode shows no
/// toast and never auto-pauses. Every host type the builder ensures must have its script GUID
/// referenced by the scene file.
/// </summary>
public sealed class DelegationSmokeSceneHostCoverageTests
{
    private static readonly Regex EnsuredHost =
        new(@"EnsurePanelHostIfMissing<(\w+)>", RegexOptions.CultureInvariant);

    private static readonly Regex GuidLine =
        new(@"^guid:\s*([0-9a-f]{32})\s*$", RegexOptions.Multiline | RegexOptions.CultureInvariant);

    /// <summary>
    /// Hosts the committed scene must carry. <c>AttentionToastPanelHost</c> runs the demo-watch seed
    /// that auto-pauses the sim (checklist DRG-162). The builder ensures 17 further hosts that the
    /// committed scene also lacks; adding them all stacks overlapping full-screen panels over the map,
    /// so that wider drift is an owner decision (see production/qa/uat-playmode-2026-11-21.md, D15).
    /// </summary>
    private static readonly string[] RequiredHosts = { "AttentionToastPanelHost" };

    [Test]
    public void Committed_scene_hosts_attention_toast_for_demo_watch_auto_pause()
    {
        var repoRoot = FindRepoRoot();
        Assert.That(repoRoot, Is.Not.Null);

        var project = Path.Combine(repoRoot!, "unity", "ProjectAegis");
        var builderPath = Path.Combine(project, "Assets", "Editor", "DelegationSmokeSceneBuilder.cs");
        var scenePath = Path.Combine(project, "Assets", "Scenes", "DelegationSmoke.unity");
        Assert.That(File.Exists(builderPath), Is.True, builderPath);
        Assert.That(File.Exists(scenePath), Is.True, scenePath);

        var ensured = EnsuredHost.Matches(File.ReadAllText(builderPath))
            .Select(m => m.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);
        var hostTypes = RequiredHosts.ToList();
        foreach (var required in hostTypes)
        {
            Assert.That(
                ensured,
                Does.Contain(required),
                $"DelegationSmokeSceneBuilder no longer ensures {required}; update RequiredHosts.");
        }

        var scene = File.ReadAllText(scenePath);
        var assets = Path.Combine(project, "Assets");
        var missing = new List<string>();

        foreach (var type in hostTypes)
        {
            var source = Directory.EnumerateFiles(assets, type + ".cs", SearchOption.AllDirectories).FirstOrDefault();
            if (source is null)
            {
                missing.Add($"{type} (no {type}.cs under Assets)");
                continue;
            }

            var meta = GuidLine.Match(File.ReadAllText(source + ".meta"));
            if (!meta.Success)
            {
                missing.Add($"{type} (script .meta has no valid GUID)");
                continue;
            }

            if (!scene.Contains(meta.Groups[1].Value, StringComparison.Ordinal))
            {
                missing.Add(type);
            }
        }

        Assert.That(
            missing,
            Is.Empty,
            "DelegationSmoke.unity is missing hosts the builder ensures. Run " +
            "'Project Aegis > Ensure UI Maturity Hosts (open scene)' and save the scene:\n" +
            string.Join("\n", missing));
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
