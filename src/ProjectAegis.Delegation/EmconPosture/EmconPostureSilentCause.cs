namespace ProjectAegis.Delegation.EmconPosture;

/// <summary>Named cause when a unit is not fully radiating (DRG-221).</summary>
public enum EmconPostureSilentCause
{
    /// <summary>Unit is radiating per policy — no suppression cause.</summary>
    None = 0,

    /// <summary>Policy EMCON Off — emitters held silent.</summary>
    PolicyOff = 1,

    /// <summary>Policy EMCON Passive / standby — listen-only, no active emissions.</summary>
    StandbyPassive = 2,

    /// <summary>Comms denied — emissions posture suppressed despite policy.</summary>
    CommsDenied = 3,
}
