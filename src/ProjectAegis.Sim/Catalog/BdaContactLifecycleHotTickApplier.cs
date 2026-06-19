namespace ProjectAegis.Sim.Catalog;

using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Sensors;

/// <summary>
/// ADR-009 bounded BDA contact lifecycle hook — maps platform damage rows to sim-kernel Lost transitions.
/// Flag-gated on <c>combatDomainsEnabled</c>; mirrors <c>OrderLogBdaProjection</c> Lost promotion rules.
/// </summary>
public static class BdaContactLifecycleHotTickApplier
{
    public sealed record DamageLifecycleApply(
        string PlatformId,
        int DamageLevel,
        double NewHpPct,
        string ReasonCode);

    public static bool IsEnabled(bool combatDomainsEnabled) => combatDomainsEnabled;

    public static bool ShouldPromoteToLost(in DamageLifecycleApply change)
    {
        if (change.NewHpPct <= 0 ||
            string.Equals(change.ReasonCode, PlatformDamageChangeReasonCodes.Kill, StringComparison.Ordinal))
        {
            return true;
        }

        if (!string.Equals(change.ReasonCode, PlatformDamageChangeReasonCodes.Hit, StringComparison.Ordinal))
        {
            return false;
        }

        return change.DamageLevel >= CombatDamageLevel.MaxLevel;
    }

    public static IReadOnlyList<string> ResolveSortedLostTargets(
        IReadOnlyList<DamageLifecycleApply> changes)
    {
        if (changes.Count == 0)
        {
            return Array.Empty<string>();
        }

        return changes
            .Where(c => ShouldPromoteToLost(c))
            .Select(c => c.PlatformId)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
    }

    public static IReadOnlyList<ContactTransition> ApplySortedTargets(
        PdDetectionContactSimulator simulator,
        ulong simTick,
        double simTime,
        IReadOnlyList<string> sortedTargetIds)
    {
        if (sortedTargetIds.Count == 0)
        {
            return Array.Empty<ContactTransition>();
        }

        var transitions = new List<ContactTransition>(sortedTargetIds.Count);
        foreach (var targetId in sortedTargetIds)
        {
            transitions.AddRange(simulator.ApplyTargetBdaLost(simTick, simTime, targetId));
        }

        return transitions;
    }

    public static IReadOnlyList<ContactTransition> ApplyFromRegistry(
        PdDetectionContactSimulator simulator,
        ulong simTick,
        double simTime,
        BdaContactLifecycleRegistry registry)
    {
        var pending = registry.DrainNewLostTargets();
        return ApplySortedTargets(simulator, simTick, simTime, pending);
    }
}