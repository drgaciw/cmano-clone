namespace ProjectAegis.MissionEditor.Cli;

using System.Text.Json;

/// <summary>Headless NL plan stub — suggests patrol/strike/ferry/support ops from keywords (req 11).</summary>
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

        if (normalized.Contains("ferry", StringComparison.Ordinal) ||
            normalized.Contains("redeploy", StringComparison.Ordinal))
        {
            suggestions.Add(new
            {
                tool = "mission_add_ferry",
                id = "ferry-1",
                units = new[] { "u1" },
                destination = "base-1",
                template = "tpl-ferry-empty",
            });
            suggestions.Add(new
            {
                tool = "mission_add_from_template",
                template = "tpl-ferry-empty",
                id = "ferry-from-tpl",
                note = "Empty ferry mission template then assign units/destination",
            });
        }

        // "ew" must not match substrings like "redeploy" — use token-ish checks.
        var hasAew = ContainsToken(normalized, "aew");
        var hasEw = ContainsToken(normalized, "ew");
        var hasTanker = normalized.Contains("tanker", StringComparison.Ordinal);
        var hasSupport = normalized.Contains("support", StringComparison.Ordinal);
        if (hasSupport || hasTanker || hasAew || hasEw)
        {
            var role = hasAew ? "AEW" : hasEw ? "EW" : "Tanker";
            suggestions.Add(new
            {
                tool = "mission_add_support",
                id = "support-1",
                units = new[] { "u1" },
                role,
                template = "tpl-support-tanker",
            });
            suggestions.Add(new
            {
                tool = "mission_add_from_template",
                template = "tpl-support-tanker",
                id = "support-from-tpl",
                note = "Support template then assign units/station",
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

        if (normalized.Contains("cyber", StringComparison.Ordinal))
        {
            suggestions.Add(new
            {
                tool = "scenario_cyber_status",
                policyId = "baltic-patrol-comms",
                note = "Cyber abort catalog + comms delay coupling",
            });
        }

        if (normalized.Contains("cca", StringComparison.Ordinal) ||
            normalized.Contains("hypersonic", StringComparison.Ordinal) ||
            normalized.Contains("near-future", StringComparison.Ordinal) ||
            normalized.Contains("near future", StringComparison.Ordinal))
        {
            suggestions.Add(new
            {
                tool = "scenario_near_future_spawn",
                note = "Validate gated CCA/hypersonic spawns from scenario metadata",
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

    /// <summary>True when <paramref name="token"/> appears as a whole token (space/punct delimited).</summary>
    private static bool ContainsToken(string haystack, string token)
    {
        if (string.IsNullOrEmpty(haystack) || string.IsNullOrEmpty(token))
        {
            return false;
        }

        if (haystack == token)
        {
            return true;
        }

        var start = 0;
        while (start <= haystack.Length - token.Length)
        {
            var idx = haystack.IndexOf(token, start, StringComparison.Ordinal);
            if (idx < 0)
            {
                return false;
            }

            var leftOk = idx == 0 || !char.IsLetterOrDigit(haystack[idx - 1]);
            var rightIdx = idx + token.Length;
            var rightOk = rightIdx >= haystack.Length || !char.IsLetterOrDigit(haystack[rightIdx]);
            if (leftOk && rightOk)
            {
                return true;
            }

            start = idx + 1;
        }

        return false;
    }
}