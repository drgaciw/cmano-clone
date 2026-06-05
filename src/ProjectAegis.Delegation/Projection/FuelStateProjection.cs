namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Sim.Scenario;

/// <summary>Fuel readout for unit detail (logistics GDD — scenario thresholds or burn model).</summary>
public static class FuelStateProjection
{
    public static string FormatUnitFuelLine(string unitId, double simTimeSeconds) =>
        FormatUnitFuelLine(unitId, simTimeSeconds, ScenarioLogisticsSettings.Default);

    public static string FormatUnitFuelLine(
        string unitId,
        double simTimeSeconds,
        ScenarioLogisticsSettings logistics)
    {
        var state = ResolveState(simTimeSeconds, logistics);
        if (logistics.UsesFuelBurnModel)
        {
            var remaining = logistics.RemainingFuelKg(simTimeSeconds);
            return $"FUEL: {state} {remaining:F0}kg ({unitId})";
        }

        return $"FUEL: {state} ({unitId})";
    }

    internal static string ResolveState(double simTimeSeconds, ScenarioLogisticsSettings logistics)
    {
        if (logistics.UsesFuelBurnModel)
        {
            var frac = logistics.RemainingFuelFraction(simTimeSeconds);
            if (frac <= logistics.BingoFuelFraction)
            {
                return "BINGO";
            }

            if (frac <= logistics.JokerFuelFraction)
            {
                return "JOKER";
            }

            return "NOMINAL";
        }

        if (simTimeSeconds >= logistics.BingoSimSeconds)
        {
            return "BINGO";
        }

        if (simTimeSeconds >= logistics.JokerSimSeconds)
        {
            return "JOKER";
        }

        return "NOMINAL";
    }
}