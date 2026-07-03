namespace ProjectAegis.MissionEditor.Cli;

using ProjectAegis.Data.Scenario.Authoring;

public static class ScenarioUndoCommand
{
    public static int Run(string scenarioPath, int editVersion, TextWriter output)
    {
        if (!File.Exists(scenarioPath))
        {
            return McpToolResult.WriteError(output, "NOT_FOUND", $"Scenario not found: {scenarioPath}");
        }

        try
        {
            var editor = ScenarioDocumentEditor.Load(scenarioPath);
            editor.RequireEditVersion(editVersion, scenarioPath);
            if (!editor.PopUndo(scenarioPath))
            {
                return McpToolResult.WriteError(output, "NO_UNDO_SNAPSHOT", "No undo snapshot is available for this scenario.");
            }

            return McpToolResult.WriteOk(output, new
            {
                ok = true,
                undone = true,
                editVersion = editor.Metadata.EditVersion,
                fileHash = editor.ComputeFileHash(),
                remainingUndoDepth = ScenarioUndoStackStore.Count(scenarioPath),
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
    }
}