namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Sim.Scenario;

/// <summary>Projects scenario mission timeline for C2 left drawer (deterministic ordering).</summary>
public static class MissionListProjection
{
    public static IReadOnlyList<MissionListEntry> Project(ScenarioMissionTimeline? timeline)
    {
        if (timeline == null || timeline.Events.Count == 0)
        {
            return Array.Empty<MissionListEntry>();
        }

        var orderIndex = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < timeline.FireOrder.Count; i++)
        {
            orderIndex[timeline.FireOrder[i]] = i;
        }

        return timeline.Events
            .OrderBy(e => e.FireAtTick)
            .ThenBy(e => orderIndex.TryGetValue(e.EventId, out var idx) ? idx : int.MaxValue)
            .ThenBy(e => e.EventId, StringComparer.Ordinal)
            .Select(e => new MissionListEntry(e.EventId, e.FireAtTick, e.Kind, e.Code))
            .ToArray();
    }
}