namespace ProjectAegis.MissionEditor.Cli;

using ProjectAegis.Data.Scenario.Authoring;

/// <summary>
/// Headless MCP/CLI verb <c>scenario_diff_summary</c> — read-only AME-7.3 semantic diff
/// between two scenario JSON documents (ME-W3 Track W3-c).
/// </summary>
public static class ScenarioDiffSummaryCommand
{
    /// <summary>
    /// Loads <paramref name="beforePath"/> and <paramref name="afterPath"/>, runs
    /// <see cref="ScenarioSemanticDiff.Summarize"/>, and writes <c>{ ok, summary }</c>.
    /// </summary>
    /// <param name="beforePath">Baseline scenario JSON path.</param>
    /// <param name="afterPath">Comparison scenario JSON path.</param>
    /// <param name="output">Stdout/writer for JSON result.</param>
    /// <returns>0 on success; 1 when a path is missing or unloadable.</returns>
    public static int Run(string beforePath, string afterPath, TextWriter output)
    {
        if (string.IsNullOrWhiteSpace(beforePath) || string.IsNullOrWhiteSpace(afterPath))
        {
            return McpToolResult.WriteError(
                output,
                "INVALID_ARGS",
                "scenario_diff_summary requires --before <path> --after <path>");
        }

        if (!File.Exists(beforePath))
        {
            return McpToolResult.WriteError(output, "NOT_FOUND", $"Before scenario not found: {beforePath}");
        }

        if (!File.Exists(afterPath))
        {
            return McpToolResult.WriteError(output, "NOT_FOUND", $"After scenario not found: {afterPath}");
        }

        try
        {
            var before = ScenarioDocumentJsonLoader.LoadFromFile(beforePath);
            var after = ScenarioDocumentJsonLoader.LoadFromFile(afterPath);
            var summary = ScenarioSemanticDiff.Summarize(before, after);
            return McpToolResult.WriteOk(output, new
            {
                ok = true,
                summary,
            });
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException or System.Text.Json.JsonException)
        {
            return McpToolResult.WriteError(output, "LOAD_FAILED", ex.Message);
        }
    }
}
