namespace ProjectAegis.MissionEditor.Cli;

using ProjectAegis.Data.Scenario.Authoring;

/// <summary>
/// S83-02 Undo CLI wiring (AME-8.5).
/// Cites: production/sprints/sprint-83-export-undo-ferry.md, production/agentic/sprint-83-parallel-kickoff-2026-07-04.md,
/// qa-plan-scenario-editor-2026-07-01.md unit #14, roadmap-execute-plan-07042026.md §4, scenario-editor-scope-boundary-2026-07-04.md,
/// implementation-tracker-2026-07-04.md (track D), AGENTS.md (GitNexus pre, editor subset).
/// Persistence: disk (see ScenarioUndoStackStore).
/// </summary>
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