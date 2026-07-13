namespace ProjectAegis.MissionEditor.Cli;

using ProjectAegis.Data.Scenario.Authoring;

/// <summary>
/// Headless MCP/CLI verb <c>orbat_upsert_unit</c> — place or replace an ORBAT unit.
/// </summary>
public static class OrbatUpsertUnitCommand
{
    /// <summary>
    /// Loads the scenario, upserts the unit, and persists with edit-version + undo semantics.
    /// </summary>
    public static int Run(
        string scenarioPath,
        int editVersion,
        string unitId,
        string sideId,
        string platformId,
        double lat,
        double lon,
        TextWriter output)
    {
        if (!File.Exists(scenarioPath))
        {
            return McpToolResult.WriteError(output, "NOT_FOUND", $"Scenario not found: {scenarioPath}");
        }

        if (string.IsNullOrWhiteSpace(unitId))
        {
            return McpToolResult.WriteError(output, "INVALID_UNIT", "A --id is required.");
        }

        if (string.IsNullOrWhiteSpace(sideId))
        {
            return McpToolResult.WriteError(output, "INVALID_SIDE", "A --side is required.");
        }

        if (string.IsNullOrWhiteSpace(platformId))
        {
            return McpToolResult.WriteError(output, "INVALID_PLATFORM", "A --platform is required.");
        }

        try
        {
            var editor = ScenarioDocumentEditor.Load(scenarioPath);
            editor.RequireEditVersion(editVersion, scenarioPath);
            var undoSnapshot = editor.CaptureUndoSnapshot();
            editor.UpsertOrbatUnit(new ScenarioOrbatUnitDto
            {
                Id = unitId,
                SideId = sideId,
                PlatformId = platformId,
                Lat = lat,
                Lon = lon,
            });
            editor.PersistUndoSnapshot(scenarioPath, undoSnapshot);
            editor.CommitMutation();
            editor.Save(scenarioPath);

            return McpToolResult.WriteOk(output, new
            {
                ok = true,
                unitId,
                sideId,
                platformId,
                lat,
                lon,
                editVersion = editor.Metadata.EditVersion,
                fileHash = editor.ComputeFileHash(),
            });
        }
        catch (ScenarioEditConflictException ex)
        {
            return McpToolResult.WriteError(
                output,
                ex.Code,
                ex.Message,
                new { currentEditVersion = ex.CurrentEditVersion, fileHash = ex.FileHash });
        }
        catch (InvalidOperationException ex)
        {
            return McpToolResult.WriteError(output, "INVALID_UNIT", ex.Message);
        }
    }
}
