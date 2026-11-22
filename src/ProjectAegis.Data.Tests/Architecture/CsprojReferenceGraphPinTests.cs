namespace ProjectAegis.Data.Tests.Architecture;

using System.Xml.Linq;
using NUnit.Framework;

/// <summary>
/// Pins the headless csproj ProjectReference graph for ADR-001 / ADR-006 / ADR-010 / ADR-030
/// (DRG-771 Lane C / CMA-30). Source/manifest scan only — no production edits.
/// </summary>
public sealed class CsprojReferenceGraphPinTests
{
    /// <summary>
    /// Exact ProjectReference Include file names (basename) allowed per product csproj.
    /// DRG-772 exited the temporary MissionEditor.Cli → Ai.Host waiver: CLI uses Ai.Contracts
    /// over a process boundary to a separate ProjectAegis.Ai.Host executable.
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
                    "ProjectAegis.Ai.Contracts.csproj",
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

    public static IEnumerable<string> PinnedProjectPaths() =>
        AllowedProjectReferenceBasenames.Keys.OrderBy(p => p, StringComparer.Ordinal);
    [TestCaseSource(nameof(PinnedProjectPaths))]
    public void Project_reference_set_matches_adr_pin(string relativePath)
    {
        var path = ResolveRepoFile(relativePath.Split('/'));
        if (path is null)
        {
            Assert.Fail($"Missing '{relativePath}'.");
            return;
        }

        var actual = LoadProjectReferenceBasenames(path)
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToArray();
        var expected = AllowedProjectReferenceBasenames[relativePath]
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToArray();

        Assert.That(            actual.SequenceEqual(expected, StringComparer.Ordinal), Is.True,             $"'{relativePath}' ProjectReference set drifted from ADR-001/006/010/030 pin.\n" +
            $"Expected: [{string.Join(", ", expected)}]\n" +
            $"Actual:   [{string.Join(", ", actual)}]");
    }
    [TestCaseSource(nameof(PinnedProjectPaths))]
    public void Headless_csproj_has_no_unityengine_package_or_reference(string relativePath)
    {
        if (!HeadlessNoUnityEngineProjects.Contains(relativePath, StringComparer.Ordinal))
        {
            return;
        }

        var path = ResolveRepoFile(relativePath.Split('/'));
        if (path is null)
        {
            Assert.Fail($"Missing '{relativePath}'.");
            return;
        }

        var xml = XDocument.Load(path);
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

        Assert.That(            packageHits.Count == 0 && projectHits.Count == 0 && refHits.Count == 0, Is.True,             $"'{relativePath}' must not reference UnityEngine/UnityEditor (ADR-010). " +
            $"Hits: {string.Join(", ", packageHits.Concat(projectHits).Concat(refHits))}");
    }

    [Test]
    public void Ai_contracts_has_zero_package_references()
    {
        var path = ResolveRepoFile("src", "ProjectAegis.Ai.Contracts", "ProjectAegis.Ai.Contracts.csproj");
        if (path is null)
        {
            Assert.Fail("Missing Ai.Contracts csproj.");
            return;
        }

        var includes = LoadPackageReferenceIncludes(path);
        Assert.That(            includes.Count == 0, Is.True,             $"Ai.Contracts must have zero PackageReference (ADR-030). Found: {string.Join(", ", includes)}");
    }
    [TestCase("src/ProjectAegis.Data/ProjectAegis.Data.csproj")]
    [TestCase("src/ProjectAegis.Sim/ProjectAegis.Sim.csproj")]
    [TestCase("src/ProjectAegis.Delegation/ProjectAegis.Delegation.csproj")]
    [TestCase("src/ProjectAegis.Delegation.UnityAdapter/ProjectAegis.Delegation.UnityAdapter.csproj")]
    [TestCase("src/ProjectAegis.Ai.Contracts/ProjectAegis.Ai.Contracts.csproj")]
    public void Unity_bound_or_contracts_csproj_has_no_ai_provider_package(string relativePath)
    {
        Assert.That(UnityBoundNetstandardProjects, Does.Contain(relativePath));
        var path = ResolveRepoFile(relativePath.Split('/'));
        if (path is null)
        {
            Assert.Fail($"Missing '{relativePath}'.");
            return;
        }

        var includes = LoadPackageReferenceIncludes(path);
        foreach (var forbidden in ForbiddenAiPackageSubstrings)
        {
            var offending = includes
                .Where(i => i.Contains(forbidden, StringComparison.OrdinalIgnoreCase))
                .ToList();
            Assert.That(                offending.Count == 0, Is.True,                 $"'{relativePath}' references AI/provider package matching '{forbidden}': {string.Join(", ", offending)} (ADR-030).");
        }
    }

    [Test]
    public void Data_has_no_project_references()
    {
        var path = ResolveRepoFile("src", "ProjectAegis.Data", "ProjectAegis.Data.csproj");
        if (path is null)
        {
            Assert.Fail("Missing ProjectAegis.Data.csproj.");
            return;
        }

        Assert.That(LoadProjectReferenceBasenames(path), Is.Empty);
    }

    [Test]
    public void MissionEditor_cli_does_not_reference_ai_host_or_ai_provider_packages()
    {
        var path = ResolveRepoFile("src", "ProjectAegis.MissionEditor.Cli", "ProjectAegis.MissionEditor.Cli.csproj");
        if (path is null)
        {
            Assert.Fail("Missing ProjectAegis.MissionEditor.Cli.csproj.");
            return;
        }

        var projectRefs = LoadProjectReferenceBasenames(path);
        Assert.That(
            projectRefs,
            Does.Not.Contain("ProjectAegis.Ai.Host.csproj"),
            "DRG-772 / ADR-030: ProjectAegis.MissionEditor.Cli must not hold an in-process ProjectReference to ProjectAegis.Ai.Host.csproj.");

        var packageRefs = LoadPackageReferenceIncludes(path);
        foreach (var forbidden in ForbiddenAiPackageSubstrings)
        {
            var offending = packageRefs
                .Where(i => i.Contains(forbidden, StringComparison.OrdinalIgnoreCase))
                .ToList();
            Assert.That(
                offending.Count == 0,
                Is.True,
                $"ProjectAegis.MissionEditor.Cli.csproj references forbidden AI/provider package matching '{forbidden}': {string.Join(", ", offending)} (ADR-030 / DRG-772).");
        }
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
