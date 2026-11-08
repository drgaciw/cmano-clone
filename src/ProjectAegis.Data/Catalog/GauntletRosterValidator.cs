using System.Text.Json;

namespace ProjectAegis.Data.Catalog;

/// <summary>
/// Oracle-0 style resolution: every detection observer/target (and optional catalogRefs)
/// in a gauntlet policy must appear in the tier roster drawn from the catalog DB.
/// </summary>
public static class GauntletRosterValidator
{
    public static IReadOnlyList<string> ValidatePolicyAgainstRoster(string policyJson, string rosterJson)
    {
        using var policy = JsonDocument.Parse(policyJson);
        using var roster = JsonDocument.Parse(rosterJson);
        return ValidatePolicyAgainstRoster(policy.RootElement, roster.RootElement);
    }

    public static IReadOnlyList<string> ValidatePolicyAgainstRoster(JsonElement policy, JsonElement roster)
    {
        var issues = new List<string>();
        var rosterIds = new HashSet<string>(StringComparer.Ordinal);
        if (roster.TryGetProperty("platforms", out var platforms) && platforms.ValueKind == JsonValueKind.Array)
        {
            foreach (var p in platforms.EnumerateArray())
            {
                if (p.TryGetProperty("platformId", out var id) && id.ValueKind == JsonValueKind.String)
                {
                    var s = id.GetString();
                    if (!string.IsNullOrEmpty(s))
                    {
                        rosterIds.Add(s);
                    }
                }
            }
        }

        if (policy.TryGetProperty("detection", out var detection) && detection.ValueKind == JsonValueKind.Array)
        {
            var i = 0;
            foreach (var d in detection.EnumerateArray())
            {
                if (d.TryGetProperty("observerId", out var obs) && obs.ValueKind == JsonValueKind.String)
                {
                    var o = obs.GetString() ?? "";
                    if (!rosterIds.Contains(o))
                    {
                        issues.Add($"detection[{i}].observerId '{o}' not in roster");
                    }
                }

                if (d.TryGetProperty("targetId", out var tgt) && tgt.ValueKind == JsonValueKind.String)
                {
                    var t = tgt.GetString() ?? "";
                    if (!rosterIds.Contains(t))
                    {
                        issues.Add($"detection[{i}].targetId '{t}' not in roster");
                    }
                }

                i++;
            }
        }

        if (policy.TryGetProperty("gauntlet", out var g)
            && g.TryGetProperty("catalogRefs", out var refs)
            && refs.ValueKind == JsonValueKind.Array)
        {
            foreach (var r in refs.EnumerateArray())
            {
                if (r.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                var id = r.GetString() ?? "";
                if (!string.IsNullOrEmpty(id) && !rosterIds.Contains(id))
                {
                    issues.Add($"catalogRefs '{id}' not in roster");
                }
            }
        }

        return issues;
    }
}
