namespace ProjectAegis.Delegation.MissionIntent;

/// <summary>Named mission constraints injected as facts (never silently dropped).</summary>
public static class MissionIntentConstraintCode
{
    /// <summary>Hold constraint active — movement / fires withheld.</summary>
    public const string Hold = "HOLD";

    /// <summary>No-strike constraint active for the scoped target set.</summary>
    public const string NoStrike = "NO_STRIKE";

    /// <summary>ROE withhold — authority facts mirror Skills disposition without coupling.</summary>
    public const string RoeWithhold = "ROE_WITHHOLD";
}
