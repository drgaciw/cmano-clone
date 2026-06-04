namespace ProjectAegis.MissionEditor.Cli;

using ProjectAegis.Data.Scenario.Authoring;

public static class MissionAddPatrolCommand
{
    public static int Run(
        string scenarioPath,
        int editVersion,
        string missionId,
        IReadOnlyList<string> unitIds,
        IReadOnlyList<ScenarioWaypointDto> zone,
        TextWriter output)
    {
        if (!File.Exists(scenarioPath))
        {
            return McpToolResult.WriteError(output, "NOT_FOUND", $"Scenario not found: {scenarioPath}");
        }

        if (zone.Count < 3)
        {
            return McpToolResult.WriteError(output, "INVALID_ZONE", "Patrol zone requires at least 3 waypoints.");
        }

        if (unitIds.Count == 0)
        {
            return McpToolResult.WriteError(output, "NO_UNITS", "At least one --unit is required.");
        }

        try
        {
            var editor = ScenarioDocumentEditor.Load(scenarioPath);
            editor.RequireEditVersion(editVersion, scenarioPath);
            editor.AddPatrolMission(missionId, unitIds, zone);
            editor.CommitMutation();
            editor.Save(scenarioPath);

            return McpToolResult.WriteOk(output, new
            {
                ok = true,
                missionId,
                type = "Patrol",
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