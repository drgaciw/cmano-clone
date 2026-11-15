namespace ProjectAegis.Data.Tests.Architecture;

using System.Xml.Linq;
using Xunit;

/// <summary>
/// Wave 0 AI host-boundary gates (DRG-733 / ADR-030 Proposed).
/// New file only — does not edit <see cref="NoDynamicExecutionGateTests"/>.
/// </summary>
public sealed class AiHostBoundaryGateTests
{
    /// <summary>
    /// Unity-copied <c>netstandard2.1</c> assemblies published via
    /// <c>tools/copy-delegation-assemblies.sh</c>. AI packages on these
    /// projects would leak into <c>Assets/Plugins/ProjectAegis</c>.
    /// </summary>
    public static TheoryData<string> UnityBoundNetstandardProjectPaths()
    {
        var data = new TheoryData<string>();
        foreach (var path in UnityBoundNetstandardProjects)
        {
            data.Add(path);
        }

        return data;
    }

    /// <summary>
    /// Same authoring/validation scan list as <see cref="NoDynamicExecutionGateTests"/>,
    /// with .NET 11 process-helper patterns the original gate does not match.
    /// </summary>
    public static TheoryData<string> AuthoringPathSourcePaths()
    {
        var data = new TheoryData<string>();
        foreach (var path in AuthoringPathSources)
        {
            data.Add(path);
        }

        return data;
    }

    private static readonly string[] UnityBoundNetstandardProjects =
    {
        "src/ProjectAegis.Data/ProjectAegis.Data.csproj",
        "src/ProjectAegis.Sim/ProjectAegis.Sim.csproj",
        "src/ProjectAegis.Delegation/ProjectAegis.Delegation.csproj",
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

    private static readonly string[] ForbiddenAiDllNameSubstrings =
    {
        "Microsoft.Extensions.AI",
        "Microsoft.Extensions.VectorData",
        "ModelContextProtocol",
        "Microsoft.ML.OnnxRuntime",
        "OpenAI",
        "Azure.AI.OpenAI",
        "Anthropic",
    };

    private static readonly string[] AuthoringPathSources =
    {
        "src/ProjectAegis.Data/Validation/ScenarioValidationEngine.cs",
        "src/ProjectAegis.Data/Validation/Rules/ValidationRules.cs",
        "src/ProjectAegis.Data/Scenario/Authoring/ScenarioDocumentEditor.cs",
        "src/ProjectAegis.Data/Validation/ScenarioValidationExportGate.cs",
        "src/ProjectAegis.Data/Validation/ReachabilityCalculator.cs",
        "src/ProjectAegis.Data/Scenario/Authoring/AiAuthoringServices.cs",
        "src/ProjectAegis.Data/Scenario/Authoring/ScenarioEditVersionGuard.cs",
    };

    private static readonly string[] ForbiddenNet11ProcessPatterns =
    {
        "Process.Run",
        "RunAndCapture",
        "StartAndForget",
        "SafeProcessHandle.Start",
    };

    /// <summary>
    /// Network / LLM call markers forbidden on Sim and Delegation tick paths
    /// (ADR-030 Decision). Optional <c>Delegation/Hindsight/</c> sidecar is excluded.
    /// </summary>
    private static readonly string[] ForbiddenNetworkSubstrings =
    {
        "System.Net.Http",
        "HttpClient",
        "System.Net.Sockets",
        "new Socket",
    };

    [Theory]
    [MemberData(nameof(UnityBoundNetstandardProjectPaths))]
    public void Unity_bound_netstandard_csproj_has_no_ai_or_provider_package(string relativePath)
    {
        var path = ResolveRepoFile(relativePath.Split('/'));
        if (path is null)
        {
            Assert.Fail($"Missing project '{relativePath}'.");
            return;
        }

        var includes = LoadPackageReferenceIncludes(path);
        foreach (var forbidden in ForbiddenAiPackageSubstrings)
        {
            var offending = includes
                .Where(i => i.Contains(forbidden, StringComparison.OrdinalIgnoreCase))
                .ToList();
            Assert.True(
                offending.Count == 0,
                $"'{path}' references AI/provider package matching '{forbidden}': {string.Join(", ", offending)}. " +
                "Per ADR-030 Proposed, AI packages stay off Unity-bound netstandard2.1 projects.");
        }
    }

    [Fact]
    public void Ai_contracts_has_zero_package_references_when_present()
    {
        var path = ResolveRepoFile("src", "ProjectAegis.Ai.Contracts", "ProjectAegis.Ai.Contracts.csproj");
        if (path is null)
        {
            // Contracts land via DRG-730; gate arms when the project exists.
            return;
        }

        var includes = LoadPackageReferenceIncludes(path);
        Assert.True(
            includes.Count == 0,
            $"'{path}' must have zero PackageReference entries (found: {string.Join(", ", includes)}).");
    }

    [Fact]
    public void Unity_plugins_projectaegis_has_no_ai_provider_dll()
    {
        var pluginsDir = ResolveRepoDirectory(
            "unity", "ProjectAegis", "Assets", "Plugins", "ProjectAegis");
        if (pluginsDir is null)
        {
            // Directory may be absent in headless clones; skip rather than false-fail.
            return;
        }

        var dlls = Directory.EnumerateFiles(pluginsDir, "*.dll", SearchOption.TopDirectoryOnly);
        foreach (var dll in dlls)
        {
            var name = Path.GetFileNameWithoutExtension(dll);
            foreach (var forbidden in ForbiddenAiDllNameSubstrings)
            {
                Assert.True(
                    !name.Contains(forbidden, StringComparison.OrdinalIgnoreCase),
                    $"'{dll}' matches forbidden AI/provider DLL substring '{forbidden}'. " +
                    "AI assemblies must not ship into Unity Plugins via copy-delegation-assemblies.");
            }
        }
    }

    [Theory]
    [MemberData(nameof(AuthoringPathSourcePaths))]
    public void Authoring_path_has_no_net11_process_helpers(string relativePath)
    {
        var path = ResolveRepoFile(relativePath.Split('/'));
        if (path is null)
        {
            Assert.Fail($"Missing source '{relativePath}'.");
            return;
        }

        var source = File.ReadAllText(path);
        foreach (var forbidden in ForbiddenNet11ProcessPatterns)
        {
            Assert.True(
                !source.Contains(forbidden, StringComparison.Ordinal),
                $"'{path}' contains forbidden process-helper substring '{forbidden}'. " +
                "Authoring/validation path must not shell out via .NET 11 Process.Run* helpers (ADR-014 / ADR-030).");
        }
    }

    /// <summary>
    /// Detector proof for AC2: synthetic violations are rejected by the same matchers.
    /// </summary>
    [Theory]
    [InlineData("Microsoft.Extensions.AI", true)]
    [InlineData("System.Text.Json", false)]
    [InlineData("OpenAI", true)]
    [InlineData("ClosedXML", false)]
    public void Forbidden_ai_package_matcher_detects_violations(string packageInclude, bool expectForbidden)
    {
        var hit = ForbiddenAiPackageSubstrings.Any(f =>
            packageInclude.Contains(f, StringComparison.OrdinalIgnoreCase));
        Assert.Equal(expectForbidden, hit);
    }

    [Theory]
    [InlineData("await Process.RunAsync(psi);", true)]
    [InlineData("Process.Start(psi);", false)] // covered by NoDynamicExecutionGateTests
    [InlineData("var text = Process.RunAndCaptureTextAsync(psi);", true)]
    [InlineData("StartAndForget(psi);", true)]
    [InlineData("SafeProcessHandle.Start(psi);", true)]
    [InlineData("File.ReadAllText(path);", false)]
    public void Forbidden_process_helper_matcher_detects_violations(string sourceSnippet, bool expectForbidden)
    {
        var hit = ForbiddenNet11ProcessPatterns.Any(f =>
            sourceSnippet.Contains(f, StringComparison.Ordinal));
        Assert.Equal(expectForbidden, hit);
    }

    [Fact]
    public void Sim_and_delegation_tick_path_have_no_http_client_or_socket()
    {
        var simRoot = ResolveRepoDirectory("src", "ProjectAegis.Sim");
        if (simRoot is null)
        {
            Assert.Fail("Missing src/ProjectAegis.Sim.");
            return;
        }

        var delegationRoot = ResolveRepoDirectory("src", "ProjectAegis.Delegation");
        if (delegationRoot is null)
        {
            Assert.Fail("Missing src/ProjectAegis.Delegation.");
            return;
        }

        foreach (var file in EnumerateCsSources(simRoot))
        {
            AssertNoForbiddenNetwork(file);
        }

        foreach (var file in EnumerateCsSources(delegationRoot))
        {
            // Optional non-tick Hindsight memory sidecar may use HttpClient; not LLM / not tick.
            if (file.Contains($"{Path.DirectorySeparatorChar}Hindsight{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || file.Contains("/Hindsight/", StringComparison.Ordinal)
                || file.Contains("\\Hindsight\\", StringComparison.Ordinal))
            {
                continue;
            }

            AssertNoForbiddenNetwork(file);
        }
    }

    [Theory]
    [InlineData("var http = new HttpClient();", true)]
    [InlineData("using System.Net.Http;", true)]
    [InlineData("using System.Net.Sockets;", true)]
    [InlineData("var s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);", true)]
    [InlineData("File.ReadAllText(path);", false)]
    public void Forbidden_network_matcher_detects_violations(string sourceSnippet, bool expectForbidden)
    {
        var hit = ForbiddenNetworkSubstrings.Any(f =>
            sourceSnippet.Contains(f, StringComparison.Ordinal));
        Assert.Equal(expectForbidden, hit);
    }

    [Fact]
    public void Unity_bound_project_list_matches_copy_surface()
    {
        Assert.Equal(4, UnityBoundNetstandardProjects.Length);
        Assert.Equal(7, AuthoringPathSources.Length);
        Assert.Equal(4, UnityBoundNetstandardProjectPaths().Count<object[]>());
        Assert.Equal(7, AuthoringPathSourcePaths().Count<object[]>());
    }

    private static void AssertNoForbiddenNetwork(string path)
    {
        var source = File.ReadAllText(path);
        foreach (var forbidden in ForbiddenNetworkSubstrings)
        {
            Assert.True(
                !source.Contains(forbidden, StringComparison.Ordinal),
                $"'{path}' contains forbidden network/LLM-path substring '{forbidden}'. " +
                "Per ADR-030, Sim and Delegation tick paths make no network or LLM calls.");
        }
    }

    private static IEnumerable<string> EnumerateCsSources(string root) =>
        Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal));

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
