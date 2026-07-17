namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Derives combat-domain HUD activity tags from message-log categories (ASSET-021 host input).
/// Pure headless mapping — no sim hot-path dependency.
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
            switch (line.Category)
            {
                case "KILL_CONFIRMED":
                case "HIT":
                    tags.Add("Surface");
                    break;
                case "COMMS":
                    tags.Add("Air");
                    break;
                case "CONTACT":
                case "CONTACT_CHANGE":
                    tags.Add("Subsurface");
                    break;
            }
        }

        return tags.Count == 0 ? Array.Empty<string>() : tags.ToArray();
    }
}
