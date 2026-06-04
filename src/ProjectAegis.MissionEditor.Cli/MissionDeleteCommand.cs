namespace ProjectAegis.MissionEditor.Cli;

using ProjectAegis.Data.Scenario.Authoring;

public static class MissionDeleteCommand
{
    public static int Run(string scenarioPath, int editVersion, string missionId, TextWriter output)
    {
        if (!File.Exists(scenarioPath))
        {
            return McpToolResult.WriteError(output, "NOT_FOUND", $"Scenario not found: {scenarioPath}");
        }

        try
        {
            var editor = ScenarioDocumentEditor.Load(scenarioPath);
            editor.RequireEditVersion(editVersion, scenarioPath);
            if (!editor.TryRemoveMission(missionId))
            {
                return McpToolResult.WriteError(output, "MISSION_NOT_FOUND", $"Mission '{missionId}' was not found.");
            }

            editor.CommitMutation();
            editor.Save(scenarioPath);
            return McpToolResult.WriteOk(output, new
            {
                ok = true,
                deletedMissionId = missionId,
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
    }
}