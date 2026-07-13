namespace ProjectAegis.MissionEditor.Cli;

using ProjectAegis.Data.Scenario.Authoring;

/// <summary>CLI: mission_update_support — patch Support mission fields (mirrors ferry update).</summary>
public static class MissionUpdateSupportCommand
{
    private static readonly string[] AllowedRoles = ["Tanker", "AEW", "EW"];

    public static int Run(
        string scenarioPath,
        int editVersion,
        string missionId,
        IReadOnlyList<string>? unitIds,
        string? supportRole,
        IReadOnlyList<ScenarioWaypointDto>? stationZone,
        TextWriter output)
    {
        if (!File.Exists(scenarioPath))
        {
            return McpToolResult.WriteError(output, "NOT_FOUND", $"Scenario not found: {scenarioPath}");
        }

        if (stationZone is { Count: > 0 and < 3 })
        {
            return McpToolResult.WriteError(output, "INVALID_ZONE", "Support station zone requires at least 3 waypoints.");
        }

        if (!string.IsNullOrWhiteSpace(supportRole) &&
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
            var undoSnapshot = editor.CaptureUndoSnapshot();
            editor.UpdateSupportMission(
                missionId,
                unitIds?.Count > 0 ? unitIds : null,
                string.IsNullOrWhiteSpace(supportRole) ? null : supportRole,
                stationZone is { Count: >= 3 } ? stationZone : null);
            editor.PersistUndoSnapshot(scenarioPath, undoSnapshot);
            editor.CommitMutation();
            editor.Save(scenarioPath);
            return McpToolResult.WriteOk(output, new
            {
                ok = true,
                missionId,
                type = "Support",
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
