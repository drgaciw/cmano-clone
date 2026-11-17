namespace ProjectAegis.Data.Tests.Architecture;

using System.Text.RegularExpressions;
using Xunit;

/// <summary>
/// Forbids new direct <c>using ProjectAegis.Sim...</c> imports in Unity Runtime scripts
/// (ADR-010 projection boundary). Existing three hosts are frozen as an allowlist baseline
/// (DRG-771 Lane C / CMA-30). Hard-no hosts stay listed; this lane does not edit them.
/// </summary>
public sealed class UnitySimImportForbidTests
{
    private static readonly string[] BaselineRelativePaths =
    {
        "unity/ProjectAegis/Assets/Scripts/Runtime/MapPlaceholderPanelHost.cs",
        "unity/ProjectAegis/Assets/Scripts/Runtime/C2TopBarPanelHost.cs",
        "unity/ProjectAegis/Assets/Scripts/Runtime/DelegationBridgeHost.cs",
    };

    private static readonly Regex SimUsingRegex = new(
        @"^\s*using\s+ProjectAegis\.Sim(\.|;)",
        RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.CultureInvariant);

    [Fact]
    public void Baseline_is_exactly_three_unity_runtime_hosts()
    {
        Assert.Equal(3, BaselineRelativePaths.Length);
    }

    [Fact]
    public void Baseline_hosts_still_import_sim()
    {
        foreach (var relative in BaselineRelativePaths)
        {
            var path = ResolveRepoFile(relative.Split('/'));
            Assert.True(path != null, $"Missing baseline host '{relative}'.");
            var source = File.ReadAllText(path!);
            Assert.True(
                SimUsingRegex.IsMatch(source),
                $"Baseline host '{relative}' no longer imports ProjectAegis.Sim — update allowlist deliberately.");
        }
    }

    [Fact]
    public void No_new_unity_runtime_scripts_import_projectaegis_sim()
    {
        var runtimeRoot = ResolveRepoDirectory("unity", "ProjectAegis", "Assets", "Scripts", "Runtime");
        Assert.True(runtimeRoot != null, "Missing unity/ProjectAegis/Assets/Scripts/Runtime.");

        var baselineFull = new HashSet<string>(
            BaselineRelativePaths
                .Select(r => ResolveRepoFile(r.Split('/')))
                .Where(p => p != null)
                .Select(p => Path.GetFullPath(p!)),
            StringComparer.OrdinalIgnoreCase);

        Assert.Equal(3, baselineFull.Count);

        var offenders = new List<string>();
        foreach (var file in Directory.EnumerateFiles(runtimeRoot!, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            var full = Path.GetFullPath(file);
            if (baselineFull.Contains(full))
            {
                continue;
            }

            var source = File.ReadAllText(file);
            if (SimUsingRegex.IsMatch(source))
            {
                offenders.Add(ToRepoRelative(full) ?? full);
            }
        }

        Assert.True(
            offenders.Count == 0,
            "New Unity Runtime script(s) import ProjectAegis.Sim directly (ADR-010). " +
            "Use projections/adapters instead; existing baseline of 3 hosts is frozen.\n" +
            string.Join("\n", offenders.OrderBy(s => s, StringComparer.Ordinal)));
    }

    private static string? ToRepoRelative(string fullPath)
    {
        var sln = ResolveRepoFile("ProjectAegis.sln");
        if (sln is null)
        {
            return null;
        }

        var root = Path.GetDirectoryName(sln)!;
        return Path.GetRelativePath(root, fullPath).Replace('\\', '/');
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

    private static string? ResolveRepoDirectory(params string[] relativeSegments)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 12; i++)
        {
            if (dir == null)
            {
                break;
            }

            var candidate = Path.Combine(new[] { dir.FullName }.Concat(relativeSegments).ToArray());
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        return null;
    }
}
