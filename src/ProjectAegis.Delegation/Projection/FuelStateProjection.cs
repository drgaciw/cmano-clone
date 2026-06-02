namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Sim.Scenario;

/// <summary>Fuel readout for unit detail (logistics GDD — scenario thresholds).</summary>
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
        return $"FUEL: {state} ({unitId})";
    }

    internal static string ResolveState(double simTimeSeconds, ScenarioLogisticsSettings logistics)
    {
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