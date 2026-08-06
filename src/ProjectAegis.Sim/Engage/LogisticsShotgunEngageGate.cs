namespace ProjectAegis.Sim.Engage;

using ProjectAegis.Sim.Logistics;

/// <summary>
/// Soft logistics gate: when remaining ordnance is in the Shotgun band, multi-salvo
/// engage (SalvoSize > 1) is denied. Single-round defensive residual fire is allowed.
/// Threshold 0 disables the gate (matches <see cref="OrdnanceStateBands"/>).
/// </summary>
public static class LogisticsShotgunEngageGate
{
    public static EngagementAbortReason? Evaluate(
        int roundsRemaining,
        int salvoSize,
        int shotgunRoundsThreshold)
    {
        if (shotgunRoundsThreshold <= 0 || Math.Max(1, salvoSize) <= 1)
        {
            return null;
        }

        var band = OrdnanceStateBands.Resolve(roundsRemaining, shotgunRoundsThreshold);
        return band == OrdnanceStateBands.Shotgun
            ? EngagementAbortReason.ShotgunOrdnance
            : null;
    }

    public static EngagementAbortReason? Evaluate(in EngageContext ctx, int liveRoundsRemaining) =>
        Evaluate(liveRoundsRemaining, ctx.SalvoSize, ctx.ShotgunRoundsThreshold);
}
