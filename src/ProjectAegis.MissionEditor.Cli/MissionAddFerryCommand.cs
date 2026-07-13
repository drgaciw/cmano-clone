namespace ProjectAegis.MissionEditor.Cli;

using ProjectAegis.Data.Scenario.Authoring;

public static class MissionAddFerryCommand
{
    public static int Run(
        string scenarioPath,
        int editVersion,
        string missionId,
        IReadOnlyList<string> unitIds,
        string ferryDestinationBaseId,
        TextWriter output)
    {
        if (!File.Exists(scenarioPath))
        {
            return McpToolResult.WriteError(output, "NOT_FOUND", $"Scenario not found: {scenarioPath}");
        }

        if (unitIds.Count == 0)
        {
            return McpToolResult.WriteError(output, "NO_UNITS", "At least one --unit is required.");
        }

        if (string.IsNullOrWhiteSpace(ferryDestinationBaseId))
        {
            return McpToolResult.WriteError(output, "INVALID_DESTINATION", "A --destination is required.");
        }

        try
        {
            var editor = ScenarioDocumentEditor.Load(scenarioPath);
            editor.RequireEditVersion(editVersion, scenarioPath);
            var undoSnapshot = editor.CaptureUndoSnapshot();
            editor.AddFerryMission(missionId, unitIds, ferryDestinationBaseId);
            editor.PersistUndoSnapshot(scenarioPath, undoSnapshot);
            editor.CommitMutation();
            editor.Save(scenarioPath);

            return McpToolResult.WriteOk(output, new
            {
                ok = true,
                missionId,
                type = "Ferry",
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
            return McpToolResult.WriteError(output, "DUPLICATE_MISSION", ex.Message);
        }
    }
}
