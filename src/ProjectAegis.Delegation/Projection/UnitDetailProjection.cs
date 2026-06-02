namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;

/// <summary>Builds right-panel unit detail from snapshot, registry, and order log (presentation only).</summary>
public static class UnitDetailProjection
{
    public static UnitDetailEntry? ProjectSelected(
        TargetId unitId,
        Func<TargetId, bool> isAlive,
        DecisionLog log,
        ScenarioPolicyProfile? policy,
        string? observerUnitId = null)
    {
        var magazineLabel = ResolveMagazineLabel(unitId, log);
        var emconLabel = ResolveEmconLabel(unitId.Value, policy, observerUnitId);
        var doctrineLabel = ResolveDoctrineLabel(unitId.Value, policy);
        var alive = isAlive(unitId);
        return new UnitDetailEntry(
            unitId.Value,
            alive,
            alive ? "OPERATIONAL" : "DESTROYED",
            magazineLabel,
            emconLabel,
            doctrineLabel);
    }

    public static UnitDetailEntry? ProjectPrimary(
        IReadOnlyList<TargetId> memberIds,
        Func<TargetId, bool> isAlive,
        DecisionLog log,
        ScenarioPolicyProfile? policy,
        string? observerUnitId = null)
    {
        if (memberIds.Count == 0)
        {
            return null;
        }

        var primary = memberIds.OrderBy(id => id.Value, StringComparer.Ordinal).First();
        return ProjectSelected(primary, isAlive, log, policy, observerUnitId);
    }

    private static string ResolveMagazineLabel(TargetId unitId, DecisionLog log)
    {
        var last = log.MagazineChanges
            .Where(m => m.ShooterTargetId == unitId)
            .OrderByDescending(m => m.SequenceId)
            .FirstOrDefault();
        return last == null
            ? "MAGAZINE: —"
            : $"MAGAZINE: mount {last.MountId} Δ{last.Delta} ({last.ReasonCode})";
    }

    private static string ResolveEmconLabel(
        string unitId,
        ScenarioPolicyProfile? policy,
        string? observerUnitId)
    {
        if (policy?.UnitRadarEmcon == null ||
            !policy.UnitRadarEmcon.TryGetValue(unitId, out var state))
        {
            return "EMCON: —";
        }

        _ = observerUnitId;
        return state == EmconState.Active ? "EMCON: ACTIVE" : "EMCON: OFF";
    }

    private static string ResolveDoctrineLabel(string unitId, ScenarioPolicyProfile? policy)
    {
        if (policy == null)
        {
            return "DOCTRINE: —";
        }

        var effective = policy.ResolveForUnit(unitId, isFriendly: true);
        return $"DOCTRINE: {effective.Roe}";
    }
}