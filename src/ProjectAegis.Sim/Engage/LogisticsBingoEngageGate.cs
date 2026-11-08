namespace ProjectAegis.Sim.Engage;

/// <summary>
/// Hard-denies engage when the shooter's fuel band is Bingo (logistics gate v1).
/// Band is primed onto <see cref="EngageContext.LogisticsBingoBlocked"/> by the session/fuel tracker.
/// </summary>
public static class LogisticsBingoEngageGate
{
    public static EngagementAbortReason? Evaluate(in EngageContext ctx) =>
        ctx.LogisticsBingoBlocked
            ? EngagementAbortReason.BingoFuel
            : null;
}
