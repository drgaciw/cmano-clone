namespace ProjectAegis.MissionEditor.Cli;

using System.Text.Json;
using ProjectAegis.Data.Scenario.Authoring;
using System.IO;

/// <summary>
/// scenario_ai_scaffold : "NL brief to draft scenario scaffold" producing draft sides/missions/objectives with provenance tags.
/// Supports --brief "text" [--out path] [--db-ref]
/// </summary>
public static class ScenarioAiScaffoldCommand
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    public static int Run(string brief, string? outPath, string? dbRef, TextWriter output)
    {
        if (string.IsNullOrWhiteSpace(brief))
        {
            return McpToolResult.WriteError(output, "MISSING_BRIEF", "scenario_ai_scaffold requires --brief \"NL text\"");
        }

        var result = AiAuthoringServices.NlScaffold(brief, dbRef);

        // Attach provenance tags to draft if needed (recorded in result)
        // Optionally save draft (uses existing public editor API only)
        if (!string.IsNullOrWhiteSpace(outPath))
        {
            if (File.Exists(outPath))
            {
                return McpToolResult.WriteError(output, "FILE_EXISTS", outPath);
            }
            // Recreate via public CreateNew + Add* to populate draft from NlScaffold result. No editor internals edited.
            var newEd = ScenarioDocumentEditor.CreateNew(dbRef ?? "baltic_patrol", 42, "ai-scaffold");
            foreach (var m in result.DraftDocument.Missions)
            {
                if (m.Type.Equals("Patrol", StringComparison.OrdinalIgnoreCase))
                    newEd.AddPatrolMission(m.Id, m.AssignedUnitIds, m.PatrolZone);
                else if (m.Type.Equals("Strike", StringComparison.OrdinalIgnoreCase))
                    newEd.AddStrikeMission(m.Id, m.AssignedUnitIds, m.TargetIds);
            }
            newEd.Save(outPath);
        }

        var payload = new
        {
            ok = true,
            brief,
            sides = result.Sides,
            missions = result.Missions,
            objectives = result.Objectives,
            provenanceTags = result.ProvenanceTags,
            explanation = result.Explanation,
            draftSaved = outPath,
            draftMissions = result.DraftDocument.Missions.Select(mm => new { mm.Id, mm.Type }).ToArray()
        };

        output.WriteLine(JsonSerializer.Serialize(payload, JsonOpts));
        return 0;
    }
}
