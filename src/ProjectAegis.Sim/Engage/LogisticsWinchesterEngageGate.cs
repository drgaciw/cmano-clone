namespace ProjectAegis.Sim.Engage;

/// <summary>
/// Hard-denies engage when remaining ordnance is Winchester (rounds ≤ 0).
/// </summary>
public static class LogisticsWinchesterEngageGate
{
    public static EngagementAbortReason? Evaluate(int roundsRemaining) =>
        roundsRemaining <= 0
            ? EngagementAbortReason.WinchesterOrdnance
            : null;
}
