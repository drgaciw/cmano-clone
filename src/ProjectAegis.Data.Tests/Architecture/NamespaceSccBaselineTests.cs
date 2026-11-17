namespace ProjectAegis.Data.Tests.Architecture;

using Xunit;

/// <summary>
/// Frozen-baseline namespace-cycle pin (DRG-771 Lane C / CMA-30).
/// Fails only when a new multi-node SCC appears vs the committed PE-metadata baseline.
/// Breaking existing Delegation (or other) SCCs is out of scope — shrinking is allowed.
/// </summary>
public sealed class NamespaceSccBaselineTests
{
    private static readonly string[] AssemblyProjectDirs =
    {
        "ProjectAegis.Data",
        "ProjectAegis.Sim",
        "ProjectAegis.Delegation",
        "ProjectAegis.Delegation.UnityAdapter",
    };

    [Fact]
    public void No_new_intra_assembly_namespace_cycles_vs_frozen_baseline()
    {
        var baselinePath = ResolveRepoFile(
            "src", "ProjectAegis.Data.Tests", "Architecture", "Baselines", "namespace-scc-baseline.json");
        Assert.True(baselinePath != null, "Missing Architecture/Baselines/namespace-scc-baseline.json.");

        var baseline = NamespaceSccAnalyzer.LoadBaseline(baselinePath!);
        var novelAll = new List<string>();

        foreach (var projectDir in AssemblyProjectDirs)
        {
            var dll = ResolveBuiltAssembly(projectDir);
            Assert.True(
                dll != null,
                $"Missing built assembly for '{projectDir}'. Build the solution (or that project) before running this gate.");

            Assert.True(
                baseline.TryGetValue(projectDir, out var expected) && expected != null,
                $"Baseline JSON missing assembly key '{projectDir}'.");

            var actual = NamespaceSccAnalyzer.AnalyzeAssembly(dll!);
            var novel = NamespaceSccAnalyzer.FindNewCycles(expected!, actual);
            foreach (var scc in novel)
            {
                novelAll.Add($"{projectDir}: [{string.Join(", ", scc)}]");
            }
        }

        Assert.True(
            novelAll.Count == 0,
            "New intra-assembly namespace cycle(s) detected vs frozen PE-metadata baseline " +
            "(DRG-771 / CMA-30). Update the baseline only with an explicit architecture decision; " +
            "do not break existing Delegation SCCs in this lane.\n" +
            string.Join("\n", novelAll));
    }

    [Fact]
    public void Baseline_covers_all_pinned_assemblies()
    {
        var baselinePath = ResolveRepoFile(
            "src", "ProjectAegis.Data.Tests", "Architecture", "Baselines", "namespace-scc-baseline.json");
        Assert.True(baselinePath != null);
        var baseline = NamespaceSccAnalyzer.LoadBaseline(baselinePath!);
        foreach (var projectDir in AssemblyProjectDirs)
        {
            Assert.True(baseline.ContainsKey(projectDir), $"Missing baseline key '{projectDir}'.");
        }
    }

    private static string? ResolveBuiltAssembly(string projectDir)
    {
        var repoRoot = ResolveRepoRoot();
        if (repoRoot is null)
        {
            return null;
        }

        var candidates = new[]
        {
            Path.Combine(repoRoot, "src", projectDir, "bin", "Debug", "net11.0", projectDir + ".dll"),
            Path.Combine(repoRoot, "src", projectDir, "bin", "Release", "net11.0", projectDir + ".dll"),
            Path.Combine(repoRoot, "src", projectDir, "bin", "Debug", "netstandard2.1", projectDir + ".dll"),
            Path.Combine(repoRoot, "src", projectDir, "bin", "Release", "netstandard2.1", projectDir + ".dll"),
        };

        return candidates.FirstOrDefault(File.Exists);
    }

    private static string? ResolveRepoRoot()
    {
        var sln = ResolveRepoFile("ProjectAegis.sln");
        return sln is null ? null : Path.GetDirectoryName(sln);
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
