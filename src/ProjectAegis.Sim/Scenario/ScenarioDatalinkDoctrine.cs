namespace ProjectAegis.Sim.Scenario;

/// <summary>TR-sensor-004 bounded side-picture sharing; optional share lag (no ECCM Phase 2).</summary>
public sealed record ScenarioDatalinkDoctrine(
    bool OrganicOnly = true,
    IReadOnlyDictionary<string, string>? UnitSides = null,
    int ShareLagTicks = 0,
    bool ShareLagTicksSpecified = false)
{
    public static ScenarioDatalinkDoctrine Default { get; } = new();

    public bool IsSharingEnabled =>
        !OrganicOnly && UnitSides is { Count: > 0 };

    public string ResolveSide(string observerId)
    {
        if (UnitSides != null && UnitSides.TryGetValue(observerId, out var side))
        {
            return side;
        }

        return string.Empty;
    }
}