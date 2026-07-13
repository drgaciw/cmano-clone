namespace ProjectAegis.MissionEditor.Cli;

using ProjectAegis.Data.Scenario.Authoring;

/// <summary>
/// Headless MCP/CLI verb <c>mission_clone</c> — deep-copy a mission under a new id.
/// </summary>
public static class MissionCloneCommand
{
    /// <summary>
    /// Loads the scenario, clones the source mission, and persists with edit-version + undo semantics.
    /// </summary>
    /// <param name="scenarioPath">Path to the scenario JSON file.</param>
    /// <param name="editVersion">Expected current edit version (optimistic concurrency).</param>
    /// <param name="sourceMissionId">Id of the mission to clone.</param>
    /// <param name="newMissionId">Id for the cloned mission.</param>
    /// <param name="output">Stdout/writer for JSON result.</param>
    public static int Run(
        string scenarioPath,
        int editVersion,
        string sourceMissionId,
        string newMissionId,
        TextWriter output)
    {
        if (!File.Exists(scenarioPath))
        {
            return McpToolResult.WriteError(output, "NOT_FOUND", $"Scenario not found: {scenarioPath}");
        }

        if (string.IsNullOrWhiteSpace(sourceMissionId))
        {
            return McpToolResult.WriteError(output, "INVALID_SOURCE", "A --source is required.");
        }

        if (string.IsNullOrWhiteSpace(newMissionId))
        {
            return McpToolResult.WriteError(output, "INVALID_MISSION", "A --id is required for the clone.");
        }

        try
        {
            var editor = ScenarioDocumentEditor.Load(scenarioPath);
            editor.RequireEditVersion(editVersion, scenarioPath);
            var undoSnapshot = editor.CaptureUndoSnapshot();
            editor.CloneMission(sourceMissionId, newMissionId);
            editor.PersistUndoSnapshot(scenarioPath, undoSnapshot);
            editor.CommitMutation();
            editor.Save(scenarioPath);

            return McpToolResult.WriteOk(output, new
            {
                ok = true,
                sourceMissionId,
                missionId = newMissionId,
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
