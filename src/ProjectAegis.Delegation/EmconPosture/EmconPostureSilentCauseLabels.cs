namespace ProjectAegis.Delegation.EmconPosture;

/// <summary>Stable display labels for EMCON posture silent causes (DRG-221).</summary>
public static class EmconPostureSilentCauseLabels
{
    public const string PolicyOff = "policy EMCON off";
    public const string StandbyPassive = "passive / standby";
    public const string CommsDenied = "comms denied";

    public static string Format(EmconPostureSilentCause cause) =>
        cause switch
        {
            EmconPostureSilentCause.PolicyOff => PolicyOff,
            EmconPostureSilentCause.StandbyPassive => StandbyPassive,
            EmconPostureSilentCause.CommsDenied => CommsDenied,
            _ => string.Empty,
        };
}
