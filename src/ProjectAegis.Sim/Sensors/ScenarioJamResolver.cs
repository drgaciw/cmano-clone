namespace ProjectAegis.Sim.Sensors;

using Scenario;

/// <summary>Resolves effective noise jam strength for a detection trial.</summary>
public static class ScenarioJamResolver
{
    public static double ResolveJam(
        string observerId,
        string targetId,
        ulong simTick,
        IReadOnlyList<ScenarioJammer> jammers) =>
        ResolveJam(observerId, targetId, simTick, jammers, RadarEccmProfile.Unspecified);

    /// <summary>
    /// EW-01 / DRG-386: each eligible jammer's strength is scaled by
    /// <see cref="EccmJamMatrix.Effectiveness"/> against the observer radar's ECCM profile before
    /// taking the strongest. An unspecified profile and generation yield the legacy result.
    /// </summary>
    public static double ResolveJam(
        string observerId,
        string targetId,
        ulong simTick,
        IReadOnlyList<ScenarioJammer> jammers,
        in RadarEccmProfile radarProfile)
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

            var raw = Math.Clamp(jammer.JamStrength, 0, 1);
            var effective = raw * EccmJamMatrix.Effectiveness(in radarProfile, jammer.TechGeneration);
            strength = Math.Max(strength, effective);
        }

        return Math.Clamp(strength, 0, 1);
    }
}
