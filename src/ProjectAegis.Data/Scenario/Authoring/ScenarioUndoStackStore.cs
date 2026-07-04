namespace ProjectAegis.Data.Scenario.Authoring;

using System.Text.Json;

/// <summary>
/// Persists committed-mutation undo snapshots alongside the canonical scenario file (AME-8.5).
/// Each CLI invocation loads/pushes/pops this stack so undo survives process boundaries.
/// 
/// S83-02 (AME-8.5): disk vs in-process note.
/// Design lock: disk-backed via JSON sidecar (ResolveStackPath = scenarioPath + ".undo-stack.json").
/// Confirmed by File IO in Load/Save/Push/TryPop (not pure in-memory on ScenarioDocumentEditor instance).
/// Enables cross-process CLI undo (e.g. separate `dotnet run` invocations). See qa-plan-scenario-editor-2026-07-01.md #14,
/// sprint-83-export-undo-ferry.md, roadmap-execute-plan-07042026.md §4, scenario-editor-scope-boundary-2026-07-04.md.
/// Round-trip tests must (and do) survive process boundaries via disk.
/// </summary>
public static class ScenarioUndoStackStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    public static void Push(string scenarioPath, ScenarioDocumentDto snapshot)
    {
        var stackPath = ResolveStackPath(scenarioPath);
        var stack = LoadStack(stackPath);
        stack.Add(CloneDocument(snapshot));
        SaveStack(stackPath, stack);
    }

    public static bool TryPop(string scenarioPath, out ScenarioDocumentDto? snapshot)
    {
        var stackPath = ResolveStackPath(scenarioPath);
        var stack = LoadStack(stackPath);
        if (stack.Count == 0)
        {
            snapshot = null;
            return false;
        }

        var index = stack.Count - 1;
        snapshot = CloneDocument(stack[index]);
        stack.RemoveAt(index);
        SaveStack(stackPath, stack);
        return true;
    }

    public static int Count(string scenarioPath) =>
        LoadStack(ResolveStackPath(scenarioPath)).Count;

    private static string ResolveStackPath(string scenarioPath) =>
        scenarioPath + ".undo-stack.json";

    private static List<ScenarioDocumentDto> LoadStack(string stackPath)
    {
        if (!File.Exists(stackPath))
        {
            return new List<ScenarioDocumentDto>();
        }

        var json = File.ReadAllText(stackPath);
        return JsonSerializer.Deserialize<List<ScenarioDocumentDto>>(json, Options)
            ?? new List<ScenarioDocumentDto>();
    }

    private static void SaveStack(string stackPath, List<ScenarioDocumentDto> stack)
    {
        var dir = Path.GetDirectoryName(stackPath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.WriteAllText(stackPath, JsonSerializer.Serialize(stack, Options));
    }

    private static ScenarioDocumentDto CloneDocument(ScenarioDocumentDto source) =>
        new()
        {
            Metadata = source.Metadata,
            Missions = source.Missions
                .Select(m => new ScenarioMissionDto
                {
                    Id = m.Id,
                    Type = m.Type,
                    AssignedUnitIds = m.AssignedUnitIds.ToArray(),
                    TargetIds = m.TargetIds.ToArray(),
                    FerryDestinationBaseId = m.FerryDestinationBaseId,
                    PatrolZone = m.PatrolZone
                        .Select(w => new ScenarioWaypointDto { Lat = w.Lat, Lon = w.Lon })
                        .ToArray(),
                    SupportRole = m.SupportRole,
                    RoeOverride = m.RoeOverride,
                })
                .ToArray(),
        };
}