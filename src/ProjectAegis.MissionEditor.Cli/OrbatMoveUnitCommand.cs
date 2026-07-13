namespace ProjectAegis.MissionEditor.Cli;

using ProjectAegis.Data.Scenario.Authoring;

/// <summary>
/// Headless MCP/CLI verb <c>orbat_move_unit</c> — move an existing ORBAT unit.
/// </summary>
public static class OrbatMoveUnitCommand
{
    /// <summary>
    /// Loads the scenario, moves the unit, and persists with edit-version + undo semantics.
    /// </summary>
    public static int Run(
        string scenarioPath,
        int editVersion,
        string unitId,
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

        try
        {
            var editor = ScenarioDocumentEditor.Load(scenarioPath);
            editor.RequireEditVersion(editVersion, scenarioPath);
            var undoSnapshot = editor.CaptureUndoSnapshot();
            editor.MoveOrbatUnit(unitId, lat, lon);
            editor.PersistUndoSnapshot(scenarioPath, undoSnapshot);
            editor.CommitMutation();
            editor.Save(scenarioPath);

            return McpToolResult.WriteOk(output, new
            {
                ok = true,
                unitId,
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
            return McpToolResult.WriteError(output, "UNIT_NOT_FOUND", ex.Message);
        }
    }
}
