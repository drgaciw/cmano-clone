namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Derives combat-domain HUD activity tags from message-log categories (ASSET-021 host input).
/// Pure headless mapping — no sim hot-path dependency.
/// Prefer <see cref="CombatDomainsHotTickTracker.ObserveMessageLog"/> when degraded states are required.
/// </summary>
public static class CombatDomainActivityTags
{
    public static IReadOnlyList<string> FromMessageLog(IReadOnlyList<MessageLogLine> lines)
    {
        if (lines == null || lines.Count == 0)
        {
            return Array.Empty<string>();
        }

        var tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in lines)
        {
            switch (line.Category?.ToUpperInvariant())
            {
                case "KILL_CONFIRMED":
                case "HIT":
                    tags.Add("Surface");
                    break;
                case "COMMS":
                case "WEAPON_LAUNCH":
                    tags.Add("Air");
                    break;
                case "CONTACT":
                case "CONTACT_CHANGE":
                    tags.Add("Subsurface");
                    break;
                case "MAGAZINE":
                    tags.Add("Facility");
                    break;
                case "MISSION":
                case "MISSION_TRANSITION":
                    tags.Add("Mine");
                    break;
                // POLICY_DENIAL maps to Degraded Land via tracker only (not a simple "active" tag).
            }
        }

        return tags.Count == 0 ? Array.Empty<string>() : tags.ToArray();
    }
}
