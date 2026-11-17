namespace ProjectAegis.Data.Tests.Architecture;

using System.Xml.Linq;
using Xunit;

/// <summary>
/// Pins the headless csproj ProjectReference graph for ADR-001 / ADR-006 / ADR-010 / ADR-030
/// (DRG-771 Lane C / CMA-30). Source/manifest scan only — no production edits.
/// </summary>
public sealed class CsprojReferenceGraphPinTests
{
    /// <summary>
    /// Exact ProjectReference Include file names (basename) allowed per product csproj.
    /// MissionEditor.Cli → Ai.Host is current main state (Decision item in DRG-771 §8.1; Lane D waiver).
    /// </summary>
    private static readonly Dictionary<string, string[]> AllowedProjectReferenceBasenames =
        new(StringComparer.Ordinal)
        {
            ["src/ProjectAegis.Data/ProjectAegis.Data.csproj"] = Array.Empty<string>(),
            ["src/ProjectAegis.Sim/ProjectAegis.Sim.csproj"] = new[] { "ProjectAegis.Data.csproj" },
            ["src/ProjectAegis.Delegation/ProjectAegis.Delegation.csproj"] =
                new[] { "ProjectAegis.Data.csproj", "ProjectAegis.Sim.csproj" },
            ["src/ProjectAegis.Ai.Contracts/ProjectAegis.Ai.Contracts.csproj"] = Array.Empty<string>(),
            ["src/ProjectAegis.Ai.Host/ProjectAegis.Ai.Host.csproj"] =
                new[]
                {
                    "ProjectAegis.Ai.Contracts.csproj",
                    "ProjectAegis.Data.csproj",
                    "ProjectAegis.Delegation.csproj",
                },
            ["src/ProjectAegis.Ai.LiveEval/ProjectAegis.Ai.LiveEval.csproj"] =
                new[] { "ProjectAegis.Ai.Contracts.csproj", "ProjectAegis.Ai.Host.csproj" },
            ["src/ProjectAegis.Data.Excel/ProjectAegis.Data.Excel.csproj"] =
                new[] { "ProjectAegis.Data.csproj" },
            ["src/ProjectAegis.Delegation.UnityAdapter/ProjectAegis.Delegation.UnityAdapter.csproj"] =
                new[]
                {
                    "ProjectAegis.Ai.Contracts.csproj",
                    "ProjectAegis.Data.csproj",
                    "ProjectAegis.Delegation.csproj",
                },
            ["src/ProjectAegis.MissionEditor.Cli/ProjectAegis.MissionEditor.Cli.csproj"] =
                new[]
                {
                    "ProjectAegis.Ai.Host.csproj",
                    "ProjectAegis.Data.csproj",
                    "ProjectAegis.Data.Excel.csproj",
                    "ProjectAegis.Delegation.UnityAdapter.csproj",
                },
        };

    private static readonly string[] HeadlessNoUnityEngineProjects =
    {
        "src/ProjectAegis.Data/ProjectAegis.Data.csproj",
        "src/ProjectAegis.Sim/ProjectAegis.Sim.csproj",
        "src/ProjectAegis.Delegation/ProjectAegis.Delegation.csproj",
        "src/ProjectAegis.Ai.Contracts/ProjectAegis.Ai.Contracts.csproj",
        "src/ProjectAegis.Ai.Host/ProjectAegis.Ai.Host.csproj",
        "src/ProjectAegis.Data.Excel/ProjectAegis.Data.Excel.csproj",
        "src/ProjectAegis.Delegation.UnityAdapter/ProjectAegis.Delegation.UnityAdapter.csproj",
    };

    private static readonly string[] ForbiddenAiPackageSubstrings =
    {
        "Microsoft.Extensions.AI",
        "Microsoft.Extensions.VectorData",
        "ModelContextProtocol",
        "Microsoft.ML.OnnxRuntime",
        "OpenAI",
        "Azure.AI.OpenAI",
        "Anthropic",
        "Google.GenAI",
        "Google.Cloud.AIPlatform",
    };

    private static readonly string[] UnityBoundNetstandardProjects =
    {
        "src/ProjectAegis.Data/ProjectAegis.Data.csproj",
        "src/ProjectAegis.Sim/ProjectAegis.Sim.csproj",
        "src/ProjectAegis.Delegation/ProjectAegis.Delegation.csproj",
        "src/ProjectAegis.Delegation.UnityAdapter/ProjectAegis.Delegation.UnityAdapter.csproj",
        "src/ProjectAegis.Ai.Contracts/ProjectAegis.Ai.Contracts.csproj",
    };

    public static TheoryData<string> PinnedProjectPaths()
    {
        var data = new TheoryData<string>();
        foreach (var path in AllowedProjectReferenceBasenames.Keys.OrderBy(p => p, StringComparer.Ordinal))
        {
            data.Add(path);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(PinnedProjectPaths))]
    public void Project_reference_set_matches_adr_pin(string relativePath)
    {
        var path = ResolveRepoFile(relativePath.Split('/'));
        Assert.True(path != null, $"Missing '{relativePath}'.");

        var actual = LoadProjectReferenceBasenames(path!)
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToArray();
        var expected = AllowedProjectReferenceBasenames[relativePath]
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            actual.SequenceEqual(expected, StringComparer.Ordinal),
            $"'{relativePath}' ProjectReference set drifted from ADR-001/006/010/030 pin.\n" +
            $"Expected: [{string.Join(", ", expected)}]\n" +
            $"Actual:   [{string.Join(", ", actual)}]");
    }

    [Theory]
    [MemberData(nameof(PinnedProjectPaths))]
    public void Headless_csproj_has_no_unityengine_package_or_reference(string relativePath)
    {
        if (!HeadlessNoUnityEngineProjects.Contains(relativePath, StringComparer.Ordinal))
        {
            return;
        }

        var path = ResolveRepoFile(relativePath.Split('/'));
        Assert.True(path != null, $"Missing '{relativePath}'.");

        var xml = XDocument.Load(path!);
        var packageHits = xml.Descendants("PackageReference")
            .Select(el => el.Attribute("Include")?.Value ?? string.Empty)
            .Where(v => v.Contains("UnityEngine", StringComparison.OrdinalIgnoreCase)
                        || v.Contains("UnityEditor", StringComparison.OrdinalIgnoreCase))
            .ToList();
        var projectHits = xml.Descendants("ProjectReference")
            .Select(el => el.Attribute("Include")?.Value ?? string.Empty)
            .Where(v => v.Contains("UnityEngine", StringComparison.OrdinalIgnoreCase)
                        || v.Contains("UnityEditor", StringComparison.OrdinalIgnoreCase))
            .ToList();
        var refHits = xml.Descendants("Reference")
            .Select(el => el.Attribute("Include")?.Value ?? string.Empty)
            .Where(v => v.Contains("UnityEngine", StringComparison.OrdinalIgnoreCase)
                        || v.Contains("UnityEditor", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.True(
            packageHits.Count == 0 && projectHits.Count == 0 && refHits.Count == 0,
            $"'{relativePath}' must not reference UnityEngine/UnityEditor (ADR-010). " +
            $"Hits: {string.Join(", ", packageHits.Concat(projectHits).Concat(refHits))}");
    }

    [Fact]
    public void Ai_contracts_has_zero_package_references()
    {
        var path = ResolveRepoFile("src", "ProjectAegis.Ai.Contracts", "ProjectAegis.Ai.Contracts.csproj");
        Assert.True(path != null, "Missing Ai.Contracts csproj.");
        var includes = LoadPackageReferenceIncludes(path!);
        Assert.True(
            includes.Count == 0,
            $"Ai.Contracts must have zero PackageReference (ADR-030). Found: {string.Join(", ", includes)}");
    }

    [Theory]
    [InlineData("src/ProjectAegis.Data/ProjectAegis.Data.csproj")]
    [InlineData("src/ProjectAegis.Sim/ProjectAegis.Sim.csproj")]
    [InlineData("src/ProjectAegis.Delegation/ProjectAegis.Delegation.csproj")]
    [InlineData("src/ProjectAegis.Delegation.UnityAdapter/ProjectAegis.Delegation.UnityAdapter.csproj")]
    [InlineData("src/ProjectAegis.Ai.Contracts/ProjectAegis.Ai.Contracts.csproj")]
    public void Unity_bound_or_contracts_csproj_has_no_ai_provider_package(string relativePath)
    {
        Assert.Contains(relativePath, UnityBoundNetstandardProjects);
        var path = ResolveRepoFile(relativePath.Split('/'));
        Assert.True(path != null, $"Missing '{relativePath}'.");
        var includes = LoadPackageReferenceIncludes(path!);
        foreach (var forbidden in ForbiddenAiPackageSubstrings)
        {
            var offending = includes
                .Where(i => i.Contains(forbidden, StringComparison.OrdinalIgnoreCase))
                .ToList();
            Assert.True(
                offending.Count == 0,
                $"'{relativePath}' references AI/provider package matching '{forbidden}': {string.Join(", ", offending)} (ADR-030).");
        }
    }

    [Fact]
    public void Data_has_no_project_references()
    {
        var path = ResolveRepoFile("src", "ProjectAegis.Data", "ProjectAegis.Data.csproj");
        Assert.True(path != null);
        Assert.Empty(LoadProjectReferenceBasenames(path!));
    }

    private static List<string> LoadProjectReferenceBasenames(string csprojPath)
    {
        var xml = XDocument.Load(csprojPath);
        return xml
            .Descendants("ProjectReference")
            .Select(el => el.Attribute("Include")?.Value ?? string.Empty)
            .Where(v => v.Length > 0)
            .Select(v => Path.GetFileName(v.Replace('\\', '/')))
            .Where(v => v.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(v => v, StringComparer.Ordinal)
            .ToList();
    }

    private static List<string> LoadPackageReferenceIncludes(string csprojPath)
    {
        var xml = XDocument.Load(csprojPath);
        return xml
            .Descendants("PackageReference")
            .Select(el => el.Attribute("Include")?.Value ?? string.Empty)
            .Where(v => v.Length > 0)
            .ToList();
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
