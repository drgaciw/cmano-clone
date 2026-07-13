using ProjectAegis.Sim.Scenario;

namespace ProjectAegis.Sim.Sensors;

/// <summary>
/// Decides whether a detection/contact target id is an engageable hostile.
/// Legacy synthetic ids use the <c>hostile*</c> prefix; catalog red ORBAT units
/// must be registered via <see cref="BalticV3SideRegistry"/> (not kebab-case heuristics).
/// </summary>
public static class HostileContactFilter
{
    public static bool IsEngageableHostileTarget(string targetId)
    {
        if (string.IsNullOrWhiteSpace(targetId))
        {
            return false;
        }

        // Never engage blue force (synthetic or catalog).
        if (BalticV3SideRegistry.IsBlueForceUnit(targetId)
            || string.Equals(targetId, "u1", StringComparison.Ordinal))
        {
            return false;
        }

        // Catalog red (scenario-registered) or legacy synthetic hostiles.
        if (BalticV3SideRegistry.IsRedForceUnit(targetId))
        {
            return true;
        }

        return targetId.StartsWith("hostile", StringComparison.Ordinal);
    }
}
