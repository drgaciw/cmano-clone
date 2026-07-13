namespace ProjectAegis.Delegation.UnityAdapter.Bridge;

using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Roe;
using ProjectAegis.Sim.Policy;

/// <summary>Headless doctrine override command handler (req 13 P0, ADR-010).</summary>
public static class DoctrineOverrideCommand
{
    public static bool TryApply(
        DelegationOrchestrator orchestrator,
        TargetId unitId,
        string roeLevelLabel,
        double simTime)
    {
        if (orchestrator == null || !Enum.TryParse<RoeLevel>(roeLevelLabel, ignoreCase: true, out var roeLevel))
        {
            return false;
        }

        var unitKey = OrderActionMapper.TargetIdToUlong(unitId);
        var currentPolicy = orchestrator.ResolveEffectivePolicyForUnit(unitKey);
        if (currentPolicy.Roe == roeLevel)
        {
            return false;
        }

        var newPolicy = new EffectivePolicy(roeLevel, currentPolicy.MaxSalvo);
        var simTick = (ulong)Math.Max(0, (long)simTime);
        var snapshotId = orchestrator.PolicySnapshots.Capture(unitId, newPolicy, simTick);

        orchestrator.DecisionLog.AppendPolicyUpdate(new PolicyUpdateRecord(
            0,
            simTime,
            simTick,
            snapshotId,
            "roe",
            currentPolicy.Roe.ToString(),
            roeLevel.ToString()));

        return true;
    }
}