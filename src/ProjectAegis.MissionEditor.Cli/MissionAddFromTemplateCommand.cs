namespace ProjectAegis.MissionEditor.Cli;

using ProjectAegis.Data.Scenario.Authoring;

/// <summary>
/// Headless MCP/CLI verb <c>mission_add_from_template</c> — create a mission from a built-in template.
/// </summary>
public static class MissionAddFromTemplateCommand
{
    /// <summary>
    /// Loads the scenario, materializes a built-in template as a new mission, and persists with
    /// edit-version + undo semantics.
    /// </summary>
    /// <param name="scenarioPath">Path to the scenario JSON file.</param>
    /// <param name="editVersion">Expected current edit version (optimistic concurrency).</param>
    /// <param name="templateId">Built-in template id (e.g. <c>tpl-patrol-empty</c>).</param>
    /// <param name="newMissionId">Id for the new mission.</param>
    /// <param name="output">Stdout/writer for JSON result.</param>
    public static int Run(
        string scenarioPath,
        int editVersion,
        string templateId,
        string newMissionId,
        TextWriter output)
    {
        if (!File.Exists(scenarioPath))
        {
            return McpToolResult.WriteError(output, "NOT_FOUND", $"Scenario not found: {scenarioPath}");
        }

        if (string.IsNullOrWhiteSpace(templateId))
        {
            return McpToolResult.WriteError(output, "INVALID_TEMPLATE", "A --template is required.");
        }

        if (string.IsNullOrWhiteSpace(newMissionId))
        {
            return McpToolResult.WriteError(output, "INVALID_MISSION", "A --id is required for the new mission.");
        }

        try
        {
            var editor = ScenarioDocumentEditor.Load(scenarioPath);
            editor.RequireEditVersion(editVersion, scenarioPath);
            var undoSnapshot = editor.CaptureUndoSnapshot();
            editor.AddMissionFromTemplate(templateId, newMissionId);
            editor.PersistUndoSnapshot(scenarioPath, undoSnapshot);
            editor.CommitMutation();
            editor.Save(scenarioPath);

            return McpToolResult.WriteOk(output, new
            {
                ok = true,
                templateId,
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
            return McpToolResult.WriteError(output, "TEMPLATE_FAILED", ex.Message);
        }
    }
}
