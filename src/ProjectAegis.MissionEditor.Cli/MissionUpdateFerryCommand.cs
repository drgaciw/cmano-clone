namespace ProjectAegis.MissionEditor.Cli;

using ProjectAegis.Data.Scenario.Authoring;

public static class MissionUpdateFerryCommand
{
    public static int Run(
        string scenarioPath,
        int editVersion,
        string missionId,
        IReadOnlyList<string>? unitIds,
        string? ferryDestinationBaseId,
        TextWriter output)
    {
        if (!File.Exists(scenarioPath))
        {
            return McpToolResult.WriteError(output, "NOT_FOUND", $"Scenario not found: {scenarioPath}");
        }

        try
        {
            var editor = ScenarioDocumentEditor.Load(scenarioPath);
            editor.RequireEditVersion(editVersion, scenarioPath);
            editor.PushUndoSnapshot(scenarioPath);
            editor.UpdateFerryMission(
                missionId,
                unitIds?.Count > 0 ? unitIds : null,
                string.IsNullOrWhiteSpace(ferryDestinationBaseId) ? null : ferryDestinationBaseId);
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
            return McpToolResult.WriteError(output, "MISSION_NOT_FOUND", ex.Message);
        }
    }
}
