namespace ProjectAegis.Delegation.Projection;

/// <summary>MVP fuel readout for unit detail (logistics GDD P0 placeholder).</summary>
public static class FuelStateProjection
{
    public static string FormatUnitFuelLine(string unitId, double simTimeSeconds)
    {
        var state = ResolveState(simTimeSeconds);
        return $"FUEL: {state} ({unitId})";
    }

    private static string ResolveState(double simTimeSeconds)
    {
        if (simTimeSeconds >= 600)
        {
            return "BINGO";
        }

        if (simTimeSeconds >= 300)
        {
            return "JOKER";
        }

        return "NOMINAL";
    }
}