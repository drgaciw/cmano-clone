namespace ProjectAegis.Delegation.UnityAdapter.Bridge;

using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;

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

        var currentPolicy = orchestrator.ResolveEffectivePolicyForUnit(OrderActionMapper.TargetIdToUlong(unitId));
        var newPolicy = new EffectivePolicy(roeLevel, currentPolicy.MaxSalvo);

        orchestrator.DecisionLog.AppendPolicyOverride(new PolicyOverrideRecord(
            0,
            simTime,
            unitId.Value,
            roeLevel.ToString(),
            "UI Override"));

        return true;
    }
}
