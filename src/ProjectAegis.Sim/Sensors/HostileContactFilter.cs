namespace ProjectAegis.Sim.Sensors;

/// <summary>Engage-primary gating: only hostile platform ids feed MVP engage victim selection.</summary>
public static class HostileContactFilter
{
    public static bool IsEngageableHostileTarget(string targetId) =>
        targetId.StartsWith("hostile", StringComparison.Ordinal);
}
