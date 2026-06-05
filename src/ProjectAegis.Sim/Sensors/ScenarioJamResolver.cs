namespace ProjectAegis.Sim.Sensors;

using ProjectAegis.Sim.Scenario;

/// <summary>Resolves effective noise jam strength for a detection trial.</summary>
public static class ScenarioJamResolver
{
    public static double ResolveJam(
        string observerId,
        string targetId,
        ulong simTick,
        IReadOnlyList<ScenarioJammer> jammers)
    {
        if (jammers.Count == 0)
        {
            return 0;
        }

        var strength = 0.0;
        foreach (var jammer in jammers)
        {
            if (simTick < jammer.ActiveFromTick)
            {
                continue;
            }

            if (!string.Equals(jammer.TargetId, targetId, StringComparison.Ordinal))
            {
                continue;
            }

            if (jammer.ObserverId != null &&
                !string.Equals(jammer.ObserverId, observerId, StringComparison.Ordinal))
            {
                continue;
            }

            strength = Math.Max(strength, jammer.JamStrength);
        }

        return Math.Clamp(strength, 0, 1);
    }
}