namespace ProjectAegis.Delegation.UnityAdapter.CommandReview;

using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Targets;
using ProjectAegis.Delegation.UnityAdapter.Bridge;

/// <summary>Finds pending coordination scopes that can no longer produce a valid group effect.</summary>
public static class CoordinationScopeReconciler
{
    /// <summary>
    /// Returns scope IDs whose group authority or exact membership was lost, or whose execution
    /// tick passed with no queued human order. Hosts may remove these after each bridge tick.
    /// </summary>
    public static IReadOnlyList<string> FindInvalidOrExpired(
        DelegationBridge bridge,
        IReadOnlyList<CoordinationApprovedScope> pendingScopes,
        ulong currentSimTick)
    {
        if (bridge is null) throw new ArgumentNullException(nameof(bridge));
        if (pendingScopes is null) throw new ArgumentNullException(nameof(pendingScopes));
        var stale = new List<string>();
        foreach (var scope in pendingScopes)
        {
            if (!bridge.Registry.TryGetBinding(scope.GroupEntity, out var binding)
                || binding.Target is not GroupTarget group
                || !string.Equals(group.Id.Value, scope.GroupId, StringComparison.Ordinal)
                || group.Slot.Active is not HumanController human)
            {
                stale.Add(scope.ScopeId);
                continue;
            }

            var currentMembers = group.Members.Select(m => m.Value).OrderBy(m => m, StringComparer.Ordinal);
            if (!currentMembers.SequenceEqual(scope.CapturedMemberIds, StringComparer.Ordinal)
                || (currentSimTick >= scope.ExecuteSimTick && human.PendingOrderCount == 0))
            {
                stale.Add(scope.ScopeId);
            }
        }

        return stale;
    }
}
