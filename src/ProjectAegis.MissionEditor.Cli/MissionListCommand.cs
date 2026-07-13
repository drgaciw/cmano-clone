namespace ProjectAegis.MissionEditor.Cli;

using ProjectAegis.Data.Scenario.Authoring;

/// <summary>
/// Headless MCP/CLI verb <c>mission_list</c> — read-only Mission Board listing with optional filters.
/// </summary>
public static class MissionListCommand
{
    /// <summary>
    /// Loads the scenario document and returns filtered Mission Board rows as JSON.
    /// Does not require <c>--edit-version</c> (read-only).
    /// </summary>
    /// <param name="scenarioPath">Path to the scenario JSON file.</param>
    /// <param name="typeFilter">Optional mission type filter (Patrol|Strike|Ferry|Support).</param>
    /// <param name="sideFilter">Optional side id filter.</param>
    /// <param name="statusFilter">Optional status filter (Assigned|Unassigned).</param>
    /// <param name="output">Stdout/writer for JSON result.</param>
    public static int Run(
        string scenarioPath,
        string? typeFilter,
        string? sideFilter,
        string? statusFilter,
        TextWriter output)
    {
        if (!File.Exists(scenarioPath))
        {
            return McpToolResult.WriteError(output, "NOT_FOUND", $"Scenario not found: {scenarioPath}");
        }

        var document = ScenarioDocumentJsonLoader.LoadFromFile(scenarioPath);
        var rows = MissionBoardQuery.List(document, typeFilter, sideFilter, statusFilter);
        var missions = rows.Select(r => new
        {
            id = r.Id,
            type = r.Type,
            sideId = r.SideId,
            status = r.Status,
            unitCount = r.UnitCount,
            summaryLine = r.SummaryLine,
        }).ToArray();

        return McpToolResult.WriteOk(output, new
        {
            ok = true,
            missions,
        });
    }
}
