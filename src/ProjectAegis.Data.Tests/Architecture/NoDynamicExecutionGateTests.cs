namespace ProjectAegis.Data.Tests.Architecture;

using System.Xml.Linq;
using Xunit;

/// <summary>
/// Operationalizes ADR-014 ("no Lua in v1") and requirement doc 11's NFR
/// ("no arbitrary code execution in play mode") as an automated, checkable
/// gate instead of a stated-only promise.
///
/// This is a source/manifest-scanning gate, not a runtime-behavior test:
/// it asserts (1) no Lua-interpreter package is referenced by the headless
/// assemblies that sit on the scenario authoring/export path, and (2) the
/// validation/authoring source files on that path do not reach for dynamic
/// code execution APIs (Reflection.Emit, Roslyn scripting, Process.Start,
/// eval-style constructs).
///
/// See docs/architecture/adr-014-lua-compatibility-scope.md and
/// Game-Requirements/requirements/11-Agentic-Mission-Editor.md.
/// </summary>
public sealed class NoDynamicExecutionGateTests
{
    /// <summary>
    /// Substrings that would indicate a Lua interpreter package reference.
    /// Matched case-insensitively against the "Include" attribute of each
    /// &lt;PackageReference&gt; element.
    /// </summary>
    private static readonly string[] ForbiddenPackageSubstrings =
    {
        "NLua",
        "MoonSharp",
        "KeraLua",
        "Lua",
    };

    /// <summary>
    /// API/type substrings that indicate reaching for dynamic code execution.
    /// Matched case-sensitively (these are real .NET/Roslyn identifiers, so a
    /// case-sensitive match avoids false positives from unrelated prose).
    /// Note: "eval(" below is a forbidden-pattern literal used to scan
    /// disallowed source text for this security gate — it is not itself a
    /// call to eval() and does not execute anything.
    /// </summary>
    private static readonly string[] ForbiddenSourceSubstrings =
    {
        "System.Reflection.Emit",
        "Roslyn",
        "CSharpScript",
        "Process.Start",
        "eval(",
    };

    [Theory]
    [InlineData("src", "ProjectAegis.Data", "ProjectAegis.Data.csproj")]
    [InlineData("src", "ProjectAegis.MissionEditor.Cli", "ProjectAegis.MissionEditor.Cli.csproj")]
    public void Csproj_has_no_lua_interpreter_package_reference(params string[] relativeSegments)
    {
        var path = ResolveRepoFile(relativeSegments);
        Assert.True(path != null, $"Could not locate project file at repo-relative path '{string.Join('/', relativeSegments)}' by walking up from {AppContext.BaseDirectory}.");

        var xml = XDocument.Load(path!);
        var packageReferenceIncludes = xml
            .Descendants("PackageReference")
            .Select(el => el.Attribute("Include")?.Value ?? string.Empty)
            .ToList();

        foreach (var forbidden in ForbiddenPackageSubstrings)
        {
            var offending = packageReferenceIncludes
                .Where(include => include.Contains(forbidden, StringComparison.OrdinalIgnoreCase))
                .ToList();

            Assert.True(
                offending.Count == 0,
                $"'{path}' references a package matching forbidden Lua-interpreter substring '{forbidden}': {string.Join(", ", offending)}. " +
                "Per ADR-014, v1 ships no Lua interpreter.");
        }
    }

    [Theory]
    [InlineData("src", "ProjectAegis.Data", "Validation", "ScenarioValidationEngine.cs")]
    [InlineData("src", "ProjectAegis.Data", "Validation", "Rules", "ValidationRules.cs")]
    [InlineData("src", "ProjectAegis.Data", "Scenario", "Authoring", "ScenarioDocumentEditor.cs")]
    public void Validation_and_authoring_source_has_no_dynamic_code_execution(params string[] relativeSegments)
    {
        var path = ResolveRepoFile(relativeSegments);
        Assert.True(path != null, $"Could not locate source file at repo-relative path '{string.Join('/', relativeSegments)}' by walking up from {AppContext.BaseDirectory}.");

        var source = File.ReadAllText(path!);

        foreach (var forbidden in ForbiddenSourceSubstrings)
        {
            Assert.True(
                !source.Contains(forbidden, StringComparison.Ordinal),
                $"'{path}' contains forbidden dynamic-code-execution substring '{forbidden}'. " +
                "Per ADR-014 / requirement doc 11 NFR, no arbitrary code execution is permitted on the " +
                "blocking validation/authoring/export path.");
        }
    }

    /// <summary>
    /// Walks upward from the test assembly's output directory looking for a
    /// file at the given repo-relative path, mirroring the idiom used by
    /// <c>ScenarioPackageTests.ResolveFixture</c>.
    /// </summary>
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
