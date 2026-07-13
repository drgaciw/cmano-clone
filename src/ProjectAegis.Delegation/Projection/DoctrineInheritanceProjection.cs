namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Delegation.Core;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;

/// <summary>Doctrine inheritance view model for UI binding (req 13 P0).</summary>
public sealed record DoctrineInheritanceEntry(
    string UnitId,
    string EffectiveRoeLabel,
    string EffectiveMaxSalvoLabel,
    string EffectiveEmconLabel,
    string InheritanceSource,
    bool IsInheritedFromMission,
    bool HasLocalOverride,
    string OverrideButtonLabel);

/// <summary>Builds doctrine inheritance panel state from scenario policy (presentation only).</summary>
public static class DoctrineInheritanceProjection
{
    public static DoctrineInheritanceEntry? ProjectUnit(
        TargetId unitId,
        ScenarioPolicyProfile? policy,
        bool isFriendly)
    {
        if (policy == null)
        {
            return new DoctrineInheritanceEntry(
                unitId.Value,
                "ROE: —",
                "SALVO: —",
                "EMCON: —",
                "SOURCE: —",
                false,
                false,
                "OVERRIDE: UNAVAILABLE");
        }

        var resolved = policy.ResolveUnitPolicy(unitId.Value, isFriendly);
        var effective = resolved.Effective;
        var source = ResolveInheritanceSource(resolved, unitId.Value, policy);
        var hasOverride = policy.UnitOverrides?.ContainsKey(unitId.Value) ?? false;

        return new DoctrineInheritanceEntry(
            unitId.Value,
            $"ROE: {effective.Roe}",
            $"SALVO: {effective.MaxSalvo}",
            FormatRadarEmconLabel(unitId.Value, policy),
            source,
            resolved.HasInheritedDoctrineFromMission,
            hasOverride,
            hasOverride ? "OVERRIDE: ACTIVE" : "OVERRIDE: NONE");
    }

    public static IReadOnlyList<DoctrineInheritanceEntry> ProjectAllUnits(
        IReadOnlyList<TargetId> unitIds,
        ScenarioPolicyProfile? policy,
        bool isFriendly)
    {
        var results = new List<DoctrineInheritanceEntry>();
        foreach (var unitId in unitIds.OrderBy(id => id.Value, StringComparer.Ordinal))
        {
            var entry = ProjectUnit(unitId, policy, isFriendly);
            if (entry != null)
            {
                results.Add(entry);
            }
        }
        return results;
    }

    private static string ResolveInheritanceSource(
        ResolvedUnitPolicy resolved,
        string unitId,
        ScenarioPolicyProfile policy)
    {
        if (resolved.HasInheritedDoctrineFromMission)
        {
            return "SOURCE: Mission";
        }

        if (policy.UnitOverrides?.ContainsKey(unitId) ?? false)
        {
            return "SOURCE: Unit Override";
        }

        return "SOURCE: Scenario Default";
    }

    private static string FormatRadarEmconLabel(string unitId, ScenarioPolicyProfile policy)
    {
        var state = ScenarioEmconResolver.ResolveRadar(unitId, policy.UnitRadarEmcon);
        return state switch
        {
            EmconState.Active => "EMCON: ACTIVE",
            EmconState.Passive => "EMCON: PASSIVE",
            _ => "EMCON: OFF",
        };
    }
}
