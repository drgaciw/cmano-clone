namespace ProjectAegis.Delegation.MissionIntent;

/// <summary>Advisory retask recommendation — never enqueues orders or retask execution.</summary>
public enum MissionIntentRetaskAdvice
{
    /// <summary>No retask recommendation.</summary>
    None = 0,

    /// <summary>Advisory withdraw recommendation (IsOrder remains false).</summary>
    Withdraw = 1,

    /// <summary>Advisory re-attack recommendation (IsOrder remains false).</summary>
    ReAttack = 2,
}
