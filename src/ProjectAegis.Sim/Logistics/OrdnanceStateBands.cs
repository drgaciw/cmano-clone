namespace ProjectAegis.Sim.Logistics;

/// <summary>
/// Ordnance readiness bands (aviation doctrine labels for gauntlet logistics variables).
/// Winchester = out of weapons; Shotgun = pre-briefed minimum remaining/defensive ordnance.
/// </summary>
public static class OrdnanceStateBands
{
    public const string Nominal = "NOMINAL";
    public const string Shotgun = "SHOTGUN";
    public const string Winchester = "WINCHESTER";

    /// <summary>
    /// Resolve band from remaining rounds after a successful fire (or current count).
    /// <paramref name="shotgunRoundsThreshold"/> of 0 disables SHOTGUN (only WINCHESTER at 0).
    /// </summary>
    public static string Resolve(int roundsRemaining, int shotgunRoundsThreshold)
    {
        if (roundsRemaining <= 0)
        {
            return Winchester;
        }

        if (shotgunRoundsThreshold > 0 && roundsRemaining <= shotgunRoundsThreshold)
        {
            return Shotgun;
        }

        return Nominal;
    }
}
