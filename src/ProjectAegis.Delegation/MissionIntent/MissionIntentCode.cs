namespace ProjectAegis.Delegation.MissionIntent;

/// <summary>Named mission-command intent codes (closed set; never silently dropped).</summary>
public static class MissionIntentCode
{
    /// <summary>Hold position / withhold offensive action.</summary>
    public const string Hold = "HOLD";

    /// <summary>Explicit no-strike posture for the scoped unit or group.</summary>
    public const string NoStrike = "NO_STRIKE";

    /// <summary>Positive execute / attack intent (advisory projection only — not an order).</summary>
    public const string Attack = "ATTACK";
}
