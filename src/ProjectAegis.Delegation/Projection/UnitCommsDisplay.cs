namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Delegation.Comms;

/// <summary>
/// CMD-17: formats own-unit status labels that treat out-of-comms as a first-class display state
/// (named unknown, not blank/zero). Presentation-only.
/// </summary>
public static class UnitCommsDisplay
{
    public const string Destroyed = "DESTROYED";
    public const string Operational = "OPERATIONAL";
    public const string OperationalCommsDegraded = "OPERATIONAL · COMMS DEGRADED";
    public const string UnknownOutOfComms = "UNKNOWN (Out of comms)";

    /// <summary>
    /// Status line body (without "STATUS:" prefix) from alive flag + net/unit comms state.
    /// Destroyed always wins; denied → named unknown; degraded → operational with comms note.
    /// </summary>
    public static string FormatStatus(bool isAlive, CommsState unitOrNetComms)
    {
        if (!isAlive)
        {
            return Destroyed;
        }

        return unitOrNetComms switch
        {
            CommsState.Denied => UnknownOutOfComms,
            CommsState.Degraded => OperationalCommsDegraded,
            _ => Operational,
        };
    }
}
