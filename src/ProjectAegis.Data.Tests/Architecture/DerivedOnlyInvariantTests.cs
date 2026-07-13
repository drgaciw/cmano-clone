namespace ProjectAegis.Data.Tests.Architecture;

using Xunit;

/// <summary>
/// AC-9 (AME-2.4): <c>editorState</c> is derived-only and must never be read by
/// validation or headless sim entry points. This test is the EditorStateLint gate.
/// </summary>
public sealed class DerivedOnlyInvariantTests
{
    private static readonly string[] KnownValidationEntryFiles =
    {
        "src/ProjectAegis.Data/Validation/ScenarioValidationEngine.cs",
        "src/ProjectAegis.MissionEditor.Cli/ScenarioSimulateSampleCommand.cs",
        "src/ProjectAegis.MissionEditor.Cli/ScenarioValidateCommand.cs",
    };

    [Theory]
    [MemberData(nameof(KnownValidationEntryFileCases))]
    public void EditorStateLint_no_editorState_reads_in_validation_or_sim_entry_points(string relativePath)
    {
        var path = ResolveRepoFile(relativePath);
        Assert.True(path != null, $"Could not locate source file at repo-relative path '{relativePath}'.");

        var source = File.ReadAllText(path!);
        Assert.False(
            source.Contains("editorState", StringComparison.Ordinal),
            $"EditorStateLint violation: '{path}' references 'editorState'. " +
            "Per AME-2.4 / AC-9, validation and sim entry points must not read derived-only editor state.");
    }

    [Fact]
    public void EditorStateLint_has_no_violations_in_known_entry_files()
    {
        var violations = new List<string>();
        foreach (var relativePath in KnownValidationEntryFiles)
        {
            var path = ResolveRepoFile(relativePath);
            if (path == null)
            {
                violations.Add($"missing file: {relativePath}");
                continue;
            }

            if (File.ReadAllText(path).Contains("editorState", StringComparison.Ordinal))
            {
                violations.Add(relativePath);
            }
        }

        Assert.True(
            violations.Count == 0,
            $"EditorStateLint violations: {string.Join(", ", violations)}");
    }

    public static IEnumerable<object[]> KnownValidationEntryFileCases() =>
        KnownValidationEntryFiles.Select(p => new object[] { p });

    private static string? ResolveRepoFile(string relativePath)
    {
        var segments = relativePath.Split('/');
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 12; i++)
        {
            if (dir == null)
            {
                break;
            }

            var candidate = Path.Combine(new[] { dir.FullName }.Concat(segments).ToArray());
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        return null;
    }
}