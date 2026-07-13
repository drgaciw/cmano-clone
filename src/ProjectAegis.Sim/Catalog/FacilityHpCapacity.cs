namespace ProjectAegis.Sim.Catalog;

/// <summary>
/// GDD combat-domains-damage § Facility: maps ledger HP% to P1 facility capacity labels.
/// </summary>
public static class FacilityHpCapacity
{
    public const string Operational = "Operational";

    public const string Damaged = "Damaged";

    public const string Destroyed = "Destroyed";

    public const double DestroyedHpThreshold = 0.0;

    public const double OperationalHpThreshold = 100.0;

    public static string MapHpPctToCapacityState(double hpPct)
    {
        var clamped = Math.Clamp(hpPct, 0.0, 100.0);
        if (clamped <= DestroyedHpThreshold)
        {
            return Destroyed;
        }

        if (clamped >= OperationalHpThreshold)
        {
            return Operational;
        }

        return Damaged;
    }

    public static bool ShouldEmitCapacityTransition(string previousState, string nextState) =>
        !string.Equals(previousState, Destroyed, StringComparison.Ordinal) &&
        !string.Equals(previousState, nextState, StringComparison.Ordinal) &&
        !(string.Equals(previousState, Damaged, StringComparison.Ordinal) &&
          string.Equals(nextState, Damaged, StringComparison.Ordinal));
}