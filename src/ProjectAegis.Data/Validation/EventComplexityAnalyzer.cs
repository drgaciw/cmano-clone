namespace ProjectAegis.Data.Validation;

using ProjectAegis.Data.Scenario.Authoring;

/// <summary>Event graph complexity warnings (GDD §4.3).</summary>
public static class EventComplexityAnalyzer
{
    public static IReadOnlyList<ValidationFinding> Analyze(
        ScenarioDocumentDto scenario,
        ValidationConfig config)
    {
        var findings = new List<ValidationFinding>();
        var events = scenario.Events;
        if (events.Count == 0)
        {
            return findings;
        }

        foreach (var evt in events)
        {
            if (evt.Conditions.Count > config.MaxConditionsPerEvent)
            {
                findings.Add(new ValidationFinding(
                    "EVENT_CONDITION_CAP",
                    ValidationSeverity.Error,
                    $"Event '{evt.Id}' exceeds {config.MaxConditionsPerEvent} conditions.",
                    Data: new Dictionary<string, string> { ["eventId"] = evt.Id }));
            }
        }

        var crossRefs = CountCrossReferences(events);
        var complexity = events.Count +
                         events.Sum(e => e.Conditions.Count) +
                         (config.CrossRefWeight * crossRefs);
        var peakDensity = events
            .GroupBy(e => e.Trigger.AtTick ?? 0)
            .Max(g => g.Count());

        if (complexity > config.WarnThreshold)
        {
            findings.Add(new ValidationFinding(
                "EVENT_COMPLEXITY_WARN",
                ValidationSeverity.Warning,
                $"Scenario event complexity {complexity} exceeds warn threshold {config.WarnThreshold}.",
                Data: new Dictionary<string, string> { ["complexity"] = complexity.ToString() }));
        }

        if (peakDensity > config.DensityThreshold)
        {
            findings.Add(new ValidationFinding(
                "EVENT_DENSITY_WARN",
                ValidationSeverity.Warning,
                $"Peak tick event density {peakDensity} exceeds threshold {config.DensityThreshold}.",
                Data: new Dictionary<string, string> { ["peak_tick_density"] = peakDensity.ToString() }));
        }

        return findings;
    }

    private static int CountCrossReferences(IReadOnlyList<ScenarioEventDto> events)
    {
        var missionIds = new HashSet<string>(StringComparer.Ordinal);
        var variableKeys = new HashSet<string>(StringComparer.Ordinal);
        var edges = 0;

        foreach (var evt in events)
        {
            foreach (var action in evt.Actions)
            {
                if (!string.IsNullOrWhiteSpace(action.MissionId))
                {
                    missionIds.Add(action.MissionId);
                }

                if (string.Equals(action.Type, "SetVariable", StringComparison.OrdinalIgnoreCase))
                {
                    variableKeys.Add(action.UnitId ?? action.MissionId ?? evt.Id);
                }
            }
        }

        foreach (var evt in events)
        {
            foreach (var cond in evt.Conditions)
            {
                if (!string.IsNullOrWhiteSpace(cond.ZoneId) && missionIds.Contains(cond.ZoneId))
                {
                    edges++;
                }
            }
        }

        return edges + variableKeys.Count;
    }
}
