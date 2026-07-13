namespace ProjectAegis.Sim.Scenario;

/// <summary>Minimal side map for Baltic v3 headless OOB (u1/hostile-1 + UCAV pair).</summary>
public static class BalticV3SideRegistry
{
    public static string? GetSideForUnit(string unitId) =>
        unitId switch
        {
            "u1" or "ucav-blue" => "blue",
            "hostile-1" or "ucav-red" => "red",
            _ => null,
        };

    public static bool IsBlueForceUnit(string unitId) =>
        string.Equals(GetSideForUnit(unitId), "blue", StringComparison.Ordinal);

    public static bool IsRedForceUnit(string unitId) =>
        string.Equals(GetSideForUnit(unitId), "red", StringComparison.Ordinal);
}
