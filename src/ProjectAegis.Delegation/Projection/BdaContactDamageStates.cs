namespace ProjectAegis.Delegation.Projection;

/// <summary>Order-log BDA contact lifecycle labels for bounded damage-level projection (ADR-009 / TR-combat-dom-003).</summary>
public static class BdaContactDamageStates
{
    public const string DegradedL1 = "Degraded-L1";

    public const string DegradedL2 = "Degraded-L2";

    public const string Lost = "Lost";

    public static string? MapDamageLevel(int damageLevel) =>
        damageLevel switch
        {
            1 => DegradedL1,
            2 => DegradedL2,
            >= 3 => Lost,
            _ => null,
        };

    public static int Rank(string state) =>
        state switch
        {
            DegradedL1 => 1,
            DegradedL2 => 2,
            Lost => 3,
            _ => 0,
        };
}