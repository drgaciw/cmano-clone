namespace ProjectAegis.Delegation.UnityAdapter.Bridge;

using Core;
using Orchestration;
using Targets;

public sealed class TargetRegistry
{
    private readonly DelegationOrchestrator _orchestrator;
    private readonly Dictionary<EntityKey, SimEntityBinding> _byEntity = new();
    private readonly Dictionary<TargetId, SimEntityBinding> _byTarget = new();
    private readonly List<TargetId> _memberIds = new();
    private readonly HashSet<TargetId> _friendlyMeshMemberIds = new();

    public TargetRegistry(DelegationOrchestrator orchestrator) =>
        _orchestrator = orchestrator;

    public IReadOnlyList<SimEntityBinding> Bindings => _byEntity.Values.ToList();

    public SimEntityBinding RegisterUnit(EntityKey entity, string targetKey)
    {
        var targetId = new TargetId(targetKey);
        var unit = new UnitTarget(targetId);
        return Register(entity, unit);
    }

    public SimEntityBinding RegisterGroup(EntityKey entity, string targetKey)
    {
        var targetId = new TargetId(targetKey);
        var group = new GroupTarget(targetId);
        return Register(entity, group);
    }

    public void LinkGroupMember(TargetId groupId, TargetId memberId)
    {
        if (!_byTarget.TryGetValue(groupId, out var groupBinding) ||
            groupBinding.Target is not GroupTarget group)
        {
            throw new InvalidOperationException($"Group target '{groupId.Value}' is not registered.");
        }

        if (!_byTarget.ContainsKey(memberId))
        {
            throw new InvalidOperationException($"Member target '{memberId.Value}' is not registered.");
        }

        group.AddMember(memberId);
        if (!_memberIds.Contains(memberId))
        {
            _memberIds.Add(memberId);
        }
    }

    public bool TryGetBinding(EntityKey entity, out SimEntityBinding binding) =>
        _byEntity.TryGetValue(entity, out binding!);

    public bool TryGetBinding(TargetId targetId, out SimEntityBinding binding) =>
        _byTarget.TryGetValue(targetId, out binding!);

    private SimEntityBinding Register(EntityKey entity, ICommandableTarget target)
    {
        if (_byEntity.ContainsKey(entity))
        {
            throw new InvalidOperationException($"Entity {entity.Value} is already registered.");
        }

        // BUG fix (qa-r2-08-unity-adapter): only EntityKey uniqueness was validated above. Two
        // different entities registered under the same string target key would silently overwrite
        // the TargetId -> binding mapping in `_byTarget` and append a duplicate TargetId into
        // `_memberIds`, which flows straight into OobTreeProjection/MapPictureBridge/UnitDetailBridge
        // (via CollectMemberIds) — rendering the same unit id twice in the C2 OOB tree/map picture.
        if (_byTarget.ContainsKey(target.Id))
        {
            throw new InvalidOperationException(
                $"Target '{target.Id.Value}' is already registered under a different entity.");
        }

        var binding = new SimEntityBinding(entity, target.Id, target);
        _byEntity[entity] = binding;
        _byTarget[target.Id] = binding;
        _orchestrator.Register(target);

        if (target is UnitTarget)
        {
            _memberIds.Add(target.Id);
        }

        return binding;
    }

    public IReadOnlyList<TargetId> CollectMemberIds() => _memberIds;

    /// <summary>Marks a unit as part of the friendly datalink mesh (side-aware; DRG-190).</summary>
    public void MarkFriendlyMeshMember(TargetId targetId) => _friendlyMeshMemberIds.Add(targetId);

    /// <summary>Clears friendly-mesh marks before <see cref="DelegationBridge.ConfigureSimulationMode"/> rebinds sides.</summary>
    public void ClearFriendlyMeshMembers() => _friendlyMeshMemberIds.Clear();

    /// <summary>Friendly-side mesh members explicitly marked or inferred for network-health projection.</summary>
    public IReadOnlyList<TargetId> CollectFriendlyMeshMemberIds()
    {
        if (_friendlyMeshMemberIds.Count == 0)
        {
            return Array.Empty<TargetId>();
        }

        var ids = new List<TargetId>(_friendlyMeshMemberIds.Count);
        foreach (var memberId in _memberIds)
        {
            if (_friendlyMeshMemberIds.Contains(memberId))
            {
                ids.Add(memberId);
            }
        }

        return ids;
    }
}
