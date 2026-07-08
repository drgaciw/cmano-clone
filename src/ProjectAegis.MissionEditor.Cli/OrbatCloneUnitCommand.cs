namespace ProjectAegis.MissionEditor.Cli;

using ProjectAegis.Data.Scenario.Authoring;

/// <summary>
/// Headless MCP/CLI verb <c>orbat_clone_unit</c> — clone an ORBAT unit under a new id.
/// </summary>
public static class OrbatCloneUnitCommand
{
    /// <summary>
    /// Loads the scenario, clones the unit, and persists with edit-version + undo semantics.
    /// </summary>
    public static int Run(
        string scenarioPath,
        int editVersion,
        string sourceUnitId,
        string newUnitId,
        double lat,
        double lon,
        TextWriter output)
    {
        if (!File.Exists(scenarioPath))
        {
            return McpToolResult.WriteError(output, "NOT_FOUND", $"Scenario not found: {scenarioPath}");
        }

        if (string.IsNullOrWhiteSpace(sourceUnitId))
        {
            return McpToolResult.WriteError(output, "INVALID_SOURCE", "A --source is required.");
        }

        if (string.IsNullOrWhiteSpace(newUnitId))
        {
            return McpToolResult.WriteError(output, "INVALID_UNIT", "A --id is required for the clone.");
        }

        try
        {
            var editor = ScenarioDocumentEditor.Load(scenarioPath);
            editor.RequireEditVersion(editVersion, scenarioPath);
            var undoSnapshot = editor.CaptureUndoSnapshot();
            editor.CloneOrbatUnit(sourceUnitId, newUnitId, lat, lon);
            editor.PersistUndoSnapshot(scenarioPath, undoSnapshot);
            editor.CommitMutation();
            editor.Save(scenarioPath);

            return McpToolResult.WriteOk(output, new
            {
                ok = true,
                sourceUnitId,
                unitId = newUnitId,
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
            return McpToolResult.WriteError(output, "CLONE_FAILED", ex.Message);
        }
    }
}
