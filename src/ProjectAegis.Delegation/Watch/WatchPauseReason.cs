namespace ProjectAegis.Delegation.Watch;

/// <summary>
/// Headless reason code for why the sim clock was auto-paused.
/// Presentation maps this to labels; sim only stores the enum.
/// </summary>
public enum WatchPauseReason : byte
{
    None = 0,
    HostileOrUnknownContact = 1,
    OwnSideLossOrDamage = 2,
    ExplicitPlayer = 3,
}
