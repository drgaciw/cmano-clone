namespace ProjectAegis.Sim.Sensors;

/// <summary>
/// Thin projection helper for SWARM-26 contact classification labels (UI / C2 later).
/// Pure string formatting only — no Unity.
/// </summary>
public static class SwarmContactLabel
{
    /// <summary>
    /// Format a classification result for projection, e.g. <c>UAS swarm cloud (0.82)</c>.
    /// </summary>
    public static string Format(SwarmContactClassificationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        var name = result.Class switch
        {
            SwarmContactClass.Unknown => "Unknown",
            SwarmContactClass.SingleAirframe => "Single airframe",
            SwarmContactClass.UasSwarmCloud => "UAS swarm cloud",
            SwarmContactClass.PossibleSwarm => "Possible swarm",
            _ => result.Class.ToString(),
        };
        return $"{name} ({result.Confidence:0.00})";
    }
}
