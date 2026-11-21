namespace ProjectAegis.MissionEditor.Cli;

using System.Text.Json;
using Data.Scenario.Authoring;

/// <summary>
/// Shared guarded scenario load for CLI verbs: malformed or unreadable scenario files yield the
/// standard <c>LOAD_FAILED</c> error envelope (exit 1) instead of an unhandled exception (UAT CLI-1).
/// </summary>
internal static class ScenarioLoadGuard
{
    /// <summary>
    /// Loads a scenario. On failure writes the <c>LOAD_FAILED</c> envelope to <paramref name="output"/>
    /// and returns false with <paramref name="exitCode"/> set to 1.
    /// </summary>
    public static bool TryLoad(string scenarioPath, TextWriter output, out ScenarioDocumentDto scenario, out int exitCode)
    {
        try
        {
            scenario = ScenarioDocumentJsonLoader.LoadFromFile(scenarioPath);
            exitCode = 0;
            return true;
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException or JsonException or UnauthorizedAccessException)
        {
            scenario = null!;
            exitCode = McpToolResult.WriteError(output, "LOAD_FAILED", ex.Message);
            return false;
        }
    }
}
