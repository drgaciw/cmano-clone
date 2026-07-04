namespace ProjectAegis.MissionEditor.Cli;

using ProjectAegis.Data.Scenario.Authoring;

public static class MissionAddSupportCommand
{
    private static readonly string[] AllowedRoles = ["Tanker", "AEW", "EW"];

    public static int Run(
        string scenarioPath,
        int editVersion,
        string missionId,
        IReadOnlyList<string> unitIds,
        string supportRole,
        IReadOnlyList<ScenarioWaypointDto> stationZone,
        TextWriter output)
    {
        if (!File.Exists(scenarioPath))
        {
            return McpToolResult.WriteError(output, "NOT_FOUND", $"Scenario not found: {scenarioPath}");
        }

        if (stationZone.Count < 3)
        {
            return McpToolResult.WriteError(output, "INVALID_ZONE", "Support station zone requires at least 3 waypoints.");
        }

        if (unitIds.Count == 0)
        {
            return McpToolResult.WriteError(output, "NO_UNITS", "At least one --unit is required.");
        }

        if (string.IsNullOrWhiteSpace(supportRole) ||
            !AllowedRoles.Contains(supportRole, StringComparer.OrdinalIgnoreCase))
        {
            return McpToolResult.WriteError(
                output,
                "INVALID_ROLE",
                "Support role must be one of: Tanker, AEW, EW.");
        }

        try
        {
            var editor = ScenarioDocumentEditor.Load(scenarioPath);
            editor.RequireEditVersion(editVersion, scenarioPath);
            editor.PushUndoSnapshot(scenarioPath);
            editor.AddSupportMission(missionId, unitIds, supportRole, stationZone);
            editor.CommitMutation();
            editor.Save(scenarioPath);

            return McpToolResult.WriteOk(output, new
            {
                ok = true,
                missionId,
                type = "Support",
                supportRole,
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