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

        // Allocation follow-up: explicit loop + HashSet + final sort (no LINQ chain).
        // Output identical to prior Distinct+OrderBy because final sort on unique ids (Ordinal).
        // Preserves determinism for replay/hash (lexicographic id order on lost targets).
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var ids = new List<string>(changes.Count);
        for (int i = 0; i < changes.Count; i++)
        {
            var c = changes[i];
            if (ShouldPromoteToLost(c))
            {
                var id = c.PlatformId;
                if (seen.Add(id))
                {
                    ids.Add(id);
                }
            }
        }
        ids.Sort(StringComparer.Ordinal);
        return ids.ToArray();
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