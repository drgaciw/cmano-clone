namespace ProjectAegis.Delegation.Orchestration;

using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Roe;
using ProjectAegis.Sim.Policy;

public sealed class PolicySnapshotRegistry
{
    private ulong _nextSnapshotId = 1;
    private readonly Dictionary<TargetId, PolicySnapshot> _byTarget = new();

    public IReadOnlyDictionary<TargetId, PolicySnapshot> Snapshots => _byTarget;

    public ulong Capture(TargetId targetId, EffectivePolicy effective, ulong capturedAtSimTick)
    {
        var snapshot = new PolicySnapshot(_nextSnapshotId++, effective, capturedAtSimTick);
        _byTarget[targetId] = snapshot;
        return snapshot.PolicySnapshotId;
    }

    public bool TryGet(TargetId targetId, out PolicySnapshot snapshot) =>
        _byTarget.TryGetValue(targetId, out snapshot!);

    public PolicyContext CreateContext(Order order)
    {
        var unitId = OrderActionMapper.TargetIdToUlong(order.Target);
        var simTick = (ulong)Math.Max(0, (long)order.SimTime);
        if (_byTarget.TryGetValue(order.Target, out var snap))
        {
            return new PolicyContext(unitId, snap.PolicySnapshotId, simTick, snap.Effective);
        }

        return new PolicyContext(unitId, 0, simTick, EffectivePolicy.DefaultFree);
    }
}
