namespace ProjectAegis.MissionEditor.Cli;

using System.Text.Json;

/// <summary>Headless NL plan stub — suggests patrol/strike ops from keywords (req 11).</summary>
public static class MissionPlanSuggestCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static int Run(string intent, TextWriter output)
    {
        if (string.IsNullOrWhiteSpace(intent))
        {
            Console.Error.WriteLine("mission_plan_suggest requires --intent");
            return 2;
        }

        var normalized = intent.ToLowerInvariant();
        var suggestions = new List<object>();
        if (normalized.Contains("patrol", StringComparison.Ordinal))
        {
            suggestions.Add(new
            {
                tool = "mission_add_patrol",
                id = "patrol-1",
                units = new[] { "u1" },
                waypoints = new[] { "57,20", "57.1,20.1", "57.2,20.2" },
            });
        }

        if (normalized.Contains("strike", StringComparison.Ordinal) || normalized.Contains("attack", StringComparison.Ordinal))
        {
            suggestions.Add(new
            {
                tool = "mission_add_strike",
                id = "strike-1",
                units = new[] { "u1" },
                targets = new[] { "hostile-1" },
            });
        }

        if (normalized.Contains("baltic", StringComparison.Ordinal))
        {
            suggestions.Add(new
            {
                tool = "scenario_create",
                policyId = "baltic-patrol",
                seed = 42,
                note = "Baltic vertical-slice policy preset",
            });
        }

        if (normalized.Contains("roe", StringComparison.Ordinal) || normalized.Contains("doctrine", StringComparison.Ordinal))
        {
            suggestions.Add(new
            {
                tool = "scenario_validate",
                policyId = "baltic-patrol-mission-roe",
                note = "Validate mission ROE inheritance fixture after edits",
            });
        }

        if (normalized.Contains("comms", StringComparison.Ordinal) || normalized.Contains("ew", StringComparison.Ordinal))
        {
            suggestions.Add(new
            {
                tool = "scenario_comms_status",
                policyId = normalized.Contains("comms", StringComparison.Ordinal)
                    ? "baltic-patrol-comms"
                    : "baltic-patrol",
                note = "Inspect comms display + timeline before simulate",
            });
        }

        if (suggestions.Count == 0)
        {
            suggestions.Add(new
            {
                tool = "scenario_validate",
                note = "No mission keyword matched; validate scenario before adding missions.",
            });
        }

        var payload = new { ok = true, intent, suggestions };
        output.WriteLine(JsonSerializer.Serialize(payload, JsonOptions));
        return 0;
    }
}