namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Single source of truth: message-log category → USS state class (ASSET-006).
/// Used by <see cref="MessageLogPanelBinder"/> and Unity hosts so category styling is not duplicated.
/// </summary>
public static class MessageLogCategoryClassMap
{
    public const string BaseRowClass = "message-log-row";
    public const string SelectableRowClass = "message-log-row--selectable";

    /// <summary>
    /// USS modifier class for a category, or null when no dedicated tint applies.
    /// </summary>
    public static string? CssClassFor(string? category)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return null;
        }

        return category.Trim().ToUpperInvariant() switch
        {
            "KILL_CONFIRMED" or "HIT" => "message-log-row--kill",
            "MAGAZINE" => "message-log-row--magazine",
            "COMMS" => "message-log-row--comms",
            "CONTACT" or "CONTACT_CHANGE" => "message-log-row--contact",
            "MISSION" or "MISSION_TRANSITION" => "message-log-row--mission",
            "POLICY_DENIAL" => "message-log-row--policy",
            "WEAPON_LAUNCH" => "message-log-row--weapon",
            _ => null,
        };
    }
}
