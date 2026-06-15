namespace ProjectAegis.Data.Scenario.Authoring;

/// <summary>Deterministic event fire order (GDD §4.2).</summary>
public static class EventFireOrderCalculator
{
    public static IReadOnlyList<string> ComputeFireOrder(IReadOnlyList<ScenarioEventDto> events)
    {
        return events
            .Select(e => new EventSortKey(
                e.Id,
                ResolveTriggerTick(e),
                e.Priority))
            .OrderBy(k => k.TriggerTickResolved)
            .ThenBy(k => k.Priority)
            .ThenBy(k => k.EventId, StringComparer.Ordinal)
            .Select(k => k.EventId)
            .ToArray();
    }

    private static int ResolveTriggerTick(ScenarioEventDto evt)
    {
        if (string.Equals(evt.Trigger.Type, "Time", StringComparison.OrdinalIgnoreCase))
        {
            return evt.Trigger.AtTick ?? 0;
        }

        return int.MaxValue;
    }

    private sealed record EventSortKey(string EventId, int TriggerTickResolved, int Priority);
}
