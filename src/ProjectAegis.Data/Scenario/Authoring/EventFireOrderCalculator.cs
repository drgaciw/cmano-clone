namespace ProjectAegis.Data.Scenario.Authoring;

/// <summary>Deterministic event fire order (GDD §4.2).</summary>
public static class EventFireOrderCalculator
{
    public static IReadOnlyList<string> ComputeFireOrder(IReadOnlyList<ScenarioEventDto> events)
    {
        return events
            .Select(e => new EventSortKey(
                e.Id,
                ResolveTriggerRank(e)))
            .OrderBy(k => k.TriggerTickResolved)
            .ThenBy(k => k.EventId, StringComparer.Ordinal)
            .Select(k => k.EventId)
            .ToArray();
    }

    private static int ResolveTriggerRank(ScenarioEventDto evt)
    {
        if (string.Equals(evt.TriggerType, "Time", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        return int.MaxValue;
    }

    private sealed record EventSortKey(string EventId, int TriggerTickResolved);
}
