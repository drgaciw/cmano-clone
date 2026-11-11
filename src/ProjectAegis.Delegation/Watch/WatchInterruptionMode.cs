namespace ProjectAegis.Delegation.Watch;

/// <summary>Controls new watch interruptions; never authorizes orders or acknowledges existing cards.</summary>
public enum WatchInterruptionMode
{
    /// <summary>Pause for every classic watch event and pending approval.</summary>
    All,

    /// <summary>Pause for critical watch events and orders awaiting approval.</summary>
    CriticalOnly,

    /// <summary>Show events without automatically pausing. Approval and resume gates still apply.</summary>
    Manual,
}
