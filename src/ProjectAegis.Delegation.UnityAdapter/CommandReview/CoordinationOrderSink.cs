namespace ProjectAegis.Delegation.UnityAdapter.CommandReview;

using Core;
using Targets;
using Bridge;

/// <summary>
/// Downstream order-sink adapter that expands a verified group-targeted order to current live
/// member entities. Non-group orders pass through unchanged.
/// </summary>
public sealed class CoordinationOrderSink : IOrderSink
{
    private readonly TargetRegistry _registry;
    private readonly ISimWorldSnapshot _snapshot;
    private readonly IOrderSink _downstream;
    private readonly IReadOnlyList<CoordinationApprovedScope> _approvedScopes;
    private readonly HashSet<string> _consumedScopeIds = new(StringComparer.Ordinal);

    /// <summary>Scope identifiers already consumed by an emitted order.</summary>
    public IReadOnlyCollection<string> ConsumedScopeIds => _consumedScopeIds;

    public CoordinationOrderSink(
        TargetRegistry registry,
        ISimWorldSnapshot snapshot,
        IOrderSink downstream,
        IReadOnlyList<CoordinationApprovedScope> approvedScopes)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
        _downstream = downstream ?? throw new ArgumentNullException(nameof(downstream));
        _approvedScopes = approvedScopes ?? throw new ArgumentNullException(nameof(approvedScopes));
    }

    /// <summary>Apply a unit order or expand one group order after validating the full member scope.</summary>
    public void ApplyOrder(EntityKey entity, in Order order)
    {
        CoordinationApprovedScope? scope = null;
        for (var i = 0; i < _approvedScopes.Count; i++)
        {
            var candidate = _approvedScopes[i];
            if (!_consumedScopeIds.Contains(candidate.ScopeId)
                && candidate.GroupEntity == entity
                && candidate.Kind == order.Kind
                && candidate.SimTime.Equals(order.SimTime)
                && string.Equals(candidate.GroupId, order.Target.Value, StringComparison.Ordinal))
            {
                scope = candidate;
                break;
            }
        }
        if (scope is null)
        {
            _downstream.ApplyOrder(entity, order);
            return;
        }

        _consumedScopeIds.Add(scope.ScopeId);

        if (!_registry.TryGetBinding(entity, out var binding)
            || binding.Target is not GroupTarget group
            || group.Slot.Active is not Controllers.HumanController)
        {
            return;
        }

        var currentIds = group.Members.Select(id => id.Value).OrderBy(id => id, StringComparer.Ordinal).ToArray();
        if (!currentIds.SequenceEqual(scope.CapturedMemberIds, StringComparer.Ordinal))
        {
            return;
        }

        var members = new List<SimEntityBinding>(group.Members.Count);
        for (var i = 0; i < group.Members.Count; i++)
        {
            var memberId = group.Members[i];
            if (!_registry.TryGetBinding(memberId, out var memberBinding)
                || memberBinding.Target is not UnitTarget
                || ((UnitTarget)memberBinding.Target).IsDetachedFromGroup
                || !_snapshot.IsMemberAlive(memberId))
            {
                return;
            }

            members.Add(memberBinding);
        }

        foreach (var member in members.OrderBy(m => m.TargetId.Value, StringComparer.Ordinal))
        {
            var memberOrder = order with { Target = member.TargetId };
            _downstream.ApplyOrder(member.Entity, memberOrder);
        }
    }
}
